"""Build the offline OpenMES documentation portal using only Python stdlib."""
from pathlib import Path
import re, json, html, hashlib, subprocess
ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path('/Users/seal/Desktop/myLog/program/SKhynix/OpenMes')
OUT = ROOT / 'portal'
CHAPTERS = json.loads((ROOT / 'developer/chapters.json').read_text())

def source_name(path):
    return hashlib.sha256(path.encode()).hexdigest()[:16] + '.html'

def inline(text):
    tokens = []
    def save(value):
        tokens.append(value)
        return f'\x00{len(tokens)-1}\x00'
    text = re.sub(r'`([^`]+)`', lambda m: save('<code>'+html.escape(m[1])+'</code>'), text)
    def link(m):
        label, url = m[1], m[2]
        if url.startswith(str(SOURCE)+'/'):
            path = url[len(str(SOURCE))+1:]
            match = re.search(r':(\d+)$', path)
            line = int(match[1]) if match else 1
            if match: path = path[:match.start()]
            url = 'sources/'+source_name(path)+f'#L{line}'
        return save(f'<a href="{html.escape(url, quote=True)}">{html.escape(label)}</a>')
    text = re.sub(r'\[([^\]]+)\]\(([^)]+)\)', link, text)
    text = html.escape(text)
    text = re.sub(r'\*\*(.*?)\*\*', r'<strong>\1</strong>', text)
    return re.sub(r'\x00(\d+)\x00', lambda m: tokens[int(m[1])], text)

def markdown(text):
    lines=text.splitlines(); out=[]; toc=[]; i=0; count=0
    while i<len(lines):
        s=lines[i]
        if not s.strip(): i+=1; continue
        if s.startswith('```'):
            lang=s[3:]; code=[]; i+=1
            while i<len(lines) and not lines[i].startswith('```'): code.append(lines[i]); i+=1
            out.append('<div class="code-block"><span>'+html.escape(lang or 'text')+'</span><pre><code>'+html.escape('\n'.join(code))+'</code></pre></div>'); i+=1; continue
        if s.startswith('#'):
            m=re.match(r'(#{1,6}) (.*)',s)
            if m:
                level=len(m[1]); count+=1; anchor=f'section-{count}'
                if level==2: toc.append((anchor,m[2]))
                out.append(f'<h{level} id="{anchor}">{inline(m[2])}</h{level}>'); i+=1; continue
        if s.startswith('|'):
            rows=[]
            while i<len(lines) and lines[i].startswith('|'):
                row=[x.strip() for x in lines[i].strip('|').split('|')]
                if not all(re.fullmatch(r':?-+:?',x) for x in row): rows.append(row)
                i+=1
            out.append('<div class="table-wrap"><table><thead><tr>'+''.join('<th>'+inline(x)+'</th>' for x in rows[0])+'</tr></thead><tbody>'+''.join('<tr>'+''.join('<td>'+inline(x)+'</td>' for x in row)+'</tr>' for row in rows[1:])+'</tbody></table></div>'); continue
        if re.match(r'(- |\d+\. )',s):
            ordered=not s.startswith('- '); tag='ol' if ordered else 'ul'; items=[]
            while i<len(lines) and re.match(r'(- |\d+\. )',lines[i]):
                items.append('<li>'+inline(re.sub(r'^(- |\d+\. )','',lines[i]))+'</li>'); i+=1
            out.append(f'<{tag}>'+''.join(items)+f'</{tag}>'); continue
        para=[s]; i+=1
        while i<len(lines) and lines[i].strip() and not re.match(r'(#|```|\||- |\d+\. )',lines[i]): para.append(lines[i]); i+=1
        out.append('<p>'+inline(' '.join(para))+'</p>')
    return '\n'.join(out),toc

def shell(title, content, active='home', toc=(), prevnext=''):
    nav='<a class="nav-home '+('active' if active=='home' else '')+'" href="index.html">⌂ <span>文档概览</span></a>'
    for i,c in enumerate(CHAPTERS):
        nav+=f'<a class="nav-item {"active" if active==c["id"] else ""}" href="{c["id"]}.html" {"aria-current=page" if active==c["id"] else ""}><span class="nav-num">{i+1:02}</span><span>{c["title"]}</span></a>'
    return f'''<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><meta name="description" content="OpenMES 开发文档：架构、生产逻辑与源码拆解"><title>{html.escape(title)} · OpenMES Atlas</title><link rel="stylesheet" href="assets/style.css"></head><body>
<a class="skip" href="#main">跳到正文</a><aside class="sidebar" id="sidebar"><a class="brand" href="index.html"><span class="brand-mark">M</span><span>OpenMES <b>ATLAS</b></span></a><div class="sidebar-caption">DEVELOPER KNOWLEDGE BASE</div><button class="search-trigger" type="button">⌕ 搜索文档 <kbd>/</kbd></button><nav aria-label="主导航">{nav}</nav><div class="sidebar-footer"><span class="status-dot"></span> 本地源码阅读基线<br><small>6048face · 2026.11.09</small><a href="../openmes-course/index.html">交互课程 ↗</a><a href="../README.md">Markdown 文档 ↗</a></div></aside>
<div class="workspace"><header class="topbar"><button id="menu-toggle" aria-expanded="false" aria-controls="sidebar" aria-label="展开导航">☰</button><div>工程知识库 <span>/</span> {html.escape(title)}</div><span class="top-tag">SOURCE VERIFIED · 静态核对</span><button id="theme-toggle" aria-label="切换明暗主题">◐</button></header><main id="main" tabindex="-1">{content}{prevnext}</main><footer class="page-footer">OpenMES / 从业务行为理解源码 <span>本地文档 · 不连接生产数据</span></footer></div>
{('<aside class="toc"><div>本页内容</div>'+''.join(f'<a href="#{a}">{html.escape(t)}</a>' for a,t in toc)+'</aside>') if toc else ''}
<dialog id="search-dialog"><div class="search-head"><label for="search-input">搜索文档</label><button id="search-close" aria-label="关闭搜索">×</button></div><input id="search-input" placeholder="工单、事务、MachineCounter…" autocomplete="off"><div id="search-count" role="status">输入关键词搜索全部章节</div><div id="search-results"></div></dialog><script src="assets/search-data.js"></script><script src="assets/app.js"></script></body></html>'''

OUT.mkdir(exist_ok=True); (OUT/'sources').mkdir(exist_ok=True)
search=[]; refs=set()
for i,c in enumerate(CHAPTERS):
    raw=(ROOT/'developer'/f'{c["id"]}.md').read_text()
    rendered,toc=markdown(raw)
    section_links = ''.join(
        f'<a href="#{anchor}">{html.escape(label)}</a>'
        for anchor, label in toc
    )
    section_nav = f'<div class="chapter-jump" role="navigation" aria-label="本章导航">{section_links}</div>'
    content = (
        f'<div class="article-meta">CHAPTER {i+1:02} <span>业务流程 · 实现契约 · 开发验收</span>'
        f'<a href="../developer/{c["id"]}.md">阅读 Markdown ↗</a></div>'
        f'{section_nav}<article class="prose">{rendered}</article>'
    )
    prevnext='<div class="page-nav">'
    if i: prevnext+=f'<a href="{CHAPTERS[i-1]["id"]}.html"><small>上一章</small>{CHAPTERS[i-1]["title"]}</a>'
    if i+1<len(CHAPTERS): prevnext+=f'<a href="{CHAPTERS[i+1]["id"]}.html"><small>下一章 →</small>{CHAPTERS[i+1]["title"]}</a>'
    prevnext+='</div>'
    (OUT/f'{c["id"]}.html').write_text(shell(c['title'],content,c['id'],toc,prevnext))
    search.append(dict(title=c['title'],url=c['id']+'.html',body=raw))
    for path,n in c['sources']: refs.add(path)
# Also include original-document directory links in an HTML reference page.
raw=(ROOT/'developer/source-docs.md').read_text(); rendered,toc=markdown(raw)
(OUT/'source-docs.html').write_text(shell('原仓库文档目录','<article class="prose">'+rendered+'</article>','sources',toc))
for p in (SOURCE/'docs').rglob('*.md'): refs.add(str(p.relative_to(SOURCE)))
files=[]
for path in sorted(refs):
    p=SOURCE/path; raw=p.read_text(); name=source_name(path)
    numbered=''.join(f'<div class="source-line" id="L{i}"><a href="#L{i}">{i}</a><code>{html.escape(line) or " "}</code></div>' for i,line in enumerate(raw.splitlines(),1))
    (OUT/'sources'/name).write_text(f'''<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{html.escape(path)} · 源码快照</title><link rel="stylesheet" href="../assets/style.css"></head><body class="source-body"><header class="source-header"><a href="../index.html">← 文档中心</a><h1>{html.escape(path)}</h1><p>源码阅读快照 · 2026-11-09 · 行号可直接引用 · SHA-256 {hashlib.sha256(p.read_bytes()).hexdigest()[:12]}</p></header><div class="source-code">{numbered}</div></body></html>''')
    files.append(dict(path=path,sha256=hashlib.sha256(p.read_bytes()).hexdigest(),lines=len(raw.splitlines())))
head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=SOURCE,text=True).strip()
status=subprocess.check_output(['git','status','--short'],cwd=SOURCE,text=True)
(ROOT/'developer/baseline.json').write_text(json.dumps(dict(source_root=str(SOURCE),review_date='2026-11-09',git_head=head,worktree_status=status,files=files),ensure_ascii=False,indent=2)+'\n')
(OUT/'assets/search-data.js').write_text('window.OPENMES_SEARCH='+json.dumps(search,ensure_ascii=False).replace('</','<\\/')+';\n')
cards=''.join(f'<a class="chapter-card" href="{c["id"]}.html"><div><span>{i+1:02}</span><span>↗</span></div><h3>{c["title"]}</h3><p>{c["summary"]}</p></a>' for i,c in enumerate(CHAPTERS))
home='''<section class="hero"><div class="eyebrow"><span class="status-dot"></span> OPENMES ENGINEERING ATLAS</div><h1>看清生产链，<br>读懂<span>每一层代码。</span></h1><p>从工单到工序，从设备读数到成品数量。<br>覆盖环境搭建、数据契约、实现步骤与验收排障，让业务行为与源码一一对应。</p><div class="hero-actions"><a class="button-primary" href="03-production.html">追踪生产主链 <span>→</span></a><a class="button-secondary" href="01-overview.html">先看项目地图 ↗</a></div><div class="hero-grid" aria-hidden="true"><div class="diagram-label">PRODUCTION CORE</div><div class="core-node">WorkOrder<small>计划 · 工艺快照</small></div><span class="connector">↓</span><div class="core-node">Batch<small>生产批 · 配置版本</small></div><span class="connector">↓</span><div class="core-node accented">BatchStep<small>执行 · 数量 · 状态</small></div><div class="diagram-note">Laravel → Eloquent → PostgreSQL</div></div></section>
<section class="metrics"><div><b>11</b><span>开发主题</span></div><div><b>3</b><span>核心领域层次</span></div><div><b>2</b><span>生产流转模式</span></div><div><b>01</b><span>源码事实基线</span></div></section>
<section class="flow-section"><div class="section-heading"><div><div class="eyebrow">FOLLOW THE REQUEST</div><h2>沿一次请求，找到规则所在</h2></div><a href="03-production.html">展开完整调用链 →</a></div><div class="flow-track"><a href="08-permissions-modules.html"><span>01 / ENTRY</span><h3>入口与授权</h3><p>Route · Request · Policy</p></a><a href="03-production.html"><span>02 / DOMAIN</span><h3>事务与业务规则</h3><p>BatchService · 行锁</p></a><a href="04-quantity.html"><span>03 / STATE</span><h3>数量与状态聚合</h3><p>Step → Batch → Order</p></a><a href="07-frontend-sync.html"><span>04 / VIEW</span><h3>提交后更新页面</h3><p>Reverb · Collection</p></a></div></section>
<section class="mode-panel"><div><div class="eyebrow">ONE BATCH, TWO FLOWS</div><h2>同一批生产，两种推进方式</h2><p>先分清计产时点，再理解工序状态。</p><div class="mode-buttons" role="group" aria-label="选择流转模式"><button data-mode="whole" aria-pressed="true">whole_batch</button><button data-mode="transfer" aria-pressed="false">transfer</button></div></div><div id="mode-content" aria-live="polite"></div></section>
<section class="chapter-section"><div class="section-heading"><div><div class="eyebrow">EXPLORE THE CODEBASE</div><h2>按问题进入源码</h2></div><span>架构与环境 → 业务契约 → 实现与验收</span></div><div class="chapter-grid">'''+cards+'''<a class="chapter-card reference-card" href="source-docs.html"><div><span>REF</span><span>↗</span></div><h3>原仓库文档目录</h3><p>角色指南、历史评审、协议说明与 RFC。区分背景材料与已核验行为。</p></a></div></section><div class="baseline-note"><strong>基于当前代码，而非旧架构描述。</strong><p>当前 Web 主入口是 React + Inertia。原开发文档中的 Blade / Livewire 描述存在滞后；所有章节附源码定位。本次为静态阅读，未执行 OpenMES 业务测试。</p><a href="11-evidence.html">查看来源与差异 →</a></div>'''
(OUT/'index.html').write_text(shell('文档概览',home))
print(f'Built {len(CHAPTERS)} chapters, {len(files)} source snapshots → {OUT}')
