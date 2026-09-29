$(document).ready(function () {
    // Disable HTML5 native validation tooltips
    $('.js-subscribe-form').attr('novalidate', 'novalidate');

    $(document).on('submit', '.js-subscribe-form', function (e) {
        e.preventDefault();
        var $form = $(this);
        var email = $form.find('.js-subscribe-email').val();
        
        if (!email) {
            Swal.fire({
                icon: 'warning',
                title: 'Oops...',
                text: 'Please enter a valid email address.'
            });
            return;
        }

        // Simple email regex validation
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(email)) {
            Swal.fire({
                icon: 'error',
                title: 'Invalid Email',
                text: 'Please enter a correct email format.'
            });
            return;
        }

        $.ajax({
            url: '/Common/Subscribe',
            type: 'POST',
            data: { email: email },
            success: function (res) {
                if (res.success) {
                    Swal.fire({
                        icon: 'success',
                        title: 'Subscribed!',
                        text: res.message,
                        confirmButtonColor: '#3085d6'
                    });
                    $form.find('.js-subscribe-email').val(''); // clear input
                } else {
                    if(res.message.includes('already')) {
                        Swal.fire({
                            icon: 'info',
                            title: 'Already Subscribed',
                            text: res.message
                        });
                    } else {
                        Swal.fire({
                            icon: 'error',
                            title: 'Error',
                            text: res.message
                        });
                    }
                }
            },
            error: function () {
                Swal.fire({
                    icon: 'error',
                    title: 'Network Error',
                    text: 'An error occurred while subscribing. Please try again later.'
                });
            }
        });
    });
});
