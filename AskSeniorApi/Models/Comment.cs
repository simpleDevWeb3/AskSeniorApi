using Microsoft.AspNetCore.Mvc;

namespace AskSeniorApi.Models
{
    public class Comment : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
