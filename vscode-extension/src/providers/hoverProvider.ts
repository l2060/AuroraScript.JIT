import * as vscode from 'vscode';
import * as fs from 'fs';
import { getImportDefs, getIncludedFiles, findImportDeclaration } from '../utils/navigation';
import { findDefinitionInText, findParameterDefinition, findLocalDefinition, findModuleLevelDefinition } from '../core/scoping';
import { inferType, resolveMember } from '../core/inference';
import { createHover, formatJSDoc } from '../utils/hoverUtils';

export class AuroraHoverProvider implements vscode.HoverProvider {
    constructor(private builtinModules: any) { }

    provideHover(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken): vscode.ProviderResult<vscode.Hover> {
        const range = document.getWordRangeAtPosition(position, /[$a-zA-Z0-9_]+/);
        if (!range) return null;

        const word = document.getText(range);

        const keywords = new Set([
            'declare', 'if', 'else', 'const', 'function', 'func', 'var', 'return',
            'debugger', 'break', 'yield', 'continue', 'enum', 'for', 'new',
            'delete', 'while', 'import', 'include', 'from', 'export', 'typeof',
            'true', 'false', 'null'
        ]);

        if (keywords.has(word)) {
            // Check if it's a property access (preceded by '.')
            const line = document.lineAt(position.line);
            const linePrefix = line.text.substring(0, range.start.character).trimEnd();

            // If it's not a property access, and not special keywords (import/include), skip it.
            if (!linePrefix.endsWith('.') && word !== 'import' && word !== 'include') {
                return null;
            }
        }

        if (/^(0x[0-9a-fA-F]+|\d+)$/.test(word)) {
            let val = NaN;
            if (word.startsWith('0x')) {
                val = parseInt(word, 16);
            } else {
                val = parseInt(word, 10);
            }

            if (!isNaN(val)) {
                const hex = '0x' + val.toString(16).toUpperCase();
                const dec = val.toString(10);
                const bin = '0b' + val.toString(2);
                return createHover('Numeric Preview', `${hex} | ${dec} | ${bin}`, 'text');
            }
        }

        if (word === 'import') {
            return createHover('Keyword: import', "Imports members from another module.\nSyntax: `import Alias from 'module';`", 'markdown');
        }
        if (word === 'include') {
            return createHover('Keyword: include', "Includes a file's content into the current scope.\nSyntax: `include 'path/to/file';`", 'markdown');
        }

        const importDefs = getImportDefs(document);
        if (importDefs[word]) {
            const info = findImportDeclaration(document, word);
            if (info) {
                return createHover('Module Import', info.lineContent);
            }
        }

        const line = document.lineAt(position.line);
        const textRaw = line.text;
        const linePrefix = textRaw.substring(0, range.start.character).trimEnd();

        if (linePrefix.endsWith('.')) {
            const moduleNameMatch = linePrefix.match(/([a-zA-Z_][a-zA-Z0-9_]*)\.$/);
            if (moduleNameMatch) {
                const moduleName = moduleNameMatch[1];
                const modulePath = importDefs[moduleName];

                if (modulePath && fs.existsSync(modulePath)) {
                    const fileContent = fs.readFileSync(modulePath, 'utf-8');
                    const defLoc = findDefinitionInText(fileContent, word, vscode.Uri.file(modulePath));
                    if (defLoc) {
                        const targetLines = fileContent.split('\n');
                        const targetLine = targetLines[defLoc.range.start.line];
                        return createHover('External Declaration', targetLine.trim());
                    }
                }
            }
        }

        if (linePrefix.endsWith('.')) {
            const moduleNameMatch = linePrefix.match(/([a-zA-Z_][a-zA-Z0-9_]*)\.$/);
            if (moduleNameMatch) {
                const moduleName = moduleNameMatch[1];
                if (Object.prototype.hasOwnProperty.call(this.builtinModules, moduleName) && this.builtinModules[moduleName].members && this.builtinModules[moduleName].members[word]) {
                    const memberInfo = this.builtinModules[moduleName].members[word];
                    const md = new vscode.MarkdownString();
                    md.appendCodeblock(memberInfo.detail, 'typescript');
                    md.appendMarkdown(formatJSDoc(memberInfo.documentation));
                    return createHover('Built-in Member', md);
                }
            }
        }

        if (Object.prototype.hasOwnProperty.call(this.builtinModules, word)) {
            const title = (word.startsWith('$') || word === 'global') ? 'Global Variable' : 'Built-in Module';
            const info = this.builtinModules[word];
            const md = new vscode.MarkdownString();

            if (info.detail) {
                md.appendCodeblock(info.detail, 'typescript');
            } else if (info.parameters || info.returnType) {
                let signature = word + "(";
                if (info.parameters && Array.isArray(info.parameters)) {
                    signature += info.parameters.map((p: any) => `${p.name}${p.optional ? '?' : ''}: ${p.type}`).join(', ');
                }
                signature += ")";
                if (info.returnType) {
                    signature += ": " + info.returnType;
                }
                md.appendCodeblock(signature, 'typescript');
            }

            if (info.description) {
                md.appendMarkdown(formatJSDoc(info.description));
            }

            if (md.value) {
                return createHover(title, md);
            }

            return createHover(title, formatJSDoc(info.description || ''), 'markdown');
        }

        if (!linePrefix.endsWith('.')) {
            const paramDef = findParameterDefinition(document, position, word);
            if (paramDef) {
                return createHover('Parameter', paramDef.detail);
            }

            const localDef = findLocalDefinition(document, position, word);
            if (localDef) {
                return createHover('Local Declaration', localDef.lineText);
            }

            const moduleDef = findModuleLevelDefinition(document, word);
            if (moduleDef) {
                return createHover('Module Declaration', moduleDef.lineText);
            }
        }

        const includedFiles = getIncludedFiles(document);
        for (const file of includedFiles) {
            const fileContent = fs.readFileSync(file, 'utf-8');
            const defLoc = findDefinitionInText(fileContent, word, vscode.Uri.file(file));
            if (defLoc) {
                const targetLines = fileContent.split('\n');
                const targetLine = targetLines[defLoc.range.start.line];
                return createHover('Included Declaration', targetLine.trim());
            }
        }

        const inferredType = inferType(document, range.start, this.builtinModules);
        if (inferredType) {
            const memberInfo = resolveMember(this.builtinModules, inferredType, word);
            if (memberInfo) {
                const md = new vscode.MarkdownString();
                md.appendCodeblock(memberInfo.detail, 'typescript');
                md.appendMarkdown(formatJSDoc(memberInfo.documentation));
                return createHover('Instance Member', md);
            }
        }

        return null;
    }
}
