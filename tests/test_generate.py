#!/usr/bin/env python3
import subprocess
import sys
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "shared" / "tools"))
import generate  # noqa: E402


class GenerateSharedResourcesTests(unittest.TestCase):
    def test_strings_parity_across_all_languages(self):
        strings = generate.load_strings()
        self.assertEqual(set(strings.keys()), {"ca", "es", "en"})
        base_keys = set(strings["en"].keys())
        self.assertGreater(len(base_keys), 100)
        for lang in ("ca", "es"):
            self.assertEqual(set(strings[lang].keys()), base_keys)

    def test_placeholder_arity_matches_across_languages_and_platforms(self):
        strings = generate.load_strings()
        for key in strings["en"]:
            for platform in ("mac", "win"):
                arities = {
                    lang: generate.arity(generate.resolve(strings[lang][key], platform))
                    for lang in generate.LANGS
                }
                self.assertEqual(
                    len(set(arities.values())),
                    1,
                    f"Arity mismatch for key '{key}' on platform '{platform}': {arities}",
                )

    def test_layouts_have_17_unique_entries(self):
        layouts = generate.load(ROOT / "shared" / "layouts.json")["layouts"]
        self.assertEqual(len(layouts), 17)
        klids = [item["klid"] for item in layouts]
        dlls = [item["dllName"] for item in layouts]
        self.assertEqual(len(set(klids)), 17)
        self.assertEqual(len(set(dlls)), 17)

    def test_generated_xaml_is_well_formed_xml(self):
        tokens = generate.load(ROOT / "shared" / "design" / "tokens.json")
        xaml_text = generate.gen_xaml_theme(tokens)
        root = ET.fromstring(xaml_text)
        self.assertTrue(root.tag.endswith("ResourceDictionary"))

    def test_windows_xml_and_xaml_files_are_well_formed_xml(self):
        patterns = ("*.xaml", "*.wxs", "*.csproj", "*.wixproj", "*.manifest")
        for pattern in patterns:
            for xml_path in (ROOT / "windows").rglob(pattern):
                with self.subTest(xml=str(xml_path.relative_to(ROOT))):
                    ET.parse(xml_path)

    def test_fallback_language_is_english(self):
        self.assertEqual(generate.FALLBACK_LANG, "en")

    def test_cli_check_passes(self):
        proc = subprocess.run(
            [sys.executable, str(ROOT / "shared" / "tools" / "generate.py"), "--check"],
            cwd=str(ROOT),
            capture_output=True,
            text=True,
        )
        self.assertEqual(proc.returncode, 0, msg=proc.stderr + proc.stdout)


if __name__ == "__main__":
    unittest.main()
