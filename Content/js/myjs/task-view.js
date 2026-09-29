/**
 * Task View/Detail Management
 * This script handles the logic for the View Task panel/drawer.
 */

window.initTaskDetail = function () {
    console.log("Initializing Task Detail logic...");

    // --- State Mock ---
    window.taskDetail_availableUsers = [
        { id: 1, name: "Alex Johnston", email: "alex.j@example.com", role: "Project Lead", avatar: "https://lh3.googleusercontent.com/aida-public/AB6AXuDYfYiS4QfKuWXUbF0HAyay8pMY1uC6mk4cy9WD-iWcbg4RIBq9qExWwiL3uGo5YpcRCC2MHEKQ_wytN6Nz0MX-NxTcQbCLpQ8zEthb67rR3uHjzUqfsV9ojGDjdplYZkdMiPZRj6i9af6vqHI1Y6j-FjkGZMfACfptegpV2O98HBjzHOsBN1lFneWpqrsy4Xw__5dg9JBb75t3fRWfr7plveHsCbcyKBv3i53BnVN38Ia4wuoVafwBq0DLSmjvG-p9HJTc4UYUUcI8" },
        { id: 2, name: "Sarah Smith", email: "sarah.s@example.com", role: "UI Designer", avatar: "https://lh3.googleusercontent.com/aida-public/AB6AXuDj3uBaa2F8uD4VCM0QFaGJ8y98zlRPgUssfUxGmduM-fdDvP2jU2pav4NvKkiVbnpF4RgfGeeK34_aPwCl0x8mjw56nM7yOxVq4sj6FGSlqaloCyWh6O200wjov3nfM_n1X6So1jhZu4wM6pvWMRB0Hmc0a2wHfuZvL9DY6VHlg1ntcu9IIcjICnsIJ5RZ6s7Td13qHoaGvXx5yvv3e1cNC5hMA8MVYzoe_6a5qo7X3GYoEWKu9eATdUHm7JBlBp8tFaQPElT9NrU" },
        { id: 3, name: "Maria Garcia", email: "maria.g@example.com", role: "Frontend Dev", avatar: "https://lh3.googleusercontent.com/aida-public/AB6AXuAjHTnSDXo6-2aK0-NIS_lCOJLV2Ng7-EHvHNAau1QFB1xRzxC4_3-2g-BbphMUcaSVbgWoXdP_LW_UKbpyjv-CLdk5AKP2x1e_dweAvS2nJHsVhePhPgyTr_vp9Wm8BgXKD69qsJ2Zu5jAtrCL2VHerqjfKdsskqyAN0rbDMkzZlBDzbEsvD-9jAgqh5X1D2bcv-jalSJR2w-XoiGkAl6grdryxR7pcYsz4cj3BcO0IIBkPlflxWM5Icu346L75sruUY4nE4keBBI" },
        { id: 4, name: "James Wilson", email: "james.w@example.com", role: "Back-end Dev", avatar: "https://lh3.googleusercontent.com/aida-public/AB6AXuAT07UisQswunrFFOnHWK6QqlW790gOBMSHH9f6cbUDOIWUDqf_NDWN3Sk9GAEgZ-j-nFPD4YtS5XJWKu8_XEkn26jQj2SnzYVydKkby3mS-n-o0BaID7QSeVL6D9sG2Rp-g7AHNG1fGypo2xeBuM5rRVqAordTg_szQSwo5fxz2Q6blZfqDlrNPb_eIkSodyf9ogIgoeE3udigzxSm9JOpeJXdvT0WE93yxioXFMZ1Jqi_THP6RMjWHI1hUY3R1DDWL58tvw5z6Jo" }
    ];

    window.taskDetail_selectedAssignees = [
        { ...window.taskDetail_availableUsers[0], assignedRole: 'Manager' },
        { ...window.taskDetail_availableUsers[1], assignedRole: 'Assignee' }
    ];

    // --- Member Management Functions ---
    window.taskDetail_openMemberManagementPanel = function (e, el) {
        if (e) e.stopPropagation();

        const panel = document.getElementById('memberManagementPanel');
        const overlay = document.getElementById('memberManagementOverlay');
        const taskTitle = document.getElementById('memberManagementTaskTitle');

        if (!panel || !overlay) {
            console.error('Member management panel or overlay not found');
            return;
        }

        // Try to get task title
        const headerTitle = document.getElementById('taskHeaderTitle');
        if (taskTitle && headerTitle) taskTitle.innerText = headerTitle.innerText;

        panel.classList.remove('translate-x-full');
        overlay.classList.remove('hidden');
        setTimeout(() => overlay.classList.add('opacity-100'), 10);

        window.taskDetail_renderAssigneeList();
    };

    window.taskDetail_closeMemberManagementPanel = function () {
        const panel = document.getElementById('memberManagementPanel');
        const overlay = document.getElementById('memberManagementOverlay');

        if (panel) panel.classList.add('translate-x-full');
        if (overlay) {
            overlay.classList.remove('opacity-100');
            setTimeout(() => overlay.classList.add('hidden'), 300);
        }
    };

    window.taskDetail_toggleUserSearch = function (role) {
        const searchContainer = document.getElementById(`searchSection_${role}`);
        const input = document.getElementById(`searchInput_${role}`);

        if (searchContainer.classList.contains('hidden')) {
            document.querySelectorAll('[id^="searchSection_"]').forEach(s => s.classList.add('hidden'));
            searchContainer.classList.remove('hidden');
            input.value = '';
            input.focus();
        } else {
            searchContainer.classList.add('hidden');
        }
    };

    window.taskDetail_handleUserSearch = function (query, role) {
        const suggestions = document.getElementById(`suggestions_${role}`);
        if (!query || query.length < 1) {
            suggestions.classList.add('hidden');
            return;
        }

        const filtered = window.taskDetail_availableUsers.filter(u =>
            (u.name.toLowerCase().includes(query.toLowerCase()) ||
                u.email.toLowerCase().includes(query.toLowerCase())) &&
            !window.taskDetail_selectedAssignees.find(s => s.id === u.id && s.assignedRole === role)
        );

        if (filtered.length > 0) {
            suggestions.innerHTML = filtered.map(u => `
                <div class="flex items-center gap-3 p-3 hover:bg-slate-50 dark:hover:bg-slate-800 cursor-pointer transition-colors border-b border-slate-50 dark:border-slate-800 last:border-0" onclick="taskDetail_addAssignee(${u.id}, '${role}')">
                    <div class="size-8 rounded-full bg-cover bg-center border border-slate-100 dark:border-slate-700" style="background-image: url('${u.avatar}')"></div>
                    <div class="flex flex-col">
                        <span class="text-[11px] font-bold text-slate-800 dark:text-slate-200">${u.name}</span>
                        <span class="text-[9px] text-slate-400 font-medium">${u.role}</span>
                    </div>
                </div>
            `).join('');
            suggestions.classList.remove('hidden');
        } else {
            suggestions.innerHTML = `<div class="p-4 text-center text-[10px] font-bold text-slate-400">No members found</div>`;
            suggestions.classList.remove('hidden');
        }
    };

    window.taskDetail_addAssignee = function (id, role) {
        const user = window.taskDetail_availableUsers.find(u => u.id === id);
        if (user) {
            // Remove user from any existing role first to ensure 1 role per user
            window.taskDetail_selectedAssignees = window.taskDetail_selectedAssignees.filter(s => s.id !== id);

            if (role === 'Manager') {
                // If adding a new manager, remove existing one
                window.taskDetail_selectedAssignees = window.taskDetail_selectedAssignees.filter(s => s.assignedRole !== 'Manager');
            }

            window.taskDetail_selectedAssignees.push({ ...user, assignedRole: role });
            window.taskDetail_renderAssigneeList();
        }
        const searchContainer = document.getElementById(`searchSection_${role}`);
        if (searchContainer) searchContainer.classList.add('hidden');
    };

    window.taskDetail_removeAssignee = function (id, role) {
        window.taskDetail_selectedAssignees = window.taskDetail_selectedAssignees.filter(s => !(s.id === id && s.assignedRole === role));
        window.taskDetail_renderAssigneeList();
    };

    window.taskDetail_updateMemberRole = function (id, oldRole, newRole) {
        const member = window.taskDetail_selectedAssignees.find(s => s.id === id && s.assignedRole === oldRole);
        if (member) {
            if (newRole === 'Manager') {
                window.taskDetail_selectedAssignees = window.taskDetail_selectedAssignees.filter(s => s.assignedRole !== 'Manager' || s.id === id);
            }
            member.assignedRole = newRole;
            window.taskDetail_renderAssigneeList();
        }
    };

    window.taskDetail_renderAssigneeList = function () {
        const listContainer = document.getElementById('sidebarAssigneeList');
        const panelContainer = document.getElementById('memberManagementList');

        const sections = { 'Manager': [], 'Assignee': [], 'Contributor': [] };
        window.taskDetail_selectedAssignees.forEach(u => {
            const role = u.assignedRole || 'Contributor';
            if (sections[role]) sections[role].push(u);
            else sections['Contributor'].push(u);
        });

        // --- Render Sidebar Overview ---
        let overviewHtml = '';
        ['Manager', 'Assignee', 'Contributor'].forEach(role => {
            const users = sections[role];
            const label = role === 'Assignee' ? 'Assignees' : (role === 'Manager' ? 'Manager' : 'Contributors');
            overviewHtml += `
                <div class="flex flex-col gap-3">
                    <h3 class="text-[10px] font-black text-slate-400 uppercase tracking-widest">${label}</h3>
                    <div class="flex flex-col gap-4">
            `;
            if (users.length === 0) {
                overviewHtml += `
                    <div class="p-4 border border-dashed border-slate-200 dark:border-slate-700 rounded-xl bg-slate-50/50 dark:bg-slate-900/30 text-center">
                        <span class="text-[10px] text-slate-400 font-bold uppercase tracking-wider">No ${label.toLowerCase()} assigned</span>
                    </div>`;
            } else {
                overviewHtml += users.map(u => `
                    <div class="assignee-item flex items-center gap-4 relative group">
                        <button class="remove-btn hidden absolute -top-1 -left-1 size-5 bg-red-500 text-white rounded-full flex items-center justify-center hover:bg-red-600 shadow-lg z-10 transition-all" onclick="window.taskDetail_removeAssignee(${u.id}, '${role}')">
                            <span class="material-symbols-outlined text-[14px]">close</span>
                        </button>
                        <div class="h-11 w-11 rounded-full bg-center bg-cover border-2 border-white dark:border-slate-700 shadow-sm" style="background-image: url('${u.avatar}')"></div>
                        <div class="flex flex-col">
                            <span class="text-sm font-bold text-slate-800 dark:text-slate-100">${u.name}</span>
                            <span class="text-[10px] text-slate-400 font-bold uppercase tracking-widest leading-tight">${u.role}</span>
                        </div>
                    </div>
                `).join('');
            }
            overviewHtml += `</div></div>`;
        });
        if (listContainer) listContainer.innerHTML = overviewHtml;

        // --- Render Panel Drawer ---
        let panelHtml = '';
        ['Manager', 'Assignee', 'Contributor'].forEach(role => {
            const users = sections[role];
            const label = role === 'Assignee' ? 'Assignee' : (role === 'Manager' ? 'Manager' : 'Contributor');
            panelHtml += `
            <div class="flex flex-col gap-3">
                <div class="flex items-center justify-between px-1">
                    <div class="flex items-center gap-2">
                        <h4 class="text-[10px] font-black text-slate-400 uppercase tracking-widest">${label}s</h4>
                        <span class="text-[9px] font-black text-slate-400 bg-slate-100 dark:bg-slate-800 px-2 py-0.5 rounded-md border border-slate-200 dark:border-slate-700">${users.length}</span>
                    </div>
                    <button onclick="window.taskDetail_toggleUserSearch('${role}')" class="flex items-center gap-1 text-[10px] font-bold text-primary hover:text-primary/80 transition-colors">
                        <span class="material-symbols-outlined text-sm">add</span>
                        <span>Add</span>
                    </button>
                </div>
                <div id="searchSection_${role}" class="hidden relative group">
                    <div class="relative">
                        <span class="material-symbols-outlined absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 text-base">search</span>
                        <input id="searchInput_${role}" type="text" class="w-full bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl pl-9 pr-4 h-10 text-[11px] font-bold focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all" placeholder="Search ${label}s..." onkeyup="window.taskDetail_handleUserSearch(this.value, '${role}')">
                    </div>
                    <div id="suggestions_${role}" class="absolute left-0 right-0 top-full mt-1 bg-white dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-xl shadow-2xl z-[130] hidden max-h-48 overflow-y-auto custom-scrollbar"></div>
                </div>
                <div class="flex flex-col gap-2.5">
            `;
            if (users.length === 0) {
                panelHtml += `
                    <div class="p-6 border-2 border-dashed border-slate-200 dark:border-slate-700 rounded-xl text-center flex flex-col items-center gap-2 bg-slate-50/50 dark:bg-slate-800/20 group hover:border-primary/20 transition-colors cursor-pointer" onclick="window.taskDetail_toggleUserSearch('${role}')">
                        <span class="material-symbols-outlined text-slate-300 text-2xl group-hover:text-primary/40">person_add</span>
                        <span class="text-[10px] text-slate-400 font-bold uppercase tracking-wider">No ${label}s assigned</span>
                    </div>`;
            } else {
                panelHtml += users.map(u => `
                    <div class="bg-white dark:bg-slate-800 p-3 rounded-2xl border border-slate-100 dark:border-slate-700 shadow-sm flex items-center justify-between group hover:border-primary/20 transition-all">
                        <div class="flex items-center gap-3">
                            <div class="size-10 rounded-full bg-cover bg-center border-2 border-white dark:border-slate-700 shadow-sm" style="background-image: url('${u.avatar}')"></div>
                            <div class="flex flex-col gap-0.5">
                                <span class="text-[12px] font-black text-slate-900 dark:text-white leading-tight">${u.name}</span>
                                <div class="relative inline-block">
                                    <select onchange="window.taskDetail_updateMemberRole(${u.id}, '${role}', this.value)" class="text-[9px] font-black text-slate-400 uppercase tracking-widest bg-transparent border-none p-0 pr-4 focus:ring-0 cursor-pointer hover:text-primary appearance-none outline-none">
                                        <option value="Assignee" ${u.assignedRole === 'Assignee' ? 'selected' : ''}>Assignee</option>
                                        <option value="Manager" ${u.assignedRole === 'Manager' ? 'selected' : ''}>Manager</option>
                                        <option value="Contributor" ${u.assignedRole === 'Contributor' ? 'selected' : ''}>Contributor</option>
                                    </select>
                                    <span class="material-symbols-outlined text-[10px] absolute right-0 top-1/2 -translate-y-1/2 pointer-events-none text-slate-300">expand_more</span>
                                </div>
                            </div>
                        </div>
                        <button onclick="window.taskDetail_removeAssignee(${u.id}, '${role}')" class="size-8 flex items-center justify-center text-slate-300 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-xl transition-all opacity-0 group-hover:opacity-100">
                            <span class="material-symbols-outlined text-lg">close</span>
                        </button>
                    </div>
                `).join('');
            }
            panelHtml += `</div></div>`;
        });
        if (panelContainer) panelContainer.innerHTML = panelHtml;

        // Re-apply inline edit state if active
        const detailContainer = document.getElementById('taskDetailContainer');
        if (detailContainer && detailContainer.classList.contains('is-editing-global')) {
            const listContainer = document.getElementById('sidebarAssigneeList');
            if (listContainer) listContainer.querySelectorAll('.remove-btn').forEach(btn => btn.classList.remove('hidden'));
        }
    };

    window.taskDetail_saveMemberRoleChanges = function (event) {
        const btn = event.currentTarget;
        const originalHtml = btn.innerHTML;
        btn.innerHTML = `<span class="material-symbols-outlined text-lg animate-spin">sync</span><span>Saving...</span>`;
        btn.disabled = true;
        setTimeout(() => {
            btn.innerHTML = `<span class="material-symbols-outlined text-lg">check_circle</span><span>Applied!</span>`;
            btn.classList.replace('bg-primary', 'bg-emerald-500');
            setTimeout(() => {
                window.taskDetail_closeMemberManagementPanel();
                setTimeout(() => {
                    btn.innerHTML = originalHtml;
                    btn.classList.replace('bg-emerald-500', 'bg-primary');
                    btn.disabled = false;
                }, 300);
            }, 600);
        }, 800);
    };

    // --- Task Status Functions ---
    window.toggleStatusDropdown = function (e) {
        if (e) e.stopPropagation();
        const dropdown = document.getElementById('statusDropdown');
        if (dropdown) dropdown.classList.toggle('hidden');
    };

    window.updateTaskStatus = function (status, colorClass, icon, textColor, bgColor, borderColor) {
        const headerText = document.getElementById('headerStatusText');
        const headerDot = document.getElementById('headerStatusDot');

        if (headerText) headerText.innerText = status;
        if (headerDot) {
            headerDot.className = `h-2 w-2 rounded-full ${colorClass}`;
        }

        const footerLabel = document.getElementById('footerStatusLabel');
        const footerBtn = document.getElementById('statusDropdownBtn');
        const footerIcon = footerBtn ? footerBtn.querySelector('.status-icon') : null;

        if (footerLabel) footerLabel.innerText = status;
        if (footerBtn) {
            footerBtn.className = `flex items-center gap-2 px-3 py-1.5 rounded-full ${bgColor} ${textColor} border ${borderColor} hover:opacity-90 transition-all text-xs font-bold group shadow-sm`;
        }
        if (footerIcon) {
            footerIcon.innerText = icon;
        }

        const dropdown = document.getElementById('statusDropdown');
        if (dropdown) dropdown.classList.add('hidden');
    };

    // --- Inline Editing Functions ---
    window.toggleGlobalEditMode = function () {
        const detailContainer = document.getElementById('taskDetailContainer');
        if (!detailContainer) return;

        const isEditing = detailContainer.classList.toggle('is-editing-global');
        const sections = ['general-info', 'assignees', 'attachments'];
        const globalEditBtn = document.getElementById('btnFullEdit');
        const fullEditLabel = document.getElementById('fullEditLabel');

        if (isEditing) {
            sections.forEach(id => window.enableSectionEdit(id, true));
            if (globalEditBtn) globalEditBtn.classList.add('text-primary', 'bg-primary/10');
            if (fullEditLabel) fullEditLabel.innerText = "Cancel Edit";
        } else {
            sections.forEach(id => window.disableSectionEdit(id));
            if (globalEditBtn) globalEditBtn.classList.remove('text-primary', 'bg-primary/10');
            if (fullEditLabel) fullEditLabel.innerText = "Full Edit";
        }
    };

    window.saveAllSections = function () {
        const sections = ['general-info', 'assignees', 'attachments'];
        const globalSaveBtn = document.getElementById('btnGlobalSave');

        if (globalSaveBtn) {
            const originalText = globalSaveBtn.innerText;
            globalSaveBtn.innerText = "Saving...";

            setTimeout(() => {
                sections.forEach(id => window.saveSectionEdit(id));
                window.toggleGlobalEditMode();
                globalSaveBtn.innerText = originalText;
            }, 800);
        }
    };

    window.enableSectionEdit = function (sectionId, isGlobal = false) {
        const section = document.getElementById(`section-${sectionId}`);
        if (!section) return;

        section.classList.add('is-editing');
        const editBtn = section.querySelector('.edit-btn');
        const saveActions = section.querySelector('.save-actions');

        if (editBtn) editBtn.classList.add('hidden');
        if (saveActions && !isGlobal) saveActions.classList.remove('hidden');

        if (sectionId === 'general-info') {
            const content = document.getElementById('editable-general-info');
            if (content) {
                content.contentEditable = "true";
                content.classList.add('ring-2', 'ring-primary/20', 'border-primary', 'bg-white');
                if (!isGlobal) content.focus();
            }
        } else {
            if (sectionId === 'assignees' || sectionId === 'attachments') {
                section.querySelectorAll('.remove-btn').forEach(btn => btn.classList.remove('hidden'));
            }

            section.querySelectorAll('.editable-text').forEach(el => {
                el.contentEditable = "true";
                el.classList.add('bg-slate-50', 'px-1', 'rounded', 'ring-1', 'ring-primary/20');
            });
        }
    };

    window.disableSectionEdit = function (sectionId) {
        const section = document.getElementById(`section-${sectionId}`);
        if (!section) return;

        section.classList.remove('is-editing');
        const editBtn = section.querySelector('.edit-btn');
        const saveActions = section.querySelector('.save-actions');

        if (editBtn) editBtn.classList.remove('hidden');
        if (saveActions) saveActions.classList.add('hidden');

        if (sectionId === 'general-info') {
            const content = document.getElementById('editable-general-info');
            if (content) {
                content.contentEditable = "false";
                content.classList.remove('ring-2', 'ring-primary/20', 'border-primary', 'bg-white');
            }
        } else if (sectionId === 'assignees' || sectionId === 'attachments') {
            section.querySelectorAll('.remove-btn').forEach(btn => btn.classList.add('hidden'));
            if (sectionId === 'attachments') {
                section.querySelectorAll('.editable-text').forEach(el => {
                    el.contentEditable = "false";
                    el.classList.remove('bg-slate-50', 'px-1', 'rounded');
                });
            }
        }
    };

    window.saveSectionEdit = function (sectionId) {
        const section = document.getElementById(`section-${sectionId}`);
        if (!section) return;

        const saveBtn = section.querySelector('.save-actions button');
        if (saveBtn) {
            const originalText = saveBtn.innerText;
            saveBtn.innerText = "Saved!";
            setTimeout(() => {
                saveBtn.innerText = originalText;
                window.disableSectionEdit(sectionId);
            }, 600);
        } else {
            window.disableSectionEdit(sectionId);
        }
    };

    window.cancelSectionEdit = function (sectionId) {
        window.disableSectionEdit(sectionId);
    };

    // --- Checklist Functions ---
    window.updateChecklistProgress = function () {
        const items = document.querySelectorAll('.checklist-checkbox');
        const total = items.length;
        const checked = Array.from(items).filter(i => i.checked).length;
        const percent = total > 0 ? Math.round((checked / total) * 100) : 0;

        const progressBar = document.getElementById('checklistProgress');
        const percentLabel = document.getElementById('checklistPercent');

        if (progressBar) progressBar.style.width = `${percent}%`;
        if (percentLabel) percentLabel.innerText = `${percent}%`;
    };

    window.addNewChecklistItem = function (input) {
        const value = input.value.trim();
        if (!value) return;

        const container = document.getElementById('checklistItemsContainer');
        if (!container) return;

        const newRow = document.createElement('div');
        newRow.className = 'checklist-row flex items-start justify-between group py-4 border-b border-transparent hover:border-slate-50 dark:hover:border-slate-800 transition-all border-l-2 border-l-transparent hover:border-l-primary/30 pl-2';

        newRow.innerHTML = `
            <div class="flex items-start gap-5 flex-1 min-w-0 pt-1.5">
                <label class="relative flex items-center justify-center cursor-pointer mt-0.5">
                    <input type="checkbox" class="checklist-checkbox peer hidden" onchange="updateChecklistProgress()">
                    <div class="size-6 rounded-lg border-2 border-slate-200 dark:border-slate-700 peer-checked:bg-primary peer-checked:border-primary flex items-center justify-center transition-all shadow-sm"></div>
                    <span class="material-symbols-outlined text-white text-[18px] font-bold absolute opacity-0 peer-checked:opacity-100 pointer-events-none transition-all">check</span>
                </label>
                <div class="flex flex-col gap-1.5 min-w-0 flex-1">
                    <span class="checklist-text text-sm font-bold text-slate-700 dark:text-slate-300 peer-checked:text-slate-300 dark:peer-checked:text-slate-600 peer-checked:line-through truncate transition-all outline-none" contenteditable="false">${value}</span>
                    <div class="flex items-center -space-x-2.5">
                        <div class="size-6 rounded-full border-2 border-white dark:border-slate-800 bg-center bg-cover shadow-sm" style="background-image: url('https://lh3.googleusercontent.com/aida-public/AB6AXuDYfYiS4QfKuWXUbF0HAyay8pMY1uC6mk4cy9WD-iWcbg4RIBq9qExWwiL3uGo5YpcRCC2MHEKQ_wytN6Nz0MX-NxTcQbCLpQ8zEthb67rR3uHjzUqfsV9ojGDjdplYZkdMiPZRj6i9af6vqHI1Y6j-FjkGZMfACfptegpV2O98HBjzHOsBN1lFneWpqrsy4Xw__5dg9JBb75t3fRWfr7plveHsCbcyKBv3i53BnVN38Ia4wuoVafwBq0DLSmjvG-p9HJTc4UYUUcI8')"></div>
                    </div>
                </div>
            </div>
            <div class="flex items-center gap-1 bg-white dark:bg-slate-800 rounded-xl p-1 border border-slate-100 dark:border-slate-700 shadow-sm">
                <button class="size-9 flex items-center justify-center rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-400 hover:text-primary transition-all" onclick="toggleChecklistEdit(this)">
                    <span class="material-symbols-outlined text-[18px]">edit</span>
                </button>
                <button class="size-9 flex items-center justify-center rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-400 hover:text-primary transition-all">
                    <span class="material-symbols-outlined text-[18px]">attach_file</span>
                </button>
                <button class="size-9 flex items-center justify-center rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-400 hover:text-primary transition-all">
                    <span class="material-symbols-outlined text-[18px]">person</span>
                </button>
                <button class="size-9 flex items-center justify-center rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-400 hover:text-red-500 transition-all" onclick="this.closest('.checklist-row').remove(); updateChecklistProgress();">
                    <span class="material-symbols-outlined text-[18px]">delete</span>
                </button>
            </div>
        `;

        container.appendChild(newRow);
        input.value = '';
        window.updateChecklistProgress();
    };

    window.toggleChecklistEdit = function (btn) {
        const row = btn.closest('.checklist-row');
        const textSpan = row.querySelector('.checklist-text');
        const isEditing = textSpan.contentEditable === 'true';

        if (isEditing) {
            textSpan.contentEditable = 'false';
            textSpan.classList.remove('bg-slate-50', 'ring-1', 'ring-primary/20', 'px-1', 'rounded');
            btn.classList.remove('text-primary', 'bg-primary/10');
            btn.querySelector('span').innerText = 'edit';
        } else {
            textSpan.contentEditable = 'true';
            textSpan.classList.add('bg-slate-50', 'ring-1', 'ring-primary/20', 'px-1', 'rounded');
            btn.classList.add('text-primary', 'bg-primary/10');
            btn.querySelector('span').innerText = 'check';
            textSpan.focus();
        }
    };

    window.handleChecklistFileSelection = function (input) {
        const feedback = document.getElementById('fileSelectedFeedback');
        if (input.files && input.files[0]) {
            if (feedback) {
                feedback.classList.remove('hidden');
                feedback.classList.add('flex');
                setTimeout(() => {
                    feedback.classList.remove('flex');
                    feedback.classList.add('hidden');
                    input.value = ''; // Reset for next selection
                }, 3000);
            }
        }
    };

    // --- Navigation & Panel Functions ---
    window.closeTaskPanel = function () {
        const overlay = document.getElementById('taskPanelOverlay');
        if (overlay) {
            overlay.classList.add('animate-slide-out-right');
            setTimeout(() => {
                overlay.style.display = 'none';
                overlay.classList.remove('animate-slide-out-right');
            }, 300);
        }
    };

    // --- Initial Render ---
    window.taskDetail_renderAssigneeList();
};

// Global click listener for status dropdown closing
document.addEventListener('click', (e) => {
    const dropdown = document.getElementById('statusDropdown');
    const btn = document.getElementById('statusDropdownBtn');
    if (dropdown && !dropdown.contains(e.target) && btn && !btn.contains(e.target)) {
        dropdown.classList.add('hidden');
    }
});
