using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using ProjectFelia.EditorXR.Core;

namespace ProjectFelia.DesktopStandalone
{
    public sealed class DesktopLauncherBootstrap : MonoBehaviour
    {
        [SerializeField]
        private Rect launcherRect = new Rect(16f, 16f, 420f, 220f);

        [SerializeField]
        private Rect createRect = new Rect(460f, 16f, 420f, 320f);

        [SerializeField, Min(0.1f)]
        private float menuScale = 0f;

        [SerializeField]
        private float referenceWidth = 1600f;

        [SerializeField]
        private float referenceHeight = 900f;

        [SerializeField]
        private string openProjectPath = string.Empty;

        [SerializeField]
        private string newProjectName = "MyProject";

        [SerializeField]
        private string newProjectParentPath = string.Empty;

        [SerializeField]
        private string editorSceneName = "DesktopEditor";

        [SerializeField]
        private Vector2 projectListSize = new Vector2(390f, 180f);

        [SerializeField]
        private Vector2 infoBoxSize = new Vector2(390f, 120f);

        private ProjectWorkspace workspace;

        private bool hasOpenedWorkspace;

        private Vector2 recentScroll;

        private IReadOnlyList<string> recentProjects;

        private enum FolderPickTarget
        {
            None,
            OpenProject,
            CreateProjectParent
        }

        private FolderPickTarget pendingFolderPickTarget = FolderPickTarget.None;

        private bool suppressAutoRestore;

        private string pendingWorkspacePathToOpen;

        private bool pendingCreateWorkspace;

        private AsyncOperation pendingSceneLoadOperation;

        private string pendingSceneName;

        private bool hasFocus = true;

        private void Awake()
        {
            Debug.Log("Project Felia launcher booting.");
            DesktopAppSettings.Load();
            // Осигурява, че launcher-ът се показва при всяко стартиране, като изключва auto-restore по време на изпълнение.
            DesktopAppSettings.AutoRestoreLastProject = false;
            menuScale = DesktopAppSettings.AdaptiveScale ? 0f : DesktopAppSettings.MenuScale;
            suppressAutoRestore = ProjectWorkspaceService.ConsumeMainMenuRequest();
            recentProjects = RecentProjectsStore.Load();
            workspace = DesktopProjectLauncher.ResolveWorkspace();
            if (workspace != null)
            {
                openProjectPath = workspace.ProjectPath;
                newProjectParentPath = Path.GetDirectoryName(workspace.ProjectPath);
                return;
            }

            if (DesktopAppSettings.AutoRestoreLastProject)
            {
                var mostRecentProjectPath = RecentProjectsStore.GetMostRecent();
                if (!string.IsNullOrWhiteSpace(mostRecentProjectPath))
                {
                    openProjectPath = mostRecentProjectPath;
                }
            }

            // Позволява да изчистваме launcher инстанциите, когато се зареждат нови сцени.
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Създава SceneActivationHelper рано, за да няма състезание при стартиране на отложено зареждане.
            SceneActivationHelper.Ensure();
        }

        private void Start()
        {
            Debug.Log("Project Felia launcher start.");
            if (suppressAutoRestore)
            {
                return;
            }

            if (workspace != null)
            {
                QueueOpenWorkspace(workspace.ProjectPath);
                return;
            }

            if (DesktopAppSettings.AutoRestoreLastProject && !string.IsNullOrWhiteSpace(openProjectPath))
            {
                QueueOpenWorkspace(openProjectPath);
            }
        }

        private void OnEnable()
        {
            // Показва launcher-а само в сцената Launcher.
            // Ако компонентът съществува в други сцени, изключва GUI рендерирането.
            var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            enabled = active.Equals("Launcher", StringComparison.OrdinalIgnoreCase) || active.Equals("Launcher.unity", StringComparison.OrdinalIgnoreCase);
        }

        private void Update()
        {
            if (!hasFocus)
            {
                return;
            }

            if (pendingSceneLoadOperation != null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(pendingWorkspacePathToOpen))
            {
                var workspacePath = pendingWorkspacePathToOpen;
                pendingWorkspacePathToOpen = string.Empty;
                TryOpenWorkspace(workspacePath);
            }

            if (pendingCreateWorkspace)
            {
                pendingCreateWorkspace = false;
                TryCreateWorkspace();
            }
        }

        private void OnGUI()
        {
            ConsumeFolderPickerResult();

            // Изчислява адаптивните правоъгълници според текущия размер на екрана.
            var screenPadding = 16f;
            var panelWidth = Mathf.Clamp(Screen.width * 0.28f, 340f, 520f);
            var createPanelWidth = Mathf.Clamp(Screen.width * 0.36f, 420f, 760f);
            var optionsPanelWidth = Mathf.Clamp(Screen.width * 0.22f, 260f, 420f);
            launcherRect.width = panelWidth;
            // Закача launcher-а в горната част на екрана, за да не покрива важни обекти от сцената.
            launcherRect.height = Mathf.Clamp(220f, 180f, Screen.height * 0.35f);
            createRect.width = createPanelWidth;
            createRect.height = Mathf.Clamp(Screen.height * 0.55f, 240f, Screen.height - 64f);

            var totalWidth = launcherRect.width + 16f + createRect.width + 16f + optionsPanelWidth;
            var totalHeight = Mathf.Max(launcherRect.height, createRect.height, Mathf.Min(320f, Screen.height - 64f));
            // Центрира хоризонтално, но го фиксира горе със screenPadding отместване.
            launcherRect.x = Mathf.Max(screenPadding, (Screen.width - totalWidth) * 0.5f);
            launcherRect.y = screenPadding;
            createRect.x = launcherRect.xMax + 16f;
            createRect.y = launcherRect.y;

            var previousMatrix = DesktopGuiUtility.PushScale(DesktopGuiUtility.GetAdaptiveScale(menuScale, referenceWidth, referenceHeight));

            if (pendingSceneLoadOperation != null)
            {
                var loadingRect = new Rect(launcherRect.x, launcherRect.y, launcherRect.width, 80f);
                GUILayout.BeginArea(loadingRect, GUI.skin.window);
                GUILayout.Label($"Loading {pendingSceneName}...");
                GUILayout.EndArea();
                DesktopGuiUtility.PopScale(previousMatrix);
                return;
            }

            if (hasOpenedWorkspace)
            {
                DesktopGuiUtility.PopScale(previousMatrix);
                return;
            }

            GUILayout.BeginArea(launcherRect, GUI.skin.window);
            DrawNavigationRow();
            GUILayout.Label("Open Project");
            GUILayout.Label("Open an existing project folder.");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Path", GUILayout.Width(40f));
            openProjectPath = GUILayout.TextField(openProjectPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80f)))
            {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                if (DesktopFolderPicker.TryBeginPickFolderMac())
                {
                    pendingFolderPickTarget = FolderPickTarget.OpenProject;
                }
#else
                if (DesktopFolderPicker.TryPickFolder(out var pickedPath))
                {
                    openProjectPath = pickedPath;
                }
#endif
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Project", GUILayout.Height(28f)))
            {
                QueueOpenWorkspace(openProjectPath);
            }

            if (GUILayout.Button("Restore Last", GUILayout.Height(28f)))
            {
                var lastProjectPath = RecentProjectsStore.GetMostRecent();
                if (!string.IsNullOrWhiteSpace(lastProjectPath))
                {
                    openProjectPath = lastProjectPath;
                    QueueOpenWorkspace(openProjectPath);
                }
            }

            if (GUILayout.Button("Refresh Recents", GUILayout.Height(28f)))
            {
                recentProjects = RecentProjectsStore.Load();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.EndArea();

            // Options panel sits to the right of the centered launcher cluster.
            var optionsRect = new Rect(createRect.xMax + 16f, launcherRect.y, optionsPanelWidth, Mathf.Min(320f, Screen.height - 64f));
            GUILayout.BeginArea(optionsRect, GUI.skin.window);
            DrawOptionsPanel();
            GUILayout.EndArea();

            GUILayout.BeginArea(createRect, GUI.skin.window);
            GUILayout.Label("Create Project");
            GUILayout.Label("Create a new project folder on disk.");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", GUILayout.Width(40f));
            newProjectName = GUILayout.TextField(newProjectName);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Location", GUILayout.Width(55f));
            newProjectParentPath = GUILayout.TextField(newProjectParentPath);
            if (GUILayout.Button("Browse", GUILayout.Width(80f)))
            {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                if (DesktopFolderPicker.TryBeginPickFolderMac())
                {
                    pendingFolderPickTarget = FolderPickTarget.CreateProjectParent;
                }
#else
                if (DesktopFolderPicker.TryPickFolder(out var projectFolder))
                {
                    newProjectParentPath = projectFolder;
                }
#endif
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Create Project", GUILayout.Height(28f)))
            {
                pendingCreateWorkspace = true;
            }

            GUILayout.Space(8f);
            DrawRecentProjects();

            GUILayout.Space(8f);
            DrawProjectInfo();

            GUILayout.Label("Launch with -projectPath or -projectFile to skip this screen.");
            GUILayout.EndArea();

            DesktopGuiUtility.PopScale(previousMatrix);
        }

        private void DrawNavigationRow()
        {
            GUILayout.BeginHorizontal();

            var hasReturnProject = ProjectWorkspaceService.HasWorkspace || workspace != null;
            GUI.enabled = hasReturnProject;
            if (GUILayout.Button("Back to Project", GUILayout.Height(24f)))
            {
                OpenCurrentWorkspace();
            }

            GUI.enabled = true;

            GUI.enabled = false;
            GUILayout.Button("Main Menu", GUILayout.Height(24f));
            GUI.enabled = true;

            if (GUILayout.Button("Refresh", GUILayout.Height(24f)))
            {
                recentProjects = RecentProjectsStore.Load();
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
        }

        private void DrawRecentProjects()
        {
            GUILayout.Label("Recent Projects");
            recentScroll = GUILayout.BeginScrollView(recentScroll, GUILayout.Height(projectListSize.y));

            if (recentProjects == null || recentProjects.Count == 0)
            {
                GUILayout.Label("No recent projects yet.");
            }
            else
            {
                for (var index = 0; index < recentProjects.Count; index++)
                {
                    var recentProjectPath = recentProjects[index];
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(Path.GetFileName(recentProjectPath), GUILayout.Width(160f)))
                    {
                        openProjectPath = recentProjectPath;
                        QueueOpenWorkspace(openProjectPath);
                    }

                    GUILayout.Label(recentProjectPath);
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();
        }

        private void DrawProjectInfo()
        {
            GUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(infoBoxSize.y));
            if (workspace == null)
            {
                GUILayout.Label("Project Info");
                GUILayout.Label("Open a project to view its manifest.");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.Label("Project Info");
            GUILayout.Label($"Name: {workspace.Manifest?.projectName ?? workspace.ProjectName}");
            GUILayout.Label($"Version: {workspace.Manifest?.projectVersion ?? "Unknown"}");
            GUILayout.Label($"Path: {workspace.ProjectPath}");
            GUILayout.Label($"Manifest: {ProjectManifestStore.ManifestFileName}");
            GUILayout.EndVertical();
        }

        private void TryOpenWorkspace(string pathToOpen)
        {
            if (string.IsNullOrWhiteSpace(pathToOpen))
            {
                return;
            }

            try
            {
                Debug.Log($"Opening workspace: {pathToOpen}");
                workspace = ProjectWorkspace.Open(pathToOpen);
                ProjectWorkspaceService.SetWorkspace(workspace);
                RecentProjectsStore.Add(workspace.ProjectPath);
                recentProjects = RecentProjectsStore.Load();
                BeginSceneLoad(editorSceneName);
                hasOpenedWorkspace = true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to open workspace at '{pathToOpen}': {exception}");
                workspace = null;
                hasOpenedWorkspace = false;
            }
        }

        private void TryCreateWorkspace()
        {
            if (string.IsNullOrWhiteSpace(newProjectParentPath) || string.IsNullOrWhiteSpace(newProjectName))
            {
                return;
            }

            try
            {
                Debug.Log($"Creating workspace: {newProjectName} in {newProjectParentPath}");
                workspace = ProjectCreator.Create(newProjectParentPath, newProjectName);
                openProjectPath = workspace.ProjectPath;
                ProjectWorkspaceService.SetWorkspace(workspace);
                RecentProjectsStore.Add(workspace.ProjectPath);
                recentProjects = RecentProjectsStore.Load();
                BeginSceneLoad(editorSceneName);
                hasOpenedWorkspace = true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to create workspace in '{newProjectParentPath}': {exception}");
                workspace = null;
                hasOpenedWorkspace = false;
            }
        }

        private void OpenCurrentWorkspace()
        {
            var currentWorkspace = ProjectWorkspaceService.CurrentWorkspace ?? workspace;
            if (currentWorkspace == null)
            {
                return;
            }

            QueueOpenWorkspace(currentWorkspace.ProjectPath);
        }

        private void QueueOpenWorkspace(string pathToOpen)
        {
            pendingWorkspacePathToOpen = pathToOpen;
        }

        private void BeginSceneLoad(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || pendingSceneLoadOperation != null)
            {
                return;
            }

            pendingSceneName = sceneName;
            Debug.Log($"Starting async scene load (deferred activation): {sceneName}");
            Debug.Log($"BeginSceneLoad: launcher activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}, enabled={enabled}");
            // Стартира асинхронното зареждане, но отлага активирането, за да остане UI-то отзивчиво.
            pendingSceneLoadOperation = SceneManager.LoadSceneAsync(sceneName);
            if (pendingSceneLoadOperation == null)
            {
                Debug.LogError($"Failed to start async scene load: {sceneName}");
                pendingSceneName = null;
                return;
            }

            // Отлага активирането, за да може engine-ът да довърши тежката начална работа.
            pendingSceneLoadOperation.allowSceneActivation = false;
            // Използва persistent helper за активирането, за да не зависи от enabled/active състоянието на launcher-а.
            SceneActivationHelper.Ensure().BeginDeferredActivation(pendingSceneLoadOperation, pendingSceneName);
        }

        private System.Collections.IEnumerator DeferredActivateScene(AsyncOperation op)
        {
            // Изчаква зареждането да достигне 90% (прага на Unity).
            // Добавя периодично логване и timeout, за да не виси безкрайно.
            var frameCount = 0;
            var logIntervalFrames = 60; // log roughly once per second at 60fps
            var timeoutFrames = 900; // ~15 seconds timeout

            while (op != null && op.progress < 0.9f)
            {
                frameCount++;
                if ((frameCount % logIntervalFrames) == 0)
                {
                    Debug.Log($"Scene load progress ({pendingSceneName}): {op.progress:F3} (frame {frameCount})");
                }

                if (frameCount >= timeoutFrames)
                {
                    Debug.LogWarning($"Scene load timeout reached for {pendingSceneName}; forcing activation.");
                    break;
                }

                // Изчаква един кадър, за да остане UI-то живо.
                yield return null;
            }

            // Дава още един кадър на UI-то да се обнови преди активиране.
            yield return null;

            if (op != null)
            {
                Debug.Log($"Activating scene: {pendingSceneName}");
                op.allowSceneActivation = true;
                // Wait until activation completes
                while (!op.isDone)
                {
                    yield return null;
                }
            }
            // Изчиства състоянието след активирането на сцената.
            pendingSceneLoadOperation = null;
            pendingSceneName = null;

            // Ако launcher-ът още присъства в новоактивираната сцена, изключва GUI-то му.
            // Това покрива случаите, в които инстанция е сериализирана в editor сцената.
            try
            {
                var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (!currentScene.Equals("Launcher", StringComparison.OrdinalIgnoreCase))
                {
                    // Изключва този компонент, за да не рисува върху editor сцената.
                    enabled = false;
                    // Гарантира и че GameObject-ът не остава активен в новата сцена.
                    try { gameObject.SetActive(false); } catch (Exception) { }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"DeferredActivateScene: failed to auto-disable launcher: {e}");
            }
        }

        // Called by SceneActivationHelper when activation completes.
        public void OnSceneActivationCompleted()
        {
            pendingSceneLoadOperation = null;
            pendingSceneName = null;

            try
            {
                var currentScene = SceneManager.GetActiveScene().name;
                if (!currentScene.Equals("Launcher", StringComparison.OrdinalIgnoreCase))
                {
                    enabled = false;
                    try { gameObject.SetActive(false); } catch (Exception) { }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"OnSceneActivationCompleted: {e}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            try
            {
                if (!scene.name.Equals("Launcher", StringComparison.OrdinalIgnoreCase))
                {
                    // Find and disable any launcher instances in the newly loaded scene.
                    var all = FindObjectsOfType<DesktopLauncherBootstrap>();
                    foreach (var l in all)
                    {
                        try
                        {
                            l.enabled = false;
                            l.gameObject.SetActive(false);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"OnSceneLoaded: failed to cleanup launcher instances: {e}");
            }
        }

        private void ConsumeFolderPickerResult()
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (!DesktopFolderPicker.TryConsumePickedFolderMac(out var pickedFolder))
            {
                return;
            }

            switch (pendingFolderPickTarget)
            {
                case FolderPickTarget.OpenProject:
                    openProjectPath = pickedFolder;
                    break;
                case FolderPickTarget.CreateProjectParent:
                    newProjectParentPath = pickedFolder;
                    break;
            }

            pendingFolderPickTarget = FolderPickTarget.None;
#endif
        }

        private void OnApplicationFocus(bool focusStatus)
        {
            hasFocus = focusStatus;
            Debug.Log($"Project Felia launcher focus: {focusStatus}");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log($"Project Felia launcher pause: {pauseStatus}");
        }

        private void DrawOptionsPanel()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("GUI Options");

            DesktopAppSettings.Load();
            var adaptiveScale = DesktopAppSettings.AdaptiveScale;
            var autoRestore = DesktopAppSettings.AutoRestoreLastProject;
            var pauseOnFocusLoss = DesktopAppSettings.PauseOnFocusLoss;
            var scaleValue = DesktopAppSettings.MenuScale <= 0.01f ? 1f : DesktopAppSettings.MenuScale;

            adaptiveScale = GUILayout.Toggle(adaptiveScale, "Adaptive menu scaling");
            GUI.enabled = !adaptiveScale;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Menu scale", GUILayout.Width(90f));
            scaleValue = GUILayout.HorizontalSlider(scaleValue, 0.75f, 1.5f);
            GUILayout.Label(scaleValue.ToString("0.00"), GUILayout.Width(44f));
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            autoRestore = GUILayout.Toggle(autoRestore, "Auto-restore last project");
            pauseOnFocusLoss = GUILayout.Toggle(pauseOnFocusLoss, "Pause on focus loss");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply"))
            {
                DesktopAppSettings.AdaptiveScale = adaptiveScale;
                DesktopAppSettings.AutoRestoreLastProject = autoRestore;
                DesktopAppSettings.PauseOnFocusLoss = pauseOnFocusLoss;
                DesktopAppSettings.SetMenuScale(adaptiveScale ? 0f : scaleValue);
                DesktopAppSettings.Save();
                Application.runInBackground = !pauseOnFocusLoss;
                menuScale = DesktopAppSettings.MenuScale;
            }

            if (GUILayout.Button("Reset"))
            {
                DesktopAppSettings.ResetToDefaults();
                menuScale = DesktopAppSettings.MenuScale;
                Application.runInBackground = !DesktopAppSettings.PauseOnFocusLoss;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}

// Helper to run deferred scene activation on a persistent object that won't be disabled.
public class SceneActivationHelper : MonoBehaviour
{
    private static SceneActivationHelper instance;

    private struct PendingActivation
    {
        public AsyncOperation op;
        public string sceneName;
        public int frameCount;
        public bool activated;
    }

    private readonly List<PendingActivation> pending = new List<PendingActivation>();

    public static SceneActivationHelper Ensure()
    {
        if (instance != null)
        {
            return instance;
        }

        var go = new GameObject("SceneActivationHelper");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneActivationHelper>();
        return instance;
    }

    public void BeginDeferredActivation(AsyncOperation op, string sceneName)
    {
        if (op == null)
        {
            return;
        }

        pending.Add(new PendingActivation { op = op, sceneName = sceneName, frameCount = 0, activated = false });
    }

    private void Update()
    {
        if (pending.Count == 0)
            return;

        for (int i = pending.Count - 1; i >= 0; i--)
        {
            var p = pending[i];
            if (p.op == null)
            {
                pending.RemoveAt(i);
                continue;
            }

            if (!p.activated)
            {
                p.frameCount++;
                if ((p.frameCount % 60) == 0)
                {
                    Debug.Log($"Scene load progress ({p.sceneName}): {p.op.progress:F3} (frame {p.frameCount})");
                }

                if (p.frameCount >= 900)
                {
                    Debug.LogWarning($"Scene load timeout reached for {p.sceneName}; forcing activation.");
                    p.op.allowSceneActivation = true;
                    p.activated = true;
                }
                else if (p.op.progress >= 0.9f)
                {
                    Debug.Log($"Activating scene: {p.sceneName}");
                    p.op.allowSceneActivation = true;
                    p.activated = true;
                }

                pending[i] = p;
            }
            else
            {
                // Already requested activation; wait for completion then notify and cleanup
                if (p.op.isDone)
                {
                    try
                    {
                        var all = FindObjectsOfType<ProjectFelia.DesktopStandalone.DesktopLauncherBootstrap>();
                        foreach (var l in all)
                        {
                            try { l.OnSceneActivationCompleted(); } catch (Exception) { }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"SceneActivationHelper: failed to notify launcher instances: {e}");
                    }

                    pending.RemoveAt(i);
                }
            }
        }
    }
}