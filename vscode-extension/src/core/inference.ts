import * as vscode from 'vscode';
import { findLocalDefinition } from './scoping';

// Helper: Resolve member in type or Object prototype
export function resolveMember(builtinModules: any, typeName: string, memberName: string): any | null {
    if (Object.prototype.hasOwnProperty.call(builtinModules, typeName) && builtinModules[typeName].members && Object.prototype.hasOwnProperty.call(builtinModules[typeName].members, memberName)) {
        return builtinModules[typeName].members[memberName];
    }
    if (typeName !== 'Object' && Object.prototype.hasOwnProperty.call(builtinModules, 'Object') && builtinModules['Object'].members && Object.prototype.hasOwnProperty.call(builtinModules['Object'].members, memberName)) {
        return builtinModules['Object'].members[memberName];
    }
    return null;
}

// Helper: Infer type of the expression before the cursor
export function inferType(document: vscode.TextDocument, position: vscode.Position, builtinModules: any, depth: number = 0): string | null {
    if (depth > 5) return null;

    const line = document.lineAt(position.line);
    let textBefore = line.text.substring(0, position.character).trimEnd();

    if (textBefore.endsWith('.')) {
        textBefore = textBefore.substring(0, textBefore.length - 1).trimEnd();
    }

    if (textBefore.endsWith('"') || textBefore.endsWith("'")) return 'String';
    if (textBefore.trimEnd().endsWith(']')) return 'Array';
    if (textBefore.endsWith('true') || textBefore.endsWith('false')) return 'Boolean';
    if (/^\d+$/.test(textBefore) || /[^a-zA-Z_$0-9]\d+$/.test(textBefore)) return 'Number';

    if (textBefore.endsWith(')')) {
        let pDepth = 0;
        let openParenIndex = -1;
        for (let i = textBefore.length - 1; i >= 0; i--) {
            if (textBefore[i] === ')') pDepth++;
            else if (textBefore[i] === '(') {
                pDepth--;
                if (pDepth === 0) {
                    openParenIndex = i;
                    break;
                }
            }
        }

        if (openParenIndex !== -1) {
            const funcCallPart = textBefore.substring(0, openParenIndex).trimEnd();
            const match = funcCallPart.match(/([a-zA-Z_][a-zA-Z0-9_]*)$/);
            if (match) {
                const funcName = match[1];
                const prefix = funcCallPart.substring(0, funcCallPart.length - funcName.length).trimEnd();

                if (prefix.endsWith('.')) {
                    const objectPart = prefix.substring(0, prefix.length - 1);
                    const endOfObjectPartIndex = textBefore.lastIndexOf(objectPart) + objectPart.length;
                    const nestedPos = new vscode.Position(position.line, endOfObjectPartIndex);

                    if (Object.prototype.hasOwnProperty.call(builtinModules, objectPart)) {
                        const member = resolveMember(builtinModules, objectPart, funcName);
                        if (member && member.returnType) return member.returnType;
                    }

                    if (objectPart === 'Date' && funcName === 'now') return 'Date';

                    const inferredBaseType = inferType(document, nestedPos, builtinModules, depth + 1);
                    if (inferredBaseType) {
                        const member = resolveMember(builtinModules, inferredBaseType, funcName);
                        if (member && member.returnType) return member.returnType;
                    }
                }
            }
        }
    }

    if (!textBefore.endsWith(')')) {
        const wordMatch = textBefore.match(/([a-zA-Z_][a-zA-Z0-9_]*)$/);
        if (wordMatch) {
            const word = wordMatch[1];
            const prefix = textBefore.substring(0, textBefore.length - word.length).trimEnd();
            if (prefix.endsWith('.')) {
                const objectPart = prefix.substring(0, prefix.length - 1);
                const endOfObjectPartIndex = textBefore.lastIndexOf(objectPart) + objectPart.length;
                const nestedPos = new vscode.Position(position.line, endOfObjectPartIndex);

                const inferredBaseType = inferType(document, nestedPos, builtinModules, depth + 1);
                if (inferredBaseType && Object.prototype.hasOwnProperty.call(builtinModules, inferredBaseType) && builtinModules[inferredBaseType].members && builtinModules[inferredBaseType].members[word]) {
                    const member = builtinModules[inferredBaseType].members[word];
                    if (member.returnType) return member.returnType;
                }
            }
        }
    }

    const wordMatch = textBefore.match(/([a-zA-Z_][a-zA-Z0-9_]*)$/);
    if (wordMatch) {
        const word = wordMatch[1];
        if (Object.prototype.hasOwnProperty.call(builtinModules, word)) return word;

        const def = findLocalDefinition(document, position, word);
        if (def && def.rhs) {
            const rhs = def.rhs;

            if (rhs.startsWith('"') || rhs.startsWith("'")) return 'String';
            if (rhs.startsWith('[') && (rhs.endsWith(']') || rhs.includes(']'))) return 'Array';
            if (rhs.startsWith('true') || rhs.startsWith('false')) return 'Boolean';
            if (/^\d/.test(rhs)) return 'Number';
            if (rhs.startsWith('new ')) {
                const ctorMatch = rhs.match(/new\s+([a-zA-Z_][a-zA-Z0-9_]*)/);
                if (ctorMatch) return ctorMatch[1];
            }
            if (rhs.match(/Date\.now\(\)/)) return 'Date';

            if (def.rhsEndPosition) {
                const inferred = inferType(document, def.rhsEndPosition, builtinModules, depth + 1);
                if (inferred) return inferred;
            }
            return null;
        }
    }

    return null;
}
