namespace Loupedeck.LogiHackPlugin
{
    using System;
    using System.IO;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Linq;
    using Loupedeck.LogiHackPlugin.PluginUtiliy;


    public static class PluginUtility
    {
        public const string API_KEY = Secrets.GOOGLE_API_KEY;

        // --- 1. SCREENSHOT LOGIC ---
        public static string GetClipboardImageAsBase64()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "logi_temp.png");
            try
            {
                var script = $"try\nset f to (POSIX file \"{tempPath}\")\nset d to the clipboard as «class PNGf»\nset o to open for access f with write permission\nset eof o to 0\nwrite d to o\nclose access o\nend try";
                var psi = new ProcessStartInfo { FileName = "osascript", Arguments = "-", RedirectStandardInput = true, CreateNoWindow = true, UseShellExecute = false };
                using (var p = Process.Start(psi))
                {
                    using (var w = p.StandardInput)
                        w.Write(script);
                    p.WaitForExit();
                }
                if (File.Exists(tempPath))
                {
                    var b64 = Convert.ToBase64String(File.ReadAllBytes(tempPath));
                    File.Delete(tempPath);
                    SaveToHistory(b64); // Auto-save history
                    return b64;
                }
            }
            catch { }
            return "";
        }

        // --- 2. HISTORY LOGIC ---
        public static void SaveToHistory(string base64)
        {
            try
            {
                var folder = Path.Combine(Path.GetTempPath(), "LogiHistory");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                File.WriteAllText(Path.Combine(folder, $"{DateTime.Now.Ticks}.txt"), base64);
            }
            catch { }
        }

        public static List<string> GetRecentHistory(int count)
        {
            try
            {
                var folder = Path.Combine(Path.GetTempPath(), "LogiHistory");
                if (!Directory.Exists(folder))
                    return new List<string>();
                return new DirectoryInfo(folder).GetFiles("*.txt").OrderByDescending(f => f.Name).Take(count).Select(f => File.ReadAllText(f.FullName)).ToList();
            }
            catch { return new List<string>(); }
        }

        // --- 3. LAUNCHER LOGIC ---
        public static void LaunchSidecar(string htmlContent)
        {
            string tempFile = Path.GetTempFileName() + ".html";
            File.WriteAllText(tempFile, htmlContent);
            // TODO: VERIFY PATH
            string sidecarPath = @"/Users/atharva/projects/hackatum25/LogiHackPlugin/LogiUiSidecar/bin/Debug/net10.0/LogiUiSidecar";
            Process.Start(new ProcessStartInfo { FileName = sidecarPath, Arguments = $"\"{tempFile}\"", UseShellExecute = false });
        }
    }
}