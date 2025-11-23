namespace Loupedeck.LogiHackPlugin
{
    using System;

    // This class implements an example command that counts button presses.
    using System.Diagnostics;
    using System.Reflection;


    // Add Gemini Namespaces
    using Google.GenAI;
    using Google.GenAI.Types;
    public class ClipboardPlugin : PluginDynamicCommand
    {
        // Initializes the command class.
        public ClipboardPlugin()
            : base(displayName: "Clipboard pluing", description: "Send clipboard for AI analysis", groupName: "Commands")
        {
            this.MakeProfileAction("text;Mode (summary, mockup)");
        }

        // // This method is called when the user executes the command.
        // protected override void RunCommand(String actionParameter)
        // {
        //     // this.ActionImageChanged(); // Notify the plugin service that the command display name and/or image has changed.
        //     var clipboardContent = GetClipboardImageAsBase64();
        //     PluginLog.Info("Clipboard Content: " + clipboardContent);
        // }

        // This method is called when Loupedeck needs to show the command on the console or the UI.
        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            "Clipboard Plugin 🤯🤯🤯";



        public static String GetClipboardImageAsBase64()
        {
            var tempFileName = "logi_clipboard_temp.png";
            var tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

            // We construct the script exactly as before
            // Note: We use Replace to ensure paths with spaces don't break the AppleScript
            var cleanPath = tempFilePath.Replace("\"", "\\\"");

            var appleScript = $@"
try
    set theFile to (POSIX file ""{cleanPath}"")
    set theData to the clipboard as «class PNGf»
    set theRef to open for access theFile with write permission
    set eof theRef to 0
    write theData to theRef
    close access theRef
on error
    return ""ERROR""
end try";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = "-", // <--- The dash tells osascript to read from Stdin
                    RedirectStandardInput = true, // <--- Enable the pipe
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    // Write the script to the process stream securely
                    using (StreamWriter writer = process.StandardInput)
                    {
                        writer.Write(appleScript);
                    } // Closing this block automatically closes the stream, telling osascript "Finished"

                    var output = process.StandardOutput.ReadToEnd();
                    var err = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    // Debugging logs (optional)
                    if (!String.IsNullOrEmpty(err))
                        PluginLog.Info($"OSASCRIPT STDERR: {err}");

                    // Check results
                    if (File.Exists(tempFilePath))
                    {
                        var imageBytes = File.ReadAllBytes(tempFilePath);
                        var base64 = Convert.ToBase64String(imageBytes);

                        // Cleanup
                        File.Delete(tempFilePath);

                        return base64;
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Info($"C# EXCEPTION: {ex.Message}");
            }

            return String.Empty;
        }

        public string GenerateHtmlInterface(string base64Image)
        {
            const string apiKey = PluginUtility.API_KEY;
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <script src=""https://cdn.tailwindcss.com""></script>
    <script src=""https://cdnjs.cloudflare.com/ajax/libs/marked/9.1.2/marked.min.js""></script>
    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css"">
    <style>
        body {{ background-color: #121212; color: #e0e0e0; font-family: 'Segoe UI', sans-serif; }}
        
        /* Scrollbar */
        ::-webkit-scrollbar {{ width: 8px; }}
        ::-webkit-scrollbar-track {{ background: transparent; }}
        ::-webkit-scrollbar-thumb {{ background: #333; border-radius: 4px; }}
        ::-webkit-scrollbar-thumb:hover {{ background: #555; }}

        /* Chat Bubbles */
        .msg-user {{ background-color: #2a2a2a; color: #fff; align-self: flex-end; border-bottom-right-radius: 2px; }}
        .msg-ai {{ background-color: #1e3a8a; color: #e0e7ff; align-self: flex-start; border-bottom-left-radius: 2px; }}
        
        /* Markdown Styles */
        .prose p {{ margin-bottom: 0.5em; }}
        .prose pre {{ background: #111; padding: 8px; border-radius: 4px; overflow-x: auto; margin-top: 5px; border: 1px solid #333; }}
        .prose code {{ font-family: monospace; background: rgba(0,0,0,0.3); padding: 2px 4px; border-radius: 3px; color: #ff79c6; }}
        .prose ul {{ list-style-type: disc; padding-left: 20px; }}
    </style>
</head>
<body class=""h-screen flex flex-col overflow-hidden"">
    
    <div class=""h-10 px-4 bg-[#1a1a1a] border-b border-[#333] flex justify-between items-center select-none shrink-0"">
        <div class=""flex items-center gap-2"">
            <i class=""fa-solid fa-bolt text-yellow-500 text-xs""></i>
            <span class=""font-bold text-xs tracking-wider text-gray-400"">GEMINI 2.5 FLASH</span>
        </div>
        <div class=""flex gap-2"">
            <button id=""browserBtn"" onclick=""openInRealBrowser()"" class=""text-[10px] bg-[#333] hover:bg-blue-600 text-white px-2 py-1 rounded transition flex items-center gap-2"">
                <i class=""fa-solid fa-arrow-up-right-from-square""></i> <span>Browser View</span>
            </button>
            <button onclick=""window.close()"" class=""text-gray-500 hover:text-white px-2""><i class=""fa-solid fa-xmark""></i></button>
        </div>
    </div>

    <div class=""flex-1 flex flex-col overflow-hidden relative"">
        <div id=""chatContainer"" class=""flex-1 overflow-y-auto p-4 flex flex-col gap-4 pb-40"">
            
            <div class=""flex flex-col gap-1 max-w-[80%] bg-[#1a1a1a] p-2 rounded-lg border border-[#333]"">
                <span class=""text-[10px] text-gray-500 font-bold uppercase"">Analyzed Image</span>
                <img src=""data:image/png;base64,{base64Image}"" class=""rounded max-h-40 object-contain bg-black"" />
            </div>

            <div id=""loading-indicator"" class=""hidden self-start bg-[#1e3a8a] text-blue-100 px-4 py-2 rounded-full text-xs flex items-center gap-2"">
                <i class=""fa-solid fa-circle-notch fa-spin""></i> Processing...
            </div>
        </div>

        <div class=""absolute bottom-0 w-full bg-[#121212] border-t border-[#333] p-3 flex flex-col gap-2 shadow-[0_-5px_15px_rgba(0,0,0,0.5)]"">
            
            <div class=""flex gap-2 overflow-x-auto pb-1 scrollbar-hide"">
                <button onclick=""runQuickAction('summary')"" class=""shrink-0 px-3 py-1 rounded-full bg-[#222] hover:bg-[#333] border border-[#333] text-[10px] text-gray-300 transition"">Summary</button>
                <button onclick=""runQuickAction('mockup')"" class=""shrink-0 px-3 py-1 rounded-full bg-[#222] hover:bg-[#333] border border-[#333] text-[10px] text-gray-300 transition"">HTML Mockup</button>
                <button onclick=""runQuickAction('explain')"" class=""shrink-0 px-3 py-1 rounded-full bg-[#222] hover:bg-[#333] border border-[#333] text-[10px] text-gray-300 transition"">Explain</button>
            </div>

            <div class=""flex items-end gap-2 bg-[#1e1e1e] border border-[#333] rounded-xl p-2 focus-within:border-blue-500 transition-colors"">
                <textarea id=""userPrompt"" placeholder=""Ask a follow-up..."" 
                    class=""flex-1 bg-transparent text-gray-200 text-sm focus:outline-none resize-none h-10 max-h-32 py-2 px-1""
                    onkeydown=""handleEnter(event)""></textarea>
                
                <button onclick=""sendUserMessage()"" class=""h-8 w-8 shrink-0 bg-blue-600 hover:bg-blue-500 text-white rounded-lg flex items-center justify-center transition mb-1"">
                    <i class=""fa-solid fa-paper-plane text-xs""></i>
                </button>
            </div>
        </div>
    </div>

    <script>
        const API_KEY = '{apiKey}';
        const BASE64_IMAGE = '{base64Image}';
        const chatContainer = document.getElementById('chatContainer');
        const loader = document.getElementById('loading-indicator');
        const inputField = document.getElementById('userPrompt');
        let msgHistory = []; 

        window.onload = () => {{
            const hash = window.location.hash;
            // 1. Restore History if opening in browser
            if (hash.startsWith('#restore=')) {{
                try {{
                    const json = decodeURIComponent(hash.substring(9));
                    const savedMsgs = JSON.parse(json);
                    savedMsgs.forEach(msg => appendBubble(msg.text, msg.role, false));
                }} catch (e) {{ console.error('Restore failed'); }}
            }} else {{
                // 2. Default Start
                runQuickAction('summary');
            }}
        }};

        function handleEnter(e) {{
            if (e.key === 'Enter' && !e.shiftKey) {{
                e.preventDefault();
                sendUserMessage();
            }}
        }}

        function runQuickAction(mode) {{
            const prompts = {{
                summary: 'Summarize this image concisely.',
                mockup: 'Create a single-file HTML/Tailwind mockup of this.',
                explain: 'Explain the technical logic here.'
            }};
            processMessage(prompts[mode], true);
        }}

        function sendUserMessage() {{
            const text = inputField.value.trim();
            if (!text) return;
            inputField.value = '';
            // Reset height
            inputField.style.height = '40px'; 
            processMessage(text, false);
        }}

        async function processMessage(text, isSystemAction) {{
            if (!isSystemAction) appendBubble(text, 'user');

            loader.classList.remove('hidden');
            chatContainer.appendChild(loader);
            scrollToBottom();

            try {{
                const responseText = await callGemini(text);
                loader.classList.add('hidden');
                appendBubble(responseText, 'ai');
            }} catch (err) {{
                loader.classList.add('hidden');
                appendBubble('Error: ' + err.message, 'ai');
            }}
        }}

        function appendBubble(text, role, save = true) {{
            if(save) msgHistory.push({{ role, text }});

            const div = document.createElement('div');
            div.className = role === 'user' 
                ? 'msg-user max-w-[80%] p-3 rounded-2xl rounded-br-none text-xs' 
                : 'msg-ai max-w-[95%] p-3 rounded-2xl rounded-bl-none text-xs prose prose-invert';
            
            if (role === 'ai') div.innerHTML = marked.parse(text);
            else div.innerText = text;

            chatContainer.insertBefore(div, loader);
            scrollToBottom();
        }}

        function scrollToBottom() {{
            chatContainer.scrollTop = chatContainer.scrollHeight;
        }}

        async function callGemini(prompt) {{
            // Using gemini-1.5-flash-latest
            const url = `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${{API_KEY}}`;
            
            const payload = {{
                contents: [{{
                    parts: [
                        {{ text: prompt }},
                        {{ inline_data: {{ mime_type: 'image/png', data: BASE64_IMAGE }} }}
                    ]
                }}]
            }};

            const response = await fetch(url, {{
                method: 'POST',
                headers: {{ 'Content-Type': 'application/json' }},
                body: JSON.stringify(payload)
            }});

            const data = await response.json();
            if (data.error) throw new Error(data.error.message);
            return data.candidates[0].content.parts[0].text;
        }}

        // --- ROBUST ""OPEN IN BROWSER"" ---
        function openInRealBrowser() {{
            const jsonHistory = JSON.stringify(msgHistory);
            const targetUrl = window.location.href.split('#')[0] + '#restore=' + encodeURIComponent(jsonHistory);
            
            // Try standard window open
            const newWin = window.open(targetUrl, '_system');
            
            // If that failed (likely inside Sidecar WebView), fallback to Clipboard
            if(!newWin) {{
                navigator.clipboard.writeText(targetUrl);
                
                const btn = document.getElementById('browserBtn');
                const originalHtml = btn.innerHTML;
                
                btn.style.backgroundColor = '#16a34a'; // Green
                btn.innerHTML = '<i class=""fa-solid fa-check""></i> <span>Link Copied!</span>';
                
                // Add a small tooltip instruction
                const tip = document.createElement('div');
                tip.className = 'fixed top-12 right-4 bg-black text-white text-[10px] p-2 rounded shadow-lg z-50';
                tip.innerText = 'Paste the link into Chrome/Safari to continue.';
                document.body.appendChild(tip);
                
                setTimeout(() => {{
                    btn.innerHTML = originalHtml;
                    btn.style.backgroundColor = '#333';
                    tip.remove();
                }}, 3000);
            }}
        }}

        // Auto-expand textarea
        inputField.addEventListener('input', function() {{
            this.style.height = 'auto';
            this.style.height = (this.scrollHeight) + 'px';
        }});
    </script>
</body>
</html>";
        }
        protected override void RunCommand(String actionParameter)
        {
            // Run in background task to keep Loupedeck responsive
            Task.Run(() =>
            {
                try
                {
                    PluginLog.Info("LOGI_PLUGIN: Starting Clipboard Action...");

                    // 1. Get the Screenshot from Clipboard
                    var base64 = GetClipboardImageAsBase64();

                    if (String.IsNullOrEmpty(base64))
                    {
                        PluginLog.Warning("LOGI_PLUGIN: No image found in clipboard.");
                        // We continue anyway so the user sees the UI, just without an image
                        base64 = "";
                    }
                    else
                    {
                        PluginLog.Info($"LOGI_PLUGIN: Image captured! Length: {base64.Length}");
                    }

                    // 2. Generate the HTML UI (Injecting the image)
                    string uiHtml = GenerateHtmlInterface(base64);

                    // 3. Launch the Window
                    LaunchSidecar(uiHtml);
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"LOGI_PLUGIN CRASH: {ex}");
                }
            });
        }

        private void LaunchSidecar(string htmlContent)
        {
            // A. Save HTML to a temp file
            string tempFile = Path.GetTempFileName() + ".html";
            File.WriteAllText(tempFile, htmlContent);

            // B. Find the Sidecar Executable
            // It MUST be in the same folder as this Plugin DLL
            string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            // string sidecarPath = Path.Combine(pluginDir, "LogiUiSidecar"); 
            string sidecarPath = @"/Users/atharva/projects/hackatum25/LogiHackPlugin/LogiUiSidecar/bin/Debug/net10.0/LogiUiSidecar";

            // C. Run it
            var startInfo = new ProcessStartInfo
            {
                FileName = sidecarPath,
                Arguments = $"\"{tempFile}\"", // Pass the file path safely
                UseShellExecute = false,
                CreateNoWindow = false
            };

            Process.Start(startInfo);
            PluginLog.Info("LOGI_PLUGIN: Launched Sidecar.");
        }

    }



}
