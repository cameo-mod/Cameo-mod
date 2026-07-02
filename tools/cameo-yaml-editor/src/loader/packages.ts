import * as path from "path";
import { YamlNode } from "../model/types";
import { findChild } from "../parser/miniyaml";

// Builds the package-alias -> absolute folder map from mod.yaml's
// "FileSystem: Packages:" block, e.g. "$cameo: cameo" or "cameo|ContentPacks: ContentPacks".
// Good-enough approximation of OpenRA's FileSystem mounting for our purposes:
// we only need to resolve "pkg|relative/path" tokens found in Rules:/Weapons:/Sequences:.
export function buildPackageAliasMap(modYamlRoot: YamlNode[], modRoot: string, engineDir: string): Map<string, string> {
	const aliasMap = new Map<string, string>();
	aliasMap.set("EngineDir", engineDir);

	const fsNode = findChild(modYamlRoot, "FileSystem");
	const packagesNode = fsNode ? findChild(fsNode.children, "Packages") : undefined;
	if (!packagesNode) return aliasMap;

	for (const line of packagesNode.children) {
		let key = line.key;
		if (key.startsWith("~")) key = key.slice(1);

		const pipeIdx = key.indexOf("|");
		const sourcePart = pipeIdx === -1 ? key : key.slice(0, pipeIdx);
		const subpath = pipeIdx === -1 ? "" : key.slice(pipeIdx + 1);

		let basePath: string | undefined;
		if (sourcePart.startsWith("$")) {
			basePath = modRoot;
		} else if (sourcePart.startsWith("^")) {
			basePath = aliasMap.get(sourcePart.slice(1));
		} else {
			basePath = aliasMap.get(sourcePart);
		}
		if (basePath === undefined) continue; // unresolved external mount (engine/support dir) - skip

		const resolved = subpath ? path.join(basePath, subpath) : basePath;
		const alias = line.value ?? (subpath ? subpath.split("/").pop()! : sourcePart.replace(/^[$^]/, ""));
		aliasMap.set(alias, resolved);
	}

	return aliasMap;
}

// Resolves a "pkg|relative/path.yaml" token (as used in Rules:/Weapons:/Sequences: lists)
// to an absolute file path.
export function resolvePackagePath(token: string, aliasMap: Map<string, string>): string | undefined {
	const pipeIdx = token.indexOf("|");
	if (pipeIdx === -1) return undefined;
	const pkg = token.slice(0, pipeIdx);
	const rel = token.slice(pipeIdx + 1);
	const base = aliasMap.get(pkg);
	if (base === undefined) return undefined;
	return path.join(base, rel);
}
