using UnityEngine;
using ProjectFelia.EditorXR.Core;

namespace ProjectFelia.EditorXR.Selection
{
    public sealed class XREditorSelectionService
    {
        private readonly XREditorState state;

        public XREditorSelectionService(XREditorState state)
        {
            this.state = state;
        }

        public int SelectableLayerMask { get; set; } = ~0;

        public float MaxSelectionDistance { get; set; } = 100f;

        public bool TrySelect(Ray ray, out RaycastHit hit)
        {
            if (Physics.Raycast(ray, out hit, MaxSelectionDistance, SelectableLayerMask))
            {
                state.SetSelection(hit.transform);
                return true;
            }

            state.ClearSelection();
            return false;
        }
    }
}