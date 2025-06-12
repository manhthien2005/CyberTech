document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        customers: [],
        pagination: {
            currentPage: 1,
            pageSize: 10,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        sortFilter: "date_desc",
        statusFilter: "",
        selectedCustomer: null,
        ranks: []
    };

    // Validation Functions
    function isValidPhoneNumber(phone) {
        if (!phone) return true; // Phone is optional
        // Kiểm tra số điện thoại Việt Nam
        const phoneRegex = /^(0|\+84)([0-9]{9,10})$/;
        return phoneRegex.test(phone.trim());
    }

    function validatePhoneInput(input) {
        const phone = input.value.trim();
        const errorDiv = input.nextElementSibling || document.createElement('div');
        
        if (!errorDiv.classList.contains('invalid-feedback')) {
            errorDiv.className = 'invalid-feedback';
            input.parentNode.appendChild(errorDiv);
        }

        if (phone && !isValidPhoneNumber(phone)) {
            input.classList.add('is-invalid');
            errorDiv.textContent = 'Số điện thoại không hợp lệ (VD: 0912345678 hoặc +84912345678)';
            return false;
        } else {
            input.classList.remove('is-invalid');
            errorDiv.textContent = '';
            return true;
        }
    }

    // API Endpoints
    const API = {
        customers: (page, pageSize, searchTerm, sortFilter, statusFilter) =>
            `/CustomerManage/GetCustomers?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (sortFilter ? `&sortBy=${sortFilter}` : '') +
            (statusFilter ? `&status=${statusFilter}` : ''),
        customer: (id) => `/CustomerManage/GetCustomer/${id}`,
        addresses: (userId) => `/CustomerManage/GetCustomerAddresses/${userId}`,
        vouchers: (userId) => `/CustomerManage/GetCustomerVouchers/${userId}`,
        orders: (userId) => `/CustomerManage/GetCustomerOrders/${userId}`,
        orderDetails: (orderId) => `/CustomerManage/GetOrderDetails/${orderId}`,
        createCustomer: '/CustomerManage/CreateCustomer',
        updateCustomer: '/CustomerManage/UpdateCustomer',
        assignVoucher: '/CustomerManage/AssignVoucher',
        removeVoucher: '/CustomerManage/RemoveVoucher',
        availableVouchers: (userId) => `/CustomerManage/GetAvailableVouchers?userId=${userId}`,
        ranks: '/CustomerManage/GetRanks'
    };

    // DOM Elements
    const DOM = {
        customerTableBody: document.getElementById("customerTableBody"),
        customerSearchInput: document.getElementById("customerSearchInput"),
        saveCustomerBtn: document.getElementById("saveCustomerBtn"),
        updateCustomerBtn: document.getElementById("updateCustomerBtn"),
        paginationContainer: document.getElementById("customerPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        sortFilter: document.getElementById("sortFilter"),
        statusFilter: document.getElementById("statusFilter"),
        addVoucherToCustomerBtn: document.getElementById("addVoucherToCustomerBtn"),
        confirmDeleteCustomerBtn: document.getElementById("confirmDeleteCustomerBtn"),
        addressList: document.getElementById("addressList"),
        vouchersList: document.getElementById("vouchersList"),
        ordersList: document.getElementById("ordersList"),
        availableVouchersTableBody: document.getElementById("availableVouchersTableBody"),
        voucherCustomerName: document.getElementById("voucherCustomerName"),
        voucherCustomerId: document.getElementById("voucherCustomerId"),
        customerRankInfo: document.getElementById("customerRankInfo"),
        customerJoinDate: document.getElementById("customerJoinDate"),
        customerTotalSpent: document.getElementById("customerTotalSpent"),
        customerOrderCount: document.getElementById("customerOrderCount"),
        totalVouchersCount: document.getElementById("totalVouchersCount"),
        activeVouchersCount: document.getElementById("activeVouchersCount"),
        usedVouchersCount: document.getElementById("usedVouchersCount"),
        totalOrdersCount: document.getElementById("totalOrdersCount"),
        completedOrdersCount: document.getElementById("completedOrdersCount"),
        processingOrdersCount: document.getElementById("processingOrdersCount"),
        cancelledOrdersCount: document.getElementById("cancelledOrdersCount")
    };

    // Skeleton Loaders
    function showCustomerSkeletons() {
        DOM.customerTableBody.innerHTML = Array(5).fill().map(() => `
            <tr>
                <td>
                    <div class="d-flex align-items-center">
                        <div class="skeleton" style="width: 40px; height: 40px; border-radius: 50%; margin-right: 10px;"></div>
                        <div>
                            <div class="skeleton" style="width: 120px; height: 18px; margin-bottom: 5px;"></div>
                            <div class="skeleton" style="width: 80px; height: 14px;"></div>
                        </div>
                    </div>
                </td>
                <td><div class="skeleton" style="width: 150px; height: 18px;"></div></td>
                <td><div class="skeleton" style="width: 100px; height: 18px;"></div></td>
                <td><div class="skeleton" style="width: 80px; height: 18px;"></div></td>
                <td><div class="skeleton" style="width: 40px; height: 18px;"></div></td>
                <td><div class="skeleton" style="width: 80px; height: 18px;"></div></td>
                <td><div class="skeleton" style="width: 60px; height: 24px; border-radius: 4px;"></div></td>
                <td>
                    <div class="d-flex">
                        <div class="skeleton" style="width: 32px; height: 32px; margin-right: 5px; border-radius: 4px;"></div>
                        <div class="skeleton" style="width: 32px; height: 32px; margin-right: 5px; border-radius: 4px;"></div>
                        <div class="skeleton" style="width: 32px; height: 32px; border-radius: 4px;"></div>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    // Helper Functions
    function getRankColor(rankId) {
        // Return a color based on rank ID
        switch (rankId) {
            case 1: return "#6c757d"; // Standard/Default
            case 2: return "#28a745"; // Silver
            case 3: return "#0d6efd"; // Gold
            case 4: return "#dc3545"; // Platinum
            case 5: return "#9932CC"; // Diamond
            default: return "#6c757d";
        }
    }

    function getStatusClass(status) {
        switch (status) {
            case "Active": return "status-active";
            case "Inactive": return "status-inactive";
            case "Suspended": return "status-suspended";
            default: return "status-active";
        }
    }

    function getStatusText(status) {
        switch (status) {
            case "Active": return "Đang hoạt động";
            case "Inactive": return "Không hoạt động";
            case "Suspended": return "Tạm khóa";
            default: return status;
        }
    }

    function getOrderStatusText(status) {
        switch (status) {
            case "Pending": return "Chờ xác nhận";
            case "Processing": return "Đang xử lý";
            case "Shipped": return "Đang giao hàng";
            case "Delivered": return "Đã giao hàng";
            case "Cancelled": return "Đã hủy";
            default: return status;
        }
    }

    function formatDate(dateString) {
        if (!dateString) return "";
        const date = new Date(dateString);
        return date.toLocaleDateString('vi-VN');
    }

    function resetAddForm() {
        const form = document.getElementById("addCustomerForm");
        form.reset();
    }

    function resetEditForm() {
        const form = document.getElementById("editCustomerForm");
        form.reset();
        state.selectedCustomer = null;
    }

    // Fill customer edit form
    function fillCustomerEditForm(customer) {
        document.getElementById("editUserId").value = customer.userId;
        document.getElementById("editName").value = customer.name;
        document.getElementById("editEmail").value = customer.email;
        document.getElementById("editUsername").value = customer.username;
        document.getElementById("editPhone").value = customer.phone || '';
        document.getElementById("editStatus").value = customer.status;
        document.getElementById("editDateOfBirth").value = customer.dateOfBirth ? new Date(customer.dateOfBirth).toISOString().split('T')[0] : '';
        
        // Gender radio buttons
        document.querySelectorAll('input[name="gender"]').forEach(radio => {
            if (customer.gender === null) {
                if (radio.value === "") radio.checked = true;
            } else {
                if (parseInt(radio.value) === customer.gender) radio.checked = true;
            }
        });
        
        // Additional info
        DOM.customerRankInfo.innerHTML = `<div class="fw-bold">${customer.rankName}</div>`;
        DOM.customerJoinDate.innerHTML = `<div class="fw-bold">${formatDate(customer.createdAt)}</div>`;
        DOM.customerTotalSpent.innerHTML = `<div class="fw-bold">${utils.formatMoney(customer.totalSpent)}</div>`;
        DOM.customerOrderCount.innerHTML = `<div class="fw-bold">${customer.orderCount} đơn hàng</div>`;
        
        // Load related data
        loadCustomerAddresses(customer.userId);
        loadCustomerVouchers(customer.userId);
        loadCustomerOrders(customer.userId);
    }

    // CRUD Operations
    async function createCustomer() {
        const form = document.getElementById("addCustomerForm");
        const phoneInput = document.getElementById("phone");
        
        if (!form.checkValidity() || !validatePhoneInput(phoneInput)) {
            form.reportValidity();
            return;
        }

        const customer = {
            Name: document.getElementById("name").value,
            Email: document.getElementById("email").value,
            Username: document.getElementById("username").value,
            Password: document.getElementById("password").value,
            Phone: phoneInput.value.trim()
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.createCustomer, 'POST', customer);

            if (result.success) {
                utils.showToast("Thêm khách hàng thành công", "success");
                await loadCustomers();
                resetAddForm();
                bootstrap.Modal.getInstance(document.getElementById('addCustomerModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi thêm khách hàng", "error");
            }
        } catch (error) {
            utils.showToast("Lỗi khi thêm khách hàng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function updateCustomer() {
        const form = document.getElementById("editCustomerForm");
        const phoneInput = document.getElementById("editPhone");
        
        if (!form.checkValidity() || !validatePhoneInput(phoneInput)) {
            form.reportValidity();
            return;
        }

        const gender = document.querySelector('input[name="gender"]:checked');
        const genderValue = gender ? (gender.value === "" ? null : parseInt(gender.value)) : null;

        const customer = {
            UserId: parseInt(document.getElementById("editUserId").value),
            Name: document.getElementById("editName").value,
            Phone: phoneInput.value.trim(),
            Gender: genderValue,
            DateOfBirth: document.getElementById("editDateOfBirth").value || null,
            Status: document.getElementById("editStatus").value
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateCustomer, 'POST', customer);

            if (result.success) {
                utils.showToast("Cập nhật thông tin khách hàng thành công", "success");
                await loadCustomers();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật thông tin khách hàng", "error");
            }
        } catch (error) {
            utils.showToast("Lỗi khi cập nhật thông tin khách hàng", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteCustomer(userId, userName) {
        document.getElementById("deleteCustomerName").textContent = userName;
        document.getElementById("deleteCustomerId").value = userId;
        
        // Get current status
        const customer = state.customers.find(c => c.userId === userId);
        const modalBody = document.querySelector("#deleteCustomerModal .modal-body");
        const confirmButton = document.getElementById("confirmDeleteCustomerBtn");
        
        if (customer) {
            if (customer.status === "Active") {
                modalBody.innerHTML = `
                    <p>Bạn có chắc chắn muốn vô hiệu hóa tài khoản của khách hàng <span id="deleteCustomerName" class="fw-bold">${userName}</span>?</p>
                    <p class="text-danger">Tài khoản sẽ bị chuyển sang trạng thái không hoạt động và khách hàng sẽ không thể đăng nhập.</p>
                `;
                confirmButton.textContent = "Vô hiệu hóa";
                confirmButton.className = "btn btn-danger";
            } else if (customer.status === "Inactive") {
                modalBody.innerHTML = `
                    <p>Bạn có chắc chắn muốn kích hoạt lại tài khoản của khách hàng <span id="deleteCustomerName" class="fw-bold">${userName}</span>?</p>
                    <p class="text-success">Tài khoản sẽ được kích hoạt và khách hàng có thể đăng nhập bình thường.</p>
                `;
                confirmButton.textContent = "Kích hoạt";
                confirmButton.className = "btn btn-success";
            } else if (customer.status === "Suspended") {
                modalBody.innerHTML = `
                    <p>Bạn có chắc chắn muốn mở khóa tài khoản của khách hàng <span id="deleteCustomerName" class="fw-bold">${userName}</span>?</p>
                    <p class="text-warning">Tài khoản sẽ được mở khóa và chuyển về trạng thái hoạt động bình thường.</p>
                `;
                confirmButton.textContent = "Mở khóa";
                confirmButton.className = "btn btn-warning";
            }
        }
        
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteCustomerModal"));
        deleteModal.show();
    }

    async function assignVoucherToCustomer(userId, voucherId) {
        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.assignVoucher, 'POST', { UserId: userId, VoucherId: voucherId });

            if (result.success) {
                utils.showToast("Gán voucher thành công", "success");
                await loadCustomerVouchers(userId);
                await loadAvailableVouchers(userId);
            } else {
                utils.showToast(result.message || "Lỗi khi gán voucher", "error");
            }
        } catch (error) {
            utils.showToast("Lỗi khi gán voucher", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function removeVoucherFromCustomer(userVoucherId) {
        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.removeVoucher, 'POST', { UserVoucherId: userVoucherId });

            if (result.success) {
                utils.showToast("Xóa voucher thành công", "success");
                const userId = document.getElementById("editUserId").value;
                await loadCustomerVouchers(userId);
                await loadAvailableVouchers(userId);
            } else {
                utils.showToast(result.message || "Lỗi khi xóa voucher", "error");
            }
        } catch (error) {
            utils.showToast("Lỗi khi xóa voucher", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Data Loading
    async function loadCustomers(page = 1, searchTerm = state.searchTerm, sortFilter = state.sortFilter, statusFilter = state.statusFilter) {
        try {
            showCustomerSkeletons();

            state.pagination.currentPage = page;
            state.searchTerm = searchTerm;
            state.sortFilter = sortFilter;
            state.statusFilter = statusFilter;

            const result = await utils.fetchData(API.customers(page, state.pagination.pageSize, searchTerm, sortFilter, statusFilter));

            if (result.success) {
                state.customers = result.data;
                state.pagination = result.pagination;
                renderCustomers();
                renderPagination();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách khách hàng", "error");
            }
        } catch (error) {
            console.error('Load customers error:', error);
            utils.showToast("Không thể tải danh sách khách hàng", "error");
        }
    }

    async function loadRanks() {
        try {
            const result = await utils.fetchData(API.ranks);
            if (result.success) {
                state.ranks = result.data;
            } else {
                utils.showToast(result.message || "Không thể tải danh sách cấp bậc", "error");
            }
        } catch (error) {
            console.error('Load ranks error:', error);
        }
    }

    async function loadCustomerAddresses(userId) {
        try {
            DOM.addressList.innerHTML = '<div class="text-center py-3"><div class="spinner-border text-primary" role="status"></div></div>';
            
            const result = await utils.fetchData(API.addresses(userId));
            if (result.success) {
                renderAddresses(result.data);
            } else {
                DOM.addressList.innerHTML = '<p class="text-center text-muted">Không thể tải địa chỉ</p>';
                console.error(result.message);
            }
        } catch (error) {
            DOM.addressList.innerHTML = '<p class="text-center text-muted">Không thể tải địa chỉ</p>';
            console.error('Load addresses error:', error);
        }
    }

    async function loadCustomerVouchers(userId) {
        try {
            DOM.vouchersList.innerHTML = '<div class="text-center py-3"><div class="spinner-border text-primary" role="status"></div></div>';
            
            const result = await utils.fetchData(API.vouchers(userId));
            if (result.success) {
                renderVouchers(result.data);
            } else {
                DOM.vouchersList.innerHTML = '<p class="text-center text-muted">Không thể tải voucher</p>';
                console.error(result.message);
            }
        } catch (error) {
            DOM.vouchersList.innerHTML = '<p class="text-center text-muted">Không thể tải voucher</p>';
            console.error('Load vouchers error:', error);
        }
    }

    async function loadCustomerOrders(userId) {
        try {
            DOM.ordersList.innerHTML = '<div class="text-center py-3"><div class="spinner-border text-primary" role="status"></div></div>';
            
            const result = await utils.fetchData(API.orders(userId));
            if (result.success) {
                renderOrders(result.data);
            } else {
                DOM.ordersList.innerHTML = '<p class="text-center text-muted">Không thể tải đơn hàng</p>';
                console.error(result.message);
            }
        } catch (error) {
            DOM.ordersList.innerHTML = '<p class="text-center text-muted">Không thể tải đơn hàng</p>';
            console.error('Load orders error:', error);
        }
    }

    async function loadOrderDetails(orderId) {
        try {
            const result = await utils.fetchData(API.orderDetails(orderId));
            if (result.success) {
                renderOrderDetails(result.data);
            } else {
                utils.showToast(result.message || "Không thể tải chi tiết đơn hàng", "error");
            }
        } catch (error) {
            console.error('Load order details error:', error);
            utils.showToast("Không thể tải chi tiết đơn hàng", "error");
        }
    }

    async function loadAvailableVouchers(userId) {
        try {
            DOM.availableVouchersTableBody.innerHTML = '<tr><td colspan="5" class="text-center"><div class="spinner-border text-primary" role="status"></div></td></tr>';
            
            const result = await utils.fetchData(API.availableVouchers(userId));
            if (result.success) {
                renderAvailableVouchers(result.data);
            } else {
                DOM.availableVouchersTableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">Không có voucher khả dụng</td></tr>';
                console.error(result.message);
            }
        } catch (error) {
            DOM.availableVouchersTableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">Không thể tải voucher</td></tr>';
            console.error('Load available vouchers error:', error);
        }
    }

    // Rendering Functions
    function renderCustomers() {
        DOM.customerTableBody.innerHTML = state.customers.length === 0
            ? `<tr><td colspan="8" class="text-center py-4">Không tìm thấy khách hàng</td></tr>`
            : state.customers.map(customer => `
                <tr>
                    <td>
                        <div class="d-flex align-items-center">
                            <div class="customer-avatar">
                                ${customer.profileImageURL 
                                    ? `<img src="${customer.profileImageURL}" alt="${customer.name}" />`
                                    : `<i class="fas fa-user"></i>`
                                }
                            </div>
                            <div>
                                <div class="fw-bold">${customer.name}</div>
                                <div class="text-muted small">${customer.username}</div>
                            </div>
                        </div>
                    </td>
                    <td>${customer.email}</td>
                    <td>${customer.phone || '<span class="text-muted">Không có</span>'}</td>
                    <td>
                        <span class="rank-badge" style="background-color: ${getRankColor(customer.rankId)}">
                            ${customer.rankName}
                        </span>
                    </td>
                    <td>${customer.orderCount}</td>
                    <td>${utils.formatMoney(customer.totalSpent)}</td>
                    <td>
                        <span class="status-badge ${getStatusClass(customer.status)}">
                            ${getStatusText(customer.status)}
                        </span>
                    </td>
                    <td>
                        <div class="btn-group" role="group">
                            <button type="button" class="btn btn-sm btn-outline-primary edit-customer-btn" 
                                data-user-id="${customer.userId}" data-bs-toggle="modal" data-bs-target="#editCustomerModal">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-danger delete-customer-btn" 
                                data-user-id="${customer.userId}" data-user-name="${customer.name}">
                                <i class="fas fa-ban"></i>
                            </button>
                            <button type="button" class="btn btn-sm btn-outline-success assign-voucher-btn" 
                                data-user-id="${customer.userId}" data-user-name="${customer.name}">
                                <i class="fas fa-ticket-alt"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            `).join('');
    }

    function renderPagination() {
        utils.renderPagination(
            state.pagination,
            DOM.paginationContainer,
            DOM.paginationItems,
            DOM.paginationPrev,
            DOM.paginationNext,
            (pageNum) => loadCustomers(pageNum, state.searchTerm, state.sortFilter, state.statusFilter)
        );
    }

    function renderAddresses(addresses) {
        if (!addresses || addresses.length === 0) {
            DOM.addressList.innerHTML = '<p class="text-center text-muted">Khách hàng chưa có địa chỉ nào</p>';
            return;
        }

        DOM.addressList.innerHTML = addresses.map(address => `
            <div class="address-card">
                ${address.isPrimary ? '<span class="primary-badge">Mặc định</span>' : ''}
                <h6>${address.recipientName}</h6>
                <p>${address.phone}</p>
                <p>${address.addressLine}, ${address.ward}, ${address.district}, ${address.city}</p>
            </div>
        `).join('');
    }

    function renderVouchers(vouchers) {
        if (!vouchers || vouchers.length === 0) {
            DOM.vouchersList.innerHTML = '<p class="text-center text-muted">Khách hàng chưa có voucher nào</p>';
            // Update counters to 0
            document.getElementById('totalVouchersCount').innerText = '0';
            document.getElementById('activeVouchersCount').innerText = '0';
            document.getElementById('usedVouchersCount').innerText = '0';
            return;
        }

        // Calculate statistics
        const now = new Date();
        const totalVouchers = vouchers.length;
        const usedVouchers = vouchers.filter(v => v.isUsed).length;
        const expiredVouchers = vouchers.filter(v => !v.isUsed && new Date(v.validTo) < now).length;
        const activeVouchers = totalVouchers - usedVouchers - expiredVouchers;
        
        // Update counters
        document.getElementById('totalVouchersCount').innerText = totalVouchers.toString();
        document.getElementById('activeVouchersCount').innerText = activeVouchers.toString();
        document.getElementById('usedVouchersCount').innerText = usedVouchers.toString();

        DOM.vouchersList.innerHTML = vouchers.map(voucher => {
            const isExpired = !voucher.isUsed && new Date(voucher.validTo) < now;
            const voucherClass = voucher.isUsed ? 'used' : (isExpired ? 'expired' : '');
            const badgeText = voucher.isUsed ? 'Đã sử dụng' : (isExpired ? 'Hết hạn' : 'Còn hiệu lực');
            const badgeClass = voucher.isUsed ? 'used' : (isExpired ? 'expired' : 'active');
            
            return `
                <div class="voucher-item ${voucherClass}" data-status="${voucher.isUsed ? 'used' : (isExpired ? 'expired' : 'active')}">
                    <span class="voucher-badge ${badgeClass}">${badgeText}</span>
                    <div class="voucher-header">
                        <div class="voucher-code">${voucher.code}</div>
                        <div class="voucher-value">
                            ${voucher.discountType === 'PERCENT' 
                                ? `${voucher.discountValue}%` 
                                : utils.formatMoney(voucher.discountValue)
                            }
                        </div>
                    </div>
                    <div class="voucher-dates">
                        ${new Date(voucher.validFrom).toLocaleDateString('vi-VN')} - 
                        ${new Date(voucher.validTo).toLocaleDateString('vi-VN')}
                    </div>
                    <div class="voucher-description">${voucher.description || 'Không có mô tả'}</div>
                    <div class="voucher-footer">
                        <div class="voucher-status">
                            ${voucher.isUsed 
                                ? `<span class="text-muted">Đã sử dụng cho đơn hàng #${voucher.orderId} vào ${new Date(voucher.usedDate).toLocaleDateString('vi-VN')}</span>` 
                                : (isExpired 
                                    ? '<span class="text-danger">Voucher đã hết hạn</span>'
                                    : '<span class="text-success">Chưa sử dụng</span>')
                            }
                        </div>
                        ${!voucher.isUsed && !isExpired ? `
                            <button class="btn btn-sm btn-outline-danger remove-voucher-btn" 
                                data-user-voucher-id="${voucher.userVoucherId}">
                                <i class="fas fa-trash"></i> Xóa
                            </button>
                        ` : ''}
                    </div>
                </div>
            `;
        }).join('');

        // Add event listeners to remove voucher buttons
        document.querySelectorAll('.remove-voucher-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const userVoucherId = parseInt(e.currentTarget.getAttribute('data-user-voucher-id'));
                if (!userVoucherId) return;
                
                if (confirm('Bạn có chắc chắn muốn xóa voucher này khỏi khách hàng?')) {
                    await removeVoucherFromCustomer(userVoucherId);
                }
            });
        });
        
        // Add event listeners to voucher filters
        document.querySelectorAll('.vouchers-filter-bar .btn').forEach(btn => {
            btn.addEventListener('click', function() {
                document.querySelectorAll('.vouchers-filter-bar .btn').forEach(b => 
                    b.classList.remove('active'));
                this.classList.add('active');
                
                const filter = this.getAttribute('data-filter');
                const voucherItems = document.querySelectorAll('.voucher-item');
                
                voucherItems.forEach(item => {
                    if (filter === 'all') {
                        item.style.display = 'block';
                    } else {
                        const status = item.getAttribute('data-status');
                        item.style.display = status === filter ? 'block' : 'none';
                    }
                });
            });
        });
    }

    function renderOrders(orders) {
        if (!orders || orders.length === 0) {
            DOM.ordersList.innerHTML = '<p class="text-center text-muted">Khách hàng chưa có đơn hàng nào</p>';
            // Update counters to 0
            document.getElementById('totalOrdersCount').innerText = '0';
            document.getElementById('completedOrdersCount').innerText = '0';
            document.getElementById('processingOrdersCount').innerText = '0';
            document.getElementById('cancelledOrdersCount').innerText = '0';
            return;
        }

        // Calculate statistics
        const totalOrders = orders.length;
        const completedOrders = orders.filter(o => o.status === 'Delivered').length;
        const cancelledOrders = orders.filter(o => o.status === 'Cancelled').length;
        const processingOrders = totalOrders - completedOrders - cancelledOrders;
        
        // Update counters
        document.getElementById('totalOrdersCount').innerText = totalOrders.toString();
        document.getElementById('completedOrdersCount').innerText = completedOrders.toString();
        document.getElementById('processingOrdersCount').innerText = processingOrders.toString();
        document.getElementById('cancelledOrdersCount').innerText = cancelledOrders.toString();

        DOM.ordersList.innerHTML = orders.map(order => `
            <div class="order-item status-${order.status.toLowerCase()}" data-status="${order.status}">
                <div class="order-header">
                    <div class="order-id">Đơn hàng #${order.orderId}</div>
                    <div class="order-date">${new Date(order.createdAt).toLocaleDateString('vi-VN')}</div>
                </div>
                
                <div class="order-info">
                    <div class="order-info-section">
                        <div class="order-info-label">Số sản phẩm</div>
                        <div class="order-info-value">${order.itemCount} sản phẩm</div>
                    </div>
                    <div class="order-info-section">
                        <div class="order-info-label">Giảm giá</div>
                        <div class="order-info-value">${utils.formatMoney(order.totalDiscountAmount)}</div>
                    </div>
                    <div class="order-info-section">
                        <div class="order-info-label">Tổng thanh toán</div>
                        <div class="order-info-value">${utils.formatMoney(order.finalPrice)}</div>
                    </div>
                </div>
                
                <div class="order-status">
                    <span class="order-status-badge status-${order.status.toLowerCase()}">
                        ${getOrderStatusText(order.status)}
                    </span>
                </div>
                
                <div class="order-footer">
                    <div>
                        <div class="order-total">
                            ${utils.formatMoney(order.finalPrice)}
                        </div>
                        <div class="order-items-count">
                            ${order.itemCount} sản phẩm
                        </div>
                    </div>
                    <button class="btn btn-sm btn-outline-primary view-order-details-btn" 
                        data-order-id="${order.orderId}" data-bs-toggle="modal" data-bs-target="#orderDetailsModal">
                        <i class="fas fa-eye"></i> Chi tiết
                    </button>
                </div>
            </div>
        `).join('');

        // Add event listeners to view order details buttons
        document.querySelectorAll('.view-order-details-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const orderId = parseInt(e.currentTarget.getAttribute('data-order-id'));
                if (!orderId) return;
                
                await loadOrderDetails(orderId);
            });
        });
        
        // Add event listeners to order filters
        document.querySelectorAll('.orders-filter-bar .btn').forEach(btn => {
            btn.addEventListener('click', function() {
                document.querySelectorAll('.orders-filter-bar .btn').forEach(b => 
                    b.classList.remove('active'));
                this.classList.add('active');
                
                const filter = this.getAttribute('data-filter');
                const orderItems = document.querySelectorAll('.order-item');
                
                orderItems.forEach(item => {
                    if (filter === 'all') {
                        item.style.display = 'block';
                    } else {
                        const status = item.getAttribute('data-status');
                        item.style.display = status === filter ? 'block' : 'none';
                    }
                });
            });
        });
    }

    function renderOrderDetails(order) {
        document.getElementById('orderIdDetail').textContent = order.orderId;
        document.getElementById('orderDateDetail').textContent = new Date(order.createdAt).toLocaleDateString('vi-VN');
        
        const statusEl = document.getElementById('orderStatusDetail');
        statusEl.textContent = getOrderStatusText(order.status);
        statusEl.className = `order-status-badge status-${order.status.toLowerCase()}`;
        
        document.getElementById('recipientNameDetail').textContent = order.recipientName;
        document.getElementById('recipientPhoneDetail').textContent = order.recipientPhone;
        document.getElementById('shippingAddressDetail').textContent = order.shippingAddress;
        
        document.getElementById('orderSubtotalDetail').textContent = utils.formatMoney(order.totalPrice);
        document.getElementById('rankDiscountDetail').textContent = `-${utils.formatMoney(order.rankDiscountAmount)}`;
        document.getElementById('voucherDiscountDetail').textContent = `-${utils.formatMoney(order.voucherDiscountAmount)}`;
        document.getElementById('productDiscountDetail').textContent = `-${utils.formatMoney(order.productDiscountAmount)}`;
        document.getElementById('orderTotalDetail').textContent = utils.formatMoney(order.finalPrice);
        
        const orderItemsTable = document.getElementById('orderItemsTableBody');
        orderItemsTable.innerHTML = order.orderItems.map(item => `
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
        `).join('');
    }

    function renderAvailableVouchers(vouchers) {
        if (!vouchers || vouchers.length === 0) {
            DOM.availableVouchersTableBody.innerHTML = '<tr><td colspan="5" class="text-center text-muted">Không có voucher khả dụng</td></tr>';
            return;
        }

        DOM.availableVouchersTableBody.innerHTML = vouchers.map(voucher => `
            <tr>
                <td>
                    <div class="fw-bold">${voucher.code}</div>
                    <div class="small text-muted">
                        ${voucher.discountType === 'PERCENT' ? 'Phần trăm' : 'Cố định'}
                    </div>
                </td>
                <td>${voucher.description || 'Không có mô tả'}</td>
                <td>
                    <div class="fw-bold">
                        ${voucher.discountType === 'PERCENT' 
                            ? `${voucher.discountValue}%` 
                            : utils.formatMoney(voucher.discountValue)
                        }
                    </div>
                </td>
                <td>${new Date(voucher.validTo).toLocaleDateString('vi-VN')}</td>
                <td>
                    <button class="btn btn-sm btn-primary assign-voucher-btn" 
                        data-voucher-id="${voucher.voucherId}">
                        <i class="fas fa-plus"></i> Gán
                    </button>
                </td>
            </tr>
        `).join('');

        // Add event listeners to assign voucher buttons
        document.querySelectorAll('#availableVouchersTableBody .assign-voucher-btn').forEach(btn => {
            btn.addEventListener('click', async (e) => {
                const voucherId = parseInt(e.currentTarget.getAttribute('data-voucher-id'));
                const userId = parseInt(DOM.voucherCustomerId.value);
                if (!voucherId || !userId) return;
                
                await assignVoucherToCustomer(userId, voucherId);
            });
        });
    }

    // Event Listeners
    function setupEventListeners() {
        // Search Input
        let searchTimeout;
        DOM.customerSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => loadCustomers(1, this.value.trim(), state.sortFilter, state.statusFilter), 300);
        });

        // Sort Filter
        DOM.sortFilter?.addEventListener("change", function () {
            loadCustomers(1, state.searchTerm, this.value, state.statusFilter);
        });

        // Status Filter
        DOM.statusFilter?.addEventListener("change", function () {
            loadCustomers(1, state.searchTerm, state.sortFilter, this.value);
        });

        // Save Customer Button
        DOM.saveCustomerBtn?.addEventListener("click", createCustomer);

        // Update Customer Button
        DOM.updateCustomerBtn?.addEventListener("click", updateCustomer);

        // Delete Customer Button
        DOM.confirmDeleteCustomerBtn?.addEventListener("click", async function () {
            const userId = document.getElementById("deleteCustomerId").value;
            if (!userId) return;

            try {
                utils.showLoadingOverlay(true);
                const customer = state.customers.find(c => c.userId === parseInt(userId));
                
                let newStatus = "Inactive";
                if (customer) {
                    switch (customer.status) {
                        case "Active":
                            newStatus = "Inactive";
                            break;
                        case "Inactive":
                        case "Suspended":
                            newStatus = "Active";
                            break;
                    }
                }
                
                const updateData = {
                    UserId: parseInt(userId),
                    Status: newStatus
                };
                
                const result = await utils.fetchData(API.updateCustomer, 'POST', updateData);

                if (result.success) {
                    let message = "";
                    switch (newStatus) {
                        case "Active":
                            message = "Kích hoạt tài khoản thành công";
                            break;
                        case "Inactive":
                            message = "Vô hiệu hóa tài khoản thành công";
                            break;
                    }
                    utils.showToast(message, "success");
                    await loadCustomers();
                    bootstrap.Modal.getInstance(document.getElementById('deleteCustomerModal')).hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi cập nhật trạng thái tài khoản", "error");
                }
            } catch (error) {
                utils.showToast("Lỗi khi cập nhật trạng thái tài khoản", "error");
            } finally {
                utils.showLoadingOverlay(false);
            }
        });

        // Add Voucher to Customer Button
        DOM.addVoucherToCustomerBtn?.addEventListener("click", function () {
            const userId = document.getElementById("editUserId").value;
            const customerName = document.getElementById("editName").value;
            if (!userId) return;

            DOM.voucherCustomerName.textContent = customerName;
            DOM.voucherCustomerId.value = userId;
            
            loadAvailableVouchers(userId);
            
            const modal = new bootstrap.Modal(document.getElementById('addVoucherToCustomerModal'));
            modal.show();
        });

        // Voucher Filter Buttons
        document.querySelectorAll('.vouchers-filter-bar .btn').forEach(btn => {
            btn.addEventListener('click', function() {
                document.querySelectorAll('.vouchers-filter-bar .btn').forEach(b => 
                    b.classList.remove('active'));
                this.classList.add('active');
                
                const filter = this.getAttribute('data-filter');
                const voucherItems = document.querySelectorAll('.voucher-item');
                
                voucherItems.forEach(item => {
                    if (filter === 'all') {
                        item.style.display = 'block';
                    } else {
                        const status = item.getAttribute('data-status');
                        item.style.display = status === filter ? 'block' : 'none';
                    }
                });
            });
        });

        // Order Filter Buttons
        document.querySelectorAll('.orders-filter-bar .btn').forEach(btn => {
            btn.addEventListener('click', function() {
                document.querySelectorAll('.orders-filter-bar .btn').forEach(b => 
                    b.classList.remove('active'));
                this.classList.add('active');
                
                const filter = this.getAttribute('data-filter');
                const orderItems = document.querySelectorAll('.order-item');
                
                orderItems.forEach(item => {
                    if (filter === 'all') {
                        item.style.display = 'block';
                    } else {
                        const status = item.getAttribute('data-status');
                        item.style.display = status === filter ? 'block' : 'none';
                    }
                });
            });
        });

        // Table Row Actions
        document.addEventListener("click", async (e) => {
            // Edit Customer Button
            const editBtn = e.target.closest(".edit-customer-btn");
            if (editBtn) {
                const userId = parseInt(editBtn.getAttribute("data-user-id"));
                if (!userId) return;
                
                try {
                    utils.showLoadingOverlay(true);
                    const result = await utils.fetchData(API.customer(userId));
                    if (result.success) {
                        state.selectedCustomer = result.data;
                        fillCustomerEditForm(result.data);
                    } else {
                        utils.showToast(result.message || "Không thể tải thông tin khách hàng", "error");
                    }
                } catch (error) {
                    utils.showToast("Không thể tải thông tin khách hàng", "error");
                } finally {
                    utils.showLoadingOverlay(false);
                }
            }

            // Delete Customer Button
            const deleteBtn = e.target.closest(".delete-customer-btn");
            if (deleteBtn) {
                const userId = parseInt(deleteBtn.getAttribute("data-user-id"));
                const userName = deleteBtn.getAttribute("data-user-name");
                if (!userId || !userName) return;
                
                deleteCustomer(userId, userName);
            }

            // Assign Voucher Button
            const assignVoucherBtn = e.target.closest(".assign-voucher-btn");
            if (assignVoucherBtn && !assignVoucherBtn.closest("#availableVouchersTableBody")) {
                const userId = parseInt(assignVoucherBtn.getAttribute("data-user-id"));
                const userName = assignVoucherBtn.getAttribute("data-user-name");
                if (!userId || !userName) return;
                
                DOM.voucherCustomerName.textContent = userName;
                DOM.voucherCustomerId.value = userId;
                
                loadAvailableVouchers(userId);
                
                const modal = new bootstrap.Modal(document.getElementById('addVoucherToCustomerModal'));
                modal.show();
            }
        });

        // Modal Events
        document.getElementById('editCustomerModal')?.addEventListener('hidden.bs.modal', resetEditForm);
        document.getElementById('addCustomerModal')?.addEventListener('hidden.bs.modal', resetAddForm);

        // Add phone validation listeners
        document.getElementById("phone")?.addEventListener("input", function() {
            validatePhoneInput(this);
        });
        
        document.getElementById("editPhone")?.addEventListener("input", function() {
            validatePhoneInput(this);
        });
    }

    // Initialize
    async function initialize() {
        try {
            await loadRanks();
            await loadCustomers();
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang", "error");
        }
    }

    // Start the application
    initialize();
}); 