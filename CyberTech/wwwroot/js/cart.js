document.addEventListener("DOMContentLoaded", () => {
    const cartItems = document.querySelectorAll(".cart-item");
    const totalPrice = document.querySelector(".total-price");
    const finalPrice = document.querySelector(".final-price");
    const checkoutBtn = document.getElementById("checkout-btn");

    // Add hover effect for cart items
    cartItems.forEach(item => {
        item.addEventListener("mouseenter", () => {
            item.style.backgroundColor = "#f8f9fa";
        });

        item.addEventListener("mouseleave", () => {
            item.style.backgroundColor = "";
        });
    });

    cartItems.forEach(item => {
        const productId = item.dataset.productId;
        const quantityInput = item.querySelector('input[type="number"]');
        const decreaseBtn = item.querySelector('.decrease');
        const increaseBtn = item.querySelector('.increase');
        const removeBtn = item.querySelector('.btn-remove');
        const subtotalElement = item.querySelector('.item-subtotal');

        // Add click effect for buttons
        [decreaseBtn, increaseBtn, removeBtn].forEach(btn => {
            btn.addEventListener("mousedown", () => {
                btn.style.transform = "scale(0.95)";
            });

            btn.addEventListener("mouseup", () => {
                btn.style.transform = "";
            });
        });

        // Update quantity
        async function updateQuantity(newQuantity) {
            if (newQuantity <= 0 || newQuantity > 99) return;

            const originalContent = subtotalElement.textContent;
            subtotalElement.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';

            try {
                utils.showLoadingOverlay(true);
                const response = await utils.fetchData(`/Cart/UpdateQuantity?productId=${productId}&quantity=${newQuantity}`, 'POST');
                
                if (response.success) {
                    quantityInput.value = newQuantity;
                    subtotalElement.textContent = response.subtotal.toLocaleString() + ' đ';
                    totalPrice.textContent = response.totalPrice.toLocaleString() + ' đ';
                    finalPrice.textContent = response.totalPrice.toLocaleString() + ' đ';

                    // Add highlight effect
                    subtotalElement.style.backgroundColor = "#e6f7ff";
                    setTimeout(() => {
                        subtotalElement.style.backgroundColor = "";
                    }, 1000);

                    utils.showToast('Cập nhật số lượng thành công', 'success');
                } else {
                    subtotalElement.textContent = originalContent;
                    utils.showToast(response.message, 'error');
                }
            } catch (error) {
                subtotalElement.textContent = originalContent;
                utils.showToast('Có lỗi xảy ra khi cập nhật số lượng', 'error');
            } finally {
                utils.showLoadingOverlay(false);
            }
        }

        // Remove item
        async function removeItem() {
            if (!confirm("Bạn có chắc chắn muốn xóa sản phẩm này khỏi giỏ hàng?")) {
                return;
            }

            item.style.transition = "opacity 0.5s";
            item.style.opacity = "0.5";

            try {
                utils.showLoadingOverlay(true);
                const response = await utils.fetchData(`/Cart/RemoveItem?productId=${productId}`, 'POST');
                
                if (response.success) {
                    item.style.transition = "all 0.5s";
                    item.style.maxHeight = "0";
                    item.style.overflow = "hidden";

                    setTimeout(() => {
                        item.remove();
                    }, 500);

                    totalPrice.textContent = response.totalPrice.toLocaleString() + ' đ';
                    finalPrice.textContent = response.totalPrice.toLocaleString() + ' đ';

                    // If cart is empty, reload page to show empty cart message
                    if (document.querySelectorAll('.cart-item').length === 0) {
                        setTimeout(() => {
                            location.reload();
                        }, 600);
                    }

                    utils.showToast('Đã xóa sản phẩm khỏi giỏ hàng', 'success');
                } else {
                    item.style.opacity = "1";
                    utils.showToast(response.message, 'error');
                }
            } catch (error) {
                item.style.opacity = "1";
                utils.showToast('Có lỗi xảy ra khi xóa sản phẩm', 'error');
            } finally {
                utils.showLoadingOverlay(false);
            }
        }

        // Event listeners
        decreaseBtn.addEventListener('click', () => {
            const currentValue = parseInt(quantityInput.value);
            if (currentValue > 1) {
                updateQuantity(currentValue - 1);
            }
        });

        increaseBtn.addEventListener('click', () => {
            const currentValue = parseInt(quantityInput.value);
            if (currentValue < 99) {
                updateQuantity(currentValue + 1);
            }
        });

        quantityInput.addEventListener('change', () => {
            let value = parseInt(quantityInput.value);
            console.log("value", value);
            if (isNaN(value) || value < 1) {
                value = 1;
                utils.showToast('Số lượng phải là số và lớn hơn 0', 'warning');
            }
            if (value > 99) {
                value = 99;
                utils.showToast('Số lượng tối đa là 99', 'warning');
            }
            quantityInput.value = value;
            updateQuantity(value);
        });

        // Add input event listener to prevent non-numeric input
        quantityInput.addEventListener('input', (e) => {
            const value = e.target.value;
            if (!/^\d*$/.test(value)) {
                e.target.value = value.replace(/\D/g, '');
                utils.showToast('Vui lòng chỉ nhập số', 'warning');
            }
        });

        removeBtn.addEventListener('click', removeItem);
    });

    // Handle checkout button
    if (checkoutBtn) {
        checkoutBtn.addEventListener("click", async (e) => {
            e.preventDefault();
            checkoutBtn.disabled = true;
            const originalContent = checkoutBtn.innerHTML;
            checkoutBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...';

            try {
                const response = await utils.fetchData('/Cart/CreateOrder', 'POST');
                
                if (response.success) {
                    utils.showToast('Đặt hàng thành công!', 'success');
                    window.location.href = '/Home/Index/' + response.orderId;
                } else {
                    checkoutBtn.disabled = false;
                    checkoutBtn.innerHTML = originalContent;
                    utils.showToast(response.message, 'error');
                }
            } catch (error) {
                checkoutBtn.disabled = false;
                checkoutBtn.innerHTML = originalContent;
                utils.showToast('Có lỗi xảy ra khi xử lý thanh toán', 'error');
            }
        });
    }
}); 