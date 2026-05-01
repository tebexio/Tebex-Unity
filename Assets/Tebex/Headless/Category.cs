using System;
using System.Collections.Generic;

#nullable enable

namespace Tebex.Headless
{
    /// <summary>
    /// Categories are used to divide packages into sections within the store.
    /// </summary>
    [Serializable]
    public class Category
    {
        public int id = -1;
        public string name = string.Empty;
        public string slug = string.Empty;
        public string description = string.Empty;
        public PackageCategory? parent = null;
        public int order = -1;
        public string display_type = string.Empty;
        public bool tiered = false;
        public List<Package> packages = new List<Package>();
        public Tier? active_tier = null;
    }
}
