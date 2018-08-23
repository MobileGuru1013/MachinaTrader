using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using MachinaTrader.Globals.Helpers;
using MachinaTrader.Models;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Models;
using Microsoft.AspNetCore.Authorization;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/config/")]
    public class ApiConfig : Controller
    {
        [HttpGet]
        [Route("mainConfig")]
        public ActionResult GetMainConfig()
        {
            return new JsonResult(Global.Configuration);
        }

        [HttpPost]
        [Route("mainConfig")]
        public void PostMainConfig([FromBody]JObject data)
        {
            try
            {
                Global.Configuration = MergeObjects.MergeCsDictionaryAndSave(Global.Configuration, Global.DataPath + "/MainConfig.json", data).ToObject<MainConfig>();
            }
            catch (Exception ex)
            {
                Global.Logger.Error(@"Can not save config file: " + ex);
            }
        }

        [HttpGet]
        [Route("runtime")]
        public ActionResult GetRuntime()
        {
            return new JsonResult(Global.RuntimeSettings);
        }
    }
}
