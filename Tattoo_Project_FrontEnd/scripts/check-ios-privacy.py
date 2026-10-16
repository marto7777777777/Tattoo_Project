"""Run before archive and after cap sync; needs Python 3, not Xcode."""
import plistlib
import re
from pathlib import Path
import xml.etree.ElementTree as ET
root = Path(__file__).resolve().parents[1]
path = root / 'ios/App/App/Info.plist'
plist = plistlib.loads(path.read_bytes())
keys = [n.text for n in ET.parse(path).getroot().find('dict') if n.tag == 'key']
assert len(keys) == len(set(keys)), 'Duplicate plist keys'
for key in ('NSCameraUsageDescription', 'NSPhotoLibraryUsageDescription'):
    assert isinstance(plist.get(key), str) and plist[key].strip(), key
assert 'NSPhotoLibraryAddUsageDescription' not in plist, 'No native Photos write API is used'
project = (root / 'ios/App/App.xcodeproj/project.pbxproj').read_text()
assert re.findall(r'INFOPLIST_FILE = ([^;]+);', project) == ['App/Info.plist'] * 2
assert re.findall(r'CURRENT_PROJECT_VERSION = ([^;]+);', project) == ['5'] * 2
assert re.findall(r'MARKETING_VERSION = ([^;]+);', project) == ['1.0'] * 2
print('PASS: canonical privacy plist, unique keys, Build 5 / version 1.0')
