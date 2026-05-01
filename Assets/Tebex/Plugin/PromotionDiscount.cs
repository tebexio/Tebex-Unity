using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class PromotionDiscount
    {
        public string type = string.Empty;
        public double percentage;
        public double value;
    }
}
