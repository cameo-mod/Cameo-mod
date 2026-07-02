import * as fs from "fs";
import { Span, YamlNode } from "../model/types";

interface StackEntry {
	depth: number;
	node: { children: YamlNode[] };
}

// "#" starts a comment unless escaped as "\#" (which becomes a literal "#").
function stripUnescapedComment(line: string): string {
	let out = "";
	for (let i = 0; i < line.length; i++) {
		const ch = line[i];
		if (ch === "#") {
			if (i > 0 && line[i - 1] === "\\") {
				out = out.slice(0, -1) + "#";
				continue;
			}
			break;
		}
		out += ch;
	}
	return out;
}

// Parses OpenRA's MiniYAML format: tab-indented, "Key: Value" or "Key:" lines,
// duplicate sibling keys allowed (e.g. repeated "Inherits:"), "#" full-line comments.
export function parseText(text: string, file: string): YamlNode[] {
	const root: { children: YamlNode[] } = { children: [] };
	const stack: StackEntry[] = [{ depth: -1, node: root }];
	const lines = text.split(/\r\n|\r|\n/);

	for (let lineNo = 0; lineNo < lines.length; lineNo++) {
		const raw = stripUnescapedComment(lines[lineNo]);
		const trimmed = raw.trim();
		if (trimmed.length === 0) continue;

		let depth = 0;
		while (depth < raw.length && raw[depth] === "\t") depth++;
		const content = raw.slice(depth);
		if (content.trim().length === 0) continue;

		const colonIdx = content.indexOf(":");
		let key: string;
		let value: string | undefined;
		let keyStartCol: number;
		let keyEndCol: number;
		let valueSpan: Span | undefined;

		if (colonIdx === -1) {
			key = content.trim();
			keyStartCol = depth + (content.length - content.trimStart().length);
			keyEndCol = keyStartCol + key.length;
			value = undefined;
		} else {
			const rawKey = content.slice(0, colonIdx);
			key = rawKey.trim();
			keyStartCol = depth + (rawKey.length - rawKey.trimStart().length);
			keyEndCol = keyStartCol + key.length;

			const rawValue = content.slice(colonIdx + 1);
			const valueTrimmed = rawValue.trim();
			if (valueTrimmed.length > 0) {
				const leadingWs = rawValue.length - rawValue.trimStart().length;
				const valueStartCol = depth + colonIdx + 1 + leadingWs;
				valueSpan = {
					file,
					start: { line: lineNo, col: valueStartCol },
					end: { line: lineNo, col: valueStartCol + valueTrimmed.length },
				};
				value = valueTrimmed;
			} else {
				value = undefined;
			}
		}

		const node: YamlNode = {
			key,
			value,
			keySpan: {
				file,
				start: { line: lineNo, col: keyStartCol },
				end: { line: lineNo, col: keyEndCol },
			},
			valueSpan,
			children: [],
			file,
		};

		while (stack.length > 0 && stack[stack.length - 1].depth >= depth) stack.pop();
		if (stack.length === 0) stack.push({ depth: -1, node: root });
		stack[stack.length - 1].node.children.push(node);
		stack.push({ depth, node });
	}

	return root.children;
}

const fileCache = new Map<string, YamlNode[]>();

export function parseFile(absPath: string, useCache = true): YamlNode[] {
	if (useCache && fileCache.has(absPath)) return fileCache.get(absPath)!;
	const text = fs.readFileSync(absPath, "utf8");
	const nodes = parseText(text, absPath);
	fileCache.set(absPath, nodes);
	return nodes;
}

export function clearParseCache(): void {
	fileCache.clear();
}

export function findChild(nodes: YamlNode[], key: string): YamlNode | undefined {
	return nodes.find((n) => n.key === key);
}

export function findChildren(nodes: YamlNode[], key: string): YamlNode[] {
	return nodes.filter((n) => n.key === key);
}
