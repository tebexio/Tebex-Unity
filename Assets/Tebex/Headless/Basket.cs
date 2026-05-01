using System;
using System.Collections.Generic;

namespace Tebex.Headless
{
    /// <summary>
    /// The primary Basket used in the Headless API.
    /// </summary>
    [Serializable]
    public class Basket
    {
        public string ident = string.Empty;
        public bool complete;
        public string email = string.Empty;
        public string username = string.Empty;
        public List<BasketCoupon> coupons = new List<BasketCoupon>();
        public List<BasketGiftCard> gift_cards = new List<BasketGiftCard>();
        public string creator_code = string.Empty;
        public string cancel_url = string.Empty;
        public string complete_url = string.Empty;
        public bool complete_auto_redirect;
        public string country = string.Empty;
        public string ip = string.Empty;
        public int? username_id;
        public float base_price;
        public float sales_tax;
        public float total_price;
        public string currency = string.Empty;
        public List<BasketPackage> packages = new List<BasketPackage>();
        public Dictionary<string, string> custom = new Dictionary<string, string>();
        public BasketLinks links = new BasketLinks();
    }
}
