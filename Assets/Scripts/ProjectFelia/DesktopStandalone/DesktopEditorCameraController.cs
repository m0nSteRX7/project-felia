using UnityEngine;

namespace ProjectFelia.DesktopStandalone
{
    public sealed class DesktopEditorCameraController : MonoBehaviour
    {
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private Transform focusTarget;

        [SerializeField, Min(0.1f)]
        private float orbitSpeed = 180f;

        [SerializeField, Min(0.1f)]
        private float panSpeed = 0.01f;

        [SerializeField, Min(0.1f)]
        private float zoomSpeed = 10f;

        [SerializeField, Min(0.1f)]
        private float minDistance = 1.5f;

        [SerializeField, Min(1f)]
        private float maxDistance = 100f;

        [SerializeField, Min(0.1f)]
        private float startDistance = 8f;

        private float yaw;

        private float pitch = 20f;

        private float distance;

        private Vector3 pivot;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            pivot = focusTarget != null ? focusTarget.position : Vector3.zero;

            var euler = transform.rotation.eulerAngles;
            yaw = euler.y;
            pitch = euler.x;
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                return;
            }

            if (focusTarget != null)
            {
                pivot = focusTarget.position;
            }

            HandleOrbit();
            HandlePan();
            HandleZoom();
            UpdateTransform();
        }

        public void SetFocus(Transform newFocusTarget)
        {
            focusTarget = newFocusTarget;
            if (focusTarget != null)
            {
                pivot = focusTarget.position;
            }
        }

        public void FrameBounds(Bounds bounds)
        {
            focusTarget = null;
            pivot = bounds.center;
            distance = Mathf.Clamp(bounds.extents.magnitude * 2f, minDistance, maxDistance);
        }

        public void ResetView()
        {
            focusTarget = null;
            pivot = Vector3.zero;
            distance = Mathf.Clamp(startDistance, minDistance, maxDistance);
            yaw = 0f;
            pitch = 20f;
            UpdateTransform();
        }

        private void HandleOrbit()
        {
            if (IsGuiCapturingInput())
            {
                return;
            }

            if (!Input.GetMouseButton(1))
            {
                return;
            }

            var mouseX = Input.GetAxis("Mouse X");
            var mouseY = Input.GetAxis("Mouse Y");

            yaw += mouseX * orbitSpeed * Time.deltaTime;
            pitch -= mouseY * orbitSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
        }

        private void HandlePan()
        {
            if (IsGuiCapturingInput())
            {
                return;
            }

            if (!Input.GetMouseButton(2))
            {
                return;
            }

            var mouseX = Input.GetAxis("Mouse X");
            var mouseY = Input.GetAxis("Mouse Y");

            var move = (-transform.right * mouseX + -transform.up * mouseY) * (distance * panSpeed);
            pivot += move;
        }

        private void HandleZoom()
        {
            if (IsGuiCapturingInput())
            {
                return;
            }

            var scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f))
            {
                return;
            }

            distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
        }

        private void UpdateTransform()
        {
            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var offset = rotation * new Vector3(0f, 0f, -distance);

            targetCamera.transform.SetPositionAndRotation(pivot + offset, rotation);
        }

        private static bool IsGuiCapturingInput()
        {
            return GUIUtility.hotControl != 0 || GUIUtility.keyboardControl != 0;
        }
    }
}