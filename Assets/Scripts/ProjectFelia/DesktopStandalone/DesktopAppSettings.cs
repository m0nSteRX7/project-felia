using UnityEngine;

namespace ProjectFelia.DesktopStandalone
{
    public static class DesktopAppSettings
    {
        private const string MenuScaleKey = "ProjectFelia.MenuScale";

        private const string AdaptiveScaleKey = "ProjectFelia.AdaptiveScale";

        private const string AutoRestoreKey = "ProjectFelia.AutoRestoreLastProject";

        private const string PauseOnFocusLossKey = "ProjectFelia.PauseOnFocusLoss";

        private static bool isLoaded;

        public static float MenuScale { get; set; } = 0f;

        public static bool AdaptiveScale { get; set; } = true;

        public static bool AutoRestoreLastProject { get; set; } = false;

        // When true, always show the launcher UI on startup and ignore command-line project args.
        // Default to false so command-line project paths can be used in automated tests and headless runs.
        public static bool ForceShowLauncher { get; set; } = false;

        public static bool PauseOnFocusLoss { get; set; } = true;

        public static void Load()
        {
            if (isLoaded)
            {
                return;
            }

            MenuScale = PlayerPrefs.GetFloat(MenuScaleKey, 0f);
            AdaptiveScale = PlayerPrefs.GetInt(AdaptiveScaleKey, 1) == 1;
            AutoRestoreLastProject = PlayerPrefs.GetInt(AutoRestoreKey, 0) == 1;
            PauseOnFocusLoss = PlayerPrefs.GetInt(PauseOnFocusLossKey, 1) == 1;
            isLoaded = true;
        }

        public static void Save()
        {
            PlayerPrefs.SetFloat(MenuScaleKey, MenuScale);
            PlayerPrefs.SetInt(AdaptiveScaleKey, AdaptiveScale ? 1 : 0);
            PlayerPrefs.SetInt(AutoRestoreKey, AutoRestoreLastProject ? 1 : 0);
            PlayerPrefs.SetInt(PauseOnFocusLossKey, PauseOnFocusLoss ? 1 : 0);
            PlayerPrefs.Save();
            isLoaded = true;
        }

        public static void SetMenuScale(float menuScale)
        {
            MenuScale = Mathf.Clamp(menuScale, 0f, 2f);
            AdaptiveScale = MenuScale <= 0.01f;
        }

        public static void ResetToDefaults()
        {
            MenuScale = 0f;
            AdaptiveScale = true;
            AutoRestoreLastProject = false;
            PauseOnFocusLoss = true;
            Save();
        }
    }
}