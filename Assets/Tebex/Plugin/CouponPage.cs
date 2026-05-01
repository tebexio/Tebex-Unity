using System;
using System.Collections.Generic;

namespace Tebex.Plugin
{
    [Serializable]
    public class CouponPage
    {
        public CouponPagination pagination = new CouponPagination();
        public List<Coupon> data = new List<Coupon>();
    }
}
