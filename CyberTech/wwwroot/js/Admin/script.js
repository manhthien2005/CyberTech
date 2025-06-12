class RestaurantManager {
    constructor() {
        this.elements = {
            sidebar: document.querySelector('.sidebar'),
            mainContent: document.querySelector('.main-content'),
            toggleSidebar: document.querySelector('.toggle-sidebar'),
            mobileOverlay: document.querySelector('.mobile-overlay'),
            tableItems: document.querySelectorAll('.table-item'),
            dropdownToggles: document.querySelectorAll('.dropdown-toggle'),
            passwordToggles: document.querySelectorAll('.password-toggle'),
            userAvatar: document.querySelector('.user-avatar'),
            userInfo: document.querySelector('.user-info')
        };

        this.isMobile = () => window.innerWidth < 992;
        this.bindEvents();
    }

    bindEvents() {
        this.elements.toggleSidebar.addEventListener('click', () => this.toggleSidebar());
        this.elements.mobileOverlay.addEventListener('click', () => this.closeMobileSidebar());
        window.addEventListener('resize', () => this.handleResize());

        this.elements.tableItems.forEach(item => {
            item.addEventListener('click', () => this.selectTable(item));
        });

        this.elements.dropdownToggles.forEach(toggle => {
            toggle.addEventListener('click', e => {
                e.stopPropagation();
                this.toggleDropdown(toggle);
            });
        });

        document.addEventListener('click', e => this.closeDropdowns(e));

        this.elements.passwordToggles.forEach(toggle => {
            toggle.addEventListener('click', () => this.togglePassword(toggle));
        });
        
        this.elements.userAvatar?.addEventListener('click', (e) => {
            console.log('User profile clicked');
            window.location.href = '/admin/userProfile';
        });
        this.elements.userInfo?.addEventListener('click', (e) => {
            console.log('User profile clicked');
            window.location.href = '/admin/userProfile';
        });
    }

    toggleSidebar() {
        if (this.isMobile()) {
            this.elements.sidebar.classList.toggle('mobile-active');
            this.elements.mobileOverlay.classList.toggle('active');
        } else {
            this.elements.sidebar.classList.toggle('collapsed');
            this.elements.mainContent.classList.toggle('expanded');
        }
    }

    closeMobileSidebar() {
        this.elements.sidebar.classList.remove('mobile-active');
        this.elements.mobileOverlay.classList.remove('active');
    }

    handleResize() {
        if (this.isMobile()) {
            this.elements.sidebar.classList.remove('collapsed');
            this.elements.mainContent.classList.remove('expanded');
        } else {
            this.elements.sidebar.classList.remove('mobile-active');
            this.elements.mobileOverlay.classList.remove('active');
        }
    }

    selectTable(table) {
        if (!table.classList.contains('active')) {
            this.elements.tableItems.forEach(item => item.classList.remove('active'));
            table.classList.add('active');
        }
    }

    toggleDropdown(toggle) {
        const menu = toggle.nextElementSibling;
        menu.classList.toggle('show');
    }

    closeDropdowns(event) {
        if (!event.target.closest('.dropdown')) {
            document.querySelectorAll('.dropdown-menu.show')
                .forEach(menu => menu.classList.remove('show'));
        }
    }

    togglePassword(toggle) {
        const container = toggle.closest('.position-relative');
        const input = container.querySelector('input');
        const icon = toggle.querySelector('i');

        input.type = input.type === 'password' ? 'text' : 'password';
        icon.classList.toggle('fa-eye');
        icon.classList.toggle('fa-eye-slash');
    }
}

document.addEventListener('DOMContentLoaded', () => new RestaurantManager());