import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';
import { AuroraDefinitionProvider } from './providers/definitionProvider';
import { AuroraHoverProvider } from './providers/hoverProvider';
import { AuroraCompletionProvider } from './providers/completionProvider';
import { AuroraFormattingProvider } from './providers/formattingProvider';
import { AuroraReferenceProvider } from './providers/referenceProvider';
import { BuiltinContentProvider } from './providers/builtinContentProvider';
import { parseLibAs } from './utils/builtinParser';
import { AuroraDebugConfigurationProvider, AuroraDebugAdapterFactory } from './debugger/auroraDebug';

export function activate(context: vscode.ExtensionContext) {
    console.log('AuroraScript extension is now active!');

    const libAsPath = path.join(context.extensionPath, 'src', 'lib.d.as');

    // Register Built-in Content Provider for Read-only view
    context.subscriptions.push(
        vscode.workspace.registerTextDocumentContentProvider(BuiltinContentProvider.scheme, new BuiltinContentProvider(libAsPath))
    );

    let builtinModules: any = {};
    if (fs.existsSync(libAsPath)) {
        try {
            const libContent = fs.readFileSync(libAsPath, 'utf-8');
            builtinModules = parseLibAs(libContent);
        } catch (e) {
            console.error('Failed to parse lib.d.as', e);
        }
    }

    // Register Definition Provider
    const definitionProvider = new AuroraDefinitionProvider(libAsPath, builtinModules);
    context.subscriptions.push(
        vscode.languages.registerDefinitionProvider('AuroraScript', definitionProvider)
    );

    // Register Reference Provider
    context.subscriptions.push(
        vscode.languages.registerReferenceProvider('AuroraScript', new AuroraReferenceProvider(definitionProvider))
    );

    // Register Formatting Provider
    context.subscriptions.push(
        vscode.languages.registerDocumentFormattingEditProvider('AuroraScript', new AuroraFormattingProvider())
    );

    // Register Hover Provider
    context.subscriptions.push(
        vscode.languages.registerHoverProvider('AuroraScript', new AuroraHoverProvider(builtinModules))
    );

    // Register Completion Provider
    context.subscriptions.push(
        vscode.languages.registerCompletionItemProvider('AuroraScript', new AuroraCompletionProvider(builtinModules), '.')
    );

    // Register Debug Configuration Provider
    context.subscriptions.push(
        vscode.debug.registerDebugConfigurationProvider('AuroraScript', new AuroraDebugConfigurationProvider())
    );

    // Register Debug Adapter Descriptor Factory
    context.subscriptions.push(
        vscode.debug.registerDebugAdapterDescriptorFactory('AuroraScript', new AuroraDebugAdapterFactory())
    );
}

export function deactivate() { }
