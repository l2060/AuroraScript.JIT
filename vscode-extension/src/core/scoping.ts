import * as vscode from 'vscode';

// Helper: Brace-aware backwards scan for local definitions (var/const/func)
export function findLocalDefinition(document: vscode.TextDocument, position: vscode.Position, word: string): { location: vscode.Location, rhs: string, lineText: string, rhsEndPosition?: vscode.Position } | null {
    const text = document.getText();
    const startOffset = document.offsetAt(position);

    if (startOffset > text.length) return null;

    let braceDepth = 0;
    let minDepth = 0;
    let inString = false;
    let quoteChar = '';

    for (let i = startOffset - 1; i >= 0; i--) {
        const char = text[i];

        if ((char === '"' || char === "'") && (i === 0 || text[i - 1] !== '\\')) {
            if (inString && quoteChar === char) {
                inString = false;
            } else if (!inString) {
                inString = true;
                quoteChar = char;
            }
        }
        if (inString) continue;
        if (char === '\n') {
            // Check if the current line (going backwards) starts with '|>' after skipping whitespace
            // Since we are going backwards, we need to check the characters after the newline
            let j = i + 1;
            while (j < text.length && (text[j] === ' ' || text[j] === '\t')) j++;
            if (j + 1 < text.length && text[j] === '|' && text[j + 1] === '>') {
                // This line is a pipe string, skip everything until the start of the line
                // (which is effectively what we are doing by ignoring it in the scan)
                // Actually, the simplest way is to just ignore brackets on this line.
            }
        }

        // Simpler approach for pipe strings: if a line starts with |>, ignore its structural symbols
        // We can check this by looking at the start of the current line
        const lineStart = text.lastIndexOf('\n', i) + 1;
        let linePrefix = text.substring(lineStart, i + 1);
        if (linePrefix.trimStart().startsWith('|>')) {
            continue;
        }

        if (char === '}') {
            braceDepth++;
        } else if (char === '{') {
            braceDepth--;
            if (braceDepth < minDepth) {
                minDepth = braceDepth;
            }
        }

        if (braceDepth > minDepth) continue;

        if (text[i] === word[word.length - 1]) {
            const start = i - word.length + 1;
            if (start >= 0 && text.substring(start, i + 1) === word) {
                const prevChar = start > 0 ? text[start - 1] : ' ';
                const nextChar = i < text.length - 1 ? text[i + 1] : ' ';

                if (!/[a-zA-Z0-9_]/.test(prevChar) && !/[a-zA-Z0-9_]/.test(nextChar)) {
                    const beforeWord = text.substring(0, start).trimEnd();

                    if (beforeWord.endsWith('var') || beforeWord.endsWith('const')) {
                        const kw = beforeWord.endsWith('var') ? 'var' : 'const';
                        const kwStart = beforeWord.length - kw.length;
                        const kwPrev = kwStart > 0 ? beforeWord[kwStart - 1] : ' ';

                        if (!/[a-zA-Z0-9_]/.test(kwPrev)) {
                            const remainder = text.substring(i + 1);
                            const eqMatch = remainder.match(/^\s*=\s*/);
                            let rhs = "";
                            let rhsEndPosition: vscode.Position | undefined;

                            if (eqMatch) {
                                const rhsStartOffset = i + 1 + eqMatch[0].length;
                                const rhsMatch = text.substring(rhsStartOffset).match(/^([^;\n]+)/);
                                if (rhsMatch) {
                                    rhs = rhsMatch[1].trim();
                                    rhsEndPosition = document.positionAt(rhsStartOffset + rhsMatch[1].trimEnd().length);
                                }
                            }

                            const loc = new vscode.Location(document.uri, document.positionAt(start));
                            const line = document.lineAt(loc.range.start.line);
                            return { location: loc, rhs, lineText: line.text.trim(), rhsEndPosition };
                        }
                    }

                    if (beforeWord.endsWith('function') || beforeWord.endsWith('func')) {
                        const kw = beforeWord.endsWith('function') ? 'function' : 'func';
                        const kwStart = beforeWord.length - kw.length;
                        const kwPrev = kwStart > 0 ? beforeWord[kwStart - 1] : ' ';

                        if (!/[a-zA-Z0-9_]/.test(kwPrev)) {
                            const loc = new vscode.Location(document.uri, document.positionAt(start));
                            const line = document.lineAt(loc.range.start.line);
                            return { location: loc, rhs: '', lineText: line.text.trim() };
                        }
                    }
                }
            }
        }
    }
    return null;
}

// Helper: Find parameter definition by scanning backwards from cursor
export function findParameterDefinition(document: vscode.TextDocument, position: vscode.Position, word: string): { location: vscode.Location, detail: string } | null {
    const text = document.getText();
    const offset = document.offsetAt(position);
    let depth = 0;

    for (let i = offset - 1; i >= 0; i--) {
        const char = text[i];
        if (char === '}') {
            depth++;
        } else if (char === '{') {
            depth--;
            if (depth < 0) {
                // Check if this brace is inside a pipe string
                const lineStart = text.lastIndexOf('\n', i) + 1;
                if (text.substring(lineStart, i + 1).trimStart().startsWith('|>')) {
                    depth++; // Undo depth change if inside pipe string
                    continue;
                }
                // We just stepped out of an opening brace. This means the word might be a parameter 
                // of the function/construct that this brace belongs to.
                const prefix = text.substring(0, i).trimEnd();

                // 1. Check for Arrow Functions: (a, b) => { ... }
                if (prefix.endsWith('=>')) {
                    const arrowIndex = prefix.lastIndexOf('=>');
                    const beforeArrow = prefix.substring(0, arrowIndex).trimEnd();
                    if (beforeArrow.endsWith(')')) {
                        const closeParen = beforeArrow.lastIndexOf(')');
                        let pDepth = 0;
                        let openParen = -1;
                        for (let j = closeParen; j >= 0; j--) {
                            if (beforeArrow[j] === ')') pDepth++;
                            else if (beforeArrow[j] === '(') {
                                pDepth--;
                                if (pDepth === 0) {
                                    openParen = j;
                                    break;
                                }
                            }
                        }

                        if (openParen !== -1) {
                            const paramsText = beforeArrow.substring(openParen + 1, closeParen);
                            const params = paramsText.split(',').map(p => p.trim());
                            if (params.includes(word)) {
                                const wordIndex = paramsText.indexOf(word);
                                const wordPos = document.positionAt(openParen + 1 + wordIndex);
                                return {
                                    location: new vscode.Location(document.uri, wordPos),
                                    detail: `(parameter) ${word}: object`
                                };
                            }
                        }
                    } else {
                        // Single parameter arrow function: x => { ... }
                        const lastWordMatch = beforeArrow.match(/([a-zA-Z0-9_]+)$/);
                        if (lastWordMatch && lastWordMatch[1] === word) {
                            const wordPos = document.positionAt(lastWordMatch.index!);
                            return {
                                location: new vscode.Location(document.uri, wordPos),
                                detail: `(parameter) ${word}: object`
                            };
                        }
                    }
                }

                // 2. Check for Named/Anonymous functions: function name(a, b) { ... }
                const funcHeaderMatch = prefix.match(/(?:function|func)\s*[a-zA-Z0-9_]*\s*\(([^)]*)\)$/);
                if (funcHeaderMatch) {
                    const paramsText = funcHeaderMatch[1];
                    const params = paramsText.split(',').map(p => p.trim());
                    if (params.includes(word)) {
                        const wordIndex = paramsText.indexOf(word);
                        const headerStart = i - funcHeaderMatch[0].length;
                        const openParenIndex = funcHeaderMatch[0].indexOf('(');
                        const wordPos = document.positionAt(headerStart + openParenIndex + 1 + wordIndex);
                        return {
                            location: new vscode.Location(document.uri, wordPos),
                            detail: `(parameter) ${word}: object`
                        };
                    }
                }

                // If not found in this scope, continue searching in parent scopes
                depth = 0;
            }
        }
    }
    return null;
}

// Helper: Find definition of a word in a text content (Naive global/first-match scan)
export function findDefinitionInText(text: string, word: string, uri: vscode.Uri, scope?: string): vscode.Location | null {
    const lines = text.split('\n');
    let startLine = 0;
    let endLine = lines.length;

    if (scope) {
        const scopeRegex = new RegExp(`//\\s*@name\\s+${scope}\\b`);
        let foundScope = false;
        for (let i = 0; i < lines.length; i++) {
            if (scopeRegex.test(lines[i])) {
                startLine = i;
                foundScope = true;
                // Find the end of this scope (next @name or EOF)
                for (let j = i + 1; j < lines.length; j++) {
                    if (lines[j].trim().startsWith('// @name')) {
                        endLine = j;
                        break;
                    }
                }
                break;
            }
        }
    }

    const escapedWord = word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const escapedScope = scope ? scope.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') : "";

    for (let i = startLine; i < endLine; i++) {
        const line = lines[i].trim();
        if (line.startsWith('|>')) continue;

        // 1. Support 'export declare' format
        if (line.includes('declare')) {
            let declarePattern = "";
            if (scope && scope !== word) {
                // Member or Constructor search
                // Matches "export declare Scope.Word", "export declare Scope.prototype.Word", or "export declare Scope(Word..."
                declarePattern = `export\\s+declare\\s+${escapedScope}(?:\\.(?:prototype\\.)?${escapedWord}\\b|\\s*\\(${escapedWord})|export\\s+declare\\s+${escapedWord}\\b`;
            } else {
                // Top-level or same-as-scope search
                // Matches "export declare Word"
                declarePattern = `export\\s+declare\\s+${escapedWord}\\b`;
            }

            if (new RegExp(declarePattern).test(line)) {
                const col = lines[i].indexOf(word);
                if (col !== -1) {
                    return new vscode.Location(uri, new vscode.Position(i, col));
                }
            }
        }

        // 2. Existing standard formats
        const funcRegex = new RegExp(`(function|func)\\s+${escapedWord}\\s*\\(`);
        if (funcRegex.test(line)) {
            return new vscode.Location(uri, new vscode.Position(i, lines[i].indexOf(word)));
        }

        const varRegex = new RegExp(`(var|const|global)\\s+${escapedWord}\\s*(=|:|\\s|;)`);
        if (varRegex.test(line)) {
            return new vscode.Location(uri, new vscode.Position(i, lines[i].indexOf(word)));
        }

        const propRegex = new RegExp(`^${escapedWord}\\s*:`);
        if (propRegex.test(line)) {
            return new vscode.Location(uri, new vscode.Position(i, lines[i].indexOf(word)));
        }
    }

    // Fallback search: if we didn't find it in the scoped block (or no scope block found),
    // try searching the whole file for the flat 'export declare' format
    if (scope && startLine !== 0) {
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i].trim();
            if (line.includes('declare')) {
                const declarePattern = scope !== word
                    ? `export\\s+declare\\s+${escapedScope}(?:\\.(?:prototype\\.)?${escapedWord}\\b|\\s*\\(${escapedWord})|export\\s+declare\\s+${escapedWord}\\b`
                    : `export\\s+declare\\s+${escapedWord}\\b`;
                if (new RegExp(declarePattern).test(line)) {
                    const col = lines[i].indexOf(word);
                    if (col !== -1) {
                        return new vscode.Location(uri, new vscode.Position(i, col));
                    }
                }
            }
        }
    }

    return null;
}

// Helper: Check if the word at the given position is a declaration (preceded by var, const, func, etc.)
export function isDeclarationAtPosition(document: vscode.TextDocument, position: vscode.Position, word: string): boolean {
    const text = document.lineAt(position.line).text;
    const wordRange = document.getWordRangeAtPosition(position, /\$?[a-zA-Z_][a-zA-Z0-9_]*/);
    if (!wordRange) return false;

    const beforeWord = text.substring(0, wordRange.start.character).trimEnd();
    const keywords = ['var', 'const', 'function', 'func', 'global', 'declare', 'export'];

    for (const kw of keywords) {
        if (beforeWord.endsWith(kw)) {
            const kwStart = beforeWord.length - kw.length;
            const kwPrev = kwStart > 0 ? beforeWord[kwStart - 1] : ' ';
            if (!/[a-zA-Z0-9_]/.test(kwPrev)) {
                return true;
            }
        }
    }
    return false;
}

// Helper: Scan entire file for top-level definitions (depth 0)
export function findModuleLevelDefinition(document: vscode.TextDocument, word: string): { location: vscode.Location, rhs: string, lineText: string } | null {
    const text = document.getText();
    const lines = text.split('\n');
    let braceDepth = 0;
    let inString = false;
    let quoteChar = '';

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        const trimmed = line.trim();
        if (trimmed.startsWith('|>')) continue;

        // Simple brace tracking for the whole file
        for (let j = 0; j < line.length; j++) {
            const char = line[j];
            if ((char === '"' || char === "'") && (j === 0 || line[j - 1] !== '\\')) {
                if (inString && quoteChar === char) inString = false;
                else if (!inString) { inString = true; quoteChar = char; }
            }
            if (inString) continue;
            if (char === '{') braceDepth++;
            else if (char === '}') braceDepth--;
        }

        // We only care about declarations at the top level
        // Check prevBraceDepth to catch the line that starts a block
        const prevBraceDepth = braceDepth - (line.match(/{/g) || []).length + (line.match(/}/g) || []).length;

        if (prevBraceDepth === 0) {
            const escapedWord = word.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
            // Match var x, const x, function x, func x, global x, export var x, etc.
            const declRegex = new RegExp(`\\b(var|const|function|func|global|export|declare)\\s+${escapedWord}\\b`);
            const match = line.match(declRegex);
            if (match) {
                const col = line.indexOf(word);
                return {
                    location: new vscode.Location(document.uri, new vscode.Position(i, col)),
                    rhs: '',
                    lineText: trimmed
                };
            }
        }
    }
    return null;
}

