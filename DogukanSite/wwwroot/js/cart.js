$(document).ready(function () {

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    // Sepet arayüzünü gelen yeni veriyle güncellemek için merkezi fonksiyon
    function updateCartUI(cartData) {
        if (!cartData || !cartData.items) return;

        // Sipariş özetini güncelle
        const subtotalFormatted = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(cartData.subtotal);
        const totalFormatted = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(cartData.total);
        const discountFormatted = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(cartData.discountAmount);
        const shippingFormatted = cartData.shippingCost > 0
            ? new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(cartData.shippingCost)
            : "Ücretsiz";

        $('#cartSubtotal').text(subtotalFormatted);
        $('#cartTotal').text(totalFormatted);
        $('#cartShippingCost').text(shippingFormatted);

        if (cartData.discountAmount > 0 && cartData.appliedCouponCode) {
            $('#cartDiscount').text('-' + discountFormatted);
            $('#appliedCouponCodeText').text(`(${cartData.appliedCouponCode})`);
            $('#discountRow').removeClass('d-none');
        } else {
            $('#discountRow').addClass('d-none');
        }
    }

    // Bu fonksiyon projenin her yerinden sepet ikonunu günceller
    function updateCartIcon(totalItemCount) {
        const cartIcon = $('.cart-item-count-badge');
        if (cartIcon.length) {
            cartIcon.text(totalItemCount);
            if (totalItemCount > 0) {
                cartIcon.removeClass('d-none');
            } else {
                cartIcon.addClass('d-none');
            }
        }
    }

    function handleAjaxRequest(url, data, successCallback) {
        $.ajax({
            type: 'POST', url: url, headers: { 'RequestVerificationToken': getAntiForgeryToken() }, data: data
        }).done(function (response) {
            // Sunucudan gelen son sepet verisiyle tüm arayüzü ve ikonu güncelle
            updateCartUI(response);
            let totalItems = response.items.reduce((sum, item) => sum + item.quantity, 0);
            updateCartIcon(totalItems);

            if (successCallback) successCallback(response);
        }).fail(function () {
            alert('İşlem sırasında bir sunucu hatası oluştu.');
        });
    }

    // Adet kutusuna manuel giriş için merkezi fonksiyon
    function updateQuantityFromInput(inputElement) {
        const input = $(inputElement);
        const cartItemId = input.data('cart-item-id');
        let quantity = parseInt(input.val());
        const maxQuantity = parseInt(input.attr('max'));
        const warningElement = input.closest('.cart-item-details-col').find('.stock-warning-message');

        // DEĞİŞTİRİLDİ: Uyarı mesajı sadece yeni bir uyarı yoksa temizlenir.
        let hasWarning = false;
        if (isNaN(quantity) || quantity < 1) {
            quantity = 1;
            input.val(quantity);
        }

        if (quantity > maxQuantity) {
            quantity = maxQuantity;
            input.val(quantity);
            warningElement.text(`Stok limiti: ${maxQuantity} adet`).fadeIn();
            hasWarning = true;
        }

        if (!hasWarning) {
            warningElement.fadeOut().text('');
        }

        handleAjaxRequest('?handler=UpdateQuantityJson', { cartItemId: cartItemId, quantity: quantity }, function (response) {
            // İlgili ürün satırının toplam fiyatını güncelle
            const itemInCart = response.items.find(item => item.id === cartItemId);
            if (itemInCart) {
                const itemRow = $(`#cart-item-${cartItemId}`);
                const totalPriceFormatted = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(itemInCart.totalPrice);
                itemRow.find('.item-total-price').text(totalPriceFormatted);
            }
        });
    }

    // +/- Butonları için olay yakalayıcı
    $('#cartPageContainer').on('click', '.quantity-btn-ajax', function (e) {
        e.preventDefault();
        const button = $(this);
        const input = button.siblings('.quantity-input-ajax');

        // DEĞİŞTİRİLDİ: Adet artırma/azaltma mantığı buraya eklendi.
        let currentQuantity = parseInt(input.val());
        let newQuantity = button.hasClass('quantity-increase-ajax') ? currentQuantity + 1 : currentQuantity - 1;

        if (newQuantity < 1) return;

        input.val(newQuantity); // Input'un değerini güncelle
        updateQuantityFromInput(input.get(0)); // Güncellenmiş input ile ana fonksiyonu çağır
    });

    // Manuel giriş için olay yakalayıcılar
    $('#cartPageContainer').on('change', '.quantity-input-ajax', function () {
        updateQuantityFromInput(this);
    });
    $('#cartPageContainer').on('keyup', '.quantity-input-ajax', function (e) {
        if (e.key === 'Enter' || e.keyCode === 13) {
            updateQuantityFromInput(this);
            $(this).blur();
        }
    });

    // Kupon ve ürün silme fonksiyonları aynı kalabilir.
    $('#couponForm').on('submit', function (e) { e.preventDefault(); handleAjaxRequest('?handler=ApplyCouponJson', { couponCode: $('#couponCodeInput').val() }); });
    $('#orderSummaryContainer').on('click', '#removeCouponBtn', function (e) { e.preventDefault(); handleAjaxRequest('?handler=ApplyCouponJson', { couponCode: '' }); });
    $('#cartPageContainer').on('click', '.remove-item-btn-ajax', function (e) {
        e.preventDefault();
        const cartItemId = $(this).data('cart-item-id');
        if (!confirm('Bu ürünü sepetten kaldırmak istediğinizden emin misiniz?')) return;

        handleAjaxRequest('?handler=RemoveItemJson', { cartItemId: cartItemId }, function (response) {
            if (response.isCartEmpty) {
                window.location.reload();
            } else {
                $(`#cart-item-${cartItemId}`).fadeOut(400, function () { $(this).remove(); });
            }
        });
    });
});