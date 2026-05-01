using System;
using System.Collections.Generic;

#nullable enable

namespace Tebex.Plugin
{
    /// <summary>
    /// Represents a purchasable package on the store.
    /// </summary>
    [Serializable]
    public class Package
    {
        public int id;
        public string name = string.Empty;
        public int order = -1;
        public string image = string.Empty;
        public double price = -1.0d;
        public PackageSaleData? sale;
        public int expiry_length;
        public string expiry_period = string.Empty;
        public string type = string.Empty;
        public Category category = new Category();
        public int global_limit;
        public string global_limit_period = string.Empty;
        public int user_limit;
        public string user_limit_period = string.Empty;
        public List<Server>? servers;
        public List<object>? required_packages;
        public bool require_any;
        public bool create_giftcard;
        public string show_until = string.Empty;
        public string gui_item = string.Empty;
        public bool disabled;
        public bool disable_quantity;
        public bool custom_price;
        public bool choose_server;
        public bool limit_expires;
        public bool inherit_commands;
        public bool variable_giftcard;
        public string description = string.Empty;

        public string GetFriendlyPayFrequency()
        {
            switch (type)
            {
                case "single": return "One-Time";
                case "subscription": return $"Each {expiry_length} {expiry_period}";
                default: return "???";
            }
        }
    }
}
