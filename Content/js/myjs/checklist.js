function updateChecklistProgress() {
    const checkboxes = document.querySelectorAll('.checklist-checkbox');
    const checked = Array.from(checkboxes).filter(cb => cb.checked).length;
    const total = checkboxes.length;
    const percent = total > 0 ? Math.round((checked / total) * 100) : 0;

    const progress = document.getElementById('checklistProgress');
    const percentLabel = document.getElementById('checklistPercent');

    if (progress) progress.style.width = `${percent}%`;
    if (percentLabel) percentLabel.innerText = `${percent}%`;
}

function addNewChecklistItem(input) {
    if (!input.value.trim()) return;

    const container = document.getElementById('checklistItemsContainer');
    if (!container) return;

    const newRow = document.createElement('div');
    newRow.className = "checklist-row flex items-center justify-between group py-4 border-b border-transparent hover:border-slate-50 dark:hover:border-slate-800 transition-all border-l-2 border-l-transparent hover:border-l-primary/30 pl-2 animate-slide-in-up";

    newRow.innerHTML = `
        <div class="flex items-center gap-5 flex-1 min-w-0">
            <label class="relative flex items-center justify-center cursor-pointer">
                <input type="checkbox" class="checklist-checkbox peer hidden" onchange="updateChecklistProgress()">
                <div class="size-6 rounded-lg border-2 border-slate-200 dark:border-slate-700 peer-checked:bg-primary peer-checked:border-primary flex items-center justify-center transition-all shadow-sm"></div>
                <span class="material-symbols-outlined text-white text-[18px] font-bold absolute opacity-0 peer-checked:opacity-100 pointer-events-none transition-all">check</span>
            </label>
            <span class="text-sm font-bold text-slate-700 dark:text-slate-300 peer-checked:text-slate-300 dark:peer-checked:text-slate-600 peer-checked:line-through truncate transition-all">${input.value.trim()}</span>
        </div>
        <div class="flex items-center gap-1 bg-white dark:bg-slate-800 rounded-xl p-1 border border-slate-100 dark:border-slate-700 shadow-sm">
            <button class="size-9 flex items-center justify-center rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-400 hover:text-primary transition-all">
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
    updateChecklistProgress();
}

window.initChecklist = updateChecklistProgress;

// Also listen for DOMContentLoaded for direct page visits
document.addEventListener('DOMContentLoaded', () => {
    if (document.getElementById('checklistItemsContainer')) {
        updateChecklistProgress();
    }
});
