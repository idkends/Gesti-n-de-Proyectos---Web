/* ================================================================
   Gestión Monster — app.js
   ================================================================ */

document.addEventListener('DOMContentLoaded', () => {

  /* ── 1. Spotlight on module-grid cards ─────────────────────── */
  document.querySelectorAll('.module-grid').forEach(grid => {
    grid.addEventListener('mousemove', e => {
      grid.querySelectorAll('.module-card').forEach(card => {
        const r = card.getBoundingClientRect();
        card.style.setProperty('--mx', `${e.clientX - r.left}px`);
        card.style.setProperty('--my', `${e.clientY - r.top}px`);
      });
    });
  });

  /* ── 2. Spotlight on regular cards ────────────────────────── */
  document.querySelectorAll('.card').forEach(card => {
    card.addEventListener('mousemove', e => {
      const r = card.getBoundingClientRect();
      card.style.setProperty('--mx', `${e.clientX - r.left}px`);
      card.style.setProperty('--my', `${e.clientY - r.top}px`);
    });
  });

  /* ── 3. Stagger: module-cards ──────────────────────────────── */
  document.querySelectorAll('.module-card').forEach((card, i) => {
    card.style.setProperty('--card-index', i);
  });

  /* ── 4. Stagger: table rows ────────────────────────────────── */
  document.querySelectorAll('.data-table tbody tr').forEach((tr, i) => {
    tr.style.setProperty('--row-index', i);
  });

  /* ── 5. Auto-dismiss alerts (exit via transform+opacity only) ─ */
  document.querySelectorAll('.alert').forEach(el => {
    setTimeout(() => {
      el.style.transition = 'opacity 260ms cubic-bezier(0.23,1,0.32,1), transform 260ms cubic-bezier(0.23,1,0.32,1)';
      el.style.opacity    = '0';
      el.style.transform  = 'translateY(-6px)';
      setTimeout(() => el.remove(), 270);
    }, 4500);
  });

  /* ── 6. Active nav link (sidebar) ─────────────────────────── */
  const parts  = location.pathname.toLowerCase().split('/').filter(Boolean);
  const ctrl   = parts[0] || '';
  const action = parts[1] || '';

  let activated = false;
  const navLinks = document.querySelectorAll('.nav-item, .side-nav > a');

  navLinks.forEach(a => {
    if (activated) return;
    const hParts  = (a.getAttribute('href') || '').toLowerCase().split('/').filter(Boolean);
    const hCtrl   = hParts[0] || '';
    const hAction = hParts[1] || '';
    if (hCtrl && hCtrl === ctrl && hAction && hAction === action) {
      a.classList.add('active'); activated = true;
    }
  });

  if (!activated) {
    navLinks.forEach(a => {
      if (activated) return;
      const hParts  = (a.getAttribute('href') || '').toLowerCase().split('/').filter(Boolean);
      const hCtrl   = hParts[0] || '';
      const hAction = hParts[1] || '';
      if (hCtrl && hCtrl === ctrl && !hAction && !action) {
        a.classList.add('active'); activated = true;
      }
    });
  }

  /* ── 7. Dept ↔ Cargo sync ──────────────────────────────────── */
  const selCargo = document.getElementById('selCargo');
  if (selCargo) {
    selCargo.addEventListener('change', function () {
      const dep    = this.options[this.selectedIndex]?.dataset?.dep;
      const selDep = document.getElementById('selDep');
      if (dep && selDep) selDep.value = dep;
    });
  }

  /* ── 8. Mobile sidebar ─────────────────────────────────────── */
  const sidebar   = document.getElementById('sidebar');
  const backdrop  = document.getElementById('sidebarBackdrop');
  const hamburger = document.getElementById('hamburgerBtn');
  const closeBtn  = document.getElementById('sidebarClose');

  function openSidebar() {
    sidebar?.classList.add('open');
    backdrop?.classList.add('visible');
    hamburger?.classList.add('open');
    document.body.style.overflow = 'hidden';
  }

  function closeSidebar() {
    sidebar?.classList.remove('open');
    backdrop?.classList.remove('visible');
    hamburger?.classList.remove('open');
    document.body.style.overflow = '';
  }

  hamburger?.addEventListener('click', openSidebar);
  closeBtn?.addEventListener('click',  closeSidebar);
  backdrop?.addEventListener('click',  closeSidebar);

  document.addEventListener('keydown', e => {
    if (e.key === 'Escape') closeSidebar();
  });

  document.querySelectorAll('.nav-item, .side-nav > a').forEach(link => {
    link.addEventListener('click', () => {
      if (window.innerWidth <= 768) closeSidebar();
    });
  });

  /* ── 9. Magnetic effect on primary buttons (MOTION_INTENSITY=6) */
  const isTouchDevice = window.matchMedia('(hover: none)').matches;
  if (!isTouchDevice) {
    document.querySelectorAll('.btn-primary').forEach(btn => {
      let rafId = null;
      let targetX = 0, targetY = 0;
      let currentX = 0, currentY = 0;

      function animate() {
        currentX += (targetX - currentX) * 0.18;
        currentY += (targetY - currentY) * 0.18;
        btn.style.transform = `translate3d(${currentX}px, ${currentY}px, 0)`;
        if (Math.abs(targetX - currentX) > 0.05 || Math.abs(targetY - currentY) > 0.05) {
          rafId = requestAnimationFrame(animate);
        } else {
          rafId = null;
        }
      }

      btn.addEventListener('mousemove', e => {
        const r = btn.getBoundingClientRect();
        const cx = r.left + r.width  / 2;
        const cy = r.top  + r.height / 2;
        targetX = (e.clientX - cx) * 0.28;
        targetY = (e.clientY - cy) * 0.28;
        if (!rafId) rafId = requestAnimationFrame(animate);
      });

      btn.addEventListener('mouseleave', () => {
        targetX = 0; targetY = 0;
        if (!rafId) rafId = requestAnimationFrame(animate);
      });
    });
  }

  /* ── 10. Focus-visible polish on inputs ────────────────────── */
  document.querySelectorAll('input, select, textarea').forEach(el => {
    el.addEventListener('focus', () => el.parentElement?.classList.add('field--focused'));
    el.addEventListener('blur',  () => el.parentElement?.classList.remove('field--focused'));
  });

});

/* ── Global: Dept sync for Editar.cshtml ─────────────────────── */
function sincronizarDepto(sel) {
  const dep    = sel.options[sel.selectedIndex]?.dataset?.dep;
  const selDep = document.getElementById('selDep');
  if (dep && selDep) selDep.value = dep;
}

/* ── TreeView ─────────────────────────────────────────────────── */
function toggle(btn) {
  const children = btn.nextElementSibling;
  if (!children) return;
  const collapsed = children.classList.toggle('collapsed');
  btn.classList.toggle('collapsed', collapsed);
}

function expandirTodo() {
  document.querySelectorAll('.tree-children').forEach(ul  => ul.classList.remove('collapsed'));
  document.querySelectorAll('.tree-toggle').forEach(btn   => btn.classList.remove('collapsed'));
}

function contraerTodo() {
  document.querySelectorAll('.tree-children').forEach(ul  => ul.classList.add('collapsed'));
  document.querySelectorAll('.tree-toggle').forEach(btn   => btn.classList.add('collapsed'));
}

function filtrarArbol(texto) {
  const q = texto.trim().toLowerCase();
  if (!q) {
    document.querySelectorAll('.tree-leaf-item').forEach(li => li.style.display = '');
    expandirTodo();
    return;
  }
  document.querySelectorAll('.tree-leaf-item').forEach(li => {
    const nombre = li.dataset.nombre || '';
    const codigo = li.dataset.codigo || '';
    li.style.display = (nombre.indexOf(q) !== -1 || codigo.indexOf(q) !== -1) ? '' : 'none';
  });
  expandirTodo();
}
