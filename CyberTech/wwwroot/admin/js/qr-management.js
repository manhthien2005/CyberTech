document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        qrCodes: [],
        tables: [],
        pagination: {
            currentPage: 1,
            pageSize: 3,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        statusFilter: "all",
        sortFilter: ""
    };

    // API Endpoints
    const API = {
        qrCodes: (page, pageSize, searchTerm, statusFilter, sortFilter) =>
            `/QRCodeManage/GetQRCodes?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (statusFilter !== 'all' ? `&status=${statusFilter}` : '') +
            (sortFilter !== 'table_asc' ? `&sortBy=${sortFilter}` : ''),
        tables: '/QRCodeManage/GetTables',
        createQR: '/QRCodeManage/CreateQRCode',
        updateQR: '/QRCodeManage/UpdateQRCode',
        deleteQR: (id) => `/QRCodeManage/DeleteQRCode?id=${id}`
    };

    // DOM Elements
    const DOM = {
        qrGrid: document.getElementById("qrGrid"),
        qrSearchInput: document.getElementById("qrSearchInput"),
        saveQRBtn: document.getElementById("saveQRBtn"),
        updateQRBtn: document.getElementById("updateQRBtn"),
        paginationContainer: document.getElementById("qrPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        statusFilter: document.getElementById("statusFilter"),
        sortFilter: document.getElementById("sortFilter"),
        qrTable: document.getElementById("qrTable"),
        editQRTable: document.getElementById("editQRTable")
    };

    // Skeleton Loaders
    function showQRSkeletons() {
        const skeletonCount = 8;
        DOM.qrGrid.innerHTML = Array(skeletonCount).fill().map(() => `
            <div class="col">
                <div class="qr-card">
                    <div class="qr-card-image skeleton" style="height: 150px;"></div>
                    <div class="qr-card-body">
                        <h5 class="skeleton" style="width: 80%; height: 24px;"></h5>
                        <div class="skeleton" style="width: 60%; height: 16px;"></div>
                        <div class="skeleton" style="width: 40%; height: 16px;"></div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    // Data Loading
    async function loadQRCodes(page = 1, searchTerm = state.searchTerm, statusFilter = state.statusFilter, sortFilter = state.sortFilter) {
        try {
            showQRSkeletons();

            state.pagination.currentPage = page;
            state.searchTerm = searchTerm;
            state.statusFilter = statusFilter;
            state.sortFilter = sortFilter;

            const url = API.qrCodes(page, state.pagination.pageSize, searchTerm, statusFilter, sortFilter);
            const result = await utils.fetchData(url);

            if (result.success) {
                state.qrCodes = result.data;
                state.pagination = result.pagination;
                renderQRCodes();
                renderPagination();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách mã QR", "error");
            }
        } catch (error) {
            console.error('Load QR codes error:', error);
            utils.showToast("Không thể tải danh sách mã QR", "error");
        }
    }

    async function loadTables() {
        try {
            const result = await utils.fetchData(API.tables);

            if (result.success) {
                state.tables = result.data;
                renderTableOptions();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách bàn", "error");
            }
        } catch (error) {
            console.error('Load tables error:', error);
            utils.showToast("Không thể tải danh sách bàn", "error");
        }
    }

    // Rendering Functions
    function renderQRCodes() {
        DOM.qrGrid.innerHTML = state.qrCodes.length === 0
            ? `
                <div class="col-12 d-flex align-items-center justify-content-center w-100" style="min-height: 200px">
                    <div class="text-center">
                        <i class="fas fa-qrcode fa-3x text-muted mb-3"></i>
                        <p class="text-muted mb-0">Không tìm thấy mã QR</p>
                    </div>
                </div>
            `
            : state.qrCodes.map(qr => `
                <div class="col">
                    <div class="qr-card">
                        <div class="qr-card-image">
                            <div class="qr-status-badge ${qr.status === 0 ? 'status-badge-active' : 'status-badge-inactive'}">
                                ${qr.status === 0 ? 'Hoạt động' : 'Không hoạt động'}
                            </div>
                            <img src="/images/QRCodes/${qr.qrCodeImage}" alt="QR Code for Table ${qr.tableNumber}" class="qr-image" data-qr-id="${qr.qrCodeID}">
                        </div>
                        <div class="qr-card-body">
                            <h5 class="qr-card-title">QRCode: ${qr.tableName}</h5>
                            <div class="qr-card-access">Lượt truy cập: ${qr.accessCount}</div>
                            <div class="qr-card-url"><a href="${qr.menuUrl}" target="_blank">Xem menu</a></div>
                        </div>
                        <div class="qr-card-footer">
                            <button class="btn btn-sm btn-outline-primary edit-qr-btn" data-qr-id="${qr.qrCodeID}" data-bs-toggle="modal" data-bs-target="#editQRModal">
                                <i class="fas fa-edit"></i> Sửa
                            </button>
                            <button class="btn btn-sm btn-outline-danger delete-qr-btn" data-qr-id="${qr.qrCodeID}">
                                <i class="fas fa-trash"></i> Xóa
                            </button>
                            <a href="/images/QRCodes/${qr.qrCodeImage}" download class="btn btn-sm btn-outline-success">
                                <i class="fas fa-download"></i> Tải
                            </a>
                        </div>
                    </div>
                </div>
            `).join('');
    }

    function renderTableOptions() {
        const options = state.tables.map(table => `
            <option value="${table.tableID}">Bàn ${table.tableName}</option>
        `).join('');
        DOM.qrTable.innerHTML = `<option value="" selected disabled>Chọn bàn</option>${options}`;
        DOM.editQRTable.innerHTML = `<option value="" selected disabled>Chọn bàn</option>${options}`;
    }

    function renderPagination() {
        utils.renderPagination(
            state.pagination,
            DOM.paginationContainer,
            DOM.paginationItems,
            DOM.paginationPrev,
            DOM.paginationNext,
            (pageNum) => loadQRCodes(pageNum, state.searchTerm, state.statusFilter, state.sortFilter)
        );
    }

    // Form Reset Functions
    function resetAddForm() {
        const form = document.getElementById("addQRForm");
        form.reset();
    }

    function resetEditForm() {
        const form = document.getElementById("editQRForm");
        form.reset();
    }

    // Zoom QR Image
    function openZoomQRModal(qrId) {
        console.log(qrId);
        // Tìm mã QR trong danh sách và hiển thị ảnh zoom ch
        const qrCode = state.qrCodes.find(q => q.qrCodeID === qrId);
        if (!qrCode) return;

        const zoomImage = document.getElementById("zoomQRImage");
        zoomImage.src = `/images/QRCodes/${qrCode.qrCodeImage}`;
        const zoomModal = new bootstrap.Modal(document.getElementById("zoomQRModal"));
        zoomModal.show();
    }
    // Event Listeners
    function setupEventListeners() {
        let searchTimeout;
        DOM.qrSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => loadQRCodes(state.pagination.currentPage, this.value.toLowerCase(), state.statusFilter, state.sortFilter), 300);
        });

        DOM.saveQRBtn?.addEventListener("click", createQRCode);
        DOM.updateQRBtn?.addEventListener("click", updateQRCode);

        DOM.paginationPrev?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage > 1) {
                loadQRCodes(state.pagination.currentPage - 1, state.searchTerm, state.statusFilter, state.sortFilter);
            }
        });

        DOM.paginationNext?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage < state.pagination.totalPages) {
                loadQRCodes(state.pagination.currentPage + 1, state.searchTerm, state.statusFilter, state.sortFilter);
            }
        });

        DOM.statusFilter?.addEventListener("click", (e) => {
            const btn = e.target.closest("button[data-status]");
            if (btn) {
                DOM.statusFilter.querySelectorAll("button").forEach(b => b.classList.remove("active"));
                btn.classList.add("active");
                const status = btn.getAttribute("data-status");
                loadQRCodes(state.pagination.currentPage, state.searchTerm, status, state.sortFilter);
            }
        });

        DOM.sortFilter?.addEventListener("change", function () {
            loadQRCodes(state.pagination.currentPage, state.searchTerm, state.statusFilter, this.value);
        });

        document.addEventListener("click", (e) => {
            const editBtn = e.target.closest(".edit-qr-btn");
            const deleteBtn = e.target.closest(".delete-qr-btn");
            const qrImage = e.target.closest(".qr-image");

            if (editBtn) openEditQRModal(parseInt(editBtn.getAttribute("data-qr-id")));
            if (deleteBtn) deleteQRCode(parseInt(deleteBtn.getAttribute("data-qr-id")));
            if (qrImage) openZoomQRModal(parseInt(qrImage.getAttribute("data-qr-id")));
        });

        document.getElementById("addQRModal")?.addEventListener("hidden.bs.modal", resetAddForm);
        document.getElementById("editQRModal")?.addEventListener("hidden.bs.modal", resetEditForm);
    
    }

    // CRUD Operations
    async function createQRCode() {
        const form = document.getElementById("addQRForm");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const qrCode = {
            TableID: parseInt(document.getElementById("qrTable").value)
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.createQR, 'POST', qrCode);

            if (result.success) {
                utils.showToast("Thêm mã QR thành công", "success");
                await loadQRCodes();
                resetAddForm();
                bootstrap.Modal.getInstance(document.getElementById('addQRModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi thêm mã QR", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function updateQRCode() {
        const form = document.getElementById("editQRForm");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const qrCode = {
            QRCodeID: parseInt(document.getElementById("editQRId").value),
            TableID: parseInt(document.getElementById("editQRTable").value),
            Status: parseInt(document.getElementById("editQRStatus").value)
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateQR, 'POST', qrCode);

            if (result.success) {
                utils.showToast("Cập nhật mã QR thành công", "success");
                await loadQRCodes();
                resetEditForm();
                bootstrap.Modal.getInstance(document.getElementById('editQRModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật mã QR", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteQRCode(qrId) {
        const qrCode = state.qrCodes.find(q => q.qrCodeID === qrId);
        if (!qrCode) return;

        document.getElementById("deleteQRTable").innerHTML = qrCode.tableName;
        document.getElementById("deleteQRId").value = qrCode.qrCodeID;
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteQRModal"));
        deleteModal.show();

        const confirmBtn = document.getElementById("confirmDeleteQRBtn");
        const deleteHandler = async () => {
            try {
                utils.showLoadingOverlay(true);
                const result = await utils.fetchData(API.deleteQR(qrId), 'POST');

                if (result.success) {
                    utils.showToast("Xóa mã QR thành công", "success");
                    await loadQRCodes();
                    deleteModal.hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi xóa mã QR", "error");
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

    async function openEditQRModal(qrId) {
        const qrCode = state.qrCodes.find(q => q.qrCodeID === qrId);
        if (!qrCode) return;
        console.log(qrCode);
        document.getElementById("editQRId").value = qrCode.qrCodeID;
        document.getElementById("editQRTable").value = qrCode.tableID;
        document.getElementById("editQRStatus").value = qrCode.status;
    }

    // Initialize
    async function initialize() {
        try {
            await Promise.all([
                loadQRCodes(),
                loadTables()
            ]);
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang", "error");
        }
    }

    // Start the application
    initialize();
});