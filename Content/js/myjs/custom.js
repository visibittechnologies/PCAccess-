function openDetailsModal(o, c, d, b) { const m = document.getElementById('detailsModal'), x = m.querySelector('.transform'); document.getElementById('modalOverdues').textContent = o; document.getElementById('modalComment').textContent = c; document.getElementById('modalEntryDate').textContent = d; document.getElementById('modalEntryBy').textContent = b; m.classList.remove('hidden'); m.classList.add('flex'); setTimeout(() => { x.classList.remove('scale-95', 'opacity-0'); x.classList.add('scale-100', 'opacity-100') }, 10) }
function closeDetailsModal() { const m = document.getElementById('detailsModal'), x = m.querySelector('.transform'); x.classList.remove('scale-100', 'opacity-100'); x.classList.add('scale-95', 'opacity-0'); setTimeout(() => { m.classList.add('hidden'); m.classList.remove('flex') }, 300) }
function openMessageModal(n, p, s) { const x = document.getElementById('messageModal'), b = x.querySelector('.transform'); document.getElementById('msgCustomerName').textContent = n; document.getElementById('msgCustomerMob').textContent = p; document.getElementById('msgContent').textContent = s; x.classList.remove('hidden'); x.classList.add('flex'); setTimeout(() => { b.classList.remove('scale-95', 'opacity-0'); b.classList.add('scale-100', 'opacity-100') }, 10) }
function closeMessageModal() { const x = document.getElementById('messageModal'), b = x.querySelector('.transform'); b.classList.remove('scale-100', 'opacity-100'); b.classList.add('scale-95', 'opacity-0'); setTimeout(() => { x.classList.add('hidden'); x.classList.remove('flex') }, 300) }
document.addEventListener('DOMContentLoaded', function () { const b = document.getElementById('rec-by-toggle'), a = document.getElementById('agent-drawer'), c = document.getElementById('conversation-drawer'), o = document.getElementById('drawer-overlay'), ca = document.getElementById('close-drawer'), cc = document.getElementById('close-conv-drawer'), cn = document.getElementById('cancel-conv-drawer'), s = document.getElementById('agent-search'), v = document.querySelectorAll('.view-conversation-btn'); function op(d) { d.classList.remove('translate-x-full'); o.classList.remove('hidden'); setTimeout(() => o.classList.add('opacity-100'), 10) } function cl() { if (a) a.classList.add('translate-x-full'); if (c) c.classList.add('translate-x-full'); if (o) { o.classList.remove('opacity-100'); setTimeout(() => o.classList.add('hidden'), 300) } if (b) b.checked = false } if (b) b.addEventListener('change', function () { if (this.checked) op(a); else cl() }); v.forEach(n => n.addEventListener('click', () => op(c))); if (ca) ca.addEventListener('click', cl); if (cc) cc.addEventListener('click', cl); if (cn) cn.addEventListener('click', cl); if (o) o.addEventListener('click', cl); if (s) s.addEventListener('input', e => { const t = e.target.value.toLowerCase(), g = document.querySelectorAll('.agent-group'); g.forEach(r => { const i = r.querySelectorAll('.agent-item'); let h = false; i.forEach(m => { const n = m.querySelector('.agent-name').textContent.toLowerCase(); if (n.includes(t)) { m.style.display = 'flex'; h = true } else m.style.display = 'none' }); r.style.display = h ? 'block' : 'none' }) }) });



/*============= CustomerDetails Page me Image Preview Js=============*/

function openPreview(element) {
    const modal = document.getElementById('imagePreviewModal');
    const overlay = document.getElementById('modalOverlay');
    const content = document.getElementById('modalContent');
    const img = element.querySelector('img');
    const previewImg = document.getElementById('previewImage');
    const placeholder = document.getElementById('previewPlaceholder');

    modal.classList.remove('hidden');
    modal.classList.add('flex');

    if (img && img.src) {
        previewImg.src = img.src;
        previewImg.classList.remove('hidden');
        placeholder.classList.add('hidden');
    }
    
    // Set dynamic name
    const customerName = document.getElementById('cdFullName').textContent || "Customer";
    const previewName = document.getElementById('cdPreviewName');
    if (previewName) {
        previewName.textContent = customerName;
    }

    setTimeout(() => {
        overlay.classList.remove('opacity-0');
        overlay.classList.add('opacity-100');
        content.classList.remove('scale-90', 'opacity-0');
        content.classList.add('scale-100', 'opacity-100');
    }, 10);
}

function closePreview() {
    const modal = document.getElementById('imagePreviewModal');
    const overlay = document.getElementById('modalOverlay');
    const content = document.getElementById('modalContent');

    overlay.classList.remove('opacity-100');
    overlay.classList.add('opacity-0');
    content.classList.remove('scale-100', 'opacity-100');
    content.classList.add('scale-90', 'opacity-0');

    setTimeout(() => {
        modal.classList.add('hidden');
        modal.classList.remove('flex');
    }, 300);
}

const modalOverlay = document.getElementById('modalOverlay');
if (modalOverlay) {
    modalOverlay.addEventListener('click', closePreview);
}




/*=========== Pay Emi Button Click Section JS================*/


function openEmiDrawer() {
    document.getElementById('emiDrawer').classList.remove('translate-x-full');
    document.getElementById('emiOverlay').classList.remove('hidden');
}

function closeEmiDrawer() {
    document.getElementById('emiDrawer').classList.add('translate-x-full');
    document.getElementById('emiOverlay').classList.add('hidden');
}
/*=========== Conversation Drawer Section JS================*/
function openConversationDrawer() {
    document.getElementById('conversationDrawer').classList.remove('translate-x-full');
    document.getElementById('conversationOverlay').classList.remove('hidden');
}
function closeConversationDrawer() {
    document.getElementById('conversationDrawer').classList.add('translate-x-full');
    document.getElementById('conversationOverlay').classList.add('hidden');
}