using UnityEngine;
using ProjectFelia.EditorXR.Core;
using ProjectFelia.EditorXR.Input;
using ProjectFelia.EditorXR.Manipulation;
using ProjectFelia.EditorXR.Selection;

namespace ProjectFelia.DesktopStandalone
{
    public sealed class DesktopStandaloneEditorBootstrap : MonoBehaviour
    {
        public XREditorState EditorState => state;

        [SerializeField]
        private Camera editorCamera;

        [SerializeField]
        private Transform pointerOrigin;

        [SerializeField, Min(0.1f)]
        private float selectionDistance = 100f;

        [SerializeField]
        private int selectableLayerMask = ~0;

        [SerializeField]
        private DesktopEditorCameraController cameraController;

        [SerializeField]
        private DesktopHierarchyPanel hierarchyPanel;

        [SerializeField]
        private bool autoActivateOnStart = true;

        [SerializeField]
        private bool enabledWhenWorkspaceIsOpen = true;

        private XREditorState state;

        private XREditorInputRouter inputRouter;

        private XREditorSelectionService selectionService;

        private XREditorManipulator manipulator;

        private bool isReturningToMainMenu;

        private AsyncOperation pendingMainMenuLoadOperation;

        private bool hasFocus = true;

        private void Awake()
        {
            Debug.Log("Project Felia editor bootstrap awake.");
            var currentWorkspace = ProjectWorkspaceService.CurrentWorkspace;
            state = new XREditorState();
            inputRouter = new XREditorInputRouter();
            selectionService = new XREditorSelectionService(state)
            {
                MaxSelectionDistance = selectionDistance,
                SelectableLayerMask = selectableLayerMask
            };
            manipulator = new XREditorManipulator(state);

            if (currentWorkspace == null)
            {
                enabled = false;
                return;
            }

            if (editorCamera == null)
            {
                editorCamera = Camera.main;
            }

            if (cameraController == null && editorCamera != null)
            {
                cameraController = editorCamera.GetComponent<DesktopEditorCameraController>();
            }

            if (cameraController != null)
            {
                cameraController.SetFocus(state.SelectedTarget);
            }

            if (hierarchyPanel == null)
            {
                var panelObject = new GameObject("DesktopHierarchyPanel");
                hierarchyPanel = panelObject.AddComponent<DesktopHierarchyPanel>();
            }

            hierarchyPanel.Bind(state, cameraController);

            Debug.Log(currentWorkspace == null
                ? "Project Felia editor bootstrap disabled: no workspace."
                : $"Project Felia editor bootstrap ready for workspace: {currentWorkspace.ProjectPath}");
        }

        private void Start()
        {
            Debug.Log("Project Felia editor bootstrap start.");
            if (!autoActivateOnStart)
            {
                enabled = false;
                return;
            }

            state.SelectionChanged += HandleSelectionChanged;

            if (!enabledWhenWorkspaceIsOpen)
            {
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (state != null)
            {
                state.SelectionChanged -= HandleSelectionChanged;
            }
        }

        private void Update()
        {
            if (!hasFocus)
            {
                return;
            }

            if (pendingMainMenuLoadOperation != null)
            {
                return;
            }

            if (IsGuiCapturingInput())
            {
                return;
            }

            // Clear any lingering IMGUI keyboard focus so editor keyboard shortcuts work
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                GUIUtility.keyboardControl = 0;
            }

            if (ProjectWorkspaceService.MainMenuRequested && !isReturningToMainMenu)
            {
                isReturningToMainMenu = true;
                Debug.Log("Project Felia returning to main menu.");
                pendingMainMenuLoadOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Launcher");
                return;
            }

            if (inputRouter.ClearSelectionPressed)
            {
                state.ClearSelection();
            }

            var requestedMode = inputRouter.GetRequestedToolMode();
            if (requestedMode.HasValue)
            {
                state.SetToolMode(requestedMode.Value);
            }

            if (inputRouter.SelectPressedThisFrame)
            {
                var ray = inputRouter.CreatePointerRay(editorCamera, pointerOrigin);
                selectionService.TrySelect(ray, out _);
            }

            manipulator.ApplyKeyboardShortcutInput(Time.deltaTime);
        }

        private static bool IsGuiCapturingInput()
        {
            return GUIUtility.hotControl != 0 || GUIUtility.keyboardControl != 0;
        }

        private void HandleSelectionChanged(Transform selectedTarget)
        {
            if (cameraController != null)
            {
                cameraController.SetFocus(selectedTarget);
            }
        }

        private void OnApplicationFocus(bool focusStatus)
        {
            hasFocus = focusStatus;
            Debug.Log($"Project Felia editor focus: {focusStatus}");
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log($"Project Felia editor pause: {pauseStatus}");
        }
    }
}