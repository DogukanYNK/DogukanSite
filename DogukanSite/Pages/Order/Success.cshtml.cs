using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DogukanSite.Pages.Order
{
    public class SuccessModel : PageModel
    {
        public int OrderId { get; set; }

        public void OnGet(int id)
        {
            OrderId = id;
        }
    }
}
