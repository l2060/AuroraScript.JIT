import * as vscode from 'vscode';
import * as path from 'path';
import * as fs from 'fs';

// Helper: Resolve import path to absolute path
export function resolveImportPath(document: vscode.TextDocument, filePath: string): string | null {
    const currentDir = path.dirname(document.uri.fsPath);
    let targetPath = path.join(currentDir, filePath);
    if (!path.extname(targetPath)) targetPath += '.as';
    return targetPath;
}

// Helper: Get map of { alias: absolutePath }
export function getImportDefs(document: vscode.TextDocument): { [alias: string]: string } {
    const text = document.getText();
    const lines = text.split('\n');
    const imports: { [alias: string]: string } = {};

    for (const line of lines) {
        const trimmed = line.trim();
        // import alias from 'path';
        const importMatch = trimmed.match(/^import\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+from\s+["']([^"']+)["']/);
        if (importMatch) {
            const alias = importMatch[1];
            const pathStr = importMatch[2];
            const resolved = resolveImportPath(document, pathStr);
            if (resolved) imports[alias] = resolved;
        }
    }
    return imports;
}

// Helper: Find all imported file paths in a document
export function findAllImports(document: vscode.TextDocument): string[] {
    const imports = getImportDefs(document);
    return Object.values(imports);
}

// Helper: Find declaration info of an import alias
export function findImportDeclaration(document: vscode.TextDocument, alias: string): { location: vscode.Location, lineContent: string } | null {
    const text = document.getText();
    const lines = text.split('\n');
    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        // match: import alias from ...
        const match = line.match(new RegExp(`^\\s*import\\s+${alias}\\s+from`));
        if (match) {
            return {
                location: new vscode.Location(document.uri, new vscode.Position(i, 0)),
                lineContent: line.trim()
            };
        }
    }
    return null;
}

// Helper: Get list of included files (absolute paths)
export function getIncludedFiles(document: vscode.TextDocument): string[] {
    const text = document.getText();
    const currentDir = path.dirname(document.uri.fsPath);
    const includedFiles: string[] = [];
    // include 'path';
    const regex = /include\s+["']([^"']+)["'];/g;
    let match;
    while ((match = regex.exec(text)) !== null) {
        const filePath = match[1];
        let targetPath = path.join(currentDir, filePath);
        if (!path.extname(targetPath)) targetPath += '.as';
        if (fs.existsSync(targetPath)) {
            includedFiles.push(targetPath);
        }
    }
    return includedFiles;
}
