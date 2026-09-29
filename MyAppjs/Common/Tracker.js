/**
 * Reusable Tracker for dynamic engagement (Views, Likes)
 * Can be included in any page where you need to track engagements.
 */
const EngagementTracker = {
    /**
     * Track a view for a specific blog/post ID
     * @param {number} blogId - The ID of the post
     * @param {function} onSuccess - Callback function passing the new formatted count
     */
    trackView: function(blogId, onSuccess) {
        if (!blogId) return;

        $.ajax({
            url: '/BlogAPI/TrackView',
            type: 'POST',
            data: { blogId: blogId },
            success: function(response) {
                if (response && response.success) {
                    const newCount = parseInt(response.view_count) || 0;
                    if (typeof onSuccess === 'function') {
                        onSuccess(EngagementTracker.formatNumber(newCount));
                    }
                }
            }
        });
    },

    /**
     * Formats a raw number into a short string (e.g., 1500 -> 1.5K)
     */
    formatNumber: function(num) {
        if (num >= 1000000) {
            return (num / 1000000).toFixed(1).replace(/\.0$/, '') + 'M';
        }
        if (num >= 1000) {
            return (num / 1000).toFixed(1).replace(/\.0$/, '') + 'K';
        }
        return num.toString();
    }
};
