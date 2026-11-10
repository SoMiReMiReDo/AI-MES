"""Check generated local links, anchors, source evidence, and chapter parity."""
from pathlib import Path
from html.parser import HTMLParser
from urllib.parse import urlsplit, unquote
import hashlib,json,re
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'portal'
class Page(HTMLParser):
    def __init__(self,text):
        super().__init__();self.refs=[];self.ids=set();self.feed(text)
    def handle_starttag(self,tag,attrs):
        attrs=dict(attrs)
        if 'id' in attrs:self.ids.add(attrs['id'])
        for name in ['href','src']:
            if name in attrs:self.refs.append(attrs[name])
pages={p.resolve():Page(p.read_text()) for p in OUT.rglob('*.html')}
errors=[];links=0
for p,page in pages.items():
    for ref in page.refs:
        u=urlsplit(ref)
        if u.scheme or u.netloc:continue
        target=(p.parent/unquote(u.path)).resolve() if u.path else p
        links+=1
        if not target.exists():errors.append(f'{p.name}: missing {ref}')
        elif u.fragment and target in pages and unquote(u.fragment) not in pages[target].ids:errors.append(f'{p.name}: missing anchor {ref}')
chapters=json.loads((ROOT/'developer/chapters.json').read_text())
for c in chapters:
    assert (OUT/(c['id']+'.html')).exists()
    for rel,line in c['sources']:
        source=Path('/Users/seal/Desktop/myLog/program/SKhynix/OpenMes')/rel
        if not 0<line<=len(source.read_text().splitlines()):errors.append(f'Invalid line {rel}:{line}')
baseline=json.loads((ROOT/'developer/baseline.json').read_text())
for f in baseline['files']:
    path=Path(baseline['source_root'])/f['path']
    if hashlib.sha256(path.read_bytes()).hexdigest()!=f['sha256']:errors.append(f'Stale source: {f["path"]}')
assert not errors,'\n'.join(errors)
print(f'PASS: {len(pages)} HTML pages, {links} local links/anchors, {len(chapters)} chapters, {len(baseline["files"])} source hashes')
