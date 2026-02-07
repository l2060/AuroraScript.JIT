import * as vscode from 'vscode';
import * as fs from 'fs';
import { getImportDefs, resolveImportPath, getIncludedFiles, findImportDeclaration } from '../utils/navigation';
import { findDefinitionInText, findParameterDefinition, findLocalDefinition, findModuleLevelDefinition } from '../core/scoping';
import { inferType } from '../core/inference';

export class AuroraDefinitionProvider implements vscode.DefinitionProvider {
    private libAsPath: string | undefined;
    private builtinNames: string[] = [];
    private builtinModules: { [key: string]: any } = {};

    constructor(libAsPath?: string, builtinModules?: any) {
        this.libAsPath = libAsPath;
        if (builtinModules) {
            this.builtinModules = builtinModules;
            this.builtinNames = Object.keys(builtinModules);
        }
    }

    provideDefinition(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Definition> {
        const line = document.lineAt(position.line);
        const text = line.text;
        const offset = position.character;

        let startQuote = -1;
        let endQuote = -1;
        let quoteChar = '';

        for (let i = offset; i >= 0; i--) {
            const char = text[i];
            if ((char === '"' || char === "'") && (i === 0 || text[i - 1] !== '\\')) {
                startQuote = i;
                quoteChar = char;
                break;
            }
            if (char === '>' && i > 0 && text[i - 1] === '|') {
                startQuote = i;
                quoteChar = '|>';
                break;
            }
        }

        if (startQuote !== -1) {
            if (quoteChar === '|>') {
                endQuote = text.length;
            } else {
                for (let i = startQuote + 1; i < text.length; i++) {
                    const char = text[i];
                    if (char === quoteChar && (i === 0 || text[i - 1] !== '\\')) {
                        endQuote = i;
                        break;
                    }
                }
            }
        }

        if (startQuote !== -1 && endQuote !== -1 && endQuote >= offset) {
            let content = text.substring(startQuote + 1, endQuote);
            if (quoteChar === '|>' && content.startsWith(' ')) {
                content = content.substring(1);
            }

            const filePath = content.trim();
            const lineTrimmed = text.trim();

            const isImport = lineTrimmed.startsWith('import') || lineTrimmed.startsWith('include');

            if (isImport) {
                const targetPath = resolveImportPath(document, filePath);
                if (targetPath && fs.existsSync(targetPath)) {
                    return new vscode.Location(vscode.Uri.file(targetPath), new vscode.Position(0, 0));
                }
            }
        }

        const wordRange = document.getWordRangeAtPosition(position, /\$?[a-zA-Z_][a-zA-Z0-9_]*/);
        if (wordRange) {
            const word = document.getText(wordRange);
            const linePrefix = text.substring(0, wordRange.start.character).trimEnd();

            // Handle Member Navigation (e.g., console.log, Date.now)
            if (linePrefix.endsWith('.')) {
                // Use robust inference from core/inference.ts
                const inferredType = inferType(document, wordRange.start, this.builtinModules);
                if (inferredType && this.builtinModules[inferredType]) {
                    const module = this.builtinModules[inferredType];
                    if (module.members && module.members[word]) {
                        if (this.libAsPath && fs.existsSync(this.libAsPath)) {
                            const libContent = fs.readFileSync(this.libAsPath, 'utf-8');
                            const builtinUri = vscode.Uri.parse('aurora-builtin:/lib.d.as');
                            // Use the module name or the inferred type as scope
                            const scope = module.name || inferredType;
                            const def = findDefinitionInText(libContent, word, builtinUri, scope);
                            if (def) return def;
                        }
                    }
                }

                // Fallback to simpler module resolution if inference fails or doesn't find member
                const moduleNameMatch = linePrefix.match(/(\$?[a-zA-Z_][a-zA-Z0-9_]*)\.$/);
                if (moduleNameMatch) {
                    const moduleName = moduleNameMatch[1];
                    if (this.builtinNames.includes(moduleName) && this.libAsPath && fs.existsSync(this.libAsPath)) {
                        const libContent = fs.readFileSync(this.libAsPath, 'utf-8');
                        const builtinUri = vscode.Uri.parse('aurora-builtin:/lib.d.as');
                        const def = findDefinitionInText(libContent, word, builtinUri, moduleName);
                        if (def) return def;
                    }

                    const importDefs = getImportDefs(document);
                    const modulePath = importDefs[moduleName];
                    if (modulePath && fs.existsSync(modulePath)) {
                        const fileContent = fs.readFileSync(modulePath, 'utf-8');
                        const def = findDefinitionInText(fileContent, word, vscode.Uri.file(modulePath));
                        if (def) return def;
                    }
                }
            }

            if (!linePrefix.endsWith('.')) {
                const localDef = findLocalDefinition(document, position, word);
                if (localDef) return localDef.location;

                const paramDef = findParameterDefinition(document, position, word);
                if (paramDef) return paramDef.location;

                // Check if word is a built-in top-level member (e.g., Math, console, $state)
                if ((this.builtinNames.includes(word) || word.startsWith('$')) && this.libAsPath && fs.existsSync(this.libAsPath)) {
                    const libContent = fs.readFileSync(this.libAsPath, 'utf-8');
                    const builtinUri = vscode.Uri.parse('aurora-builtin:/lib.d.as');
                    const def = findDefinitionInText(libContent, word, builtinUri);
                    if (def) return def;
                }

                const moduleDef = findModuleLevelDefinition(document, word);
                if (moduleDef) return moduleDef.location;
            }

            const includedFiles = getIncludedFiles(document);
            for (const file of includedFiles) {
                const fileContent = fs.readFileSync(file, 'utf-8');
                const includeDef = findDefinitionInText(fileContent, word, vscode.Uri.file(file));
                if (includeDef) return includeDef;
            }

            const importDefs = getImportDefs(document);
            if (importDefs[word]) {
                const info = findImportDeclaration(document, word);
                if (info) {
                    return info.location;
                }
            }

            for (const modName in importDefs) {
                const file = importDefs[modName];
                if (fs.existsSync(file)) {
                    const fileContent = fs.readFileSync(file, 'utf-8');
                    const importDef = findDefinitionInText(fileContent, word, vscode.Uri.file(file));
                    if (importDef) return importDef;
                }
            }
        }

        return null;
    }
}
