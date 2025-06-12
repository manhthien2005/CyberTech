document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        vouchers: [],
        pagination: {
            currentPage: 1,
            pageSize: 3,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        sortFilter: "validto_desc",
        selectedProducts: [],
        selectedUser: null
    };

    // Date range variables - defined at the top for accessibility
    let currentStartDate = null;
    let currentEndDate = null;

    // API Endpoints
    const API = {
        vouchers: (page, pageSize, searchTerm, sortFilter) =>
            `/VoucherManage/GetVouchers?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (sortFilter ? `&sortBy=${sortFilter}` : ''),
        createVoucher: '/VoucherManage/CreateVoucher',
        updateVoucher: '/VoucherManage/UpdateVoucher',
        deleteVoucher: (id) => `/VoucherManage/DeleteVoucher?id=${id}`,
        sendVoucherToUser: (voucherId, userId) => `/VoucherManage/SendVoucherToUser?voucherId=${voucherId}&userId=${userId}`,
        sendVoucherToAll: (voucherId) => `/VoucherManage/SendVoucherToAllUsers?voucherId=${voucherId}`,
        getUsers: (searchTerm) => `/VoucherManage/GetUsers?searchTerm=${encodeURIComponent(searchTerm)}`,
        getProducts: (searchTerm) => `/VoucherManage/GetProducts?searchTerm=${encodeURIComponent(searchTerm)}`
    };

    // DOM Elements
    const DOM = {
        voucherGrid: document.getElementById("voucherGrid"),
        voucherSearchInput: document.getElementById("voucherSearchInput"),
        saveVoucherBtn: document.getElementById("saveVoucherBtn"),
        updateVoucherBtn: document.getElementById("updateVoucherBtn"),
        paginationContainer: document.getElementById("voucherPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        sortFilter: document.getElementById("sortFilter"),
        productSearch: document.getElementById("productSearch"),
        productSuggestions: document.getElementById("productSuggestions"),
        selectedProducts: document.getElementById("selectedProducts"),
        editProductSearch: document.getElementById("editProductSearch"),
        editProductSuggestions: document.getElementById("editProductSuggestions"),
        editSelectedProducts: document.getElementById("editSelectedProducts"),
        appliesTo: document.getElementById("appliesTo"),
        editAppliesTo: document.getElementById("editAppliesTo"),
        productSelectionContainer: document.getElementById("productSelectionContainer"),
        editProductSelectionContainer: document.getElementById("editProductSelectionContainer"),
        userSearch: document.getElementById("userSearch"),
        userSuggestions: document.getElementById("userSuggestions"),
        selectedUser: document.getElementById("selectedUser"),
        sendToAllUsers: document.getElementById("sendToAllUsers"),
        confirmSendVoucherBtn: document.getElementById("confirmSendVoucherBtn")
    };

    // Skeleton Loaders
    function showVoucherSkeletons() {
        const skeletonCount = 6;
        DOM.voucherGrid.innerHTML = Array(skeletonCount).fill().map(() => `
            <div class="col">
                <div class="voucher-card">
                    <div class="voucher-card-header">
                        <div class="skeleton" style="width: 70%; height: 24px;"></div>
                        <div class="skeleton" style="width: 40%; height: 16px; margin-top: 8px;"></div>
                    </div>
                    <div class="voucher-card-body">
                        <div class="skeleton" style="width: 60%; height: 16px; margin-bottom: 8px;"></div>
                        <div class="skeleton" style="width: 80%; height: 16px; margin-bottom: 8px;"></div>
                        <div class="skeleton" style="width: 50%; height: 16px;"></div>
                    </div>
                    <div class="voucher-card-footer">
                        <div class="skeleton" style="width: 100%; height: 36px;"></div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    // Data Loading
    async function loadVouchers(page = 1, searchTerm = state.searchTerm, sortFilter = state.sortFilter) {
        try {
            showVoucherSkeletons();

            state.pagination.currentPage = page;
            state.searchTerm = searchTerm;
            state.sortFilter = sortFilter;

            let url = API.vouchers(page, state.pagination.pageSize, searchTerm, sortFilter);
            if (currentStartDate && currentEndDate) {
                url += `&startDate=${currentStartDate}&endDate=${currentEndDate}`;
            }

            const result = await utils.fetchData(url);

            if (result.success) {
                state.vouchers = result.data;
                state.pagination = result.pagination;
                renderVouchers();
                renderPagination();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách voucher", "error");
            }
        } catch (error) {
            console.error('Load vouchers error:', error);
            utils.showToast("Không thể tải danh sách voucher", "error");
        }
    }

    // Rendering Functions
    function renderVouchers() {
        DOM.voucherGrid.innerHTML = state.vouchers.length === 0
            ? `
                <div class="col-12 d-flex align-items-center justify-content-center w-100" style="min-height: 200px">
                    <div class="text-center">
                        <i class="fas fa-ticket-alt fa-3x text-muted mb-3"></i>
                        <p class="text-muted mb-0">Không tìm thấy voucher</p>
                    </div>
                </div>
            `
            : state.vouchers.map(voucher => `
                <div class="col">
                    <div class="voucher-card">
                        <div class="discount-badge">${voucher.discountType === 'PERCENT' ? voucher.discountValue + '%' : utils.formatMoney(voucher.discountValue)}</div>
                        <div class="voucher-card-header">
                            <h5 class="voucher-card-title">${voucher.code}</h5>
                            <div class="voucher-card-validity">Hiệu lực: ${new Date(voucher.validFrom).toLocaleDateString('vi-VN')} - ${new Date(voucher.validTo).toLocaleDateString('vi-VN')}</div>
                        </div>
                        <div class="voucher-card-body">
                            <div class="voucher-card-info">
                                <div class="voucher-card-info-label">Loại giảm giá</div>
                                <div class="voucher-card-info-value">${voucher.discountType === 'PERCENT' ? 'Phần trăm' : 'Cố định'}</div>
                            </div>
                            <div class="voucher-card-info">
                                <div class="voucher-card-info-label">Áp dụng cho</div>
                                <div class="voucher-card-info-value">${voucher.appliesTo === 'Order' ? 'Toàn bộ đơn hàng' : 'Sản phẩm cụ thể'} (${voucher.productCount} sản phẩm)</div>
                            </div>
                            <div class="voucher-card-description">${voucher.description || 'Không có mô tả'}</div>
                        </div>
                        <div class="voucher-card-footer">
                            <button class="btn btn-sm btn-outline-primary edit-voucher-btn" data-voucher-id="${voucher.voucherId}" data-bs-toggle="modal" data-bs-target="#editVoucherModal">
                                <i class="fas fa-edit"></i> Sửa
                            </button>
                            <button class="btn btn-sm btn-outline-danger delete-voucher-btn" data-voucher-id="${voucher.voucherId}">
                                <i class="fas fa-trash"></i> Xóa
                            </button>
                            <button class="btn btn-sm btn-outline-success send-voucher-btn" data-voucher-id="${voucher.voucherId}" data-voucher-code="${voucher.code}" data-bs-toggle="modal" data-bs-target="#sendVoucherModal">
                                <i class="fas fa-paper-plane"></i> Gửi
                            </button>
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
            (pageNum) => loadVouchers(pageNum, state.searchTerm, state.sortFilter)
        );
    }

    function renderSelectedProducts(container, products) {
        container.innerHTML = products.length > 0
            ? products.map(p => `
                <span class="selected-item-badge" data-id="${p.id}">
                    ${p.name}
                    <span class="remove-btn" onclick="removeProduct(${p.id})" style="cursor: pointer; margin-left: 5px;">&times;</span>
                </span>
            `).join('')
            : '<p class="text-muted">Chưa có sản phẩm nào được chọn</p>';
    }

    function renderSelectedUser() {
        if (state.selectedUser) {
            DOM.selectedUser.innerHTML = `
                <span class="selected-item-badge" data-id="${state.selectedUser.id}">
                    ${state.selectedUser.name} (${state.selectedUser.email})
                    <span class="remove-btn" onclick="removeSelectedUser()" style="cursor: pointer; margin-left: 5px;">&times;</span>
                </span>
            `;
        } else {
            DOM.selectedUser.innerHTML = '<p class="text-muted">Chưa chọn người dùng nào</p>';
        }
    }

    // Form Reset Functions
    function resetAddForm() {
        const form = document.getElementById("addVoucherForm");
        form.reset();
        state.selectedProducts = [];
        renderSelectedProducts(DOM.selectedProducts, state.selectedProducts);
        DOM.productSelectionContainer.classList.add('d-none');
        document.getElementById("isActive").checked = true;
    }

    function resetEditForm() {
        const form = document.getElementById("editVoucherForm");
        form.reset();
        state.selectedProducts = [];
        renderSelectedProducts(DOM.editSelectedProducts, state.selectedProducts);
        DOM.editProductSelectionContainer.classList.add('d-none');
    }

    function resetSendForm() {
        const form = document.getElementById("sendVoucherForm");
        form.reset();
        state.selectedUser = null;
        DOM.sendToAllUsers.checked = false;
        renderSelectedUser();
    }

    // CRUD Operations
    async function createVoucher() {
        const form = document.getElementById("addVoucherForm");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const voucher = {
            Code: document.getElementById("voucherCode").value,
            Description: document.getElementById("description").value,
            DiscountType: document.getElementById("discountType").value,
            DiscountValue: parseFloat(document.getElementById("discountValue").value),
            QuantityAvailable: document.getElementById("quantityAvailable").value ? parseInt(document.getElementById("quantityAvailable").value) : null,
            ValidFrom: document.getElementById("validFrom").value,
            ValidTo: document.getElementById("validTo").value,
            IsActive: document.getElementById("isActive").checked,
            AppliesTo: document.getElementById("appliesTo").value,
            IsSystemWide: document.getElementById("isSystemWide").value === 'true',
            ProductIds: state.selectedProducts.map(p => p.id)
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.createVoucher, 'POST', voucher);

            if (result.success) {
                utils.showToast("Thêm voucher thành công", "success");
                await loadVouchers();
                resetAddForm();
                bootstrap.Modal.getInstance(document.getElementById('addVoucherModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi thêm voucher", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function updateVoucher() {
        const form = document.getElementById("editVoucherForm");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const voucher = {
            VoucherId: parseInt(document.getElementById("editVoucherId").value),
            Code: document.getElementById("editVoucherCode").value,
            Description: document.getElementById("editDescription").value,
            DiscountType: document.getElementById("editDiscountType").value,
            DiscountValue: parseFloat(document.getElementById("editDiscountValue").value),
            QuantityAvailable: document.getElementById("editQuantityAvailable").value ? parseInt(document.getElementById("editQuantityAvailable").value) : null,
            ValidFrom: document.getElementById("editValidFrom").value,
            ValidTo: document.getElementById("editValidTo").value,
            IsActive: document.getElementById("editIsActive").checked,
            AppliesTo: document.getElementById("editAppliesTo").value,
            IsSystemWide: document.getElementById("editIsSystemWide").value === 'true',
            ProductIds: state.selectedProducts.map(p => p.id)
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateVoucher, 'POST', voucher);

            if (result.success) {
                utils.showToast("Cập nhật voucher thành công", "success");
                await loadVouchers();
                resetEditForm();
                bootstrap.Modal.getInstance(document.getElementById('editVoucherModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật voucher", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteVoucher(voucherId) {
        const voucher = state.vouchers.find(v => v.voucherId === voucherId);
        if (!voucher) return;

        document.getElementById("deleteVoucherCode").textContent = voucher.code;
        document.getElementById("deleteVoucherId").value = voucher.voucherId;
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteVoucherModal"));
        deleteModal.show();

        const confirmBtn = document.getElementById("confirmDeleteVoucherBtn");
        const deleteHandler = async () => {
            try {
                utils.showLoadingOverlay(true);
                const result = await utils.fetchData(API.deleteVoucher(voucherId), 'POST');

                if (result.success) {
                    utils.showToast("Xóa voucher thành công", "success");
                    await loadVouchers();
                    deleteModal.hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi xóa voucher", "error");
                }
            } catch (error) {
                utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
            } finally {
                utils.showLoadingOverlay(false);
                confirmBtn.removeEventListener("click", deleteHandler);
            }
        };

        confirmBtn.addEventListener("click", deleteHandler);
    }

    async function openEditVoucherModal(voucherId) {
        const voucher = state.vouchers.find(v => v.voucherId === voucherId);
        if (!voucher) return;

        document.getElementById("editVoucherId").value = voucher.voucherId;
        document.getElementById("editVoucherCode").value = voucher.code;
        document.getElementById("editDescription").value = voucher.description || '';
        document.getElementById("editDiscountType").value = voucher.discountType;
        document.getElementById("editDiscountValue").value = voucher.discountValue;
        document.getElementById("editQuantityAvailable").value = voucher.quantityAvailable || '';
        document.getElementById("editValidFrom").value = new Date(voucher.validFrom).toISOString().split('T')[0];
        document.getElementById("editValidTo").value = new Date(voucher.validTo).toISOString().split('T')[0];
        document.getElementById("editIsActive").checked = voucher.isActive;
        document.getElementById("editAppliesTo").value = voucher.appliesTo;
        document.getElementById("editIsSystemWide").value = voucher.isSystemWide.toString();

        if (voucher.appliesTo === 'Product') {
            DOM.editProductSelectionContainer.classList.remove('d-none');
            // In a real scenario, you would fetch the actual products associated with this voucher
            state.selectedProducts = [];
            renderSelectedProducts(DOM.editSelectedProducts, state.selectedProducts);
        } else {
            DOM.editProductSelectionContainer.classList.add('d-none');
            state.selectedProducts = [];
            renderSelectedProducts(DOM.editSelectedProducts, state.selectedProducts);
        }
    }

    async function openSendVoucherModal(voucherId, voucherCode) {
        document.getElementById("sendVoucherId").value = voucherId;
        document.getElementById("sendVoucherCode").textContent = voucherCode;
        resetSendForm();
    }

    async function sendVoucher() {
        const voucherId = parseInt(document.getElementById("sendVoucherId").value);
        const sendToAll = DOM.sendToAllUsers.checked;

        try {
            utils.showLoadingOverlay(true);
            let result;
            if (sendToAll) {
                result = await utils.fetchData(API.sendVoucherToAll(voucherId), 'POST');
            } else {
                if (!state.selectedUser) {
                    utils.showToast("Vui lòng chọn người dùng", "error");
                    return;
                }
                result = await utils.fetchData(API.sendVoucherToUser(voucherId, state.selectedUser.id), 'POST');
            }

            if (result.success) {
                utils.showToast(result.message || "Gửi voucher thành công", "success");
                bootstrap.Modal.getInstance(document.getElementById('sendVoucherModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi gửi voucher", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Autocomplete Functions
    async function searchProducts(searchTerm, container, callback) {
        try {
            const result = await utils.fetchData(API.getProducts(searchTerm));
            if (result && Array.isArray(result)) {
                container.innerHTML = result.map(item => `
                    <a class="dropdown-item" href="#" data-id="${item.id}" data-name="${item.name}">${item.name}</a>
                `).join('');
                container.classList.add('show');

                container.querySelectorAll('.dropdown-item').forEach(item => {
                    item.addEventListener('click', (e) => {
                        e.preventDefault();
                        const id = parseInt(item.getAttribute('data-id'));
                        const name = item.getAttribute('data-name');
                        callback({ id, name });
                        container.classList.remove('show');
                    });
                });
            } else {
                container.innerHTML = '<p class="text-muted p-2">Không tìm thấy sản phẩm</p>';
                container.classList.add('show');
            }
        } catch (error) {
            console.error('Error searching products:', error);
            container.innerHTML = '<p class="text-muted p-2">Lỗi khi tìm kiếm sản phẩm</p>';
            container.classList.add('show');
        }
    }

    async function searchUsers(searchTerm) {
        try {
            const result = await utils.fetchData(API.getUsers(searchTerm));
            if (result && Array.isArray(result)) {
                DOM.userSuggestions.innerHTML = result.map(item => `
                    <a class="dropdown-item" href="#" data-id="${item.id}" data-name="${item.name}" data-email="${item.email}">${item.name} (${item.email})</a>
                `).join('');
                DOM.userSuggestions.classList.add('show');

                DOM.userSuggestions.querySelectorAll('.dropdown-item').forEach(item => {
                    item.addEventListener('click', (e) => {
                        e.preventDefault();
                        const id = parseInt(item.getAttribute('data-id'));
                        const name = item.getAttribute('data-name');
                        const email = item.getAttribute('data-email');
                        state.selectedUser = { id, name, email };
                        renderSelectedUser();
                        DOM.userSearch.value = '';
                        DOM.userSuggestions.classList.remove('show');
                    });
                });
            } else {
                DOM.userSuggestions.innerHTML = '<p class="text-muted p-2">Không tìm thấy người dùng</p>';
                DOM.userSuggestions.classList.add('show');
            }
        } catch (error) {
            console.error('Error searching users:', error);
            DOM.userSuggestions.innerHTML = '<p class="text-muted p-2">Lỗi khi tìm kiếm người dùng</p>';
            DOM.userSuggestions.classList.add('show');
        }
    }

    // Helper Functions for Product Selection
    function addProduct(product) {
        if (!state.selectedProducts.some(p => p.id === product.id)) {
            state.selectedProducts.push(product);
            renderSelectedProducts(DOM.selectedProducts, state.selectedProducts);
            DOM.productSearch.value = '';
        }
    }

    function addEditProduct(product) {
        if (!state.selectedProducts.some(p => p.id === product.id)) {
            state.selectedProducts.push(product);
            renderSelectedProducts(DOM.editSelectedProducts, state.selectedProducts);
            DOM.editProductSearch.value = '';
        }
    }

    function removeProduct(productId) {
        state.selectedProducts = state.selectedProducts.filter(p => p.id !== productId);
        renderSelectedProducts(DOM.selectedProducts, state.selectedProducts);
        renderSelectedProducts(DOM.editSelectedProducts, state.selectedProducts);
    }

    function removeSelectedUser() {
        state.selectedUser = null;
        renderSelectedUser();
    }

    // Event Listeners
    function setupEventListeners() {
        let searchTimeout;
        DOM.voucherSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => loadVouchers(1, this.value.toLowerCase(), state.sortFilter), 300);
        });

        DOM.saveVoucherBtn?.addEventListener("click", createVoucher);
        DOM.updateVoucherBtn?.addEventListener("click", updateVoucher);
        DOM.confirmSendVoucherBtn?.addEventListener("click", sendVoucher);

        DOM.paginationPrev?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage > 1) {
                loadVouchers(state.pagination.currentPage - 1, state.searchTerm, state.sortFilter);
            }
        });

        DOM.paginationNext?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage < state.pagination.totalPages) {
                loadVouchers(state.pagination.currentPage + 1, state.searchTerm, state.sortFilter);
            }
        });

        DOM.sortFilter?.addEventListener("change", function () {
            loadVouchers(1, state.searchTerm, this.value);
        });

        // Product Autocomplete for Add Modal
        let productSearchTimeout;
        DOM.productSearch?.addEventListener("input", function () {
            clearTimeout(productSearchTimeout);
            productSearchTimeout = setTimeout(() => {
                const searchTerm = this.value.trim();
                if (searchTerm.length >= 2) {
                    searchProducts(searchTerm, DOM.productSuggestions, addProduct);
                } else {
                    DOM.productSuggestions.classList.remove('show');
                }
            }, 300);
        });

        // Product Autocomplete for Edit Modal
        let editProductSearchTimeout;
        DOM.editProductSearch?.addEventListener("input", function () {
            clearTimeout(editProductSearchTimeout);
            editProductSearchTimeout = setTimeout(() => {
                const searchTerm = this.value.trim();
                if (searchTerm.length >= 2) {
                    searchProducts(searchTerm, DOM.editProductSuggestions, addEditProduct);
                } else {
                    DOM.editProductSuggestions.classList.remove('show');
                }
            }, 300);
        });

        // User Autocomplete for Send Modal
        let userSearchTimeout;
        DOM.userSearch?.addEventListener("input", function () {
            clearTimeout(userSearchTimeout);
            userSearchTimeout = setTimeout(() => {
                const searchTerm = this.value.trim();
                if (searchTerm.length >= 2) {
                    searchUsers(searchTerm);
                } else {
                    DOM.userSuggestions.classList.remove('show');
                }
            }, 300);
        });

        // Show/Hide Product Selection based on AppliesTo
        DOM.appliesTo?.addEventListener("change", function () {
            if (this.value === 'Product') {
                DOM.productSelectionContainer.classList.remove('d-none');
            } else {
                DOM.productSelectionContainer.classList.add('d-none');
                state.selectedProducts = [];
                renderSelectedProducts(DOM.selectedProducts, state.selectedProducts);
            }
        });

        DOM.editAppliesTo?.addEventListener("change", function () {
            if (this.value === 'Product') {
                DOM.editProductSelectionContainer.classList.remove('d-none');
            } else {
                DOM.editProductSelectionContainer.classList.add('d-none');
                state.selectedProducts = [];
                renderSelectedProducts(DOM.editSelectedProducts, state.selectedProducts);
            }
        });

        // Send to All Users Toggle
        DOM.sendToAllUsers?.addEventListener("change", function () {
            if (this.checked) {
                DOM.userSearch.disabled = true;
                state.selectedUser = null;
                renderSelectedUser();
            } else {
                DOM.userSearch.disabled = false;
            }
        });

        // Card Button Clicks
        document.addEventListener("click", (e) => {
            const editBtn = e.target.closest(".edit-voucher-btn");
            const deleteBtn = e.target.closest(".delete-voucher-btn");
            const sendBtn = e.target.closest(".send-voucher-btn");

            if (editBtn) openEditVoucherModal(parseInt(editBtn.getAttribute("data-voucher-id")));
            if (deleteBtn) deleteVoucher(parseInt(deleteBtn.getAttribute("data-voucher-id")));
            if (sendBtn) openSendVoucherModal(parseInt(sendBtn.getAttribute("data-voucher-id")), sendBtn.getAttribute("data-voucher-code"));
        });

        // Modal Hidden Events
        document.getElementById("addVoucherModal")?.addEventListener("hidden.bs.modal", resetAddForm);
        document.getElementById("editVoucherModal")?.addEventListener("hidden.bs.modal", resetEditForm);
        document.getElementById("sendVoucherModal")?.addEventListener("hidden.bs.modal", resetSendForm);
    }

    // Initialize
    async function initialize() {
        try {
            await loadVouchers();
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang", "error");
        }
    }

    // Start the application
    initialize();

    // Make functions available to global scope if needed for inline event handlers
    window.removeProduct = removeProduct;
    window.removeSelectedUser = removeSelectedUser;

    // Initialize date range picker
    $(document).ready(function() {
        $('#dateRangeFilter').daterangepicker({
            opens: 'left',
            locale: {
                format: 'DD/MM/YYYY',
                cancelLabel: 'Hủy',
                applyLabel: 'Áp dụng',
                fromLabel: 'Từ',
                toLabel: 'Đến',
                customRangeLabel: 'Tùy chỉnh',
                daysOfWeek: ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'],
                monthNames: ['Tháng 1', 'Tháng 2', 'Tháng 3', 'Tháng 4', 'Tháng 5', 'Tháng 6', 'Tháng 7', 'Tháng 8', 'Tháng 9', 'Tháng 10', 'Tháng 11', 'Tháng 12'],
                firstDay: 1
            },
            ranges: {
                'Hôm nay': [moment(), moment()],
                'Tuần này': [moment().startOf('week'), moment().endOf('week')],
                'Tháng này': [moment().startOf('month'), moment().endOf('month')],
                'Tháng trước': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')]
            }
        }, function(start, end, label) {
            currentStartDate = start.format('YYYY-MM-DD');
            currentEndDate = end.format('YYYY-MM-DD');
            loadVouchers(1);
        });

        // Clear date filter on cancel
        $('#dateRangeFilter').on('cancel.daterangepicker', function(ev, picker) {
            $(this).val('');
            currentStartDate = null;
            currentEndDate = null;
            loadVouchers(1);
        });
    });
}); 