document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        orders: [],
        pagination: {
            currentPage: 1,
            pageSize: 6,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        sortFilter: "date_desc",
        statusFilter: "",
        dateFilter: {
            startDate: "",
            endDate: ""
        }
    };

    // API Endpoints
    const API = {
        orders: (page, pageSize, searchTerm, sortFilter, statusFilter, startDate, endDate) =>
            `/OrderManage/GetOrders?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (sortFilter ? `&sortBy=${sortFilter}` : '') +
            (statusFilter ? `&status=${statusFilter}` : '') +
            (startDate ? `&startDate=${startDate}` : '') +
            (endDate ? `&endDate=${endDate}` : ''),
        order: (id) => `/OrderManage/GetOrder/${id}`,
        updateOrder: '/OrderManage/UpdateOrder',
        deleteOrder: (id) => `/OrderManage/DeleteOrder?id=${id}`
    };

    // DOM Elements
    const DOM = {
        orderGrid: document.getElementById("orderGrid"),
        orderSearchInput: document.getElementById("orderSearchInput"),
        updateOrderBtn: document.getElementById("updateOrderBtn"),
        printOrderBtn: document.getElementById("printOrderBtn"),
        downloadPdfBtn: document.getElementById("downloadPdfBtn"),
        paginationContainer: document.getElementById("orderPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        sortFilter: document.getElementById("sortFilter"),
        statusFilter: document.getElementById("statusFilter"),
        startDateFilter: document.getElementById("startDateFilter"),
        endDateFilter: document.getElementById("endDateFilter"),
        quickDateFilter: document.getElementById("quickDateFilter"),
        applyFiltersBtn: document.getElementById("applyFiltersBtn"),
        editOrderId: document.getElementById("editOrderId"),
        editOrderStatus: document.getElementById("editOrderStatus"),
        displayOrderId: document.getElementById("displayOrderId"),
        displayOrderDate: document.getElementById("displayOrderDate"),
        recipientName: document.getElementById("recipientName"),
        recipientPhone: document.getElementById("recipientPhone"),
        shippingAddress: document.getElementById("shippingAddress"),
        orderItemsTable: document.getElementById("orderItemsTable"),
        orderSubtotal: document.getElementById("orderSubtotal"),
        rankDiscount: document.getElementById("rankDiscount"),
        voucherDiscount: document.getElementById("voucherDiscount"),
        productDiscount: document.getElementById("productDiscount"),
        orderTotal: document.getElementById("orderTotal"),
        deleteOrderName: document.getElementById("deleteOrderName"),
        deleteOrderId: document.getElementById("deleteOrderId"),
        confirmDeleteOrderBtn: document.getElementById("confirmDeleteOrderBtn"),
        // Print elements
        printOrderTemplate: document.getElementById("printOrderTemplate"),
        printOrderId: document.getElementById("printOrderId"),
        printOrderDate: document.getElementById("printOrderDate"),
        printCustomerName: document.getElementById("printCustomerName"),
        printCustomerPhone: document.getElementById("printCustomerPhone"),
        printCustomerAddress: document.getElementById("printCustomerAddress"),
        printOrderItems: document.getElementById("printOrderItems"),
        printSubtotal: document.getElementById("printSubtotal"),
        printRankDiscount: document.getElementById("printRankDiscount"),
        printVoucherDiscount: document.getElementById("printVoucherDiscount"),
        printProductDiscount: document.getElementById("printProductDiscount"),
        printTotal: document.getElementById("printTotal")
    };

    // Skeleton Loaders
    function showOrderSkeletons() {
        const skeletonCount = 6;
        DOM.orderGrid.innerHTML = Array(skeletonCount).fill().map(() => `
            <div class="col">
                <div class="order-card">
                    <div class="order-card-header">
                        <div class="skeleton" style="width: 70%; height: 24px;"></div>
                        <div class="skeleton" style="width: 40%; height: 16px; margin-top: 8px;"></div>
                    </div>
                    <div class="order-card-body">
                        <div class="skeleton" style="width: 60%; height: 16px; margin-bottom: 8px;"></div>
                        <div class="skeleton" style="width: 80%; height: 16px; margin-bottom: 8px;"></div>
                        <div class="skeleton" style="width: 50%; height: 16px;"></div>
                    </div>
                    <div class="order-card-footer">
                        <div class="skeleton" style="width: 100%; height: 36px;"></div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    // Helper Functions
    function getStatusClass(status) {
        switch (status) {
            case "Pending": return "status-pending";
            case "Processing": return "status-processing";
            case "Shipped": return "status-shipped";
            case "Delivered": return "status-delivered";
            case "Cancelled": return "status-cancelled";
            default: return "status-pending";
        }
    }

    function getStatusText(status) {
        switch (status) {
            case "Pending": return "Chờ duyệt";
            case "Processing": return "Đang xử lý";
            case "Shipped": return "Đang giao";
            case "Delivered": return "Đã giao";
            case "Cancelled": return "Đã hủy";
            default: return status;
        }
    }

    function formatDate(dateString) {
        if (!dateString) return "";
        const date = new Date(dateString);
        return date.toLocaleDateString('vi-VN', { 
            year: 'numeric', 
            month: '2-digit', 
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    function resetEditForm() {
        const form = document.getElementById("editOrderForm");
        form.reset();
        DOM.orderItemsTable.innerHTML = '';
        document.getElementById("hiddenProducts").innerHTML = '';
        DOM.displayOrderId.textContent = '';
        DOM.displayOrderDate.textContent = '';
        DOM.recipientName.textContent = '';
        DOM.recipientPhone.textContent = '';
        DOM.shippingAddress.textContent = '';
        DOM.orderSubtotal.textContent = '';
        DOM.rankDiscount.textContent = '';
        DOM.voucherDiscount.textContent = '';
        DOM.productDiscount.textContent = '';
        DOM.orderTotal.textContent = '';
        
        // Reset current order data
        currentOrderData = null;
        
        // Reset timeline
        const timelineItems = document.querySelectorAll('.timeline-item');
        timelineItems.forEach(item => {
            item.classList.remove('active', 'completed', 'cancelled');
        });
        
        // Reset toggle button
        const toggleBtn = document.getElementById("toggleProductsBtn");
        if (toggleBtn) {
            toggleBtn.classList.add("d-none");
            toggleBtn.classList.remove("collapsed");
            const showMoreText = toggleBtn.querySelector(".show-more-text");
            const showLessText = toggleBtn.querySelector(".show-less-text");
            if (showMoreText) showMoreText.classList.remove("d-none");
            if (showLessText) showLessText.classList.add("d-none");
            const icon = toggleBtn.querySelector("i");
            if (icon) icon.className = "fas fa-chevron-down";
        }
        
        // Make sure the collapsible section is collapsed
        const hiddenProducts = document.getElementById("hiddenProducts");
        if (hiddenProducts) hiddenProducts.classList.remove("show");
    }

    // Data Loading
    async function loadOrders(page = 1, searchTerm = state.searchTerm, sortFilter = state.sortFilter, statusFilter = state.statusFilter, startDate = state.dateFilter.startDate, endDate = state.dateFilter.endDate) {
        try {
            showOrderSkeletons();

            state.pagination.currentPage = page;
            state.searchTerm = searchTerm;
            state.sortFilter = sortFilter;
            state.statusFilter = statusFilter;
            state.dateFilter.startDate = startDate;
            state.dateFilter.endDate = endDate;

            const url = API.orders(page, state.pagination.pageSize, searchTerm, sortFilter, statusFilter, startDate, endDate);
            const result = await utils.fetchData(url);

            if (result.success) {
                state.orders = result.data;
                state.pagination = result.pagination;
                renderOrders();
                renderPagination();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách đơn hàng", "error");
            }
        } catch (error) {
            console.error('Load orders error:', error);
            utils.showToast("Không thể tải danh sách đơn hàng", "error");
        }
    }

    // Rendering Functions
    function renderOrders() {
        DOM.orderGrid.innerHTML = state.orders.length === 0
            ? `
                <div class="col-12 d-flex align-items-center justify-content-center w-100" style="min-height: 200px">
                    <div class="text-center">
                        <i class="fas fa-shopping-cart fa-3x text-muted mb-3"></i>
                        <p class="text-muted mb-0">Không tìm thấy đơn hàng</p>
                    </div>
                </div>
            `
            : state.orders.map(order => `
                <div class="col">
                    <div class="order-card">
                        <div class="order-card-header">
                            <div class="d-flex justify-content-between align-items-center">
                                <h5 class="order-card-title mb-0">Đơn #${order.orderId}</h5>
                                <span class="status-badge ${getStatusClass(order.status)}">${getStatusText(order.status)}</span>
                            </div>
                            <small class="text-muted">${formatDate(order.createdAt)}</small>
                        </div>
                        <div class="order-card-body">
                            <div class="order-card-info">
                                <div class="d-flex justify-content-between">
                                    <div>
                                        <div class="order-card-info-label">
                                            <i class="fas fa-box me-1"></i> Số sản phẩm
                                        </div>
                                        <div class="order-card-info-value">${order.itemCount}</div>
                                    </div>
                                    <div>
                                        <div class="order-card-info-label text-end">
                                            <i class="fas fa-money-bill-wave me-1"></i> Tổng tiền
                                        </div>
                                        <div class="order-card-info-value text-end fw-bold">${utils.formatMoney(order.finalPrice)}</div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="order-card-footer">
                            <div class="btn-group w-100">
                                <button class="btn btn-sm btn-outline-primary edit-order-btn" data-order-id="${order.orderId}" data-bs-toggle="modal" data-bs-target="#editOrderModal">
                                    <i class="fas fa-edit"></i> Chi tiết
                                </button>
                                <button class="btn btn-sm btn-outline-info download-invoice-btn" data-order-id="${order.orderId}">
                                    <i class="fas fa-file-pdf"></i> Hóa đơn
                                </button>
                                <button class="btn btn-sm btn-outline-danger delete-order-btn" data-order-id="${order.orderId}">
                                    <i class="fas fa-ban"></i> Hủy
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `).join('');
    }

    function renderPagination() {
        utils.renderPagination(
            state.pagination,
            DOM.paginationContainer,
            DOM.paginationItems,
            DOM.paginationPrev,
            DOM.paginationNext,
            (pageNum) => loadOrders(pageNum, state.searchTerm, state.sortFilter, state.statusFilter, state.dateFilter.startDate, state.dateFilter.endDate)
        );
    }

    function renderOrderDetails(order) {
        DOM.editOrderId.value = order.orderId;
        DOM.displayOrderId.textContent = order.orderId;
        DOM.displayOrderDate.textContent = formatDate(order.createdAt);
        DOM.editOrderStatus.value = order.status;
        DOM.recipientName.textContent = order.recipientName;
        DOM.recipientPhone.textContent = order.recipientPhone;
        DOM.shippingAddress.textContent = order.shippingAddress;
        DOM.orderSubtotal.textContent = utils.formatMoney(order.totalPrice);
        DOM.rankDiscount.textContent = `-${utils.formatMoney(order.rankDiscountAmount)}`;
        DOM.voucherDiscount.textContent = `-${utils.formatMoney(order.voucherDiscountAmount)}`;
        DOM.productDiscount.textContent = `-${utils.formatMoney(order.productDiscountAmount)}`;
        DOM.orderTotal.textContent = utils.formatMoney(order.finalPrice);

        // Update timeline status
        updateOrderTimeline(order.status);

        // Handle product list with collapsible functionality
        const toggleBtn = document.getElementById("toggleProductsBtn");
        const showMoreText = toggleBtn.querySelector(".show-more-text");
        const showLessText = toggleBtn.querySelector(".show-less-text");
        
        // Reset product lists
        DOM.orderItemsTable.innerHTML = '';
        document.getElementById("hiddenProducts").innerHTML = '';
        toggleBtn.classList.add("d-none");
        
        if (order.orderItems.length > 3) {
            // Show first 3 items in main table
            DOM.orderItemsTable.innerHTML = order.orderItems.slice(0, 3).map(item => createOrderItemRow(item)).join('');
            
            // Show remaining items in collapsible section
            document.getElementById("hiddenProducts").innerHTML = order.orderItems.slice(3).map(item => createOrderItemRow(item)).join('');
            
            // Show toggle button with correct count
            toggleBtn.classList.remove("d-none");
            const remainingCount = order.orderItems.length - 3;
            showMoreText.textContent = `Xem thêm ${remainingCount} sản phẩm`;
            
            // Add event listener for toggle button
            toggleBtn.addEventListener('click', function() {
                const isCollapsed = toggleBtn.classList.contains('collapsed');
                if (isCollapsed) {
                    showMoreText.classList.remove('d-none');
                    showLessText.classList.add('d-none');
                    toggleBtn.querySelector('i').classList.remove('fa-chevron-up');
                    toggleBtn.querySelector('i').classList.add('fa-chevron-down');
                } else {
                    showMoreText.classList.add('d-none');
                    showLessText.classList.remove('d-none');
                    toggleBtn.querySelector('i').classList.remove('fa-chevron-down');
                    toggleBtn.querySelector('i').classList.add('fa-chevron-up');
                }
            });
        } else {
            // Show all items in main table if 3 or fewer
            DOM.orderItemsTable.innerHTML = order.orderItems.map(item => createOrderItemRow(item)).join('');
        }
    }

    // Helper function to create order item row HTML
    function createOrderItemRow(item) {
        return `
            <tr>
                <td>${item.productName}</td>
                <td class="text-center">${item.quantity}</td>
                <td class="text-end">${utils.formatMoney(item.unitPrice)}</td>
                <td class="text-end">
                    ${item.discountAmount > 0 
                        ? `<del class="text-muted">${utils.formatMoney(item.subtotal)}</del><br>${utils.formatMoney(item.finalSubtotal)}`
                        : utils.formatMoney(item.subtotal)
                    }
                </td>
            </tr>
        `;
    }

    // Update timeline based on order status
    function updateOrderTimeline(status) {
        const timelineItems = document.querySelectorAll('.timeline-item');
        
        // Reset all timeline items
        timelineItems.forEach(item => {
            item.classList.remove('active', 'completed', 'cancelled');
        });
        
        // Handle different status cases
        if (status === 'Cancelled') {
            // If cancelled, mark the cancelled item as active
            const cancelledItem = document.querySelector('.timeline-item[data-status="Cancelled"]');
            cancelledItem.classList.add('cancelled');
            return;
        }
        
        // Map of statuses to their order in the flow
        const statusOrder = {
            'Pending': 0,
            'Processing': 1,
            'Shipped': 2,
            'Delivered': 3
        };
        
        // Get the index of the current status
        const currentStatusIndex = statusOrder[status];
        
        // Mark all previous statuses as completed and the current one as active
        timelineItems.forEach(item => {
            const itemStatus = item.getAttribute('data-status');
            if (itemStatus === 'Cancelled') return;
            
            const itemStatusIndex = statusOrder[itemStatus];
            
            if (itemStatusIndex < currentStatusIndex) {
                item.classList.add('completed');
            } else if (itemStatusIndex === currentStatusIndex) {
                item.classList.add('active');
            }
        });
    }

    // CRUD Operations
    async function updateOrder() {
        const form = document.getElementById("editOrderForm");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const order = {
            OrderId: parseInt(DOM.editOrderId.value),
            Status: DOM.editOrderStatus.value
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateOrder, 'POST', order);

            if (result.success) {
                utils.showToast("Cập nhật trạng thái đơn hàng thành công", "success");
                await loadOrders();
                bootstrap.Modal.getInstance(document.getElementById('editOrderModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật trạng thái đơn hàng", "error");
            }
        } catch (error) {
            utils.showToast("Lỗi khi cập nhật trạng thái đơn hàng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteOrder(orderId) {
        const order = state.orders.find(o => o.orderId === orderId);
        if (!order) return;

        DOM.deleteOrderName.textContent = `#${order.orderId}`;
        DOM.deleteOrderId.value = orderId;
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteOrderModal"));
        deleteModal.show();

        const confirmBtn = DOM.confirmDeleteOrderBtn;
        const deleteHandler = async () => {
            try {
                utils.showLoadingOverlay(true);
                const result = await utils.fetchData(API.deleteOrder(orderId), 'POST');

                if (result.success) {
                    utils.showToast("Hủy đơn hàng thành công", "success");
                    await loadOrders();
                    deleteModal.hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi hủy đơn hàng", "error");
                }
            } catch (error) {
                utils.showToast("Lỗi khi hủy đơn hàng", "error");
            } finally {
                utils.showLoadingOverlay(false);
                confirmBtn.removeEventListener("click", deleteHandler);
            }
        };

        confirmBtn.addEventListener("click", deleteHandler);
    }

    async function openEditOrderModal(orderId) {
        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.order(orderId));

            if (result.success) {
                // Store the current order data for printing
                currentOrderData = result.data;
                renderOrderDetails(result.data);
            } else {
                utils.showToast(result.message || "Không thể tải chi tiết đơn hàng", "error");
            }
        } catch (error) {
            utils.showToast("Không thể tải chi tiết đơn hàng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Date Helper Functions
    function getDateRangeFromPreset(preset) {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        
        const startDate = new Date(today);
        const endDate = new Date(today);
        
        switch(preset) {
            case 'today':
                // startDate is already today at midnight
                endDate.setHours(23, 59, 59, 999);
                break;
            case 'yesterday':
                startDate.setDate(today.getDate() - 1);
                endDate.setDate(today.getDate() - 1);
                endDate.setHours(23, 59, 59, 999);
                break;
            case 'last7days':
                startDate.setDate(today.getDate() - 6);
                endDate.setHours(23, 59, 59, 999);
                break;
            case 'last30days':
                startDate.setDate(today.getDate() - 29);
                endDate.setHours(23, 59, 59, 999);
                break;
            case 'thisMonth':
                startDate.setDate(1);
                endDate.setHours(23, 59, 59, 999);
                break;
            case 'lastMonth':
                startDate.setMonth(today.getMonth() - 1);
                startDate.setDate(1);
                endDate.setDate(0); // Last day of previous month
                endDate.setHours(23, 59, 59, 999);
                break;
            default:
                return { startDate: '', endDate: '' };
        }
        
        return {
            startDate: formatDateForInput(startDate),
            endDate: formatDateForInput(endDate)
        };
    }

    function formatDateForInput(date) {
        return date.toISOString().split('T')[0];
    }

    // Print functionality
    function printOrder() {
        // Get current order data
        const orderId = DOM.editOrderId.value;
        const orderData = currentOrderData;
        
        if (!orderData) {
            utils.showToast("Không thể in đơn hàng. Dữ liệu không có sẵn.", "error");
            return;
        }
        
        // Generate PDF filename based on order information
        const orderDate = new Date(orderData.createdAt);
        const formattedDate = orderDate.toISOString().split('T')[0].replace(/-/g, '');
        const filename = `CyberTech_Order_${orderId}_${formattedDate}.pdf`;
        
        // Create a new window for printing
        const printWindow = window.open('', '_blank');
        
        // Populate print template with order data
        const printTemplate = document.createElement('div');
        printTemplate.innerHTML = DOM.printOrderTemplate.innerHTML;
        
        // Set order information
        printTemplate.querySelector('#printOrderId').textContent = orderId;
        printTemplate.querySelector('#printOrderDate').textContent = formatDate(orderData.createdAt);
        printTemplate.querySelector('#printCustomerName').textContent = orderData.recipientName;
        printTemplate.querySelector('#printCustomerPhone').textContent = orderData.recipientPhone;
        printTemplate.querySelector('#printCustomerAddress').textContent = orderData.shippingAddress;
        
        // Populate order items - limit to max 15 items to fit on one landscape page
        const orderItems = orderData.orderItems;
        const maxVisibleItems = 15;
        const displayItems = orderItems.length > maxVisibleItems ? 
            orderItems.slice(0, maxVisibleItems - 1) : orderItems;
        
        let orderItemsHtml = '';
        
        displayItems.forEach((item, index) => {
            orderItemsHtml += `
                <tr>
                    <td style="text-align: center;">${index + 1}</td>
                    <td>${item.productName}</td>
                    <td style="text-align: center;">${item.quantity}</td>
                    <td style="text-align: right;">${utils.formatMoney(item.unitPrice)}</td>
                    <td style="text-align: right;">${utils.formatMoney(item.finalSubtotal)}</td>
                </tr>
            `;
        });
        
        // If there are more items than can fit on the page, add a summary row
        if (orderItems.length > maxVisibleItems) {
            const remainingItems = orderItems.length - (maxVisibleItems - 1);
            const remainingTotal = orderItems.slice(maxVisibleItems - 1).reduce((sum, item) => sum + item.finalSubtotal, 0);
            
            orderItemsHtml += `
                <tr>
                    <td style="text-align: center;">...</td>
                    <td colspan="2">Và ${remainingItems} sản phẩm khác</td>
                    <td style="text-align: right;"></td>
                    <td style="text-align: right;">${utils.formatMoney(remainingTotal)}</td>
                </tr>
            `;
        }
        
        printTemplate.querySelector('#printOrderItems').innerHTML = orderItemsHtml;
        
        // Populate summary
        printTemplate.querySelector('#printSubtotal').textContent = utils.formatMoney(orderData.totalPrice);
        printTemplate.querySelector('#printRankDiscount').textContent = `-${utils.formatMoney(orderData.rankDiscountAmount)}`;
        printTemplate.querySelector('#printVoucherDiscount').textContent = `-${utils.formatMoney(orderData.voucherDiscountAmount)}`;
        printTemplate.querySelector('#printProductDiscount').textContent = `-${utils.formatMoney(orderData.productDiscountAmount)}`;
        printTemplate.querySelector('#printTotal').textContent = utils.formatMoney(orderData.finalPrice);
        
        // Add necessary styles and scripts for PDF generation
        printWindow.document.write(`
            <html>
            <head>
                <title>Đơn hàng #${orderId} - CyberTech</title>
                <style>
                    @page {
                        size: A4 landscape;
                        margin: 10mm;
                    }
                    
                    body {
                        font-family: Arial, sans-serif;
                        margin: 0;
                        padding: 0;
                        font-size: 12px;
                    }
                    
                    .print-order-container {
                        max-width: 100%;
                        margin: 0 auto;
                        padding: 10px;
                    }
                    
                    .print-header {
                        display: flex;
                        margin-bottom: 15px;
                        border-bottom: 1px solid #333;
                        padding-bottom: 10px;
                    }
                    
                    .print-logo {
                        width: 80px;
                        margin-right: 20px;
                    }
                    
                    .print-logo img {
                        max-width: 100%;
                        height: auto;
                    }
                    
                    .print-company-info {
                        flex: 1;
                    }
                    
                    .print-company-info h2 {
                        margin: 0 0 5px 0;
                        font-size: 20px;
                    }
                    
                    .print-company-info p {
                        margin: 2px 0;
                        font-size: 12px;
                    }
                    
                    .print-title {
                        text-align: center;
                        margin-bottom: 15px;
                    }
                    
                    .print-title h1 {
                        font-size: 18px;
                        margin: 0 0 5px 0;
                        text-transform: uppercase;
                    }
                    
                    .print-order-details-row {
                        display: flex;
                        justify-content: space-between;
                        font-size: 12px;
                        margin-top: 5px;
                    }
                    
                    .print-customer-info, .print-order-details, .print-order-summary {
                        margin-bottom: 15px;
                    }
                    
                    .print-customer-info h3, .print-order-details h3 {
                        font-size: 14px;
                        margin: 0 0 8px 0;
                        border-bottom: 1px solid #ddd;
                        padding-bottom: 5px;
                    }
                    
                    .print-info-grid {
                        display: grid;
                        grid-template-columns: 1fr 1fr;
                        gap: 8px;
                    }
                    
                    .print-info-item {
                        display: flex;
                        align-items: baseline;
                    }
                    
                    .print-info-full {
                        grid-column: span 2;
                    }
                    
                    .print-info-label {
                        font-weight: bold;
                        margin-right: 5px;
                        min-width: 120px;
                    }
                    
                    .print-products-table {
                        width: 100%;
                        border-collapse: collapse;
                        margin-bottom: 10px;
                        font-size: 11px;
                        table-layout: fixed;
                    }
                    
                    .print-products-table th, .print-products-table td {
                        border: 1px solid #ddd;
                        padding: 5px;
                        text-align: left;
                        vertical-align: top;
                        word-wrap: break-word;
                    }
                    
                    .print-products-table th {
                        background-color: #f5f5f5;
                        font-weight: bold;
                    }
                    
                    .print-products-table th:nth-child(1) {
                        width: 30px;
                        text-align: center;
                    }
                    
                    .print-products-table th:nth-child(2),
                    .print-products-table td:nth-child(2) {
                        width: auto;
                    }
                    
                    .print-products-table th:nth-child(3),
                    .print-products-table td:nth-child(3) {
                        text-align: center;
                        width: 40px;
                    }
                    
                    .print-products-table th:nth-child(4),
                    .print-products-table td:nth-child(4),
                    .print-products-table th:nth-child(5),
                    .print-products-table td:nth-child(5) {
                        text-align: right;
                        width: 80px;
                    }
                    
                    .print-summary-table {
                        width: 300px;
                        border-collapse: collapse;
                        margin-left: auto;
                        font-size: 12px;
                    }
                    
                    .print-summary-table td {
                        padding: 4px;
                        vertical-align: top;
                    }
                    
                    .print-summary-table td:first-child {
                        font-weight: bold;
                        text-align: right;
                    }
                    
                    .print-summary-table td:last-child {
                        text-align: right;
                        width: 100px;
                    }
                    
                    .print-total td {
                        font-weight: bold;
                        font-size: 14px;
                        border-top: 1px solid #333;
                        padding-top: 5px;
                    }
                    
                    .print-footer {
                        margin-top: 20px;
                    }
                    
                    .print-note {
                        text-align: center;
                        font-style: italic;
                        padding-top: 10px;
                        border-top: 1px dashed #ddd;
                    }
                    
                    .print-note p {
                        margin: 3px 0;
                        font-size: 12px;
                    }
                    
                    #download-info {
                        position: fixed;
                        top: 10px;
                        right: 10px;
                        background-color: #f8f9fa;
                        border: 1px solid #dee2e6;
                        border-radius: 4px;
                        padding: 10px;
                        font-size: 14px;
                        box-shadow: 0 2px 5px rgba(0,0,0,0.1);
                        display: none;
                    }
                    
                    @media print {
                        #download-info {
                            display: none !important;
                        }
                    }
                </style>
                <script>
                    // Add script to handle PDF download
                    window.onload = function() {
                        // Show download info
                        const downloadInfo = document.getElementById('download-info');
                        if (downloadInfo) {
                            downloadInfo.style.display = 'block';
                        }
                        
                        // Auto-trigger print dialog that will save as PDF
                        setTimeout(function() {
                            // Set filename
                            const filename = "${filename}";
                            
                            // Modern browsers support this
                            if (window.navigator && window.navigator.msSaveOrOpenBlob) {
                                // For IE
                                window.print();
                            } else {
                                // For Chrome, Firefox, etc.
                                const pdfOptions = {
                                    filename: filename
                                };
                                
                                // Print with filename
                                window.print();
                            }
                        }, 1000);
                    };
                </script>
            </head>
            <body>
                <div id="download-info">
                    <p><strong>Lưu ý:</strong> Để lưu thành file PDF, vui lòng chọn "Lưu dưới dạng PDF" hoặc "Save as PDF" trong hộp thoại in.</p>
                    <p>Tên file: <strong>${filename}</strong></p>
                </div>
                ${printTemplate.innerHTML}
            </body>
            </html>
        `);
        
        printWindow.document.close();
    }

    // Add a separate function to download as PDF directly (for browsers that support it)
    function downloadOrderAsPDF() {
        // This is a placeholder for future implementation with a PDF generation library
        // For now, we're using the browser's built-in PDF printing functionality
        printOrder();
    }

    // Store current order data for printing
    let currentOrderData = null;

    // Event Listeners
    function setupEventListeners() {
        let searchTimeout;
        DOM.orderSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => loadOrders(1, this.value.toLowerCase(), state.sortFilter, state.statusFilter, state.dateFilter.startDate, state.dateFilter.endDate), 300);
        });

        DOM.sortFilter?.addEventListener("change", function () {
            loadOrders(1, state.searchTerm, this.value, state.statusFilter, state.dateFilter.startDate, state.dateFilter.endDate);
        });

        DOM.statusFilter?.addEventListener("change", function () {
            loadOrders(1, state.searchTerm, state.sortFilter, this.value, state.dateFilter.startDate, state.dateFilter.endDate);
        });
        
        // Date range filter events
        DOM.quickDateFilter?.addEventListener("change", function() {
            if (this.value) {
                const dateRange = getDateRangeFromPreset(this.value);
                DOM.startDateFilter.value = dateRange.startDate;
                DOM.endDateFilter.value = dateRange.endDate;
            }
        });
        
        DOM.applyFiltersBtn?.addEventListener("click", function() {
            loadOrders(
                1, 
                state.searchTerm, 
                state.sortFilter, 
                state.statusFilter, 
                DOM.startDateFilter.value, 
                DOM.endDateFilter.value
            );
        });

        DOM.updateOrderBtn?.addEventListener("click", updateOrder);
        
        // Print order button
        DOM.printOrderBtn?.addEventListener("click", printOrder);
        
        // Download PDF button
        DOM.downloadPdfBtn?.addEventListener("click", downloadOrderAsPDF);
        
        // Add event listener for order status dropdown to update timeline
        DOM.editOrderStatus?.addEventListener("change", function() {
            updateOrderTimeline(this.value);
        });

        DOM.paginationPrev?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage > 1) {
                loadOrders(
                    state.pagination.currentPage - 1, 
                    state.searchTerm, 
                    state.sortFilter, 
                    state.statusFilter, 
                    state.dateFilter.startDate, 
                    state.dateFilter.endDate
                );
            }
        });

        DOM.paginationNext?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage < state.pagination.totalPages) {
                loadOrders(
                    state.pagination.currentPage + 1, 
                    state.searchTerm, 
                    state.sortFilter, 
                    state.statusFilter, 
                    state.dateFilter.startDate, 
                    state.dateFilter.endDate
                );
            }
        });

        document.addEventListener("click", (e) => {
            const editBtn = e.target.closest(".edit-order-btn");
            const deleteBtn = e.target.closest(".delete-order-btn");
            const downloadInvoiceBtn = e.target.closest(".download-invoice-btn");

            if (editBtn) openEditOrderModal(parseInt(editBtn.getAttribute("data-order-id")));
            if (deleteBtn) deleteOrder(parseInt(deleteBtn.getAttribute("data-order-id")));
            if (downloadInvoiceBtn) downloadOrderInvoice(parseInt(downloadInvoiceBtn.getAttribute("data-order-id")));
        });

        document.getElementById("editOrderModal")?.addEventListener("hidden.bs.modal", resetEditForm);
    }

    // Download order invoice directly from card
    async function downloadOrderInvoice(orderId) {
        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.order(orderId));

            if (result.success) {
                // Store the current order data for printing
                currentOrderData = result.data;
                // Generate PDF
                downloadOrderAsPDF();
            } else {
                utils.showToast(result.message || "Không thể tải chi tiết đơn hàng", "error");
            }
        } catch (error) {
            utils.showToast("Không thể tải chi tiết đơn hàng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Initialize
    async function initialize() {
        try {
            await loadOrders();
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang", "error");
        }
    }

    // Start the application
    initialize();
});