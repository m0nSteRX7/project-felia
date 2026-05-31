using UnityEngine;

namespace ProjectFelia.DesktopStandalone
{
    public static class DesktopGuiUtility
    {
        public static float GetAdaptiveScale(float requestedScale, float referenceWidth = 1600f, float referenceHeight = 900f)
        {
            if (requestedScale > 0.01f)
            {
                return requestedScale;
            }

            var widthScale = Screen.width / referenceWidth;
            var heightScale = Screen.height / referenceHeight;
            return Mathf.Clamp(Mathf.Min(widthScale, heightScale), 0.75f, 1.35f);
        }

        public static Matrix4x4 PushScale(float scale)
        {
            var previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            return previousMatrix;
        }

        public static void PopScale(Matrix4x4 previousMatrix)
        {
            GUI.matrix = previousMatrix;
        }
    }
}