document.addEventListener("DOMContentLoaded", () => {
    const API = { forgotPassword: '/Account/ForgotPassword' };
    const DOM = {
        form: document.getElementById('forgotPasswordForm'),
        email: document.getElementById('email'),
        submitButton: document.querySelector('button[type="submit"]'),
        emailError: document.getElementById('emailError')
    };

    function clearErrors() {
        DOM.emailError.textContent = '';
    }

    function validateForm() {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        clearErrors();

        if (!DOM.email.value) {
            DOM.emailError.textContent = "Email là bắt buộc";
            return false;
        } else if (!emailRegex.test(DOM.email.value)) {
            DOM.emailError.textContent = "Vui lòng nhập email hợp lệ";
            return false;
        }
        return true;
    }

    function setupEventListeners() {
        DOM.form.addEventListener('submit', async (e) => {
            e.preventDefault();
            if (!validateForm()) return;

            const formData = new FormData();
            formData.append('email', DOM.email.value);

            // Thêm antiforgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (token) {
                formData.append('__RequestVerificationToken', token);
            }

            try {
                utils.showLoadingOverlay(true);
                DOM.submitButton.disabled = true;

                const result = await utils.fetchData(API.forgotPassword, 'POST', formData);

                if (result.success) {
                    utils.showToast("Yêu cầu đặt lại mật khẩu đã được gửi!", "success");
                    setTimeout(() => window.location.href = '/Account/ResetPasswordConfirmation', 1500);
                } else {
                    DOM.emailError.textContent = result.errorMessage || "Không thể gửi yêu cầu";
                    utils.showToast(result.errorMessage || "Không thể gửi yêu cầu", "error");
                }
            } catch (error) {
                console.error('Error:', error);
                const errorMessage = error.message || "Đã xảy ra lỗi khi gửi yêu cầu. Vui lòng thử lại sau.";
                DOM.emailError.textContent = errorMessage;
                utils.showToast(errorMessage, "error");
            } finally {
                utils.showLoadingOverlay(false);
                DOM.submitButton.disabled = false;
            }
        });

        DOM.email.addEventListener('input', () => DOM.emailError.textContent = '');
    }

    setupEventListeners();
});