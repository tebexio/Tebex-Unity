using System;

#nullable enable

namespace Tebex.Plugin
{
    [Serializable]
    public class CouponExpiry
    {
        public bool redeem_unlimited;
        public bool expire_never;
        public int limit;
        public DateTime? date;
    }
}
