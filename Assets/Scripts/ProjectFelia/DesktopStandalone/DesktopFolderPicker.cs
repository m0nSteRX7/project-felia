using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ProjectFelia.DesktopStandalone
{
    public static class DesktopFolderPicker
    {
        private static Task<string> pendingMacFolderTask;

        public static bool TryPickFolder(out string folderPath)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return TryPickFolderWindows(out folderPath);
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            return TryPickFolderMac(out folderPath);
#else
            folderPath = string.Empty;
            return false;
#endif
        }

        public static bool TryBeginPickFolderMac()
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (pendingMacFolderTask != null && !pendingMacFolderTask.IsCompleted)
            {
                return false;
            }

            pendingMacFolderTask = Task.Run(() => TryPickFolderMacBlocking());
            return true;
#else
            return false;
#endif
        }

        public static bool TryConsumePickedFolderMac(out string folderPath)
        {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            if (pendingMacFolderTask == null || !pendingMacFolderTask.IsCompleted)
            {
                folderPath = string.Empty;
                return false;
            }

            folderPath = pendingMacFolderTask.Result;
            pendingMacFolderTask = null;
            return !string.IsNullOrWhiteSpace(folderPath);
#else
            folderPath = string.Empty;
            return false;
#endif
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        private static bool TryPickFolderWindows(out string folderPath)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select a Project Felia project folder";
                var result = dialog.ShowDialog();
                folderPath = result == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : string.Empty;
                return result == System.Windows.Forms.DialogResult.OK;
            }
        }
#endif

#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        private static bool TryPickFolderMac(out string folderPath)
        {
            folderPath = TryPickFolderMacBlocking();
            return !string.IsNullOrWhiteSpace(folderPath);
        }

        private static string TryPickFolderMacBlocking()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/usr/bin/osascript",
                    Arguments = "-e 'POSIX path of (choose folder with prompt \"Select a Project Felia project folder\")'",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return string.Empty;
                    }

                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    return process.ExitCode == 0 ? output : string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
#endif
    }
}