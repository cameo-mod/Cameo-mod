import * as path from "path";
import * as vscode from "vscode";
import { FactionEntry, ModIndex } from "../loader/modIndex";
import { RawIndex } from "../loader/rawIndex";

type TreeElement =
	| { type: "theme"; name: string; factions: FactionEntry[] }
	| { type: "faction"; label: string; ruleFiles: string[] }
	| { type: "file"; file: string }
	| { type: "actor"; key: string };

export class FactionTreeProvider implements vscode.TreeDataProvider<TreeElement> {
	private readonly _onDidChangeTreeData = new vscode.EventEmitter<void>();
	readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

	constructor(
		private getModIndex: () => ModIndex | undefined,
		private getRulesIndex: () => RawIndex | undefined,
	) {}

	refresh(): void {
		this._onDidChangeTreeData.fire();
	}

	getTreeItem(element: TreeElement): vscode.TreeItem {
		switch (element.type) {
			case "theme": {
				const item = new vscode.TreeItem(element.name, vscode.TreeItemCollapsibleState.Collapsed);
				item.iconPath = new vscode.ThemeIcon("folder-library");
				return item;
			}
			case "faction": {
				const item = new vscode.TreeItem(element.label, vscode.TreeItemCollapsibleState.Collapsed);
				item.iconPath = new vscode.ThemeIcon("organization");
				return item;
			}
			case "file": {
				const item = new vscode.TreeItem(path.basename(element.file), vscode.TreeItemCollapsibleState.Collapsed);
				item.iconPath = new vscode.ThemeIcon("file-code");
				item.resourceUri = vscode.Uri.file(element.file);
				return item;
			}
			case "actor": {
				const item = new vscode.TreeItem(element.key, vscode.TreeItemCollapsibleState.None);
				item.iconPath = new vscode.ThemeIcon("symbol-class");
				item.command = {
					command: "cameoYamlEditor.openActor",
					title: "Open Actor",
					arguments: [element.key],
				};
				return item;
			}
		}
	}

	getChildren(element?: TreeElement): TreeElement[] {
		const modIndex = this.getModIndex();
		const rulesIndex = this.getRulesIndex();
		if (!modIndex || !rulesIndex) return [];

		if (!element) {
			const themeMap = new Map<string, FactionEntry[]>();
			for (const f of modIndex.factions) {
				const theme = f.pathSegments[0];
				const list = themeMap.get(theme);
				if (list) list.push(f);
				else themeMap.set(theme, [f]);
			}
			const themes: TreeElement[] = [...themeMap.entries()]
				.sort(([a], [b]) => a.localeCompare(b))
				.map(([name, factions]) => ({ type: "theme", name, factions }));

			if (modIndex.baseRuleFiles.length > 0) {
				themes.unshift({
					type: "theme",
					name: "Base Rules",
					factions: [{ pathSegments: ["Base Rules"], contentYamlPath: "", ruleFiles: modIndex.baseRuleFiles }],
				});
			}
			return themes;
		}

		if (element.type === "theme") {
			if (element.factions.length === 1 && element.factions[0].pathSegments.length <= 1) {
				return element.factions[0].ruleFiles.map((file): TreeElement => ({ type: "file", file }));
			}
			return element.factions
				.map((f): TreeElement => ({ type: "faction", label: f.pathSegments[f.pathSegments.length - 1], ruleFiles: f.ruleFiles }))
				.sort((a, b) => (a.type === "faction" && b.type === "faction" ? a.label.localeCompare(b.label) : 0));
		}

		if (element.type === "faction") {
			return element.ruleFiles.map((file): TreeElement => ({ type: "file", file }));
		}

		if (element.type === "file") {
			return rulesIndex
				.keysDefinedIn(element.file)
				.filter((k) => !k.startsWith("^"))
				.sort((a, b) => a.localeCompare(b))
				.map((key): TreeElement => ({ type: "actor", key }));
		}

		return [];
	}
}
