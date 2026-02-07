import * as vscode from 'vscode';
import * as fs from 'fs';
import { AuroraDefinitionProvider } from './definitionProvider';
import { isDeclarationAtPosition } from '../core/scoping';

export class AuroraReferenceProvider implements vscode.ReferenceProvider {
    private definitionProvider: AuroraDefinitionProvider;

    constructor(definitionProvider: AuroraDefinitionProvider) {
        this.definitionProvider = definitionProvider;
    }

    async provideReferences(
        document: vscode.TextDocument,
        position: vscode.Position,
        context: vscode.ReferenceContext,
        token: vscode.CancellationToken
    ): Promise<vscode.Location[]> {
        const wordRange = document.getWordRangeAtPosition(position, /\$?[a-zA-Z_][a-zA-Z0-9_]*/);
        if (!wordRange) return [];

        const word = document.getText(wordRange);

        // 1. Get the definition of the symbol under the cursor
        let definition = await this.definitionProvider.provideDefinition(document, position, token);

        let targetUri: vscode.Uri;
        let targetPosition: vscode.Position;

        if (!definition) {
            // Check if it's a declaration
            if (isDeclarationAtPosition(document, position, word)) {
                targetUri = document.uri;
                targetPosition = wordRange.start;
            } else {
                return [];
            }
        } else {
            if (Array.isArray(definition)) {
                if (definition.length === 0) return [];
                const loc = definition[0] as vscode.Location;
                targetUri = loc.uri;
                targetPosition = loc.range.start;
            } else if (definition instanceof vscode.Location) {
                targetUri = definition.uri;
                targetPosition = definition.range.start;
            } else {
                // LocationLink
                const link = definition as vscode.LocationLink;
                targetUri = link.targetUri;
                targetPosition = link.targetRange.start;
            }
        }

        // 2. Determine Scope
        let isGlobal = false;
        if (targetUri.scheme === 'aurora-builtin') {
            isGlobal = true;
        } else {
            const targetDoc = await vscode.workspace.openTextDocument(targetUri);
            const lineText = targetDoc.lineAt(targetPosition.line).text.trim();
            if (lineText.startsWith('export') || lineText.includes('global')) {
                isGlobal = true;
            }
        }

        const references: vscode.Location[] = [];

        // 3. Scan files based on scope
        const filesToScan: vscode.Uri[] = [];
        if (isGlobal) {
            const allFiles = await vscode.workspace.findFiles('**/*.as');
            filesToScan.push(...allFiles);
        } else {
            filesToScan.push(document.uri);
            if (targetUri.toString() !== document.uri.toString()) {
                filesToScan.push(targetUri);
            }
        }

        for (const file of filesToScan) {
            if (token.isCancellationRequested) break;

            const doc = await vscode.workspace.openTextDocument(file);
            const text = doc.getText();

            const escapedWord = word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            const regex = new RegExp(`\\b${escapedWord}\\b`, 'g');
            let match;

            while ((match = regex.exec(text)) !== null) {
                if (token.isCancellationRequested) break;

                const matchPosition = doc.positionAt(match.index);

                // 4. For each hit, check if it resolves to the same definition
                const matchDefinition = await this.definitionProvider.provideDefinition(doc, matchPosition, token);

                let matchTargetUri: vscode.Uri | undefined;
                let matchTargetPosition: vscode.Position | undefined;

                if (!matchDefinition) {
                    if (isDeclarationAtPosition(doc, matchPosition, word)) {
                        matchTargetUri = doc.uri;
                        matchTargetPosition = doc.getWordRangeAtPosition(matchPosition, /\$?[a-zA-Z_][a-zA-Z0-9_]*/)?.start;
                    }
                } else {
                    if (Array.isArray(matchDefinition)) {
                        if (matchDefinition.length > 0) {
                            const loc = matchDefinition[0] as vscode.Location;
                            matchTargetUri = loc.uri;
                            matchTargetPosition = loc.range.start;
                        }
                    } else if (matchDefinition instanceof vscode.Location) {
                        matchTargetUri = matchDefinition.uri;
                        matchTargetPosition = matchDefinition.range.start;
                    } else {
                        const link = matchDefinition as vscode.LocationLink;
                        matchTargetUri = link.targetUri;
                        matchTargetPosition = link.targetRange.start;
                    }
                }

                if (matchTargetUri && matchTargetPosition &&
                    matchTargetUri.toString() === targetUri.toString() &&
                    matchTargetPosition.line === targetPosition.line &&
                    matchTargetPosition.character === targetPosition.character) {

                    references.push(new vscode.Location(file, doc.getWordRangeAtPosition(matchPosition, /\$?[a-zA-Z_][a-zA-Z0-9_]*/) || new vscode.Range(matchPosition, matchPosition)));
                }
            }
        }

        return references;
    }
}
