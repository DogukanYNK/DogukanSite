// DogukanSite/Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations; // DataAnnotations için eklendi

namespace DogukanSite.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Gerekirse özel alanlar eklenebilir
        [PersonalData] // Bu verinin kişisel veri olduğunu belirtir (GDPR vb. için)
        [StringLength(50)]
        public string? FirstName { get; set; } // Nullable olabilir veya Required eklenebilir

        [PersonalData]
        [StringLength(50)]
        public string? LastName { get; set; } // Nullable olabilir veya Required eklenebilir

        // İleride kayıtlı kullanıcılar için varsayılan adres bilgileri de buraya eklenebilir:
        // public string? DefaultAddressLine1 { get; set; }
        // public string? DefaultCity { get; set; }
        // public string? DefaultDistrict { get; set; }
        // public string? DefaultPostalCode { get; set; }
    }
}