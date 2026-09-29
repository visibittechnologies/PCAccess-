
$(document).on("change", ".custom-file-input", function () {
    let fileName = this.files[0]?.name || "Choose File...";
    $(this)
        .next("label")
        .find(".file-name")
        .text(fileName);

});
function loadReference(type, elementId, mode, flag, keyId, callback = null) {
    $.ajax({
        url: '/Common/GetAllDropDownValues',
        type: 'GET',
        data: { type: type, flag: flag, keyId: keyId },
        success: function (res) {
            if (mode === "dropdown") {
                var ddl = $(elementId);
                ddl.empty();
                ddl.append('<option value="0">Select Option</option>');
                $.each(res, function (i, item) {
                    ddl.append('<option value="' + item.MasterId + '">' + item.MasterName + '</option>');
                });

            }
            if (mode === "radio") {
                var container = $(elementId);
                container.empty();
                $.each(res, function (i, item) {

                    var radioHtml =
                        '<label class="flex items-center gap-2 cursor-pointer">' +
                        '<input type="radio" name="' + type + '" value="' + item.MasterId + '" class="h-4 w-4 border-gray-300 text-primary focus:ring-primary">' +
                        '<span class="text-sm">' + item.MasterName + '</span>' +
                        '</label>';

                    container.append(radioHtml);
                });
            }
            if (mode === "CategoryTypeContainer") {
                var container = $("#CategoryTypeContainer");
                container.empty();
                $.each(res, function (i, item) {

                    var checked = i === 0 ? "checked" : "";
                    var description = "";
                    if (item.MasterName.toLowerCase().includes("serialized")) {
                        description = "Requires IMEI or Serial Number tracking for each unit.";
                    }
                    else {
                        description = "Standard inventory tracked by quantity only.";
                    }
                    var html =

                        '<label class="relative flex items-start p-4 cursor-pointer rounded-lg border border-slate-200 dark:border-slate-700 bg-slate-50 dark:bg-slate-800/50 hover:border-primary dark:hover:border-primary/50 transition-colors">' +
                        '<div class="flex items-center h-5">' +
                        '<input ' + checked + ' class="size-4 text-primary border-slate-300 dark:border-slate-600 focus:ring-primary" name="CategoryType" value="' + item.MasterId + '" type="radio">' +
                        '</div>' +
                        '<div class="ml-3 text-sm">' +
                        '<span class="block font-medium text-slate-900 dark:text-white">' + item.MasterName + ' Item</span>' +
                        '<span class="block text-slate-500 dark:text-slate-400 text-xs mt-0.5">' + description + '</span>' +
                        '</div>' +
                        '</label>';
                    container.append(html);
                });
            }

            if (mode === "SupportNeedsContainer") {
                var container = $("#needsSelection");
                container.empty();
                $.each(res, function (i, item) {
                    var icon = "support"; // default icon
                    var desc = item.Description || "";
                    var name = item.MasterName.toLowerCase();

                    // Map icons based on name if not provided in description
                    if (name.includes("meal")) icon = "restaurant";
                    else if (name.includes("companionship")) icon = "diversity_1";
                    else if (name.includes("nursing")) icon = "medical_services";
                    else if (name.includes("mobility") || name.includes("walking")) icon = "accessible";

                    // Try to extract icon from Description if it's formatted like "icon:description"
                    if (desc.includes(":")) {
                        var parts = desc.split(":");
                        icon = parts[0].trim();
                        desc = parts[1].trim();
                    }
                    var html =
                        '<div class="need-card group cursor-pointer relative flex items-start gap-3 md:gap-4 p-4 md:p-5 rounded-xl border-2 border-slate-100 dark:border-slate-800 bg-white dark:bg-slate-800/50 hover:border-primary/30 transition-all duration-300 active:scale-[0.98]" data-id="' + item.MasterId + '">' +
                        '    <div class="bg-slate-50 dark:bg-slate-700 text-primary rounded-full p-2 md:p-2.5 shadow-sm shrink-0 group-hover:scale-110 md:group-hover:scale-110 transition-transform">' +
                        '        <span class="material-symbols-outlined text-xl md:text-2xl">' + icon + '</span>' +
                        '    </div>' +
                        '    <div>' +
                        '        <h3 class="font-bold text-slate-800 dark:text-white text-sm md:text-base mb-1 font-display">' + item.MasterName + '</h3>' +
                        '        <p class="text-[11px] md:text-xs text-slate-500 dark:text-slate-400 leading-relaxed font-body">' + desc + '</p>' +
                        '    </div>' +
                        '    <div class="check-icon absolute top-4 right-4 text-primary opacity-0 scale-50 transition-all">' +
                        '        <span class="material-symbols-outlined text-xl md:text-2xl fill-1">check_circle</span>' +
                        '    </div>' +
                        '</div>';
                    container.append(html);
                });
            }
            if (callback) {
                callback();
            }
        }
    });
}
function validatePhoneNumber(phone) {
    var phoneRegex = /^(?:\+91|91)?[6-9]\d{9}$/;
    return phoneRegex.test(phone);
}
function validateEmail(email) {
    var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}
function commonDelete(id, flag) {

    Swal.fire({
        title: "Are you sure?",
        text: "Do you really want to delete this record?",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#F44336 !important",
        cancelButtonColor: "#F44336 !important",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/Common/CommonDelete',
                type: 'POST',
                data: JSON.stringify({ Id: id, Flag: flag }),
                contentType: "application/json; charset=utf-8",
                beforeSend: function () {
                    $("#loading-div-background").css("display", "flex");
                },
                success: function (data) {
                    $("#loading-div-background").hide();
                    if (data.status === "success") {
                        Swal.fire({
                            icon: "success",
                            title: "Success",
                            text: data.message,
                            confirmButtonColor: "##137FEC"
                        }).then(() => {
                            if (data.url)
                                window.location.href = data.url;
                            else
                                window.location.reload();
                        });
                    } else {
                        Swal.fire("Error", data.message, "error");
                    }
                },
                error: function () {
                    $("#loading-div-background").hide();
                    Swal.fire("Error", "Something went wrong!", "error");
                }
            });
        }
    });
}
function formatDate(dateStr) {
    if (!dateStr) return 'N/A';
    let date;
    if (typeof dateStr === 'string' && dateStr.includes('/Date(')) {
        date = new Date(parseInt(dateStr.substr(6)));
    } else {
        date = new Date(dateStr);
    }
    if (isNaN(date.getTime())) return dateStr;
    try {
        const formatter = new Intl.DateTimeFormat('en-US', {
            timeZone: 'America/New_York',

            month: 'short',
            day: '2-digit',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
            hour12: true
        });
        const parts = formatter.formatToParts(date);
        const getPart = (type) => parts.find(p => p.type === type)?.value || '';
        const month = getPart('month');
        const day = getPart('day');
        const year = getPart('year');
        const hour = getPart('hour');
        const minute = getPart('minute');
        const dayPeriod = getPart('dayPeriod');
        return `${month} ${day}, ${year}<br/><span class="text-[11px] font-bold text-slate-400">${hour}:${minute} ${dayPeriod} EST</span>`;
    } catch (e) {
        if (window.moment) {
            return moment(date).format('MMM DD, YYYY') + '<br/><span class="text-[11px] font-bold text-slate-400">' + moment(date).format('hh:mm A') + ' EST</span>';
        }
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString();
    }
}
