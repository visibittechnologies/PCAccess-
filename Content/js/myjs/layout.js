// =========================================================================
// Layout & Global UI Interactivity Script
// Safe, defensive null-checked execution across all views
// =========================================================================

// --- 1. User Profile Dropdown ---
const userBtn = document.getElementById('userDropdownBtn');
const userMenu = document.getElementById('userDropdown');

if (userBtn && userMenu) {
    userBtn.addEventListener('click', (e) => {
        e.stopPropagation();
        const open = userMenu.classList.contains('opacity-100');
        userMenu.classList.toggle('opacity-100', !open);
        userMenu.classList.toggle('scale-100', !open);
        userMenu.classList.toggle('opacity-0', open);
        userMenu.classList.toggle('scale-95', open);
        userMenu.classList.toggle('invisible', open);
    });

    document.addEventListener('click', (e) => {
        if (!userBtn.contains(e.target) && !userMenu.contains(e.target)) {
            userMenu.classList.add('opacity-0', 'invisible', 'scale-95');
            userMenu.classList.remove('opacity-100', 'scale-100');
        }
    });
}

// --- 2. Category Multi-Select Filter (Optional, where present) ---
const selectBox = document.getElementById('selectBox');
const dropdownMenu = document.getElementById('dropdownMenu');
const arrowIcon = document.getElementById('arrowIcon');
const checkAll = document.getElementById('checkAll');
const items = document.querySelectorAll('.chkItem');
const selectedText = document.getElementById('selectedText');
const sidebarSections = document.querySelectorAll('[data-category]');

if (selectBox && dropdownMenu) {
    selectBox.addEventListener('click', (e) => {
        e.stopPropagation();
        dropdownMenu.classList.toggle('hidden');
        if (arrowIcon) arrowIcon.classList.toggle('rotate-180');
    });

    document.addEventListener('click', () => {
        dropdownMenu.classList.add('hidden');
        if (arrowIcon) arrowIcon.classList.remove('rotate-180');
    });

    dropdownMenu.addEventListener('click', (e) => e.stopPropagation());
}

if (checkAll && items && items.length > 0) {
    checkAll.addEventListener('change', function () {
        items.forEach(chk => chk.checked = checkAll.checked);
        updateSelected();
    });
}

if (items && items.length > 0) {
    items.forEach(chk => chk.addEventListener('change', updateSelected));
}

function updateSelected() {
    if (!items || items.length === 0) return;
    const selectedValues = [...items].filter(x => x.checked).map(x => x.value);
    try {
        localStorage.setItem('selectedCategories', JSON.stringify(selectedValues));
    } catch (err) { }

    if (selectedText) {
        if (selectedValues.length === 0) selectedText.textContent = 'Select Options';
        else if (selectedValues.length === items.length) selectedText.textContent = 'All Selected';
        else selectedText.textContent = selectedValues.length + ' Selected';
    }
    if (checkAll) {
        checkAll.checked = (selectedValues.length === items.length);
    }
    if (sidebarSections && sidebarSections.length > 0) {
        sidebarSections.forEach(section => {
            const category = section.getAttribute('data-category');
            if (category) {
                if (selectedValues.includes(category)) {
                    section.style.display = '';
                } else {
                    section.style.display = 'none';
                }
            }
        });
    }
}

window.addEventListener('DOMContentLoaded', () => {
    if (items && items.length > 0) {
        const savedCategories = localStorage.getItem('selectedCategories');
        if (savedCategories) {
            try {
                const selectedValues = JSON.parse(savedCategories);
                items.forEach(chk => { chk.checked = selectedValues.includes(chk.value); });
            } catch (err) {
                items.forEach(chk => chk.checked = true);
            }
        } else {
            items.forEach(chk => chk.checked = true);
        }
        updateSelected();
    }
});

// --- 3. Responsive Sidebar Navigation ---
const sidebarEl = document.getElementById('sidebar');
const menuBtnEl = document.getElementById('menuBtn');
const mainContentEl = document.getElementById('mainContent');
const pageContentEl = document.getElementById('pageContent');
const appHeaderEl = document.getElementById('appHeader');
const headerLogoEl = document.getElementById('headerLogo');
const headerLeftGroupEl = document.getElementById('headerLeftGroup');

function openSidebar() {
    if (!sidebarEl) return;
    sidebarEl.classList.remove('-translate-x-full');
    if (mainContentEl) mainContentEl.classList.add('md:pl-64');
    if (appHeaderEl) appHeaderEl.classList.add('md:left-64');
    if (headerLogoEl) headerLogoEl.classList.add('opacity-0', 'pointer-events-none', 'w-0', 'overflow-hidden');
    if (headerLeftGroupEl) {
        headerLeftGroupEl.classList.remove('gap-4');
        headerLeftGroupEl.classList.add('gap-0');
    }
}

function closeSidebar() {
    if (!sidebarEl) return;
    sidebarEl.classList.add('-translate-x-full');
    if (mainContentEl) mainContentEl.classList.remove('md:pl-64');
    if (appHeaderEl) appHeaderEl.classList.remove('md:left-64');
    if (headerLogoEl) headerLogoEl.classList.remove('opacity-0', 'pointer-events-none', 'w-0', 'overflow-hidden');
    if (headerLeftGroupEl) {
        headerLeftGroupEl.classList.remove('gap-0');
        headerLeftGroupEl.classList.add('gap-4');
    }
}

if (menuBtnEl && sidebarEl) {
    menuBtnEl.addEventListener('click', (e) => {
        e.stopPropagation();
        sidebarEl.classList.contains('-translate-x-full') ? openSidebar() : closeSidebar();
    });
}

document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && sidebarEl && !sidebarEl.classList.contains('-translate-x-full')) {
        closeSidebar();
    }
});

window.addEventListener('load', () => {
    if (sidebarEl) closeSidebar();
});

// --- 4. Sidebar Active Link Highlighting & Scroll Position ---
const sidebarLinks = document.querySelectorAll('.sidebar-link');
const sidebarScroll = document.getElementById('sidebarScroll');
const currentPath = window.location.pathname.toLowerCase();

function normalizePath(path) {
    if (!path) return '';
    let cleanPath = path.replace(/\/+$/, '');
    if (!cleanPath.startsWith('/')) cleanPath = '/' + cleanPath;
    cleanPath = cleanPath.replace(/^~\//, '/');
    return cleanPath.toLowerCase();
}

if (sidebarLinks && sidebarLinks.length > 0) {
    sidebarLinks.forEach(link => {
        const linkPath = normalizePath(link.getAttribute('href'));
        if (linkPath && (currentPath === linkPath || (linkPath !== '/' && currentPath.startsWith(linkPath + '/')))) {
            link.classList.add('bg-primary', 'text-white', 'font-semibold', 'active-menu');
            window.addEventListener('load', () => {
                if (sidebarScroll) {
                    const sidebarHeight = sidebarScroll.clientHeight;
                    const itemOffsetTop = link.offsetTop;
                    const itemHeight = link.offsetHeight;
                    const scrollTo = itemOffsetTop - (sidebarHeight / 2) + (itemHeight / 2);
                    sidebarScroll.scrollTo({ top: scrollTo, behavior: 'smooth' });
                }
            });
        }
        link.addEventListener('click', function () {
            if (sidebarScroll) {
                try {
                    localStorage.setItem('sidebarScrollTop', sidebarScroll.scrollTop);
                } catch (err) { }
            }
        });
    });
}

window.addEventListener('load', () => {
    if (sidebarScroll) {
        const savedScroll = localStorage.getItem('sidebarScrollTop');
        if (savedScroll !== null) {
            sidebarScroll.scrollTop = parseInt(savedScroll, 10);
        }
    }
});
