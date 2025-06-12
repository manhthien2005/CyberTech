document.addEventListener("DOMContentLoaded", () => {
    const API = { login: '/Account/Login' };
    const DOM = {
        form: document.getElementById('loginForm'),
        email: document.getElementById('email'),
        password: document.getElementById('password'),
        rememberMe: document.getElementById('rememberMe'),
        submitButton: document.querySelector('button[type="submit"]'),
        emailError: document.getElementById('emailError'),
        passwordError: document.getElementById('passwordError')
    };

    function clearErrors() {
        DOM.emailError.textContent = '';
        DOM.passwordError.textContent = '';
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

        if (!DOM.password.value) {
            DOM.passwordError.textContent = "Mật khẩu là bắt buộc";
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
            formData.append('password', DOM.password.value);
            formData.append('rememberMe', DOM.rememberMe.checked);

            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (token) {
                formData.append('__RequestVerificationToken', token);
            }

            try {
                utils.showLoadingOverlay(true);
                const result = await utils.fetchData(API.login, 'POST', formData);

                if (result.success) {
                    utils.showToast("Đăng nhập thành công!", "success");
                    setTimeout(() => {
                        window.location.href = result.returnUrl || '/';
                    }, 1000);
                } else {
                    DOM.passwordError.textContent = result.errorMessage || "Email hoặc mật khẩu không đúng";
                    utils.showToast(result.errorMessage || "Đăng nhập thất bại", "error");
                }
            } catch (error) {
                console.error('Error:', error);
                const errorMessage = error.message || "Đã xảy ra lỗi khi đăng nhập. Vui lòng thử lại sau.";
                DOM.passwordError.textContent = errorMessage;
                utils.showToast(errorMessage, "error");
            } finally {
                utils.showLoadingOverlay(false);
            }
        });

        DOM.email.addEventListener('input', () => DOM.emailError.textContent = '');
        DOM.password.addEventListener('input', () => DOM.passwordError.textContent = '');
    }

    setupEventListeners();
});