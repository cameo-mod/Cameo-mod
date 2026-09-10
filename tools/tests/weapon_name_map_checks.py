"""Shared bounded map scan for weapon-identity regressions."""
import re
import zipfile


def assert_no_old_weapon_names(test, root, names):
    names = tuple(names)
    test.assertTrue(names, "Weapon-name inventory must not be empty")
    pattern = re.compile(r'(?i)(?<![A-Za-z0-9_])(?:' + '|'.join(re.escape(n) for n in names) + r')(?![A-Za-z0-9_])')
    directory = root / 'mods/cameo/maps'
    maps = list(directory.rglob('*.oramap'))
    test.assertTrue(maps, "Expected bundled map archives")
    for path in maps:
        with zipfile.ZipFile(path) as archive:
            for entry in archive.infolist():
                if entry.filename.lower().endswith(('.yaml', '.lua')):
                    test.assertLessEqual(entry.file_size, 10_000_000)
                    test.assertIsNone(pattern.search(archive.read(entry).decode('utf-8-sig')), str(path))
    for path in directory.rglob('*'):
        if path.is_file() and path.suffix.lower() in ('.yaml', '.lua'):
            test.assertIsNone(pattern.search(path.read_text(encoding='utf-8-sig')), str(path))
