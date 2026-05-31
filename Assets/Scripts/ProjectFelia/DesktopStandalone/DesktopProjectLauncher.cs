using System;
using System.IO;
using UnityEngine;
using ProjectFelia.EditorXR.Core;

namespace ProjectFelia.DesktopStandalone
{
    public static class DesktopProjectLauncher
    {
        private const string ProjectPathArgument = "-projectPath";

        private const string ProjectFileArgument = "-projectFile";

        public static ProjectWorkspace ResolveWorkspace()
        {
            // Honor runtime setting to always show the launcher even when a project path/file
            // is provided on the command line.
            DesktopAppSettings.Load();
            if (DesktopAppSettings.ForceShowLauncher)
            {
                return null;
            }

            var args = Environment.GetCommandLineArgs();
            var projectPath = TryGetArgumentValue(args, ProjectPathArgument);
            if (!string.IsNullOrWhiteSpace(projectPath))
            {
                var workspace = ProjectWorkspace.Open(projectPath);
                ProjectWorkspaceService.SetWorkspace(workspace);
                RecentProjectsStore.Add(workspace.ProjectPath);
                return workspace;
            }

            var projectFile = TryGetArgumentValue(args, ProjectFileArgument);
            if (!string.IsNullOrWhiteSpace(projectFile) && File.Exists(projectFile))
            {
                var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(projectFile));
                if (!string.IsNullOrWhiteSpace(projectDirectory))
                {
                    var workspace = ProjectWorkspace.Open(projectDirectory);
                    ProjectWorkspaceService.SetWorkspace(workspace);
                    RecentProjectsStore.Add(workspace.ProjectPath);
                    return workspace;
                }
            }

            return null;
        }

        private static string TryGetArgumentValue(string[] args, string argumentName)
        {
            for (var index = 0; index < args.Length - 1; index++)
            {
                if (!string.Equals(args[index], argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return args[index + 1];
            }

            return null;
        }
    }
}