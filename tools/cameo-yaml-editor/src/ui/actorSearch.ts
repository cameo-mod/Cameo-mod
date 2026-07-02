import * as vscode from "vscode";
import { RawIndex } from "../loader/rawIndex";

interface ActorQuickPickItem extends vscode.QuickPickItem {
	entityKey: string;
}

// Cheap (non-inheritance-resolving) name/description lookup: reads each
// actor's OWN raw Tooltip block if present. Actors that only inherit their
// Tooltip (rare - most author it locally) just show their key.
function buildItems(rulesIndex: RawIndex): ActorQuickPickItem[] {
	const items: ActorQuickPickItem[] = [];
	for (const key of rulesIndex.allKeys()) {
		if (key.startsWith("^")) continue;
		const occurrences = rulesIndex.occurrences(key);
		let name: string | undefined;
		let description: string | undefined;
		for (const occ of occurrences) {
			const tooltip = occ.children.find((c) => c.key === "Tooltip" || c.key.startsWith("Tooltip@"));
			if (!tooltip) continue;
			name = tooltip.children.find((c) => c.key === "Name")?.value ?? name;
			description = tooltip.children.find((c) => c.key === "Description")?.value ?? description;
		}
		items.push({
			entityKey: key,
			label: key,
			description: name,
			detail: description,
		});
	}
	return items.sort((a, b) => a.label.localeCompare(b.label));
}

export async function searchActors(rulesIndex: RawIndex): Promise<string | undefined> {
	const picked = await vscode.window.showQuickPick(buildItems(rulesIndex), {
		title: "Search Cameo Actors",
		placeHolder: "Search by actor key, display name, or description...",
		matchOnDescription: true,
		matchOnDetail: true,
	});
	return picked?.entityKey;
}
