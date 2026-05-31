using System;

namespace ProjectFelia.EditorXR.Core
{
    [Serializable]
    public sealed class ProjectManifest
    {
        public string projectName = "Unnamed Project";

        public string projectVersion = "0.1.0";

        public string description = string.Empty;

        public string initialScene = string.Empty;
    }
}