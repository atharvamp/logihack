using Photino.NET;
using System;
using System.IO;

namespace LogiUiSidecar
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // 1. Check for the HTML file argument
            if (args.Length == 0) return;
            string htmlPath = args[0];

            if (!File.Exists(htmlPath)) return;

            // 2. Read the HTML content created by the Plugin
            string htmlContent = File.ReadAllText(htmlPath);

            // 3. Create the Photino Window
            var window = new PhotinoWindow()
                .SetTitle("Logi AI Assistant")
                .SetUseOsDefaultSize(false)
                .SetSize(600, 600)
                .Center()
                .SetResizable(true)
                .LoadRawString(htmlContent); // Inject the HTML

            // 4. Block here until user closes the window
            window.WaitForClose();

            // 5. Cleanup: Delete the temp file
            try { File.Delete(htmlPath); } catch { }
        }
    }
}