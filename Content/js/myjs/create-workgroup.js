/**
 * Create Workgroup Panel Logic
 */

// Dummy User Database for demonstration
const userDB = [
    { id: 1, name: 'Alex Rivera', role: 'Sales Lead', img: 'https://lh3.googleusercontent.com/aida-public/AB6AXuCdaCsJUiNp539aie9SJ4lV8pTbY-gcZIBqoARlMUx9k525h7W40g66leO-vID2QRwjC127f9kespa9PmAkRU9UAp0Qj6MTf3bRX472-fUUZH66-rozH45qLVuaMxK7MvzisJ69OXhtb-y1LmI32eqrb00l95zZUWN5TitI6bzLWfA-sg3q-likUYFjRNti7agwuLNMbaG3pwLG4gFnecslV6nPEY0bBiOpGY3iAYjOsfdWBKgxMReKs_ttyXPdHW1HgBIlumYxtyc' },
    { id: 2, name: 'James Wilson', role: 'Account Exec', img: 'https://lh3.googleusercontent.com/aida-public/AB6AXuChFFlfliw2uc8ZdA4E3evNx7UjfUFprdhrYhHxAdx784PgH2KQmG3k9R3Z1Tl0ZrMKNec5CwBDFwgmI36jGyIiyB5MLi6WnpQpmmY3yC20jrcQiE4mlMMDULK0al1Mvfz7SA_dPCXvtD08q_JrKoIwH60v2n27QB61nKaLA4alVc-IeoUrW5HiayTpmMlsUhcnuB2OhJ0OULdfJmrmmq8i14MXSBO5trFGuyGri4VxMiLL1cb8q1mwI-Rv0qr1FroJxg1o6jpWDpg' },
    { id: 3, name: 'Elena Rodriguez', role: 'Marketing Lead', img: 'https://lh3.googleusercontent.com/aida-public/AB6AXuAKv3PQ5qQXBSfnBKnAAaUxrpvx8ppzEvXGqwt0Yku10trQpQwx9j8HSi1AAMq64_jlSrSoWAXdoprwAElln9DpFPIk3RfeT6F1pXxsC380esFo76jC8gTYf6B-CZLjD7NGSYg1mLE-MOsUofROWPXpWVC_mhOLJ0l64hjrRe28-dmgG5-nLgmhlSgmSyePvtA8M8YkV4unDt3itmcGzQ69An0EUnUPinUqNJKJRkXKR2xF59eW68mrQ5phIUmJWEiXEcBaLTaNff8' },
    { id: 4, name: 'Marcus Chen', role: 'Engineering Lead', img: 'https://lh3.googleusercontent.com/aida-public/AB6AXuBQ95XQjgDUw6Cr7AS_IGRwSFTTBs6h6c_GLJ4FaNymRoIRHgtKhQWwiJXdNfqVzt0p29DepBPSSa9KGCVXVvgdx4O-MGVj3epGchfyZWD-50ouAMLcBAOx8F6CETJEJCFb43ltnJFMmzbOy9DreSwGtPZ59bCZL6kg59b5TX4Rp1NAhYLLsBJ9X1RbUCwiSLHGGNClHshEimHcCmvUQBj9WvRBq81Xrn_7C0e0efomtafqgRE7CYHeIwgU0BJ8vzCEr6cQjca5EKE' }
];

let selectedMemberIds = [1, 2];
let selectedManagerIds = [4];

window.updateStepUI = function () {
    const isDetails = document.getElementById('tab-details')?.checked;
    const isTeam = document.getElementById('tab-team')?.checked;
    const isClients = document.getElementById('tab-clients')?.checked;

    const backBtn = document.getElementById('footer-back-btn');
    const nextBtn = document.getElementById('footer-next-btn');
    const createBtn = document.getElementById('footer-create-btn');
    const nextBtnText = document.getElementById('footer-next-text');

    if (!backBtn || !nextBtn || !createBtn || !nextBtnText) return;

    if (isDetails) backBtn.classList.add('hidden');
    else backBtn.classList.remove('hidden');

    if (isClients) {
        nextBtn.classList.add('hidden');
        createBtn.classList.remove('hidden');
    } else {
        nextBtn.classList.remove('hidden');
        createBtn.classList.add('hidden');
        nextBtnText.innerText = isTeam ? 'Next: Clients' : 'Next Step';
    }
};

window.nextStep = function () {
    if (document.getElementById('tab-details')?.checked) {
        document.getElementById('tab-team').checked = true;
    } else if (document.getElementById('tab-team')?.checked) {
        document.getElementById('tab-clients').checked = true;
    }
    window.updateStepUI();
};

window.prevStep = function () {
    if (document.getElementById('tab-team')?.checked) {
        document.getElementById('tab-details').checked = true;
    } else if (document.getElementById('tab-clients')?.checked) {
        document.getElementById('tab-team').checked = true;
    }
    window.updateStepUI();
};

window.toggleDropdown = function (id, forceShow = false) {
    const el = document.getElementById(id);
    if (!el) return;

    if (forceShow) el.classList.remove('hidden');
    else el.classList.toggle('hidden');

    // Rotate arrow for manager
    if (id === 'managerDropdown') {
        const arrow = document.getElementById('managerArrow');
        if (arrow) arrow.classList.toggle('rotate-180', !el.classList.contains('hidden'));
    }
};

window.addManager = function (userId) {
    const user = userDB.find(u => u.id == userId);
    if (!user || selectedManagerIds.includes(userId)) {
        window.toggleDropdown('managerSearchDropdown');
        return;
    }

    selectedManagerIds.push(userId);
    const list = document.getElementById('selectedManagersList');

    const item = document.createElement('div');
    item.className = 'flex items-center gap-3 p-2.5 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-800 group border border-transparent hover:border-slate-100 transition-all';
    item.dataset.userId = userId;
    item.innerHTML = `
        <div class="bg-center bg-no-repeat aspect-square bg-cover rounded-full size-10" style='background-image: url("${user.img}")'></div>
        <div class="flex-1">
            <p class="text-sm font-bold dark:text-white">${user.name}</p>
            <p class="text-xs text-slate-500">${user.role}</p>
        </div>
        <button onclick="removeManager(this)" class="opacity-0 group-hover:opacity-100 text-slate-400 hover:text-red-500 transition-all p-1">
            <span class="material-symbols-outlined text-xl">do_not_disturb_on</span>
        </button>
    `;
    list.appendChild(item);

    updateManagerCount();
    window.toggleDropdown('managerSearchDropdown');
    const input = document.getElementById('managerSearchInput');
    if (input) input.value = '';
};

window.removeManager = function (btn) {
    const item = btn.closest('[data-user-id]');
    const id = parseInt(item.dataset.userId);
    selectedManagerIds = selectedManagerIds.filter(mid => mid !== id);
    item.remove();
    updateManagerCount();
};

function updateManagerCount() {
    const badge = document.getElementById('managerCountBadge');
    if (badge) badge.innerText = `${selectedManagerIds.length} Selected`;
}

window.addMember = function (userId) {
    const user = userDB.find(u => u.id == userId);
    if (!user || selectedMemberIds.includes(userId)) {
        window.toggleDropdown('memberSearchDropdown');
        return;
    }

    selectedMemberIds.push(userId);
    const list = document.getElementById('selectedMembersList');

    const item = document.createElement('div');
    item.className = 'flex items-center gap-3 p-2.5 rounded-xl hover:bg-slate-50 dark:hover:bg-slate-800 group border border-transparent hover:border-slate-100 transition-all';
    item.dataset.userId = userId;
    item.innerHTML = `
        <div class="bg-center bg-no-repeat aspect-square bg-cover rounded-full size-10" style='background-image: url("${user.img}")'></div>
        <div class="flex-1">
            <p class="text-sm font-bold dark:text-white">${user.name}</p>
            <p class="text-xs text-slate-500">${user.role}</p>
        </div>
        <button onclick="removeMember(this)" class="opacity-0 group-hover:opacity-100 text-slate-400 hover:text-red-500 transition-all p-1">
            <span class="material-symbols-outlined text-xl">do_not_disturb_on</span>
        </button>
    `;
    list.appendChild(item);

    updateMemberCount();
    window.toggleDropdown('memberSearchDropdown');
    document.getElementById('memberSearchInput').value = '';
};

window.removeMember = function (btn) {
    const item = btn.closest('[data-user-id]');
    const id = parseInt(item.dataset.userId);
    selectedMemberIds = selectedMemberIds.filter(mid => mid !== id);
    item.remove();
    updateMemberCount();
};

function updateMemberCount() {
    const badge = document.getElementById('memberCountBadge');
    if (badge) badge.innerText = `${selectedMemberIds.length} Selected`;
}

window.handleFileSelect = function (input) {
    const fileList = document.getElementById('fileList');
    if (!fileList) return;
    const files = Array.from(input.files);

    files.forEach(file => {
        const fileItem = document.createElement('div');
        fileItem.className = 'flex items-center justify-between p-3 bg-slate-50 dark:bg-slate-800/50 rounded-xl border border-slate-100 dark:border-slate-700 group';
        fileItem.innerHTML = `
            <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-lg bg-white dark:bg-slate-800 flex items-center justify-center text-primary border border-slate-200 dark:border-slate-700">
                    <span class="material-symbols-outlined">description</span>
                </div>
                <div>
                    <p class="text-sm font-bold text-slate-900 dark:text-white">${file.name}</p>
                    <p class="text-[10px] text-slate-400 uppercase font-bold">${(file.size / (1024 * 1024)).toFixed(2)} MB</p>
                </div>
            </div>
            <button onclick="this.parentElement.remove()" class="text-slate-400 hover:text-red-500 transition-colors">
                <span class="material-symbols-outlined">delete</span>
            </button>
        `;
        fileList.appendChild(fileItem);
    });
};

window.initWorkgroupDrawer = function () {
    setTimeout(() => {
        if ($('#workgroupDescription').length) {
            $('#workgroupDescription').summernote({
                height: 200,
                placeholder: 'Provide context about the goals and scope of this workgroup...',
                toolbar: [
                    ['style', ['style']],
                    ['font', ['bold', 'underline', 'clear']],
                    ['fontname', ['fontname']],
                    ['color', ['color']],
                    ['para', ['ul', 'ol', 'paragraph']],
                    ['table', ['table']],
                    ['insert', ['link', 'picture']],
                    ['view', ['fullscreen', 'codeview', 'help']]
                ]
            });
        }

        // Populate Members search dropdown
        const memResults = document.getElementById('memberSearchResults');
        if (memResults) {
            memResults.innerHTML = userDB.map(u => `
                <div onclick="addMember(${u.id})" class="flex items-center gap-3 p-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 cursor-pointer transition-colors">
                    <div class="size-8 rounded-full bg-cover" style='background-image: url("${u.img}")'></div>
                    <div><p class="text-sm font-bold dark:text-white">${u.name}</p><p class="text-[10px] text-slate-500">${u.role}</p></div>
                </div>
            `).join('');
        }

        // Populate Managers search dropdown
        const mgrResults = document.getElementById('managerSearchResults');
        if (mgrResults) {
            mgrResults.innerHTML = userDB.map(u => `
                <div onclick="addManager(${u.id})" class="flex items-center gap-3 p-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 cursor-pointer transition-colors">
                    <div class="size-8 rounded-full bg-cover" style='background-image: url("${u.img}")'></div>
                    <div><p class="text-sm font-bold dark:text-white">${u.name}</p><p class="text-[10px] text-slate-500">${u.role}</p></div>
                </div>
            `).join('');
        }

        // Close dropdowns on outside click
        document.addEventListener('click', (e) => {
            if (!e.target.closest('#managerSelectionContainer')) {
                document.getElementById('managerSearchDropdown')?.classList.add('hidden');
            }
            if (!e.target.closest('.relative')) {
                document.getElementById('memberSearchDropdown')?.classList.add('hidden');
            }
        });

        // Search logic
        document.getElementById('memberSearchInput')?.addEventListener('input', (e) => {
            const term = e.target.value.toLowerCase();
            const filtered = userDB.filter(u => u.name.toLowerCase().includes(term));
            const results = document.getElementById('memberSearchResults');
            if (results) {
                results.innerHTML = filtered.map(u => `
                    <div onclick="addMember(${u.id})" class="flex items-center gap-3 p-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 cursor-pointer transition-colors">
                        <div class="size-8 rounded-full bg-cover" style='background-image: url("${u.img}")'></div>
                        <div><p class="text-sm font-bold dark:text-white">${u.name}</p><p class="text-[10px] text-slate-500">${u.role}</p></div>
                    </div>
                `).join('');
            }
        });

        document.getElementById('managerSearchInput')?.addEventListener('input', (e) => {
            const term = e.target.value.toLowerCase();
            const filtered = userDB.filter(u => u.name.toLowerCase().includes(term));
            const results = document.getElementById('managerSearchResults');
            if (results) {
                results.innerHTML = filtered.map(u => `
                    <div onclick="addManager(${u.id})" class="flex items-center gap-3 p-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 cursor-pointer transition-colors">
                        <div class="size-8 rounded-full bg-cover" style='background-image: url("${u.img}")'></div>
                        <div><p class="text-sm font-bold dark:text-white">${u.name}</p><p class="text-[10px] text-slate-500">${u.role}</p></div>
                    </div>
                `).join('');
            }
        });

        window.updateStepUI();
    }, 100);
};

$(document).ready(function () {
    if (document.getElementById('createWorkgroupContainer') && !window.isDrawerLoading) {
        window.initWorkgroupDrawer();
    }
});
