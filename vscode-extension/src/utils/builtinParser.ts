export interface BuiltinMember {
    kind: 'function' | 'property' | 'constant';
    detail: string;
    documentation: string;
    returnType: string;
}

export interface BuiltinModule {
    description: string;
    members: { [key: string]: BuiltinMember };
    name?: string;
    detail?: string;
    returnType?: string;
    parameters?: any[];
}

export function parseLibAs(content: string): { [key: string]: BuiltinModule } {
    const modules: { [key: string]: BuiltinModule } = {};
    const lines = content.split('\n');
    let currentDoc = "";
    let inDoc = false;

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i].trim();

        if (line.startsWith('/**')) {
            inDoc = true;
            currentDoc = line.substring(3).replace('*/', '').trim();
            if (line.endsWith('*/')) inDoc = false;
            // If it's a single line /** ... */, don't add newline yet
            continue;
        }

        if (inDoc) {
            const cleanLine = line.replace('*/', '').replace(/^\s*\*\s?/, '').trim();
            if (line.endsWith('*/')) {
                inDoc = false;
                if (cleanLine) {
                    currentDoc += currentDoc ? "\n" + cleanLine : cleanLine;
                }
            } else {
                currentDoc += currentDoc ? "\n" + cleanLine : cleanLine;
            }
            continue;
        }

        if (line.startsWith('export declare ')) {
            const decl = line.substring('export declare '.length).replace(';', '').trim();

            // Handle Top-level modules
            // e.g. export declare console;
            // e.g. export declare $state: object;
            const topLevelMatch = decl.match(/^(\$?[a-zA-Z_][a-zA-Z0-9_]*)(?:\s*:\s*([a-zA-Z_][a-zA-Z0-9_]*\[?\]?))?$/);
            if (topLevelMatch) {
                const name = topLevelMatch[1];
                const type = topLevelMatch[2];
                if (!modules[name]) {
                    modules[name] = { description: currentDoc, members: {}, name: name };
                } else {
                    modules[name].description = currentDoc;
                    modules[name].name = name; // Ensure name is set even if module existed
                }
                if (type) {
                    modules[name].detail = `${name}: ${type}`;
                    modules[name].returnType = normalizeType(type);
                }
                currentDoc = "";
                continue;
            }

            // Handle constructors
            // e.g. export declare Array(capacity?: number): Array;
            const ctorMatch = decl.match(/^([a-zA-Z_][a-zA-Z0-9_]*)\s*\(([^)]*)\)(?:\s*:\s*([a-zA-Z_][a-zA-Z0-9_]*))?$/);
            if (ctorMatch) {
                const name = ctorMatch[1];
                const params = ctorMatch[2];
                const retType = ctorMatch[3];

                if (!modules[name]) modules[name] = { description: "", members: {}, name: name }; // Ensure name is set
                modules[name].returnType = normalizeType(retType);
                modules[name].detail = `${name}(${params})${retType ? ': ' + retType : ''}`;
                // Simplified parameter parsing if needed, but returnType is most important for now
                currentDoc = "";
                continue;
            }

            // Handle members
            // e.g. export declare Math.abs(x: number): number;
            // e.g. export declare Math.PI: number;
            // e.g. export declare Object.prototype.toString(): string;
            const memberMatch = decl.match(/^([a-zA-Z_][a-zA-Z0-9_$]*)\.(?:prototype\.)?([a-zA-Z_][a-zA-Z0-9_$]*)(.*)$/);
            if (memberMatch) {
                const modName = memberMatch[1];
                const memberName = memberMatch[2];
                const remainder = memberMatch[3].trim();

                if (!modules[modName]) modules[modName] = { description: "", members: {}, name: modName }; // Ensure name is set

                const isFunction = remainder.startsWith('(');
                let returnType = "";
                const retMatch = remainder.match(/:\s*([a-zA-Z_][a-zA-Z0-9_]*\[?\]?)$/);
                if (retMatch) {
                    returnType = normalizeType(retMatch[1]);
                }

                modules[modName].members[memberName] = {
                    kind: isFunction ? 'function' : 'property',
                    detail: decl,
                    documentation: currentDoc,
                    returnType: returnType
                };
                currentDoc = "";
                continue;
            }

            currentDoc = "";
        } else if (line === "") {
            // maybe keep doc? usually declarations follow doc immediately
        } else {
            // Reset doc if we hit something else
            if (!line.startsWith('//')) {
                currentDoc = "";
            }
        }
    }

    return modules;
}

function normalizeType(type: string): string {
    if (!type) return type;
    const lower = type.toLowerCase();
    if (lower === 'number') return 'Number';
    if (lower === 'string') return 'String';
    if (lower === 'boolean') return 'Boolean';
    if (lower === 'object') return 'Object';
    if (lower === 'array') return 'Array';
    return type;
}
