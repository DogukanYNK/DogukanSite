$(document).ready(function () {

    function getAntiForgeryToken() {
        return $('input[name="__RequestVerificationToken"]').val();
    }

    // --- YENİ EKLENEN FONKSİYON: DEBOUNCE ---
    // Bir fonksiyonun belirli bir süre içinde tekrar tekrar çalışmasını engeller.
    function debounce(func, delay) {
        let timeout;
        return function (...args) {
            clearTimeout(timeout);
            timeout = setTimeout(() => func.apply(this, args), delay);
        };
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

        // Kupon bilgilerini ve mesajını güncelle
        if (cartData.discountAmount > 0 && cartData.appliedCouponCode) {
            $('#cartDiscount').text('-' + discountFormatted);
            $('#appliedCouponCodeText').text(`(${cartData.appliedCouponCode})`);
            $('#discountRow').removeClass('d-none');
            $('#removeCouponBtn').show();
        } else {
            $('#discountRow').addClass('d-none');
            $('#removeCouponBtn').hide();
        }

        // Kupon mesajını göster
        if (cartData.couponResponseMessage) {
            const alertClass = cartData.couponAppliedSuccessfully ? 'alert-success' : 'alert-danger';
            const couponMessageDiv = `<div class="alert ${alertClass} alert-dismissible fade show small p-2" role="alert">
                                         ${cartData.couponResponseMessage}
                                         <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
                                      </div>`;
            $('#couponMessageContainer').html(couponMessageDiv);
        } else {
            $('#couponMessageContainer').empty();
        }
    }

    // Bu fonksiyon projenin her yerinden sepet ikonunu günceller
    function updateCartIcon(totalItemCount) {
        const cartIcon = $('.cart-item-count-badge');
        if (cartIcon.length) {
            cartIcon.text(totalItemCount);
            cartIcon.toggleClass('d-none', totalItemCount === 0);
        }
    }

    // Merkezi AJAX istek fonksiyonu
    function handleAjaxRequest(url, data, loadingSpinner, successCallback) {
        if (loadingSpinner) loadingSpinner.show();

        $.ajax({
            type: 'POST',
            url: url,
            headers: { 'RequestVerificationToken': getAntiForgeryToken() },
            data: data
        }).done(function (response) {
            updateCartUI(response);
            let totalItems = response.items.reduce((sum, item) => sum + item.quantity, 0);
            updateCartIcon(totalItems);

            if (successCallback) successCallback(response);

        }).fail(function () {
            alert('İşlem sırasında bir sunucu hatası oluştu.');
        }).always(function () {
            if (loadingSpinner) loadingSpinner.hide();
        });
    }

    // Adet kutusuna manuel giriş için debounce edilmiş fonksiyon
    const debouncedUpdateQuantity = debounce(function (inputElement) {
        const input = $(inputElement);
        const cartItemId = input.data('cart-item-id');
        let quantity = parseInt(input.val());
        const maxQuantity = parseInt(input.attr('max'));
        const spinner = input.closest('.quantity-controls').find('.ajax-loading-spinner');

        if (isNaN(quantity) || quantity < 1) quantity = 1;
        if (quantity > maxQuantity) quantity = maxQuantity;
        input.val(quantity); // Kullanıcının girdiği geçersiz değeri düzelt

        handleAjaxRequest(`?handler=UpdateQuantityJson`, { cartItemId, quantity }, spinner, function (response) {
            const itemInCart = response.items.find(item => item.id === cartItemId);
            if (itemInCart) {
                const itemRow = $(`#cart-item-${cartItemId}`);
                const totalPriceFormatted = new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(itemInCart.totalPrice);
                itemRow.find('.item-total-price').text(totalPriceFormatted);
            }
        });
    }, 400); // 400ms bekle

    // Olay dinleyicileri (event listeners)
    const cartContainer = $('#cartPageContainer');

    cartContainer.on('click', '.quantity-btn-ajax', function (e) {
        e.preventDefault();
        const button = $(this);
        const input = button.siblings('.quantity-input-ajax');
        let currentQuantity = parseInt(input.val());
        let newQuantity = button.hasClass('quantity-increase-ajax') ? currentQuantity + 1 : currentQuantity - 1;

        if (newQuantity < 1) return;
        input.val(newQuantity);
        debouncedUpdateQuantity(input.get(0));
    });

    cartContainer.on('input', '.quantity-input-ajax', function () {
        debouncedUpdateQuantity(this);
    });

    cartContainer.on('click', '.remove-item-btn-ajax', function (e) {
        e.preventDefault();
        const cartItemId = $(this).data('cart-item-id');
        if (!confirm('Bu ürünü sepetten kaldırmak istediğinizden emin misiniz?')) return;

        handleAjaxRequest(`?handler=RemoveItemJson`, { cartItemId }, null, function (response) {
            if (response.isCartEmpty) {
                window.location.reload();
            } else {
                $(`#cart-item-${cartItemId}`).fadeOut(300, function () { $(this).remove(); });
            }
        });
    });

    // Kupon Formu
    $('#couponForm').on('submit', function (e) {
        e.preventDefault();
        handleAjaxRequest(`?handler=ApplyCouponJson`, { couponCode: $('#couponCodeInput').val() }, null);
    });

    $('#removeCouponBtn').on('click', function (e) {
        e.preventDefault();
        // --- DÜZELTME: Doğru handler'ı çağır ---
        handleAjaxRequest(`?handler=RemoveCouponJson`, {}, null, function () {
            $('#couponCodeInput').val(''); // Kupon kodunu input'tan da temizle
        });
    });
});