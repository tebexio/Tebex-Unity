using System;
using System.Collections.Generic;

namespace Tebex.Headless
{
    /// <summary>
    /// A Package is a purchasable item in the store.
    /// </summary>
    [Serializable]
    public class Package
    {
        public int id;
        public string name = string.Empty;
        public string description = string.Empty;
        public string image = string.Empty;
        public string type = string.Empty;
        public PackageCategory category = new PackageCategory();
        public float base_price;
        public float sales_tax;
        public float total_price;
        public string currency = string.Empty;
        public float prorate_price;
        public float discount;
        public bool disable_quantity;
        public bool disable_gifting;
        public string created_at = string.Empty;
        public string updated_at = string.Empty;
        public int order = -1;
        public List<PackageVariable> variables = new List<PackageVariable>();
    }
}
