using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Plugin.Sample
{
    /// <summary>
    /// Sample for web api
    /// You can call this api with /api/sample/sample
    /// </summary>
    [Authorize, Route("api/sample/")]
    public class ApiConfig : Controller
    {
        [HttpGet]
        [Route("sample")]
        public ActionResult Get()
        {
            return new JsonResult(new JObject());
        }

    }

}
