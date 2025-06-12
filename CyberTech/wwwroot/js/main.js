// Main JavaScript file for the entire application
document.addEventListener("DOMContentLoaded", () => {
    initializeCart();
    initializeQuantityControls();
    initializeToastSystem();
    initializeLoadingOverlay();
    initializePasswordToggle();
    initializeTooltips();
    initializePopovers();
});

// Cart functionality
function initializeCart() {
    // Add to cart functionality
    const addToCartButtons = document.querySelectorAll(".btn-add-cart");
    addToCartButtons.forEach(button => {
        button.addEventListener("click", handleAddToCart);
    });

    // Buy now functionality
    const buyNowButtons = document.querySelectorAll(".btn-buy-now");
    buyNowButtons.forEach(button => {
        button.addEventListener("click", handleBuyNow);
    });
}

async function handleAddToCart(event) {
    const button = event.currentTarget;
    if (button.disabled) return;

    const productId = button.getAttribute("data-product-id");
    const quantityInput = button.closest('.product-actions')?.querySelector('input[type="number"]');
    const quantity = quantityInput ? parseInt(quantityInput.value) : 1;

    // Show loading state
    const originalText = button.innerHTML;
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang thêm...';
    button.disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            throw new Error("Security token not found");
        }

        const response = await fetch(`/Cart/AddToCart?productId=${productId}&quantity=${quantity}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest",
                "RequestVerificationToken": token
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        if (result.success) {
            // Success animation
            button.innerHTML = '<i class="fas fa-check"></i> Đã thêm';
            button.classList.add("success");

            // Update cart count if available
            if (result.cartCount) {
                updateCartCount(result.cartCount);
            }

            // Reset button after delay
            setTimeout(() => {
                button.innerHTML = originalText;
                button.classList.remove("success");
                button.disabled = false;
            }, 2000);

            showToast("Thêm vào giỏ hàng thành công", "success");
        } else {
            // Check if login is required
            if (result.requireLogin) {
                // Reset button state
                button.innerHTML = originalText;
                button.disabled = false;
                
                // Show login toast with action button
                showLoginToast(result.message || 'Bạn cần đăng nhập để thêm sản phẩm vào giỏ hàng');
            } else {
                throw new Error(result.message || "Không thể thêm vào giỏ hàng");
            }
        }
    } catch (error) {
        console.error("Error adding to cart:", error);
        button.innerHTML = originalText;
        button.disabled = false;
        showToast(error.message || "Lỗi khi thêm vào giỏ hàng", "error");
    }
}

async function handleBuyNow(event) {
    const button = event.currentTarget;
    if (button.disabled) return;

    const productId = button.getAttribute("data-product-id");
    const quantityInput = button.closest('.product-actions')?.querySelector('input[type="number"]');
    const quantity = quantityInput ? parseInt(quantityInput.value) : 1;

    // Show loading state
    const originalText = button.innerHTML;
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...';
    button.disabled = true;

    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            throw new Error("Security token not found");
        }

        const response = await fetch(`/Cart/AddToCart?productId=${productId}&quantity=${quantity}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest",
                "RequestVerificationToken": token
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        if (result.success) {
            // Update cart count if available
            if (result.cartCount) {
                updateCartCount(result.cartCount);
            }
            // Redirect to checkout
            window.location.href = "/Cart/Checkout";
        } else {
            // Check if login is required
            if (result.requireLogin) {
                // Reset button state
                button.innerHTML = originalText;
                button.disabled = false;
                
                // Show login toast with action button
                showLoginToast(result.message || 'Bạn cần đăng nhập để mua sản phẩm');
            } else {
                throw new Error(result.message || "Không thể xử lý đơn hàng");
            }
        }
    } catch (error) {
        console.error("Error processing buy now:", error);
        button.innerHTML = originalText;
        button.disabled = false;
        showToast(error.message || "Lỗi khi xử lý đơn hàng", "error");
    }
}

// Quantity controls
function initializeQuantityControls() {
    const quantityControls = document.querySelectorAll('.quantity-control');
    quantityControls.forEach(control => {
        const input = control.querySelector('input[type="number"]');
        const minusBtn = control.querySelector('.minus');
        const plusBtn = control.querySelector('.plus');

        if (input && minusBtn && plusBtn) {
            minusBtn.addEventListener('click', () => {
                const currentValue = parseInt(input.value);
                if (currentValue > parseInt(input.min)) {
                    input.value = currentValue - 1;
                    input.dispatchEvent(new Event('change'));
                }
            });

            plusBtn.addEventListener('click', () => {
                const currentValue = parseInt(input.value);
                if (currentValue < parseInt(input.max)) {
                    input.value = currentValue + 1;
                    input.dispatchEvent(new Event('change'));
                }
            });

            input.addEventListener('change', () => {
                let value = parseInt(input.value);
                if (isNaN(value) || value < parseInt(input.min)) value = parseInt(input.min);
                if (value > parseInt(input.max)) value = parseInt(input.max);
                input.value = value;
            });
        }
    });
}

// Toast notification system
function initializeToastSystem() {
    // Add toast container if not exists
    if (!document.getElementById('toast-container')) {
        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
}

function showToast(message, type = 'info') {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
        <div class="toast-content">
            <i class="fas ${getToastIcon(type)}"></i>
            <span>${message}</span>
        </div>
    `;

    container.appendChild(toast);
    setTimeout(() => toast.classList.add('show'), 100);

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

function showLoginToast(message) {
    const container = document.getElementById('toast-container');
    const toast = document.createElement('div');
    toast.className = 'toast toast-warning login-toast';
    toast.innerHTML = `
        <div class="toast-content">
            <i class="fas fa-user-lock"></i>
            <span>${message}</span>
        </div>
        <div class="toast-actions">
            <button class="btn-login">Đăng nhập</button>
        </div>
    `;

    container.appendChild(toast);
    setTimeout(() => toast.classList.add('show'), 100);

    // Add login button click handler
    const loginButton = toast.querySelector('.btn-login');
    if (loginButton) {
        loginButton.addEventListener('click', () => {
            window.location.href = '/Account/Login?returnUrl=' + encodeURIComponent(window.location.pathname);
        });
    }

    // Auto-hide after 5 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}

function getToastIcon(type) {
    switch (type) {
        case 'success': return 'fa-check-circle';
        case 'error': return 'fa-exclamation-circle';
        case 'warning': return 'fa-exclamation-triangle';
        default: return 'fa-info-circle';
    }
}

// Loading overlay
function initializeLoadingOverlay() {
    // Add loading overlay if not exists
    if (!document.getElementById('loading-overlay')) {
        const overlay = document.createElement('div');
        overlay.id = 'loading-overlay';
        overlay.className = 'loading-overlay';
        overlay.innerHTML = `
            <div class="spinner-border text-primary" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        `;
        document.body.appendChild(overlay);
    }
}

function showLoadingOverlay(show = true) {
    const overlay = document.getElementById('loading-overlay');
    if (overlay) {
        overlay.style.display = show ? 'flex' : 'none';
    }
}

// Cart count update
function updateCartCount(count) {
    const cartCountElements = document.querySelectorAll('.cart-count');
    cartCountElements.forEach(element => {
        element.textContent = count;
        element.classList.add('updated');
        setTimeout(() => element.classList.remove('updated'), 1000);
    });
}

// Add required styles
function addRequiredStyles() {
    if (!document.getElementById('main-styles')) {
        const style = document.createElement('style');
        style.id = 'main-styles';
        style.textContent = `
            .toast-container {
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 9999;
            }

            .toast {
                background: white;
                border-radius: 4px;
                padding: 12px 20px;
                margin-bottom: 10px;
                box-shadow: 0 2px 5px rgba(0,0,0,0.2);
                opacity: 0;
                transform: translateX(100%);
                transition: all 0.3s ease;
            }

            .toast.show {
                opacity: 1;
                transform: translateX(0);
            }

            .toast-success { border-left: 4px solid #28a745; }
            .toast-error { border-left: 4px solid #dc3545; }
            .toast-warning { border-left: 4px solid #ffc107; }
            .toast-info { border-left: 4px solid #17a2b8; }

            .toast-content {
                display: flex;
                align-items: center;
            }

            .toast-content i {
                margin-right: 10px;
            }

            .loading-overlay {
                position: fixed;
                top: 0;
                left: 0;
                width: 100%;
                height: 100%;
                background: rgba(255,255,255,0.8);
                display: none;
                justify-content: center;
                align-items: center;
                z-index: 9999;
            }

            .cart-count {
                transition: transform 0.3s;
            }

            .cart-count.updated {
                transform: scale(1.2);
            }

            .btn-add-cart.success {
                background-color: #28a745;
                border-color: #28a745;
            }
        `;
        document.head.appendChild(style);
    }
}

// Initialize styles
addRequiredStyles();

// Password visibility toggle functionality
function initializePasswordToggle() {
    const passwordToggles = document.querySelectorAll('.password-toggle');
    passwordToggles.forEach(toggle => {
        toggle.addEventListener('click', function() {
            const input = this.previousElementSibling;
            const type = input.getAttribute('type') === 'password' ? 'text' : 'password';
            input.setAttribute('type', type);
            this.querySelector('i').classList.toggle('fa-eye');
            this.querySelector('i').classList.toggle('fa-eye-slash');
        });
    });
}

function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    tooltipTriggerList.map(function(tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

function initializePopovers() {
    const popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    popoverTriggerList.map(function(popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
}

// Handle image loading errors
document.addEventListener('error', function(e) {
    if (e.target.tagName.toLowerCase() === 'img') {
        e.target.src = '/images/no-image.png';
    }
}, true); 