document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        orders: [],
        pagination: {
            currentPage: 1,
            pageSize: 8,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        statusFilter: "all",
        startDate: "",
        endDate: "",
        searchInDetails: false
    };

    // API Endpoints
    const API = {
        orders: (page, pageSize, searchTerm, statusFilter, startDate, endDate, searchInDetails) =>
            `/OrderManage/GetOrders?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (statusFilter !== 'all' ? `&status=${statusFilter}` : '') +
            (startDate ? `&startDate=${encodeURIComponent(startDate)}` : '') +
            (endDate ? `&endDate=${encodeURIComponent(endDate)}` : '') +
            (searchInDetails ? `&searchInDetails=true` : ''),
        updateOrder: '/OrderManage/UpdateOrder',
        deleteOrder: (id) => `/OrderManage/DeleteOrder?id=${id}`,
        menuItems: '/Pos/GetMenuItems?pageSize=1000'
    };

    // DOM Elements
    const DOM = {
        ordersGrid: document.getElementById("ordersGrid"),
        orderSearchInput: document.getElementById("orderSearchInput"),
        startDateInput: document.getElementById("startDateInput"),
        endDateInput: document.getElementById("endDateInput"),
        clearFiltersBtn: document.getElementById("clearFiltersBtn"),
        filterResult: document.getElementById("filterResult"),
        updateOrderBtn: document.getElementById("updateOrderBtn"),
        paginationContainer: document.getElementById("orderPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        statusFilter: document.getElementById("statusFilter"),
        editOrderDetails: document.getElementById("editOrderDetails")
    };

    // Skeleton Loaders
    function showOrderSkeletons() {
        const skeletonCount = 8;
        DOM.ordersGrid.innerHTML = Array(skeletonCount).fill().map(() => `
            <tr>
                <td><div class="skeleton" style="width: 80%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 80%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 60%; height: 20px;"></div></td>
                <td><div class="skeleton" style="width: 80%; height: 20px;"></div></td>
            </tr>
        `).join('');
    }

    // Data Loading
    async function loadOrders(page = 1) {
        try {
            showOrderSkeletons();

            state.pagination.currentPage = page;

            const url = API.orders(
                page,
                state.pagination.pageSize,
                state.searchTerm,
                state.statusFilter,
                state.startDate,
                state.endDate,
                state.searchInDetails
            );
            const result = await utils.fetchData(url);

            if (result.success) {
                state.orders = result.data;
                state.pagination = result.pagination;
                renderOrders();
                renderPagination();
                updateFilterResult();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách đơn hàng", "error");
            }
        } catch (error) {
            console.error('Load orders error:', error);
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        }
    }

    // Rendering Functions
    function renderOrders() {
        DOM.ordersGrid.innerHTML = state.orders.length === 0
            ? `
                <tr>
                    <td colspan="9" class="text-center">
                        <div class="py-4">
                            <i class="fas fa-box-open fa-3x text-muted mb-3"></i>
                            <p class="text-muted mb-0">Không tìm thấy đơn hàng</p>
                        </div>
                    </td>
                </tr>
            `
            : state.orders.map(order => `
                <tr>
                    <td>#${order.orderID}</td>
                    <td>${order.tableName}</td>
                    <td>${order.customerName}</td>
                    <td>${order.employeeName || 'Chưa phân công'}</td>
                    <td>${new Date(order.orderTime).toLocaleString()}</td>
                    <td>${getPaymentMethodText(order.paymentMethod)}</td>
                    <td>${utils.formatMoney(order.total)}</td>
                    <td>
                        <span class="order-status-badge ${getStatusBadgeClass(order.status)}">
                            ${getStatusText(order.status)}
                        </span>
                    </td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary edit-order-btn" data-order-id="${order.orderID}" data-bs-toggle="modal" data-bs-target="#editOrderModal" title="Sửa đơn hàng">
                            <i class="fas fa-edit"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-danger delete-order-btn" data-order-id="${order.orderID}" title="Xóa đơn hàng">
                            <i class="fas fa-trash"></i>
                        </button>
                        <button class="btn btn-sm btn-outline-secondary print-order-btn" data-order-id="${order.orderID}" title="Xóa đơn hàng">
                            <i class="fas fa-print me-1"></i>
                        </button>
                    </td>
                </tr>
            `).join('');
            
            document.querySelectorAll(".print-order-btn").forEach(btn => {
                btn.addEventListener("click", () => {
                    const orderId = btn.getAttribute("data-order-id");
                    window.open(`he-thong/in-hoa-don/${orderId}`, '_blank', 'width=800,height=600');
                });
            });
    }

    function getPaymentMethodText(paymentMethod) {
        switch (parseInt(paymentMethod)) {
            case 0: return 'Tiền mặt';
            case 1: return 'Thẻ';
            case 2: return 'Thanh toán online';
            default: return 'Không xác định';
        }
    }

    function getStatusText(status) {
        switch (status) {
            case 0: return 'Đã hủy';
            case 1: return 'Chờ xác nhận';
            case 2: return 'Đang chuẩn bị';
            case 3: return 'Hoàn thành';
            default: return 'Không xác định';
        }
    }

    function getStatusBadgeClass(status) {
        switch (status) {
            case 0: return 'status-badge-inactive';
            case 1: return 'status-badge-pending';
            case 2: return 'status-badge-preparing';
            case 3: return 'status-badge-active';
            default: return '';
        }
    }

    function renderPagination() {
        utils.renderPagination(
            state.pagination,
            DOM.paginationContainer,
            DOM.paginationItems,
            DOM.paginationPrev,
            DOM.paginationNext,
            (pageNum) => loadOrders(pageNum)
        );
    }

    function updateFilterResult() {
        DOM.filterResult.textContent = `Tìm thấy ${state.pagination.totalItems} đơn hàng`;
    }

    // Form Reset Functions
    function resetEditForm() {
        const form = document.getElementById("editOrderForm");
        form.reset();
        DOM.editOrderDetails.innerHTML = "";
        console.log("Edit form reset");
    }

    function resetFilters() {
        state.searchTerm = "";
        state.statusFilter = "all";
        state.startDate = "";
        state.endDate = "";
        state.searchInDetails = false;

        DOM.orderSearchInput.value = "";
        DOM.startDateInput.value = "";
        DOM.endDateInput.value = "";
        DOM.statusFilter.querySelectorAll("button").forEach(b => b.classList.remove("active"));
        DOM.statusFilter.querySelector("button[data-status='all']").classList.add("active");

        loadOrders(1);
    }

    // Event Listeners
    function setupEventListeners() {
        let searchTimeout;

        DOM.orderSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            state.searchTerm = this.value.toLowerCase();
            state.searchInDetails = state.searchTerm.length > 0;
            searchTimeout = setTimeout(() => loadOrders(1), 300);
        });

        DOM.startDateInput?.addEventListener("change", function () {
            state.startDate = this.value;
            loadOrders(1);
        });

        DOM.endDateInput?.addEventListener("change", function () {
            state.endDate = this.value;
            loadOrders(1);
        });

        DOM.clearFiltersBtn?.addEventListener("click", resetFilters);

        DOM.updateOrderBtn?.addEventListener("click", updateOrder);

        DOM.paginationPrev?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage > 1) {
                loadOrders(state.pagination.currentPage - 1);
            }
        });

        DOM.paginationNext?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage < state.pagination.totalPages) {
                loadOrders(state.pagination.currentPage + 1);
            }
        });

        DOM.statusFilter?.addEventListener("click", (e) => {
            const btn = e.target.closest("button[data-status]");
            if (btn) {
                DOM.statusFilter.querySelectorAll("button").forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                state.statusFilter = btn.getAttribute("data-status");
                loadOrders(1);
            }
        });

        document.addEventListener("click", (e) => {
            const editBtn = e.target.closest(".edit-order-btn");
            const deleteBtn = e.target.closest(".delete-order-btn");

            if (editBtn) openEditOrderModal(parseInt(editBtn.getAttribute("data-order-id")));
            if (deleteBtn) deleteOrder(parseInt(deleteBtn.getAttribute("data-order-id")));
        });

        document.getElementById("addOrderDetailBtn")?.addEventListener("click", () => {
            const container = DOM.editOrderDetails;
            const detailId = Date.now();
            const detailItem = document.createElement("div");
            detailItem.className = "order-detail-item mb-2";
            detailItem.setAttribute("data-detail-id", detailId);
            
            // Thêm tiêu đề cột nếu là món đầu tiên
            if (container.children.length === 0) {
                const headerRow = document.createElement("div");
                headerRow.className = "row g-2 align-items-center mb-2 fw-bold";
                headerRow.innerHTML = `
                    <div class="col-5">Tên món</div>
                    <div class="col-3">Số lượng</div>
                    <div class="col-3">Ghi chú</div>
                    <div class="col-1"></div>
                `;
                container.appendChild(headerRow);
            }
            
            detailItem.innerHTML = `
                <small class="text-muted mt-1 d-block">Món #${container.children.length}</small>
                <div class="row g-2 align-items-center">
                    <div class="col-5">
                        <select class="form-select form-select-sm menu-item-select" required>
                            <option value="" selected disabled>Chọn món</option>
                        </select>
                    </div>
                    <div class="col-3">
                        <input type="number" class="form-control form-control-sm quantity" placeholder="Số lượng" min="1" value="1" required>
                    </div>
                    <div class="col-3">
                        <input type="text" class="form-control form-control-sm note" placeholder="Ghi chú">
                    </div>
                    <div class="col-1 d-flex align-items-center">
                        <i class="fas fa-trash text-danger detail-remove" style="cursor: pointer;" data-detail-id="${detailId}"></i>
                    </div>
                </div>
            `;
            container.appendChild(detailItem);

            loadMenuItemsForSelect(detailItem.querySelector(".menu-item-select"), null);

            detailItem.querySelector(".detail-remove").addEventListener("click", () => {
                detailItem.remove();
                console.log(`Order detail removed`, { detailId });
                
                // Cập nhật lại số thứ tự của các món
                updateOrderDetailNumbers();
            });

            console.log(`Order detail added`, { detailId });
        });
        
        // Hàm cập nhật số thứ tự của các món
        function updateOrderDetailNumbers() {
            const detailItems = document.querySelectorAll("#editOrderDetails .order-detail-item");
            detailItems.forEach((item, index) => {
                const numberLabel = item.querySelector("small.text-muted");
                if (numberLabel) {
                    numberLabel.textContent = `Món #${index + 1}`;
                }
            });
        }

        document.getElementById("editOrderModal")?.addEventListener("hidden.bs.modal", resetEditForm);
    }

    async function loadMenuItemsForSelect(selectElement, selectedMenuItemId = null) {
        try {
            const result = await utils.fetchData(API.menuItems);
            if (result.success && result.data?.length > 0) {
                const currentValue = selectedMenuItemId || selectElement.value;
                let options = result.data.map(item => `
                    <option value="${item.menuItemID}" data-price="${item.price}" ${item.menuItemID == currentValue ? 'selected' : ''}>
                        ${item.name} (${utils.formatMoney(item.price)})
                    </option>
                `).join('');
                selectElement.innerHTML = `<option value="" ${!currentValue ? 'selected' : ''} disabled>Chọn món</option>${options}`;
            } else {
                selectElement.innerHTML = `<option value="" selected disabled>Không có món nào</option>`;
            }
        } catch (error) {
            console.error('Load menu items error:', error);
            selectElement.innerHTML = `<option value="" selected disabled>Lỗi tải món</option>`;
        }
    }

    async function updateOrder() {
        console.log("Starting updateOrder function");
        const form = document.getElementById("editOrderForm");
        if (!form.checkValidity()) {
            console.log("Edit form validation failed");
            form.reportValidity();
            return;
        }
        console.log("Edit form validation passed");

        const order = {
            OrderID: parseInt(document.getElementById("editOrderId").value),
            Status: parseInt(document.getElementById("editOrderStatus").value),
            PaymentMethod: document.getElementById("editPaymentMethod").value,
            OrderDetails: []
        };

        let detailError = false;
        const detailItems = document.querySelectorAll("#editOrderDetails .order-detail-item");
        console.log(`Found ${detailItems.length} order detail items to process for update`);

        detailItems.forEach((item, index) => {
            const menuItemSelect = item.querySelector(".menu-item-select");
            const menuItemID = parseInt(menuItemSelect.value);
            const quantity = parseInt(item.querySelector(".quantity").value);
            const note = item.querySelector(".note").value.trim();
            console.log(`Processing update detail #${index + 1}:`, { menuItemID, quantity, note });

            if (menuItemID && quantity > 0) {
                order.OrderDetails.push({ MenuItemID: menuItemID, Quantity: quantity, Note: note });
                console.log(`Update detail #${index + 1} added successfully`);
            } else {
                detailError = true;
                console.log(`Update detail #${index + 1} validation failed`);
                if (!menuItemID) menuItemSelect.reportValidity();
                if (!quantity || quantity <= 0) item.querySelector(".quantity").reportValidity();
            }
        });

        if (detailError) {
            console.log("Order detail validation failed - stopping form submission");
            return;
        }
        if (order.OrderDetails.length === 0) {
            console.log("No valid order details provided");
            utils.showToast("Vui lòng thêm ít nhất một món vào đơn hàng", "error");
            return;
        }
        console.log(`All ${order.OrderDetails.length} update details validated successfully`);

        console.log("Updating order", { order });

        try {
            console.log("Showing loading overlay and starting update API request");
            utils.showLoadingOverlay(true);
            console.log("Sending POST request to:", API.updateOrder);

            const response = await fetch(API.updateOrder, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(order)
            });
            console.log("Update API response received:", {
                status: response.status,
                statusText: response.statusText
            });

            const result = await response.json();
            console.log("Update order result:", result);

            if (result.success) {
                console.log("Order updated successfully");
                utils.showToast("Cập nhật đơn hàng thành công", "success");
                console.log("Reloading orders after update");
                await loadOrders(state.pagination.currentPage);
                console.log("Resetting edit form");
                resetEditForm();
                console.log("Closing edit modal");
                bootstrap.Modal.getInstance(document.getElementById('editOrderModal')).hide();
            } else {
                console.error("Update API returned error:", result.message);
                utils.showToast(result.message || "Lỗi khi cập nhật đơn hàng", "error");
            }
        } catch (error) {
            console.error("Update order error:", error);
            utils.showToast("Đã xảy ra lỗi khi cập nhật đơn hàng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteOrder(orderId) {
        const order = state.orders.find(o => o.orderID === orderId);
        if (!order) return;

        console.log("Order to delete:", order.orderID);
        document.getElementById("deleteOrderName").innerHTML = `#${order.orderID}`;
        document.getElementById("deleteOrderId").value = order.orderID;
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteOrderModal"));
        deleteModal.show();

        const confirmBtn = document.getElementById("confirmDeleteOrderBtn");
        const deleteHandler = async () => {
            try {
                utils.showLoadingOverlay(true);
                console.log("Deleting order", { orderId });
                const result = await utils.fetchData(API.deleteOrder(orderId), 'POST');
                console.log("Delete order result", result);

                if (result.success) {
                    utils.showToast("Xóa đơn hàng thành công", "success");
                    await loadOrders(state.pagination.currentPage);
                    deleteModal.hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi xóa đơn hàng", "error");
                }
            } catch (error) {
                console.error("Delete order error", error);
                utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
            } finally {
                utils.showLoadingOverlay(false);
                confirmBtn.removeEventListener("click", deleteHandler);
            }
        };

        confirmBtn.addEventListener("click", deleteHandler);
    }

    async function openEditOrderModal(orderId) {
        try {
            const order = state.orders.find(o => o.orderID === orderId);
            if (!order) {
                utils.showToast("Không tìm thấy đơn hàng", "error");
                return;
            }

            document.getElementById("editOrderId").value = order.orderID;
            document.getElementById("editOrderStatus").value = order.status;
            document.getElementById("editPaymentMethod").value = order.paymentMethod;
            
            // Xóa chi tiết đơn hàng cũ
            DOM.editOrderDetails.innerHTML = "";
            
            // Thêm tiêu đề cột nếu có chi tiết đơn hàng
            if (order.orderDetails && order.orderDetails.length > 0) {
                const headerRow = document.createElement("div");
                headerRow.className = "row g-2 align-items-center mb-2 fw-bold";
                headerRow.innerHTML = `
                    <div class="col-5">Tên món</div>
                    <div class="col-3">Số lượng</div>
                    <div class="col-3">Ghi chú</div>
                    <div class="col-1"></div>
                `;
                DOM.editOrderDetails.appendChild(headerRow);
                
                // Thêm từng chi tiết đơn hàng
                order.orderDetails.forEach((detail, index) => {
                    const detailId = Date.now() + index;
                    const detailItem = document.createElement("div");
                    detailItem.className = "order-detail-item mb-2";
                    detailItem.setAttribute("data-detail-id", detailId);
                    
                    detailItem.innerHTML = `
                        <small class="text-muted mt-1 d-block">Món #${index + 1}</small>
                        <div class="row g-2 align-items-center">
                            <div class="col-5">
                                <select class="form-select form-select-sm menu-item-select" required>
                                    <option value="" selected disabled>Chọn món</option>
                                </select>
                            </div>
                            <div class="col-3">
                                <input type="number" class="form-control form-control-sm quantity" placeholder="Số lượng" min="1" value="${detail.quantity}" required>
                            </div>
                            <div class="col-3">
                                <input type="text" class="form-control form-control-sm note" placeholder="Ghi chú" value="${detail.note || ''}">
                            </div>
                            <div class="col-1 d-flex align-items-center">
                                <i class="fas fa-trash text-danger detail-remove" style="cursor: pointer;" data-detail-id="${detailId}"></i>
                            </div>
                        </div>
                    `;
                    DOM.editOrderDetails.appendChild(detailItem);
                    
                    const selectElement = detailItem.querySelector(".menu-item-select");
                    loadMenuItemsForSelect(selectElement, detail.menuItemID);
                    
                    detailItem.querySelector(".detail-remove").addEventListener("click", () => {
                        detailItem.remove();
                        updateOrderDetailNumbers();
                    });
                });
            }
            
            console.log("Edit order modal opened for order:", order);
        } catch (error) {
            console.error("Open edit modal error:", error);
            utils.showToast("Lỗi khi mở form chỉnh sửa", "error");
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

    initialize();
});