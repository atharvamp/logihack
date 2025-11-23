"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = activate;
exports.deactivate = deactivate;
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
const vscode = __importStar(require("vscode"));
const http = __importStar(require("http"));
const LISTENER_HOST = 'localhost'; // localhost
const LISTENER_PORT = 6500;
// This method is called when your extension is activated
// Your extension is activated the very first time the command is executed
function activate(context) {
    // Use the console to output diagnostic information (console.log) and errors (console.error)
    // This line of code will only be executed once when your extension is activated
    console.log('Congratulations, your extension "logihack-vscode-ext" is now active!');
    // The command has been defined in the package.json file
    // Now provide the implementation of the command with registerCommand
    // The commandId parameter must match the command field in package.json
    // const disposable = vscode.commands.registerCommand('logihack-vscode-ext.helloWorld', () => {
    // The code you place here will be executed every time your command is executed
    // Display a message box to the user
    // 	vscode.window.showInformationMessage('Hello World from LogiHack!');
    // });
    // commands: 
    // - broadcast build result
    // listens for VSCode Tasks
    // potentially use context.subscriptions.push() for correct garbage collection
    const taskEndListener = vscode.tasks.onDidEndTaskProcess((event) => {
        // maybe filter to only react to specific tasks (e.g., tasks named "build")
        // if (!event.execution.task.name.toLowerCase().includes("build")) return;
        const exitCode = event.exitCode;
        console.log(`Task exit code generated: ${exitCode}`);
        exitCode == 0 ? sendBuildStatus('success') : sendBuildStatus('failure');
    });
    // Terminal Executions
    const terminalEndListener = vscode.window.onDidEndTerminalShellExecution((event => {
        // ignore basic terminal commands
        const ignoredCommands = ['cd', 'ls', 'la', 'll', 'clear', 'cls', 'pwd', 'vim', 'man'];
        const commandName = event.execution.commandLine.value.trim().split(' ')[0];
        if (ignoredCommands.includes(commandName))
            return;
        const exitCode = event.exitCode;
        console.log(`Terminal exit code generated: ${exitCode}`);
        (exitCode == 0) ? sendBuildStatus('failure') : sendBuildStatus('failure');
    }));
    // Debug Sessions (Run File / F5 are considered debug sessions for vscode api)
    // maybe: remove debug listener, F5 sessions are already captured by the terminal listener and actual debugging 
    // should be a separate usecase.
    // const debugSessionListener = vscode.debug.onDidTerminateDebugSession((session) => {
    // console.log(`Debug Session exit code generated from session: ${session.name}`)
    // debug session only "ends" if it was run successfully => only success case
    // 	sendBuildStatus("success");
    // })
    function sendBuildStatus(status) {
        const options = {
            hostname: LISTENER_HOST,
            port: LISTENER_PORT,
            path: '/build/' + status, // The endpoint your C# plugin expects
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Content-Length': 0
            }
        };
        console.log("Options:");
        console.log(options);
        // Send the request
        const req = http.request(options, (res) => {
            // For debugging: Log the response from the Logi Plugin
            res.setEncoding('utf8');
            res.on('data', (chunk) => {
                console.log(`Logi Plugin Response: ${chunk}`);
            });
        });
        req.on('error', (e) => {
            console.error(e);
        });
        req.end();
    }
    // context.subscriptions.push(disposable);
    context.subscriptions.push(taskEndListener);
    context.subscriptions.push(terminalEndListener);
    // context.subscriptions.push(debugSessionListener);
}
// This method is called when your extension is deactivated
function deactivate() { }
//# sourceMappingURL=extension.js.map