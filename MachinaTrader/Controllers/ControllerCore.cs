using Microsoft.AspNetCore.Mvc;

namespace MachinaTrader.Controllers
{
    [Route("favicon.ico")]
    public class FaviconController : Controller
    {
        [HttpGet]
        public IActionResult File()
        {
            return File("~/img/favicon.ico", "image/x-icon");
        }
    }
}
