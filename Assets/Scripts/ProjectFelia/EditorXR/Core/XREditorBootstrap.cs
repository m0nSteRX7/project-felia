using UnityEngine;
using ProjectFelia.EditorXR.Input;
using ProjectFelia.EditorXR.Manipulation;
using ProjectFelia.EditorXR.Selection;

namespace ProjectFelia.EditorXR.Core
{
    public sealed class XREditorBootstrap : MonoBehaviour
    {
        [SerializeField]
        private Camera editorCamera;

        [SerializeField]
        private Transform pointerOrigin;

        [SerializeField, Min(0.1f)]
        private float selectionDistance = 100f;

        [SerializeField]
        private int selectableLayerMask = ~0;

        private XREditorState state;

        private XREditorInputRouter inputRouter;

        private XREditorSelectionService selectionService;

        private XREditorManipulator manipulator;

        private void Awake()
        {
            state = new XREditorState();
            inputRouter = new XREditorInputRouter();
            selectionService = new XREditorSelectionService(state)
            {
                MaxSelectionDistance = selectionDistance,
                SelectableLayerMask = selectableLayerMask
            };
            manipulator = new XREditorManipulator(state);

            if (editorCamera == null)
            {
                editorCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (GUIUtility.hotControl != 0 || GUIUtility.keyboardControl != 0)
            {
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
    }
}