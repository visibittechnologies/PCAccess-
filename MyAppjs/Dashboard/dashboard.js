/*
================================================================================
MODULE: dashboard.js
AUTHOR: Antigravity / Senior .NET Developer
DATE: 2026-09-22
PURPOSE:
  Handles real-time device status updates via SignalR, instant UI transitions 
  for Online/Offline states, audio/visual toast feedback, and the "Add Device" 
  pairing modal.

REASON & ARCHITECTURAL PATTERN:
  - Follows the project's strict rule: JavaScript kept separate from .cshtml in MyAppjs/.
  - Reuses SignalR hub client ($.connection.deviceHub) matching the existing pattern.
  - Follows standard AJAX response checks (if (res.status === 'success')).
  - High-contrast visual cues: Green pulse (Online) vs Clear Red pill (Offline).
  - Dynamic Recent Activity logging and SweetAlert toast alerts.
  - Zero inline scripts in Razor views.
================================================================================
*/

var DashboardManager = (function ($) {
    "use strict";

    var currentUserId = 0;
    var timerInterval = null;
    var currentPairingCode = "";

    function init(userId) {
        currentUserId = userId;
        initSignalR();
        bindEvents();
    }

    // =========================================================================
    // 1. SignalR Real-Time Device Status Hub
    // =========================================================================
    function initSignalR() {
        if (!$.connection || !$.connection.deviceHub) {
            console.warn("SignalR DeviceHub not loaded or initialized.");
            return;
        }

        var hub = $.connection.deviceHub;

        // REASON: Handle real-time device online/offline notification from server
        hub.client.deviceStatusChanged = function (data) {
            console.log("Real-time device status update received:", data);
            if (!data || !data.deviceGuid) return;

            updateDeviceUI(data.deviceGuid, data.status, data.lastSeen);
        };

        // Connect to SignalR server
        $.connection.hub.start()
            .done(function () {
                console.log("Connected to SignalR DeviceHub. ConnectionId:", $.connection.hub.id);
                if (currentUserId > 0) {
                    hub.server.joinDashboardGroup(currentUserId);
                }
            })
            .fail(function (err) {
                console.error("SignalR DeviceHub connection error:", err);
            });

        // Reconnect handlers
        $.connection.hub.reconnecting(function () {
            console.log("SignalR DeviceHub reconnecting...");
        });

        $.connection.hub.reconnected(function () {
            console.log("SignalR DeviceHub reconnected.");
            if (currentUserId > 0) {
                hub.server.joinDashboardGroup(currentUserId);
            }
        });
    }

    // =========================================================================
    // 2. DOM Updates on Device Status Change (Online / Offline Visual Cues)
    // =========================================================================
    function updateDeviceUI(deviceGuid, status, lastSeen) {
        var guidLower = deviceGuid.toLowerCase();
        var $row = $('tr[data-device-guid="' + guidLower + '"]');
        var $card = $('div.device-mobile-card[data-device-guid="' + guidLower + '"]');

        var devName = $row.attr("data-device-name") || $card.attr("data-device-name") || "Connected Device";

        // 1. Update Desktop/Tablet Table Row
        if ($row.length > 0) {
            var $badge = $row.find(".device-status-badge");
            var $lastSeen = $row.find(".device-last-seen");
            var $actionBtn = $row.find(".device-action-btn");
            var $iconBox = $row.find(".device-icon-box");

            if (status === "online") {
                // High-contrast emerald badge with pulsing dot
                $badge.html('<span class="relative flex h-2 w-2 mr-1.5">' +
                    '<span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>' +
                    '<span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>' +
                    '</span>' +
                    '<span>Online</span>');
                $badge.attr("class", "device-status-badge inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-300 shadow-sm");

                $iconBox.attr("class", "device-icon-box w-8 h-8 rounded-lg bg-emerald-50 text-emerald-600 border border-emerald-100 flex items-center justify-center transition-colors");

                $actionBtn.attr("href", "/Admin/DeviceFolders?deviceGuid=" + deviceGuid).html('<span>Open</span><i data-lucide="external-link" class="w-3.5 h-3.5"></i>')
                    .prop("disabled", false)
                    .attr("title", "Open remote file manager")
                    .attr("class", "device-action-btn inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-primary hover:bg-emerald-700 text-white text-xs font-semibold shadow-sm transition-all");
            } else {
                // High-contrast rose/red badge with solid red dot for clear offline distinction
                $badge.html('<span class="w-2 h-2 rounded-full bg-rose-500 mr-1.5"></span>' +
                    '<span>Offline</span>');
                $badge.attr("class", "device-status-badge inline-flex items-center px-2.5 py-1 rounded-full text-xs font-bold bg-rose-50 text-rose-700 border border-rose-200 shadow-sm");

                $iconBox.attr("class", "device-icon-box w-8 h-8 rounded-lg bg-slate-100 text-slate-400 border border-slate-200 flex items-center justify-center transition-colors");

                $actionBtn.html('<span>Disconnected</span><i data-lucide="cloud-off" class="w-3.5 h-3.5"></i>')
                    .prop("disabled", true)
                    .attr("title", "Device is offline. Start FileAccessAgent on PC to connect.")
                    .attr("class", "device-action-btn inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-slate-100 text-slate-400 border border-slate-200 text-xs font-semibold cursor-not-allowed transition-all opacity-80");
            }

            if (lastSeen) {
                $lastSeen.text(lastSeen);
            }
        }

        // 2. Update Mobile Card
        if ($card.length > 0) {
            var $mBadge = $card.find(".device-status-badge");
            var $mLastSeen = $card.find(".device-last-seen");
            var $mActionBtn = $card.find(".device-action-btn");
            var $mIconBox = $card.find(".device-icon-box");

            if (status === "online") {
                $mBadge.html('<span class="relative flex h-1.5 w-1.5 mr-1.5">' +
                    '<span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>' +
                    '<span class="relative inline-flex rounded-full h-1.5 w-1.5 bg-emerald-500"></span>' +
                    '</span>' +
                    '<span>Online</span>');
                $mBadge.attr("class", "device-status-badge inline-flex items-center px-2 py-0.5 rounded-full text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-300 shadow-sm");

                $mIconBox.attr("class", "device-icon-box w-8 h-8 rounded-lg bg-emerald-50 text-emerald-600 border border-emerald-100 flex items-center justify-center transition-colors");

                $mActionBtn.attr("href", "/Admin/DeviceFolders?deviceGuid=" + deviceGuid).html('<span>Open Device</span><i data-lucide="external-link" class="w-3.5 h-3.5"></i>')
                    .prop("disabled", false)
                    .attr("title", "Open remote file manager")
                    .attr("class", "device-action-btn w-full py-2.5 rounded-lg bg-primary hover:bg-emerald-700 text-white text-xs font-semibold shadow-sm text-center transition-all flex items-center justify-center gap-2");
            } else {
                $mBadge.html('<span class="w-1.5 h-1.5 rounded-full bg-rose-500 mr-1.5"></span>' +
                    '<span>Offline</span>');
                $mBadge.attr("class", "device-status-badge inline-flex items-center px-2 py-0.5 rounded-full text-xs font-bold bg-rose-50 text-rose-700 border border-rose-200 shadow-sm");

                $mIconBox.attr("class", "device-icon-box w-8 h-8 rounded-lg bg-slate-100 text-slate-400 border border-slate-200 flex items-center justify-center transition-colors");

                $mActionBtn.html('<span>Device Disconnected</span><i data-lucide="cloud-off" class="w-3.5 h-3.5"></i>')
                    .prop("disabled", true)
                    .attr("title", "Device is offline. Start FileAccessAgent on PC to connect.")
                    .attr("class", "device-action-btn w-full py-2.5 rounded-lg bg-slate-100 text-slate-400 border border-slate-200 text-xs font-semibold cursor-not-allowed text-center transition-all flex items-center justify-center gap-2 opacity-80");
            }

            if (lastSeen) {
                $mLastSeen.text(lastSeen);
            }
        }

        // 3. Re-render Lucide Icons for dynamic buttons
        if (window.lucide) {
            lucide.createIcons();
        }

        // 4. Prepend Real-Time Event to "Recent Activity" Feed
        var $activityList = $("#recentActivityList");
        if ($activityList.length > 0) {
            var activityHtml = "";
            if (status === "offline") {
                activityHtml =
                    '<div class="flex items-start gap-3 p-2.5 rounded-lg bg-rose-50/70 border border-rose-200/80 transition-all shadow-xs animate-in fade-in slide-in-from-top-2 duration-300">' +
                    '<div class="w-8 h-8 rounded-lg bg-rose-100 text-rose-600 flex items-center justify-center shrink-0 mt-0.5">' +
                    '<i data-lucide="power-off" class="w-4 h-4"></i>' +
                    '</div>' +
                    '<div class="flex-1 min-w-0">' +
                    '<p class="text-xs font-bold text-rose-900 leading-snug">' + devName + ' disconnected (Offline)</p>' +
                    '<span class="text-[11px] text-rose-600 font-medium">Agent stopped / Ctrl+C &bull; Just now</span>' +
                    '</div>' +
                    '</div>';
            } else {
                activityHtml =
                    '<div class="flex items-start gap-3 p-2.5 rounded-lg bg-emerald-50/70 border border-emerald-200/80 transition-all shadow-xs animate-in fade-in slide-in-from-top-2 duration-300">' +
                    '<div class="w-8 h-8 rounded-lg bg-emerald-100 text-emerald-600 flex items-center justify-center shrink-0 mt-0.5">' +
                    '<i data-lucide="wifi" class="w-4 h-4"></i>' +
                    '</div>' +
                    '<div class="flex-1 min-w-0">' +
                    '<p class="text-xs font-bold text-emerald-900 leading-snug">' + devName + ' is now Online</p>' +
                    '<span class="text-[11px] text-emerald-600 font-medium">SignalR session active &bull; Just now</span>' +
                    '</div>' +
                    '</div>';
            }
            $activityList.prepend(activityHtml);
            $activityList.children().slice(6).remove();
            if (window.lucide) {
                lucide.createIcons();
            }
        }

        // 5. Fire Non-Intrusive SweetAlert Toast Notification
        if (window.Swal) {
            var Toast = Swal.mixin({
                toast: true,
                position: 'top-end',
                showConfirmButton: false,
                timer: 3500,
                timerProgressBar: true
            });

            if (status === "offline") {
                Toast.fire({
                    icon: 'warning',
                    title: devName + ' is now Offline',
                    text: 'Agent disconnected (Ctrl+C)'
                });
            } else if (status === "online") {
                Toast.fire({
                    icon: 'success',
                    title: devName + ' is now Online',
                    text: 'Connected via SignalR DeviceHub'
                });
            }
        }

        // 6. Update Online Summary Counter in Card 1
        updateSummaryCounters();
    }

    function updateSummaryCounters() {
        var onlineCount = $('.device-status-badge:contains("Online")').length;
        var actualOnline = Math.ceil(onlineCount / 2);
        var $container = $("#summaryOnlineContainer");
        if ($container.length > 0) {
            if (actualOnline > 0) {
                $container.html(
                    '<span class="relative flex h-2 w-2">' +
                    '<span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-75"></span>' +
                    '<span class="relative inline-flex rounded-full h-2 w-2 bg-emerald-500"></span>' +
                    '</span>' +
                    '<span id="summaryOnlineCount" class="text-xs font-semibold text-emerald-700">' + actualOnline + ' Online now</span>'
                );
            } else {
                $container.html(
                    '<span class="w-2 h-2 rounded-full bg-slate-400"></span>' +
                    '<span id="summaryOnlineCount" class="text-xs font-semibold text-slate-500">0 Online now</span>'
                );
            }
        }
    }

    // =========================================================================
    // 3. "Add Device" Pairing Modal Logic
    // =========================================================================
    function bindEvents() {
        $("#btnAddDeviceHeader, #btnAddDeviceTable, #btnAddDeviceEmpty").on("click", function (e) {
            e.preventDefault();
            openAddDeviceModal();
        });

        $("#btnClosePairingModal, #btnCancelPairingModal").on("click", function (e) {
            e.preventDefault();
            closeAddDeviceModal();
        });

        $("#btnCopyPairingCode").on("click", function (e) {
            e.preventDefault();
            copyPairingCode();
        });

        $("#btnRefreshPairingCode").on("click", function (e) {
            e.preventDefault();
            generateNewPairingCode();
        });
    }

    function openAddDeviceModal() {
        $("#modalAddDevice").removeClass("hidden").addClass("flex");
        generateNewPairingCode();
    }

    function closeAddDeviceModal() {
        $("#modalAddDevice").addClass("hidden").removeClass("flex");
        if (timerInterval) {
            clearInterval(timerInterval);
            timerInterval = null;
        }
    }

    function generateNewPairingCode() {
        var $display = $("#txtPairingCodeDisplay");
        var $timer = $("#txtPairingTimer");
        var $btnRefresh = $("#btnRefreshPairingCode");

        $display.text("--- ---");
        $timer.text("Generating code...");
        $btnRefresh.prop("disabled", true);

        // REASON: Standard AJAX call to existing DeviceAPI controller pattern
        $.ajax({
            url: "/DeviceAPI/GeneratePairingCode",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: function (res) {
                $btnRefresh.prop("disabled", false);
                if (res.status === "success" && res.data) {
                    currentPairingCode = res.data.pairing_code;
                    $display.text(currentPairingCode);
                    startCountdown(res.data.expires_in_seconds || 600);
                } else {
                    $display.text("ERROR");
                    $timer.text(res.message || "Could not generate pairing code.");
                }
            },
            error: function () {
                $btnRefresh.prop("disabled", false);
                $display.text("ERROR");
                $timer.text("Network error generating code.");
            }
        });
    }

    function startCountdown(seconds) {
        if (timerInterval) {
            clearInterval(timerInterval);
        }

        var remaining = seconds;
        var $timer = $("#txtPairingTimer");

        function tick() {
            var mins = Math.floor(remaining / 60);
            var secs = remaining % 60;
            var formatted = "Expires in " + (mins < 10 ? "0" : "") + mins + ":" + (secs < 10 ? "0" : "") + secs;
            $timer.text(formatted);

            if (remaining <= 0) {
                clearInterval(timerInterval);
                $timer.text("Pairing code expired. Click Refresh to get a new code.");
                $("#txtPairingCodeDisplay").addClass("line-through opacity-50");
            }
            remaining--;
        }

        $("#txtPairingCodeDisplay").removeClass("line-through opacity-50");
        tick();
        timerInterval = setInterval(tick, 1000);
    }

    function copyPairingCode() {
        if (!currentPairingCode) return;

        navigator.clipboard.writeText(currentPairingCode).then(function () {
            var $btn = $("#btnCopyPairingCode");
            var originalText = $btn.html();
            $btn.html('<i data-lucide="check" class="w-4 h-4 mr-1 text-emerald-600"></i> Copied!');
            if (window.lucide) lucide.createIcons();

            setTimeout(function () {
                $btn.html(originalText);
                if (window.lucide) lucide.createIcons();
            }, 2000);
        }).catch(function () {
            alert("Pairing code: " + currentPairingCode);
        });
    }

    return {
        init: init,
        openAddDeviceModal: openAddDeviceModal,
        closeAddDeviceModal: closeAddDeviceModal
    };
})(jQuery);
