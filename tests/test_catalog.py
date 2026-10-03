"""校验真实发布包身份及目录拒绝路径，不进行网络发布。"""
import importlib.util
import json
import subprocess
import sys
import tempfile
import unittest
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('catalog', ROOT / 'tools/catalog.py')
catalog = importlib.util.module_from_spec(spec)
spec.loader.exec_module(catalog)


class CatalogTests(unittest.TestCase):
    def index(self):
        return {'schemaVersion': 1, 'modules': [{
            'id': 'example.module', 'name': '示例', 'version': '1.0.0',
            'downloadUrl': 'https://api.github.com/repos/example/modules/releases/assets/1',
            'sha256': 'a' * 64}]}

    def test_valid_catalog(self):
        catalog.validate(self.index())

    def test_duplicate_identity_rejected(self):
        index = self.index()
        index['modules'].append(dict(index['modules'][0], id='EXAMPLE.module'))
        with self.assertRaises(AssertionError):
            catalog.validate(index)

    def test_credentials_and_insecure_urls_rejected(self):
        for url in ['http://example.com/a.tpm', 'https://user:secret@example.com/a.tpm',
                    'https://example.com:8443/a.tpm', 'https://example.com/a.tpm#token']:
            with self.subTest(url=url):
                index = self.index()
                index['modules'][0]['downloadUrl'] = url
                with self.assertRaises(AssertionError):
                    catalog.validate(index)

    def test_invalid_hash_rejected(self):
        index = self.index()
        index['modules'][0]['sha256'] = 'not-a-hash'
        with self.assertRaises(AssertionError):
            catalog.validate(index)

    def test_add_uses_package_identity_and_duplicate_does_not_write(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / 'tools').mkdir()
            script = root / 'tools/catalog.py'
            script.write_bytes((ROOT / 'tools/catalog.py').read_bytes())
            index = root / 'index.json'
            index.write_text('{"schemaVersion": 1, "modules": []}', encoding='utf-8')
            package = root / 'misleading-file-name.tpm'
            with zipfile.ZipFile(package, 'w') as archive:
                archive.writestr('manifest.json', json.dumps({
                    'id': 'example.actual', 'name': '实际包', 'version': '2.0.0'}))
            command = [sys.executable, str(script), 'add', str(package),
                       'https://api.github.com/repos/example/modules/releases/assets/2']
            result = subprocess.run(command, capture_output=True, timeout=10)
            self.assertEqual(result.returncode, 0, result.stderr)
            before = index.read_bytes()
            item = json.loads(before)['modules'][0]
            self.assertEqual((item['id'], item['version']), ('example.actual', '2.0.0'))
            import hashlib
            self.assertEqual(item['sha256'], hashlib.sha256(package.read_bytes()).hexdigest())
            result = subprocess.run(command, capture_output=True, timeout=10)
            self.assertNotEqual(result.returncode, 0)
            self.assertEqual(index.read_bytes(), before)


if __name__ == '__main__':
    unittest.main()
