import { YamlNode } from "../model/types";
import { parseFile } from "../parser/miniyaml";

// All raw top-level occurrences of every entity key, across every file in a
// Rules:/Weapons:/Sequences: list, in load order. A key can legitimately have
// multiple occurrences (the same entity re-opened/extended in a later file).
// Entity/actor names are matched case-insensitively (OpenRA's own ruleset
// dictionaries are), unlike trait/field keys nested inside them.
export class RawIndex {
	private byKey = new Map<string, YamlNode[]>();
	private displayKey = new Map<string, string>(); // lowercase -> first-seen original casing
	readonly files: string[];

	constructor(files: string[]) {
		this.files = files;
		for (const file of files) {
			let nodes: YamlNode[];
			try {
				nodes = parseFile(file);
			} catch {
				continue; // missing/unreadable file - skip rather than fail the whole index
			}
			for (const node of nodes) {
				const low = node.key.toLowerCase();
				const list = this.byKey.get(low);
				if (list) list.push(node);
				else {
					this.byKey.set(low, [node]);
					this.displayKey.set(low, node.key);
				}
			}
		}
	}

	has(key: string): boolean {
		return this.byKey.has(key.toLowerCase());
	}

	// All raw occurrences of an entity's top-level node (usually one, but can be
	// more than one when a later file re-opens the same key).
	occurrences(key: string): YamlNode[] {
		return this.byKey.get(key.toLowerCase()) ?? [];
	}

	// The casing the entity was first defined with (for display purposes).
	canonicalKey(key: string): string {
		return this.displayKey.get(key.toLowerCase()) ?? key;
	}

	// All entity keys defined directly (not via Inherits) in a given file.
	keysDefinedIn(file: string): string[] {
		const out: string[] = [];
		for (const [low, nodes] of this.byKey) {
			if (nodes.some((n) => n.file === file)) out.push(this.displayKey.get(low)!);
		}
		return out;
	}

	allKeys(): string[] {
		return [...this.byKey.values()].map((nodes) => nodes[0].key);
	}
}
