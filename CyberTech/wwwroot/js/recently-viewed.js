// ========================================
// RECENTLY VIEWED PRODUCTS HELPER
// ========================================

/**
 * Thêm sản phẩm vào danh sách đã xem trong localStorage
 * @param {number} productId - ID sản phẩm
 * @param {string} productName - Tên sản phẩm  
 * @param {string} productImage - URL hình ảnh sản phẩm
 * @param {number} currentPrice - Giá hiện tại
 * @param {number} originalPrice - Giá gốc (không bắt buộc)
 * @param {string} productUrl - URL đến trang chi tiết sản phẩm
 * @param {number} rating - Rating trung bình (không bắt buộc)
 * @param {number} reviewCount - Số lượng đánh giá (không bắt buộc)
 */
function addToRecentlyViewed(productId, productName, productImage, currentPrice, originalPrice, productUrl, rating, reviewCount) {
    try {
        const productData = {
            id: productId,
            name: productName,
            image: productImage,
            currentPrice: parseFloat(currentPrice),
            originalPrice: originalPrice ? parseFloat(originalPrice) : parseFloat(currentPrice),
            url: productUrl,
            rating: rating || 0,
            reviewCount: reviewCount || 0,
            timestamp: Date.now()
        };
        
        // Lấy danh sách hiện tại từ localStorage
        let recentlyViewed = JSON.parse(localStorage.getItem('recentlyViewedProducts') || '[]');
        
        // Loại bỏ sản phẩm nếu đã tồn tại (tránh duplicate)
        recentlyViewed = recentlyViewed.filter(item => item.id !== productData.id);
        
        // Thêm sản phẩm mới vào đầu array
        recentlyViewed.unshift(productData);
        
        // Giới hạn tối đa 10 sản phẩm
        if (recentlyViewed.length > 10) {
            recentlyViewed = recentlyViewed.slice(0, 10);
        }
        
        // Lưu vào localStorage
        localStorage.setItem('recentlyViewedProducts', JSON.stringify(recentlyViewed));
        
        console.log('Product added to recently viewed:', productData);
        
        // Trigger refresh recently viewed display nếu đang ở trang home
        if (typeof refreshRecentlyViewed === 'function') {
            refreshRecentlyViewed();
        }
    } catch (error) {
        console.error('Error adding product to recently viewed:', error);
    }
}

/**
 * Lấy danh sách sản phẩm đã xem
 * @returns {Array} Mảng các sản phẩm đã xem
 */
function getRecentlyViewedProducts() {
    try {
        return JSON.parse(localStorage.getItem('recentlyViewedProducts') || '[]');
    } catch (error) {
        console.error('Error getting recently viewed products:', error);
        return [];
    }
}

/**
 * Xóa tất cả sản phẩm đã xem
 */
function clearRecentlyViewed() {
    try {
        localStorage.removeItem('recentlyViewedProducts');
        console.log('Recently viewed products cleared');
        
        // Ẩn section nếu đang ở trang home
        const section = document.getElementById('recentlyViewedSection');
        if (section) {
            section.style.display = 'none';
        }
    } catch (error) {
        console.error('Error clearing recently viewed products:', error);
    }
}

/**
 * Xóa một sản phẩm cụ thể khỏi danh sách đã xem
 * @param {number} productId - ID sản phẩm cần xóa
 */
function removeFromRecentlyViewed(productId) {
    try {
        let recentlyViewed = JSON.parse(localStorage.getItem('recentlyViewedProducts') || '[]');
        recentlyViewed = recentlyViewed.filter(item => item.id !== productId);
        localStorage.setItem('recentlyViewedProducts', JSON.stringify(recentlyViewed));
        
        console.log('Product removed from recently viewed:', productId);
        
        // Refresh display nếu đang ở trang home
        if (typeof loadRecentlyViewedProducts === 'function') {
            loadRecentlyViewedProducts();
        }
    } catch (error) {
        console.error('Error removing product from recently viewed:', error);
    }
}

// Làm cho các function có thể truy cập từ global scope
window.addToRecentlyViewed = addToRecentlyViewed;
window.getRecentlyViewedProducts = getRecentlyViewedProducts;
window.clearRecentlyViewed = clearRecentlyViewed;
window.removeFromRecentlyViewed = removeFromRecentlyViewed; 