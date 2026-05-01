using System;
using System.Collections.Generic;

#nullable enable

namespace Tebex.Plugin
{
    /// <summary>
    /// Represents a category within the webstore.
    /// </summary>
    [Serializable]
    public class Category
    {
        public int id;
        public int order;
        public string name = string.Empty;
        public bool only_subcategories;
        public List<Category> subcategories = new List<Category>();
        public List<Package> packages = new List<Package>();
        public object? gui_item;
    }
}
