function openModal(modalId) {
    const $modal = $(modalId);
    if ($modal.length === 0) return;
    $modal.removeClass('hidden').addClass('flex');
    setTimeout(() => {
        $modal.find('.modal-content').removeClass('scale-95 opacity-0').addClass('scale-100 opacity-100');
    }, 10);
    $("body").css("overflow", "hidden");
}
function closeModal(modalId) {
    const $modal = $(modalId);
    if ($modal.length === 0) return;
    $modal.find('.modal-content').addClass('scale-95 opacity-0').removeClass('scale-100 opacity-100');
    setTimeout(() => {
        $modal.removeClass('flex').addClass('hidden');
        $("body").css("overflow", "");
    }, 300);
}
$(document).on("click", ".modal-container", function (e) {
    if (e.target === this) {
        const id = $(this).attr('id');
        closeModal('#' + id);
    }
});
$(document).on("keydown", function(e) {
    if (e.key === "Escape") {
        $(".modal-container:not(.hidden)").each(function() {
            closeModal('#' + $(this).attr('id'));
        });
    }
});
