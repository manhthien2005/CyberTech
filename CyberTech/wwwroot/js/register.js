document.addEventListener("DOMContentLoaded", () => {
    const API = { register: '/Account/Register' };
    const DOM = {
        form: document.getElementById('registerForm'),
        name: document.getElementById('name'),
        username: document.getElementById('username'),
        email: document.getElementById('email'),
        password: document.getElementById('password'),
        submitButton: document.querySelector('button[type="submit"]'),
        nameError: document.getElementById('nameError'),
        usernameError: document.getElementById('usernameError'),
        emailError: document.getElementById('emailError'),
        passwordError: document.getElementById('passwordError')
    };

    function clearErrors() {
        DOM.nameError.textContent = '';
        DOM.usernameError.textContent = '';
        DOM.emailError.textContent = '';
        DOM.passwordError.textContent = '';
    }

    function validateForm() {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        const usernameRegex = /^[a-zA-Z0-9_]{3,20}$/;
        clearErrors();

        if (!DOM.name.value) {
            DOM.nameError.textContent = "Họ và tên là bắt buộc";
            return false;
        }

        if (!DOM.username.value) {
            DOM.usernameError.textContent = "Tên người dùng là bắt buộc";
            return false;
        } else if (!usernameRegex.test(DOM.username.value)) {
            DOM.usernameError.textContent = "Tên người dùng phải từ 3-20 ký tự và chỉ chứa chữ cái, số và dấu gạch dưới";
            return false;
        }

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
        } else if (DOM.password.value.length < 6) {
            DOM.passwordError.textContent = "Mật khẩu phải có ít nhất 6 ký tự";
            return false;
        }

        return true;
    }

    function setupEventListeners() {
        DOM.form.addEventListener('submit', async (e) => {
            e.preventDefault();
            if (!validateForm()) return;

            const formData = new FormData();
            formData.append('name', DOM.name.value);
            formData.append('username', DOM.username.value);
            formData.append('email', DOM.email.value);
            formData.append('password', DOM.password.value);

            // Thêm antiforgery token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (token) {
                formData.append('__RequestVerificationToken', token);
            }

            try {
                utils.showLoadingOverlay(true);
                const result = await utils.fetchData(API.register, 'POST', formData);

                if (result.success) {
                    utils.showToast("Đăng ký thành công! Vui lòng kiểm tra email để xác minh tài khoản.", "success");
                    setTimeout(() => {
                        window.location.href = '/Account/RegistrationSuccess';
                    }, 1500);
                } else {
                    if (result.errorMessage.includes("Email")) {
                        DOM.emailError.textContent = result.errorMessage;
                    } else if (result.errorMessage.includes("Tên người dùng")) {
                        DOM.usernameError.textContent = result.errorMessage;
                    } else {
                        utils.showToast(result.errorMessage, "error");
                    }
                }
            } catch (error) {
                console.error('Error:', error);
                const errorMessage = error.message || "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.";
                utils.showToast(errorMessage, "error");
            } finally {
                utils.showLoadingOverlay(false);
            }
        });

        DOM.name.addEventListener('input', () => DOM.nameError.textContent = '');
        DOM.username.addEventListener('input', () => DOM.usernameError.textContent = '');
        DOM.email.addEventListener('input', () => DOM.emailError.textContent = '');
        DOM.password.addEventListener('input', () => DOM.passwordError.textContent = '');
    }

    setupEventListeners();
});