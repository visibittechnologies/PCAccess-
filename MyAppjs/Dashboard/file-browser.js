/**
 * FileBrowserManager - Remote File and Folder Browser
 * Step 4 Implementation: Remote folder navigation, dual view (Table/Grid),
 * instant in-memory search, column sorting, breadcrumbs, history navigation,
 * and clean pagination.
 */
var FileBrowserManager = (function ($) {
    "use strict";

    // State
    var currentFolderGuid = "";
    var rootFolderName = "";
    var deviceName = "";
    var currentRelativePath = "";
    var rawItems = [];
    var filteredItems = [];
    var viewMode = localStorage.getItem("pcaccess_browser_view") || "table"; // 'table' or 'grid'
    var sortColumn = "name";
    var sortDirection = "asc";
    var searchQuery = "";
    var currentPage = 1;
    var pageSize = 50;
    var canDownload = false; // Step 5: set from server-side permission
    var canUpload = false;   // Step 6: set from server-side permission
    var selectedUploadFile = null;
    var currentUploadXhr = null;
    var isUploading = false;
    var allowedExtensions = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".txt", ".jpg", ".jpeg", ".png", ".gif", ".zip", ".mp4", ".mp3", ".json", ".xml"];
    var maxUploadSizeBytes = 100 * 1024 * 1024; // 100 MB

    // =========================================================================
    // 1. Initialization
    // =========================================================================
    function init(folderGuid, folderName, devName, initialPath, canDl, canUp) {
        currentFolderGuid = folderGuid;
        rootFolderName = folderName || "Shared Folder";
        deviceName = devName || "Host PC";
        currentRelativePath = (initialPath || "").trim();
        canDownload = (canDl === true || canDl === "true");
        canUpload = (canUp === true || canUp === "true");

        initUI();
        initUploadUI();
        initHistory();
        loadDirectory(currentRelativePath, false);
    }

    function initUI() {
        // Search Input
        $("#txtSearch").on("input", function () {
            searchQuery = $(this).val().trim().toLowerCase();
            if (searchQuery.length > 0) {
                $("#btnClearSearch").removeClass("hidden");
            } else {
                $("#btnClearSearch").addClass("hidden");
            }
            currentPage = 1;
            applyFilterAndSort();
        });

        $("#btnClearSearch").on("click", function () {
            $("#txtSearch").val("");
            searchQuery = "";
            $(this).addClass("hidden");
            currentPage = 1;
            applyFilterAndSort();
        });

        // Up Button
        $("#btnNavUp").on("click", function () {
            navUp();
        });

        // Refresh Button
        $("#btnRefreshDirectory").on("click", function () {
            loadDirectory(currentRelativePath, false);
        });

        // View Mode Toggles
        $("#btnViewTable").on("click", function () {
            setViewMode("table");
        });
        $("#btnViewGrid").on("click", function () {
            setViewMode("grid");
        });

        // Apply saved view mode UI
        applyViewModeButtons();
    }

    function initHistory() {
        window.addEventListener("popstate", function (event) {
            var path = (event.state && event.state.path !== undefined) ? event.state.path : getQueryParam("path") || "";
            loadDirectory(path, false);
        });
    }

    function getQueryParam(name) {
        var urlParams = new URLSearchParams(window.location.search);
        return urlParams.get(name);
    }

    // =========================================================================
    // 2. Load Directory Contents via AJAX (SignalR Agent Resolution)
    // =========================================================================
    function loadDirectory(relPath, pushHistory) {
        if (pushHistory === undefined) pushHistory = true;

        relPath = (relPath || "").replace(/\\/g, "/").replace(/^\/+|\/+$/g, "");
        currentRelativePath = relPath;

        // Update Up button state
        $("#btnNavUp").prop("disabled", !currentRelativePath);

        // Update Breadcrumb UI immediately
        renderBreadcrumb();

        // Update URL History
        if (pushHistory) {
            var newUrl = "/Admin/FileBrowser?folderGuid=" + currentFolderGuid + (relPath ? "&path=" + encodeURIComponent(relPath) : "");
            window.history.pushState({ path: relPath }, "", newUrl);
        }

        // Show Loading State
        showState("loading");

        var payload = {
            folder_guid: currentFolderGuid,
            relative_path: relPath
        };

        $.ajax({
            url: "/FolderAPI/GetDirectoryContents",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(payload),
            success: function (res) {
                if (res && res.status === "success" && res.data) {
                    rawItems = res.data.items || [];
                    currentPage = 1;
                    applyFilterAndSort();
                } else {
                    var errorMsg = (res && res.message) ? res.message : "Unable to read directory.";
                    var isOffline = res && res.is_device_offline;
                    showErrorState(errorMsg, isOffline);
                }
            },
            error: function (xhr, status, error) {
                showErrorState("Server connection error: " + (error || "Request failed. Please try again."));
            }
        });
    }

    // =========================================================================
    // 3. Filtering, Sorting & Pagination
    // =========================================================================
    function applyFilterAndSort() {
        // 1. Filter
        if (searchQuery) {
            filteredItems = rawItems.filter(function (item) {
                return item.name.toLowerCase().indexOf(searchQuery) >= 0;
            });
        } else {
            filteredItems = rawItems.slice();
        }

        // 2. Sort (Folders always on top, then sorted by chosen column)
        filteredItems.sort(function (a, b) {
            if (a.is_folder && !b.is_folder) return -1;
            if (!a.is_folder && b.is_folder) return 1;

            var valA, valB;
            if (sortColumn === "size") {
                valA = a.size_bytes;
                valB = b.size_bytes;
            } else if (sortColumn === "date") {
                valA = new Date(a.last_modified).getTime();
                valB = new Date(b.last_modified).getTime();
            } else if (sortColumn === "type") {
                valA = (a.type || "").toLowerCase();
                valB = (b.type || "").toLowerCase();
            } else {
                // name
                valA = (a.name || "").toLowerCase();
                valB = (b.name || "").toLowerCase();
            }

            if (valA < valB) return sortDirection === "asc" ? -1 : 1;
            if (valA > valB) return sortDirection === "asc" ? 1 : -1;
            return 0;
        });

        // 3. Update Counts Badge
        var folderCount = rawItems.filter(function (i) { return i.is_folder; }).length;
        var fileCount = rawItems.filter(function (i) { return !i.is_folder; }).length;
        var totalBytes = rawItems.filter(function (i) { return !i.is_folder; }).reduce(function (sum, i) { return sum + (i.size_bytes || 0); }, 0);
        var sizeFormatted = formatBytes(totalBytes);

        $("#lblItemCount").html(
            '<span><strong class="text-slate-700">' + folderCount + '</strong> folders, <strong class="text-slate-700">' + fileCount + '</strong> files (' + sizeFormatted + ')</span>'
        );

        // 4. Determine State (Content / Empty / Search Empty)
        if (rawItems.length === 0) {
            showState("empty");
            return;
        }

        if (filteredItems.length === 0 && searchQuery) {
            $("#lblSearchQuery").text('"' + searchQuery + '"');
            showState("emptySearch");
            return;
        }

        showState("content");
        renderCurrentPage();
    }

    function renderCurrentPage() {
        var total = filteredItems.length;
        var totalPages = Math.ceil(total / pageSize) || 1;
        if (currentPage > totalPages) currentPage = totalPages;
        if (currentPage < 1) currentPage = 1;

        var startIdx = (currentPage - 1) * pageSize;
        var endIdx = Math.min(startIdx + pageSize, total);
        var pageItems = filteredItems.slice(startIdx, endIdx);

        if (viewMode === "grid") {
            renderGridView(pageItems);
        } else {
            renderTableView(pageItems);
        }

        renderPagination(total, totalPages, startIdx, endIdx);
    }

    // =========================================================================
    // 4. Render Table View
    // =========================================================================
    function renderTableView(items) {
        var html = '';

        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            var iconInfo = getIconMarkup(item.icon_type, item.is_folder);

            if (item.is_folder) {
                // Folder Row (Clickable)
                html += '<tr class="group hover:bg-emerald-50/50 cursor-pointer transition-colors" onclick="FileBrowserManager.openFolder(\'' + escapeJsString(item.relative_path) + '\')">';
                html += '  <td class="py-3 px-4 sm:px-5">';
                html += '    <div class="flex items-center gap-3">';
                html += '      <div class="w-8 h-8 rounded-lg bg-amber-50 text-amber-600 border border-amber-200/80 flex items-center justify-center shrink-0 shadow-2xs group-hover:scale-105 transition-transform">';
                html += '        ' + iconInfo.iconHtml;
                html += '      </div>';
                html += '      <span class="font-bold text-slate-900 group-hover:text-primary transition-colors select-none">' + escapeHtml(item.name) + '</span>';
                html += '    </div>';
                html += '  </td>';
                html += '  <td class="py-3 px-4 text-slate-500 font-medium hidden sm:table-cell">Folder</td>';
                html += '  <td class="py-3 px-4 text-right text-slate-400 font-mono">-</td>';
                html += '  <td class="py-3 px-4 text-right text-slate-500 hidden md:table-cell">' + escapeHtml(item.last_modified_formatted) + '</td>';
                html += '  <td class="py-3 px-4 text-center">';
                html += '    <span class="text-[11px] text-slate-400 italic">Click to open</span>';
                html += '  </td>';
                html += '</tr>';
            } else {
                // File Row
                html += '<tr class="hover:bg-slate-50/70 transition-colors select-none">';
                html += '  <td class="py-3 px-4 sm:px-5">';
                html += '    <div class="flex items-center gap-3">';
                html += '      <div class="w-8 h-8 rounded-lg ' + iconInfo.bgClass + ' ' + iconInfo.textClass + ' border ' + iconInfo.borderClass + ' flex items-center justify-center shrink-0 shadow-2xs">';
                html += '        ' + iconInfo.iconHtml;
                html += '      </div>';
                html += '      <div class="min-w-0 flex-1 cursor-pointer" onclick="FileBrowserManager.openFile(\'' + escapeJsString(item.relative_path) + '\', \'' + escapeJsString(item.name) + '\')" title="Click to Open">';
                html += '        <span class="font-semibold text-slate-800 hover:text-blue-600 transition-colors truncate block">' + escapeHtml(item.name) + '</span>';
                html += '        <span class="text-[11px] text-slate-400 sm:hidden block">' + escapeHtml(item.type) + ' &bull; ' + escapeHtml(item.size_formatted) + '</span>';
                html += '      </div>';
                html += '    </div>';
                html += '  </td>';
                html += '  <td class="py-3 px-4 text-slate-500 font-medium hidden sm:table-cell">' + escapeHtml(item.type) + '</td>';
                html += '  <td class="py-3 px-4 text-right text-slate-700 font-mono font-medium">' + escapeHtml(item.size_formatted) + '</td>';
                html += '  <td class="py-3 px-4 text-right text-slate-500 hidden md:table-cell">' + escapeHtml(item.last_modified_formatted) + '</td>';
                html += '  <td class="py-3 px-4 text-center">';
                html += '    <div class="inline-flex items-center gap-1.5 justify-center">';
                html += '      <button type="button" onclick="event.stopPropagation(); FileBrowserManager.openFile(\'' + escapeJsString(item.relative_path) + '\', \'' + escapeJsString(item.name) + '\')" title="Open in new tab" class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-semibold transition-colors shadow-2xs">';
                html += '        <i data-lucide="eye" class="w-3.5 h-3.5"></i> <span>Open</span>';
                html += '      </button>';
                if (canDownload) {
                    html += '      <button type="button" onclick="event.stopPropagation(); FileBrowserManager.downloadFile(\'' + escapeJsString(item.relative_path) + '\', \'' + escapeJsString(item.name) + '\')" title="Download file" class="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-blue-50 hover:bg-blue-100 text-blue-700 border border-blue-200 text-xs font-semibold transition-colors shadow-2xs">';
                    html += '        <i data-lucide="download" class="w-3.5 h-3.5"></i> <span>Download</span>';
                    html += '      </button>';
                }
                html += '    </div>';
                html += '  </td>';
                html += '</tr>';
            }
        }

        $('#browserTableBody').html(html);
        updateSortIcons();
        if (window.lucide) lucide.createIcons();
    }

    // =========================================================================
    // 5. Render Grid / Card View
    // =========================================================================
    function renderGridView(items) {
        var html = '';

        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            var iconInfo = getIconMarkup(item.icon_type, item.is_folder);

            if (item.is_folder) {
                // Folder Tile
                html += '<div class="group p-3.5 bg-white hover:bg-emerald-50/50 rounded-xl border border-slate-200/80 hover:border-emerald-300 shadow-2xs hover:shadow-sm cursor-pointer transition-all flex flex-col justify-between" onclick="FileBrowserManager.openFolder(\'' + escapeJsString(item.relative_path) + '\')">';
                html += '  <div class="flex items-start justify-between gap-2">';
                html += '    <div class="w-10 h-10 rounded-xl bg-amber-50 text-amber-600 border border-amber-200 flex items-center justify-center shrink-0 group-hover:scale-105 transition-transform">';
                html += '      <i data-lucide="folder" class="w-5 h-5"></i>';
                html += '    </div>';
                html += '    <span class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-amber-50 text-amber-700 border border-amber-200">DIR</span>';
                html += '  </div>';
                html += '  <div class="mt-3">';
                html += '    <h4 class="font-bold text-xs text-slate-900 group-hover:text-primary transition-colors truncate" title="' + escapeHtml(item.name) + '">' + escapeHtml(item.name) + '</h4>';
                html += '    <p class="text-[10px] text-slate-400 mt-0.5">' + escapeHtml(item.last_modified_formatted) + '</p>';
                html += '  </div>';
                html += '</div>';
            } else {
                // File Tile
                html += '<div class="group p-3.5 bg-white hover:border-slate-300 rounded-xl border border-slate-200/80 shadow-2xs hover:shadow-xs transition-all flex flex-col justify-between">';
                html += '  <div class="cursor-pointer" onclick="FileBrowserManager.openFile(\'' + escapeJsString(item.relative_path) + '\', \'' + escapeJsString(item.name) + '\')" title="Click to Open">';
                html += '    <div class="flex items-start justify-between gap-2">';
                html += '      <div class="w-10 h-10 rounded-xl ' + iconInfo.bgClass + ' ' + iconInfo.textClass + ' border ' + iconInfo.borderClass + ' flex items-center justify-center shrink-0 group-hover:scale-105 transition-transform">';
                html += '        ' + iconInfo.iconHtml;
                html += '      </div>';
                html += '      <span class="text-[10px] font-bold font-mono px-1.5 py-0.5 rounded bg-slate-100 text-slate-600 border border-slate-200">' + (escapeHtml(item.extension).replace(".", "").toUpperCase() || "FILE") + '</span>';
                html += '    </div>';
                html += '    <div class="mt-3">';
                html += '      <h4 class="font-bold text-xs text-slate-800 group-hover:text-primary transition-colors truncate" title="' + escapeHtml(item.name) + '">' + escapeHtml(item.name) + '</h4>';
                html += '      <p class="text-[11px] text-slate-500 font-mono mt-0.5">' + escapeHtml(item.size_formatted) + '</p>';
                html += '    </div>';
                html += '  </div>';
                html += '  <div class="mt-3 pt-2.5 border-t border-slate-100 flex items-center gap-1.5">';
                html += '    <button type="button" onclick="event.stopPropagation(); FileBrowserManager.openFile(\'' + escapeJsString(item.relative_path) + '\', \'' + escapeJsString(item.name) + '\')" title="Open in new tab" class="flex-1 inline-flex items-center justify-center gap-1 px-2 py-1.5 rounded-lg bg-slate-100 hover:bg-slate-200 text-slate-700 text-[11px] font-semibold transition-colors shadow-2xs">';
                html += '      <i data-lucide="eye" class="w-3.5 h-3.5"></i> <span>Open</span>';
                html += '    </button>';
                if (canDownload) {
                    html += '    <button type="button" onclick="event.stopPropagation(); FileBrowserManager.downloadFile(\'' + escapeJsString(item.relative_path) + '\', \'' + escapeJsString(item.name) + '\')" title="Download file" class="flex-1 inline-flex items-center justify-center gap-1 px-2 py-1.5 rounded-lg bg-blue-50 hover:bg-blue-100 text-blue-700 border border-blue-200 text-[11px] font-semibold transition-colors shadow-2xs">';
                    html += '      <i data-lucide="download" class="w-3.5 h-3.5"></i> <span>Download</span>';
                    html += '    </button>';
                }
                html += '  </div>';
                html += '</div>';
            }
        }

        $('#gridViewWrapper').html(html);
        if (window.lucide) lucide.createIcons();
    }

    // =========================================================================
    // 6. Navigation (Open Folder, Up, Breadcrumb)
    // =========================================================================
    function openFolder(itemRelativePath) {
        $("#txtSearch").val("");
        searchQuery = "";
        $("#btnClearSearch").addClass("hidden");
        loadDirectory(itemRelativePath, true);
    }

    function navUp() {
        if (!currentRelativePath) return;

        var parts = currentRelativePath.split("/");
        parts.pop();
        var parentPath = parts.join("/");
        loadDirectory(parentPath, true);
    }

    function renderBreadcrumb() {
        var $list = $("#breadcrumbList");
        var html = '';

        // Home
        html += '<li class="flex items-center gap-1.5">';
        html += '  <a href="/Admin/DashboardOverview" class="hover:text-primary transition-colors flex items-center gap-1">';
        html += '    <i data-lucide="home" class="w-3.5 h-3.5"></i>';
        html += '    <span>Dashboard</span>';
        html += '  </a>';
        html += '</li>';
        html += '<li class="text-slate-300">/</li>';

        // Device
        html += '<li class="flex items-center gap-1.5">';
        html += '  <a href="/Admin/DeviceFolders" class="hover:text-primary transition-colors flex items-center gap-1">';
        html += '    <i data-lucide="monitor" class="w-3.5 h-3.5"></i>';
        html += '    <span>' + escapeHtml(deviceName) + '</span>';
        html += '  </a>';
        html += '</li>';
        html += '<li class="text-slate-300">/</li>';

        // Root Shared Folder
        if (!currentRelativePath) {
            html += '<li class="flex items-center gap-1.5 font-bold text-slate-900">';
            html += '  <i data-lucide="folder-open" class="w-3.5 h-3.5 text-primary"></i>';
            html += '  <span>' + escapeHtml(rootFolderName) + '</span>';
            html += '</li>';
        } else {
            html += '<li class="flex items-center gap-1.5">';
            html += '  <a href="javascript:void(0)" onclick="FileBrowserManager.loadDirectory(\'\')" class="hover:text-primary transition-colors flex items-center gap-1 font-semibold">';
            html += '    <i data-lucide="folder" class="w-3.5 h-3.5 text-primary"></i>';
            html += '    <span>' + escapeHtml(rootFolderName) + '</span>';
            html += '  </a>';
            html += '</li>';

            // Subdirectories
            var segments = currentRelativePath.split("/");
            var accumulated = "";

            for (var i = 0; i < segments.length; i++) {
                var segment = segments[i];
                if (!segment) continue;

                accumulated += (accumulated ? "/" : "") + segment;
                var isLast = (i === segments.length - 1);

                html += '<li class="text-slate-300">/</li>';

                if (isLast) {
                    html += '<li class="flex items-center gap-1.5 font-bold text-slate-900">';
                    html += '  <i data-lucide="folder-open" class="w-3.5 h-3.5 text-primary"></i>';
                    html += '  <span>' + escapeHtml(segment) + '</span>';
                    html += '</li>';
                } else {
                    html += '<li class="flex items-center gap-1.5">';
                    html += '  <a href="javascript:void(0)" onclick="FileBrowserManager.loadDirectory(\'' + escapeJsString(accumulated) + '\')" class="hover:text-primary transition-colors font-medium">';
                    html += '    <span>' + escapeHtml(segment) + '</span>';
                    html += '  </a>';
                    html += '</li>';
                }
            }
        }

        $list.html(html);
        if (window.lucide) lucide.createIcons();
    }

    // =========================================================================
    // 7. Pagination Controls
    // =========================================================================
    function renderPagination(total, totalPages, startIdx, endIdx) {
        var $container = $("#browserPagination");

        if (total <= pageSize) {
            $container.addClass("hidden");
            return;
        }

        $container.removeClass("hidden");
        $("#paginationInfo").html('Showing <strong class="text-slate-800">' + (startIdx + 1) + '-' + endIdx + '</strong> of <strong class="text-slate-800">' + total + '</strong> items');

        var btnsHtml = "";

        // Prev
        btnsHtml += '<button type="button" onclick="FileBrowserManager.changePage(' + (currentPage - 1) + ')" ' + (currentPage <= 1 ? "disabled" : "") +
            ' class="px-2.5 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-700 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-50 transition-colors shadow-2xs font-semibold">' +
            '<i data-lucide="chevron-left" class="w-3.5 h-3.5 inline"></i> Prev' +
            '</button>';

        // Pages
        for (var p = 1; p <= totalPages; p++) {
            if (p === 1 || p === totalPages || (p >= currentPage - 2 && p <= currentPage + 2)) {
                var isActive = (p === currentPage);
                btnsHtml += '<button type="button" onclick="FileBrowserManager.changePage(' + p + ')" ' +
                    'class="w-7 h-7 rounded-lg text-xs font-bold transition-colors ' +
                    (isActive ? 'bg-primary text-white shadow-xs' : 'border border-slate-200 bg-white text-slate-700 hover:bg-slate-50') + '">' +
                    p + '</button>';
            } else if (p === currentPage - 3 || p === currentPage + 3) {
                btnsHtml += '<span class="text-slate-400 px-1">...</span>';
            }
        }

        // Next
        btnsHtml += '<button type="button" onclick="FileBrowserManager.changePage(' + (currentPage + 1) + ')" ' + (currentPage >= totalPages ? "disabled" : "") +
            ' class="px-2.5 py-1.5 rounded-lg border border-slate-200 bg-white text-slate-700 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-slate-50 transition-colors shadow-2xs font-semibold">' +
            'Next <i data-lucide="chevron-right" class="w-3.5 h-3.5 inline"></i>' +
            '</button>';

        $("#paginationButtons").html(btnsHtml);
        if (window.lucide) lucide.createIcons();
    }

    function changePage(page) {
        currentPage = page;
        renderCurrentPage();
        $("html, body").animate({ scrollTop: $("#browserContentContainer").offset().top - 120 }, 200);
    }

    // =========================================================================
    // 8. Sorting & View Mode Handlers
    // =========================================================================
    function changeSort(column) {
        if (sortColumn === column) {
            sortDirection = (sortDirection === "asc") ? "desc" : "asc";
        } else {
            sortColumn = column;
            sortDirection = "asc";
        }
        applyFilterAndSort();
    }

    function updateSortIcons() {
        var columns = ["name", "type", "size", "date"];
        for (var i = 0; i < columns.length; i++) {
            var col = columns[i];
            var $icon = $("#sortIcon" + col.charAt(0).toUpperCase() + col.slice(1));
            if (sortColumn === col) {
                var iconName = (sortDirection === "asc") ? "chevron-up" : "chevron-down";
                $icon.html('<i data-lucide="' + iconName + '" class="w-3.5 h-3.5 text-primary"></i>');
            } else {
                $icon.html('<i data-lucide="chevrons-up-down" class="w-3.5 h-3.5 text-slate-300"></i>');
            }
        }
        if (window.lucide) lucide.createIcons();
    }

    function setViewMode(mode) {
        viewMode = mode;
        localStorage.setItem("pcaccess_browser_view", mode);
        applyViewModeButtons();
        renderCurrentPage();
    }

    function applyViewModeButtons() {
        if (viewMode === "grid") {
            $("#btnViewGrid").removeClass("text-slate-500").addClass("bg-white text-slate-900 shadow-xs");
            $("#btnViewTable").removeClass("bg-white text-slate-900 shadow-xs").addClass("text-slate-500");
            $("#tableViewWrapper").addClass("hidden");
            $("#gridViewWrapper").removeClass("hidden");
        } else {
            $("#btnViewTable").removeClass("text-slate-500").addClass("bg-white text-slate-900 shadow-xs");
            $("#btnViewGrid").removeClass("bg-white text-slate-900 shadow-xs").addClass("text-slate-500");
            $("#gridViewWrapper").addClass("hidden");
            $("#tableViewWrapper").removeClass("hidden");
        }
    }

    // =========================================================================
    // 9. UI State Management
    // =========================================================================
    function showState(state) {
        $("#browserLoadingState, #browserErrorState, #browserEmptyState, #browserEmptySearchState, #tableViewWrapper, #gridViewWrapper, #browserPagination").addClass("hidden");

        if (state === "loading") {
            $("#browserLoadingState").removeClass("hidden");
        } else if (state === "empty") {
            $("#browserEmptyState").removeClass("hidden");
        } else if (state === "emptySearch") {
            $("#browserEmptySearchState").removeClass("hidden");
        } else if (state === "error") {
            $("#browserErrorState").removeClass("hidden");
        } else if (state === "content") {
            if (viewMode === "grid") {
                $("#gridViewWrapper").removeClass("hidden");
            } else {
                $("#tableViewWrapper").removeClass("hidden");
            }
        }
    }

    function showErrorState(message, isOffline) {
        showState("error");
        $("#browserErrorMessage").text(message || "Unable to read directory.");
        if (isOffline) {
            $("#browserErrorDetails").text("The host computer is offline or disconnected. Please verify that FileAccessAgent is running on the target PC.");
            $("#browserOfflineBanner").removeClass("hidden").addClass("flex");
        } else {
            $("#browserErrorDetails").text("The directory path may be invalid or access may be restricted on the remote machine.");
        }
    }

    // =========================================================================
    // 10. Helpers & Utilities
    // =========================================================================
    function getIconMarkup(iconType, isFolder) {
        if (isFolder) {
            return {
                iconHtml: '<i data-lucide="folder" class="w-4 h-4"></i>',
                bgClass: 'bg-amber-50',
                textClass: 'text-amber-600',
                borderClass: 'border-amber-200'
            };
        }

        switch (iconType) {
            case "pdf":
                return {
                    iconHtml: '<i data-lucide="file-text" class="w-4 h-4"></i>',
                    bgClass: 'bg-rose-50',
                    textClass: 'text-rose-600',
                    borderClass: 'border-rose-200'
                };
            case "word":
                return {
                    iconHtml: '<i data-lucide="file-text" class="w-4 h-4"></i>',
                    bgClass: 'bg-blue-50',
                    textClass: 'text-blue-600',
                    borderClass: 'border-blue-200'
                };
            case "excel":
                return {
                    iconHtml: '<i data-lucide="file-spreadsheet" class="w-4 h-4"></i>',
                    bgClass: 'bg-emerald-50',
                    textClass: 'text-emerald-600',
                    borderClass: 'border-emerald-200'
                };
            case "image":
                return {
                    iconHtml: '<i data-lucide="image" class="w-4 h-4"></i>',
                    bgClass: 'bg-purple-50',
                    textClass: 'text-purple-600',
                    borderClass: 'border-purple-200'
                };
            case "video":
                return {
                    iconHtml: '<i data-lucide="film" class="w-4 h-4"></i>',
                    bgClass: 'bg-pink-50',
                    textClass: 'text-pink-600',
                    borderClass: 'border-pink-200'
                };
            case "audio":
                return {
                    iconHtml: '<i data-lucide="music" class="w-4 h-4"></i>',
                    bgClass: 'bg-cyan-50',
                    textClass: 'text-cyan-600',
                    borderClass: 'border-cyan-200'
                };
            case "archive":
                return {
                    iconHtml: '<i data-lucide="archive" class="w-4 h-4"></i>',
                    bgClass: 'bg-amber-50',
                    textClass: 'text-amber-700',
                    borderClass: 'border-amber-200'
                };
            case "code":
                return {
                    iconHtml: '<i data-lucide="code" class="w-4 h-4"></i>',
                    bgClass: 'bg-indigo-50',
                    textClass: 'text-indigo-600',
                    borderClass: 'border-indigo-200'
                };
            default:
                return {
                    iconHtml: '<i data-lucide="file" class="w-4 h-4"></i>',
                    bgClass: 'bg-slate-50',
                    textClass: 'text-slate-600',
                    borderClass: 'border-slate-200'
                };
        }
    }

    function formatBytes(bytes) {
        if (!bytes || bytes <= 0) return "0 B";
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + " KB";
        if (bytes < 1024 * 1024 * 1024) return (bytes / (1024 * 1024)).toFixed(1) + " MB";
        return (bytes / (1024 * 1024 * 1024)).toFixed(2) + " GB";
    }

    function escapeHtml(text) {
        if (!text) return "";
        return $("<div>").text(text).html();
    }

    function escapeJsString(str) {
        if (!str) return "";
        return str.replace(/\\/g, "\\\\").replace(/'/g, "\\'");
    }


    // =========================================================================
    // 11. STEP 5: Secure File Open & Download
    // =========================================================================
    /**
     * WHAT: Opens / Previews a remote file in a new browser tab.
     * REASON: Allows direct inline reading for PDFs, images, text, and media.
     */
    function openFile(relativePath, fileName) {
        if (!currentFolderGuid || !relativePath) return;

        var openUrl = '/FolderAPI/DownloadFile'
            + '?folderGuid=' + encodeURIComponent(currentFolderGuid)
            + '&relativePath=' + encodeURIComponent(relativePath)
            + '&inline=true';

        window.open(openUrl, '_blank');
    }

    /**
     * WHAT: Triggers a secure download of a remote file to user local computer.
     * REASON: Uses direct link trigger for 100% reliable native browser download.
     */
    function downloadFile(relativePath, fileName) {
        if (!currentFolderGuid || !relativePath) return;

        // Show download toast
        var $toast = $('#downloadToast');
        $('#downloadToastFileName').text(fileName || 'Preparing download...');
        $('#downloadToastStatus').text('Connecting to host PC...');
        $toast.removeClass('hidden');

        var downloadUrl = '/FolderAPI/DownloadFile'
            + '?folderGuid=' + encodeURIComponent(currentFolderGuid)
            + '&relativePath=' + encodeURIComponent(relativePath)
            + '&inline=false';

        // Trigger native download dialog via anchor
        var link = document.createElement("a");
        link.href = downloadUrl;
        link.download = fileName || "download";
        link.target = "_self";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        setTimeout(function () {
            $('#downloadToastStatus').text('Transfer complete!');
        }, 2000);

        setTimeout(function () {
            $toast.addClass('hidden');
        }, 5000);
    }


    // =========================================================================
    // 12. STEP 6: Secure Remote File Upload
    // =========================================================================
    function initUploadUI() {
        // Open Modal Button
        $("#btnOpenUploadModal").on("click", function () {
            openUploadModal();
        });

        // Close Modal Buttons
        $("#btnCloseUploadModal, #btnCancelUpload").on("click", function () {
            closeUploadModal();
        });

        // File Browse trigger
        $("#uploadDropZone").on("click", function (e) {
            if (!isUploading) {
                $("#uploadFileInput").trigger("click");
            }
        });

        $("#uploadFileInput").on("click", function (e) {
            e.stopPropagation();
        });

        $("#uploadFileInput").on("change", function () {
            if (this.files && this.files.length > 0) {
                handleFileSelection(this.files[0]);
            }
        });

        // Drag & Drop handlers
        var $dropZone = $("#uploadDropZone");

        $dropZone.on("dragenter dragover", function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (!isUploading) {
                $dropZone.addClass("border-emerald-500 bg-emerald-50/40");
            }
        });

        $dropZone.on("dragleave drop", function (e) {
            e.preventDefault();
            e.stopPropagation();
            $dropZone.removeClass("border-emerald-500 bg-emerald-50/40");
        });

        $dropZone.on("drop", function (e) {
            if (!isUploading && e.originalEvent.dataTransfer && e.originalEvent.dataTransfer.files.length > 0) {
                handleFileSelection(e.originalEvent.dataTransfer.files[0]);
            }
        });

        // Remove selected file
        $("#btnRemoveSelectedFile").on("click", function (e) {
            e.stopPropagation();
            resetSelectedFile();
        });

        // Start Upload Button
        $("#btnStartUpload").on("click", function () {
            startUpload();
        });
    }

    function openUploadModal() {
        if (!canUpload) {
            alert("You do not have permission to upload files to this shared folder.");
            return;
        }

        // Update target destination display
        var targetDisplay = rootFolderName;
        if (currentRelativePath) {
            targetDisplay += " / " + currentRelativePath.replace(/\\/g, " / ").replace(/\//g, " / ");
        }
        $("#uploadTargetFolderDisplay").text(targetDisplay);

        resetUploadModal();
        $("#uploadModal").removeClass("hidden");
        if (window.lucide) lucide.createIcons();
    }

    function closeUploadModal() {
        if (isUploading) {
            if (confirm("An upload is currently in progress. Do you want to cancel it?")) {
                cancelUpload();
            } else {
                return;
            }
        }
        $("#uploadModal").addClass("hidden");
        resetUploadModal();
    }

    function resetUploadModal() {
        resetSelectedFile();
        $("#uploadValidationError").addClass("hidden");
        $("#uploadValidationMessage").text("");
        $("#uploadProgressSection").addClass("hidden");
        $("#uploadProgressBar").css("width", "0%");
        $("#uploadPercentText").text("0%");
        $("#uploadBytesText").text("0 KB / 0 KB");
        $("#uploadStatusText").text("Uploading to server...");
        $("#uploadStageText").text("Stage 1/2");
        $("#btnStartUpload").prop("disabled", true).removeClass("opacity-50");
        $("#btnCancelUpload").prop("disabled", false).text("Cancel");
        isUploading = false;
        currentUploadXhr = null;
    }

    function resetSelectedFile() {
        selectedUploadFile = null;
        $("#uploadFileInput").val("");
        $("#uploadSelectedFileCard").addClass("hidden");
        $("#uploadDropZone").removeClass("hidden");
        $("#btnStartUpload").prop("disabled", true);
    }

    function handleFileSelection(file) {
        if (!file) return;

        // 1. Validate file size
        if (file.size > maxUploadSizeBytes) {
            showUploadError("The selected file exceeds the maximum allowed upload size of 100 MB (" + formatBytes(file.size) + ").");
            resetSelectedFile();
            return;
        }

        // 2. Validate file extension
        var fileName = file.name || "";
        var ext = "." + (fileName.split(".").pop() || "").toLowerCase();
        var isAllowed = false;
        for (var i = 0; i < allowedExtensions.length; i++) {
            if (allowedExtensions[i].toLowerCase() === ext) {
                isAllowed = true;
                break;
            }
        }

        if (!isAllowed) {
            showUploadError("This file type ('" + ext + "') is not allowed for upload. Allowed formats: PDF, Word, Excel, CSV, Images, Zip, Media.");
            resetSelectedFile();
            return;
        }

        // Valid file chosen
        $("#uploadValidationError").addClass("hidden");
        selectedUploadFile = file;

        // Update Card UI
        $("#uploadFileName").text(file.name);
        $("#uploadFileSize").text(formatBytes(file.size));

        var iconMeta = getIconMarkup(ext.replace(".", ""), false);
        $("#uploadFileIconWrapper").html(iconMeta.iconHtml).attr("class", "w-9 h-9 rounded-lg flex items-center justify-center shrink-0 " + iconMeta.bgClass + " " + iconMeta.textClass);

        $("#uploadDropZone").addClass("hidden");
        $("#uploadSelectedFileCard").removeClass("hidden");
        $("#btnStartUpload").prop("disabled", false);

        if (window.lucide) lucide.createIcons();
    }

    function showUploadError(message) {
        $("#uploadValidationMessage").text(message || "An error occurred during file upload.");
        $("#uploadValidationError").removeClass("hidden");
        if (window.lucide) lucide.createIcons();
    }

    function startUpload() {
        if (!selectedUploadFile || isUploading) return;

        isUploading = true;
        $("#btnStartUpload").prop("disabled", true).addClass("opacity-50");
        $("#btnCancelUpload").text("Cancel Upload");
        $("#uploadValidationError").addClass("hidden");
        $("#uploadProgressSection").removeClass("hidden");
        $("#uploadProgressBar").css("width", "0%").removeClass("bg-rose-500").addClass("bg-emerald-500");
        $("#uploadStatusText").text("Streaming to server...");
        $("#uploadStageText").text("Stage 1/2");

        var conflictAction = $('input[name="conflictOption"]:checked').val() || "autorename";

        var formData = new FormData();
        formData.append("folderGuid", currentFolderGuid);
        formData.append("relativePath", currentRelativePath);
        formData.append("conflictAction", conflictAction);
        formData.append("file", selectedUploadFile);

        currentUploadXhr = new XMLHttpRequest();
        currentUploadXhr.open("POST", "/FolderAPI/UploadFile", true);

        // Upload progress (Client -> Server)
        currentUploadXhr.upload.onprogress = function (e) {
            if (e.lengthComputable) {
                var percent = Math.min(99, Math.round((e.loaded / e.total) * 100));
                $("#uploadProgressBar").css("width", percent + "%");
                $("#uploadPercentText").text(percent + "%");
                $("#uploadBytesText").text(formatBytes(e.loaded) + " / " + formatBytes(e.total));

                if (percent >= 99) {
                    $("#uploadStatusText").text("Finalizing file on host PC...");
                    $("#uploadStageText").text("Stage 2/2");
                }
            }
        };

        // Complete handler
        currentUploadXhr.onload = function () {
            isUploading = false;
            currentUploadXhr = null;

            try {
                var response = JSON.parse(this.responseText);
                if (this.status === 200 && response && response.status === "success") {
                    // Success!
                    $("#uploadProgressBar").css("width", "100%");
                    $("#uploadPercentText").text("100%");
                    $("#uploadStatusText").text("Upload completed!");

                    // Show success toast
                    var toastMsg = "File '" + (response.fileName || selectedUploadFile.name) + "' uploaded successfully.";
                    showUploadSuccessToast(toastMsg);

                    // Close modal and refresh directory list
                    setTimeout(function () {
                        $("#uploadModal").addClass("hidden");
                        resetUploadModal();
                        loadDirectory(currentRelativePath, false);
                    }, 800);
                } else {
                    var err = (response && response.message) ? response.message : ("Server returned status " + this.status);
                    $("#uploadProgressBar").removeClass("bg-emerald-500").addClass("bg-rose-500");
                    $("#uploadStatusText").text("Upload failed");
                    showUploadError(err);
                    $("#btnStartUpload").prop("disabled", false).removeClass("opacity-50");
                    $("#btnCancelUpload").text("Close");
                }
            } catch (ex) {
                $("#uploadProgressBar").removeClass("bg-emerald-500").addClass("bg-rose-500");
                $("#uploadStatusText").text("Upload failed");
                showUploadError("Unexpected server response: " + ex.message);
                $("#btnStartUpload").prop("disabled", false).removeClass("opacity-50");
                $("#btnCancelUpload").text("Close");
            }
        };

        currentUploadXhr.onerror = function () {
            isUploading = false;
            currentUploadXhr = null;
            $("#uploadProgressBar").removeClass("bg-emerald-500").addClass("bg-rose-500");
            $("#uploadStatusText").text("Network error");
            showUploadError("A network error occurred while uploading the file. Please verify your connection.");
            $("#btnStartUpload").prop("disabled", false).removeClass("opacity-50");
            $("#btnCancelUpload").text("Close");
        };

        currentUploadXhr.onabort = function () {
            isUploading = false;
            currentUploadXhr = null;
            $("#uploadProgressBar").removeClass("bg-emerald-500").addClass("bg-slate-400");
            $("#uploadStatusText").text("Upload cancelled");
            showUploadError("Upload was cancelled by user.");
            $("#btnStartUpload").prop("disabled", false).removeClass("opacity-50");
            $("#btnCancelUpload").text("Close");
        };

        currentUploadXhr.send(formData);
    }

    function cancelUpload() {
        if (currentUploadXhr) {
            currentUploadXhr.abort();
        }
        isUploading = false;
    }

    function showUploadSuccessToast(message) {
        var $toast = $("#downloadToast");
        $("#downloadToastFileName").text(message);
        $("#downloadToastStatus").text("Saved to remote PC");
        $toast.removeClass("hidden");
        setTimeout(function () {
            $toast.addClass("hidden");
        }, 5000);
    }

    // Public API
    return {
        init: init,
        loadDirectory: loadDirectory,
        openFolder: openFolder,
        changePage: changePage,
        changeSort: changeSort,
        openFile: openFile,
        downloadFile: downloadFile,
        openUploadModal: openUploadModal,
        cancelUpload: cancelUpload
    };

})(jQuery);




