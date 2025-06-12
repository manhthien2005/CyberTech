const utils = {
    async fetchData(url, method = 'GET', data = null) {
        try {
            const options = {
                method,
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                credentials: 'same-origin' // Thêm credentials để gửi cookies
            };

            // Xử lý dữ liệu gửi đi
            if (data) {
                if (data instanceof FormData) {
                    // Không cần set Content-Type cho FormData
                    options.body = data;
                } else if (typeof data === 'object') {
                    options.headers['Content-Type'] = 'application/json';
                    options.body = JSON.stringify(data);
                } else {
                    options.body = data;
                }
            }

            // Thêm antiforgery token nếu có
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
            if (token) {
                options.headers['RequestVerificationToken'] = token;
            }

            const response = await fetch(url, options);

            // Xử lý response
            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(errorData?.errorMessage || `HTTP error! status: ${response.status}`);
            }

            // Xử lý response trống
            const contentType = response.headers.get('content-type');
            if (contentType && contentType.includes('application/json')) {
                return await response.json();
            }
            return null;
        } catch (error) {
            console.error('Fetch error:', error);
            throw error;
        }
    },

    getInitials(name) {
        return name?.split(' ').map(word => word[0]).join('').toUpperCase() || '?';
    },

    getEnumText(enumType, value) {
        const enumMaps = {
            UserRole: {
                'Customer': 'Khách hàng',
                'Support': 'Hỗ trợ',
                'Manager': 'Quản lý',
                'SuperAdmin': 'Quản trị viên'
            },
            UserStatus: {
                'Active': 'Hoạt động',
                'Inactive': 'Tạm khóa',
                'Suspended': 'Bị đình chỉ'
            },
            AuthType: {
                'Password': 'Mật khẩu',
                'Google': 'Google',
                'Facebook': 'Facebook'
            },
            DiscountType: {
                'PERCENT': 'Phần trăm',
                'FIXED': 'Cố định'
            },
            OrderStatus: {
                'Pending': 'Đang chờ',
                'Processing': 'Đang xử lý',
                'Shipped': 'Đã giao hàng',
                'Delivered': 'Đã giao',
                'Cancelled': 'Đã hủy'
            },
            PaymentMethod: {
                'COD': 'Thanh toán khi nhận hàng',
                'VNPay': 'VNPay',
                'Momo': 'Momo'
            },
            PaymentStatus: {
                'Pending': 'Đang chờ',
                'Completed': 'Hoàn tất',
                'Failed': 'Thất bại',
                'Refunded': 'Đã hoàn tiền'
            },
            ShippingMethod: {
                'Standard': 'Tiêu chuẩn',
                'Express': 'Nhanh'
            },
            ShippingStatus: {
                'Pending': 'Đang chờ',
                'Shipped': 'Đã giao hàng',
                'InTransit': 'Đang vận chuyển',
                'Delivered': 'Đã giao'
            }
        };

        if (!enumMaps[enumType] || typeof value !== 'string') {
            return 'Không xác định';
        }

        return enumMaps[enumType][value] || 'Không xác định';
    },

    showToast: function(message, type = 'info') {
        const toastContainer = document.getElementById('toast-container') || (() => {
            const container = document.createElement('div');
            container.id = 'toast-container';
            container.style.position = 'fixed';
            container.style.top = '20px';
            container.style.right = '20px';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
            return container;
        })();

        const toast = document.createElement('div');
        toast.className = `toast align-items-center text-white bg-${type} border-0`;
        toast.setAttribute('role', 'alert');
        toast.setAttribute('aria-live', 'assertive');
        toast.setAttribute('aria-atomic', 'true');

        // Thêm icon tương ứng với loại toast
        let icon = '';
        switch(type) {
            case 'success':
                icon = '<i class="fas fa-check-circle me-2"></i>';
                break;
            case 'error':
                icon = '<i class="fas fa-exclamation-circle me-2"></i>';
                break;
            case 'warning':
                icon = '<i class="fas fa-exclamation-triangle me-2"></i>';
                break;
            case 'info':
                icon = '<i class="fas fa-info-circle me-2"></i>';
                break;
        }

        toast.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${icon}${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;

        toastContainer.appendChild(toast);
        const bsToast = new bootstrap.Toast(toast, {
            autohide: true,
            delay: 3000
        });
        bsToast.show();

        toast.addEventListener('hidden.bs.toast', () => {
            toast.remove();
        });
    },

    formatCurrency: function(amount) {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(amount);
    },

    formatDate: function(date) {
        return new Date(date).toLocaleDateString('vi-VN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        });
    },

    validateEmail: function(email) {
        const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return re.test(email);
    },

    validatePhone: function(phone) {
        const re = /^[0-9]{10,11}$/;
        return re.test(phone);
    },

    debounce: function(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    renderPagination({ currentPage, totalPages }, container, itemsContainer, prevButton, nextButton, pageChangeCallback) {
        if (!container || !itemsContainer || totalPages <= 1) {
            container?.classList.add('d-none');
            return;
        }

        container.classList.remove('d-none');
        itemsContainer.innerHTML = '';

        const startPage = Math.max(1, currentPage - 2);
        const endPage = Math.min(totalPages, startPage + 4);

        if (startPage > 1) {
            this.addPageItem(1, itemsContainer, pageChangeCallback, currentPage);
            if (startPage > 2) this.addEllipsis(itemsContainer);
        }

        for (let i = startPage; i <= endPage; i++) {
            this.addPageItem(i, itemsContainer, pageChangeCallback, currentPage);
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) this.addEllipsis(itemsContainer);
            this.addPageItem(totalPages, itemsContainer, pageChangeCallback, currentPage);
        }

        prevButton?.classList.toggle('disabled', currentPage <= 1);
        nextButton?.classList.toggle('disabled', currentPage >= totalPages);
    },

    addPageItem(pageNum, container, callback, currentPage) {
        const li = document.createElement('li');
        li.className = `page-item ${pageNum === currentPage ? 'active' : ''}`;
        
        const a = document.createElement('a');
        a.className = 'page-link';
        a.href = '#';
        a.textContent = pageNum;

        if (pageNum !== currentPage) {
            a.addEventListener('click', e => {
                e.preventDefault();
                callback(pageNum);
            });
        }

        li.appendChild(a);
        container.appendChild(li);
    },

    addEllipsis(container) {
        const li = document.createElement('li');
        li.className = 'page-item disabled';
        li.innerHTML = '<span class="page-link">…</span>';
        container.appendChild(li);
    },

    showLoadingOverlay(show = true) {
        let overlay = document.getElementById('loadingOverlay');
        if (!overlay && show) {
            overlay = document.createElement('div');
            overlay.id = 'loadingOverlay';
            overlay.innerHTML = `
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
            `;
            overlay.style.cssText = 'position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(255,255,255,0.7); display: flex; justify-content: center; align-items: center; z-index: 9999;';
            document.body.appendChild(overlay);
        } else if (overlay && !show) {
            overlay.remove();
        }
    }
};

window.utils = utils;