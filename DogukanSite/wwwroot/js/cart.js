$(document).ready(function () {
    if ($('#cartPageContainer').length === 0) return;

    // --- YARDIMCI FONKSİYONLAR ---

    // Sipariş özetini ve indirim satırını günceller
    function updateOrderSummary(totals) {
        if (!totals) return;
        const formatCurrency = (value) => (value || 0).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' });

        $('#cartSubtotal').text(formatCurrency(totals.subtotal));
        $('#cartShippingCost').text(totals.shippingCost > 0 ? formatCurrency(totals.shippingCost) : "Ücretsiz");
        $('#cartTotal').text(formatCurrency(totals.total));

        const $discountRow = $('#discountRow');
        if (totals.discountAmount > 0) {
            $('#cartDiscount').text('-' + formatCurrency(totals.discountAmount));
            $('#appliedCouponCodeText').text(`(${totals.appliedCouponCode})`);
            $discountRow.removeClass('d-none');
        } else {
            $discountRow.addClass('d-none');
        }
    }

    // Bildirim mesajı gösterir (alert() yerine)
    function showCartMessage(message, type = 'warning') {
        const alertClass = type === 'success' ? 'alert-success' : 'alert-warning';
        $('#cartActionStatusMessages').html(
            `<div class="alert ${alertClass} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Kapat"></button>
            </div>`
        );
    }

    // Sunucuya sepet güncelleme isteği gönderen merkezi fonksiyon
    function handleCartUpdate(url, data, $initiatingElement) {
        let $cartItemRow = $initiatingElement.closest('.cart-item');
        if ($cartItemRow.length === 0) $cartItemRow = $('body'); // Kupon gibi genel bir işlemse

        $cartItemRow.addClass('pe-none'); // Tıklamaları engelle

        $.ajax({
            type: "POST",
            url: url,
            data: data,
            success: function (response) {
                if (response && response.success) {
                    // DÜZELTME: Sunucudan gelen güncel adet bilgisini input'a yazdır.
                    if (response.newQuantity !== undefined && response.itemId) {
                        $(`#cart-item-${response.itemId}`).find('.quantity-input-ajax').val(response.newQuantity);
                    }

                    updateOrderSummary(response.totals);
                    if (typeof updateCartItemCountNavbar === "function") {
                        updateCartItemCountNavbar();
                    }

                    if (response.totals.isCartEmpty) {
                        $('#cartPageContainer').load(location.href + " #cartPageContainer > *");
                    } else if (response.removedItemId) {
                        $(`#cart-item-${response.removedItemId}`).fadeOut(function () { $(this).remove(); });
                    }
                    if (response.message) {
                        showCartMessage(response.message, 'success');
                    }
                } else {
                    showCartMessage(response.message || "Bir hata oluştu.", 'danger');
                }
            },
            error: function () {
                showCartMessage("Sunucuyla iletişim kurulamadı. Lütfen sayfayı yenileyin.", 'danger');
            },
            complete: function () {
                $cartItemRow.removeClass('pe-none');
            }
        });
    }

    // --- OLAY DİNLEYİCİLERİ ---

    // Adet butonları
    $(document).on('click', '.quantity-btn-ajax', function () {
        var $button = $(this);
        var $input = $button.siblings('input.quantity-input-ajax');
        var cartItemId = $button.closest('.quantity-controls').data('cart-item-id');
        var currentVal = parseInt($input.val());
        var newVal = $button.hasClass('quantity-increase-ajax') ? currentVal + 1 : currentVal - 1;

        handleCartUpdate(
            "/Cart?handler=UpdateQuantityJson",
            { cartItemId: cartItemId, quantity: newVal },
            $button
        );
    });

    // Ürün silme butonu
    $(document).on('click', '.remove-item-btn-ajax', function () {
        var $button = $(this);
        var cartItemId = $button.data('cart-item-id');
        if (confirm('Bu ürünü sepetten kaldırmak istediğinize emin misiniz?')) {
            handleCartUpdate(
                "/Cart?handler=RemoveItemJson",
                { cartItemId: cartItemId },
                $button
            );
        }
    });

    // Kupon formu
    $('#couponForm').on('submit', function (e) {
        e.preventDefault();
        var $form = $(this);
        var couponCode = $form.find('input[name="couponCode"]').val();
        handleCartUpdate(
            "/Cart?handler=ApplyCouponJson",
            { couponCode: couponCode },
            $form
        );
    });

    // Giriş sonrası sepet birleştiyse navbar'ı güncelle
    if ($('body').data('cart-merged') === 'True') {
        if (typeof updateCartItemCountNavbar === "function") {
            updateCartItemCountNavbar();
        }
    }
});