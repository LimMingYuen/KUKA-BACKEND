// Dashboard sidebar functionality
(function () {
    'use strict';

    // Sidebar toggle functionality
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.querySelector('.sidebar');
    const toggleIcon = sidebarToggle?.querySelector('.toggle-icon');

    if (sidebarToggle && sidebar && toggleIcon) {
        // Load saved state from localStorage
        const savedState = localStorage.getItem('sidebarCollapsed');
        if (savedState === 'true') {
            sidebar.classList.add('collapsed');
            toggleIcon.innerHTML = '&#9776;'; // Hamburger icon
        } else {
            toggleIcon.innerHTML = '&#9776;'; // Hamburger icon (same for both states)
        }

        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('collapsed');

            // Save state to localStorage
            const isCollapsed = sidebar.classList.contains('collapsed');
            localStorage.setItem('sidebarCollapsed', isCollapsed);

            // Icon stays as hamburger menu in both states
            toggleIcon.innerHTML = '&#9776;';
        });
    }

    // Main menu toggle functionality
    const mainMenuToggle = document.getElementById('mainMenuToggle');
    const mainSubmenu = document.getElementById('mainSubmenu');

    if (mainMenuToggle && mainSubmenu) {
        // Show submenu by default
        mainSubmenu.classList.add('show');
        mainMenuToggle.classList.add('expanded');

        mainMenuToggle.addEventListener('click', function (e) {
            e.preventDefault();
            mainSubmenu.classList.toggle('show');
            mainMenuToggle.classList.toggle('expanded');
        });
    }

    // Handle other menu items with submenus
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(function (link) {
        link.addEventListener('click', function (e) {
            // Only handle links that have a submenu (except main menu which is handled above)
            const submenu = this.nextElementSibling;
            if (submenu && submenu.classList.contains('submenu') && this.id !== 'mainMenuToggle') {
                e.preventDefault();
                submenu.classList.toggle('show');
                this.classList.toggle('expanded');
            }
        });
    });

    // Highlight active page in navigation
    const currentPath = window.location.pathname;
    const submenuLinks = document.querySelectorAll('.submenu-link');
    submenuLinks.forEach(function (link) {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });
})();
