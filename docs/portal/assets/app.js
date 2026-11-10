(() => {
  const $ = s => document.querySelector(s);
  try { document.documentElement.dataset.theme = localStorage.getItem('openmes-docs-theme') || 'light'; } catch {}
  $('#theme-toggle')?.addEventListener('click', () => {
    const theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
    document.documentElement.dataset.theme = theme;
    try { localStorage.setItem('openmes-docs-theme', theme); } catch {}
  });
  $('#menu-toggle')?.addEventListener('click', () => {
    const open = document.body.classList.toggle('menu-open');
    $('#menu-toggle').setAttribute('aria-expanded', String(open));
  });
  const dialog = $('#search-dialog'), input = $('#search-input');
  const openSearch = () => { if (!dialog.open) dialog.showModal(); input.focus(); };
  document.querySelectorAll('.search-trigger').forEach(b => b.addEventListener('click', openSearch));
  $('#search-close')?.addEventListener('click', () => dialog.close());
  dialog?.addEventListener('click', e => { if (e.target === dialog) { const r = dialog.getBoundingClientRect(); if(e.clientX < r.left || e.clientX > r.right || e.clientY < r.top || e.clientY > r.bottom) dialog.close(); } });
  document.addEventListener('keydown', e => {
    if (e.key === '/' && !/INPUT|TEXTAREA|SELECT/.test(e.target.tagName) && !e.target.isContentEditable) { e.preventDefault(); openSearch(); }
  });
  input?.addEventListener('input', () => {
    const query = input.value.trim().toLocaleLowerCase();
    const tokens = query.split(/\s+/).filter(Boolean);
    const results = tokens.length ? (window.OPENMES_SEARCH || []).filter(c => tokens.every(t => (c.title+' '+c.body).toLocaleLowerCase().includes(t))) : [];
    $('#search-count').textContent = tokens.length ? `${results.length} 个匹配章节` : '输入关键词搜索全部章节';
    $('#search-results').replaceChildren();
    for (const c of results) {
      const a = document.createElement('a'); a.href = c.url;
      const title = document.createElement('strong'); title.textContent = c.title;
      const excerpt = document.createElement('p');
      const pos = c.body.toLocaleLowerCase().indexOf(tokens[0]);
      excerpt.textContent = '…'+c.body.slice(Math.max(0,pos-30),Math.max(0,pos-30)+150).replace(/[#`|]/g,'')+'…';
      a.append(title,excerpt); $('#search-results').append(a);
    }
    if (tokens.length && !results.length) { const p = document.createElement('p'); p.className='empty-search'; p.textContent='没有找到相关章节。试试“工序”“库存”或服务类名。'; $('#search-results').append(p); }
  });
  const modes = {
    whole: ['整批完成，再推进下游','前序完成 → 下游就绪 → 完批结算','工单汇总已完成批次的产量。步骤完工时转出剩余量，最终产量取完批提交值或目标量。'],
    transfer: ['数量先流转，工序可重叠','上游报数 → 下游获得输入 → 末工序实时计产','例如第一道通过 60，第二道通过 55，成品是 55。工序关闭还要满足无待处理量与上游输入关闭等条件。']
  };
  function setMode(mode) {
    const el=$('#mode-content'); if(!el) return;
    el.replaceChildren();
    for (const [i,text] of modes[mode].entries()) { const p=document.createElement(i===0?'h3':'p'); p.textContent=text; if(i===1)p.className='mode-chain'; el.append(p); }
    const a=document.createElement('a'); a.href='04-quantity.html'; a.textContent='阅读数量账本 →'; el.append(a);
    document.querySelectorAll('[data-mode]').forEach(b=>b.setAttribute('aria-pressed',String(b.dataset.mode===mode)));
  }
  document.querySelectorAll('[data-mode]').forEach(b=>b.addEventListener('click',()=>setMode(b.dataset.mode)));
  setMode('whole');
})();
