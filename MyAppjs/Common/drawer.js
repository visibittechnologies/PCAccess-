var originalParent = null;

function openDrawer(formSelector, title) {
    let form = $(formSelector);
    if (form.length === 0) return;

    // store original parent
    originalParent = form.parent();
    
    $("#drawerTitle").text(title);
    $("#drawerBody").empty();
    
    // Move form into drawer to maintain state (events, inputs)
    form.removeClass("hidden").appendTo("#drawerBody");
    
    $("#globalDrawerOverlay").removeClass("hidden");
    setTimeout(() => {
        $("#globalDrawerOverlay").removeClass("opacity-0").addClass("opacity-100");
        $("#globalDrawer").removeClass("translate-x-full");
    }, 10);
    
    $("body").css("overflow", "hidden");
}

function closeDrawer() {
    $("#globalDrawer").addClass("translate-x-full");
    $("#globalDrawerOverlay").removeClass("opacity-100").addClass("opacity-0");
    
    setTimeout(function () {
        $("#globalDrawerOverlay").addClass("hidden");
        // move form back to original place
        $("#drawerBody").children().each(function () {
            $(this).appendTo(originalParent || "body").addClass("hidden");
        });
        $("body").css("overflow", "");
    }, 300);
}

// Close drawer events
$(document).on("click", "#closeDrawerBtn, #globalDrawerOverlay", function (e) {
    // Only close if the background overlay or close button is clicked directly
    if (e.target === this || $(this).attr('id') === 'closeDrawerBtn' || $(this).parents('#closeDrawerBtn').length > 0) {
        closeDrawer();
    }
});

// ESC key to close
$(document).on("keydown", function(e) {
    if (e.key === "Escape" && !$("#globalDrawer").hasClass("translate-x-full")) {
        closeDrawer();
    }
});

// Helper for standardized file uploads
function handleFileSelect(input, labelId, previewContainerId, previewImgId, infoId) {
    const file = input.files[0];
    if (file) {
        $(`#${labelId}`).text(file.name).removeClass('text-slate-400').addClass('text-slate-700 dark:text-slate-200');
        $(`#${previewContainerId}`).removeClass('hidden');
        const reader = new FileReader();
        reader.onload = function(e) {
            $(`#${previewImgId}`).attr('src', e.target.result);
        }
        reader.readAsDataURL(file);
        const sizeMB = (file.size / (1024 * 1024)).toFixed(2);
        $(`#${infoId}`).text(`${sizeMB} MB`);
    } else {
        $(`#${previewContainerId}`).addClass('hidden');
    }
}