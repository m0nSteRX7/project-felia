namespace ProjectFelia.EditorXR.Core
{
    public static class ProjectWorkspaceService
    {
        public static ProjectWorkspace CurrentWorkspace { get; private set; }

        private static bool returnToMainMenuRequested;

        public static bool HasWorkspace => CurrentWorkspace != null;

        public static bool MainMenuRequested => returnToMainMenuRequested;

        public static void SetWorkspace(ProjectWorkspace workspace)
        {
            CurrentWorkspace = workspace;
        }

        public static void RequestMainMenu()
        {
            returnToMainMenuRequested = true;
        }

        public static bool ConsumeMainMenuRequest()
        {
            var requested = returnToMainMenuRequested;
            returnToMainMenuRequested = false;
            return requested;
        }
    }
}