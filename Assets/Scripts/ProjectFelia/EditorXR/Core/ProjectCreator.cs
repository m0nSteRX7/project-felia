using System;
using System.IO;

namespace ProjectFelia.EditorXR.Core
{
    public static class ProjectCreator
    {
        public static ProjectWorkspace Create(string parentDirectory, string projectName)
        {
            if (string.IsNullOrWhiteSpace(parentDirectory))
            {
                throw new ArgumentException("Parent directory cannot be empty.", nameof(parentDirectory));
            }

            if (string.IsNullOrWhiteSpace(projectName))
            {
                throw new ArgumentException("Project name cannot be empty.", nameof(projectName));
            }

            var sanitizedName = SanitizeName(projectName);
            var projectPath = Path.Combine(Path.GetFullPath(parentDirectory), sanitizedName);

            Directory.CreateDirectory(projectPath);
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "Scenes"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "Materials"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "Prefabs"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "Scripts"));
            Directory.CreateDirectory(Path.Combine(projectPath, "Assets", "Textures"));
            Directory.CreateDirectory(Path.Combine(projectPath, "ProjectSettings"));

            var manifest = new ProjectManifest
            {
                projectName = sanitizedName,
                projectVersion = "0.1.0",
                description = "New Project Felia workspace",
                initialScene = string.Empty
            };

            ProjectManifestStore.Save(projectPath, manifest);

            return ProjectWorkspace.Open(projectPath);
        }

        private static string SanitizeName(string projectName)
        {
            var invalidCharacters = Path.GetInvalidFileNameChars();
            var chars = projectName.Trim().ToCharArray();

            for (var index = 0; index < chars.Length; index++)
            {
                var character = chars[index];
                for (var invalidIndex = 0; invalidIndex < invalidCharacters.Length; invalidIndex++)
                {
                    if (character == invalidCharacters[invalidIndex])
                    {
                        chars[index] = '_';
                        break;
                    }
                }
            }

            var cleanedName = new string(chars).Trim();
            return string.IsNullOrWhiteSpace(cleanedName) ? "ProjectFeliaProject" : cleanedName;
        }
    }
}