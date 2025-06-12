$(document).ready(function() {
    // Initialize toastr options
    toastr.options = {
        closeButton: true,
        progressBar: true,
        positionClass: "toast-top-right",
        timeOut: 3000,
        showDuration: "300",
        hideDuration: "1000",
        extendedTimeOut: "1000",
    };

    // Form validation
    const form = document.querySelector('form');
    form.addEventListener('submit', function(event) {
        if (!form.checkValidity()) {
            event.preventDefault();
            event.stopPropagation();
        }
        form.classList.add('was-validated');
    });

    // Handle password visibility toggle
    $('.toggle-password').on('click', function() {
        const button = $(this);
        const input = button.closest('.input-group').find('input');
        const icon = button.find('i');
        
        if (input.attr('type') === 'password') {
            input.attr('type', 'text');
            icon.removeClass('fa-eye').addClass('fa-eye-slash');
        } else {
            input.attr('type', 'password');
            icon.removeClass('fa-eye-slash').addClass('fa-eye');
        }
    });

    // Handle form submission
    $('.login-form form').on('submit', function(e) {
        e.preventDefault();
        
        const form = $(this);
        if (!form[0].checkValidity()) {
            return;
        }
        
        const email = $('#email').val();
        const password = $('#password').val();
        const rememberMe = $('#rememberMe').is(':checked');
        const token = $('input[name="__RequestVerificationToken"]').val();

        // Show loading state
        const loginBtn = $('#login-btn');
        const loginText = $('#login-text');
        const loadingSpinner = $('#loading-spinner');
        
        loginBtn.prop('disabled', true);
        loginText.addClass('d-none');
        loadingSpinner.removeClass('d-none');

        // Add animation to the button
        loginBtn.css('transform', 'scale(0.95)');

        $.ajax({
            url: '/Admin/Login',
            type: 'POST',
            data: {
                email: email,
                password: password,
                rememberMe: rememberMe,
                __RequestVerificationToken: token
            },
            success: function(response) {
                if (response.redirectUrl) {
                    toastr.success('Đăng nhập thành công', '', {
                        onHidden: function() {
                            window.location.href = response.redirectUrl;
                        }
                    });
                } else {
                    showError(response.error || 'Email hoặc mật khẩu không đúng');
                }
            },
            error: function(xhr) {
                if (xhr.status === 400) {
                    showError(xhr.responseJSON?.error || 'Dữ liệu không hợp lệ');
                } else {
                    showError('Có lỗi xảy ra, vui lòng thử lại');
                }
            },
            complete: function() {
                // Reset button state
                loginBtn.prop('disabled', false);
                loginText.removeClass('d-none');
                loadingSpinner.addClass('d-none');
                loginBtn.css('transform', '');
            }
        });
    });

    // Helper function to show errors
    function showError(message) {
        toastr.error(message, '', {
            closeButton: true,
            timeOut: 5000,
            extendedTimeOut: 2000,
            progressBar: true,
            newestOnTop: true,
            preventDuplicates: true
        });
    }

    // Add floating animation to shapes
    const shapes = document.querySelectorAll('.shape');
    shapes.forEach((shape, index) => {
        shape.style.animationDelay = `${index * 2}s`;
    });

    // Add hover effect to social buttons
    $('.social-buttons a').hover(
        function() {
            $(this).css('transform', 'translateY(-2px)');
        },
        function() {
            $(this).css('transform', '');
        }
    );
}); 