import * as vscode from 'vscode';
import { WorkspaceFolder, DebugConfiguration, ProviderResult, CancellationToken } from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

export class AuroraDebugConfigurationProvider implements vscode.DebugConfigurationProvider {

    /**
     * Massage a debug configuration just before a debug session is being launched,
     * e.g. add all missing attributes to the debug configuration.
     */
    resolveDebugConfiguration(folder: WorkspaceFolder | undefined, config: DebugConfiguration, token?: CancellationToken): ProviderResult<DebugConfiguration> {

        // if launch.json is missing or empty
        if (!config.type && !config.request && !config.name) {
            const editor = vscode.window.activeTextEditor;
            if (editor && editor.document.languageId === 'AuroraScript') {
                config.type = 'AuroraScript';
                config.name = 'Launch';
                config.request = 'launch';
                config.program = '${file}';
                config.stopOnEntry = true;
            }
        }

        if (config.request !== 'attach' && !config.program) {
            return vscode.window.showInformationMessage("Cannot find a program to debug").then(_ => {
                return undefined;
            });
        }

        return config;
    }
}

export class AuroraDebugAdapterFactory implements vscode.DebugAdapterDescriptorFactory {

    createDebugAdapterDescriptor(session: vscode.DebugSession, executable: vscode.DebugAdapterExecutable | undefined): ProviderResult<vscode.DebugAdapterDescriptor> {

        if (session.configuration.request === 'attach') {
            const host = session.configuration.host || 'localhost';
            const port = session.configuration.port || 4711;
            return new vscode.DebugAdapterServer(port, host);
        }

        const workspaceFolder = session.workspaceFolder;
        if (!workspaceFolder) {
            return undefined;
        }

        // Try to find the built executable
        const debugBinPath = path.join(workspaceFolder.uri.fsPath, 'examples', 'bin', 'Debug', 'net6.0', 'examples.dll');
        const releaseBinPath = path.join(workspaceFolder.uri.fsPath, 'examples', 'bin', 'Release', 'net6.0', 'examples.dll');

        // Default to debug bin
        let targetDll = debugBinPath;
        if (fs.existsSync(releaseBinPath) && !fs.existsSync(debugBinPath)) {
            targetDll = releaseBinPath;
        }

        // Check if we can run it
        if (!fs.existsSync(targetDll)) {
            // Fallback to dotnet run if no binary found (might fail due to stdout pollution)
            // However, for debugging we really prefer the DLL.
            const projectPath = path.join(workspaceFolder.uri.fsPath, 'examples', 'examples.csproj');
            return new vscode.DebugAdapterExecutable('dotnet', [
                'run',
                '--project',
                projectPath,
                '--',
                '--debug-stdio'
            ]);
        }

        return new vscode.DebugAdapterExecutable('dotnet', [
            targetDll,
            '--debug-stdio'
        ]);
    }
}
