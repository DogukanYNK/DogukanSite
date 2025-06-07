namespace DogukanSite.Models
{
    public enum OrderStatus
    {
        Pending,        // Beklemede
        Processing,     // İşleniyor
        Shipped,        // Kargoya Verildi
        Delivered,      // Teslim Edildi
        Cancelled,      // İptal Edildi
        Refunded        // İade Edildi
    }
}