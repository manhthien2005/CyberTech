// Product detail page functionality

document.addEventListener("DOMContentLoaded", () => {
    // Product image gallery
    const mainImage = document.querySelector(".main-image img")
    const thumbnails = document.querySelectorAll(".thumbnail")

    if (mainImage && thumbnails.length > 0) {
        thumbnails.forEach((thumbnail) => {
            thumbnail.addEventListener("click", () => {
                // Remove active class from all thumbnails
                thumbnails.forEach((t) => t.classList.remove("active"))

                // Add active class to clicked thumbnail
                thumbnail.classList.add("active")

                // Update main image
                const thumbnailImg = thumbnail.querySelector("img")
                if (thumbnailImg) {
                    mainImage.src = thumbnailImg.src
                }
            })
        })
    }

    // Quantity controls
    const quantityInput = document.querySelector(".quantity-control input")
    const minusBtn = document.querySelector(".quantity-btn.minus")
    const plusBtn = document.querySelector(".quantity-btn.plus")

    if (quantityInput && minusBtn && plusBtn) {
        minusBtn.addEventListener("click", () => {
            const value = Number.parseInt(quantityInput.value)
            if (value > Number.parseInt(quantityInput.min)) {
                quantityInput.value = value - 1
            }
        })

        plusBtn.addEventListener("click", () => {
            const value = Number.parseInt(quantityInput.value)
            if (value < Number.parseInt(quantityInput.max)) {
                quantityInput.value = value + 1
            }
        })
    }

    // Tab controls
    const tabBtns = document.querySelectorAll(".tab-btn")
    const tabPanes = document.querySelectorAll(".tab-pane")

    if (tabBtns.length > 0 && tabPanes.length > 0) {
        tabBtns.forEach((btn) => {
            btn.addEventListener("click", () => {
                // Remove active class from all buttons and panes
                tabBtns.forEach((b) => b.classList.remove("active"))
                tabPanes.forEach((p) => p.classList.remove("active"))

                // Add active class to clicked button
                btn.classList.add("active")

                // Show corresponding tab pane
                const tabId = btn.getAttribute("data-tab")
                const tabPane = document.getElementById(tabId)
                if (tabPane) {
                    tabPane.classList.add("active")
                }
            })
        })
    }

    // Add to cart functionality
    const addToCartBtn = document.querySelector(".product-actions .btn-add-to-cart")

    if (addToCartBtn) {
        addToCartBtn.addEventListener("click", () => {
            // Get product info
            const productTitle = document.querySelector(".product-title").textContent
            const productPrice = document.querySelector(".current-price").textContent
            const quantity = quantityInput ? Number.parseInt(quantityInput.value) : 1

            // Show notification
            showNotification(`Đã thêm "${productTitle}" (x${quantity}) vào giỏ hàng!`)
        })
    }

    // Buy now functionality
    const buyNowBtn = document.querySelector(".product-actions .btn-buy-now")

    if (buyNowBtn) {
        buyNowBtn.addEventListener("click", () => {
            // Redirect to checkout page (would be implemented in a real site)
            alert("Chức năng mua ngay sẽ chuyển đến trang thanh toán")
        })
    }

    // Notification function
    function showNotification(message) {
        // Create notification element
        const notification = document.createElement("div")
        notification.className = "notification"
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fas fa-check-circle"></i>
                <span>${message}</span>
            </div>
            <button class="notification-close"><i class="fas fa-times"></i></button>
        `

        // Add to DOM
        document.body.appendChild(notification)

        // Add active class after a small delay (for animation)
        setTimeout(() => {
            notification.classList.add("active")
        }, 10)

        // Remove notification after 3 seconds
        setTimeout(() => {
            notification.classList.remove("active")
            setTimeout(() => {
                notification.remove()
            }, 300)
        }, 3000)

        // Close button functionality
        const closeBtn = notification.querySelector(".notification-close")
        if (closeBtn) {
            closeBtn.addEventListener("click", () => {
                notification.classList.remove("active")
                setTimeout(() => {
                    notification.remove()
                }, 300)
            })
        }
    }
})
