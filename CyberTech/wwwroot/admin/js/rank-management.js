document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        ranks: [],
        pagination: {
            currentPage: 1,
            pageSize: 3,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        sortFilter: "priority_asc"
    };

    // API Endpoints
    const API = {
        ranks: (page, pageSize, searchTerm, sortFilter) =>
            `/RankManage/GetRanks?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (sortFilter ? `&sortBy=${sortFilter}` : ''),
        createRank: '/RankManage/CreateRank',
        updateRank: '/RankManage/UpdateRank',
        deleteRank: (id) => `/RankManage/DeleteRank?id=${id}`
    };

    // DOM Elements
    const DOM = {
        rankGrid: document.getElementById("rankGrid"),
        rankSearchInput: document.getElementById("rankSearchInput"),
        saveRankBtn: document.getElementById("saveRankBtn"),
        updateRankBtn: document.getElementById("updateRankBtn"),
        paginationContainer: document.getElementById("rankPagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        sortFilter: document.getElementById("sortFilter")
    };

    // Skeleton Loaders
    function showRankSkeletons() {
        const skeletonCount = 6;
        DOM.rankGrid.innerHTML = Array(skeletonCount).fill().map(() => `
            <div class="col">
                <div class="rank-card">
                    <div class="rank-card-header">
                        <div class="skeleton" style="width: 70%; height: 24px;"></div>
                        <div class="skeleton" style="width: 40%; height: 16px; margin-top: 8px;"></div>
                    </div>
                    <div class="rank-card-body">
                        <div class="skeleton" style="width: 60%; height: 16px; margin-bottom: 8px;"></div>
                        <div class="skeleton" style="width: 80%; height: 16px; margin-bottom: 8px;"></div>
                        <div class="skeleton" style="width: 50%; height: 16px;"></div>
                    </div>
                    <div class="rank-card-footer">
                        <div class="skeleton" style="width: 100%; height: 36px;"></div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    // Data Loading
    async function loadRanks(page = 1, searchTerm = state.searchTerm, sortFilter = state.sortFilter) {
        try {
            showRankSkeletons();

            state.pagination.currentPage = page;
            state.searchTerm = searchTerm;
            state.sortFilter = sortFilter;

            const url = API.ranks(page, state.pagination.pageSize, searchTerm, sortFilter);
            const result = await utils.fetchData(url);

            if (result.success) {
                state.ranks = result.data;
                state.pagination = result.pagination;
                renderRanks();
                renderPagination();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách cấp bậc", "error");
            }
        } catch (error) {
            console.error('Load ranks error:', error);
            utils.showToast("Không thể tải danh sách cấp bậc", "error");
        }
    }

    // Rendering Functions
    function renderRanks() {
        DOM.rankGrid.innerHTML = state.ranks.length === 0
            ? `
                <div class="col-12 d-flex align-items-center justify-content-center w-100" style="min-height: 200px">
                    <div class="text-center">
                        <i class="fas fa-trophy fa-3x text-muted mb-3"></i>
                        <p class="text-muted mb-0">Không tìm thấy cấp bậc</p>
                    </div>
                </div>
            `
            : state.ranks.map(rank => `
                <div class="col">
                    <div class="rank-card">
                        <div class="discount-badge">${rank.discountPercent}% Giảm giá</div>
                        <div class="rank-card-header">
                            <h5 class="rank-card-title">${rank.rankName}</h5>
                            <div class="rank-card-priority">Mức ưu tiên: ${rank.priorityLevel}</div>
                        </div>
                        <div class="rank-card-body">
                            <div class="rank-card-info">
                                <div class="rank-card-info-label">Chi tiêu tối thiểu</div>
                                <div class="rank-card-info-value">${utils.formatMoney(rank.minTotalSpent)}</div>
                            </div>
                            <div class="rank-card-description">${rank.description || 'Không có mô tả'}</div>
                        </div>
                        <div class="rank-card-footer">
                            <button class="btn btn-sm btn-outline-primary edit-rank-btn" data-rank-id="${rank.rankId}" data-bs-toggle="modal" data-bs-target="#editRankModal">
                                <i class="fas fa-edit"></i> Sửa
                            </button>
                            <button class="btn btn-sm btn-outline-danger delete-rank-btn" data-rank-id="${rank.rankId}">
                                <i class="fas fa-trash"></i> Xóa
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
            (pageNum) => loadRanks(pageNum, state.searchTerm, state.sortFilter)
        );
    }

    // Form Reset Functions
    function resetAddForm() {
        const form = document.getElementById("addRankForm");
        form.reset();
    }

    function resetEditForm() {
        const form = document.getElementById("editRankForm");
        form.reset();
    }

    // CRUD Operations
    async function createRank() {
        const form = document.getElementById("addRankForm");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const rank = {
            RankName: document.getElementById("rankName").value,
            MinTotalSpent: parseFloat(document.getElementById("minTotalSpent").value),
            DiscountPercent: parseFloat(document.getElementById("discountPercent").value),
            PriorityLevel: parseInt(document.getElementById("priorityLevel").value),
            Description: document.getElementById("description").value
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.createRank, 'POST', rank);

            if (result.success) {
                utils.showToast("Thêm cấp bậc thành công", "success");
                await loadRanks();
                resetAddForm();
                bootstrap.Modal.getInstance(document.getElementById('addRankModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi thêm cấp bậc", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function updateRank() {
        const form = document.getElementById("editRankForm");
        if (!form.checkValidity()) {
            form.reportValidity();
            return;
        }

        const rank = {
            RankId: parseInt(document.getElementById("editRankId").value),
            RankName: document.getElementById("editRankName").value,
            MinTotalSpent: parseFloat(document.getElementById("editMinTotalSpent").value),
            DiscountPercent: parseFloat(document.getElementById("editDiscountPercent").value),
            PriorityLevel: parseInt(document.getElementById("editPriorityLevel").value),
            Description: document.getElementById("editDescription").value
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateRank, 'POST', rank);

            if (result.success) {
                utils.showToast("Cập nhật cấp bậc thành công", "success");
                await loadRanks();
                resetEditForm();
                bootstrap.Modal.getInstance(document.getElementById('editRankModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật cấp bậc", "error");
            }
        } catch (error) {
            utils.showToast("Bạn không có quyền truy cập chức năng này", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteRank(rankId) {
        const rank = state.ranks.find(r => r.rankId === rankId);
        if (!rank) return;

        document.getElementById("deleteRankName").textContent = rank.rankName;
        document.getElementById("deleteRankId").value = rank.rankId;
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteRankModal"));
        deleteModal.show();

        const confirmBtn = document.getElementById("confirmDeleteRankBtn");
        const deleteHandler = async () => {
            try {
                utils.showLoadingOverlay(true);
                const result = await utils.fetchData(API.deleteRank(rankId), 'POST');

                if (result.success) {
                    utils.showToast("Xóa cấp bậc thành công", "success");
                    await loadRanks();
                    deleteModal.hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi xóa cấp bậc", "error");
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

    async function openEditRankModal(rankId) {
        const rank = state.ranks.find(r => r.rankId === rankId);
        if (!rank) return;

        document.getElementById("editRankId").value = rank.rankId;
        document.getElementById("editRankName").value = rank.rankName;
        document.getElementById("editMinTotalSpent").value = rank.minTotalSpent;
        document.getElementById("editDiscountPercent").value = rank.discountPercent;
        document.getElementById("editPriorityLevel").value = rank.priorityLevel;
        document.getElementById("editDescription").value = rank.description || '';
    }

    // Event Listeners
    function setupEventListeners() {
        let searchTimeout;
        DOM.rankSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => loadRanks(1, this.value.toLowerCase(), state.sortFilter), 300);
        });

        DOM.saveRankBtn?.addEventListener("click", createRank);
        DOM.updateRankBtn?.addEventListener("click", updateRank);

        DOM.paginationPrev?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage > 1) {
                loadRanks(state.pagination.currentPage - 1, state.searchTerm, state.sortFilter);
            }
        });

        DOM.paginationNext?.addEventListener("click", (e) => {
            e.preventDefault();
            if (state.pagination.currentPage < state.pagination.totalPages) {
                loadRanks(state.pagination.currentPage + 1, state.searchTerm, state.sortFilter);
            }
        });

        DOM.sortFilter?.addEventListener("change", function () {
            loadRanks(1, state.searchTerm, this.value);
        });

        document.addEventListener("click", (e) => {
            const editBtn = e.target.closest(".edit-rank-btn");
            const deleteBtn = e.target.closest(".delete-rank-btn");

            if (editBtn) openEditRankModal(parseInt(editBtn.getAttribute("data-rank-id")));
            if (deleteBtn) deleteRank(parseInt(deleteBtn.getAttribute("data-rank-id")));
        });

        document.getElementById("addRankModal")?.addEventListener("hidden.bs.modal", resetAddForm);
        document.getElementById("editRankModal")?.addEventListener("hidden.bs.modal", resetEditForm);
    }

    // Initialize
    async function initialize() {
        try {
            await loadRanks();
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang", "error");
        }
    }

    // Start the application
    initialize();
}); 