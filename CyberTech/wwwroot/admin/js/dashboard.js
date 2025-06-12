document.addEventListener("DOMContentLoaded", () => {
    const state = {
        dashboardStats: {},
        revenueData: {
            labels: [],
            data: []
        },
        topSellingFoods: [],
        timeRanges: {
            revenue: 'month',
            topFoods: 'day'
        },
        customDateRanges: {
            revenue: {
                startDate: null,
                endDate: null
            },
            topFoods: {
                startDate: null,
                endDate: null
            }
        }
    };

    // API endpoints
    const API = {
        dashboardStats: '/Dashboard/GetDashboardStats',
        revenueData: (timeRange, startDate, endDate) => {
            let url = `/Dashboard/GetRevenueData?timeRange=${timeRange}`;
            if (timeRange === 'custom' && startDate && endDate) {
                url += `&startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`;
            }
            return url;
        },
        topSellingFoods: (timeRange, startDate, endDate) => {
            let url = `/Dashboard/GetTopSellingFoods?timeRange=${timeRange}`;
            if (timeRange === 'custom' && startDate && endDate) {
                url += `&startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`;
            }
            return url;
        }
    };

    // DOM elements
    const DOM = {
        revenueTimeRange: document.getElementById("revenueTimeRange"),
        topFoodTimeRange: document.getElementById("topFoodTimeRange"),
        revenueChart: document.getElementById("revenueChart"),
        topFoodsList: document.querySelector(".top-foods-list"),
        revenueDateRangePicker: document.getElementById("revenueDateRangePicker"),
        revenueCustomDateRange: document.getElementById("revenueCustomDateRange"),
        foodDateRangePicker: document.getElementById("foodDateRangePicker"),
        foodCustomDateRange: document.getElementById("foodCustomDateRange"),
        statCards: {
            revenue: {
                title: document.querySelector('.stat-card.revenue .stat-card-title'),
                change: document.querySelector('.stat-card.revenue .stat-card-change')
            },
            orders: {
                title: document.querySelector('.stat-card.orders .stat-card-title'),
                change: document.querySelector('.stat-card.orders .stat-card-change')
            },
            customers: {
                title: document.querySelector('.stat-card.customers .stat-card-title'),
                change: document.querySelector('.stat-card.customers .stat-card-change')
            },
            tables: {
                title: document.querySelector('.stat-card.tables .stat-card-title'),
                change: document.querySelector('.stat-card.tables .stat-card-change')
            }
        },
        chartContainer: document.querySelector('.chart-container')
    };

    // Initialize
    async function initialize() {
        try {
            initDatePickers();
            await Promise.all([
                loadDashboardStats(),
                initRevenueChart(),
                loadTopSellingFoods(state.timeRanges.topFoods)
            ]);
            setupEventListeners();
        } catch (error) {
            console.error("Initialization error:", error);
            utils.showToast("Đã xảy ra lỗi khi tải dữ liệu", "error");
        }
    }

    // Initialize date pickers
    function initDatePickers() {
        // Revenue date picker
        const revenueDatePicker = flatpickr(DOM.revenueCustomDateRange, {
            mode: "range",
            dateFormat: "d/m/Y",
            locale: "vn",
            maxDate: "today",
            onChange: function(selectedDates) {
                if (selectedDates.length === 2) {
                    state.customDateRanges.revenue.startDate = selectedDates[0];
                    state.customDateRanges.revenue.endDate = selectedDates[1];
                    loadRevenueData('custom', selectedDates[0], selectedDates[1]);
                }
            }
        });

        // Top foods date picker
        const foodDatePicker = flatpickr(DOM.foodCustomDateRange, {
            mode: "range",
            dateFormat: "d/m/Y",
            locale: "vn",
            maxDate: "today",
            onChange: function(selectedDates) {
                if (selectedDates.length === 2) {
                    state.customDateRanges.topFoods.startDate = selectedDates[0];
                    state.customDateRanges.topFoods.endDate = selectedDates[1];
                    loadTopSellingFoods('custom', selectedDates[0], selectedDates[1]);
                }
            }
        });
    }

    // Skeleton loaders
    function showTopFoodsSkeletons() {
        DOM.topFoodsList.innerHTML = Array(5).fill().map(() => `
            <div class="food-item skeleton-item">
                <div class="food-rank skeleton"></div>
                <div class="food-image skeleton"></div>
                <div class="food-content">
                    <div class="food-title skeleton"></div>
                    <div class="food-stats">
                        <div class="food-price skeleton"></div>
                        <div class="food-sold skeleton"></div>
                    </div>
                </div>
            </div>
        `).join('');
    }

    // Data loading
    async function loadDashboardStats() {
        try {
            utils.showLoadingOverlay(true);
            const result = await utils.fetchData(API.dashboardStats);
            state.dashboardStats = result;
            renderDashboardStats(result);
        } catch (error) {
            utils.showToast("Không thể tải dữ liệu thống kê", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function loadRevenueData(timeRange, startDate, endDate) {
        try {
            utils.showLoadingOverlay(true);
            state.timeRanges.revenue = timeRange;
            
            if (timeRange === 'custom') {
                state.customDateRanges.revenue.startDate = startDate || state.customDateRanges.revenue.startDate;
                state.customDateRanges.revenue.endDate = endDate || state.customDateRanges.revenue.endDate;
                startDate = state.customDateRanges.revenue.startDate;
                endDate = state.customDateRanges.revenue.endDate;
            }
            
            const result = await utils.fetchData(API.revenueData(timeRange, startDate, endDate));
            state.revenueData = result;
            updateRevenueChart(result);
        } catch (error) {
            utils.showToast("Không thể tải dữ liệu doanh thu", "error");
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    async function loadTopSellingFoods(timeRange, startDate, endDate) {
        try {
            utils.showLoadingOverlay(true);
            showTopFoodsSkeletons();
            state.timeRanges.topFoods = timeRange;
            
            if (timeRange === 'custom') {
                state.customDateRanges.topFoods.startDate = startDate || state.customDateRanges.topFoods.startDate;
                state.customDateRanges.topFoods.endDate = endDate || state.customDateRanges.topFoods.endDate;
                startDate = state.customDateRanges.topFoods.startDate;
                endDate = state.customDateRanges.topFoods.endDate;
            }
            
            const result = await utils.fetchData(API.topSellingFoods(timeRange, startDate, endDate));
            state.topSellingFoods = result;
            renderTopSellingFoods(result);
        } catch (error) {
            utils.showToast("Không thể tải dữ liệu món ăn bán chạy", "error");
            DOM.topFoodsList.innerHTML = '<div class="text-center py-4">Không thể tải dữ liệu. Vui lòng thử lại sau.</div>';
        } finally {
            utils.showLoadingOverlay(false);
        }
    }

    // Rendering
    function renderDashboardStats(data) {
        // Cập nhật doanh thu tháng
        DOM.statCards.revenue.title.textContent = utils.formatMoney(data.monthlyRevenue);
        
        // Cập nhật phần trăm thay đổi doanh thu
        DOM.statCards.revenue.change.innerHTML = `<i class="fas fa-arrow-${data.revenueChangePercent >= 0 ? 'up' : 'down'}"></i> ${Math.abs(data.revenueChangePercent)}% so với tháng trước`;
        DOM.statCards.revenue.change.className = `stat-card-change ${data.revenueChangePercent >= 0 ? 'positive' : 'negative'}`;
        
        // Cập nhật đơn hàng tháng
        DOM.statCards.orders.title.textContent = data.monthlyOrders.toLocaleString('vi-VN');
        
        // Cập nhật phần trăm thay đổi đơn hàng
        DOM.statCards.orders.change.innerHTML = `<i class="fas fa-arrow-${data.ordersChangePercent >= 0 ? 'up' : 'down'}"></i> ${Math.abs(data.ordersChangePercent)}% so với tháng trước`;
        DOM.statCards.orders.change.className = `stat-card-change ${data.ordersChangePercent >= 0 ? 'positive' : 'negative'}`;
        
        // Cập nhật khách hàng mới
        DOM.statCards.customers.title.textContent = data.newCustomers.toLocaleString('vi-VN');
        
        // Cập nhật phần trăm thay đổi khách hàng mới
        DOM.statCards.customers.change.innerHTML = `<i class="fas fa-arrow-${data.newCustomersChangePercent >= 0 ? 'up' : 'down'}"></i> ${Math.abs(data.newCustomersChangePercent)}% so với tháng trước`;
        DOM.statCards.customers.change.className = `stat-card-change ${data.newCustomersChangePercent >= 0 ? 'positive' : 'negative'}`;
        
        // Cập nhật tỷ lệ sử dụng bàn
        DOM.statCards.tables.title.textContent = data.tableUsagePercent + '%';
        
        // Cập nhật phần trăm thay đổi tỷ lệ sử dụng bàn
        DOM.statCards.tables.change.innerHTML = `<i class="fas fa-arrow-${data.tableUsageChangePercent >= 0 ? 'up' : 'down'}"></i> ${Math.abs(data.tableUsageChangePercent)}% so với tháng trước`;
        DOM.statCards.tables.change.className = `stat-card-change ${data.tableUsageChangePercent >= 0 ? 'positive' : 'negative'}`;
    }

    function renderTopSellingFoods(foods) {
        let foodsHTML = '';
        
        foods.forEach(food => {
            foodsHTML += `
            <div class="food-item">
                <div class="food-rank">${food.rank}</div>
                <div class="food-image">
                    <img src="/images/MenuItem/${food.image}" alt="${food.name}">
                </div>
                <div class="food-content">
                    <h6 class="food-title">${food.name}</h6>
                    <div class="food-stats">
                        <span class="food-price">${utils.formatMoney(food.price)}</span>
                        <span class="food-sold">Đã bán: ${food.soldQuantity}</span>
                    </div>
                </div>
            </div>
            `;
        });
        
        DOM.topFoodsList.innerHTML = foodsHTML || '<div class="text-center py-4">Không có dữ liệu món ăn bán chạy.</div>';
    }

    // Event listeners
    function setupEventListeners() {
        DOM.revenueTimeRange?.addEventListener("change", function() {
            const selectedValue = this.value;
            if (selectedValue === 'custom') {
                DOM.revenueDateRangePicker.style.display = 'block';
            } else {
                DOM.revenueDateRangePicker.style.display = 'none';
                loadRevenueData(selectedValue);
            }
        });
        
        DOM.topFoodTimeRange?.addEventListener("change", function() {
            const selectedValue = this.value;
            if (selectedValue === 'custom') {
                DOM.foodDateRangePicker.style.display = 'block';
            } else {
                DOM.foodDateRangePicker.style.display = 'none';
                loadTopSellingFoods(selectedValue);
            }
        });
    }

    // Chart functions
    function initRevenueChart() {
        const ctx = DOM.revenueChart.getContext('2d');
        
        // Cấu hình biểu đồ
        const config = {
            type: 'line',
            data: {
                labels: [],
                datasets: [{
                    label: 'Doanh thu',
                    data: [],
                    backgroundColor: 'rgba(40, 167, 69, 0.2)',
                    borderColor: '#28a745',
                    borderWidth: 2,
                    pointBackgroundColor: '#28a745',
                    pointBorderColor: '#fff',
                    pointBorderWidth: 2,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    tension: 0.3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.7)',
                        padding: 10,
                        titleFont: {
                            size: 14,
                            weight: 'bold'
                        },
                        bodyFont: {
                            size: 13
                        },
                        callbacks: {
                            label: function(context) {
                                return utils.formatMoney(context.parsed.y);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: {
                            display: false
                        }
                    },
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function(value) {
                                return utils.formatMoney(value);
                            }
                        }
                    }
                }
            }
        };
        
        // Tạo biểu đồ
        window.revenueChart = new Chart(ctx, config);
        
        // Tải dữ liệu ban đầu
        return loadRevenueData(state.timeRanges.revenue);
    }

    function updateRevenueChart(data) {
        window.revenueChart.data.labels = data.labels;
        window.revenueChart.data.datasets[0].data = data.data;
        window.revenueChart.update();
        
        // Tính tổng doanh thu
        const totalRevenue = data.data.reduce((sum, value) => sum + value, 0);
        
        // Hiển thị tổng doanh thu
        const totalRevenueElement = document.getElementById('totalRevenue');
        if (totalRevenueElement) {
            totalRevenueElement.textContent = utils.formatMoney(totalRevenue);
        }
        
        // Cập nhật tiêu đề biểu đồ theo khoảng thời gian
        let timeRangeText = getTimeRangeText(state.timeRanges.revenue);
        
        // Nếu là khoảng thời gian tùy chỉnh, hiển thị ngày bắt đầu và kết thúc
        if (state.timeRanges.revenue === 'custom' && state.customDateRanges.revenue.startDate && state.customDateRanges.revenue.endDate) {
            const startDate = state.customDateRanges.revenue.startDate.toLocaleDateString('vi-VN');
            const endDate = state.customDateRanges.revenue.endDate.toLocaleDateString('vi-VN');
            timeRangeText = `từ ${startDate} đến ${endDate}`;
        }
        
        const chartTitle = document.querySelector('.card-header h5');
        if (chartTitle) {
            chartTitle.textContent = `Doanh thu ${timeRangeText}`;
        }
    }
    
    // Hàm hỗ trợ để lấy text mô tả khoảng thời gian
    function getTimeRangeText(timeRange) {
        switch(timeRange) {
            case 'day': return 'hôm nay';
            case 'week': return 'tuần này';
            case 'month': return 'tháng này';
            case 'quarter': return 'quý này';
            case 'year': return 'năm nay';
            case 'custom': return 'tùy chỉnh';
            default: return 'tháng này';
        }
    }

    // Khởi chạy ứng dụng
    initialize();
});