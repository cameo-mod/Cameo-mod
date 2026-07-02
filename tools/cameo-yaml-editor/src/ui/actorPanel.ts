import * as path from "path";
import * as vscode from "vscode";
import { EntityKind, ResolvedNode } from "../model/types";
import { Indexes, findResolvedNode, resolveEntity } from "../resolve/resolver";
import { applyFieldEdit, revealNode } from "../write/yamlWriter";

function escapeHtml(s: string): string {
	return s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
}

function escapeAttr(s: string): string {
	return escapeHtml(s).replace(/'/g, "&#39;");
}

function renderNodes(nodes: ResolvedNode[], entityKey: string, kind: EntityKind, depth: number): string {
	let html = "";
	for (const n of nodes) {
		const isLeaf = n.children.length === 0;
		const isOwn = n.sourceEntityKey.toLowerCase() === entityKey.toLowerCase();
		const provenance = isOwn ? "own" : `inherited from ${escapeHtml(n.sourceEntityKey)}`;
		const pathJson = escapeAttr(JSON.stringify(n.path));
		const revealAttrs = `data-reveal-path='${pathJson}' data-reveal-kind="${kind}" data-reveal-entity="${escapeAttr(entityKey)}"`;

		if (isLeaf && n.value !== undefined) {
			html += `
			<div class="field" data-search="${escapeAttr(n.key.toLowerCase())}" style="margin-left:${depth * 16}px">
				<span class="key">${escapeHtml(n.key)}</span>
				<input type="text" value="${escapeAttr(n.value)}" title="Clear the value and Save to remove this field" data-path='${pathJson}' data-kind="${kind}" data-entity="${escapeAttr(entityKey)}" />
				<button class="save" data-path='${pathJson}' data-kind="${kind}" data-entity="${escapeAttr(entityKey)}">Save</button>
				<button class="reveal" title="Open source line" ${revealAttrs}>&#8599;</button>
				<span class="src ${isOwn ? "own" : "inherited"}">${provenance}</span>
			</div>`;
		} else {
			html += `
			<div class="branch" data-search="${escapeAttr(n.key.toLowerCase())}" style="margin-left:${depth * 16}px">
				<div class="branch-key">${escapeHtml(n.key)} <button class="reveal" title="Open source line" ${revealAttrs}>&#8599;</button></div>
				${renderNodes(n.children, entityKey, kind, depth + 1)}
			</div>`;
		}
	}
	return html;
}

function renderArmamentWeapons(entityChildren: ResolvedNode[], indexes: Indexes): string {
	const armaments = entityChildren.filter((n) => n.key === "Armament" || n.key.startsWith("Armament@"));
	if (armaments.length === 0) return "";

	let html = `<h2 id="weapons">Weapons</h2>`;
	for (const arm of armaments) {
		const weaponField = arm.children.find((c) => c.key === "Weapon" && c.value);
		if (!weaponField || !weaponField.value) continue;
		try {
			const weapon = resolveEntity(weaponField.value, "weapons", indexes);
			html += `<div class="weapon-block">
				<h3>${escapeHtml(arm.key)} &rarr; ${escapeHtml(weapon.key)}</h3>
				${renderNodes(weapon.children, weapon.key, "weapons", 0)}
			</div>`;
		} catch (err) {
			html += `<div class="weapon-block error">${escapeHtml(arm.key)} &rarr; ${escapeHtml(weaponField.value)}: ${escapeHtml(String(err))}</div>`;
		}
	}
	return html;
}

function renderSequences(entityKey: string, indexes: Indexes): string {
	if (!indexes.sequences.has(entityKey)) return "";
	try {
		const seq = resolveEntity(entityKey, "sequences", indexes);
		return `<h2 id="sequences">Sequences</h2>${renderNodes(seq.children, seq.key, "sequences", 0)}`;
	} catch (err) {
		return `<h2 id="sequences">Sequences</h2><div class="error">${escapeHtml(String(err))}</div>`;
	}
}

const STYLE = `
body { font-family: var(--vscode-font-family); color: var(--vscode-foreground); padding: 8px 16px 40px; }
h1 { font-size: 1.3em; }
h2 { font-size: 1.05em; margin-top: 24px; border-bottom: 1px solid var(--vscode-panel-border); padding-bottom: 4px; scroll-margin-top: 44px; }
h3 { font-size: 0.95em; opacity: 0.85; }
.chain { opacity: 0.7; font-size: 0.85em; margin-bottom: 12px; }
.navbar { position: sticky; top: 0; z-index: 10; display: flex; align-items: center; gap: 12px; background: var(--vscode-editor-background); border-bottom: 1px solid var(--vscode-panel-border); padding: 6px 0; margin: -8px -16px 8px; padding-left: 16px; padding-right: 16px; }
.navbar a { color: var(--vscode-textLink-foreground); text-decoration: none; cursor: pointer; }
.navbar a:hover { text-decoration: underline; }
.navbar input { flex: 1; background: var(--vscode-input-background); color: var(--vscode-input-foreground); border: 1px solid var(--vscode-input-border); padding: 3px 6px; }
.field { display: flex; align-items: center; gap: 6px; padding: 2px 0; }
.field .key { min-width: 200px; font-family: var(--vscode-editor-font-family); }
.field input { flex: 0 0 160px; background: var(--vscode-input-background); color: var(--vscode-input-foreground); border: 1px solid var(--vscode-input-border); padding: 2px 4px; }
.field .src { opacity: 0.6; }
.field .src.inherited { color: var(--vscode-descriptionForeground); }
.branch-key { font-weight: 600; margin-top: 6px; opacity: 0.9; }
.weapon-block { margin-bottom: 16px; }
.error { color: var(--vscode-errorForeground); }
button.save, button.reveal { cursor: pointer; }
button.reveal { background: transparent; border: none; color: var(--vscode-textLink-foreground); font-size: 0.9em; }
`;

const SCRIPT = `
const vscode = acquireVsCodeApi();
document.addEventListener("click", (e) => {
	const target = e.target;
	if (!(target instanceof HTMLElement)) return;
	if (target.classList.contains("save")) {
		const path = JSON.parse(target.dataset.path);
		const kind = target.dataset.kind;
		const entity = target.dataset.entity;
		const row = target.closest(".field");
		const input = row.querySelector("input");
		vscode.postMessage({ command: "save", entityKey: entity, kind, path, value: input.value });
	} else if (target.classList.contains("reveal")) {
		const path = JSON.parse(target.dataset.revealPath);
		const kind = target.dataset.revealKind;
		const entity = target.dataset.revealEntity;
		vscode.postMessage({ command: "reveal", entityKey: entity, kind, path });
	}
});

function applyFilter(q) {
	q = q.toLowerCase();
	document.querySelectorAll(".branch").forEach((b) => (b.dataset.matched = "0"));
	document.querySelectorAll(".field").forEach((f) => {
		const match = !q || f.dataset.search.includes(q);
		f.style.display = match ? "" : "none";
		if (match) {
			let p = f.parentElement;
			while (p) {
				if (p.classList && p.classList.contains("branch")) p.dataset.matched = "1";
				p = p.parentElement;
			}
		}
	});
	document.querySelectorAll(".branch").forEach((b) => {
		const selfMatch = !q || (b.dataset.search && b.dataset.search.includes(q));
		b.style.display = selfMatch || b.dataset.matched === "1" ? "" : "none";
	});
}

document.getElementById("filter").addEventListener("input", (e) => applyFilter(e.target.value));
`;

function renderActorHtml(entityKey: string, indexes: Indexes): string {
	const entity = resolveEntity(entityKey, "rules", indexes);
	const chain = entity.inheritsChain.length > 0 ? entity.inheritsChain.join(" &larr; ") : "(no ancestors)";

	return `<!DOCTYPE html>
<html>
<head><meta charset="UTF-8" /><style>${STYLE}</style></head>
<body>
	<h1>${escapeHtml(entity.key)}</h1>
	<div class="chain">Inherits: ${chain}</div>
	<div class="navbar">
		<a href="#traits">Traits</a>
		<a href="#weapons">Weapons</a>
		<a href="#sequences">Sequences</a>
		<input id="filter" type="text" placeholder="Filter fields by name..." />
	</div>
	<h2 id="traits">Traits</h2>
	${renderNodes(entity.children, entity.key, "rules", 0)}
	${renderArmamentWeapons(entity.children, indexes)}
	${renderSequences(entity.key, indexes)}
	<script>${SCRIPT}</script>
</body>
</html>`;
}

export class ActorPanelManager {
	private panels = new Map<string, { panel: vscode.WebviewPanel; entityKey: string }>();

	constructor(private getIndexes: () => Indexes | undefined) {}

	open(entityKey: string): void {
		const low = entityKey.toLowerCase();
		const existing = this.panels.get(low);
		if (existing) {
			existing.panel.reveal();
			return;
		}

		const indexes = this.getIndexes();
		if (!indexes) {
			vscode.window.showErrorMessage("Cameo YAML Editor index is not ready yet.");
			return;
		}

		const panel = vscode.window.createWebviewPanel("cameoYamlEditor.actor", entityKey, vscode.ViewColumn.Active, {
			enableScripts: true,
			retainContextWhenHidden: true,
		});
		this.panels.set(low, { panel, entityKey });
		panel.onDidDispose(() => this.panels.delete(low));

		panel.webview.onDidReceiveMessage(async (msg) => {
			const idx = this.getIndexes();
			if (!idx || !msg?.command) return;

			if (msg.command === "reveal") {
				try {
					const targetEntity = resolveEntity(msg.entityKey, msg.kind as EntityKind, idx);
					const node = findResolvedNode(targetEntity.children, msg.path);
					if (!node) throw new Error(`Field not found: ${msg.path.join(".")}`);
					await revealNode(node);
				} catch (err) {
					vscode.window.showErrorMessage(`Cameo YAML Editor: could not open source - ${err instanceof Error ? err.message : String(err)}`);
				}
				return;
			}

			if (msg.command === "save") {
				try {
					const targetEntity = resolveEntity(msg.entityKey, msg.kind as EntityKind, idx);
					const node = findResolvedNode(targetEntity.children, msg.path);
					if (!node) throw new Error(`Field not found: ${msg.path.join(".")}`);
					await applyFieldEdit(targetEntity.key, msg.kind as EntityKind, node, msg.value, idx);
					const label = msg.value.trim().length === 0 ? `removed ${msg.path.join(".")}` : `saved ${msg.path.join(".")} = ${msg.value}`;
					vscode.window.setStatusBarMessage(`Cameo YAML Editor: ${label}`, 4000);
				} catch (err) {
					vscode.window.showErrorMessage(`Cameo YAML Editor: save failed - ${err instanceof Error ? err.message : String(err)}`);
				}
				this.renderOne(low);
			}
		});

		this.renderOne(low);
	}

	refreshAll(): void {
		for (const key of this.panels.keys()) this.renderOne(key);
	}

	private renderOne(low: string): void {
		const entry = this.panels.get(low);
		const indexes = this.getIndexes();
		if (!entry || !indexes) return;
		try {
			entry.panel.webview.html = renderActorHtml(entry.entityKey, indexes);
		} catch (err) {
			entry.panel.webview.html = `<pre>${escapeHtml(err instanceof Error ? err.stack ?? err.message : String(err))}</pre>`;
		}
	}
}

export function actorPanelTitle(entityKey: string): string {
	return path.basename(entityKey);
}
