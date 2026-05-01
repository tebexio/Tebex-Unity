using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class GiftCardBalance
    {
        public double starting = -1.0d;
        public double remaining = -1.0d;
        public string currency = string.Empty;
    }
}
