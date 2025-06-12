// Product slider functionality

document.addEventListener("DOMContentLoaded", () => {
    // Initialize product sliders
    const productSliders = document.querySelectorAll(".product-slider")

    productSliders.forEach((slider) => {
        // Create slider controls
        const sliderControls = document.createElement("div")
        sliderControls.className = "slider-controls"
        sliderControls.innerHTML = `
            <button class="slider-prev"><i class="fas fa-chevron-left"></i></button>
            <button class="slider-next"><i class="fas fa-chevron-right"></i></button>
        `

        // Add controls to slider
        slider.appendChild(sliderControls)

        // Get the existing slider track
        const sliderTrack = slider.querySelector(".slider-track")
        if (!sliderTrack) return; // Exit if,No track found

        // Ensure track styles
        sliderTrack.style.width = "100%"
        sliderTrack.style.display = "flex"
        sliderTrack.style.transition = "transform 0.3s ease"

        // Get slider controls
        const prevBtn = slider.querySelector(".slider-prev")
        const nextBtn = slider.querySelector(".slider-next")

        // Set initial position
        let position = 0
        const itemWidth = 230 // Width: 180px card + 20px margin (10px mỗi bên)
        
        // Tính toán động số sản phẩm hiển thị và giới hạn
        function calculateLimits() {
            const sliderWidth = slider.offsetWidth
            const totalItems = sliderTrack.children.length
            const visibleItems = Math.floor(sliderWidth / itemWidth)
            
            // Sửa logic: maxPosition phải tính chính xác để không vượt quá
            const maxPosition = Math.max(0, totalItems - visibleItems)
            
            console.log(`Slider width: ${sliderWidth}px, Item width: ${itemWidth}px`)
            console.log(`Total items: ${totalItems}, Visible: ${visibleItems}, Max position: ${maxPosition}`)
            return { visibleItems, maxPosition }
        }
        
        let { visibleItems, maxPosition } = calculateLimits()

        // Update slider position
        function updateSliderPosition() {
            // Cập nhật lại maxPosition để đảm bảo tính toán chính xác
            const currentLimits = calculateLimits()
            maxPosition = currentLimits.maxPosition
            
            // Giới hạn position trong phạm vi hợp lệ
            position = Math.max(0, Math.min(position, maxPosition))
            
            // Di chuyển chính xác theo itemWidth
            const translateX = position * itemWidth
            sliderTrack.style.transform = `translateX(${-translateX}px)`

            // Update button states - KIỂM TRA CHẶT CHẼ
            const isAtStart = position <= 0
            const isAtEnd = position >= maxPosition
            
            prevBtn.disabled = isAtStart
            nextBtn.disabled = isAtEnd

            // Update button appearance
            prevBtn.style.opacity = isAtStart ? "0.5" : "1"
            nextBtn.style.opacity = isAtEnd ? "0.5" : "1"
            
            console.log(`Position: ${position}/${maxPosition}, TranslateX: ${-translateX}px, At end: ${isAtEnd}`)
        }

        // Add event listeners to controls
        prevBtn.addEventListener("click", () => {
            if (position > 0) {
                position--
                updateSliderPosition()
            }
        })

        nextBtn.addEventListener("click", () => {
            if (position < maxPosition) {
                position++
                updateSliderPosition()
            }
        })

        // Initial update
        updateSliderPosition()

        // Update on window resize
        window.addEventListener("resize", () => {
            const newLimits = calculateLimits()
            maxPosition = newLimits.maxPosition
            visibleItems = newLimits.visibleItems
            
            // Điều chỉnh position nếu vượt giới hạn
            if (position > maxPosition) {
                position = maxPosition
            }

            updateSliderPosition()
        })
    })

    // Add slider styles
    const sliderStyles = document.createElement("style")
    sliderStyles.textContent = `
    .product-slider {
        position: relative;
        overflow: hidden;
        margin-bottom: 20px;
        width: 100%;
        max-width: 100%;
        box-sizing: border-box;
    }
    
    .slider-track {
        display: flex;
        transition: transform 0.3s ease;
        width: 100%;
        box-sizing: border-box;
    }
    
    .slider-track .proloop {
        flex: 0 0 220px;
        margin: 0 5px;
        max-width: 220px;
        box-sizing: border-box;
    }
    
    .slider-controls {
        position: absolute;
        top: 50%;
        left: 10px;
        right: 10px;
        transform: translateY(-50%);
        display: flex;
        justify-content: space-between;
        pointer-events: none;
        z-index: 10;
        width: calc(100% - 20px);
    }
    
    .slider-prev, .slider-next {
        width: 40px;
        height: 40px;
        background-color: white;
        border: none;
        border-radius: 50%;
        box-shadow: 0 2px 5px rgba(0, 0, 0, 0.1);
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        pointer-events: auto;
        transition: opacity 0.3s;
        z-index: 20;
    }
    
    .slider-prev:disabled, .slider-next:disabled {
        cursor: not-allowed;
    }
`
    document.head.appendChild(sliderStyles)
})