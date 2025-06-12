
document.addEventListener("DOMContentLoaded", () => {
    // Quản lý trạng thái ứng dụng
    const state = {
        // Dữ liệu chính của tính năng
        data: {},
        // Các bộ lọc và cài đặt
        filters: {
            timeRange: 'month',
            // Thêm các bộ lọc khác tùy theo tính năng
        }
    };

    // Các endpoint API
    const API = {
        // Định nghĩa các endpoint API cần thiết
        getData: '/Feature/GetData',
        getFilteredData: (filter) => `/Feature/GetFilteredData?filter=${filter}`,
        // Thêm các endpoint khác tùy theo tính năng
    };

    // Các phần tử DOM
    const DOM = {
        // Các phần tử chính
        container: document.querySelector('.feature-container'),
        filterSelect: document.getElementById('filterSelect'),
        dataList: document.querySelector('.data-list'),
        // Thêm các phần tử DOM khác tùy theo tính năng
    };

    // Khởi tạo ứng dụng
    async function initialize() {
        try {
            // Tải dữ liệu ban đầu
            await Promise.all([
                loadData(),
                // Thêm các hàm tải dữ liệu khác nếu cần
            ]);
            
            // Thiết lập các sự kiện
            setupEventListeners();
        } catch (error) {
            console.error("Lỗi khởi tạo:", error);
            utils.showToast("Đã xảy ra lỗi khi tải dữ liệu", "error");
        }
    }

    // Hiển thị skeleton loader
    function showSkeletons() {
        DOM.dataList.innerHTML = Array(5).fill().map(() => `
            <div class="item skeleton-item">
                <div class="item-title skeleton"></div>
                <div class="item-content skeleton"></div>
            </div>
        `).join('');
    }

    // Tải dữ liệu
    async function loadData() {
        try {
            utils.showLoadingOverlay(true);
            showSkeletons();
            const result = await utils.fetchData(API.getData);
            state.data = result;
            renderData(result);
        } catch (error) {
            utils.showToast("Không thể tải dữ liệu", "error");
            DOM.dataList.innerHTML = '<div class="text-center py-4">Không thể tải dữ liệu. Vui lòng thử lại sau.</div>';
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Tải dữ liệu có bộ lọc
    async function loadFilteredData(filter) {
        try {
            utils.showLoadingOverlay(true);
            showSkeletons();
            state.filters.timeRange = filter;
            const result = await utils.fetchData(API.getFilteredData(filter));
            state.data = result;
            renderData(result);
        } catch (error) {
            utils.showToast("Không thể tải dữ liệu đã lọc", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Hiển thị dữ liệu
    function renderData(data) {
        // Kiểm tra dữ liệu trống
        if (!data || data.length === 0) {
            DOM.dataList.innerHTML = '<div class="text-center py-4">Không có dữ liệu.</div>';
            return;
        }

        // Tạo HTML từ dữ liệu
        let html = '';
        data.forEach(item => {
            html += `
            <div class="item">
                <div class="item-title">${item.title}</div>
                <div class="item-content">${item.content}</div>
                <div class="item-actions">
                    <button class="btn btn-sm btn-primary view-btn" data-id="${item.id}">
                        <i class="fas fa-eye me-1"></i> Xem
                    </button>
                    <button class="btn btn-sm btn-secondary edit-btn" data-id="${item.id}">
                        <i class="fas fa-edit me-1"></i> Sửa
                    </button>
                </div>
            </div>
            `;
        });
        
        DOM.dataList.innerHTML = html;
        
        // Thiết lập sự kiện cho các nút trong danh sách
        setupItemEvents();
    }

    // Thiết lập sự kiện cho các phần tử trong danh sách
    function setupItemEvents() {
        // Xử lý sự kiện nút Xem
        document.querySelectorAll('.view-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const itemId = btn.getAttribute('data-id');
                viewItem(itemId);
            });
        });
        
        // Xử lý sự kiện nút Sửa
        document.querySelectorAll('.edit-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const itemId = btn.getAttribute('data-id');
                editItem(itemId);
            });
        });
    }

    // Xem chi tiết một mục
    function viewItem(id) {
        const item = state.data.find(i => i.id === parseInt(id));
        if (!item) {
            utils.showToast("Không tìm thấy mục", "error");
            return;
        }
        
        // Xử lý hiển thị chi tiết
        console.log("Xem chi tiết mục:", item);
        // Thêm code hiển thị modal hoặc chuyển trang
    }

    // Chỉnh sửa một mục
    function editItem(id) {
        const item = state.data.find(i => i.id === parseInt(id));
        if (!item) {
            utils.showToast("Không tìm thấy mục", "error");
            return;
        }
        
        // Xử lý chỉnh sửa
        console.log("Chỉnh sửa mục:", item);
        // Thêm code hiển thị form chỉnh sửa
    }

    // Thiết lập các sự kiện
    function setupEventListeners() {
        // Xử lý sự kiện thay đổi bộ lọc
        DOM.filterSelect?.addEventListener("change", function() {
            loadFilteredData(this.value);
        });
        
        // Thêm các sự kiện khác tùy theo tính năng
    }

    // Khởi chạy ứng dụng
    initialize();
});