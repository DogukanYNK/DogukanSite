$(document).ready(function () {
    const sidebar = document.getElementById('adminSidebar');
    const overlay = document.querySelector('.sidebar-overlay');

    // Mobil menüyü açma/kapama butonu
    $(document).on('click', '[data-trigger="sidebar-toggle"]', function () {
        sidebar.classList.toggle('is-open');
    });

    // Arka plana tıklayınca menüyü kapat
    if (overlay) {
        overlay.addEventListener('click', function () {
            sidebar.classList.remove('is-open');
        });
    }
});