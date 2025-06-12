document.addEventListener("DOMContentLoaded", () => {
    // State Management
    const state = {
        employees: [],
        pagination: {
            currentPage: 1,
            pageSize: 10,
            totalItems: 0,
            totalPages: 0
        },
        searchTerm: "",
        sortFilter: "date_desc",
        statusFilter: "",
        selectedEmployee: null,
        currentUserId: null // To store current user ID
    };

    // Validation Functions
    function isValidPhoneNumber(phone) {
        if (!phone) return true; // Phone is optional
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
        employees: (page, pageSize, searchTerm, sortFilter, statusFilter) =>
            `/EmployeeManage/GetEmployees?page=${page}&pageSize=${pageSize}` +
            (searchTerm ? `&searchTerm=${encodeURIComponent(searchTerm)}` : '') +
            (sortFilter ? `&sortBy=${sortFilter}` : '') +
            (statusFilter ? `&status=${statusFilter}` : ''),
        employee: (id) => `/EmployeeManage/GetEmployee/${id}`,
        createEmployee: '/EmployeeManage/CreateEmployee',
        updateEmployee: '/EmployeeManage/UpdateEmployee'
    };

    // DOM Elements
    const DOM = {
        employeeTableBody: document.getElementById("employeeTableBody"),
        employeeSearchInput: document.getElementById("employeeSearchInput"),
        saveEmployeeBtn: document.getElementById("saveEmployeeBtn"),
        updateEmployeeBtn: document.getElementById("updateEmployeeBtn"),
        paginationContainer: document.getElementById("employeePagination"),
        paginationPrev: document.getElementById("paginationPrev"),
        paginationNext: document.getElementById("paginationNext"),
        paginationItems: document.getElementById("paginationItems"),
        sortFilter: document.getElementById("sortFilter"),
        statusFilter: document.getElementById("statusFilter"),
        confirmDeleteEmployeeBtn: document.getElementById("confirmDeleteEmployeeBtn"),
        employeeJoinDate: document.getElementById("employeeJoinDate"),
        authMethodsList: document.getElementById("authMethodsList")
    };

    // Skeleton Loaders
    function showEmployeeSkeletons() {
        DOM.employeeTableBody.innerHTML = Array(5).fill().map(() => `
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
                <td><div class="skeleton" style="width: 80px; height: 18px;"></div></td>
                <td><div class="skeleton" style="width: 60px; height: 24px; border-radius: 4px;"></div></td>
                <td>
                    <div class="d-flex">
                        <div class="skeleton" style="width: 32px; height: 32px; margin-right: 5px; border-radius: 4px;"></div>
                        <div class="skeleton" style="width: 32px; height: 32px; border-radius: 4px;"></div>
                    </div>
                </td>
            </tr>
        `).join('');
    }

    // Helper Functions
    function getRoleColor(role) {
        switch (role) {
            case "Support": return "#6c757d";
            case "Manager": return "#0d6efd";
            case "SuperAdmin": return "#dc3545";
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

    function getRoleText(role) {
        switch (role) {
            case "Support": return "Hỗ trợ";
            case "Manager": return "Quản lý";
            case "SuperAdmin": return "Quản trị viên cấp cao";
            default: return role;
        }
    }

    function getAuthMethodClass(authType) {
        switch (authType) {
            case "Password": return "auth-password";
            case "Google": return "auth-google";
            case "Facebook": return "auth-facebook";
            default: return "auth-password";
        }
    }

    function getAuthMethodText(authType) {
        switch (authType) {
            case "Password": return "Mật khẩu";
            case "Google": return "Google";
            case "Facebook": return "Facebook";
            default: return authType;
        }
    }

    function formatDate(dateString) {
        if (!dateString) return "";
        const date = new Date(dateString);
        return date.toLocaleDateString('vi-VN');
    }

    function resetAddForm() {
        const form = document.getElementById("addEmployeeForm");
        form.reset();
    }

    function resetEditForm() {
        const form = document.getElementById("editEmployeeForm");
        if (form) form.reset();
        if (DOM.authMethodsList) {
            DOM.authMethodsList.innerHTML = '<p class="text-center text-muted">Chưa có phương thức đăng nhập nào</p>';
        }
        state.selectedEmployee = null;
    }

    // Fill employee edit form
    function fillEmployeeEditForm(employee) {
        const editUserId = document.getElementById("editUserId");
        const editName = document.getElementById("editName");
        const editEmail = document.getElementById("editEmail");
        const editUsername = document.getElementById("editUsername");
        const editPhone = document.getElementById("editPhone");
        const editStatus = document.getElementById("editStatus");
        const editDateOfBirth = document.getElementById("editDateOfBirth");
        const editRole = document.getElementById("editRole");
        const editSalary = document.getElementById("editSalary");
        
        if (editUserId) editUserId.value = employee.userId;
        if (editName) editName.value = employee.name;
        if (editEmail) editEmail.value = employee.email;
        if (editUsername) editUsername.value = employee.username;
        if (editPhone) editPhone.value = employee.phone || '';
        if (editStatus) editStatus.value = employee.status;
        if (editDateOfBirth) editDateOfBirth.value = employee.dateOfBirth ? new Date(employee.dateOfBirth).toISOString().split('T')[0] : '';
        if (editRole) editRole.value = employee.role;
        if (editSalary) editSalary.value = employee.salary || '';
        
        // Gender radio buttons
        document.querySelectorAll('input[name="gender"]').forEach(radio => {
            if (employee.gender === null) {
                if (radio.value === "") radio.checked = true;
            } else {
                if (parseInt(radio.value) === employee.gender) radio.checked = true;
            }
        });
        
        // Additional info
        if (DOM.employeeJoinDate) {
            DOM.employeeJoinDate.innerHTML = `<div class="fw-bold">${formatDate(employee.createdAt)}</div>`;
        }
        
        // Render authentication methods
        renderAuthMethods(employee.authMethods);
    }

    // Render authentication methods
    function renderAuthMethods(authMethods) {
        if (!DOM.authMethodsList) return;
        
        if (!authMethods || authMethods.length === 0) {
            DOM.authMethodsList.innerHTML = '<p class="text-center text-muted">Chưa có phương thức đăng nhập nào</p>';
            return;
        }

        DOM.authMethodsList.innerHTML = authMethods.map(authType => `
            <span class="auth-method-badge ${getAuthMethodClass(authType)}">
                ${getAuthMethodText(authType)}
            </span>
        `).join('');
    }

    // CRUD Operations
    async function createEmployee() {
        const form = document.getElementById("addEmployeeForm");
        const phoneInput = document.getElementById("phone");
        
        if (!form.checkValidity() || !validatePhoneInput(phoneInput)) {
            form.reportValidity();
            return;
        }

        const employee = {
            Name: document.getElementById("name").value,
            Email: document.getElementById("email").value,
            Username: document.getElementById("username").value,
            Password: document.getElementById("password").value,
            Phone: phoneInput.value.trim(),
            Role: document.getElementById("role").value,
            Salary: parseFloat(document.getElementById("salary").value) || null
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.createEmployee, 'POST', employee);

            if (result.success) {
                utils.showToast("Thêm nhân viên thành công", "success");
                await loadEmployees();
                resetAddForm();
                bootstrap.Modal.getInstance(document.getElementById('addEmployeeModal')).hide();
            } else {
                utils.showToast(result.message || "Lỗi khi thêm nhân viên", "error");
            }
        } catch (error) {
            utils.showToast("Lỗi khi thêm nhân viên", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function updateEmployee() {
        const form = document.getElementById("editEmployeeForm");
        const phoneInput = document.getElementById("editPhone");
        
        if (!form.checkValidity() || !validatePhoneInput(phoneInput)) {
            form.reportValidity();
            return;
        }

        const userId = parseInt(document.getElementById("editUserId").value);
        if (userId === state.currentUserId && document.getElementById("editStatus").value !== "Active") {
            utils.showToast("Không thể vô hiệu hóa hoặc tạm khóa tài khoản của chính bạn", "error");
            return;
        }

        const gender = document.querySelector('input[name="gender"]:checked');
        const genderValue = gender ? (gender.value === "" ? null : parseInt(gender.value)) : null;

        const employee = {
            UserId: userId,
            Name: document.getElementById("editName").value,
            Phone: phoneInput.value.trim(),
            Gender: genderValue,
            DateOfBirth: document.getElementById("editDateOfBirth").value || null,
            Status: document.getElementById("editStatus").value,
            Role: document.getElementById("editRole").value,
            Salary: parseFloat(document.getElementById("editSalary").value) || null
        };

        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.updateEmployee, 'POST', employee);

            if (result.success) {
                utils.showToast("Cập nhật thông tin nhân viên thành công", "success");
                await loadEmployees();
            } else {
                utils.showToast(result.message || "Lỗi khi cập nhật thông tin nhân viên", "error");
            }
        } catch (error) {
            utils.showToast("Lỗi khi cập nhật thông tin nhân viên", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function deleteEmployee(userId, userName) {
        if (userId === state.currentUserId) {
            utils.showToast("Không thể thay đổi trạng thái tài khoản của chính bạn", "error");
            return;
        }

        // Get current status
        const employee = state.employees.find(e => e.userId === userId);
        if (!employee) return;
        
        const modalContent = document.getElementById("deleteEmployeeModal");
        const modalBody = modalContent.querySelector(".modal-body");
        const confirmButton = document.getElementById("confirmDeleteEmployeeBtn");
        const modalFooter = modalContent.querySelector(".modal-footer");
        
        // Clear any previous extra buttons
        const extraButton = document.getElementById("suspendEmployeeBtn");
        if (extraButton) extraButton.remove();
        
        document.getElementById("deleteEmployeeName").textContent = userName;
        document.getElementById("deleteEmployeeId").value = userId;
        
        if (employee.status === "Active") {
            modalBody.innerHTML = `
                <p>Bạn muốn thực hiện hành động nào với tài khoản của nhân viên <span id="deleteEmployeeName" class="fw-bold">${userName}</span>?</p>
                <input type="hidden" id="deleteEmployeeId" value="${userId}">
                <input type="hidden" id="actionType" value="deactivate">
            `;
            confirmButton.textContent = "Vô hiệu hóa";
            confirmButton.className = "btn btn-danger";
            
            // Add suspend button
            const suspendBtn = document.createElement("button");
            suspendBtn.type = "button";
            suspendBtn.className = "btn btn-warning";
            suspendBtn.id = "suspendEmployeeBtn";
            suspendBtn.textContent = "Tạm khóa";
            suspendBtn.addEventListener("click", function() {
                document.getElementById("actionType").value = "suspend";
                DOM.confirmDeleteEmployeeBtn.click();
            });
            
            // Insert before the confirm button
            modalFooter.insertBefore(suspendBtn, confirmButton);
        } else if (employee.status === "Inactive") {
            modalBody.innerHTML = `
                <p>Bạn có chắc chắn muốn kích hoạt lại tài khoản của nhân viên <span id="deleteEmployeeName" class="fw-bold">${userName}</span>?</p>
                <p class="text-success">Tài khoản sẽ được kích hoạt và nhân viên có thể đăng nhập bình thường.</p>
                <input type="hidden" id="deleteEmployeeId" value="${userId}">
                <input type="hidden" id="actionType" value="activate">
            `;
            confirmButton.textContent = "Kích hoạt";
            confirmButton.className = "btn btn-success";
        } else if (employee.status === "Suspended") {
            modalBody.innerHTML = `
                <p>Bạn có chắc chắn muốn mở khóa tài khoản của nhân viên <span id="deleteEmployeeName" class="fw-bold">${userName}</span>?</p>
                <p class="text-warning">Tài khoản sẽ được mở khóa và chuyển về trạng thái hoạt động bình thường.</p>
                <input type="hidden" id="deleteEmployeeId" value="${userId}">
                <input type="hidden" id="actionType" value="activate">
            `;
            confirmButton.textContent = "Mở khóa";
            confirmButton.className = "btn btn-warning";
        }
        
        const deleteModal = new bootstrap.Modal(document.getElementById("deleteEmployeeModal"));
        deleteModal.show();
    }

    // Data Loading
    async function loadEmployees(page = 1, searchTerm = state.searchTerm, sortFilter = state.sortFilter, statusFilter = state.statusFilter) {
        try {
            showEmployeeSkeletons();

            state.pagination.currentPage = page;
            state.searchTerm = searchTerm;
            state.sortFilter = sortFilter;
            state.statusFilter = statusFilter;

            const result = await utils.fetchData(API.employees(page, state.pagination.pageSize, searchTerm, sortFilter, statusFilter));

            if (result.success) {
                state.employees = result.data;
                state.pagination = result.pagination;
                renderEmployees();
                renderPagination();
            } else {
                utils.showToast(result.message || "Không thể tải danh sách nhân viên", "error");
            }
        } catch (error) {
            console.error('Load employees error:', error);
            utils.showToast("Không thể tải danh sách nhân viên", "error");
        }
    }

    // Rendering Functions
    function renderEmployees() {
        DOM.employeeTableBody.innerHTML = state.employees.length === 0
            ? `<tr><td colspan="7" class="text-center py-4">Không tìm thấy nhân viên</td></tr>`
            : state.employees.map(employee => {
                // Determine button style and icon based on status
                let btnClass, iconClass;
                switch (employee.status) {
                    case "Active":
                        btnClass = "btn-outline-danger";
                        iconClass = "fa-ban";
                        break;
                    case "Inactive":
                        btnClass = "btn-outline-success";
                        iconClass = "fa-check-circle";
                        break;
                    case "Suspended":
                        btnClass = "btn-outline-warning";
                        iconClass = "fa-unlock";
                        break;
                    default:
                        btnClass = "btn-outline-danger";
                        iconClass = "fa-ban";
                }
                
                return `
                <tr>
                    <td>
                        <div class="d-flex align-items-center">
                            <div class="employee-avatar">
                                ${employee.profileImageURL 
                                    ? `<img src="${employee.profileImageURL}" alt="${employee.name}" />`
                                    : `<i class="fas fa-user"></i>`
                                }
                            </div>
                            <div>
                                <div class="fw-bold">${employee.name}</div>
                                <div class="text-muted small">${employee.username}</div>
                            </div>
                        </div>
                    </td>
                    <td>${employee.email}</td>
                    <td>${employee.phone || '<span class="text-muted">Không có</span>'}</td>
                    <td>
                        <span class="role-badge" style="background-color: ${getRoleColor(employee.role)}">
                            ${getRoleText(employee.role)}
                        </span>
                    </td>
                    <td>${employee.salary ? utils.formatMoney(employee.salary) : '<span class="text-muted">Chưa thiết lập</span>'}</td>
                    <td>
                        <span class="status-badge ${getStatusClass(employee.status)}">
                            ${getStatusText(employee.status)}
                        </span>
                    </td>
                    <td>
                        <div class="btn-group" role="group">
                            <button type="button" class="btn btn-sm btn-outline-primary edit-employee-btn" 
                                data-user-id="${employee.userId}" data-bs-toggle="modal" data-bs-target="#editEmployeeModal">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button type="button" class="btn btn-sm ${btnClass} delete-employee-btn" 
                                data-user-id="${employee.userId}" data-user-name="${employee.name}">
                                <i class="fas ${iconClass}"></i>
                            </button>
                        </div>
                    </td>
                </tr>
            `}).join('');
    }

    function renderPagination() {
        utils.renderPagination(
            state.pagination,
            DOM.paginationContainer,
            DOM.paginationItems,
            DOM.paginationPrev,
            DOM.paginationNext,
            (pageNum) => loadEmployees(pageNum, state.searchTerm, state.sortFilter, state.statusFilter)
        );
    }

    // Event Listeners
    function setupEventListeners() {
        // Search Input
        let searchTimeout;
        DOM.employeeSearchInput?.addEventListener("input", function () {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => loadEmployees(1, this.value.trim(), state.sortFilter, state.statusFilter), 300);
        });

        // Sort Filter
        DOM.sortFilter?.addEventListener("change", function () {
            loadEmployees(1, state.searchTerm, this.value, state.statusFilter);
        });

        // Status Filter
        DOM.statusFilter?.addEventListener("change", function () {
            loadEmployees(1, state.searchTerm, state.sortFilter, this.value);
        });

        // Save Employee Button
        DOM.saveEmployeeBtn?.addEventListener("click", createEmployee);

        // Update Employee Button
        DOM.updateEmployeeBtn?.addEventListener("click", updateEmployee);

        // Delete Employee Button
        DOM.confirmDeleteEmployeeBtn?.addEventListener("click", async function () {
            const userId = parseInt(document.getElementById("deleteEmployeeId").value);
            if (!userId) return;

            if (userId === state.currentUserId) {
                utils.showToast("Không thể thay đổi trạng thái tài khoản của chính bạn", "error");
                bootstrap.Modal.getInstance(document.getElementById('deleteEmployeeModal')).hide();
                return;
            }

            try {
                utils.showLoadingOverlay(true);
                const employee = state.employees.find(e => e.userId === userId);
                const actionType = document.getElementById("actionType")?.value || "deactivate";
                
                let newStatus;
                switch (actionType) {
                    case "deactivate":
                        newStatus = "Inactive";
                        break;
                    case "activate":
                        newStatus = "Active";
                        break;
                    case "suspend":
                        newStatus = "Suspended";
                        break;
                    default:
                        newStatus = "Inactive";
                }
                
                const updateData = {
                    UserId: userId,
                    Name: employee.name,
                    Phone: employee.phone,
                    Gender: employee.gender,
                    DateOfBirth: employee.dateOfBirth,
                    Role: employee.role,
                    Salary: employee.salary,
                    Status: newStatus
                };
                
                const result = await utils.fetchData(API.updateEmployee, 'POST', updateData);

                if (result.success) {
                    let message = "";
                    switch (newStatus) {
                        case "Active":
                            message = "Kích hoạt tài khoản thành công";
                            break;
                        case "Inactive":
                            message = "Vô hiệu hóa tài khoản thành công";
                            break;
                        case "Suspended":
                            message = "Tạm khóa tài khoản thành công";
                            break;
                    }
                    utils.showToast(message, "success");
                    await loadEmployees();
                    bootstrap.Modal.getInstance(document.getElementById('deleteEmployeeModal')).hide();
                } else {
                    utils.showToast(result.message || "Lỗi khi cập nhật trạng thái tài khoản", "error");
                }
            } catch (error) {
                utils.showToast("Lỗi khi cập nhật trạng thái tài khoản", "error");
            } finally {
                utils.showLoadingOverlay(false);
            }
        });

        // Table Row Actions
        document.addEventListener("click", async (e) => {
            // Edit Employee Button
            const editBtn = e.target.closest(".edit-employee-btn");
            if (editBtn) {
                const userId = parseInt(editBtn.getAttribute("data-user-id"));
                if (!userId) return;
                
                try {
                    utils.showLoadingOverlay(true);
                    const result = await utils.fetchData(API.employee(userId));
                    if (result.success) {
                        state.selectedEmployee = result.data;
                        fillEmployeeEditForm(result.data);
                    } else {
                        utils.showToast(result.message || "Không thể tải thông tin nhân viên", "error");
                    }
                } catch (error) {
                    utils.showToast("Không thể tải thông tin nhân viên", "error");
                } finally {
                    utils.showLoadingOverlay(false);
                }
            }

            // Delete Employee Button
            const deleteBtn = e.target.closest(".delete-employee-btn");
            if (deleteBtn) {
                const userId = parseInt(deleteBtn.getAttribute("data-user-id"));
                const userName = deleteBtn.getAttribute("data-user-name");
                if (!userId || !userName) return;
                
                deleteEmployee(userId, userName);
            }
        });

        // Modal Events
        document.getElementById('editEmployeeModal')?.addEventListener('hidden.bs.modal', resetEditForm);
        document.getElementById('addEmployeeModal')?.addEventListener('hidden.bs.modal', resetAddForm);

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
            // Fetch current user ID (you may need to implement this endpoint or use a different method)
            // For now, we'll assume it's available via a meta tag or similar
            const currentUserIdMeta = document.querySelector('meta[name="current-user-id"]');
            state.currentUserId = currentUserIdMeta ? parseInt(currentUserIdMeta.content) : null;

            await loadEmployees();
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi khởi tạo trang", "error");
        }
    }

    // Start the application
    initialize();
});