using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MachinaTrader.Globals;
using MachinaTrader.Models;
using Newtonsoft.Json.Linq;

namespace MachinaTrader.Utility
{
    public class WebNavigation
    {
        public static List<ModelNavigation> Get()
        {
            List<ModelNavigation> navItems = new List<ModelNavigation>();
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
                if (htmlPagesSearchFolders[plugin.Name] == null || (bool)htmlPagesSearchFolders[plugin.Name]["Enabled"] == false)
                {
                    Console.WriteLine(plugin.Name + @" not found -> Ignoring");
                    continue;
                }

                if (!Directory.Exists((string)htmlPagesSearchFolders[plugin.Name]["WwwRoot"] + "/views"))
                {
                    Console.WriteLine(plugin.Name + @"has no pages folder -> Ignoring");
                    continue;
                }

                ModelNavigation currentNavigation =
                    new ModelNavigation
                    {
                        MenuTitle = (plugin.Name).Replace("MachinaTrader.Plugin.", ""),
                        MenuEnabled = true,
                        MenuFolder = true,
                        MenuAutoOpen = false,
                        MenuIconClass = "fal fa-folder",
                        MenuOrder = 99
                    };


                if (File.Exists((string)htmlPagesSearchFolders[plugin.Name]["WwwRoot"] + "/views/__plugin__.json"))
                {

                    JObject pluginMenuConfigFile = JObject.Parse(File.ReadAllText((string)htmlPagesSearchFolders[plugin.Name]["WwwRoot"] + "/views/__plugin__.json"));
                    if (pluginMenuConfigFile["menuTitle"] != null)
                    {
                        currentNavigation.MenuTitle = (string)pluginMenuConfigFile["menuTitle"];
                    }

                    if (pluginMenuConfigFile["menuEnabled"] != null)
                    {
                        currentNavigation.MenuEnabled = (bool)pluginMenuConfigFile["menuEnabled"];
                    }

                    if (pluginMenuConfigFile["menuFolder"] != null)
                    {
                        currentNavigation.MenuFolder = (bool)pluginMenuConfigFile["menuFolder"];
                    }
                    if (pluginMenuConfigFile["menuOrder"] != null)
                    {
                        currentNavigation.MenuOrder = (int)pluginMenuConfigFile["menuOrder"];
                    }
                }

                if (htmlPagesSearchFolders[plugin.Name]["WwwRoot"] != null)
                {
                    if (!currentNavigation.MenuEnabled)
                    {
                        continue;
                    }

                    String[] pluginHtmlFiles = Directory.GetFiles((string)htmlPagesSearchFolders[plugin.Name]["WwwRoot"] + "/views", "*.html", SearchOption.TopDirectoryOnly);

                    foreach (string fileName in pluginHtmlFiles)
                    {
                        if (File.Exists(fileName.Replace(".html", ".json")))
                        {
                            JObject pluginConfigJsonFile = JObject.Parse(File.ReadAllText(fileName.Replace(".html", ".json")));

                            if ((bool)pluginConfigJsonFile["menuEnabled"] == false)
                            {
                                continue;
                            }

                            ModelNavigation currentNavigationChild =
                                new ModelNavigation
                                {
                                    MenuTitle = Path.GetFileName(fileName),
                                    MenuHyperlink = plugin.Name + "/views/" + Path.GetFileName(fileName)
                                };
                            if (plugin.Name == "Core")
                            {
                                currentNavigationChild.MenuHyperlink = "views/" + Path.GetFileName(fileName);
                            }
                            if (pluginConfigJsonFile["menuTitle"] != null)
                            {
                                currentNavigationChild.MenuTitle = (string)pluginConfigJsonFile["menuTitle"];
                            }
                            if (pluginConfigJsonFile["menuIconClass"] != null)
                            {
                                currentNavigationChild.MenuIconClass = (string)pluginConfigJsonFile["menuIconClass"];
                            }
                            if (pluginConfigJsonFile["menuOrder"] != null)
                            {
                                currentNavigationChild.MenuOrder = (int)pluginConfigJsonFile["menuOrder"];
                            }
                            if (pluginConfigJsonFile["menuCustomFolder"] != null)
                            {
                                currentNavigationChild.MenuCustomFolder = (string)pluginConfigJsonFile["menuCustomFolder"];
                            }
                            currentNavigation.MenuChilds.Add(currentNavigationChild);
                        }
                    }
                }
                navItems.Add(currentNavigation);
            }

            List<ModelNavigation> navItemsOrdered = new List<ModelNavigation>();
            foreach (var items in navItems)
            {
                if (items.MenuChilds.Count > 1 && items.MenuFolder && items.MenuTitle != "Core")
                {
                    var currentModel = navItemsOrdered.FirstOrDefault(a => a.MenuTitle == items.MenuTitle);
                    if (!string.IsNullOrEmpty(items.MenuCustomFolder))
                    {
                        currentModel = navItemsOrdered.FirstOrDefault(a => a.MenuCustomFolder == items.MenuCustomFolder);
                    }

                    if (currentModel == null)
                    {
                        ModelNavigation currentModelNew = new ModelNavigation
                        {
                            MenuTitle = items.MenuTitle
                        };
                        navItemsOrdered.Add(currentModelNew);
                    }

                    foreach (var item in items.MenuChilds)
                    {
                        currentModel = navItemsOrdered.FirstOrDefault(a => a.MenuTitle == items.MenuTitle);
                        if (!string.IsNullOrEmpty(item.MenuCustomFolder))
                        {
                            currentModel = navItemsOrdered.FirstOrDefault(a => a.MenuTitle == item.MenuCustomFolder);
                            if (currentModel == null)
                            {
                                ModelNavigation currentModelNew = new ModelNavigation
                                {
                                    MenuTitle = item.MenuCustomFolder
                                };
                                navItemsOrdered.Add(currentModelNew);
                                currentModel = navItemsOrdered.FirstOrDefault(a => a.MenuTitle == item.MenuCustomFolder);
                            }
                        }

                        currentModel?.MenuChilds.Add(item);
                    }
                }
                else
                {
                    foreach (var item in items.MenuChilds)
                    {
                        if (!string.IsNullOrEmpty(item.MenuCustomFolder))
                        {
                            var currentModel = navItemsOrdered.FirstOrDefault(a => a.MenuTitle == item.MenuCustomFolder);
                            if (currentModel == null)
                            {
                                ModelNavigation currentModelNew = new ModelNavigation
                                {
                                    MenuTitle = item.MenuCustomFolder
                                };
                                navItemsOrdered.Add(currentModelNew);
                                currentModel = navItemsOrdered.FirstOrDefault(a => a.MenuTitle == item.MenuCustomFolder);
                            }

                            currentModel?.MenuChilds.Add(item);
                        }
                        else
                        {
                            navItemsOrdered.Add(item);
                        }
                    }
                }
            }

            return navItemsOrdered;

        }
    }
}
