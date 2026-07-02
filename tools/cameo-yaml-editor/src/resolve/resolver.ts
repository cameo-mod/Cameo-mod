import { EntityKind, ResolvedEntity, ResolvedNode, YamlNode } from "../model/types";
import { RawIndex } from "../loader/rawIndex";

export interface Indexes {
	rules: RawIndex;
	weapons: RawIndex;
	sequences: RawIndex;
}

function cloneNode(n: YamlNode): YamlNode {
	return {
		key: n.key,
		value: n.value,
		keySpan: n.keySpan,
		valueSpan: n.valueSpan,
		children: n.children.map(cloneNode),
		file: n.file,
		sourceEntityKey: n.sourceEntityKey,
	};
}

// Stamps ownerEntityKey onto every node in this subtree that doesn't already
// carry a stamp (already-stamped nodes came from a deeper, already-resolved
// ancestor subtree and keep their real provenance). Must run recursively -
// a subtree freshly introduced by one entity's own raw YAML belongs to that
// entity all the way down, not just at its top node.
function stampMissing(n: YamlNode, ownerEntityKey: string): void {
	if (n.sourceEntityKey === undefined) n.sourceEntityKey = ownerEntityKey;
	for (const c of n.children) stampMissing(c, ownerEntityKey);
}

// A bare "-Key" line (no value, no children of its own) removes all existing
// siblings with that key. Anything else starting with "-" is a literal field.
function isRemoval(n: YamlNode): boolean {
	return n.key.startsWith("-") && n.value === undefined && n.children.length === 0;
}

function removeAllWithKey(target: YamlNode[], key: string): void {
	for (let i = target.length - 1; i >= 0; i--) if (target[i].key === key) target.splice(i, 1);
}

// Mirrors OpenRA's MiniYaml.MergePartial: same key merges in place (value wins
// if the incoming node defines one, children recurse), new keys append. "-Key"
// removal is recognized at every nesting level, not just an entity's direct
// children - matches the engine's WeakResolveRemovals running inside the same
// recursive MergePartial.
function mergeInto(target: YamlNode[], source: YamlNode, ownerEntityKey: string | undefined): void {
	if (isRemoval(source)) {
		removeAllWithKey(target, source.key.slice(1));
		return;
	}

	const stamped = cloneNode(source);
	if (ownerEntityKey) stampMissing(stamped, ownerEntityKey);

	const existing = target.find((t) => t.key === stamped.key);
	if (!existing) {
		target.push(stamped);
		return;
	}
	if (stamped.value !== undefined) {
		existing.value = stamped.value;
		existing.valueSpan = stamped.valueSpan;
		existing.file = stamped.file;
		existing.keySpan = stamped.keySpan;
		existing.sourceEntityKey = stamped.sourceEntityKey;
	}
	for (const c of stamped.children) mergeInto(existing.children, c, ownerEntityKey);
}

function indexForKind(indexes: Indexes, kind: EntityKind): RawIndex {
	return kind === "rules" ? indexes.rules : kind === "weapons" ? indexes.weapons : indexes.sequences;
}

// Recreates OpenRA's ResolveInherits: walk this entity's own children in file
// order; each is either an Inherits/Inherits@X line (recurse into the parent,
// merge its resolved tree in), a "-Key" removal, or a plain field (merge in
// directly, tagged as owned by this entity).
function resolveRaw(key: string, kind: EntityKind, indexes: Indexes, visiting: Set<string>): YamlNode[] {
	const index = indexForKind(indexes, kind);
	const visitKey = `${kind}::${key.toLowerCase()}`;
	if (visiting.has(visitKey)) {
		throw new Error(`Inheritance cycle detected while resolving "${key}" (${kind})`);
	}
	if (!index.has(key)) {
		throw new Error(`"${key}" not found in ${kind}`);
	}

	visiting.add(visitKey);
	const resolved: YamlNode[] = [];
	const canonicalKey = index.canonicalKey(key);

	for (const occurrence of index.occurrences(key)) {
		for (const child of occurrence.children) {
			if (child.key === "Inherits" || child.key.startsWith("Inherits@")) {
				if (!child.value) continue;
				const parentResolved = resolveRaw(child.value, kind, indexes, visiting);
				for (const p of parentResolved) mergeInto(resolved, p, undefined);
			} else {
				mergeInto(resolved, child, canonicalKey);
			}
		}
	}

	visiting.delete(visitKey);
	return resolved;
}

// Flattens the ancestor chain (Inherits/Inherits@X targets only, not their
// own transitive ancestors' text) for display purposes.
function collectInheritsChain(key: string, kind: EntityKind, indexes: Indexes, seen: Set<string>): string[] {
	const index = indexForKind(indexes, kind);
	const low = key.toLowerCase();
	if (seen.has(low) || !index.has(key)) return [];
	seen.add(low);

	const chain: string[] = [];
	for (const occurrence of index.occurrences(key)) {
		for (const child of occurrence.children) {
			if ((child.key === "Inherits" || child.key.startsWith("Inherits@")) && child.value) {
				chain.push(child.value);
				chain.push(...collectInheritsChain(child.value, kind, indexes, seen));
			}
		}
	}
	return chain;
}

function toResolvedNode(n: YamlNode, entityKey: string, path: string[]): ResolvedNode {
	const myPath = [...path, n.key];
	return {
		key: n.key,
		value: n.value,
		sourceFile: n.file,
		// Should always be set by mergeInto's recursive stamping; the fallback
		// only guards against an unforeseen gap rather than being expected to fire.
		sourceEntityKey: n.sourceEntityKey ?? entityKey,
		keySpan: n.keySpan,
		valueSpan: n.valueSpan,
		path: myPath,
		children: n.children.map((c) => toResolvedNode(c, entityKey, myPath)),
	};
}

export function resolveEntity(key: string, kind: EntityKind, indexes: Indexes): ResolvedEntity {
	const index = indexForKind(indexes, kind);
	const canonicalKey = index.canonicalKey(key);
	const raw = resolveRaw(key, kind, indexes, new Set());
	return {
		key: canonicalKey,
		kind,
		inheritsChain: collectInheritsChain(key, kind, indexes, new Set()),
		children: raw.map((n) => toResolvedNode(n, canonicalKey, [])),
	};
}

export function findResolvedNode(children: ResolvedNode[], path: string[]): ResolvedNode | undefined {
	let level = children;
	let node: ResolvedNode | undefined;
	for (const segment of path) {
		node = level.find((n) => n.key === segment);
		if (!node) return undefined;
		level = node.children;
	}
	return node;
}
