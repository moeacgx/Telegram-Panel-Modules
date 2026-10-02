"""维护模块目录；仅处理指定的本地包和目录，不读取凭据或发起网络请求。"""
import argparse
import hashlib
import json
import re
import zipfile
from pathlib import Path
from urllib.parse import urlsplit

ROOT = Path(__file__).resolve().parent.parent


def validate(index):
    assert index.get('schemaVersion') == 1, 'schemaVersion 必须为 1'
    modules = index.get('modules')
    assert isinstance(modules, list) and len(modules) <= 1000, 'modules 必须为不超过 1000 项的数组'
    seen = set()
    for module in modules:
        assert re.fullmatch(r'[a-zA-Z0-9][a-zA-Z0-9._-]{0,99}', module['id']), '模块 ID 无效'
        assert re.fullmatch(r'\d+\.\d+\.\d+', module['version']), '版本须为 x.y.z'
        assert 0 < len(module['name']) <= 100, '名称无效'
        assert len(module.get('description', '')) <= 2000, '描述过长'
        assert re.fullmatch(r'[0-9a-fA-F]{64}', module['sha256']), 'SHA-256 无效'
        url = urlsplit(module['downloadUrl'])
        assert url.scheme == 'https' and url.hostname and url.port in (None, 443) and not url.username and not url.password and not url.fragment, '下载地址必须为 HTTPS 443 且不能含凭据或片段'
        key = (module['id'].lower(), module['version'])
        assert key not in seen, '模块 ID/版本重复'
        seen.add(key)
    assert len(json.dumps(index, ensure_ascii=False).encode()) <= 2 * 1024 * 1024, '目录不能超过 2MB'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    commands.add_parser('validate')
    add = commands.add_parser('add')
    add.add_argument('package', type=Path)
    add.add_argument('download_url')
    args = parser.parse_args()
    path = ROOT / 'index.json'
    index = json.loads(path.read_text(encoding='utf-8-sig'))
    if args.command == 'add':
        assert args.package.stat().st_size <= 50 * 1024 * 1024, '包不能超过 50MB'
        data = args.package.read_bytes()
        with zipfile.ZipFile(args.package) as archive:
            assert len(archive.infolist()) <= 5000, '包条目超过限制'
            assert sum(item.file_size for item in archive.infolist()) <= 200 * 1024 * 1024, '包解压后超过 200MB'
            manifest = json.loads(archive.read('manifest.json'))
        index['modules'].append({
            'id': manifest['id'], 'name': manifest['name'], 'version': manifest['version'],
            'downloadUrl': args.download_url, 'sha256': hashlib.sha256(data).hexdigest(),
        })
    validate(index)
    if args.command == 'add':
        path.write_text(json.dumps(index, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f"validated: {len(index['modules'])} modules")


if __name__ == '__main__':
    main()
