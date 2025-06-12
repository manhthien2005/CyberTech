const utils = {
    async fetchData(url, method = 'GET', data = null) {
        const options = {
            method,
            headers: { 'Content-Type': 'application/json' }
        };
        if (data) options.body = JSON.stringify(data);
        return (await fetch(url, options)).json();
    },

    getInitials(name) {
        return name?.split(' ').map(word => word[0]).join('').toUpperCase() || '?';
    },

    getStatusText(status) {
        const statusMap = {
            // User statuses
            'Active': 'Hoạt động',
            'Inactive': 'Tạm khóa',
            'Suspended': 'Nghỉ việc',
            
            // Order statuses  
            'Pending': 'Chờ xử lý',
            'Processing': 'Đang xử lý', 
            'Shipped': 'Đang giao hàng',
            'Delivered': 'Đã giao hàng',
            'Cancelled': 'Đã hủy',
            'Returned': 'Đã hoàn trả',

            // Payment statuses
            'Completed': 'Đã thanh toán',
            'Failed': 'Thanh toán thất bại',
            'Refunded': 'Đã hoàn tiền'
        };

        return statusMap[status] || status || 'Không xác định';
    },

    getStatusClass(status) {
        const statusClasses = {
            // User statuses
            'Active': 'success',
            'Inactive': 'warning',
            'Suspended': 'danger',

            // Order statuses
            'Pending': 'warning',
            'Processing': 'info',
            'Shipped': 'primary', 
            'Delivered': 'success',
            'Cancelled': 'danger',
            'Returned': 'secondary',

            // Payment statuses
            'Completed': 'success',
            'Failed': 'danger',
            'Refunded': 'info'
        };

        return statusClasses[status] || 'secondary';
    },

    formatStatus(status) {
        return `<span class="badge bg-${this.getStatusClass(status)}">${this.getStatusText(status)}</span>`;
    },

    showToast(message, type = 'info') {
        const container = document.getElementById('toastContainer') || Object.assign(document.createElement('div'), {
            id: 'toastContainer',
            className: 'toast-container position-fixed bottom-0 end-0 p-3'
        });
        document.body.appendChild(container);

        const toastId = `toast-${Date.now()}`;
        const toastEl = Object.assign(document.createElement('div'), {
            id: toastId,
            className: `toast align-items-center text-white bg-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'primary'} border-0`,
            role: 'alert'
        });

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

        toastEl.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    ${icon}${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;

        container.appendChild(toastEl);
        const toast = new bootstrap.Toast(toastEl, { autohide: true, delay: 3000 });
        toast.show();
        toastEl.addEventListener('hidden.bs.toast', () => toastEl.remove());
    },

    formatMoney(amount, locale = 'vi-VN', currency = 'VND') {
        if (amount > 9999999999) {
            console.warn("Amount exceeds maximum allowed value:", amount);
            amount = 9999999999;
        }
        return new Intl.NumberFormat(locale, { style: 'currency', currency }).format(amount);
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

    // Utility Functions
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