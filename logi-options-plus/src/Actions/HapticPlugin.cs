namespace Loupedeck.LogiHackPlugin
{
    using System;
    using System.Net;
    using System.Text;

    public class HapticPlugin : PluginDynamicCommand
    {
        private HttpListener listener;

        public HapticPlugin()
            : base("Button Press", "Invokes the haptic event on button press", "Haptics")
        {
        }

        protected override Boolean OnLoad()
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add("http://localhost:6500/");
            this.listener.Start();
            this.listener.BeginGetContext(this.OnRequest, null);

            // Register the event so the UI can update
        
            return true;


        }

        private void OnRequest(IAsyncResult result)
        {
            // 1. Get Context and keep listening
            var context = this.listener.EndGetContext(result);
            this.listener.BeginGetContext(this.OnRequest, null);

            var request = context.Request;
            var response = context.Response;
            var responseString = "OK";

            // 2. CHECK: Is it the right request?
            if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/build/failure")
            {
                PluginLog.Info("🔥 HTTP TRIGGER: Build Failed! 🔥");
                
                this.RunCommand("failure");

                responseString = "Failure Haptic Event Invoked";
            } else if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/build/success")
            {
                PluginLog.Info("🥳 HTTP TRIGGER: Build Succeeded! 🥳");
                
                this.RunCommand("success");

                responseString = "Success Haptic Event Invoked";
            }

            // 4. Respond to Curl
            var buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }

        // This ONLY runs if you physically press the button on the device
        protected override void RunCommand(String actionParameter)
        {
            // PluginLog.Info("👆" + actionParameter);
            PluginLog.Info("👆 BUTTON PRESS: Physical button pushed.");
        
            if (actionParameter == "failure")
            {
                this.Plugin.PluginEvents.RaiseEvent("buttonPressF");
            } else if(actionParameter == "success")
            {
                this.Plugin.PluginEvents.RaiseEvent("buttonPressS");
            }
            // this.Plugin.PluginEvents.RaiseEvent("buttonPress");
        
            PluginLog.Info("Raised event");
        }
    }
}