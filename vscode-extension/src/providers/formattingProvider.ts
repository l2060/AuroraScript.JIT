import * as vscode from 'vscode';
import { AuroraFormatter } from '../formatter';

export class AuroraFormattingProvider implements vscode.DocumentFormattingEditProvider {
    provideDocumentFormattingEdits(document: vscode.TextDocument, options: vscode.FormattingOptions, token: vscode.CancellationToken): vscode.TextEdit[] {
        const formatter = new AuroraFormatter();
        return formatter.formatDocument(document, options);
    }
}
