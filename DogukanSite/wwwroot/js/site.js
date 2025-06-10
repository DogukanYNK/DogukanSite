// wwwroot/js/site.js
// Bu dosya, tüm sitede ortak olan genel işlevleri barındırır.
$(document).ready(function () {
    console.log("DEBUG: Ana site.js dosyası çalıştı.");

    // Tüm POST AJAX isteklerine sahteciliğe karşı koruma token'ını otomatik olarak ekler.
    $.ajaxPrefilter(function (options, originalOptions, jqXHR) {
        if (options.type.toUpperCase() === "POST") {
            var token = $('meta[name="RequestVerificationToken"]').attr('content');
            if (token) {
                jqXHR.setRequestHeader("RequestVerificationToken", token);
            }
        }
    });

    // Yukarı Çık Butonu
    var scrollTopBtn = document.getElementById("scrollTopBtn");
    if (scrollTopBtn) {
        window.onscroll = function () {
            if (document.body.scrollTop > 150 || document.documentElement.scrollTop > 150) {
                // Doğrudan stil vermek yerine sadece class ekle
                scrollTopBtn.classList.add("visible");
            } else {
                // Sadece class'ı kaldır
                scrollTopBtn.classList.remove("visible");
            }
        };
        // Tıklama olayı aynı kalabilir
        scrollTopBtn.addEventListener('click', function () {
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    // Mobil menü (Off-Canvas) içindeki bir linke tıklanınca menüyü kapatma
    $('#mainOffcanvas .offcanvas-body').on('click', 'a', function () {
        var offcanvasElement = document.getElementById('mainOffcanvas');
        if (offcanvasElement) {
            var offcanvasInstance = bootstrap.Offcanvas.getInstance(offcanvasElement);
            if (offcanvasInstance) {
                offcanvasInstance.hide();
            }
        }
    });
});