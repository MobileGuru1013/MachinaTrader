using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.DrawingCore;
using System.DrawingCore.Imaging;
using System.Linq;
using MachinaTrader.Globals.Helpers;
using System.IO;
using MachinaTrader.Globals;
using Microsoft.AspNetCore.Authorization;
using System.Runtime.InteropServices;
using System.Text;
using MachinaTrader.Models;

namespace MachinaTrader.Controllers
{
    [Authorize, Route("api/core/")]
    public class ApiCore : Controller
    {
        [HttpGet]
        [Route("navigation")]
        public ActionResult Navigation()
        {
            JObject pluginMenu = new JObject();

            JToken htmlPagesSearchFolders = (Global.CoreRuntime["Plugins"]).DeepClone();

            //Add Core Folder
            htmlPagesSearchFolders["Core"] = new JObject
            {
                ["Enabled"] = true,
                ["WwwRoot"] = Global.AppPath + "/wwwroot"
            };

            foreach (var jToken in htmlPagesSearchFolders)
            {
                var plugin = (JProperty)jToken;
                //string pluginName = (plugin.Name).Replace("MachinaCore.Plugin.", "");
                string pluginName = plugin.Name;
                if (htmlPagesSearchFolders[plugin.Name] == null || (bool)htmlPagesSearchFolders[plugin.Name]["Enabled"] == false)
                {
                    Global.Logger.Information(plugin.Name + @" not found -> Ignoring");
                    continue;
                }

                if (!Directory.Exists((string)htmlPagesSearchFolders[plugin.Name]["WwwRoot"] + "/pages"))
                {
                    Global.Logger.Information(plugin.Name + @"has no pages folder -> Ignoring");
                    continue;
                }

                if (htmlPagesSearchFolders[plugin.Name]["WwwRoot"] != null)
                {
                    JObject pageMenuConfig = new JObject();

                    String[] pluginHtmlFiles = Directory.GetFiles((string)htmlPagesSearchFolders[plugin.Name]["WwwRoot"] + "/pages", "*.html", SearchOption.TopDirectoryOnly);

                    JObject pluginMenuConfig =
                        new JObject
                        {
                            ["menuTitle"] = (plugin.Name).Replace("MachinaCore.Plugin.", ""),
                            ["menuEnabled"] = true,
                            ["menuFolder"] = true,
                            ["menuAutoOpen"] = false,
                            ["menuIconClass"] = "fas fa-folder",
                            ["menuOrder"] = 99
                        };

                    if (System.IO.File.Exists((string)htmlPagesSearchFolders[plugin.Name]["WwwRoot"] + "/pages/__plugin__.json"))
                    {
                        JObject pluginMenuConfigFile = JObject.Parse(System.IO.File.ReadAllText((string)htmlPagesSearchFolders[plugin.Name]["WwwRoot"] + "/pages/__plugin__.json"));
                        pluginMenuConfig = MergeObjects.CombineJObjects(pluginMenuConfig, pluginMenuConfigFile);
                    }

                    if (!(bool)pluginMenuConfig["menuEnabled"])
                    {
                        continue;
                    }

                    int countPages = 0;
                    string countCurrentName = "";

                    foreach (string fileName in pluginHtmlFiles)
                    {
                        string shortName = pluginName + "_" + Path.GetFileName(fileName);
                        bool useUserDefinedParent = false;

                        if (System.IO.File.Exists(fileName.Replace(".html", ".json")))
                        {
                            JObject pluginConfigJsonFile = JObject.Parse(System.IO.File.ReadAllText(fileName.Replace(".html", ".json")));

                            if (pluginConfigJsonFile["menuEnabled"] == null)
                            {
                                pluginConfigJsonFile["menuEnabled"] = true;
                            }

                            if ((bool)pluginConfigJsonFile["menuEnabled"] == false)
                            {
                                continue;
                            }

                            pageMenuConfig[shortName] = new JObject
                            {
                                ["menuTitle"] = Path.GetFileName(fileName),
                                ["menuHyperlink"] = plugin.Name + "/pages/" + Path.GetFileName(fileName),
                                ["menuEnabled"] = true,
                                ["menuIconClass"] = "fas fa-star",
                                ["menuCustomFolder"] = "",
                                ["menuOrder"] = 99,
                                ["menuAjaxLoad"] = true,
                                ["menuRuntimeIsFolder"] = false
                            };

                            pageMenuConfig[shortName] = MergeObjects.CombineJObjects((JObject)pageMenuConfig[shortName], pluginConfigJsonFile);

                            if (plugin.Name == "Core")
                            {
                                pageMenuConfig[shortName]["menuHyperlink"] = "pages/" + Path.GetFileName(fileName);
                                pageMenuConfig[shortName]["menuOrder"] = 1;
                            }

                            if ((string)pageMenuConfig[shortName]["menuCustomFolder"] != "")
                            {
                                useUserDefinedParent = true;
                            }

                            if (useUserDefinedParent)
                            {
                                //Create Category of not exist
                                if (pluginMenu[(string)pageMenuConfig[shortName]["menuCustomFolder"]] == null)
                                {
                                    pluginMenu[(string)pageMenuConfig[shortName]["menuCustomFolder"]] =
                                        new JObject
                                        {
                                            ["menuOrder"] = 99,
                                            ["menuIconClass"] = pluginMenuConfig["menuIconClass"],
                                            ["menuTitle"] = (string)pageMenuConfig[shortName]["menuCustomFolder"],
                                            ["menuEnabled"] = pluginMenuConfig["menuEnabled"],
                                            ["menuRuntimeIsFolder"] = true
                                        };
                                }
                                pluginMenu[(string)pageMenuConfig[shortName]["menuCustomFolder"]][shortName] = pageMenuConfig[shortName];
                                pageMenuConfig.Remove(shortName);
                                continue;
                            }

                            countPages = countPages + 1;
                            countCurrentName = shortName;

                            if (plugin.Name == "Core")
                            {
                                pluginMenu[shortName] = pageMenuConfig[shortName];
                                pluginMenu[shortName]["menuOrder"] = 1;
                                pageMenuConfig.Remove(shortName);
                                continue;
                            }

                            if (!(bool)pluginMenuConfig["menuFolder"])
                            {
                                pluginMenu[shortName] = pageMenuConfig[shortName];
                                pluginMenu[shortName]["menuRuntimeIsFolder"] = false;
                                pageMenuConfig.Remove(shortName);
                            }
                        }
                    }

                    if (pageMenuConfig.HasValues)
                    {
                        if (countPages == 1)
                        {
                            pluginMenu[countCurrentName] = pageMenuConfig[countCurrentName];
                        }
                        else
                        {
                            pluginMenu[pluginName] = pageMenuConfig;
                            pluginMenu[pluginName]["menuOrder"] = pluginMenuConfig["menuOrder"];
                            pluginMenu[pluginName]["menuIconClass"] = pluginMenuConfig["menuIconClass"];
                            pluginMenu[pluginName]["menuTitle"] = pluginMenuConfig["menuTitle"];
                            pluginMenu[pluginName]["menuEnabled"] = pluginMenuConfig["menuEnabled"];
                            pluginMenu[pluginName]["menuRuntimeIsFolder"] = true;
                        }
                    }
                }
            }

            //Sort Inner Nodes
            foreach (var pluginFolderInner in pluginMenu)
            {
                JObject currentValue = (JObject)pluginFolderInner.Value;

                JObject sortedPluginFolderInner = new JObject(currentValue.Properties()
                    .Where(obj => currentValue[obj.Name].Type == JTokenType.Object)
                    .OrderBy(obj => (int)currentValue[obj.Name]["menuOrder"])
                    .ThenBy(obj => (string)currentValue[obj.Name]["menuTitle"], StringComparer.OrdinalIgnoreCase)
                );

                if (sortedPluginFolderInner.HasValues)
                {
                    //We rewrite this after sort -> Save old values
                    int currentMenuOrder = (int)pluginMenu[pluginFolderInner.Key]["menuOrder"];
                    string currentPluginMenuIconClass = (string)pluginMenu[pluginFolderInner.Key]["menuIconClass"];
                    string currentName = (string)pluginMenu[pluginFolderInner.Key]["menuTitle"];
                    bool currentEnableMenu = (bool)pluginMenu[pluginFolderInner.Key]["menuEnabled"];
                    bool menuRuntimeIsFolder = (bool)pluginMenu[pluginFolderInner.Key]["menuRuntimeIsFolder"];
                    pluginMenu[pluginFolderInner.Key] = sortedPluginFolderInner;
                    pluginMenu[pluginFolderInner.Key]["menuOrder"] = currentMenuOrder;
                    pluginMenu[pluginFolderInner.Key]["menuIconClass"] = currentPluginMenuIconClass;
                    pluginMenu[pluginFolderInner.Key]["menuTitle"] = currentName;
                    pluginMenu[pluginFolderInner.Key]["menuEnabled"] = currentEnableMenu;
                    pluginMenu[pluginFolderInner.Key]["menuRuntimeIsFolder"] = menuRuntimeIsFolder;
                }
            }

            //Sort by main values
            JObject pluginMenuSorted = new JObject(pluginMenu.Properties()
                .OrderBy(obj => (int)pluginMenu[obj.Name]["menuOrder"])
                .ThenBy(obj => (string)pluginMenu[obj.Name]["menuTitle"], StringComparer.OrdinalIgnoreCase)
            );

            return new JsonResult(pluginMenuSorted);
        }

        [HttpGet]
        [Route("plugins")]
        public ActionResult Plugins()
        {
            return new JsonResult(Global.CoreRuntime["Plugins"]);
        }

        [HttpGet]
        [Route("avatar")]
        public FileContentResult Get()
        {
            if (Global.RuntimeSettings.Os == "Windows")
            {
                using (var ms = new MemoryStream())
                {
                    Image userAvatarImage = GetUserAvatar.GetUserTile(Global.RuntimeSettings.UserName);
                    userAvatarImage.Save(ms, ImageFormat.Jpeg);
                    if (!ms.TryGetBuffer(out var buffer)) throw new ArgumentException();
                    return File(buffer.Array, "image/png");
                }
            }
            byte[] filedata = System.IO.File.ReadAllBytes(Global.AppPath + "/wwwroot/img/user_avatar_default.png");
            return File(filedata, "image/png");
        }
    }

    public class GetUserAvatar
    {
        [DllImport("shell32.dll", EntryPoint = "#261", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void GetUserTilePath(
            string username,
            UInt32 whatever, // 0x80000000
            StringBuilder picpath, int maxLength);

        public static string GetUserTilePath(string username)
        {
            // username: use null for current user
            var sb = new StringBuilder(1000);
            GetUserTilePath(username, 0x80000000, sb, sb.Capacity);
            return sb.ToString();
        }

        public static Image GetUserTile(string username)
        {
            return Image.FromFile(GetUserTilePath(username));
        }
    }
}
