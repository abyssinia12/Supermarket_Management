using System.Collections.Generic;

namespace SuperMarket.Models.ViewModels
{
    public class MenuIndexViewModel
    {
        public List<Menu> Menus { get; set; } = new();
        public List<string> Categories { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public string? SearchString { get; set; }
        public bool? IsMenuOnly { get; set; }
        public int TotalItems { get; set; }
        public int MenuCount { get; set; }
        public int ActiveCount { get; set; }
        public int TraceableCount { get; set; }
    }
}

