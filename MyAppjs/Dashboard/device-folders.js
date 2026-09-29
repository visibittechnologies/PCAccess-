/*
================================================================================
MODULE: device-folders.js
AUTHOR: Antigravity / Senior .NET Developer
DATE: 2026-09-22
PURPOSE:
  Handles Step 3 Shared Folder management, SignalR real-time path validation 
  with the connected PC Agent, folder CRUD operations, and user permission matrices.

REASON & ARCHITECTURAL PATTERN:
  - Keeps JavaScript strictly separate from Razor views (MyAppjs/ pattern).
  - Browser never verifies physical paths: sends validation request to PC Agent via SignalR.
  - Granular permissions (View, Download, Upload, Delete) saved via FolderAPIController.
  - SweetAlert confirmations and toast notifications for modern, responsive UX.
================================================================================
*/

var DeviceFoldersManager = (function ($) {
    "use strict";

    var currentUserId = 0;
    var currentDeviceGuid = "";
    var currentValidationRequestId = null;
    var validationTimeout = null;
    var hub = null;
    var originalEditPath = "";

    function init(userId, deviceGuid) {
        currentUserId = userId;
        currentDeviceGuid = deviceGuid;

        initSignalR();
        bindEvents();
    }

    // =========================================================================
    // 1. SignalR Hub Initialization & Listeners
    // =========================================================================
    function initSignalR() {
        if (!$.connection || !$.connection.deviceHub) {
            console.warn("SignalR DeviceHub not loaded.");
            return;
        }

        hub = $.connection.deviceHub;

        // Listener: Real-time Device Online / Offline Status Change
        hub.client.deviceStatusChanged = function (data) {
            if (!data || !data.deviceGuid) return;

            if (data.deviceGuid.toLowerCase() === currentDeviceGuid.toLowerCase()) {
                updateDeviceStatusBadge(data.status);
            }
        };

        // Listener: Real-time Path Validation Result from PC Agent
        hub.client.folderValidationResult = function (requestId, isValid, message, normalizedPath) {
            console.log("Received folder validation result:", requestId, isValid, message, normalizedPath);
            if (requestId !== currentValidationRequestId) return;

            if (validationTimeout) {
                clearTimeout(validationTimeout);
                validationTimeout = null;
            }

            handleValidationResponse(isValid, message, normalizedPath);
        };

        // Connect
        $.connection.hub.start()
            .done(function () {
                console.log("DeviceFoldersManager connected to DeviceHub. ConnectionId:", $.connection.hub.id);
                if (currentUserId > 0) {
                    hub.server.joinDashboardGroup(currentUserId);
                }
            })
            .fail(function (err) {
                console.error("SignalR connection failed:", err);
            });
    }

    function updateDeviceStatusBadge(status) {
        var isOnline = (status === "online");
        var $badge = $("#pageDeviceStatusBadge");
        var $banner = $("#deviceOfflineBanner");

        if (isOnline) {
            $badge.html('<span class="relative flex h-2 w-2 mr-1.5">' +
                '<span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>' +
                '<span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>' +
                '</span><span>Online</span>');
            $badge.attr("class", "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-300 shadow-sm");
            $banner.addClass("hidden").removeClass("flex");
        } else {
            $badge.html('<span class="w-2 h-2 rounded-full bg-rose-500 mr-1.5"></span><span>Offline</span>');
            $badge.attr("class", "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-bold bg-rose-50 text-rose-700 border border-rose-200 shadow-sm");
            $banner.removeClass("hidden").addClass("flex");
        }
    }

    // =========================================================================
    // 2. Event Handlers & Modal Management
    // =========================================================================
    function bindEvents() {
        // Device Switcher dropdown
        $("#ddlDeviceSwitcher").on("change", function () {
            var selectedGuid = $(this).val();
            if (selectedGuid) {
                window.location.href = "/Admin/DeviceFolders?deviceGuid=" + selectedGuid;
            }
        });

        // Open Add Folder Modal
        $("#btnOpenAddFolderModal").on("click", function (e) {
            e.preventDefault();
            openAddFolderModal();
        });

        // Close Folder Modal
        $("#btnCloseFolderModal, #btnCancelFolderModal").on("click", function (e) {
            e.preventDefault();
            closeFolderModal();
        });

        // Validate Path Button click
        $("#btnValidatePath").on("click", function (e) {
            e.preventDefault();
            triggerPathValidation();
        });

        // Reset validation flag on local path change
        $("#txtLocalPath").on("input", function () {
            var newPath = $(this).val().trim();
            var folderId = parseInt($("#hdnFolderId").val() || "0", 10);

            if (folderId > 0 && newPath.toLowerCase() === originalEditPath.toLowerCase()) {
                $("#hdnIsPathValidated").val("true");
                $("#btnSaveSharedFolder").prop("disabled", false);
                $("#validationFeedback").addClass("hidden");
            } else {
                $("#hdnIsPathValidated").val("false");
                $("#btnSaveSharedFolder").prop("disabled", true);
                $("#validationFeedback").addClass("hidden");
            }
        });

        // Save Folder Button click
        $("#btnSaveSharedFolder").on("click", function (e) {
            e.preventDefault();
            submitFolderForm();
        });

        // Close Permissions Modal
        $("#btnClosePermModal, #btnDonePermModal").on("click", function (e) {
            e.preventDefault();
            $("#modalPermissions").addClass("hidden").removeClass("flex");
        });
    }

    // =========================================================================
    // 3. Real-Time Path Validation via SignalR Agent
    // =========================================================================
    function triggerPathValidation() {
        var path = $("#txtLocalPath").val().trim();
        var $btn = $("#btnValidatePath");
        var $btnText = $("#btnValidateText");
        var $feedback = $("#validationFeedback");

        if (!path) {
            $feedback.removeClass("hidden bg-emerald-50 border-emerald-200 text-emerald-800")
                .addClass("bg-rose-50 border-rose-200 text-rose-800")
                .html('<div class="flex items-center gap-1.5"><i data-lucide="alert-circle" class="w-4 h-4 text-rose-600"></i> Please enter a local folder path (e.g. D:\\CompanyFiles).</div>');
            if (window.lucide) lucide.createIcons();
            return;
        }

        // Proactive offline check
        var isOffline = $("#pageDeviceStatusBadge").text().toLowerCase().indexOf("offline") >= 0;
        if (isOffline) {
            $feedback.removeClass("hidden bg-emerald-50 border-emerald-200 text-emerald-800")
                .addClass("bg-amber-50 border-amber-200 text-amber-900")
                .html('<div class="flex items-center gap-1.5"><i data-lucide="alert-triangle" class="w-4 h-4 text-amber-600"></i> Target PC is offline. Please start FileAccessAgent on your PC to validate folder.</div>');
            if (window.lucide) lucide.createIcons();
            return;
        }

        // Set Loading State
        $btn.prop("disabled", true);
        $btnText.text("Validating on PC...");
        $feedback.removeClass("hidden bg-rose-50 border-rose-200 text-rose-800 bg-emerald-50 border-emerald-200 text-emerald-800")
            .addClass("bg-slate-50 border-slate-200 text-slate-700")
            .html('<div class="flex items-center gap-2"><span class="animate-spin w-3.5 h-3.5 border-2 border-primary border-t-transparent rounded-full"></span> Sending check request to PC Agent...</div>');

        currentValidationRequestId = "VAL_" + Date.now();

        // Dispatch SignalR command to Agent
        try {
            if (hub && hub.server && hub.server.validateFolderOnAgent) {
                hub.server.validateFolderOnAgent(currentDeviceGuid, path, currentValidationRequestId);
            } else {
                handleValidationResponse(false, "SignalR connection to server is inactive. Please refresh.", path);
                return;
            }
        } catch (err) {
            handleValidationResponse(false, "Could not contact SignalR hub: " + err.message, path);
            return;
        }

        // 8-second safety timeout
        validationTimeout = setTimeout(function () {
            handleValidationResponse(false, "Validation timed out. Ensure FileAccessAgent is running and connected.", path);
        }, 8000);
    }

    function handleValidationResponse(isValid, message, normalizedPath) {
        var $btn = $("#btnValidatePath");
        var $btnText = $("#btnValidateText");
        var $feedback = $("#validationFeedback");
        var $btnSave = $("#btnSaveSharedFolder");

        $btn.prop("disabled", false);
        $btnText.text("Validate Path");

        if (isValid) {
            $("#hdnIsPathValidated").val("true");
            if (normalizedPath) {
                $("#txtLocalPath").val(normalizedPath);
            }
            $btnSave.prop("disabled", false);

            $feedback.removeClass("hidden bg-rose-50 border-rose-200 text-rose-800 bg-slate-50 border-slate-200 text-slate-700 bg-amber-50 border-amber-200 text-amber-900")
                .addClass("bg-emerald-50 border-emerald-200 text-emerald-800")
                .html('<div class="flex items-center gap-2 font-medium"><i data-lucide="check-circle-2" class="w-4 h-4 text-emerald-600 shrink-0"></i> ' + (message || "Path verified on PC.") + '</div>');
        } else {
            $("#hdnIsPathValidated").val("false");
            $btnSave.prop("disabled", true);

            $feedback.removeClass("hidden bg-emerald-50 border-emerald-200 text-emerald-800 bg-slate-50 border-slate-200 text-slate-700 bg-amber-50 border-amber-200 text-amber-900")
                .addClass("bg-rose-50 border-rose-200 text-rose-800")
                .html('<div class="flex items-center gap-2 font-medium"><i data-lucide="x-circle" class="w-4 h-4 text-rose-600 shrink-0"></i> ' + (message || "Path does not exist on PC.") + '</div>');
        }

        if (window.lucide) lucide.createIcons();
    }

    // =========================================================================
    // 4. Folder CRUD (Save, Edit, Delete)
    // =========================================================================
    function openAddFolderModal() {
        $("#hdnFolderId").val("0");
        $("#txtFolderName").val("");
        $("#txtLocalPath").val("");
        $("#txtFolderDescription").val("");
        $("#modalFolderTitle").text("Add Shared Folder");
        $("#activeToggleContainer").addClass("hidden").removeClass("flex");
        $("#validationFeedback").addClass("hidden").html("");
        $("#btnSaveSharedFolder").prop("disabled", true);
        $("#hdnIsPathValidated").val("false");
        originalEditPath = "";

        $("#modalSharedFolder").removeClass("hidden").addClass("flex");
        $("#txtFolderName").focus();
        if (window.lucide) lucide.createIcons();
    }

    function openEditFolderModal(folderId, folderName, localPath, description, isActive) {
        $("#hdnFolderId").val(folderId);
        $("#txtFolderName").val(folderName);
        $("#txtLocalPath").val(localPath);
        $("#txtFolderDescription").val(description || "");
        $("#chkFolderIsActive").prop("checked", isActive);
        $("#modalFolderTitle").text("Edit Shared Folder");
        $("#activeToggleContainer").removeClass("hidden").addClass("flex");
        $("#validationFeedback").addClass("hidden").html("");
        $("#btnSaveSharedFolder").prop("disabled", false);
        $("#hdnIsPathValidated").val("true");
        originalEditPath = localPath;

        $("#modalSharedFolder").removeClass("hidden").addClass("flex");
        $("#txtFolderName").focus();
        if (window.lucide) lucide.createIcons();
    }

    function closeFolderModal() {
        $("#modalSharedFolder").addClass("hidden").removeClass("flex");
    }

    function submitFolderForm() {
        var folderId = parseInt($("#hdnFolderId").val() || "0", 10);
        var folderName = $("#txtFolderName").val().trim();
        var localPath = $("#txtLocalPath").val().trim();
        var description = $("#txtFolderDescription").val().trim();
        var isActive = $("#chkFolderIsActive").is(":checked");
        var isValidated = $("#hdnIsPathValidated").val() === "true";

        if (!folderName) {
            Swal.fire("Required", "Please enter a folder name.", "warning");
            return;
        }

        if (!localPath) {
            Swal.fire("Required", "Please enter a local path.", "warning");
            return;
        }

        if (!isValidated && folderId === 0) {
            Swal.fire("Validation Required", "Please click 'Validate Path' to confirm the folder exists on your PC.", "warning");
            return;
        }

        var isEdit = (folderId > 0);
        var endpoint = isEdit ? "/FolderAPI/UpdateSharedFolder" : "/FolderAPI/SaveSharedFolder";

        var payload = {
            folder_id: isEdit ? folderId : null,
            device_guid: currentDeviceGuid,
            folder_name: folderName,
            local_path: localPath,
            description: description,
            is_active: isActive
        };

        var $saveBtn = $("#btnSaveSharedFolder");
        $saveBtn.prop("disabled", true).html('<span class="animate-spin w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full mr-2 inline-block"></span> Saving...');

        $.ajax({
            url: endpoint,
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(payload),
            success: function (res) {
                $saveBtn.prop("disabled", false).html('<i data-lucide="save" class="w-4 h-4"></i><span>Save Folder</span>');
                if (window.lucide) lucide.createIcons();

                if (res.status === "success") {
                    closeFolderModal();
                    Swal.fire({
                        icon: "success",
                        title: isEdit ? "Folder Updated!" : "Folder Added!",
                        text: res.message,
                        timer: 1500,
                        showConfirmButton: false
                    }).then(function () {
                        window.location.reload();
                    });
                } else {
                    Swal.fire("Failed", res.message || "Operation failed.", "error");
                }
            },
            error: function () {
                $saveBtn.prop("disabled", false).html('<i data-lucide="save" class="w-4 h-4"></i><span>Save Folder</span>');
                if (window.lucide) lucide.createIcons();
                Swal.fire("Error", "Network error saving folder.", "error");
            }
        });
    }

    function confirmDeleteFolder(folderId, folderName) {
        Swal.fire({
            title: 'Remove "' + folderName + '"?',
            text: "This will remove the remote access configuration from this system. Physical files on your PC will remain untouched.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#64748b',
            confirmButtonText: 'Yes, remove folder'
        }).then(function (result) {
            if (result.isConfirmed) {
                $.ajax({
                    url: "/FolderAPI/DeleteSharedFolder",
                    type: "POST",
                    contentType: "application/json; charset=utf-8",
                    dataType: "json",
                    data: JSON.stringify({ folderId: folderId }),
                    success: function (res) {
                        if (res.status === "success") {
                            Swal.fire({
                                icon: "success",
                                title: "Folder Removed",
                                text: res.message,
                                timer: 1200,
                                showConfirmButton: false
                            }).then(function () {
                                window.location.reload();
                            });
                        } else {
                            Swal.fire("Error", res.message, "error");
                        }
                    },
                    error: function () {
                        Swal.fire("Error", "Network error removing folder.", "error");
                    }
                });
            }
        });
    }

    // =========================================================================
    // 5. User Access Permissions Matrix Modal
    // =========================================================================
    function openPermissionsModal(folderId, folderName) {
        $("#hdnPermFolderId").val(folderId);
        $("#permModalFolderName").text(folderName);
        $("#modalPermissions").removeClass("hidden").addClass("flex");

        var $tbody = $("#permTableBody");
        $tbody.html('<tr><td colspan="6" class="py-8 text-center text-slate-400"><div class="flex items-center justify-center gap-2"><span class="animate-spin w-4 h-4 border-2 border-primary border-t-transparent rounded-full"></span> Loading user permissions...</div></td></tr>');

        $.ajax({
            url: "/FolderAPI/GetFolderPermissions?folderId=" + folderId,
            type: "GET",
            dataType: "json",
            success: function (res) {
                if (res.status === "success" && res.data) {
                    renderPermissionsTable(res.data, folderId);
                } else {
                    $tbody.html('<tr><td colspan="6" class="py-6 text-center text-rose-500 font-medium">' + (res.message || "Failed to load permissions.") + '</td></tr>');
                }
            },
            error: function () {
                $tbody.html('<tr><td colspan="6" class="py-6 text-center text-rose-500 font-medium">Network error loading permissions.</td></tr>');
            }
        });

        if (window.lucide) lucide.createIcons();
    }

    function renderPermissionsTable(users, folderId) {
        var $tbody = $("#permTableBody");
        $tbody.empty();

        if (!users || users.length === 0) {
            $tbody.html('<tr><td colspan="6" class="py-6 text-center text-slate-400 font-medium">No team users found.</td></tr>');
            return;
        }

        users.forEach(function (u) {
            var isOwner = u.is_owner;
            var trHtml = 
                '<tr data-user-id="' + u.user_id + '" class="hover:bg-slate-50/60 transition-colors">' +
                '<td class="py-3 px-4">' +
                '<div class="flex items-center gap-2.5">' +
                '<div class="w-7 h-7 rounded-full bg-slate-100 text-slate-700 font-bold text-xs flex items-center justify-center shrink-0 border border-slate-200">' +
                u.user_display_name.charAt(0).toUpperCase() +
                '</div>' +
                '<div>' +
                '<span class="font-bold text-slate-900 block">' + u.user_display_name + (isOwner ? ' <span class="text-[10px] text-emerald-700 font-semibold px-1.5 py-0.5 rounded bg-emerald-100">Owner</span>' : '') + '</span>' +
                '<span class="text-[11px] text-slate-400">' + u.email + '</span>' +
                '</div>' +
                '</div>' +
                '</td>' +
                
                // Can View
                '<td class="py-3 px-3 text-center">' +
                '<input type="checkbox" class="chk-perm chk-view rounded text-primary focus:ring-primary h-4 w-4 border-slate-300" ' + (u.can_view || isOwner ? 'checked ' : '') + (isOwner ? 'disabled' : '') + ' />' +
                '</td>' +

                // Can Download
                '<td class="py-3 px-3 text-center">' +
                '<input type="checkbox" class="chk-perm chk-download rounded text-primary focus:ring-primary h-4 w-4 border-slate-300" ' + (u.can_download || isOwner ? 'checked ' : '') + (isOwner ? 'disabled' : '') + ' />' +
                '</td>' +

                // Can Upload
                '<td class="py-3 px-3 text-center">' +
                '<input type="checkbox" class="chk-perm chk-upload rounded text-primary focus:ring-primary h-4 w-4 border-slate-300" ' + (u.can_upload || isOwner ? 'checked ' : '') + (isOwner ? 'disabled' : '') + ' />' +
                '</td>' +

                // Can Delete
                '<td class="py-3 px-3 text-center">' +
                '<input type="checkbox" class="chk-perm chk-delete rounded text-primary focus:ring-primary h-4 w-4 border-slate-300" ' + (u.can_delete || isOwner ? 'checked ' : '') + (isOwner ? 'disabled' : '') + ' />' +
                '</td>' +

                // Action Save Button
                '<td class="py-3 px-4 text-right">' +
                (isOwner ? 
                    '<span class="text-xs text-slate-400 font-semibold">Full Access</span>' : 
                    '<button type="button" onclick="DeviceFoldersManager.saveUserPermission(' + folderId + ', ' + u.user_id + ', this)" class="btn-save-perm px-3 py-1 rounded bg-slate-100 hover:bg-primary hover:text-white text-slate-700 text-xs font-semibold transition-colors">Save</button>'
                ) +
                '</td>' +
                '</tr>';

            $tbody.append(trHtml);
        });

        if (window.lucide) lucide.createIcons();
    }

    function saveUserPermission(folderId, targetUserId, btnElement) {
        var $tr = $(btnElement).closest("tr");
        var canView = $tr.find(".chk-view").is(":checked");
        var canDownload = $tr.find(".chk-download").is(":checked");
        var canUpload = $tr.find(".chk-upload").is(":checked");
        var canDelete = $tr.find(".chk-delete").is(":checked");

        var $btn = $(btnElement);
        var origText = $btn.text();
        $btn.prop("disabled", true).text("Saving...");

        var payload = {
            folder_id: folderId,
            target_user_id: targetUserId,
            can_view: canView,
            can_download: canDownload,
            can_upload: canUpload,
            can_delete: canDelete
        };

        $.ajax({
            url: "/FolderAPI/UpdatePermission",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            data: JSON.stringify(payload),
            success: function (res) {
                $btn.prop("disabled", false);
                if (res.status === "success") {
                    $btn.attr("class", "px-3 py-1 rounded bg-emerald-600 text-white text-xs font-semibold").text("Saved ✓");
                    setTimeout(function () {
                        $btn.attr("class", "btn-save-perm px-3 py-1 rounded bg-slate-100 hover:bg-primary hover:text-white text-slate-700 text-xs font-semibold transition-colors").text(origText);
                    }, 2000);
                } else {
                    $btn.text(origText);
                    Swal.fire("Failed", res.message || "Failed to save permission.", "error");
                }
            },
            error: function () {
                $btn.prop("disabled", false).text(origText);
                Swal.fire("Error", "Network error saving permission.", "error");
            }
        });
    }

    return {
        init: init,
        openAddFolderModal: openAddFolderModal,
        openEditFolderModal: openEditFolderModal,
        confirmDeleteFolder: confirmDeleteFolder,
        openPermissionsModal: openPermissionsModal,
        saveUserPermission: saveUserPermission
    };
})(jQuery);
