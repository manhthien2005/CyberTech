// Category page filter functionality

document.addEventListener("DOMContentLoaded", () => {
    // Filter selects
    const filterSelects = document.querySelectorAll(".filter-select")

    filterSelects.forEach((select) => {
        select.addEventListener("change", () => {
            // Get selected value
            const value = select.value

            // Get all product cards
            const productCards = document.querySelectorAll(".product-card")

            // Sort products based on selected value
            if (value === "price-asc") {
                sortProductsByPrice(productCards, "asc")
            } else if (value === "price-desc") {
                sortProductsByPrice(productCards, "desc")
            } else if (value === "newest") {
                // In a real implementation, this would sort by date
                // For demo purposes, we'll just randomize
                shuffleProducts(productCards)
            } else {
                // Default (popular) - in a real implementation, this would sort by popularity
                // For demo purposes, we'll just reset to original order
                resetProductOrder(productCards)
            }
        })
    })

    // Filter checkboxes
    const filterCheckboxes = document.querySelectorAll('.filter-options input[type="checkbox"]')

    filterCheckboxes.forEach((checkbox) => {
        checkbox.addEventListener("change", () => {
            // Get all checked values
            const checkedBrands = Array.from(
                document.querySelectorAll('.filter-options:nth-of-type(1) input[type="checkbox"]:checked'),
            ).map((cb) => cb.parentElement.textContent.trim().toLowerCase())

            const checkedPriceRanges = Array.from(
                document.querySelectorAll('.filter-options:nth-of-type(2) input[type="checkbox"]:checked'),
            ).map((cb) => cb.parentElement.textContent.trim().toLowerCase())

            // Get all product cards
            const productCards = document.querySelectorAll(".product-card")

            // Filter products
            productCards.forEach((card) => {
                // In a real implementation, we would check actual product data
                // For demo purposes, we'll just show/hide randomly

                // If no filters are selected, show all products
                if (checkedBrands.length === 0 && checkedPriceRanges.length === 0) {
                    card.style.display = ""
                    return
                }

                // Randomly determine if product matches filters
                const matchesBrand = checkedBrands.length === 0 || Math.random() > 0.5
                const matchesPriceRange = checkedPriceRanges.length === 0 || Math.random() > 0.5

                // Show/hide product based on filter match
                if (matchesBrand && matchesPriceRange) {
                    card.style.display = ""
                } else {
                    card.style.display = "none"
                }
            })
        })
    })

    // Sort products by price
    function sortProductsByPrice(products, order) {
        // Convert NodeList to Array for sorting
        const productsArray = Array.from(products)

        // Sort products by price
        productsArray.sort((a, b) => {
            const priceA = getPriceFromProduct(a)
            const priceB = getPriceFromProduct(b)

            return order === "asc" ? priceA - priceB : priceB - priceA
        })

        // Reorder products in the DOM
        const productGrid = products[0].parentElement
        productsArray.forEach((product) => {
            productGrid.appendChild(product)
        })
    }

    // Get price from product card
    function getPriceFromProduct(product) {
        const priceText = product.querySelector(".current-price").textContent
        // Remove non-numeric characters and convert to number
        return Number.parseInt(priceText.replace(/\D/g, ""))
    }

    // Shuffle products (for "newest" sort)
    function shuffleProducts(products) {
        // Convert NodeList to Array for shuffling
        const productsArray = Array.from(products)

        // Fisher-Yates shuffle algorithm
        for (let i = productsArray.length - 1; i > 0; i--) {
            const j = Math.floor(Math.random() * (i + 1))
                ;[productsArray[i], productsArray[j]] = [productsArray[j], productsArray[i]]
        }

        // Reorder products in the DOM
        const productGrid = products[0].parentElement
        productsArray.forEach((product) => {
            productGrid.appendChild(product)
        })
    }

    // Reset product order (for "popular" sort)
    function resetProductOrder(products) {
        // In a real implementation, this would reset to the original order
        // For demo purposes, we'll just sort by product title

        // Convert NodeList to Array for sorting
        const productsArray = Array.from(products)

        // Sort products by title
        productsArray.sort((a, b) => {
            const titleA = a.querySelector(".product-title").textContent
            const titleB = b.querySelector(".product-title").textContent

            return titleA.localeCompare(titleB)
        })

        // Reorder products in the DOM
        const productGrid = products[0].parentElement
        productsArray.forEach((product) => {
            productGrid.appendChild(product)
        })
    }
})
