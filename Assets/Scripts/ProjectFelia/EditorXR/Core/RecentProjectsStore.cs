using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ProjectFelia.EditorXR.Core
{
    [Serializable]
    public sealed class RecentProjectsStore
    {
        [Serializable]
        private sealed class RecentProjectList
        {
            public List<string> projects = new List<string>();
        }

        private const int MaxProjects = 10;

        private static string StorePath => Path.Combine(Application.persistentDataPath, "ProjectFeliaRecentProjects.json");

        public static IReadOnlyList<string> Load()
        {
            if (!File.Exists(StorePath))
            {
                return Array.Empty<string>();
            }

            try
            {
                var json = File.ReadAllText(StorePath);
                var list = JsonUtility.FromJson<RecentProjectList>(json);
                return list?.projects ?? new List<string>();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public static void Add(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
            {
                return;
            }

            var projects = new List<string>(Load());
            projects.RemoveAll(path => string.Equals(path, projectPath, StringComparison.OrdinalIgnoreCase));
            projects.Insert(0, projectPath);

            if (projects.Count > MaxProjects)
            {
                projects.RemoveRange(MaxProjects, projects.Count - MaxProjects);
            }

            Save(projects);
        }

        public static string GetMostRecent()
        {
            var projects = Load();
            return projects.Count > 0 ? projects[0] : null;
        }

        private static void Save(IReadOnlyList<string> projects)
        {
            var payload = new RecentProjectList { projects = new List<string>(projects) };
            var json = JsonUtility.ToJson(payload, true);
            File.WriteAllText(StorePath, json);
        }
    }
}