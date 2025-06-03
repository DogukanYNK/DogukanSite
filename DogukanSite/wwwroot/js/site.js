// wwwroot/js/site.js

console.log("DEBUG: site.js dosyası en üst seviyede yüklendi."); // <-- BU SATIRI EKLE (DOSYANIN EN BAŞINA)

function addAntiForgeryToken(xhr) {
    var token = $('meta[name="RequestVerificationToken"]').attr('content');
    if (token) {
        xhr.setRequestHeader("RequestVerificationToken", token);
    } else {
        console.error("CSRF Token not found in meta tag!");
    }
}

var offcanvasInstances = {};
function getOffcanvasInstance(elementId) {
    var offcanvasElement = document.getElementById(elementId);
    if (!offcanvasElement) return null;
    return bootstrap.Offcanvas.getInstance(offcanvasElement) || new bootstrap.Offcanvas(offcanvasElement);
}

function updateOrderSummary(data) {
    if (data) {
        $('#cartSubtotal').text((data.subtotal !== undefined ? data.subtotal : 0).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' }));
        $('#cartShippingCost').text((data.shippingCost !== undefined ? data.shippingCost : 0) > 0 ? (data.shippingCost).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' }) : "Ücretsiz");
        var $discountRow = $('#discountRow');
        var $cartDiscount = $('#cartDiscount');
        var $appliedCouponCodeText = $('#appliedCouponCodeText');
        if (data.discountAmount > 0 && data.appliedCouponCode) {
            $cartDiscount.text('-' + data.discountAmount.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' }));
            if ($appliedCouponCodeText.length) { $appliedCouponCodeText.text('(' + data.appliedCouponCode + ')'); }
            $discountRow.removeClass('d-none');
        } else {
            $cartDiscount.text(''); $appliedCouponCodeText.text(''); $discountRow.addClass('d-none');
        }
        $('#cartTotal').text((data.total !== undefined ? data.total : 0).toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' }));
    }
}

function showEmptyCartMessage() {
    var $cartPageContainer = $('#cartPageContainer');
    if ($cartPageContainer.length) {
        $cartPageContainer.html(
            '<div class="text-center py-5 empty-cart-message">' +
            '<i class="fas fa-shopping-cart fa-4x text-muted mb-3"></i>' +
            '<h3 class="mb-3">Sepetiniz Şu Anda Boş</h3>' +
            '<p class="lead text-muted mb-4">Görünüşe göre henüz sepetinize bir şey eklememişsiniz.</p>' +
            '<a href="/Products/Index" class="btn btn-primary btn-lg">' +
            '<i class="fas fa-store me-2"></i>Hemen Alışverişe Başla' +
            '</a></div>');
        $('#orderSummaryContainer').addClass('d-none');
    }
}

function getCurrentTotalQuantityFromDOM() {
    let totalQuantity = 0;
    $('.cart-page .quantity-input-ajax, .cart-items-container .quantity-input-ajax').each(function () {
        totalQuantity += parseInt($(this).val()) || 0;
    });
    return totalQuantity;
}

function refreshCartBadgeOnNavbar(newCount) {
    var cartBadgeDesktop = document.getElementById("cartItemCountLayoutDesktop");
    var cartBadgeMobile = document.getElementById("cartItemCountLayoutMobile");
    newCount = parseInt(newCount) || 0;
    function updateSingleBadge(badgeElement) {
        if (badgeElement) {
            badgeElement.innerText = newCount;
            $(badgeElement).toggleClass('d-none', newCount <= 0);
        }
    }
    updateSingleBadge(cartBadgeDesktop);
    updateSingleBadge(cartBadgeMobile);
}

function updateCartItemCountNavbar() {
    $.ajax({
        type: "GET", url: "/?handler=CartCount",
        success: function (response) {
            if (response && response.success && typeof response.count !== 'undefined') {
                refreshCartBadgeOnNavbar(response.count);
            } else { refreshCartBadgeOnNavbar(0); }
        },
        error: function () { refreshCartBadgeOnNavbar(0); }
    });
}

function scrollToTop() { window.scrollTo({ top: 0, behavior: 'smooth' }); }


function initializeCartPageEvents() {
    console.log("DEBUG: initializeCartPageEvents fonksiyonu ÇAĞRILDI.");

    // updateCartItemQuantity fonksiyonu ve diğer sepet olay dinleyicileri buraya gelecek
    function updateCartItemQuantity(cartItemId, quantity, $cartItemRow) {
        console.log("DEBUG: AJAX Call => CartItemId:", cartItemId, "New Quantity:", quantity);

        if (cartItemId === undefined || cartItemId === null) {
            $('#cartActionStatusMessages').html('<div class="alert alert-danger alert-dismissible fade show" role="alert">Ürün ID hatası!<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');
            return;
        }

        var $inputField = $cartItemRow.find('.quantity-input-ajax');
        var $decreaseButton = $cartItemRow.find('.quantity-decrease-ajax');
        var $increaseButton = $cartItemRow.find('.quantity-increase-ajax');
        var $removeButton = $cartItemRow.find('.remove-item-btn-ajax');
        var $spinner = $cartItemRow.find('.ajax-loading-spinner');

        var originalRemoveButtonHtml = $removeButton.data('original-html-remove');
        if (!originalRemoveButtonHtml) {
            originalRemoveButtonHtml = $removeButton.html();
            $removeButton.data('original-html-remove', originalRemoveButtonHtml);
        }

        $decreaseButton.prop('disabled', true);
        $increaseButton.prop('disabled', true);
        $inputField.prop('disabled', true);
        if ($spinner.length) $spinner.show();

        if (quantity <= 0) {
            $removeButton.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Siliniyor...');
            if ($spinner.length) $spinner.hide();
        }

        $.ajax({
            type: "POST",
            url: quantity > 0 ? "/Cart/Index?handler=UpdateQuantityJson" : "/Cart/Index?handler=RemoveItemJson",
            data: { cartItemId: cartItemId, quantity: quantity > 0 ? quantity : undefined },
            beforeSend: addAntiForgeryToken,
            success: function (response) {
                console.log("DEBUG: AJAX Success Response:", response);
                $('#cartActionStatusMessages').empty();
                var $stockErrorMsg = $('#stock-error-message-' + cartItemId);
                if ($stockErrorMsg.length) $stockErrorMsg.hide().text('');

                if (response) {
                    if (response.success) {
                        refreshCartBadgeOnNavbar(response.cartItemCount);

                        if (response.removedItemId && response.removedItemId == cartItemId) {
                            console.log("DEBUG: Ürün kaldırılıyor (removedItemId):", response.removedItemId);
                            $cartItemRow.fadeOut(function () {
                                $(this).remove();
                                if (response.isCartEmpty) {
                                    console.log("DEBUG: Sepet boşaldı, boş sepet mesajı gösteriliyor.");
                                    showEmptyCartMessage();
                                    $('#orderSummaryContainer').addClass('d-none');
                                } else {
                                    $('#orderSummaryContainer').removeClass('d-none');
                                    if ($('.cart-item').length === 0) {
                                        console.log("DEBUG: DOM'da .cart-item kalmadı, boş sepet mesajı gösteriliyor.");
                                        showEmptyCartMessage();
                                    }
                                }
                            });
                            if (response.message) {
                                $('#cartActionStatusMessages').html('<div class="alert alert-success alert-dismissible fade show" role="alert">' + response.message + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');
                            }
                        } else if (response.itemId && response.itemId == cartItemId) {
                            console.log("DEBUG: Ürün adedi güncelleniyor (itemId):", response.itemId, "Yeni Adet:", response.newQuantity);
                            $inputField.val(response.newQuantity);
                            $cartItemRow.find('.item-total-price').text(response.newTotalPriceForItem.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' }));
                            if (response.message && response.message !== "Sepet güncellendi.") {
                                if ($stockErrorMsg.length) $stockErrorMsg.text(response.message).show();
                            }
                        }
                        updateOrderSummary(response);

                    } else {
                        console.log("DEBUG: AJAX yanıtı başarısız (success:false). Mesaj:", response.message);
                        if (response.removedItemId && response.removedItemId == cartItemId) {
                            console.log("DEBUG: Ürün kaldırılıyor (success:false ama removedItemId var):", response.removedItemId);
                            $cartItemRow.fadeOut(function () { $(this).remove(); if (response.isCartEmpty) showEmptyCartMessage(); });
                        } else if (response.itemId && response.itemId == cartItemId && response.newQuantity !== undefined) {
                            console.log("DEBUG: Adet backend tarafından düzeltildi (success:false):", response.newQuantity);
                            $inputField.val(response.newQuantity);
                            if (response.newTotalPriceForItem !== undefined) {
                                $cartItemRow.find('.item-total-price').text(response.newTotalPriceForItem.toLocaleString('tr-TR', { style: 'currency', currency: 'TRY' }));
                            }
                        }
                        if (response.message) {
                            $('#cartActionStatusMessages').html('<div class="alert alert-warning alert-dismissible fade show" role="alert">' + response.message + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');
                        }
                        refreshCartBadgeOnNavbar(response.cartItemCount !== undefined ? response.cartItemCount : getCurrentTotalQuantityFromDOM());
                        updateOrderSummary(response);
                    }
                } else {
                    console.log("DEBUG: AJAX yanıtı boş veya tanımsız.");
                    $('#cartActionStatusMessages').html('<div class="alert alert-danger alert-dismissible fade show" role="alert">Sepet güncellenemedi veya bilinmeyen bir yanıt alındı.<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');
                }
            },
            error: function (xhr, status, error) {
                console.error("DEBUG: Cart Update AJAX Error. Status:", status, "Error:", error, "ResponseText:", xhr.responseText);
                $('#cartActionStatusMessages').html('<div class="alert alert-danger alert-dismissible fade show" role="alert">Sepet güncellenirken bir sunucu hatası oluştu.<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');
            },
            complete: function () {
                console.log("DEBUG: AJAX İsteği tamamlandı (complete). CartItemId:", cartItemId);
                if ($cartItemRow && $cartItemRow.length > 0 && !$cartItemRow.is(':hidden')) {
                    $inputField.prop('disabled', false);
                    if ($spinner.length) $spinner.hide();

                    var actualQuantityInInput = parseInt($inputField.val()) || 0;
                    var productMinStock = parseInt($inputField.attr('min')) || 1;
                    var productMaxStock = parseInt($inputField.attr('max')) || 999;

                    $decreaseButton.prop('disabled', actualQuantityInInput <= productMinStock);
                    $increaseButton.prop('disabled', actualQuantityInInput >= productMaxStock);

                    $removeButton.prop('disabled', false);
                    if ($removeButton.find('.fa-spinner').length > 0) {
                        $removeButton.html(originalRemoveButtonHtml || '<i class="fas fa-trash-alt me-1"></i>Kaldır');
                    }
                }
            }
        });
    }

    // Adet Artırma/Azaltma Butonları için Event Listener'lar
    // .cart-page class'ı artık body'de olduğu için direkt bu class üzerinden de gidebiliriz
    // ya da document.on ile devam edebiliriz. Document.on daha garanti.
    $(document).on('click', '.cart-page .quantity-btn-ajax', function () {
        console.log("DEBUG: SEPET - Adet Butonu (artırma/azaltma genel) tıklandı!");
        var $button = $(this);
        // ... (önceki mesajımdaki gibi devamı) ...
        var $quantityControlsDiv = $button.closest('.quantity-controls');
        var cartItemId = $quantityControlsDiv.data('cart-item-id');
        var $input = $quantityControlsDiv.find('.quantity-input-ajax');

        if ($input.length === 0 || cartItemId === undefined) {
            console.log("DEBUG: Adet butonu için input veya cartItemId bulunamadı.");
            return;
        }
        var currentQuantity = parseInt($input.val()) || 0;
        var newQuantity;
        var maxVal = parseInt($input.attr('max')) || 999;
        var minVal = 1;

        if ($button.hasClass('quantity-increase-ajax')) {
            console.log("DEBUG: SEPET - Adet ARTIRMA butonu spesifik olarak tıklandı!");
            newQuantity = currentQuantity + 1;
            if (newQuantity > maxVal) newQuantity = maxVal;
        } else if ($button.hasClass('quantity-decrease-ajax')) {
            console.log("DEBUG: SEPET - Adet AZALTMA butonu spesifik olarak tıklandı!");
            newQuantity = currentQuantity - 1;
            if (newQuantity < minVal) newQuantity = minVal;
        } else {
            console.log("DEBUG: Tıklanan adet butonu ne artırma ne de azaltma class'ına sahip değil.");
            return;
        }
        console.log("DEBUG: SEPET - Hesaplanmış yeni adet:", newQuantity, "Ürün ID:", cartItemId);
        updateCartItemQuantity(cartItemId, newQuantity, $button.closest('.cart-item'));
    });

    // Adet Input'u Değiştiğinde (Enter veya focus kaybı)
    $(document).on('change', '.cart-page .quantity-input-ajax', function () {
        console.log("DEBUG: SEPET - Adet INPUT alanı değişti (change event).");
        var $input = $(this);
        // ... (önceki mesajımdaki gibi devamı) ...
        var cartItemId = $input.closest('.quantity-controls').data('cart-item-id');
        if (cartItemId === undefined) {
            console.log("DEBUG: Adet inputu için cartItemId bulunamadı.");
            return;
        }

        var newQuantity = parseInt($input.val()) || 0;
        var minVal = parseInt($input.attr('min')) || 1;
        var maxVal = parseInt($input.attr('max')) || 999;

        if (newQuantity < minVal && newQuantity !== 0) {
            newQuantity = minVal;
        }
        if (newQuantity > maxVal) {
            newQuantity = maxVal;
        }
        $input.val(newQuantity);
        console.log("DEBUG: SEPET - Inputtan okunan/düzeltilen yeni adet:", newQuantity, "Ürün ID:", cartItemId);
        updateCartItemQuantity(cartItemId, newQuantity, $input.closest('.cart-item'));
    });

    // Ürünü Sepetten Kaldırma Butonu
    $(document).on('click', '.cart-page .remove-item-btn-ajax', function () {
        console.log("DEBUG: SEPET - Ürün KALDIRMA butonu (spesifik event listener) tıklandı!");
        var $button = $(this);
        // ... (önceki mesajımdaki gibi devamı) ...
        var cartItemId = $button.data('cart-item-id');
        if (cartItemId === undefined) {
            console.log("DEBUG: Kaldırma butonu için cartItemId bulunamadı.");
            return;
        }
        if (!confirm('Bu ürünü sepetten kaldırmak istediğinize emin misiniz?')) {
            console.log("DEBUG: Kullanıcı ürün kaldırmayı iptal etti.");
            return;
        }
        console.log("DEBUG: SEPET - Ürün kaldırılacak (quantity 0), Ürün ID:", cartItemId);
        updateCartItemQuantity(cartItemId, 0, $button.closest('.cart-item'));
    });

    // Kupon Kodu Formu (AJAX ile)
    // Bu event listener doğrudan forma ID ile bağlandığı için document.on kullanmaya gerek yok,
    // form DOM'da hazır olduğunda zaten çalışır.
    $('#couponForm').on('submit', function (e) {
        console.log("DEBUG: SEPET - KUPON FORMU gönderildi (submit event listener)!");
        e.preventDefault();
        var $form = $(this);
        // ... (önceki mesajımdaki gibi devamı) ...
        var couponCode = $form.find('input[name="couponCode"]').val();
        var $submitButton = $form.find('button[type="submit"]');
        var originalButtonText = $submitButton.html();
        $submitButton.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Uygulanıyor');
        var $couponMessageContainer = $('#couponMessageContainer');
        $couponMessageContainer.html('');
        console.log("DEBUG: SEPET - Kupon Kodu AJAX İsteği Başlıyor. Kod:", couponCode);

        $.ajax({
            type: "POST", url: "/Cart/Index?handler=ApplyCouponJson",
            data: { couponCode: couponCode },
            beforeSend: addAntiForgeryToken,
            success: function (response) {
                console.log("DEBUG: Kupon AJAX Success Response:", response);
                if (response) {
                    updateOrderSummary(response);
                    var alertClass = response.success ? 'alert-success' : 'alert-danger';
                    var messageToShow = response.message || (response.success ? "Kupon uygulandı." : "Kupon uygulanamadı.");
                    $couponMessageContainer.html('<div class="alert ' + alertClass + ' alert-dismissible fade show mt-2" role="alert">' + messageToShow + '<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');

                    if (response.success && response.appliedCouponCode) {
                        $('#couponCodeInput').val(response.appliedCouponCode);
                    } else if (!response.success && !response.appliedCouponCode) {
                        $('#couponCodeInput').val('');
                    }
                } else {
                    console.log("DEBUG: Kupon AJAX yanıtı boş veya tanımsız.");
                    $couponMessageContainer.html('<div class="alert alert-danger alert-dismissible fade show mt-2" role="alert">Kupon uygulanamadı veya geçersiz yanıt.<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');
                }
            },
            error: function (xhr, status, error) {
                console.error("DEBUG: Kupon AJAX Error. Status:", status, "Error:", error, "ResponseText:", xhr.responseText);
                $couponMessageContainer.html('<div class="alert alert-danger alert-dismissible fade show mt-2" role="alert">Kupon uygulanırken bir sunucu hatası oluştu.<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button></div>');
            },
            complete: function () {
                console.log("DEBUG: Kupon AJAX İsteği tamamlandı (complete).");
                $submitButton.prop('disabled', false).html(originalButtonText);
            }
        });
    });
} // initializeCartPageEvents fonksiyonunun sonu


$(document).ready(function () {

    console.log("DEBUG: Document ready fonksiyonu (jQuery hazır) çalıştı."); // <-- BU SATIRI EKLE

    // --- 1. Yukarı Çık Butonu ---
    var scrollTopBtn = document.getElementById("scrollTopBtn");
    if (scrollTopBtn) {
        window.onscroll = function () { scrollFunction(); };
        function scrollFunction() {
            if (document.body.scrollTop > 150 || document.documentElement.scrollTop > 150) {
                scrollTopBtn.style.visibility = "visible"; scrollTopBtn.style.opacity = "1";
            } else {
                scrollTopBtn.style.opacity = "0";
                setTimeout(function () {
                    if (scrollTopBtn.style.opacity === "0" && (document.body.scrollTop <= 150 && document.documentElement.scrollTop <= 150)) {
                        scrollTopBtn.style.visibility = "hidden";
                    }
                }, 300);
            }
        }
    }

    // --- 2. Navbar Sepet Sayacı Güncelleme (Sayfa Yüklenince) ---
    updateCartItemCountNavbar();

    // --- 3. Arama Modalı Odaklanma ve Ana Offcanvas Kapatma ---
    var searchModal = document.getElementById('searchModal');
    if (searchModal) {
        searchModal.addEventListener('shown.bs.modal', function () {
            var searchInputInModal = searchModal.querySelector('input[type="search"]');
            if (searchInputInModal) { searchInputInModal.focus(); }
            var mainOffcanvasInstance = getOffcanvasInstance('mainOffcanvas');
            if (mainOffcanvasInstance && mainOffcanvasInstance._element && mainOffcanvasInstance._element.classList.contains('show')) {
                mainOffcanvasInstance.hide();
            }
        });
    }

    // --- 4. Ürün Kartı Hover - Adet Artırma/Azaltma ---
    // (Bu kısım olduğu gibi kalabilir, sepet sayfası dışındaki ürün kartları için)
    $(document).on('click', '.product-card-hover-actions .quantity-increase', function () {
        var $input = $(this).siblings('.quantity-input');
        if ($input.length === 0) { $input = $(this).closest('.d-flex').find('.quantity-input'); if ($input.length === 0) return; }
        var currentValue = parseInt($input.val()) || 0;
        var maxValue = parseInt($input.attr('max')) || 100;
        if (currentValue < maxValue) { $input.val(currentValue + 1).trigger('change'); }
    });
    $(document).on('click', '.product-card-hover-actions .quantity-decrease', function () {
        var $input = $(this).siblings('.quantity-input');
        if ($input.length === 0) { $input = $(this).closest('.d-flex').find('.quantity-input'); if ($input.length === 0) return; }
        var currentValue = parseInt($input.val()) || 1;
        var minValue = parseInt($input.attr('min')) || 1;
        if (currentValue > minValue) { $input.val(currentValue - 1).trigger('change'); }
        else { $input.val(minValue).trigger('change'); }
    });
    $(document).on('change keyup', '.product-card-hover-actions .quantity-input', function (e) {
        if (e.type === 'change' || (e.type === 'keyup' && (e.key === 'Enter' || e.keyCode === 13))) {
            var $input = $(this);
            var currentValue = parseInt($input.val()) || 1;
            var minValue = parseInt($input.attr('min')) || 1;
            var maxValue = parseInt($input.attr('max')) || 100;
            if (currentValue < minValue) $input.val(minValue);
            if (currentValue > maxValue) $input.val(maxValue);
        }
    });

    // --- 5. Favori İkonları Tıklama Olayı ---
    // (Bu kısım olduğu gibi kalabilir)
    $(document).on('click', '.favorite-icon', function (e) {
        var $button = $(this);
        if ($button.data('bs-toggle') === 'modal') { return; }
        e.preventDefault();
        var productId = $button.data('product-id');
        if (!productId) {
            if ($button.is('a') && $button.attr('href') && $button.attr('href') !== '#') { window.location.href = $button.attr('href'); }
            else { console.error("Favori butonu için ürün ID'si bulunamadı!"); }
            return;
        }
        var $icon = $button.find('i');
        var originalIconClasses = $icon.attr('class');
        $icon.removeClass('far fa-heart fas text-danger').addClass('fas fa-spinner fa-spin');
        $button.prop('disabled', true);
        $.ajax({
            type: "POST", url: "/?handler=ToggleFavorite", data: { productId: productId },
            beforeSend: addAntiForgeryToken,
            success: function (response) {
                if (response && response.success) {
                    if (response.isFavorite) {
                        $icon.removeClass('fas fa-spinner fa-spin').addClass('fas fa-heart text-danger');
                        $button.attr('title', 'Favorilerden Çıkar');
                    } else {
                        $icon.removeClass('fas fa-spinner fa-spin').addClass('far fa-heart');
                        $button.attr('title', 'Favorilere Ekle');
                    }
                    if (window.location.pathname.toLowerCase().includes('/account/favorites') && !response.isFavorite) {
                        $button.closest('.col.d-flex.align-items-stretch').fadeOut(function () {
                            $(this).remove();
                            if ($('.product-grid .col').length === 0 && $('#productListContainer').length > 0) {
                                $('#productListContainer').html('<div class="text-center py-5 empty-cart-message"><i class="fas fa-heart-broken fa-3x text-muted mb-3"></i><p class="lead">Favori listenizde ürün kalmadı.</p><a href="/Products/Index" class="btn btn-primary mt-3">Alışverişe Başla</a></div>');
                            }
                        });
                    }
                } else {
                    console.error('Favori toggle hatası:', response ? response.message : 'Bilinmeyen hata');
                    if (response && response.redirectToLogin) {
                        var loginModalEl = document.getElementById('loginOrRegisterToFavoriteModal');
                        if (loginModalEl) {
                            var loginModal = bootstrap.Modal.getOrCreateInstance(loginModalEl);
                            loginModal.show();
                        }
                    }
                    $icon.attr('class', originalIconClasses);
                }
            },
            error: function (xhr) { console.error("ToggleFavorite AJAX Hatası:", xhr.status, xhr.responseText); $icon.attr('class', originalIconClasses); },
            complete: function () { $button.prop('disabled', false); }
        });
    });

    // --- 6. Sepete Ekle Butonu Tıklama Olayı (Ürün Kartlarından) ---
    // (Bu kısım olduğu gibi kalabilir)
    $(document).on('click', '.add-to-cart-js-btn', function (e) {
        e.preventDefault();
        var $button = $(this);
        var productId = $button.data('product-id');
        var $quantityInput = $button.closest('.product-card-hover-actions').find('.quantity-input');
        var quantity = $quantityInput.length ? parseInt($quantityInput.val()) : 1;
        if (isNaN(quantity) || quantity < 1) { quantity = 1; $quantityInput.val(1); }
        if (!productId) { console.error("Ürün ID'si bulunamadı!"); return; }
        var originalButtonText = $button.html();
        $button.prop('disabled', true).html('<i class="fas fa-spinner fa-spin me-1"></i>Ekleniyor...');
        var postData = { productId: productId, quantity: quantity };
        $.ajax({
            type: "POST", url: "/?handler=AddToCart", data: postData, beforeSend: addAntiForgeryToken,
            success: function (response) {
                if (response && response.success) {
                    refreshCartBadgeOnNavbar(response.newCount);
                    $button.html('<i class="fas fa-check me-1"></i>Eklendi!');
                    setTimeout(function () { $button.prop('disabled', false).html(originalButtonText); }, 2000);
                } else {
                    alert('Hata: ' + (response ? response.message : 'Ürün sepete eklenemedi.'));
                    $button.prop('disabled', false).html(originalButtonText);
                }
            },
            error: function (xhr) {
                console.error("AddToCart AJAX Hatası:", xhr.status, xhr.responseText);
                alert('Sepete eklenirken bir sunucu hatası oluştu.');
                $button.prop('disabled', false).html(originalButtonText);
            }
        });
    });

    // --- 7. Off-Canvas (Ana Menü) Linkleri İçin Kapatma Mekanizması ---
    // (Bu kısım olduğu gibi kalabilir)
    $('#mainOffcanvas .offcanvas-body').on('click', 'a[href], form button[type="submit"]', function (e) {
        var $this = $(this);
        var href = $this.is('a') ? $this.attr('href') : null;
        if ($this.data('bs-toggle') === 'collapse' || $this.data('bs-toggle') === 'modal' ||
            (href && (href === '#' || href.trim() === '' || href.toLowerCase().startsWith('javascript:')))) {
            if ($this.data('bs-toggle') !== 'modal') { return; }
        }
        var offcanvasElement = document.getElementById('mainOffcanvas');
        if (offcanvasElement) {
            var offcanvasInstance = getOffcanvasInstance('mainOffcanvas');
            if (offcanvasInstance && offcanvasInstance._isShown) {
                if (!($this.is('button') && $this.closest('form[method="post"]').length > 0)) {
                    setTimeout(function () { if (offcanvasInstance._isShown) { offcanvasInstance.hide(); } }, 150);
                }
            }
        }
    });

    // === 8. AJAX FİLTRELEME VE SIRALAMA (Products/Index sayfası için) ===
    // Bu if kontrolü, bu scriptlerin sadece ürün listeleme sayfasında çalışmasını sağlar.
    if ($('#productListContainer').length > 0) {
        var isLoading = false;

        function getCurrentFilters() {
            var filters = {
                // Arama terimini sidebar/offcanvas'taki formdan veya URL'den al
                searchTerm: $('#filterSearchTerm_full').val() || new URLSearchParams(window.location.search).get('SearchTerm') || '',
                category: '', // Aktif linkten veya _full ID'li inputtan alacağız
                sortBy: '',   // Aktif/görünür olan select'ten alacağız
                currentPage: 1
            };

            // Kategoriyi öncelikle aktif linkten al
            var activeCategoryLink = $('.category-filter-list-v2 a.category-link.active');
            if (activeCategoryLink.length > 0) {
                filters.category = activeCategoryLink.data('category');
            } else {
                // Aktif link yoksa (örn: ilk yükleme, URL'den direkt gelme), 
                // _full ID'li gizli input'tan (eğer varsa) veya URL'den almayı dene
                filters.category = $('#filterCategory_full').val() || new URLSearchParams(window.location.search).get('Category') || '';
            }

            // Sıralamayı görünür olan ve değeri olan select'ten al
            var desktopSortSelect = $('#sortByProductPage_sortOnly');
            var sidebarSortSelect = $('#sortByProductPage_full');

            if (desktopSortSelect.is(':visible') && desktopSortSelect.val()) {
                filters.sortBy = desktopSortSelect.val();
            } else if (sidebarSortSelect.is(':visible') && sidebarSortSelect.val()) {
                filters.sortBy = sidebarSortSelect.val();
            } else { // Fallback veya ilk yükleme
                filters.sortBy = new URLSearchParams(window.location.search).get('SortBy') || 'newest';
            }

            console.log("DEBUG: getCurrentFilters:", filters);
            return filters;
        }

        function loadProducts(pageNumber, updateHistory = true) {
            // ... (loadProducts fonksiyonunuzun geri kalanı büyük ölçüde aynı kalabilir) ...
            // Sadece getCurrentFilters() çağrısı yukarıdaki güncellenmiş hali kullanacak.
            if (isLoading) {
                console.log("DEBUG: Zaten bir yükleme işlemi devam ediyor. Yeni istek engellendi.");
                return;
            }
            isLoading = true;
            $('#productListContainer').html('<div class="text-center py-5"><i class="fas fa-spinner fa-spin fa-3x text-primary"></i><p class="mt-2 text-muted">Ürünler yükleniyor...</p></div>');
            $('#productCountInfoDesktop').html('<span class="text-muted"><i class="fas fa-spinner fa-spin"></i></span>');
            $('#productCountInfoMobile').html('<small class="text-muted"><i class="fas fa-spinner fa-spin"></i></small>');

            var filters = getCurrentFilters(); // Güncellenmiş fonksiyon çağrılıyor
            filters.currentPage = parseInt(pageNumber) || 1;

            // Sayfalama için currentPage değerini de gizli input'a yazabiliriz (opsiyonel, popstate için)
            $('#filterCurrentPage_full').val(filters.currentPage);
            $('#filterCurrentPage_sortOnly').val(filters.currentPage);


            var requestData = {
                handler: "LoadProductsPartial",
                searchTerm: filters.searchTerm,
                category: filters.category,
                sortBy: filters.sortBy,
                currentPage: filters.currentPage
            };
            console.log("DEBUG: loadProducts AJAX isteği gönderiliyor:", requestData);

            $.ajax({
                url: window.location.pathname,
                type: 'GET',
                data: requestData,
                success: function (responseHtml) {
                    console.log("DEBUG: loadProducts AJAX BAŞARILI.");
                    $('#productListContainer').html(responseHtml);
                    var totalProducts = parseInt($('#productListContainer .product-grid').data('total-products')) || 0;
                    updateProductCount(totalProducts, filters.searchTerm, filters.category);
                    if (updateHistory) {
                        var newUrl = window.location.pathname +
                            '?SearchTerm=' + encodeURIComponent(filters.searchTerm) +
                            '&Category=' + encodeURIComponent(filters.category) +
                            '&SortBy=' + encodeURIComponent(filters.sortBy) +
                            '&CurrentPage=' + encodeURIComponent(filters.currentPage);
                        history.pushState({ path: newUrl, filters: filters }, '', newUrl); // state'e filtreleri de ekleyebiliriz
                        console.log("DEBUG: Tarayıcı geçmişi güncellendi:", newUrl);
                    }
                },
                error: function (xhr, status, error) {
                    console.error("DEBUG: loadProducts AJAX HATASI:", status, error, xhr.responseText);
                    $('#productListContainer').html('<div class="alert alert-danger text-center">Ürünler yüklenirken bir hata oluştu. Lütfen tekrar deneyin.</div>');
                },
                complete: function () {
                    isLoading = false;
                    console.log("DEBUG: loadProducts AJAX TAMAMLANDI.");
                }
            });
        }

        function updateProductCount(totalCount, searchTerm, category) {
            // ... (Bu fonksiyon aynı kalabilir) ...
            var message = "";
            if (totalCount > 0) {
                message = totalCount + " ürün bulundu";
            } else if (searchTerm || category) {
                message = "Ürün bulunamadı";
            } else {
                message = "Listelenecek ürün yok";
            }
            $('#productCountInfoDesktop').html('<span class="text-muted">' + message + '</span>');
            $('#productCountInfoMobile').html('<small class="text-muted">' + message + '</small>');
            console.log("DEBUG: Ürün sayısı güncellendi:", message);
        }

        function initializeFiltersFromUrl() {
            var params = new URLSearchParams(window.location.search);
            var category = params.get('Category') || ''; // Boş string varsayılan
            var sortBy = params.get('SortBy') || 'newest';
            var searchTerm = params.get('SearchTerm') || '';

            // Kategori linklerini ayarla
            $('.category-filter-list-v2 a.category-link').removeClass('active');
            if (category) {
                $('.category-filter-list-v2 a.category-link[data-category="' + category + '"]').addClass('active');
            } else {
                $('.category-filter-list-v2 a.category-link[data-category=""]').addClass('active');
            }
            $('#filterCategory_full').val(category); // _full ID'li gizli input'u güncelle

            // Sıralamayı ayarla
            $('#sortByProductPage_full').val(sortBy);
            $('#sortByProductPage_sortOnly').val(sortBy);

            // Arama terimini ayarla
            $('#filterSearchTerm_full').val(searchTerm);
            // Eğer masaüstü kontrol barında da bir arama inputu varsa onu da güncellemelisiniz.

            console.log("DEBUG: Filtreler URL'den yüklendi: Category=", category, "SortBy=", sortBy, "SearchTerm=", searchTerm);
        }

        // Kategori Filtre Linkleri
        $(document).on('click', '.ajax-filter-link', function (e) {
            e.preventDefault();
            if (isLoading) return;
            var $this = $(this);

            $('.ajax-filter-link.active').removeClass('active');
            $this.addClass('active');

            var category = $this.data('category');
            // Her zaman "full" moddaki gizli input'u güncelle, çünkü kategoriler sadece sidebar/offcanvas'ta
            // ve getCurrentFilters bu _full ID'li input'u okuyabilir.
            $('#filterCategory_full').val(category);

            var offcanvasInstance = getOffcanvasInstance('productFiltersOffcanvas');
            if (offcanvasInstance && offcanvasInstance._element.classList.contains('show')) {
                offcanvasInstance.hide();
            }
            console.log("DEBUG: Kategori filtresi tıklandı, data-category:", category);
            loadProducts(1);
        });

        // Sıralama Dropdown
        $(document).on('change', '.ajax-sort-select', function () {
            if (isLoading) return;
            var sortBy = $(this).val();
            var changedSelectId = $(this).attr('id');

            // Her iki select'i de senkronize et
            if (changedSelectId === 'sortByProductPage_full') {
                $('#sortByProductPage_sortOnly').val(sortBy);
            } else if (changedSelectId === 'sortByProductPage_sortOnly') {
                $('#sortByProductPage_full').val(sortBy);
            }

            console.log("DEBUG: Sıralama değişti:", sortBy);
            loadProducts(1);
        });

        // Sayfalama Linkleri
        $(document).on('click', '#productListContainer .pagination .page-link[data-page]', function (e) {
            // ... (Bu kısım aynı kalabilir) ...
            e.preventDefault();
            if (isLoading) return;
            var pageNumber = $(this).data('page');
            if (pageNumber) {
                console.log("DEBUG: Sayfalama linki tıklandı, Sayfa:", pageNumber);
                loadProducts(pageNumber);
            }
        });

        // Tarayıcı Geri/İleri Butonları
        $(window).on('popstate', function (event) {
            console.log("DEBUG: Popstate event tetiklendi.", event.originalEvent.state);
            initializeFiltersFromUrl();

            var page = 1;
            if (event.originalEvent.state && event.originalEvent.state.filters) {
                page = event.originalEvent.state.filters.currentPage || 1;
            } else {
                var params = new URLSearchParams(window.location.search);
                page = parseInt(params.get('CurrentPage')) || 1;
            }
            loadProducts(page, false); // Geçmişi tekrar güncelleme (false)
        });

        initializeFiltersFromUrl();

        var initialTotalProducts = parseInt($('#productListContainer .product-grid').data('total-products'));
        if (!isNaN(initialTotalProducts) && $('#productListContainer .product-grid').length > 0) {
            var initialFilters = getCurrentFilters(); // Sayfa yüklendiğindeki filtreleri al
            updateProductCount(initialTotalProducts, initialFilters.searchTerm, initialFilters.category);
        } else if ($('#productListContainer .alert-danger').length === 0 && $('#productListContainer .empty-cart-message').length === 0 && $('#productListContainer .text-center p').length === 0) { // Eğer hata veya boş mesaj yoksa ve ürünler yüklenmemişse
            // Eğer URL'de filtreler varsa ve sayfa ilk kez yükleniyorsa (örn: bookmark'tan)
            // ve ürünler henüz sunucudan bu filtrelere göre gelmediyse, ilk yüklemeyi yap.
            // Ancak OnGetAsync zaten bunu yapıyor olmalı. Bu kontrolü dikkatli yapın.
            // Belki sadece ürün sayısı güncellenmeli.
        }

    }

});