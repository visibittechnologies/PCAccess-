/**
 * Task Drawer Management
 * This script handles opening the Create Task drawer via AJAX across all pages.
 */

document.addEventListener('DOMContentLoaded', function () {
    const sideDrawerContainer = document.getElementById('sideDrawerContainer');
    const drawerContent = document.getElementById('drawerContent');
    const drawerOverlay = document.getElementById('drawerOverlay');

    if (!sideDrawerContainer || !drawerContent || !drawerOverlay) return;

    function openDrawer(url = '/Admin/CreateTask', taskName = null) {
        // If already on the target page, don't open as a drawer
        if (window.location.pathname.toLowerCase() === url.toLowerCase()) return;

        // Show loading state could be added here
        fetch(url)
            .then(response => response.text())
            .then(html => {
                const parser = new DOMParser();
                const doc = parser.parseFromString(html, 'text/html');

                // Try to find specific containers by ID first, then fallback to classes
                const innerContent = doc.querySelector('#taskDetailContainer, #createTaskContainer, #createWorkgroupContainer, .w-full.max-w-7xl, .w-full.max-w-6xl, .w-full.max-w\\[850px\\], .w-full.max-w\\[650px\\]');

                if (innerContent) {
                    // Remove fixed positioning and layout classes that interfere with drawer container
                    innerContent.classList.remove('fixed', 'inset-0', 'top-0', 'right-0', 'left-0', 'bottom-0', 'z-40', 'z-50', 'z-[60]', 'animate-slide-in-right', 'md:h-[95vh]', 'max-w-[1250px]', 'md:rounded-3xl', 'ml-auto', 'shadow-2xl');

                    // Reset drawer width
                    drawerContent.style.width = '';

                    // Detect width from classes
                    const widthMatch = innerContent.className.match(/lg:w-\[(\d+%)\]/);
                    const maxWMatch = innerContent.className.match(/max-w-\[(\d+px)\]/);

                    if (widthMatch) {
                        drawerContent.style.width = widthMatch[1];
                        innerContent.classList.remove(widthMatch[0]);
                    } else if (maxWMatch) {
                        drawerContent.style.width = maxWMatch[1];
                        innerContent.classList.remove(maxWMatch[0]);
                    } else if (innerContent.id === 'taskDetailContainer') {
                        // Fallback for taskDetailContainer if no class found
                        drawerContent.style.width = '80%';
                    } else if (innerContent.id === 'createTaskContainer') {
                        drawerContent.style.width = '1250px';
                        innerContent.style.width = '100%';
                        innerContent.style.maxWidth = 'none';
                    }

                    drawerContent.innerHTML = innerContent.outerHTML;

                    // Dynamically update the task title if provided
                    if (taskName) {
                        const titleEl = drawerContent.querySelector('#taskHeaderTitle');
                        if (titleEl) {
                            titleEl.innerText = taskName;
                        }
                    }

                    sideDrawerContainer.classList.add('active');

                    // Initialize specialized logic if applicable
                    if (window.initCreateTask) {
                        window.initCreateTask();
                    }
                    if (window.initChecklist) {
                        window.initChecklist();
                    }
                    if (window.initWorkgroupDrawer) {
                        window.initWorkgroupDrawer();
                    }
                    if (window.initTaskDetail) {
                        window.initTaskDetail();
                    }

                    // Add close event listeners
                    setupCloseEvents();
                }
            })
            .catch(error => console.error('Error loading drawer content:', error));
    }

    function closeDrawer() {
        sideDrawerContainer.classList.remove('active');
        // Clear content after animation to free up memory
        setTimeout(() => {
            drawerContent.innerHTML = '';
        }, 400);
    }

    function setupCloseEvents() {
        // Find close icon button by searching for "close" or "X" in material symbols
        const allButtons = drawerContent.querySelectorAll('button');

        allButtons.forEach(btn => {
            const text = btn.innerText.trim().toLowerCase();
            const id = btn.id;
            const hasCloseIcon = btn.querySelector('.material-symbols-outlined')?.innerText.trim().toLowerCase() === 'close' ||
                btn.querySelector('.text-3xl')?.innerText.trim() === '×' ||
                btn.querySelector('.text-3xl')?.innerText.toLowerCase() === 'x';

            if (text === 'cancel' || id === 'btnCloseDrawer' || id === 'btnCancelTask' || hasCloseIcon) {
                btn.addEventListener('click', closeDrawer);
            }
        });
    }

    // Global listener for Drawer triggers
    document.addEventListener('click', function (e) {
        const trigger = e.target.closest('#btnCreateTask, #btnCreateTaskEmpty, .btn-create-task, [data-drawer-url]');
        if (trigger) {
            e.preventDefault();
            const url = trigger.dataset.drawerUrl || '/Admin/CreateTask';

            // Extract task name if clicking on a task container (card or row)
            let taskName = null;
            const container = trigger.closest('.task-card, tr, .task-item');

            if (container) {
                // Try specific candidates in order of precision: custom class, then h4, then specific span
                const titleEl = container.querySelector('.task-title-text, h4, .text-navy-dark');
                if (titleEl) {
                    taskName = titleEl.innerText.trim();
                }
            }

            openDrawer(url, taskName);
        }
    });

    // Close on overlay click
    drawerOverlay.addEventListener('click', closeDrawer);

    // Close on Escape key
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && sideDrawerContainer.classList.contains('active')) {
            closeDrawer();
        }
    });
});
