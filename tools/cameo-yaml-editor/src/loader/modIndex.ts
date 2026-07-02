import * as path from "path";
import { YamlNode } from "../model/types";
import { parseFile } from "../parser/miniyaml";
import { buildPackageAliasMap, resolvePackagePath } from "./packages";

export interface FactionEntry {
	// e.g. ["RedAlert2Mod", "SchwarzerMond"] or ["RedAlert2"] for non-split themes
	pathSegments: string[];
	contentYamlPath: string;
	ruleFiles: string[];
}

export interface ModIndex {
	modRoot: string;
	aliasMap: Map<string, string>;
	ruleFiles: string[];
	weaponFiles: string[];
	sequenceFiles: string[];
	factions: FactionEntry[];
	baseRuleFiles: string[];
}

function fileListValues(listNode: YamlNode): string[] {
	return listNode.children.map((c) => c.key).filter((k) => k.length > 0);
}

export function buildModIndex(modYamlPath: string, engineDir: string): ModIndex {
	const modRoot = path.dirname(modYamlPath);
	const rootNodes = parseFile(modYamlPath, false);
	const aliasMap = buildPackageAliasMap(rootNodes, modRoot, engineDir);

	const ruleFiles: string[] = [];
	const weaponFiles: string[] = [];
	const sequenceFiles: string[] = [];
	const baseRuleFiles: string[] = [];
	const factions: FactionEntry[] = [];

	function resolveFileList(listNode: YamlNode): string[] {
		const out: string[] = [];
		for (const token of fileListValues(listNode)) {
			const resolved = resolvePackagePath(token, aliasMap);
			if (resolved) out.push(resolved);
		}
		return out;
	}

	function deriveFactionSegments(contentYamlAbsPath: string): string[] {
		const rel = path.relative(path.join(aliasMap.get("ContentPacks") ?? modRoot), contentYamlAbsPath);
		const segs = rel.split(path.sep).filter(Boolean);
		segs.pop(); // drop content.yaml
		return segs.length > 0 ? segs : [path.basename(path.dirname(contentYamlAbsPath))];
	}

	// walk() processes a file's top-level nodes in order, following Include: inline
	// (matches how OpenRA splices Include'd nodes at their textual position),
	// accumulating Rules/Weapons/Sequences into the flat load-order arrays.
	function walk(nodes: YamlNode[], fileDir: string, seen: Set<string>, isRoot: boolean): void {
		for (const node of nodes) {
			if (node.key === "Include" && node.value) {
				const incPath = path.resolve(fileDir, node.value);
				if (seen.has(incPath)) continue;
				seen.add(incPath);
				const incNodes = parseFile(incPath, false);

				const factionRuleFiles: string[] = [];
				const before = ruleFiles.length;
				walk(incNodes, path.dirname(incPath), seen, false);
				factionRuleFiles.push(...ruleFiles.slice(before));

				factions.push({
					pathSegments: deriveFactionSegments(incPath),
					contentYamlPath: incPath,
					ruleFiles: factionRuleFiles,
				});
			} else if (node.key === "Rules") {
				const files = resolveFileList(node);
				ruleFiles.push(...files);
				if (isRoot) baseRuleFiles.push(...files);
			} else if (node.key === "Weapons") {
				weaponFiles.push(...resolveFileList(node));
			} else if (node.key === "Sequences") {
				sequenceFiles.push(...resolveFileList(node));
			}
		}
	}

	walk(rootNodes, modRoot, new Set([modYamlPath]), true);

	return { modRoot, aliasMap, ruleFiles, weaponFiles, sequenceFiles, factions, baseRuleFiles };
}
