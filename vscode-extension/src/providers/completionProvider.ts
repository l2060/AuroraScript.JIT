import * as vscode from 'vscode';
import * as fs from 'fs';
import { getImportDefs } from '../utils/navigation';
import { inferType } from '../core/inference';

export class AuroraCompletionProvider implements vscode.CompletionItemProvider {
    constructor(private builtinModules: any) { }

    provideCompletionItems(document: vscode.TextDocument, position: vscode.Position, token: vscode.CancellationToken, context: vscode.CompletionContext) {
        const line = document.lineAt(position.line);
        const linePrefix = line.text.substring(0, position.character);

        const typeOfMatch = linePrefix.match(/typeof\s+[^=]+={2,3}\s*["']$/);
        if (typeOfMatch) {
            const types = ['object', 'array', 'date', 'string', 'number', 'boolean', 'null', 'regex', 'function', 'clr:function', 'clr:bonding', 'clr:type'];
            const completions: vscode.CompletionItem[] = [];
            for (const t of types) {
                const item = new vscode.CompletionItem(t, vscode.CompletionItemKind.Value);
                item.detail = `typeof return value: ${t}`;
                completions.push(item);
            }
            return completions;
        }

        const match = linePrefix.match(/([a-zA-Z_][a-zA-Z0-9_]*)\.$/);
        if (match) {
            const moduleName = match[1];
            const importDefs = getImportDefs(document);
            const modulePath = importDefs[moduleName];

            if (modulePath && fs.existsSync(modulePath)) {
                const fileContent = fs.readFileSync(modulePath, 'utf-8');
                const completions: vscode.CompletionItem[] = [];
                const lines = fileContent.split('\n');

                for (const lineContent of lines) {
                    const trimmed = lineContent.trim();
                    const funcMatch = trimmed.match(/export\s+(?:function|func)\s+([a-zA-Z_][a-zA-Z0-9_]*)/);
                    if (funcMatch) {
                        const item = new vscode.CompletionItem(funcMatch[1], vscode.CompletionItemKind.Function);
                        item.detail = `(function) ${funcMatch[1]}`;
                        completions.push(item);
                        continue;
                    }

                    const varMatch = trimmed.match(/export\s+(?:var|const)\s+([a-zA-Z_][a-zA-Z0-9_]*)/);
                    if (varMatch) {
                        const item = new vscode.CompletionItem(varMatch[1], vscode.CompletionItemKind.Variable);
                        item.detail = `(variable) ${varMatch[1]}`;
                        completions.push(item);
                    }
                }
                return completions;
            }

            if (Object.prototype.hasOwnProperty.call(this.builtinModules, moduleName)) {
                const completions: vscode.CompletionItem[] = [];
                const members = this.builtinModules[moduleName].members;
                for (const memberName in members) {
                    const member = members[memberName];
                    const isInstance = member.detail.includes('.prototype.');

                    if (!isInstance) {
                        const item = new vscode.CompletionItem(memberName,
                            member.kind === 'function' ? vscode.CompletionItemKind.Method : vscode.CompletionItemKind.Property);
                        item.detail = member.detail;
                        item.documentation = new vscode.MarkdownString(member.documentation);
                        completions.push(item);
                    }
                }
                return completions;
            }
        }

        if (linePrefix.endsWith('.')) {
            const inferredType = inferType(document, position.translate(0, -1), this.builtinModules);
            if (inferredType && Object.prototype.hasOwnProperty.call(this.builtinModules, inferredType)) {
                const completions: vscode.CompletionItem[] = [];

                const addMembers = (typeName: string) => {
                    if (!Object.prototype.hasOwnProperty.call(this.builtinModules, typeName) || !this.builtinModules[typeName].members) return;
                    const members = this.builtinModules[typeName].members;
                    for (const memberName in members) {
                        const member = members[memberName];
                        const isInstance = member.detail.includes('.prototype.') || member.detail.startsWith(typeName + '.prototype.');

                        if (isInstance) {
                            const item = new vscode.CompletionItem(memberName,
                                member.kind === 'function' ? vscode.CompletionItemKind.Method : vscode.CompletionItemKind.Property);
                            item.detail = member.detail;
                            item.documentation = new vscode.MarkdownString(member.documentation);
                            completions.push(item);
                        }
                    }
                };

                addMembers(inferredType);
                if (inferredType !== 'Object') {
                    addMembers('Object');
                }

                return completions;
            }
        }

        return undefined;
    }
}
