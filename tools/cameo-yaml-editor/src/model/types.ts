export interface Position {
	line: number; // 0-based
	col: number; // 0-based, in characters
}

export interface Span {
	file: string;
	start: Position;
	end: Position;
}

// A single parsed MiniYAML line, tree-structured by indentation.
// MiniYAML allows duplicate keys as siblings (e.g. repeated "Inherits:" lines),
// so children are an ordered array, never a map.
export interface YamlNode {
	key: string;
	value: string | undefined;
	keySpan: Span;
	valueSpan: Span | undefined;
	children: YamlNode[];
	file: string;
	// Set only on resolved (post-merge) clones: which top-level entity key last
	// wrote this node's current value/children. Undefined on raw parser output.
	sourceEntityKey?: string;
}

export type EntityKind = "rules" | "weapons" | "sequences";

// A leaf/branch in a *resolved* (post-Inherits) entity tree, annotated with
// where its current value actually came from, so the UI can show provenance
// and the writer knows which file/span to edit.
export interface ResolvedNode {
	key: string;
	value: string | undefined;
	children: ResolvedNode[];
	// Where the effective value (this exact node) was last set from.
	sourceFile: string;
	sourceEntityKey: string;
	keySpan: Span;
	valueSpan: Span | undefined;
	// Full key path from the entity root, e.g. ["Armament@elite", "Weapon"]
	path: string[];
}

export interface ResolvedEntity {
	key: string;
	kind: EntityKind;
	inheritsChain: string[]; // flattened ancestor entity keys, in resolution order
	children: ResolvedNode[];
}
