using System.Collections.Generic;
using UnityEngine;
using ProjectFelia.EditorXR.Core;

namespace ProjectFelia.DesktopStandalone
{
    public sealed class DesktopHierarchyPanel : MonoBehaviour
    {
        private const int MaxHierarchyNodesPerFrame = 256;

        private const int MaxHierarchyDepth = 8;

        [SerializeField]
        private XREditorState editorState;

        [SerializeField]
        private DesktopEditorCameraController cameraController;

        [SerializeField]
        private Rect hierarchyRect = new Rect(12f, 72f, 320f, 560f);

        [SerializeField]
        private Rect toolbarRect = new Rect(12f, 12f, 420f, 44f);

        [SerializeField, Min(0.1f)]
        private float menuScale = 0f;

        [SerializeField]
        private float referenceWidth = 1600f;

        [SerializeField]
        private float referenceHeight = 900f;

        [SerializeField]
        private bool showWorkspaceMenu = true;

        // Адаптивно подреждане: options панелът се поставя вдясно от hierarchy, за да не се притиска UI-то.
        private Rect OptionsRect
        {
            get
            {
                var contentRect = ContentRect;
                var hierarchyWidth = GetHierarchyWidth(contentRect.width);
                var w = Mathf.Max(280f, contentRect.width - hierarchyWidth - 12f);
                var x = contentRect.x + hierarchyWidth + 12f;
                var y = contentRect.y;
                var h = contentRect.height;
                return new Rect(x, y, w, h);
            }
        }

        private Rect ContentRect
        {
            get
            {
                var margin = 12f;
                var top = ToolbarRect.yMax + 12f;
                var height = Mathf.Max(320f, Screen.height - top - margin);
                return new Rect(margin, top, Screen.width - margin * 2f, height);
            }
        }

        // Изчислени правоъгълници за по-адаптивно подреждане.
        private Rect ToolbarRect
        {
            get
            {
                return new Rect(12f, 12f, Screen.width - 24f, 56f);
            }
        }

        private Rect HierarchyRect
        {
            get
            {
                var contentRect = ContentRect;
                var w = GetHierarchyWidth(contentRect.width);
                var x = contentRect.x;
                var y = contentRect.y;
                var h = contentRect.height;
                return new Rect(x, y, w, h);
            }
        }

        private static float GetHierarchyWidth(float contentWidth)
        {
            return Mathf.Max(320f, contentWidth - Mathf.Clamp(contentWidth * 0.28f, 300f, 420f) - 12f);
        }

        private Vector2 scrollPosition;

        private GUIStyle selectedStyle;

        private GUIStyle objectStyle;

        private void Awake()
        {
            if (editorState == null)
            {
                var bootstrap = FindObjectOfType<DesktopStandaloneEditorBootstrap>();
                var editorCameraController = FindObjectOfType<DesktopEditorCameraController>();
                if (bootstrap != null)
                {
                    Bind(bootstrap.EditorState, editorCameraController);
                }
            }
        }

        public void Bind(XREditorState state, DesktopEditorCameraController editorCameraController)
        {
            editorState = state;
            cameraController = editorCameraController;
        }

        private void InitializeStyles()
        {
            if (selectedStyle != null && objectStyle != null)
            {
                return;
            }

            selectedStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                normal =
                {
                    textColor = Color.white
                }
            };

            objectStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft
            };
        }

        private void OnGUI()
        {
            if (editorState == null)
            {
                return;
            }

            DesktopAppSettings.Load();
            menuScale = DesktopAppSettings.AdaptiveScale ? 0f : DesktopAppSettings.MenuScale;
            var adaptive = DesktopGuiUtility.GetAdaptiveScale(menuScale, referenceWidth, referenceHeight);
            var previousMatrix = DesktopGuiUtility.PushScale(adaptive);
            InitializeStyles();

            DrawToolbar();
            if (showWorkspaceMenu)
            {
                DrawHierarchy();
            }

            DesktopGuiUtility.PopScale(previousMatrix);
        }

        private void DrawToolbar()
        {
            GUILayout.BeginArea(ToolbarRect, GUI.skin.box);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(showWorkspaceMenu ? "Close Menu" : "Open Menu", GUILayout.Width(110f), GUILayout.Height(34f)))
            {
                showWorkspaceMenu = !showWorkspaceMenu;
            }

            GUILayout.Space(6);

            if (!showWorkspaceMenu)
            {
                GUILayout.Label("Scene Hierarchy и editor инструментите са скрити.");
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Main Menu", GUILayout.Width(120f), GUILayout.Height(34f)))
                {
                    ProjectWorkspaceService.RequestMainMenu();
                    if (cameraController != null) cameraController.ResetView();
                }

                GUILayout.EndHorizontal();
                GUILayout.EndArea();
                return;
            }

            GUILayout.Space(6);
            DrawToolButton("Select", XREditorToolMode.Select);
            GUILayout.Space(8);
            DrawToolButton("Move", XREditorToolMode.Move);
            GUILayout.Space(8);
            DrawToolButton("Rotate", XREditorToolMode.Rotate);
            GUILayout.Space(8);
            DrawToolButton("Scale", XREditorToolMode.Scale);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset View", GUILayout.Width(120f), GUILayout.Height(34f)))
            {
                if (cameraController != null) cameraController.ResetView();
            }

            if (GUILayout.Button("Main Menu", GUILayout.Width(120f), GUILayout.Height(34f)))
            {
                ProjectWorkspaceService.RequestMainMenu();
                if (cameraController != null) cameraController.ResetView();
            }

            if (GUILayout.Button("Frame Selected", GUILayout.Width(150f), GUILayout.Height(34f)))
            {
                FrameSelectedObject();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // Options панелът е вдясно от hierarchy, за да се избегне припокриване.
            GUILayout.BeginArea(OptionsRect, GUI.skin.box);
            DrawOptionsPanel();
            GUILayout.EndArea();
        }

        private void DrawToolButton(string label, XREditorToolMode toolMode)
        {
            var isActive = editorState.ToolMode == toolMode;
            var buttonStyle = isActive ? selectedStyle : objectStyle;

            if (GUILayout.Button(label, buttonStyle, GUILayout.Width(80f)))
            {
                editorState.SetToolMode(toolMode);
            }
        }

        private void DrawHierarchy()
        {
            GUILayout.BeginArea(HierarchyRect, GUI.skin.box);
            GUILayout.Label("Scene Hierarchy");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var remainingNodeBudget = MaxHierarchyNodesPerFrame;
            for (var index = 0; index < roots.Length; index++)
            {
                if (!DrawHierarchyObject(roots[index].transform, 0, ref remainingNodeBudget))
                {
                    break;
                }
            }

            if (remainingNodeBudget <= 0)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Hierarchy truncated to keep the editor responsive.");
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private bool DrawHierarchyObject(Transform target, int depth, ref int remainingNodeBudget)
        {
            if (remainingNodeBudget <= 0 || depth > MaxHierarchyDepth)
            {
                return false;
            }

            remainingNodeBudget--;

            GUILayout.BeginHorizontal();
            GUILayout.Space(depth * 18f);

            var isSelected = editorState.SelectedTarget == target;
            var content = isSelected ? $"> {target.name}" : target.name;
            var buttonStyle = isSelected ? selectedStyle : objectStyle;

            if (GUILayout.Button(content, buttonStyle, GUILayout.Height(28f)))
            {
                editorState.SetSelection(target);
                if (cameraController != null)
                {
                    cameraController.SetFocus(target);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            for (var childIndex = 0; childIndex < target.childCount; childIndex++)
            {
                if (!DrawHierarchyObject(target.GetChild(childIndex), depth + 1, ref remainingNodeBudget))
                {
                    break;
                }
            }

            return remainingNodeBudget > 0;
        }

        private void FrameSelectedObject()
        {
            var selectedTarget = editorState.SelectedTarget;
            if (selectedTarget == null || cameraController == null)
            {
                return;
            }

            var renderers = selectedTarget.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                cameraController.SetFocus(selectedTarget);
                return;
            }

            var bounds = renderers[0].bounds;
            for (var index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }

            cameraController.FrameBounds(bounds);
            cameraController.SetFocus(selectedTarget);
        }

        private void DrawOptionsPanel()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("GUI Options");

            DesktopAppSettings.Load();
            var adaptiveScale = DesktopAppSettings.AdaptiveScale;
            var scaleValue = DesktopAppSettings.MenuScale <= 0.01f ? 1f : DesktopAppSettings.MenuScale;

            adaptiveScale = GUILayout.Toggle(adaptiveScale, "Adaptive menu scaling");
            GUI.enabled = !adaptiveScale;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Menu scale", GUILayout.Width(90f));
            scaleValue = GUILayout.HorizontalSlider(scaleValue, 0.75f, 1.5f);
            GUILayout.Label(scaleValue.ToString("0.00"), GUILayout.Width(44f));
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            if (GUILayout.Button("Apply"))
            {
                DesktopAppSettings.AdaptiveScale = adaptiveScale;
                DesktopAppSettings.SetMenuScale(adaptiveScale ? 0f : scaleValue);
                DesktopAppSettings.Save();
                menuScale = DesktopAppSettings.MenuScale;
            }

            GUILayout.Space(8f);
            GUILayout.Label("Controls");
            GUILayout.Label("1 Select  2 Move  3 Rotate  4 Scale");
            GUILayout.Label("LMB select, RMB orbit, MMB pan, wheel zoom");

            GUILayout.EndVertical();
        }
    }
}