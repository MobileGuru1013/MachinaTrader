using System.Collections.Generic;

namespace MachinaTrader.Models
{
    public class ModelNavigation
    {
        public string MenuTitle { get; set; }
        public string MenuHyperlink { get; set; } = "";
        public bool MenuEnabled { get; set; } = true;
        public bool MenuFolder { get; set; } = true;
        public bool MenuAutoOpen { get; set; } = false;

        public string MenuIconClass { get; set; } = "fas fa-star";
        public string MenuCustomFolder { get; set; } = null;
        public int MenuOrder { get; set; } = 99;
        public bool MenuRuntimeIsFolder { get; set; } = false;
        public List<ModelNavigation> MenuChilds { get; set; } = new List<ModelNavigation>{};
    }
}
