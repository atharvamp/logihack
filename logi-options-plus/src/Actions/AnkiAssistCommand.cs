namespace Loupedeck.LogiHackPlugin
{
    using System;
    using System.Threading.Tasks;

    public class AnkiAssistCommand : PluginDynamicCommand
    {
        public AnkiAssistCommand()
            : base(displayName: "Logi Anki Assist", description: "Instant Flashcard Generator", groupName: "Study Tools")
        {
        }

        protected override void RunCommand(String actionParameter)
        {
            Task.Run(() =>
            {
                var b64 = PluginUtility.GetClipboardImageAsBase64();
                if (string.IsNullOrEmpty(b64)) return;

                string html = GenerateAnkiHtml(b64);
                PluginUtility.LaunchSidecar(html);
            });
        }

        private string GenerateAnkiHtml(string base64)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <script src='https://cdn.tailwindcss.com'></script>
    <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css'>
    <style>
        body {{ background: #121212; color: #eee; font-family: 'Segoe UI', sans-serif; }}
        ::-webkit-scrollbar {{ width: 8px; }}
        ::-webkit-scrollbar-thumb {{ background: #333; border-radius: 4px; }}
        .card {{ background: #1e1e1e; border: 1px solid #333; padding: 10px; border-radius: 6px; }}
        .field {{ background: #121212; border: 1px solid #333; width: 100%; padding: 5px; font-size: 12px; color: #ddd; font-family: sans-serif; }}
    </style>
</head>
<body class='h-screen flex flex-col p-4 gap-4 overflow-hidden'>
    
    <div class='flex justify-between items-center border-b border-[#333] pb-2 shrink-0'>
        <h1 class='font-bold text-sm text-green-400 flex items-center gap-2'>
            <i class='fa-solid fa-graduation-cap'></i> Anki Assist
        </h1>
        <div class='flex gap-2 items-center'>
            <span id='status' class='text-[10px] text-gray-500'>Checking Anki...</span>
            <button onclick='window.close()' class='text-gray-500 hover:text-white ml-2'><i class='fa-solid fa-xmark'></i></button>
        </div>
    </div>

    <div class='flex gap-4 h-full overflow-hidden'>
        <div class='w-1/3 flex flex-col gap-3 shrink-0'>
            <div class='h-48 bg-black border border-[#333] rounded flex items-center justify-center overflow-hidden shrink-0 relative'>
                <img src='data:image/png;base64,{base64}' class='w-full h-full object-contain opacity-90' />
                 <div class='absolute bottom-0 w-full bg-black/70 text-[10px] text-center text-gray-400 py-1'>Captured Context</div>
            </div>
            
            <div class='bg-[#1e1e1e] p-3 rounded border border-[#333] flex-1 flex flex-col'>
                <label class='text-[10px] text-gray-500 uppercase font-bold'>Target Deck</label>
                <input id='deckName' value='Default' class='w-full bg-[#121212] border border-[#333] text-xs p-2 rounded text-white mt-1 mb-4 focus:border-blue-500 outline-none' />
                
                <div class='flex-1'></div> 

                <button onclick='generate()' id='genBtn' class='w-full bg-blue-600 hover:bg-blue-500 text-white text-xs font-bold py-3 rounded shadow-lg flex items-center justify-center gap-2 transition'>
                    <i class='fa-solid fa-rotate'></i> Regenerate Cards
                </button>
            </div>
        </div>

        <div id='cards-container' class='flex-1 overflow-y-auto space-y-3 pr-1 pb-4'>
            </div>
    </div>

    <script>
        const API_KEY = '{PluginUtility.API_KEY}';
        const B64 = '{base64}';

        // --- SAFE STRING ESCAPING HELPER ---
        function safe(str) {{
            if (!str) return '';
            return str.replace(/""/g, '&quot;').replace(/'/g, '&#39;');
        }}

        window.onload = () => {{
            checkAnki();
            generate(); 
        }};

        async function generate() {{
            const con = document.getElementById('cards-container');
            const btn = document.getElementById('genBtn');
            
            btn.disabled = true; 
            btn.innerHTML = '<i class=""fa-solid fa-circle-notch fa-spin""></i> Analyzing...';
            con.innerHTML = '<div class=""flex flex-col items-center justify-center h-full text-gray-500 text-xs gap-2""><i class=""fa-solid fa-brain text-2xl""></i><span>Asking Gemini to create study materials...</span></div>';

            try {{
                const res = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${{API_KEY}}`, {{
                    method: 'POST', 
                    headers: {{'Content-Type':'application/json'}},
                    body: JSON.stringify({{ contents: [{{ parts: [ 
                        {{ text: 'Analyze this image deeply. Create 3 distinct Anki flashcards. Return a raw JSON array where each object has exactly two keys: ""front"" and ""back"" (lowercase). Do not use markdown blocks.' }}, 
                        {{ inline_data: {{ mime_type: 'image/png', data: B64 }} }} 
                    ] }}] }})
                }});
                
                const data = await res.json();

                if (data.error) throw new Error('Google API Error: ' + data.error.message);
                if (!data.candidates || data.candidates.length === 0) throw new Error('No content returned.');

                const rawText = data.candidates[0].content.parts[0].text;
                const json = rawText.replace(/```json/g,'').replace(/```/g,'').trim();
                
                render(JSON.parse(json));

            }} catch(e) {{ 
                con.innerHTML = `<div class=""bg-red-900/30 border border-red-800 p-4 rounded text-red-300 text-xs""><strong class=""block mb-1"">Analysis Failed:</strong>${{e.message}}</div>`; 
            }} finally {{ 
                btn.disabled = false; 
                btn.innerHTML = '<i class=""fa-solid fa-rotate""></i> Regenerate Cards'; 
            }}
        }}

        function render(cards) {{
            const con = document.getElementById('cards-container');
            con.innerHTML = `
                <div class=""flex justify-between items-center mb-2 pb-2 border-b border-[#333]"">
                    <span class=""text-[10px] text-gray-400"">Gemini suggested ${{cards.length}} cards:</span>
                    <button onclick=""addAll()"" class=""bg-blue-600 hover:bg-blue-500 text-white text-[10px] px-3 py-1 rounded font-bold transition""><i class=""fa-solid fa-check-double mr-1""></i> Add All & Close</button>
                </div>
            `;

            cards.forEach((c, i) => {{
                // ROBUST KEY CHECKING (Fixes ""undefined"" error)
                const frontText = c.front || c.Front || c.question || c.Question || 'Error: Missing Question';
                const backText = c.back || c.Back || c.answer || c.Answer || 'Error: Missing Answer';

                const div = document.createElement('div');
                div.className = 'card relative group transition-all duration-300';
                div.innerHTML = `
                    <div class='flex justify-between items-center mb-2'>
                        <span class='text-[10px] font-bold text-blue-400 uppercase bg-blue-900/30 px-2 py-0.5 rounded'>Card ${{i+1}}</span>
                         <div class=""flex gap-2"">
                            <button onclick=""discard(this)"" class=""text-gray-600 hover:text-red-400 transition""><i class=""fa-solid fa-trash""></i></button>
                            <button onclick='addToAnki(this)' class='btn-add bg-green-700 hover:bg-green-600 text-white text-[10px] px-3 py-1 rounded flex items-center gap-1 transition'><i class=""fa-solid fa-plus""></i> Add</button>
                        </div>
                    </div>
                    <input class='field mb-1 font-semibold text-blue-100' value=""${{safe(frontText)}}"" />
                    <textarea class='field h-16 leading-relaxed text-gray-300 resize-none'>${{backText}}</textarea>
                `;
                con.appendChild(div);
            }});
        }}

        function discard(btn) {{
             const card = btn.closest('.card');
             card.style.opacity = '0';
             setTimeout(() => card.remove(), 300);
        }}

        async function addToAnki(btn) {{
            const div = btn.closest('.card');
            const f = div.querySelector('input').value;
            const b = div.querySelector('textarea').value;
            const d = document.getElementById('deckName').value;

            const oldText = btn.innerHTML;
            btn.innerHTML = '<i class=""fa-solid fa-circle-notch fa-spin""></i>';

            try {{
                const res = await fetch('http://127.0.0.1:8765', {{ 
                    method: 'POST', 
                    body: JSON.stringify({{ 
                        action: 'addNote', version: 6, 
                        params: {{ note: {{ deckName: d, modelName: 'Basic', fields: {{ Front: f, Back: b }}, tags: ['logi-assist'] }} }} 
                    }}) 
                }});
                const json = await res.json();
                if(json.error) throw new Error(json.error);

                btn.innerHTML = '<i class=""fa-solid fa-check""></i> Saved';
                btn.className = 'text-[10px] text-green-500 border border-green-900 px-3 py-1 rounded cursor-default';
                btn.disabled = true;
                div.classList.add('opacity-60'); 
            }} catch(e) {{ 
                alert('Anki Error: ' + e.message); 
                btn.innerHTML = oldText;
            }}
        }}

        async function addAll() {{
            const btns = document.querySelectorAll('.btn-add');
            for(const b of btns) {{ 
                if(!b.disabled) {{
                     await b.click(); 
                     await new Promise(r=>setTimeout(r,150));
                }}
            }}
            setTimeout(() => window.close(), 800);
        }}

        async function checkAnki() {{
            try {{ 
                await fetch('http://127.0.0.1:8765', {{ method: 'POST', body: JSON.stringify({{ action: 'version', version: 6 }}) }}); 
                document.getElementById('status').innerText = 'Connected';
                document.getElementById('status').className = 'text-[10px] text-green-500';
            }} catch {{ 
                document.getElementById('status').innerText = 'Anki Disconnected';
                document.getElementById('status').className = 'text-[10px] text-red-500 font-bold';
            }}
        }}
    </script>
</body>
</html>";
        }
    }
}