using System;

namespace Tebex.Headless
{
    /// <summary>
    /// A coupon placed on a Basket.
    /// </summary>
    [Serializable]
    public class BasketCoupon
    {
        public string coupon_code;

        public BasketCoupon(string couponCode)
        {
            coupon_code = couponCode;
        }
    }
}
