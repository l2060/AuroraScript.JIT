import * as vscode from 'vscode';

// Helper: Create a unified hover
export function createHover(title: string, content: string | vscode.MarkdownString, language: string = 'AuroraScript'): vscode.Hover {
    const md = new vscode.MarkdownString();
    md.appendMarkdown(`**${title}**\n\n`);
    if (content instanceof vscode.MarkdownString) {
        md.appendMarkdown(content.value);
    } else {
        md.appendCodeblock(content, language);
    }
    return new vscode.Hover(md);
}

/**
 * Formats JSDoc description by converting @param and @returns into markdown sections.
 */
export function formatJSDoc(doc: string): string {
    if (!doc) return "";

    const lines = doc.split('\n');
    let formattedDoc = "";
    let inParams = false;
    let inReturns = false;

    for (let line of lines) {
        line = line.trim();
        if (line.startsWith('@param')) {
            if (!inParams) {
                formattedDoc += "\n\n**Parameters:**\n";
                inParams = true;
            }
            const match = line.match(/@param\s+([$a-zA-Z0-9_]+)\s*(.*)/);
            if (match) {
                formattedDoc += `- \`${match[1]}\`: ${match[2]}\n`;
            } else {
                formattedDoc += `- ${line.substring(7).trim()}\n`;
            }
        } else if (line.startsWith('@returns')) {
            formattedDoc += "\n\n**Returns:**\n" + line.substring(8).trim() + "\n";
            inReturns = true;
        } else if (line.startsWith('@return')) {
            formattedDoc += "\n\n**Returns:**\n" + line.substring(7).trim() + "\n";
            inReturns = true;
        } else {
            // Regular description text
            formattedDoc += (formattedDoc ? "\n" : "") + line;
        }
    }

    return formattedDoc.trim();
}
