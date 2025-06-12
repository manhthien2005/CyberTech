document.addEventListener("DOMContentLoaded", () => {
    const API = { resetPassword: '/Account/ResetPassword' };
    const DOM = {
        form: document.getElementById('resetPasswordForm'),
        newPassword: document.getElementById('newPassword'),
        confirmPassword: document.getElementById('confirmPassword'),
        submitButton: document.querySelector('button[type="submit"]'),
        newPasswordError: document.getElementById('newPasswordError'),
        confirmPasswordError: document.getElementById('confirmPasswordError')
    };

    // Kiểm tra xem form có tồn tại không
    if (!DOM.form) {
        console.log('Reset password form not found, skipping initialization');
        return;
    }

    function clearErrors() {
        if (DOM.newPasswordError) DOM.newPasswordError.textContent = '';
        if (DOM.confirmPasswordError) DOM.confirmPasswordError.textContent = '';
    }

    function validatePassword(password) {
        const minLength = 6;
        const hasUpperCase = /[A-Z]/.test(password);
        const hasLowerCase = /[a-z]/.test(password);
        const hasNumbers = /\d/.test(password);
        const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(password);

        if (password.length < minLength) {
            return "Mật khẩu phải có ít nhất 6 ký tự";
        }
        if (!hasUpperCase) {
            return "Mật khẩu phải chứa ít nhất một chữ hoa";
        }
        if (!hasLowerCase) {
            return "Mật khẩu phải chứa ít nhất một chữ thường";
        }
        if (!hasNumbers) {
            return "Mật khẩu phải chứa ít nhất một số";
        }
        if (!hasSpecialChar) {
            return "Mật khẩu phải chứa ít nhất một ký tự đặc biệt";
        }
        return null;
    }

    function validateForm() {
        clearErrors();
        let isValid = true;

        // Validate new password
        if (!DOM.newPassword || !DOM.newPassword.value) {
            if (DOM.newPasswordError) {
                DOM.newPasswordError.textContent = "Mật khẩu mới là bắt buộc";
            }
            isValid = false;
        } else {
            const passwordError = validatePassword(DOM.newPassword.value);
            if (passwordError && DOM.newPasswordError) {
                DOM.newPasswordError.textContent = passwordError;
                isValid = false;
            }
        }

        // Validate confirm password
        if (!DOM.confirmPassword || !DOM.confirmPassword.value) {
            if (DOM.confirmPasswordError) {
                DOM.confirmPasswordError.textContent = "Xác nhận mật khẩu là bắt buộc";
            }
            isValid = false;
        } else if (DOM.newPassword && DOM.newPassword.value !== DOM.confirmPassword.value) {
            if (DOM.confirmPasswordError) {
                DOM.confirmPasswordError.textContent = "Mật khẩu xác nhận không khớp";
            }
            isValid = false;
        }

        return isValid;
    }

    function setLoading(isLoading) {
        if (DOM.submitButton) {
            DOM.submitButton.disabled = isLoading;
            DOM.submitButton.innerHTML = isLoading 
                ? '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Đang xử lý...'
                : '<i class="fas fa-key me-2"></i>Đặt lại mật khẩu';
        }
    }

    function setupEventListeners() {
        if (!DOM.form) return;

        DOM.form.addEventListener('submit', async (e) => {
            e.preventDefault();
            if (!validateForm()) return;

            const tokenInput = DOM.form.querySelector('input[name="token"]');
            if (!tokenInput || !tokenInput.value) {
                utils.showToast("Token không hợp lệ", "error");
                return;
            }

            const formData = new FormData();
            formData.append('token', tokenInput.value);
            formData.append('newPassword', DOM.newPassword.value);

            try {
                setLoading(true);
                utils.showLoadingOverlay(true);

                const result = await utils.fetchData(API.resetPassword, 'POST', formData);

                if (result.success) {
                    utils.showToast("Mật khẩu đã được đặt lại thành công!", "success");
                    setTimeout(() => window.location.href = '/Account/Login', 1500);
                } else {
                    utils.showToast(result.errorMessage || "Không thể đặt lại mật khẩu", "error");
                }
            } catch (error) {
                console.error('Error:', error);
                utils.showToast("Đã xảy ra lỗi khi kết nối đến máy chủ", "error");
            } finally {
                setLoading(false);
                utils.showLoadingOverlay(false);
            }
        });

        // Real-time validation
        if (DOM.newPassword) {
            DOM.newPassword.addEventListener('input', () => {
                if (DOM.newPasswordError) DOM.newPasswordError.textContent = '';
                if (DOM.confirmPassword && DOM.confirmPassword.value) {
                    if (DOM.confirmPasswordError) {
                        DOM.confirmPasswordError.textContent = 
                            DOM.newPassword.value !== DOM.confirmPassword.value 
                                ? "Mật khẩu xác nhận không khớp" 
                                : '';
                    }
                }
            });
        }

        if (DOM.confirmPassword) {
            DOM.confirmPassword.addEventListener('input', () => {
                if (DOM.confirmPasswordError && DOM.newPassword) {
                    DOM.confirmPasswordError.textContent = 
                        DOM.newPassword.value !== DOM.confirmPassword.value 
                            ? "Mật khẩu xác nhận không khớp" 
                            : '';
                }
            });
        }
    }

    setupEventListeners();
});