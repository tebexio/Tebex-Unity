using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class Sale
    {
        public int id;
        public string name = string.Empty;
        public PromotionEffectiveListings effective = new PromotionEffectiveListings();
        public PromotionDiscount discount = new PromotionDiscount();
        public int start;
        public int expire;
        public int order;
    }
}
