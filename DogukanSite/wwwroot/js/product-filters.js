$(document).ready(function () {
    const productListContainer = $('#productListContainer');
    if (productListContainer.length === 0) return;

    let isLoading = false;

    // Herhangi bir filtreleme işlemi için o anki tüm filtreleri UI'dan toplayan ana fonksiyon
    function getCurrentFilters() {
        return {
            // Arama kutusunun değerini al
            SearchTerm: $('#filterSearchTerm').val() || '',
            // Aktif olan kategori linkinin data-category değerini al
            Category: $('.category-link.active').data('category') || '',
            // Sıralama menüsünün seçili değerini al
            SortBy: $('.ajax-sort-select').val() || 'newest',
            // Sayfa numarasını şimdilik 1 olarak ayarla, sayfalama bunu güncelleyecek
            Page: 1
        };
    }

    // Sayfa başlığını ve alt başlığı güncelleyen fonksiyon
    function updatePageHeader(filters) {
        let pageTitle = "Tüm Ürünler";
        let pageSubTitle = "";

        if (filters.Category) {
            pageTitle = filters.Category;
            pageSubTitle = "kategorisindeki ürünler listeleniyor.";
        } else if (filters.SearchTerm) {
            pageTitle = `"${filters.SearchTerm}"`;
            pageSubTitle = "için arama sonuçları.";
        }

        $('.page-header .page-title').text(pageTitle);
        if (pageSubTitle) {
            $('.page-header .lead').text(pageSubTitle).show();
        } else {
            $('.page-header .lead').hide();
        }
    }

    // Favori ikonlarını güncelleyen fonksiyon
    function updateFavoriteIcons() {
        const favoritesData = $('#userFavoritesData');
        if (!favoritesData.length) return;
        const favoriteIds = (favoritesData.data('favorite-ids') || '').toString().split(',');
        if (favoriteIds.length === 0 || favoriteIds[0] === '') return;

        $('.favorite-icon').each(function () {
            const $button = $(this);
            const productId = $button.data('product-id').toString();
            const $icon = $button.find('i');
            if (favoriteIds.includes(productId)) {
                $icon.removeClass('far').addClass('fas text-danger');
            } else {
                $icon.removeClass('fas text-danger').addClass('far');
            }
        });
    }

    // Ürünleri AJAX ile yükleyen ana fonksiyon
    function loadProducts(filters, updateHistory = true) {
        if (isLoading) return;
        isLoading = true;

        productListContainer.html('<div class="d-flex justify-content-center py-5"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Yükleniyor...</span></div></div>');

        const url = `/Products/Index?handler=LoadProductsPartial&${$.param(filters)}`;

        $.get(url)
            .done(function (responseHtml) {
                productListContainer.html(responseHtml);
                if (updateHistory) {
                    const newUrl = `/Products?${$.param(filters)}`;
                    history.pushState(filters, '', newUrl);
                }
                updatePageHeader(filters);
                const totalCount = productListContainer.find('.product-list-partial').data('total-count') || 0;
                $('#productCountInfoDesktop .text-muted').text(`${totalCount} ürün bulundu`);
            })
            .fail(function () {
                productListContainer.html('<div class="alert alert-danger">Ürünler yüklenirken bir hata oluştu.</div>');
            })
            .always(function () {
                isLoading = false;
            });
    }

    // Olay Dinleyicileri (Event Listeners)

    // Arama Formu Gönderildiğinde
    $('#productFilterForm').on('submit', function (e) {
        e.preventDefault();
        loadProducts(getCurrentFilters());
    });

    // Kategori Linkine Tıklandığında
    $(document).on('click', '.category-link', function (e) {
        e.preventDefault();
        $('.category-link.active').removeClass('active');
        $(this).addClass('active');
        var offcanvas = bootstrap.Offcanvas.getInstance(document.getElementById('productFiltersOffcanvas'));
        if (offcanvas) offcanvas.hide();
        loadProducts(getCurrentFilters());
    });

    // Sıralama Değiştiğinde
    $(document).on('change', '.ajax-sort-select', function () {
        loadProducts(getCurrentFilters());
    });

    // Sayfalama Linkine Tıklandığında
    $(document).on('click', '#productListContainer .pagination a', function (e) {
        e.preventDefault();
        let filters = getCurrentFilters();
        filters.Page = $(this).data('page');
        loadProducts(filters);
    });

    // Tarayıcı Geri/İleri Tuşları
    $(window).on('popstate', function (e) {
        if (e.originalEvent.state) {
            // Filtreleri UI üzerinde de güncellemek gerekebilir. Şimdilik sayfayı yenilemek en basit çözüm.
            location.reload();
        }
    });

    // Kategori menüsü aç/kapat işlevi
    $(document).on('click', '.category-item-row .expander-icon', function (e) {
        e.preventDefault();
        e.stopPropagation(); // Linkin tıklanmasını engelle

        var $parentLi = $(this).closest('.has-children');
        var $subList = $parentLi.find('> .sub-category-list');
        var $icon = $(this).find('i');

        $subList.slideToggle(200);
        $icon.toggleClass('fa-caret-right fa-caret-down');
        $parentLi.toggleClass('open');
    });
});