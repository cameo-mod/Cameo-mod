import importlib.util
from pathlib import Path

import pytest


SCRIPT = Path(__file__).resolve().parents[1] / "ai" / "generate_cameo_dev_manifest.py"
spec = importlib.util.spec_from_file_location("generate_cameo_dev_manifest", SCRIPT)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


def make_release(tmp_path, include="ContentPacks/Core/content.yaml"):
    (tmp_path / "mod.yaml").write_text(
        "FileSystem:\n\tSystemPackages:\n\t\t$cameo: cameo\n"
        f"Include: {include}\n"
        "Rules:\n\tcameo|rules/world.yaml\n"
        "Assemblies: OpenRA.Mods.Cameo.Unified.dll, OpenRA.Mods.CA.dll\n",
        encoding="utf-8",
    )
    path = tmp_path / "ContentPacks/Core/content.yaml"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("Rules:\n\tContentPacks|Core/yaml/faction.yaml\n", encoding="utf-8")


def test_includes_are_expanded_before_dev_mounts(tmp_path):
    make_release(tmp_path)
    generated = module.generate(tmp_path)
    assert "Include:" not in generated
    assert "ContentPacks|Core/yaml/faction.yaml" in generated
    assert "^EngineDir|mods/cameo: cameo" in generated
    assert "cameo|rules/dev/logging.yaml" in generated
    assert "OpenRA.Mods.Cameo.Dev.dll" in generated


def test_missing_or_escaping_include_fails_closed(tmp_path):
    make_release(tmp_path, "../outside.yaml")
    with pytest.raises(ValueError):
        module.generate(tmp_path)
    make_release(tmp_path, "ContentPacks/missing.yaml")
    with pytest.raises(FileNotFoundError):
        module.generate(tmp_path)


def test_nested_include_requires_review(tmp_path):
    make_release(tmp_path)
    (tmp_path / "ContentPacks/Core/content.yaml").write_text("Include: nested.yaml\n", encoding="utf-8")
    with pytest.raises(ValueError):
        module.generate(tmp_path)
