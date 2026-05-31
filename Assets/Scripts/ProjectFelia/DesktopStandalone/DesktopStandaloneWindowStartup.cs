using UnityEngine;

namespace ProjectFelia.DesktopStandalone
{
    public static class DesktopStandaloneWindowStartup
    {
        private const int DefaultWidth = 1600;

        private const int DefaultHeight = 900;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyWindowedStartup()
        {
            DesktopAppSettings.Load();
            Screen.fullScreenMode = FullScreenMode.Windowed;
            Screen.SetResolution(DefaultWidth, DefaultHeight, FullScreenMode.Windowed);
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            Application.runInBackground = !DesktopAppSettings.PauseOnFocusLoss;
#else
            Application.runInBackground = true;
#endif
        }
    }
}