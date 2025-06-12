document.addEventListener("DOMContentLoaded", () => {
    // Tab functionality
    const tabButtons = document.querySelectorAll(".tab-btn")
    const tabContents = document.querySelectorAll(".tab-content")

    tabButtons.forEach((button) => {
        button.addEventListener("click", () => {
            tabButtons.forEach((btn) => btn.classList.remove("active"))
            tabContents.forEach((content) => content.classList.remove("active"))
            button.classList.add("active")
            const tabId = button.getAttribute("data-tab")
            document.getElementById(tabId).classList.add("active")
        })
    })

    // Product gallery functionality
    const mainImage = document.getElementById("mainImage")
    const thumbnails = document.querySelectorAll(".thumbnail")
    const galleryPrevBtn = document.querySelector(".gallery-prev")
    const galleryNextBtn = document.querySelector(".gallery-next")
    
    let currentImageIndex = 0
    const images = Array.from(thumbnails).map(thumb => thumb.getAttribute("data-image"))

    // Thumbnail click
    thumbnails.forEach((thumbnail, index) => {
        thumbnail.addEventListener("click", () => {
            currentImageIndex = index
            updateMainImage()
            updateActiveThumbnail()
        })
    })

    // Gallery navigation buttons
    if (galleryPrevBtn && galleryNextBtn) {
        galleryPrevBtn.addEventListener("click", () => {
            currentImageIndex = currentImageIndex > 0 ? currentImageIndex - 1 : images.length - 1
            updateMainImage()
            updateActiveThumbnail()
        })

        galleryNextBtn.addEventListener("click", () => {
            currentImageIndex = currentImageIndex < images.length - 1 ? currentImageIndex + 1 : 0
            updateMainImage()
            updateActiveThumbnail()
        })
    }

    function updateMainImage() {
        if (mainImage && images[currentImageIndex]) {
            mainImage.src = images[currentImageIndex]
        }
    }

    function updateActiveThumbnail() {
        thumbnails.forEach((thumb, index) => {
            thumb.classList.toggle("active", index === currentImageIndex)
        })
    }

    // Quantity controls
    const quantityInput = document.getElementById("quantity")
    const minusBtn = document.querySelector(".quantity-btn.minus")
    const plusBtn = document.querySelector(".quantity-btn.plus")

    if (quantityInput && minusBtn && plusBtn) {
        // Remove any existing event listeners
        minusBtn.replaceWith(minusBtn.cloneNode(true))
        plusBtn.replaceWith(plusBtn.cloneNode(true))
        
        // Get fresh references
        const newMinusBtn = document.querySelector(".quantity-btn.minus")
        const newPlusBtn = document.querySelector(".quantity-btn.plus")
        
        newMinusBtn.addEventListener("click", (e) => {
            e.preventDefault()
            e.stopPropagation()
            
            const currentValue = parseInt(quantityInput.value) || 1
            const minValue = parseInt(quantityInput.min) || 1
            
            if (currentValue > minValue) {
                quantityInput.value = currentValue - 1
            }
        })

        newPlusBtn.addEventListener("click", (e) => {
            e.preventDefault()
            e.stopPropagation()
            
            const currentValue = parseInt(quantityInput.value) || 1
            const maxValue = parseInt(quantityInput.max) || 999
            
            if (currentValue < maxValue) {
                quantityInput.value = currentValue + 1
            }
        })
        
        // Prevent form submission and manual input
        quantityInput.addEventListener("keydown", (e) => {
            if (e.key === "Enter") {
                e.preventDefault()
            }
        })
        
        quantityInput.addEventListener("input", (e) => {
            const value = parseInt(e.target.value) || 1
            const minValue = parseInt(quantityInput.min) || 1
            const maxValue = parseInt(quantityInput.max) || 999
            
            if (value < minValue) {
                e.target.value = minValue
            } else if (value > maxValue) {
                e.target.value = maxValue
            }
        })
    }

    // Related products slider
    const sliderContainer = document.querySelector(".product-grid-slider")
    const sliderPrevBtn = document.querySelector(".slider-prev")
    const sliderNextBtn = document.querySelector(".slider-next")
    
    if (sliderContainer && sliderPrevBtn && sliderNextBtn) {
        let currentSlideIndex = 0
        const slidesToShow = window.innerWidth <= 768 ? 1 : window.innerWidth <= 1024 ? 2 : 3
        const totalSlides = sliderContainer.children.length
        const maxSlideIndex = Math.max(0, totalSlides - slidesToShow)

        function updateSlider() {
            if (sliderContainer.children.length === 0) return;
            
            const slideWidth = sliderContainer.children[0].offsetWidth + 20 // including gap
            const translateX = -currentSlideIndex * slideWidth
            sliderContainer.style.transform = `translateX(${translateX}px)`
            
            // Update button states
            sliderPrevBtn.style.opacity = currentSlideIndex === 0 ? "0.5" : "1"
            sliderNextBtn.style.opacity = currentSlideIndex >= maxSlideIndex ? "0.5" : "1"
            sliderPrevBtn.style.pointerEvents = currentSlideIndex === 0 ? "none" : "auto"
            sliderNextBtn.style.pointerEvents = currentSlideIndex >= maxSlideIndex ? "none" : "auto"
        }

        sliderPrevBtn.addEventListener("click", () => {
            if (currentSlideIndex > 0) {
                currentSlideIndex--
                updateSlider()
            }
        })

        sliderNextBtn.addEventListener("click", () => {
            if (currentSlideIndex < maxSlideIndex) {
                currentSlideIndex++
                updateSlider()
            }
        })

        // Initialize slider
        setTimeout(() => {
            updateSlider()
        }, 100) // Delay để đảm bảo DOM đã render

        // Update on window resize
        window.addEventListener("resize", () => {
            const newSlidesToShow = window.innerWidth <= 768 ? 1 : window.innerWidth <= 1024 ? 2 : 3
            const newMaxSlideIndex = Math.max(0, totalSlides - newSlidesToShow)
            if (currentSlideIndex > newMaxSlideIndex) {
                currentSlideIndex = newMaxSlideIndex
            }
            setTimeout(() => {
                updateSlider()
            }, 100)
        })
    }

    // Star rating functionality
    const starRating = document.querySelectorAll(".star-rating i")
    let selectedRating = 0

    starRating.forEach((star) => {
        star.addEventListener("mouseover", () => {
            const rating = Number.parseInt(star.getAttribute("data-rating"))
            highlightStars(rating)
        })

        star.addEventListener("mouseout", () => {
            highlightStars(selectedRating)
        })

        star.addEventListener("click", () => {
            selectedRating = Number.parseInt(star.getAttribute("data-rating"))
            highlightStars(selectedRating)
        })
    })

    function highlightStars(rating) {
        starRating.forEach((star) => {
            const starRating = Number.parseInt(star.getAttribute("data-rating"))
            if (starRating <= rating) {
                star.classList.remove("far")
                star.classList.add("fas")
                star.classList.add("active")
            } else {
                star.classList.remove("fas")
                star.classList.remove("active")
                star.classList.add("far")
            }
        })
    }

    // Review form submission
    const reviewForm = document.getElementById("reviewForm")
    if (reviewForm) {
        reviewForm.addEventListener("submit", (e) => {
            e.preventDefault()

            const reviewContent = document.getElementById("reviewContent").value

            if (selectedRating === 0) {
                alert("Vui lòng chọn số sao đánh giá!")
                return
            }

            if (reviewContent.trim() === "") {
                alert("Vui lòng nhập nội dung đánh giá!")
                return
            }

            // Here you would typically send the review data to your server
            // For this example, we'll just show a success message
            alert("Cảm ơn bạn đã gửi đánh giá!")

            // Reset form
            reviewForm.reset()
            selectedRating = 0
            highlightStars(0)
        })
    }

    // Add to cart functionality
    const addToCartButton = document.querySelector(".btn-add-cart")
    if (addToCartButton) {
        addToCartButton.addEventListener("click", async () => {
            if (addToCartButton.disabled) return

            const productId = addToCartButton.getAttribute("data-product-id")
            const quantity = parseInt(quantityInput.value)
            
            // Hiệu ứng loading
            const originalText = addToCartButton.innerHTML
            addToCartButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang thêm...'
            addToCartButton.disabled = true
            
            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value
                const response = await fetch(`/Cart/AddToCart?productId=${productId}&quantity=${quantity}`, {
                    method: "POST",
                    headers: { 
                        "Content-Type": "application/json",
                        "X-Requested-With": "XMLHttpRequest",
                        "RequestVerificationToken": token
                    }
                })
                
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`)
                }
                
                const result = await response.json()
                if (result.success) {
                    // Hiệu ứng thành công
                    addToCartButton.innerHTML = '<i class="fas fa-check"></i> Đã thêm'
                    addToCartButton.classList.add("success")
                    
                    // Cập nhật số lượng giỏ hàng nếu có
                    if (result.cartCount) {
                        updateCartCount(result.cartCount)
                    }
                    
                    setTimeout(() => {
                        addToCartButton.innerHTML = originalText
                        addToCartButton.classList.remove("success")
                        addToCartButton.disabled = false
                    }, 2000)
                    
                    showToast("Thêm vào giỏ hàng thành công", "success")
                } else {
                    addToCartButton.innerHTML = originalText
                    addToCartButton.disabled = false
                    showToast(result.message || "Không thể thêm vào giỏ hàng", "error")
                }
            } catch (error) {
                console.error("Error adding to cart:", error)
                addToCartButton.innerHTML = originalText
                addToCartButton.disabled = false
                showToast("Lỗi khi thêm vào giỏ hàng", "error")
            }
        })
    }

    // Keyboard navigation for gallery
    document.addEventListener("keydown", (e) => {
        if (e.key === "ArrowLeft" && galleryPrevBtn) {
            galleryPrevBtn.click()
        } else if (e.key === "ArrowRight" && galleryNextBtn) {
            galleryNextBtn.click()
        }
    })

    // Add to Compare functionality
    const compareButton = document.querySelector(".btn-compare")
    if (compareButton) {
        compareButton.addEventListener("click", async () => {
            if (compareButton.disabled) return

            const productId = compareButton.getAttribute("data-product-id")
            
            // Hiệu ứng loading
            const originalIcon = compareButton.innerHTML
            compareButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i>'
            compareButton.disabled = true
            
            try {
                const token = document.querySelector('input[name="__RequestVerificationToken"]').value
                const response = await fetch(`/ProductCompare/AddToCompare`, {
                    method: "POST",
                    headers: { 
                        "Content-Type": "application/x-www-form-urlencoded",
                        "X-Requested-With": "XMLHttpRequest",
                        "RequestVerificationToken": token
                    },
                    body: `productId=${productId}`
                })
                
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`)
                }
                
                const result = await response.json()
                if (result.success) {
                    // Hiệu ứng thành công
                    compareButton.innerHTML = '<i class="fas fa-check"></i>'
                    compareButton.classList.add("active")
                    
                    setTimeout(() => {
                        compareButton.innerHTML = originalIcon
                        compareButton.disabled = false
                    }, 2000)
                    
                    // Thông báo được xử lý bởi utils.showToast trong ProductDetail.cshtml
                } else {
                    compareButton.innerHTML = originalIcon
                    compareButton.disabled = false
                    // Thông báo lỗi được xử lý bởi utils.showToast trong ProductDetail.cshtml
                }
            } catch (error) {
                console.error("Error adding to compare:", error)
                compareButton.innerHTML = originalIcon
                compareButton.disabled = false
                // Thông báo lỗi được xử lý bởi utils.showToast trong ProductDetail.cshtml
            }
        })
    }
})

// Utility functions
function updateCartCount(count) {
    const cartBadge = document.querySelector(".cart-count")
    if (cartBadge) {
        cartBadge.textContent = count
        cartBadge.classList.add("updated")
        setTimeout(() => {
            cartBadge.classList.remove("updated")
        }, 300)
    }
}

function showToast(message, type) {
    // Create toast element if it doesn't exist
    let toast = document.querySelector(".toast")
    if (!toast) {
        toast = document.createElement("div")
        toast.className = "toast"
        document.body.appendChild(toast)
    }

    // Set toast content and type
    toast.innerHTML = `
        <div class="toast-content">
            <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
            ${message}
        </div>
    `
    toast.className = `toast toast-${type}`

    // Show toast
    setTimeout(() => {
        toast.classList.add("show")
    }, 100)

    // Hide toast after 3 seconds
    setTimeout(() => {
        toast.classList.remove("show")
    }, 3000)
}
