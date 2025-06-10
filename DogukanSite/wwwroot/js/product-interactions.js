$(document).ready(function () {
    console.log("DEBUG: product-interactions.js çalıştı.");

    // =================================================================
    // GENEL AYARLAR VE YARDIMCI FONKSİYONLAR
    // =================================================================

    // Anti-Forgery Token'ı her AJAX isteğiyle göndermek için global ayar
    $.ajaxSetup({
        headers: {
            'RequestVerificationToken': $('meta[name="RequestVerificationToken"]').attr('content')
        }
    });

    // Navbar'daki sepet ikonunun sayacını güncelleyen global fonksiyon
    window.updateCartItemCountNavbar = function () {
        $.ajax({
            type: "GET",
            url: "/?handler=CartCount",
            success: function (response) {
                if (response && response.success) {
                    $('.cart-item-count-badge').text(response.count).toggleClass('d-none', response.count <= 0);
                }
            }
        });
    };


    // =================================================================
    // ÜRÜN KARTI ETKİLEŞİMLERİ (OLAY DİNLEYİCİLERİ)
    // =================================================================

    // --- 1. Adet Artırma/Azaltma Butonları (+/-) ---
    $(document).on('click', '.quantity-increase, .quantity-decrease', function () {
        var $button = $(this);
        var $input = $button.closest('.d-flex').find('.quantity-input');
        var currentVal = parseInt($input.val());
        var maxVal = parseInt($input.attr('max'));
        var minVal = parseInt($input.attr('min'));

        if ($button.hasClass('quantity-increase')) {
            if (currentVal < maxVal) {
                $input.val(currentVal + 1);
            }
        } else {
            if (currentVal > minVal) {
                $input.val(currentVal - 1);
            }
        }
    });

    // --- 2. Adet Kutusuna Yazıp Enter'a Basma ---
    $(document).on('keyup', '.product-card-hover-actions .quantity-input, .product-details-actions .quantity-input', function (e) {
        if (e.key === 'Enter' || e.keyCode === 13) {
            e.preventDefault();
            var $input = $(this);
            var currentVal = parseInt($input.val());
            var maxVal = parseInt($input.attr('max'));
            var minVal = parseInt($input.attr('min'));

            if (isNaN(currentVal) || currentVal < minVal) $input.val(minVal);
            else if (currentVal > maxVal) $input.val(maxVal);

            $input.closest('.product-card-hover-actions, .product-details-actions').find('.add-to-cart-js-btn').click();
        }
    });

    // --- 3. Sepete Ekle Butonu ---
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
        $button.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i>');

        $.ajax({
            type: "POST", url: "/?handler=AddToCart", data: { productId: productId, quantity: quantity },
            success: function (response) {
                if (response && response.success) {
                    updateCartItemCountNavbar();
                    $button.html('<i class="fas fa-check"></i> Eklendi!');
                    setTimeout(function () { $button.prop('disabled', false).html(originalButtonHtml); }, 2000);
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

    // --- 4. Favori Butonu ---
    $(document).on('click', '.favorite-icon', function (e) {
        var $button = $(this);
        if ($button.data('bs-toggle') === 'modal') return;
        e.preventDefault();

        var productId = $button.data('product-id');
        var $icon = $button.find('i');

        $button.prop('disabled', true);
        $icon.removeClass('far fa-heart fas fa-heart text-danger').addClass('fas fa-spinner fa-spin');

        $.ajax({
            type: "POST", url: "/?handler=ToggleFavorite", data: { productId: productId },
            success: function (response) {
                if (response && response.success) {
                    $icon.removeClass('fas fa-spinner fa-spin');
                    if (response.isFavorite) {
                        $icon.addClass('fas fa-heart text-danger').removeClass('far');
                        $button.attr('title', 'Favorilerden Çıkar');
                    } else {
                        $icon.addClass('far fa-heart').removeClass('fas text-danger');
                        $button.attr('title', 'Favorilere Ekle');
                    }
                    $(document).trigger('favoriteToggled', [productId, response.isFavorite, $button.get(0)]);
                } else {
                    $icon.removeClass('fas fa-spinner fa-spin').addClass('far fa-heart');
                    alert(response.message || "İşlem sırasında bir hata oluştu.");
                }
            },
            error: function () {
                $icon.removeClass('fas fa-spinner fa-spin').addClass('far fa-heart');
                alert('Sunucu ile iletişim kurulamadı.');
            },
            complete: function () {
                $button.prop('disabled', false);
            }
        });
    });

    // Sayfa ilk yüklendiğinde sepet sayacını al
    updateCartItemCountNavbar();
});