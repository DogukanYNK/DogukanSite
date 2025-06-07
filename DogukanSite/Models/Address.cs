namespace DogukanSite.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string AddressTitle { get; set; } // "Ev Adresim", "İş Adresim" etc.
        public string ContactName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public bool IsDefaultShipping { get; set; } = false;
        public bool IsDefaultBilling { get; set; } = false;


        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}