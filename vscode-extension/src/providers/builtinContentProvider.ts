import * as vscode from 'vscode';
import * as fs from 'fs';

export class BuiltinContentProvider implements vscode.TextDocumentContentProvider {
    static scheme = 'aurora-builtin';

    constructor(private libAsPath: string) { }

    provideTextDocumentContent(uri: vscode.Uri): string {
        if (fs.existsSync(this.libAsPath)) {
            return fs.readFileSync(this.libAsPath, 'utf8');
        }
        return `// Error: Could not find lib.d.as at ${this.libAsPath}`;
    }
}
