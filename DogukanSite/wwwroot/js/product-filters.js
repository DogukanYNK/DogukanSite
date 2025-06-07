// wwwroot/js/product-filters.js

function initializeProductFilters() {
    // Bu kontrol, scriptlerin sadece ürün listeleme sayfasında çalışmasını sağlar.
    if ($('#productListContainer').length === 0) return;

    console.log("DEBUG: Product Filters (AJAX Listing) başlatılıyor.");

    var isLoading = false;

    function getCurrentFilters() {
        return {
            searchTerm: $('#filterSearchTerm_full').val() || '',
            category: $('.category-filter-list a.active').data('category') || '',
            sortBy: $('.ajax-sort-select').val() || 'newest',
            currentPage: 1 // Bu, sayfalama tarafından güncellenecek
        };
    }

    function loadProducts(pageNumber, updateHistory = true) {
        if (isLoading) return;
        isLoading = true;

        $('#productListContainer').html('<div class="text-center py-5"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Yükleniyor...</span></div></div>');

        var filters = getCurrentFilters();
        filters.currentPage = parseInt(pageNumber) || 1;

        var requestData = {
            handler: "LoadProductsPartial",
            ...filters
        };

        $.ajax({
            url: '/Products/Index',
            type: 'GET',
            data: requestData,
            success: function (responseHtml) {
                $('#productListContainer').html(responseHtml);
                if (updateHistory) {
                    var newUrl = `/Products/Index?SearchTerm=${encodeURIComponent(filters.searchTerm)}&Category=${encodeURIComponent(filters.category)}&SortBy=${encodeURIComponent(filters.sortBy)}&CurrentPage=${encodeURIComponent(filters.currentPage)}`;
                    history.pushState({ path: newUrl }, '', newUrl);
                }
            },
            error: function () {
                $('#productListContainer').html('<div class="alert alert-danger text-center">Ürünler yüklenirken bir hata oluştu.</div>');
            },
            complete: function () {
                isLoading = false;
            }
        });
    }

    // Olay Dinleyicileri (Event Listeners)
    $(document).on('click', '.ajax-filter-link', function (e) {
        e.preventDefault();
        $('.ajax-filter-link.active').removeClass('active');
        $(this).addClass('active');
        loadProducts(1);
    });

    $(document).on('change', '.ajax-sort-select', function () {
        loadProducts(1);
    });

    $(document).on('submit', '#productFilterForm', function (e) {
        e.preventDefault();
        loadProducts(1);
    });

    $(document).on('click', '#productListContainer .pagination a', function (e) {
        e.preventDefault();
        var page = $(this).attr('href').split('CurrentPage=')[1];
        loadProducts(page);
    });

    $(window).on('popstate', function () {
        location.reload(); // En basit geri/ileri tuşu yönetimi
    });
}