using System;
using UnityEngine;

namespace ProjectFelia.EditorXR.Core
{
    public enum XREditorToolMode
    {
        Select,
        Move,
        Rotate,
        Scale
    }

    public sealed class XREditorState
    {
        public XREditorToolMode ToolMode { get; private set; } = XREditorToolMode.Select;

        public Transform SelectedTarget { get; private set; }

        public event Action<Transform> SelectionChanged;

        public event Action<XREditorToolMode> ToolModeChanged;

        public void SetToolMode(XREditorToolMode toolMode)
        {
            if (ToolMode == toolMode)
            {
                return;
            }

            ToolMode = toolMode;
            ToolModeChanged?.Invoke(ToolMode);
        }

        public void SetSelection(Transform target)
        {
            if (SelectedTarget == target)
            {
                return;
            }

            SelectedTarget = target;
            SelectionChanged?.Invoke(SelectedTarget);
        }

        public void ClearSelection()
        {
            SetSelection(null);
        }
    }
}