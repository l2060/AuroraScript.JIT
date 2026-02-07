import * as vscode from 'vscode';

export class AuroraFormatter {
    public formatDocument(document: vscode.TextDocument, options: vscode.FormattingOptions): vscode.TextEdit[] {
        const text = document.getText();
        const formatted = this.format(text, options);
        const fullRange = new vscode.Range(
            document.positionAt(0),
            document.positionAt(text.length)
        );
        return [vscode.TextEdit.replace(fullRange, formatted)];
    }

    private format(text: string, options: vscode.FormattingOptions): string {
        let output = '';
        let i = 0;
        const len = text.length;

        // Indentation state
        let indentLevel = 0;
        const singleIndent = options.insertSpaces ? ' '.repeat(options.tabSize) : '\t';

        // Scanner state
        let state: 'CODE' | 'STRING_SINGLE' | 'STRING_DOUBLE' | 'STRING_BACKTICK' | 'PIPE_STRING' | 'COMMENT_LINE' | 'COMMENT_BLOCK' | 'REGEX' = 'CODE';
        let braceNestingStack: number[] = [0];
        let lastTokenChar = ''; // To help detect unary operators and REGEX start
        let regexInCharClass = false; // To track [...] inside regex
        let lastWasUnary = false;

        // Accumulate current line to apply indentation before appending to output
        let currentLineBuffer = '';

        // Helper to append text
        const append = (str: string) => {
            currentLineBuffer += str;
        };

        const flushLine = () => {
            // Apply indentation if the line is not empty
            const trimmed = currentLineBuffer.trim();
            if (trimmed.length > 0) {
                // Adjust indent for closing braces
                if (trimmed.startsWith('}') || trimmed.startsWith(']') || trimmed.startsWith(')')) {
                    indentLevel = Math.max(0, indentLevel - 1);
                }

                output += singleIndent.repeat(indentLevel) + trimmed;

                // Adjust indent for opening braces
                // We count braces in the line to update level for NEXT line
                // But simplified: just check endsWith for now, or scan the line.
                // Better: Check tokens as we scanned them? 
                // Since we are rebuilding the string, let's trust the braces we encountered.
                // We will update indentLevel dynamically during scan? 
                // Problem: Indent is applied at start of line.
                // So we need to calculate net indent change for the line.
            }
            output += '\n'; // Preserve newline
            currentLineBuffer = '';
        };

        // We'll build the whole text, but handle indentation on newlines.
        // Actually, simpler:
        // 1. Tokenize/Format the stream into a single clean string with correct token spacing (ignoring indentation).
        // 2. Pass 2: Split by newline and apply indentation.

        // Pass 1: Spacing
        let formattedStream = '';
        let structureStream = ''; // To track what is code vs what is content

        while (i < len) {
            const c = text[i];
            const next = i + 1 < len ? text[i + 1] : '';
            const next2 = i + 2 < len ? text[i + 2] : '';

            if (state === 'CODE') {
                // Whitespace
                if (/\s/.test(c)) {
                    if (c === '\n') {
                        formattedStream = formattedStream.replace(/[ \t]+$/, ''); // Only trim horizontal whitespace
                        formattedStream += '\n';
                        structureStream += '\n';
                        lastWasUnary = false;
                    } else if (c !== '\r') {
                        // Collapse spaces
                        // Ensure we don't add space if previous char is ( or [ or unary operator
                        if (!formattedStream.endsWith(' ') && !formattedStream.endsWith('\n') && !formattedStream.endsWith('(') && !formattedStream.endsWith('[') && !lastWasUnary) {
                            formattedStream += ' ';
                            structureStream += ' ';
                        }
                    }
                    i++;
                    continue;
                }

                // Check comments
                if (c === '/' && next === '/') {
                    state = 'COMMENT_LINE';
                    formattedStream += '//';
                    structureStream += '  ';
                    i += 2;
                    continue;
                }
                if (c === '/' && next === '*') {
                    state = 'COMMENT_BLOCK';
                    formattedStream += '/*';
                    structureStream += '  ';
                    i += 2;
                    continue;
                }

                // Check Regex (context sensitive)
                if (c === '/') {
                    // It's a regex if it follows an operator, punctuation (except ), ], }), or certain keywords
                    // Simplified: check last token char
                    const isRegexStart = lastTokenChar === '' || '(=,:[!&|?+-*%/><^~;{'.includes(lastTokenChar) ||
                        /(^|[\s\W])(return|yield|case|throw|delete|typeof|void|in|new|func|function)$/.test(formattedStream.trimEnd());

                    if (isRegexStart) {
                        state = 'REGEX';
                        regexInCharClass = false;
                        formattedStream += '/';
                        structureStream += ' ';
                        i++;
                        continue;
                    }
                }

                // Check Strings
                if (c === '"') { state = 'STRING_DOUBLE'; formattedStream += c; structureStream += ' '; i++; continue; }
                if (c === "'") { state = 'STRING_SINGLE'; formattedStream += c; structureStream += ' '; i++; continue; }
                if (c === '`') { state = 'STRING_BACKTICK'; formattedStream += c; structureStream += ' '; i++; continue; }
                if (c === '|' && next === '>') { state = 'PIPE_STRING'; formattedStream += '|>'; structureStream += '  '; i += 2; continue; }

                // Punctuation & Operators
                if (c === ',') {
                    formattedStream += ', ';
                    structureStream += ', ';
                    i++;
                    lastTokenChar = ',';
                    lastWasUnary = false;
                    continue;
                }
                if (c === ';') {
                    formattedStream += ';';
                    structureStream += ';';
                    if (next !== '\n' && next !== '\r' && next !== ' ' && next !== '\t' && next !== '') {
                        formattedStream += ' ';
                        structureStream += ' ';
                    }
                    i++;
                    lastTokenChar = ';';
                    lastWasUnary = false;
                    continue;
                }

                // Braces & Parens
                if (c === '{') {
                    if (!formattedStream.endsWith(' ') && !formattedStream.endsWith('\n') && !formattedStream.endsWith('(') && !formattedStream.endsWith('[')) {
                        formattedStream += ' ';
                        structureStream += ' ';
                    }
                    formattedStream += '{';
                    structureStream += '{';
                    braceNestingStack[braceNestingStack.length - 1]++;
                    if (next !== '}' && next !== '\n' && next !== '\r') {
                        formattedStream += ' ';
                        structureStream += ' ';
                    }
                    i++;
                    lastTokenChar = '{';
                    lastWasUnary = false;
                    continue;
                }
                if (c === '}') {
                    if (braceNestingStack[braceNestingStack.length - 1] > 0) {
                        braceNestingStack[braceNestingStack.length - 1]--;
                    } else if (braceNestingStack.length > 1) {
                        // Exit template interpolation
                        if (!formattedStream.endsWith(' ') && !formattedStream.endsWith('\n')) {
                            formattedStream += ' ';
                            structureStream += ' ';
                        }
                        formattedStream += '}';
                        structureStream += '}';
                        braceNestingStack.pop();
                        state = 'STRING_BACKTICK';
                        i++;
                        lastTokenChar = '}';
                        lastWasUnary = false;
                        continue;
                    }

                    if (lastTokenChar !== '{' && !formattedStream.endsWith(' ') && !formattedStream.endsWith('\n')) {
                        formattedStream += ' ';
                        structureStream += ' ';
                    }
                    formattedStream += '}';
                    structureStream += '}';
                    i++;
                    lastTokenChar = '}';
                    lastWasUnary = false;
                    continue;
                }

                if (c === '(') {
                    const trimmedSafe = formattedStream.trimEnd();
                    if (/(^|[\s\W])(if|for|while|switch|catch|with)$/.test(trimmedSafe)) {
                        if (!formattedStream.endsWith(' ')) {
                            formattedStream += ' ';
                            structureStream += ' ';
                        }
                    } else if (trimmedSafe.endsWith('function') || trimmedSafe.endsWith('func')) {
                        if (!formattedStream.endsWith(' ')) {
                            formattedStream += ' ';
                            structureStream += ' ';
                        }
                    }

                    formattedStream += '(';
                    structureStream += '(';
                    i++;
                    lastTokenChar = '(';
                    lastWasUnary = false;
                    continue;
                }

                if (c === ')') {
                    if (formattedStream.endsWith(' ')) {
                        formattedStream = formattedStream.slice(0, -1);
                        structureStream = structureStream.slice(0, -1);
                    }
                    formattedStream += ')';
                    structureStream += ')';
                    i++;
                    lastTokenChar = ')';
                    lastWasUnary = false;
                    continue;
                }

                if (c === '[') {
                    formattedStream += c;
                    structureStream += c;
                    i++;
                    lastTokenChar = '[';
                    lastWasUnary = false;
                    continue;
                }

                if (c === ']') {
                    if (formattedStream.endsWith(' ')) {
                        formattedStream = formattedStream.slice(0, -1);
                        structureStream = structureStream.slice(0, -1);
                    }
                    formattedStream += ']';
                    structureStream += ']';
                    i++;
                    lastTokenChar = ']';
                    lastWasUnary = false;
                    continue;
                }

                // Colon
                if (c === ':') {
                    formattedStream += ':';
                    structureStream += ':';
                    if (next !== ' ' && next !== '\t' && next !== '\n' && next !== '\r') {
                        formattedStream += ' ';
                        structureStream += ' ';
                    }
                    i++;
                    lastTokenChar = ':';
                    lastWasUnary = false;
                    continue;
                }

                // Operators
                // Identify operator
                let op = '';
                if (['+', '-', '*', '/', '%', '=', '!', '<', '>', '&', '|', '^', '?'].includes(c)) {
                    // Check for 3-char ops (e.g. >>>, === if exists, etc)
                    if (['>>=', '<<=', '>>>'].includes(c + next + next2)) op = c + next + next2;
                    else if (['+=', '-=', '*=', '/=', '%=', '==', '!=', '<=', '>=', '&&', '||', '++', '--', '=>', '<<', '>>'].includes(c + next)) op = c + next;
                    else op = c;
                }

                if (op) {
                    const trimmedSafe = formattedStream.trimEnd();
                    const isKeywordUnary = /(^|[\s\W])(return|throw|case|yield|delete|typeof|void|in|new|func|function|default)$/.test(trimmedSafe);
                    // If op is preceded by anything that triggers unary, OR a keyword like return
                    const isUnary = ['+', '-', '!', '~'].includes(op) && (lastTokenChar === '' || '+-*/%=!&|^<>,({[:;'.includes(lastTokenChar) || isKeywordUnary);

                    // Exceptions: ++, -- attached to var?
                    if (op === '++' || op === '--') {
                        formattedStream += op; // Connect tight?
                        structureStream += ' '.repeat(op.length);
                        lastWasUnary = false;
                    } else if (isUnary) {
                        formattedStream += op;
                        structureStream += ' '.repeat(op.length);
                        lastWasUnary = true;
                    } else {
                        // Binary: space around
                        if (!formattedStream.endsWith(' ') && !formattedStream.endsWith('\n')) {
                            formattedStream += ' ';
                            structureStream += ' ';
                        }
                        formattedStream += op;
                        structureStream += ' '.repeat(op.length);
                        if (next !== ' ' && next !== '\t' && next !== '\n' && next !== '\r') {
                            formattedStream += ' ';
                            structureStream += ' ';
                        }
                        lastWasUnary = false;
                    }

                    i += op.length;
                    lastTokenChar = op[0];
                    continue;
                }

                // If previous char was ')' or '}', and we are starting a word (not dot), add space.
                // e.g. "if (cond) ret" -> ") ret", "}else" -> "} else"
                if ((lastTokenChar === ')' || lastTokenChar === '}') && /[a-zA-Z_$]/.test(c)) {
                    if (!formattedStream.endsWith(' ') && !formattedStream.endsWith('\n')) {
                        formattedStream += ' ';
                        structureStream += ' ';
                    }
                }

                formattedStream += c;
                structureStream += ' '; // Mask identifier content
                i++;
                lastTokenChar = c; // assume identifier part
                lastWasUnary = false;
            }
            else if (state === 'STRING_DOUBLE') {
                formattedStream += c;
                structureStream += ' ';
                if (c === '"' && text[i - 1] !== '\\') {
                    state = 'CODE';
                    lastTokenChar = '"';
                    lastWasUnary = false;
                }
                i++;
            }
            else if (state === 'STRING_BACKTICK') {
                if (c === '\\' && (next === '$' || next === '`')) {
                    formattedStream += c + next;
                    structureStream += '  ';
                    i += 2;
                    continue;
                }
                if (c === '$' && next === '{') {
                    formattedStream += '${ ';
                    structureStream += '${ ';
                    i += 2;
                    state = 'CODE';
                    braceNestingStack.push(0);
                    continue;
                }

                formattedStream += c;
                structureStream += ' ';
                if (c === '`' && (i === 0 || text[i - 1] !== '\\')) {
                    state = 'CODE';
                    lastTokenChar = '`';
                    lastWasUnary = false;
                }
                i++;
            }
            else if (state === 'STRING_SINGLE') {
                formattedStream += c;
                structureStream += ' ';
                if (c === "'" && text[i - 1] !== '\\') {
                    state = 'CODE';
                    lastTokenChar = "'";
                    lastWasUnary = false;
                }
                i++;
            }
            else if (state === 'PIPE_STRING') {
                formattedStream += c;
                structureStream += c === '\n' ? '\n' : ' ';
                if (c === '\n') {
                    state = 'CODE';
                    lastTokenChar = '>';
                    lastWasUnary = false;
                }
                i++;
            }
            else if (state === 'COMMENT_LINE') {
                formattedStream += c;
                structureStream += c === '\n' ? '\n' : ' ';
                if (c === '\n') state = 'CODE';
                i++;
            }
            else if (state === 'REGEX') {
                formattedStream += c;
                structureStream += ' ';
                if (c === '\\') {
                    // Escape next char
                    i++;
                    if (i < len) {
                        formattedStream += text[i];
                        structureStream += ' ';
                    }
                } else if (c === '[') {
                    regexInCharClass = true;
                } else if (c === ']') {
                    regexInCharClass = false;
                } else if (c === '/' && !regexInCharClass) {
                    // Close regex
                    state = 'CODE';
                    lastTokenChar = '/';
                    // Consume flags
                    i++;
                    while (i < len && /[a-z]/i.test(text[i])) {
                        formattedStream += text[i];
                        structureStream += ' ';
                        i++;
                    }
                    continue; // Skip the default i++ at end of loop
                }
                i++;
            }
            else if (state === 'COMMENT_BLOCK') {
                formattedStream += c;
                structureStream += c === '\n' ? '\n' : ' ';
                if (c === '/' && text[i - 1] === '*') state = 'CODE';
                i++;
            }
        }

        // Pass 2: Indentation
        let finalOutput = '';
        const rawLines = formattedStream.split('\n');
        const structureLines = structureStream.split('\n');

        for (let j = 0; j < rawLines.length; j++) {
            const line = rawLines[j];
            const structureLine = structureLines[j];
            const trimmed = line.trim();
            const structureTrimmed = structureLine.trim();

            if (j > 0) finalOutput += '\n';

            if (trimmed.length === 0) {
                // Preserve perfectly empty line
                continue;
            }

            // Adjust indent level
            if (structureTrimmed.startsWith('}') || structureTrimmed.startsWith(']') || structureTrimmed.startsWith(')')) {
                indentLevel = Math.max(0, indentLevel - 1);
            }

            finalOutput += singleIndent.repeat(indentLevel) + trimmed;

            // Post-line indent adjustment
            // Count open/close braces in the structure stream to verify net change
            if (structureTrimmed.endsWith('{') || structureTrimmed.endsWith('[') || structureTrimmed.endsWith('(')) {
                indentLevel++;
            }
        }

        return finalOutput;
    }
}
