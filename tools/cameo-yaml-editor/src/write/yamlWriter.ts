import * as vscode from "vscode";
import { EntityKind, ResolvedNode, Span, YamlNode } from "../model/types";
import { RawIndex } from "../loader/rawIndex";
import { Indexes } from "../resolve/resolver";

function indexForKind(indexes: Indexes, kind: EntityKind): RawIndex {
	return kind === "rules" ? indexes.rules : kind === "weapons" ? indexes.weapons : indexes.sequences;
}

function maxLine(n: YamlNode): number {
	let m = n.keySpan.end.line;
	if (n.valueSpan) m = Math.max(m, n.valueSpan.end.line);
	for (const c of n.children) m = Math.max(m, maxLine(c));
	return m;
}

function toVscodeRange(span: Span): vscode.Range {
	return new vscode.Range(span.start.line, span.start.col, span.end.line, span.end.col);
}

async function saveDoc(uri: vscode.Uri): Promise<void> {
	const doc = vscode.workspace.textDocuments.find((d) => d.uri.fsPath === uri.fsPath);
	if (doc && doc.isDirty) await doc.save();
}

// Appends the missing tail of `path` under the entity's own block in its own
// file (creating intermediate nested blocks as needed). When newValue is
// undefined, the final line is a "-Key" removal instead of "Key: value".
async function insertOverride(
	entityKey: string,
	kind: EntityKind,
	path: string[],
	newValue: string | undefined,
	indexes: Indexes,
): Promise<void> {
	const index = indexForKind(indexes, kind);
	const occurrences = index.occurrences(entityKey);
	if (occurrences.length === 0) throw new Error(`Cannot find "${entityKey}" to edit`);
	const ownNode = occurrences[occurrences.length - 1];

	let level = ownNode.children;
	let matchedDepth = 0;
	let anchor: YamlNode = ownNode;
	for (let i = 0; i < path.length; i++) {
		const found = level.find((c) => c.key === path[i]);
		if (!found) break;
		matchedDepth++;
		anchor = found;
		level = found.children;
	}

	if (matchedDepth === path.length && newValue === undefined) {
		// The full path already exists as this entity's own text - just delete it.
		await deleteOwnLine(anchor.file, anchor.keySpan.start.line);
		return;
	}

	const baseIndent = 1; // entity itself is indent 0; its direct children start at indent 1
	const lines: string[] = [];
	for (let i = matchedDepth; i < path.length; i++) {
		const indent = "\t".repeat(baseIndent + i);
		const isLast = i === path.length - 1;
		if (isLast) lines.push(indent + (newValue === undefined ? `-${path[i]}` : `${path[i]}: ${newValue}`));
		else lines.push(indent + path[i] + ":");
	}

	const uri = vscode.Uri.file(ownNode.file);
	const edit = new vscode.WorkspaceEdit();
	const insertLine = maxLine(anchor) + 1;
	edit.insert(uri, new vscode.Position(insertLine, 0), lines.join("\n") + "\n");
	await vscode.workspace.applyEdit(edit);
	await saveDoc(uri);
}

// Deletes a whole line (the node's own literal text), not just a value span -
// used when clearing an own-authored field entirely rather than blanking it.
async function deleteOwnLine(file: string, line: number): Promise<void> {
	const uri = vscode.Uri.file(file);
	const doc = await vscode.workspace.openTextDocument(uri);
	const edit = new vscode.WorkspaceEdit();
	const range =
		line + 1 < doc.lineCount
			? new vscode.Range(line, 0, line + 1, 0)
			: new vscode.Range(Math.max(0, line - 1), doc.lineAt(Math.max(0, line - 1)).range.end.character, line, doc.lineAt(line).range.end.character);
	edit.delete(uri, range);
	await vscode.workspace.applyEdit(edit);
	await saveDoc(uri);
}

// Edits (or clears) a resolved leaf field's value:
// - non-empty newValue on an own field: replace the value text in place.
// - non-empty newValue on an inherited-only field: append the missing
//   override under the entity's own block.
// - empty newValue on an own field: delete that whole line.
// - empty newValue on an inherited-only field: append a "-Key" removal line.
// Never edits an ancestor's file - only ever touches the currently-viewed
// entity's own file.
export async function applyFieldEdit(entityKey: string, kind: EntityKind, node: ResolvedNode, newValue: string, indexes: Indexes): Promise<void> {
	const isOwn = node.sourceEntityKey.toLowerCase() === entityKey.toLowerCase();
	const isRemoval = newValue.trim().length === 0;

	if (isOwn && node.valueSpan && !isRemoval) {
		const uri = vscode.Uri.file(node.valueSpan.file);
		const edit = new vscode.WorkspaceEdit();
		edit.replace(uri, toVscodeRange(node.valueSpan), newValue);
		await vscode.workspace.applyEdit(edit);
		await saveDoc(uri);
		return;
	}

	if (isOwn && isRemoval) {
		await deleteOwnLine(node.sourceFile, node.keySpan.start.line);
		return;
	}

	await insertOverride(entityKey, kind, node.path, isRemoval ? undefined : newValue, indexes);
}

// Opens the file/line a resolved node actually comes from (its own file if
// own-authored, or the ancestor file that currently supplies it).
export async function revealNode(node: ResolvedNode): Promise<void> {
	const uri = vscode.Uri.file(node.sourceFile);
	const doc = await vscode.workspace.openTextDocument(uri);
	const editor = await vscode.window.showTextDocument(doc, { preview: false });
	const pos = new vscode.Position(node.keySpan.start.line, node.keySpan.start.col);
	editor.selection = new vscode.Selection(pos, pos);
	editor.revealRange(new vscode.Range(pos, pos), vscode.TextEditorRevealType.InCenter);
}
