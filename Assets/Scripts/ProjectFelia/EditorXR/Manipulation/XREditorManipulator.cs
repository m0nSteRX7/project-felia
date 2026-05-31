using UnityEngine;
using ProjectFelia.EditorXR.Core;

namespace ProjectFelia.EditorXR.Manipulation
{
    public sealed class XREditorManipulator
    {
        private readonly XREditorState state;

        public XREditorManipulator(XREditorState state)
        {
            this.state = state;
        }

        public float MoveSpeed { get; set; } = 0.5f;

        public float RotateSpeed { get; set; } = 45f;

        public float ScaleSpeed { get; set; } = 0.5f;

        public void ApplyKeyboardShortcutInput(float deltaTime)
        {
            var target = state.SelectedTarget;
            if (target == null)
            {
                return;
            }

            switch (state.ToolMode)
            {
                case XREditorToolMode.Move:
                    ApplyMove(target, deltaTime);
                    break;
                case XREditorToolMode.Rotate:
                    ApplyRotate(target, deltaTime);
                    break;
                case XREditorToolMode.Scale:
                    ApplyScale(target, deltaTime);
                    break;
            }
        }

        private void ApplyMove(Transform target, float deltaTime)
        {
            var horizontal = global::UnityEngine.Input.GetAxisRaw("Horizontal");
            var vertical = global::UnityEngine.Input.GetAxisRaw("Vertical");
            var height = 0f;

            if (global::UnityEngine.Input.GetKey(KeyCode.PageUp))
            {
                height += 1f;
            }

            if (global::UnityEngine.Input.GetKey(KeyCode.PageDown))
            {
                height -= 1f;
            }

            var movement = new Vector3(horizontal, height, vertical) * MoveSpeed * deltaTime;
            target.Translate(movement, Space.Self);
        }

        private void ApplyRotate(Transform target, float deltaTime)
        {
            var yaw = 0f;

            if (global::UnityEngine.Input.GetKey(KeyCode.A))
            {
                yaw -= RotateSpeed * deltaTime;
            }

            if (global::UnityEngine.Input.GetKey(KeyCode.D))
            {
                yaw += RotateSpeed * deltaTime;
            }

            if (yaw != 0f)
            {
                target.Rotate(Vector3.up, yaw, Space.World);
            }
        }

        private void ApplyScale(Transform target, float deltaTime)
        {
            var scaleDelta = 0f;

            if (global::UnityEngine.Input.GetKey(KeyCode.Equals) || global::UnityEngine.Input.GetKey(KeyCode.KeypadPlus))
            {
                scaleDelta += ScaleSpeed * deltaTime;
            }

            if (global::UnityEngine.Input.GetKey(KeyCode.Minus) || global::UnityEngine.Input.GetKey(KeyCode.KeypadMinus))
            {
                scaleDelta -= ScaleSpeed * deltaTime;
            }

            if (scaleDelta == 0f)
            {
                return;
            }

            var scaleFactor = Mathf.Clamp(1f + scaleDelta, 0.1f, 10f);
            target.localScale *= scaleFactor;
        }
    }
}