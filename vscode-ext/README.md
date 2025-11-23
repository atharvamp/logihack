# LogiHack VSCode Extension

### "Feel" your build status
This extension integrates Visual Studio Code with the Logitech MX Master 4 mouse. It triggers the mouse's built-in haptic engine (Smart Shift sensor) to pulse when long-running tasks finish, allowing you to multitask without staring at the terminal.

### Features
Triggers differeing haptic pulses for build successes & failures
Automatically detects: 
- `task.json` task completions (npm, dotnet, make, etc.)
- Integrated Terminal commands

### Requirements 
This extension is Part 1 of 2!! It does not communicate directly with the MX Masters 4 drivers!
1. **Hardware**: Logitech MX Masters 4 Mouse
2. **Software**: Logi Options+ installed and device connected.
3. **Recieve**: This is the Part 2. You must have the Haptic Receiver (Localhost Listener) running in the background to bridge the HTTP signal to the mouse. This is a custom logi actions SDK plugin.

### Configuration
By default, the extension targets the local reciever on port 6500.