import * as path from "path";
import * as vscode from "vscode";
import { buildModIndex, ModIndex } from "./loader/modIndex";
import { RawIndex } from "./loader/rawIndex";
import { clearParseCache } from "./parser/miniyaml";
import { Indexes } from "./resolve/resolver";
import { FactionTreeProvider } from "./ui/factionTree";
import { ActorPanelManager } from "./ui/actorPanel";
import { searchActors } from "./ui/actorSearch";

export function activate(context: vscode.ExtensionContext): void {
	const workspaceRootMaybe = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
	if (!workspaceRootMaybe) return;
	const workspaceRoot: string = workspaceRootMaybe;

	let modIndex: ModIndex | undefined;
	let indexes: Indexes | undefined;

	const treeProvider = new FactionTreeProvider(
		() => modIndex,
		() => indexes?.rules,
	);
	const panelManager = new ActorPanelManager(() => indexes);

	function rebuild(): void {
		clearParseCache();
		const config = vscode.workspace.getConfiguration("cameoYamlEditor");
		const modYamlRel = config.get<string>("modYamlPath") ?? "mods/cameo/mod.yaml";
		const engineDirRel = config.get<string>("engineDir") ?? "engine";
		const modYamlPath = path.join(workspaceRoot, modYamlRel);
		const engineDir = path.join(workspaceRoot, engineDirRel);

		try {
			modIndex = buildModIndex(modYamlPath, engineDir);
			indexes = {
				rules: new RawIndex(modIndex.ruleFiles),
				weapons: new RawIndex(modIndex.weaponFiles),
				sequences: new RawIndex(modIndex.sequenceFiles),
			};
		} catch (err) {
			modIndex = undefined;
			indexes = undefined;
			vscode.window.showErrorMessage(
				`Cameo YAML Editor: failed to build index - ${err instanceof Error ? err.message : String(err)}`,
			);
		}
		treeProvider.refresh();
		panelManager.refreshAll();
	}

	context.subscriptions.push(
		vscode.window.registerTreeDataProvider("cameoYamlEditor.factionTree", treeProvider),
		vscode.commands.registerCommand("cameoYamlEditor.refresh", rebuild),
		vscode.commands.registerCommand("cameoYamlEditor.openActor", (key: string) => panelManager.open(key)),
		vscode.commands.registerCommand("cameoYamlEditor.searchActors", async () => {
			if (!indexes) {
				vscode.window.showErrorMessage("Cameo YAML Editor index is not ready yet.");
				return;
			}
			const key = await searchActors(indexes.rules);
			if (key) panelManager.open(key);
		}),
	);

	rebuild();
}

export function deactivate(): void {}
