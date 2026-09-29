/**
 * Create Task Initialization and Logic
 * Handles both standalone page and drawer behavior.
 */

// --- Global Data & State ---
window.createTask_availableUsers = [
    { id: 1, name: "Alex Johnston", email: "alex.j@example.com", role: "Project Lead", avatar: "https://lh3.googleusercontent.com/aida-public/AB6AXuDYfYiS4QfKuWXUbF0HAyay8pMY1uC6mk4cy9WD-iWcbg4RIBq9qExWwiL3uGo5YpcRCC2MHEKQ_wytN6Nz0MX-NxTcQbCLpQ8zEthb67rR3uHjzUqfsV9ojGDjdplYZkdMiPZRj6i9af6vqHI1Y6j-FjkGZMfACfptegpV2O98HBjzHOsBN1lFneWpqrsy4Xw__5dg9JBb75t3fRWfr7plveHsCbcyKBv3i53BnVN38Ia4wuoVafwBq0DLSmjvG-p9HJTc4UYUUcI8" },
    { id: 2, name: "Sarah Smith", email: "sarah.s@example.com", role: "UI Designer", avatar: "https://lh3.googleusercontent.com/aida-public/AB6AXuDj3uBaa2F8uD4VCM0QFaGJ8y98zlRPgUssfUxGmduM-fdDvP2jU2pav4NvKkiVbnpF4RgfGeeK34_aPwCl0x8mjw56nM7yOxVq4sj6FGSlqaloCyWh6O200wjov3nfM_n1X6So1jhZu4wM6pvWMRB0Hmc0a2wHfuZvL9DY6VHlg1ntcu9IIcjICnsIJ5RZ6s7Td13qHoaGvXx5yvv3e1cNC5hMA8MVYzoe_6a5qo7X3GYoEWKu9eATdUHm7JBlBp8tFaQPElT9NrU" },
    { id: 3, name: "Maria Garcia", email: "maria.g@example.com", role: "Frontend Dev", avatar: "https://lh3.googleusercontent.com/aida-public/AB6AXuAjHTnSDXo6-2aK0-NIS_lCOJLV2Ng7-EHvHNAau1QFB1xRzxC4_3-2g-BbphMUcaSVbgWoXdP_LW_UKbpyjv-CLdk5AKP2x1e_dweAvS2nJHsVhePhPgyTr_vp9Wm8BgXKD69qsJ2Zu5jAtrCL2VHerqjfKdsskqyAN0rbDMkzZlBDzbEsvD-9jAgqh5X1D2bcv-jalSJR2w-XoiGkAl6grdryxR7pcYsz4cj3BcO0IIBkPlflxWM5Icu346L75sruUY4nE4keBBI" },
    { id: 4, name: "James Wilson", email: "james.w@example.com", role: "Back-end Dev", avatar: "https://lh3.googleusercontent.com/aida-public/AB6AXuAT07UisQswunrFFOnHWK6QqlW790gOBMSHH9f6cbUDOIWUDqf_NDWN3Sk9GAEgZ-j-nFPD4YtS5XJWKu8_XEkn26jQj2SnzYVydKkby3mS-n-o0BaID7QSeVL6D9sG2Rp-g7AHNG1fGypo2xeBuM5rRVqAordTg_szQSwo5fxz2Q6blZfqDlrNPb_eIkSodyf9ogIgoeE3udigzxSm9JOpeJXdvT0WE93yxioXFMZ1Jqi_THP6RMjWHI1hUY3R1DDWL58tvw5z6Jo" }
];

window.checklistItems = [
    { id: 1, text: "Design new logos", completed: true, assigneeId: 1, attachments: [] },
    { id: 2, text: "Prepare documentation", completed: false, assigneeId: 2, attachments: [] },
    { id: 3, text: "Client meeting", completed: false, assigneeId: 3, attachments: [] },
    { id: 4, text: "Launch website", completed: false, assigneeId: null, attachments: [] }
];

window.createTask_quickAddAssigneeId = null;
window.createTask_quickAddAttachments = [];
window.createTask_selectedMembers = {
    manager: [],
    assignee: [],
    contributor: []
};
window.createTask_currentRole = null;
let itemFileInput = null; // Will be initialized in initCreateTask

// --- Global Functions (Exposed for HTML onclick handlers) ---

window.createTask_renderChecklist = function () {
    const container = document.getElementById('checklistContainer');
    if (!container) return;

    container.innerHTML = window.checklistItems.map(item => `
        <div id="item-${item.id}" class="checklist-row flex items-start justify-between group py-4 border-b border-transparent hover:border-slate-50 dark:hover:border-slate-800 transition-all border-l-2 border-l-transparent hover:border-l-primary/30 pl-1 rounded-sm">
            <div class="flex items-start gap-4 flex-1 min-w-0 pt-1">
                <label class="relative flex items-center justify-center cursor-pointer">
                    <input type="checkbox" ${item.completed ? 'checked' : ''} class="checklist-checkbox peer hidden" onchange="createTask_toggleItem(${item.id})">
                    <div class="size-5 rounded border-2 border-slate-200 dark:border-slate-700 peer-checked:bg-primary peer-checked:border-primary flex items-center justify-center transition-all"></div>
                    <span class="material-symbols-outlined text-white text-[16px] font-bold absolute opacity-0 peer-checked:opacity-100 pointer-events-none transition-all">check</span>
                </label>
                <div class="flex flex-col gap-1 min-w-0 flex-1">
                    <span id="text-${item.id}" class="checklist-text text-sm font-bold text-slate-700 dark:text-slate-300 ${item.completed ? 'opacity-50 line-through' : ''} truncate outline-none transition-all" contenteditable="false">${item.text}</span>
                    ${item.attachments && item.attachments.length > 0 ? `
                        <div class="flex flex-wrap gap-2 mt-1">
                            ${item.attachments.map(file => `
                                <div class="flex items-center gap-1 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded text-[10px] text-slate-500">
                                    <span class="material-symbols-outlined text-[10px]">attachment</span>
                                    <span class="truncate max-w-[100px]">${file.name}</span>
                                </div>
                            `).join('')}
                        </div>
                    ` : ''}
                </div>
            </div>
            <div class="flex items-center gap-1 bg-white dark:bg-slate-800 rounded-lg p-1 border border-slate-100 dark:border-slate-700 shadow-sm transition-all opacity-100">
                <button class="size-8 flex items-center justify-center rounded hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-400 hover:text-primary transition-all" onclick="createTask_toggleEditItem(${item.id}, this)">
                    <span class="material-symbols-outlined text-[18px]">edit</span>
                </button>
                <button class="size-8 flex items-center justify-center rounded hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-400 hover:text-primary transition-all overflow-hidden" title="Attach" onclick="createTask_triggerItemAttachment(${item.id})">
                    <span class="material-symbols-outlined text-[18px]">attach_file</span>
                </button>
                <button class="size-8 flex items-center justify-center rounded hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-400 hover:text-primary transition-all overflow-hidden" title="Assign" onclick="createTask_toggleAssigneeDropdown(event, ${item.id}, this)">
                    ${item.assigneeId ?
            `<div class="size-6 rounded-full bg-cover bg-center border border-white dark:border-slate-700" style="background-image: url('${window.createTask_availableUsers.find(u => u.id === item.assigneeId)?.avatar}')"></div>` :
            '<span class="material-symbols-outlined text-[18px]">person_add</span>'}
                </button>
                <button class="size-8 flex items-center justify-center rounded hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-400 hover:text-red-500 transition-all" onclick="createTask_removeItem(${item.id})">
                    <span class="material-symbols-outlined text-[18px]">delete</span>
                </button>
            </div>
        </div>
    `).join('');
    createTask_updateProgress();
};

let currentAttachmentItemId = null;

window.createTask_triggerItemAttachment = function (itemId) {
    currentAttachmentItemId = itemId;
    if (itemFileInput) itemFileInput.click();
};

window.createTask_handleItemFileSelection = function (e) {
    if (!currentAttachmentItemId) return;
    const files = Array.from(e.target.files);
    if (files.length === 0) return;

    const item = window.checklistItems.find(i => i.id === currentAttachmentItemId);
    if (item) {
        if (!item.attachments) item.attachments = [];
        files.forEach(file => {
            item.attachments.push({ name: file.name, size: file.size, type: file.type });
        });
        createTask_renderChecklist();
    }
    e.target.value = '';
    currentAttachmentItemId = null;
};

window.createTask_handleChecklistFileSelection = function (input) {
    const files = Array.from(input.files);
    if (files.length === 0) return;

    window.createTask_quickAddAttachments = [...window.createTask_quickAddAttachments, ...files.map(f => ({ name: f.name, size: f.size, type: f.type }))];

    const btn = input.nextElementSibling;
    if (btn) {
        btn.classList.add('text-primary');
        btn.title = `${window.createTask_quickAddAttachments.length} files attached`;
    }
};

window.createTask_openMemberSelection = function (role) {
    window.createTask_currentRole = role;
    const panel = document.getElementById('memberSelectionPanel');
    const overlay = document.getElementById('memberSelectionOverlay');
    const title = document.getElementById('selectionPanelTitle');
    const searchInput = document.getElementById('memberSearchInput');

    if (!panel || !overlay) return;

    // Set side panel title and description
    let label = role.charAt(0).toUpperCase() + role.slice(1);
    const isSingleSelect = (role === 'assignee' || role === 'checklist');

    if (role === 'checklist') label = 'Assignee';

    title.innerText = `Select ${label}${isSingleSelect ? '' : 's'}`;
    title.nextElementSibling.innerText = isSingleSelect ? 'Single select enabled' : 'Multi-select enabled';

    // Reset search
    if (searchInput) searchInput.value = '';

    // Show panel
    overlay.classList.remove('hidden');
    setTimeout(() => {
        overlay.classList.add('opacity-100');
        panel.classList.remove('translate-x-full');
    }, 10);

    window.createTask_renderMemberList();
};

window.createTask_closeMemberSelection = function () {
    const panel = document.getElementById('memberSelectionPanel');
    const overlay = document.getElementById('memberSelectionOverlay');

    if (panel) panel.classList.add('translate-x-full');
    if (overlay) {
        overlay.classList.remove('opacity-100');
        setTimeout(() => overlay.classList.add('hidden'), 300);
    }
    window.createTask_currentRole = null;
};

window.createTask_handleMemberSearch = function (query) {
    window.createTask_renderMemberList(query);
};

window.createTask_renderMemberList = function (query = '') {
    const list = document.getElementById('selectionMemberList');
    const countInfo = document.getElementById('selectedCountInfo');
    if (!list) return;

    const role = window.createTask_currentRole;
    let selectedIds = [];
    if (role === 'checklist') {
        const targetId = window.createTask_checklistTargetId;
        if (targetId === 'quickAdd') {
            selectedIds = window.createTask_quickAddAssigneeId ? [window.createTask_quickAddAssigneeId] : [];
        } else {
            const item = window.checklistItems.find(i => i.id == targetId);
            selectedIds = (item && item.assigneeId) ? [item.assigneeId] : [];
        }
    } else {
        selectedIds = window.createTask_selectedMembers[role] || [];
    }

    const filteredUsers = window.createTask_availableUsers.filter(u =>
        u.name.toLowerCase().includes(query.toLowerCase()) ||
        u.role.toLowerCase().includes(query.toLowerCase()) ||
        u.email.toLowerCase().includes(query.toLowerCase())
    );

    list.innerHTML = filteredUsers.map(user => {
        const isSelected = selectedIds.includes(user.id);
        return `
            <div onclick="window.createTask_toggleMemberSelection(${user.id})" class="flex items-center justify-between p-3 rounded-xl border-2 transition-all cursor-pointer ${isSelected ? 'border-primary bg-primary/5' : 'border-slate-50 dark:border-slate-800 hover:border-slate-200 dark:hover:border-slate-700 bg-white dark:bg-slate-900'}">
                <div class="flex items-center gap-3">
                    <div class="size-10 rounded-full bg-cover bg-center border-2 border-white dark:border-slate-800 shadow-sm" style="background-image: url('${user.avatar}')"></div>
                    <div class="flex flex-col">
                        <span class="text-[13px] font-bold ${isSelected ? 'text-primary' : 'text-slate-800 dark:text-slate-200'}">${user.name}</span>
                        <span class="text-[10px] text-slate-400 font-bold uppercase tracking-widest">${user.role}</span>
                    </div>
                </div>
                <div class="size-6 rounded-full border-2 flex items-center justify-center transition-all ${isSelected ? 'bg-primary border-primary' : 'border-slate-200 dark:border-slate-700'}">
                    ${isSelected ? '<span class="material-symbols-outlined text-white text-[16px] font-bold">check</span>' : ''}
                </div>
            </div>
        `;
    }).join('');

    if (countInfo) {
        countInfo.innerText = `${selectedIds.length} SELECTED`;
    }
};

window.createTask_toggleMemberSelection = function (userId) {
    const role = window.createTask_currentRole;
    if (!role) return;

    if (role === 'checklist') {
        const targetId = window.createTask_checklistTargetId;
        if (targetId === 'quickAdd') {
            window.createTask_quickAddAssigneeId = userId === window.createTask_quickAddAssigneeId ? null : userId;

            // Visual feedback for quick add button
            const assignBtn = document.querySelector('button[onclick*="createTask_toggleAssigneeDropdown"][onclick*="quickAdd"]');
            if (assignBtn) {
                if (window.createTask_quickAddAssigneeId) {
                    const user = window.createTask_availableUsers.find(u => u.id === window.createTask_quickAddAssigneeId);
                    assignBtn.innerHTML = `<div class="size-5 rounded-full bg-cover bg-center" style="background-image: url('${user.avatar}')"></div>`;
                    assignBtn.classList.add('ring-2', 'ring-primary/20');
                } else {
                    assignBtn.innerHTML = '<span class="material-symbols-outlined text-sm">person_add</span>';
                    assignBtn.classList.remove('ring-2', 'ring-primary/20');
                }
            }
        } else {
            const item = window.checklistItems.find(i => i.id == targetId);
            if (item) {
                item.assigneeId = userId === item.assigneeId ? null : userId;
                window.createTask_renderChecklist();
            }
        }
        window.createTask_closeMemberSelection();
        return;
    }

    let selected = [...window.createTask_selectedMembers[role]];
    const index = selected.indexOf(userId);

    if (role === 'assignee') {
        // Single select for assignee
        window.createTask_selectedMembers[role] = index > -1 ? [] : [userId];
    } else {
        // Multi select for manager/contributor
        if (index > -1) {
            selected.splice(index, 1);
        } else {
            selected.push(userId);
        }
        window.createTask_selectedMembers[role] = selected;
    }

    window.createTask_renderMemberList(document.getElementById('memberSearchInput').value || '');
    window.createTask_updateRolePreviews();
};

window.createTask_updateRolePreviews = function () {
    const roles = ['manager', 'assignee', 'contributor'];

    roles.forEach(role => {
        const listDiv = document.getElementById(`${role}List`);
        const hiddenInput = document.getElementById(`task${role.charAt(0).toUpperCase() + role.slice(1)}${role === 'assignee' ? '' : 's'}`);
        if (!listDiv || !hiddenInput) return;

        const selectedIds = window.createTask_selectedMembers[role];
        hiddenInput.value = selectedIds.join(',');

        if (selectedIds.length === 0) {
            listDiv.innerHTML = `<span class="text-slate-400 dark:text-slate-500 text-[11px] font-bold py-1">Select ${role.charAt(0).toUpperCase() + role.slice(1)}${role === 'assignee' ? '' : 's'}</span>`;
        } else {
            listDiv.innerHTML = selectedIds.map(id => {
                const user = window.createTask_availableUsers.find(u => u.id === id);
                if (!user) return '';
                return `
                    <div class="flex items-center gap-1 bg-slate-100 dark:bg-slate-800 pl-1 pr-1.5 py-0.5 rounded-lg border border-slate-200 dark:border-slate-700 group/tag shrink-0">
                        <div class="size-5 rounded-full bg-cover bg-center shrink-0 shadow-sm" style="background-image: url('${user.avatar}')"></div>
                        <span class="text-[10px] font-bold text-slate-700 dark:text-slate-300 font-sans truncate max-w-[60px]">${user.name.split(' ')[0]}</span>
                        <button type="button" onclick="event.stopPropagation(); window.createTask_toggleMemberSelectionFromTag('${role}', ${id})" class="flex items-center justify-center text-slate-400 hover:text-red-500 transition-colors shrink-0">
                            <span class="material-symbols-outlined text-[12px] font-bold">close</span>
                        </button>
                    </div>
                `;
            }).join('');
        }
    });
};

window.createTask_toggleMemberSelectionFromTag = function (role, userId) {
    window.createTask_currentRole = role;
    window.createTask_toggleMemberSelection(userId);
    window.createTask_currentRole = null;
};

window.createTask_toggleAssigneeDropdown = function (e, targetId, btn) {
    if (e) e.stopPropagation();

    if (typeof targetId === 'number' || targetId === 'quickAdd') {
        window.createTask_checklistTargetId = targetId;
        window.createTask_openMemberSelection('checklist');
    }
};

window.createTask_addNewChecklistItem = function () {
    const input = document.getElementById('quickTaskInput');
    const val = input.value.trim();
    if (!val) return;

    window.checklistItems.push({
        id: Date.now(),
        text: val,
        completed: false,
        assigneeId: window.createTask_quickAddAssigneeId,
        attachments: window.createTask_quickAddAttachments
    });

    window.createTask_quickAddAssigneeId = null;
    window.createTask_quickAddAttachments = [];

    createTask_renderChecklist();
    createTask_toggleAddSection(false);

    const fileBtn = document.getElementById('checklistFileInput')?.nextElementSibling;
    if (fileBtn) {
        fileBtn.classList.remove('text-primary');
        fileBtn.title = 'Attach';
    }
};

window.createTask_toggleItem = function (id) {
    const item = window.checklistItems.find(i => i.id === id);
    if (item) { item.completed = !item.completed; createTask_renderChecklist(); }
};

window.createTask_removeItem = function (id) {
    window.checklistItems = window.checklistItems.filter(i => i.id !== id);
    createTask_renderChecklist();
};

window.createTask_toggleEditItem = function (id, btn) {
    const span = document.getElementById(`text-${id}`);
    const isEditing = span.contentEditable === 'true';
    if (isEditing) {
        span.contentEditable = 'false';
        span.classList.remove('bg-slate-50', 'ring-1', 'ring-primary/20', 'px-1', 'rounded');
        btn.querySelector('span').innerText = 'edit';
        const item = window.checklistItems.find(i => i.id === id);
        if (item) item.text = span.innerText;
    } else {
        span.contentEditable = 'true';
        span.classList.add('bg-slate-50', 'ring-1', 'ring-primary/20', 'px-1', 'rounded');
        btn.querySelector('span').innerText = 'check';
        span.focus();
    }
};

window.createTask_updateProgress = function () {
    const total = window.checklistItems.length;
    const checked = window.checklistItems.filter(i => i.completed).length;
    const percent = total > 0 ? Math.round((checked / total) * 100) : 0;
    // Optional chaining in case elements are missing during transition
    const bar = document.getElementById('checklistProgressBar');
    const text = document.getElementById('checklistProgressText');
    if (bar) bar.style.width = percent + '%';
    if (text) text.innerText = percent + '%';
};

window.createTask_toggleAddSection = function (show) {
    const s = document.getElementById('addItemSection');
    const b = document.getElementById('addChecklistItemBtn');
    const i = document.getElementById('quickTaskInput');
    if (show) { s?.classList.remove('hidden'); b?.classList.add('hidden'); i?.focus(); }
    else { s?.classList.add('hidden'); b?.classList.remove('hidden'); if (i) i.value = ''; }
};

// --- Initialization ---

window.initCreateTask = function () {
    // 1. Initialize Main Attachments (From original create-task.js)
    const dropZone = document.getElementById('dropZone');
    const attachmentInput = document.getElementById('attachmentInput');
    const attachmentList = document.getElementById('attachmentList');
    let uploadedFiles = [];

    if (dropZone && attachmentInput) {
        dropZone.onclick = () => attachmentInput.click();
        attachmentInput.onchange = (e) => handleFiles(e.target.files);

        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, (e) => { e.preventDefault(); e.stopPropagation(); }, false);
        });

        ['dragenter', 'dragover'].forEach(eventName => {
            dropZone.addEventListener(eventName, () => dropZone.classList.add('border-primary', 'bg-primary/5'), false);
        });

        ['dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, () => dropZone.classList.remove('border-primary', 'bg-primary/5'), false);
        });

        dropZone.addEventListener('drop', (e) => handleFiles(e.dataTransfer.files));
    }

    function handleFiles(files) {
        [...files].forEach(file => {
            if (!uploadedFiles.some(f => f.name === file.name && f.size === file.size)) {
                uploadedFiles.push(file);
                addFileToList(file);
            }
        });
    }

    function addFileToList(file) {
        if (!attachmentList) return;
        const isImage = file.type.startsWith('image/');
        const li = document.createElement('div');
        li.className = 'flex items-center justify-between p-2.5 bg-slate-50 dark:bg-slate-800/50 rounded-lg group border border-slate-200 dark:border-slate-700 hover:border-primary/50 transition-all animate-in fade-in slide-in-from-left-2';
        li.innerHTML = `
            <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-lg bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 flex items-center justify-center overflow-hidden">
                    ${isImage ? `<img src="${URL.createObjectURL(file)}" class="w-full h-full object-cover" />` : `<span class="material-symbols-outlined text-slate-400">description</span>`}
                </div>
                <div>
                    <p class="text-sm font-medium text-slate-700 dark:text-slate-200 truncate max-w-[150px]">${file.name}</p>
                    <p class="text-[10px] text-slate-500">${(file.size / 1024).toFixed(1)} KB</p>
                </div>
            </div>
            <button type="button" class="remove-file p-1.5 text-slate-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 rounded-md transition-all">
                <span class="material-symbols-outlined text-sm">close</span>
            </button>
        `;
        li.querySelector('.remove-file').addEventListener('click', () => {
            li.classList.add('scale-95', 'opacity-0');
            setTimeout(() => { li.remove(); uploadedFiles = uploadedFiles.filter(f => f !== file); }, 200);
        });
        attachmentList.appendChild(li);
    }

    // 2. Initialize Checklist Item Attachments Input
    if (!document.getElementById('checklist-item-file-input')) {
        itemFileInput = document.createElement('input');
        itemFileInput.id = 'checklist-item-file-input';
        itemFileInput.type = 'file';
        itemFileInput.multiple = true;
        itemFileInput.style.display = 'none';
        itemFileInput.onchange = window.createTask_handleItemFileSelection;
        document.body.appendChild(itemFileInput);
    } else {
        itemFileInput = document.getElementById('checklist-item-file-input');
    }

    // 3. Reset and Render Checklist
    // Note: We might want to clear checklistItems here if we want a fresh state every time
    // window.checklistItems = [...defaultItems]; 
    window.createTask_renderChecklist();
    window.createTask_updateRolePreviews(); // Added to show defaults if any

    const quickInput = document.getElementById('quickTaskInput');
    if (quickInput) {
        quickInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') createTask_addNewChecklistItem();
        });
    }

    // 4. Initialize Select2 (from original code)
    if (typeof jQuery !== 'undefined' && $.fn.select2) {
        $('#managerSelect, #contributorSelect').select2({
            placeholder: "Search & Select...",
            allowClear: true,
            width: '100%'
        });
    }

    // 5. Standalone Page Logic (Close/Cancel buttons)
    const sideDrawer = document.getElementById('sideDrawerContainer');
    const checklistContainer = document.getElementById('checklistContainer');
    // If checklistContainer is NOT inside sidebar, we are on standalone page
    if (checklistContainer && sideDrawer && !sideDrawer.contains(checklistContainer)) {
        const closeBtn = document.getElementById('btnCloseDrawer');
        const cancelBtn = document.getElementById('btnCancelTask');
        [closeBtn, cancelBtn].forEach(btn => {
            if (btn) btn.onclick = () => window.location.href = '/Admin/Kanban';
        });
    }
};

// Auto-init for standalone page load
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('checklistContainer')) {
        window.initCreateTask();
    }
});

// Close dropdown/panel on outside click
document.addEventListener('click', (e) => {
    const p = document.getElementById('memberSelectionPanel');
    const o = document.getElementById('memberSelectionOverlay');
    if (p && !p.classList.contains('translate-x-full') && !p.contains(e.target) && e.target === o) {
        window.createTask_closeMemberSelection();
    }
});
