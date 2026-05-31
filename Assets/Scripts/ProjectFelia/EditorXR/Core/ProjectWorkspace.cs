using System;
using System.IO;

namespace ProjectFelia.EditorXR.Core
{
    [Serializable]
    public sealed class ProjectWorkspace
    {
        public string ProjectPath { get; private set; }

        public string ProjectName { get; private set; }

        public ProjectManifest Manifest { get; private set; }

        public static ProjectWorkspace Open(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new ArgumentException("Project path cannot be empty.", nameof(projectPath));
            }

            var normalizedPath = Path.GetFullPath(projectPath);
            var workspace = new ProjectWorkspace
            {
                ProjectPath = normalizedPath,
                ProjectName = new DirectoryInfo(normalizedPath).Name,
                Manifest = ProjectManifestStore.Load(normalizedPath) ?? new ProjectManifest
                {
                    projectName = new DirectoryInfo(normalizedPath).Name
                }
            };

            return workspace;
        }
    }
}