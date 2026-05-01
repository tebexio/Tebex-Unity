using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class Coupon
    {
        public int id;
        public string code = string.Empty;
        public PromotionEffectiveListings effective = new PromotionEffectiveListings();
        public PromotionDiscount discount = new PromotionDiscount();
        public CouponExpiry expire = new CouponExpiry();
        public string basket_type = string.Empty;
        public DateTime start_date;
        public int user_limit;
        public int minimum;
        public string username = string.Empty;
        public string note = string.Empty;
    }
}
