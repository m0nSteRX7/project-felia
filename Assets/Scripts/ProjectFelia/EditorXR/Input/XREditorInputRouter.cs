using UnityEngine;
using ProjectFelia.EditorXR.Core;

namespace ProjectFelia.EditorXR.Input
{
    public sealed class XREditorInputRouter
    {
        public bool SelectPressedThisFrame => global::UnityEngine.Input.GetMouseButtonDown(0);

        public bool ClearSelectionPressed => global::UnityEngine.Input.GetKeyDown(KeyCode.Escape);

        public XREditorToolMode? GetRequestedToolMode()
        {
            if (global::UnityEngine.Input.GetKeyDown(KeyCode.Alpha1))
            {
                return XREditorToolMode.Select;
            }

            if (global::UnityEngine.Input.GetKeyDown(KeyCode.Alpha2))
            {
                return XREditorToolMode.Move;
            }

            if (global::UnityEngine.Input.GetKeyDown(KeyCode.Alpha3))
            {
                return XREditorToolMode.Rotate;
            }

            if (global::UnityEngine.Input.GetKeyDown(KeyCode.Alpha4))
            {
                return XREditorToolMode.Scale;
            }

            return null;
        }

        public Ray CreatePointerRay(Camera editorCamera, Transform pointerOrigin)
        {
            if (pointerOrigin != null)
            {
                return new Ray(pointerOrigin.position, pointerOrigin.forward);
            }

            if (editorCamera != null)
            {
                return editorCamera.ScreenPointToRay(global::UnityEngine.Input.mousePosition);
            }

            return new Ray(Vector3.zero, Vector3.forward);
        }
    }
}