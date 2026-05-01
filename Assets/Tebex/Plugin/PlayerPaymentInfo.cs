using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class PlayerPaymentInfo
    {
        public string transaction_id = string.Empty;
        public long time;
        public double price;
        public string currency = string.Empty;
        public string status = string.Empty;
    }
}
