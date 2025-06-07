// wwwroot/js/product-interactions.js
// Site genelindeki ürün kartları üzerindeki etkileşimleri yönetir.
$(document).ready(function () {
    console.log("DEBUG: product-interactions.js çalıştı.");

    // Navbar'daki sepet ikonunun sayacını güncelleyen global fonksiyon
    window.updateCartItemCountNavbar = function () {
        $.ajax({
            type: "GET", url: "/?handler=CartCount",
            success: function (response) {
                var count = (response && response.success) ? response.count : 0;
                $('.cart-item-count-badge').each(function () {
                    $(this).text(count).toggleClass('d-none', count <= 0);
                });
            }
        });
    }

    // Sepete Ekle Butonu
    $(document).on('click', '.add-to-cart-js-btn', function (e) {
        e.preventDefault();
        var $button = $(this);
        var productId = $button.data('product-id');
        var quantity = 1;
        var $quantityInput = $button.closest('.product-card-hover-actions, .product-details-actions').find('.quantity-input');
        if ($quantityInput.length) {
            quantity = parseInt($quantityInput.val()) || 1;
        }

        var originalButtonHtml = $button.html();
        $button.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>');

        $.ajax({
            type: "POST", url: "/?handler=AddToCart",
            data: { productId: productId, quantity: quantity },
            success: function (response) {
                if (response && response.success) {
                    updateCartItemCountNavbar(); // Sepet sayacını güncelle
                    $button.html('<i class="fas fa-check me-1"></i>Eklendi!');
                    setTimeout(function () {
                        $button.prop('disabled', false).html(originalButtonHtml);
                    }, 2000);
                } else {
                    alert(response.message || 'Ürün sepete eklenemedi.');
                    $button.prop('disabled', false).html(originalButtonHtml);
                }
            },
            error: function () {
                alert('Sunucu hatası oluştu.');
                $button.prop('disabled', false).html(originalButtonHtml);
            }
        });
    });

    // Favori Butonu
    $(document).on('click', '.favorite-icon', function (e) {
        var $button = $(this);
        if ($button.data('bs-toggle') === 'modal') return;

        e.preventDefault();
        var productId = $button.data('product-id');
        var $icon = $button.find('i');
        $button.prop('disabled', true);
        $icon.removeClass('far fa-heart fas fa-heart text-danger').addClass('fas fa-spinner fa-spin');

        $.ajax({
            type: "POST", url: "/?handler=ToggleFavorite",
            data: { productId: productId },
            success: function (response) {
                if (response && response.success) {
                    $icon.removeClass('fas fa-spinner fa-spin');
                    if (response.isFavorite) {
                        $icon.addClass('fas fa-heart text-danger');
                        $button.attr('title', 'Favorilerden Çıkar');
                    } else {
                        $icon.addClass('far fa-heart');
                        $button.attr('title', 'Favorilere Ekle');
                    }
                }
            },
            error: function () {
                $icon.removeClass('fas fa-spinner fa-spin').addClass('far fa-heart');
            },
            complete: function () {
                $button.prop('disabled', false);
            }
        });
    });

    // Sayfa ilk yüklendiğinde sepet sayacını al
    updateCartItemCountNavbar();
});