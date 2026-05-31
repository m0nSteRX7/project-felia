using System;
using System.IO;
using UnityEngine;

namespace ProjectFelia.EditorXR.Core
{
    public static class ProjectManifestStore
    {
        public const string ManifestFileName = "ProjectFelia.project.json";

        public static ProjectManifest Load(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                return null;
            }

            var manifestPath = Path.Combine(projectPath, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            try
            {
                var json = File.ReadAllText(manifestPath);
                return JsonUtility.FromJson<ProjectManifest>(json);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void Save(string projectPath, ProjectManifest manifest)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                throw new ArgumentException("Project path cannot be empty.", nameof(projectPath));
            }

            var manifestPath = Path.Combine(projectPath, ManifestFileName);
            var json = JsonUtility.ToJson(manifest, true);
            File.WriteAllText(manifestPath, json);
        }
    }
}