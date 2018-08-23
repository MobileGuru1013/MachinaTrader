using Autofac;
using System.IO;
using MachinaTrader.Globals;
using MachinaTrader.Globals.Helpers;
using MachinaTrader.Plugin.Sample.Models;

namespace MachinaTrader.Plugin.Sample
{
    /// <summary>
    /// This is a simple sample how to create plugins
    /// For Development and Debuggung you should reference this project in MachinaTrader
    /// In Release a reference is not needed, plugins get load on runtime
    /// </summary>
    public class Sample : Module
    {
        public static string PluginName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType?.Namespace;
        public static PluginConfig PluginConfig = new PluginConfig();

        /// <summary>
        /// Add your init code here
        /// </summary>
        /// <param name="builder"></param>
        protected override void Load(ContainerBuilder builder)
        {

            Global.Logger.Information("Plugin " + PluginName + " initialized...");

            if (!Directory.Exists(Global.DataPath + "/" + PluginName))
            {
                Directory.CreateDirectory(Global.DataPath + "/" + PluginName);
            }

            PluginConfig = MergeObjects.MergeCsDictionaryAndSave(new PluginConfig(), Global.DataPath + "/" + PluginName + "/PluginConfig.json").ToObject<PluginConfig>();
        }
    }
}
