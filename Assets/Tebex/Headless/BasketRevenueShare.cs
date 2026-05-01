using System;

namespace Tebex.Headless
{
    /// <summary>
    /// Information about the basket's revenue share.
    /// </summary>
    [Serializable]
    public class BasketRevenueShare
    {
        public string wallet_ref = string.Empty;
        public float amount = -1.0f;
        public int gateway_fee_percent = -1;
    }
}
