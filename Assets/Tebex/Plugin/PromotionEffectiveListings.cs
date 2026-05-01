using System;
using System.Collections.Generic;

namespace Tebex.Plugin
{
    [Serializable]
    public class PromotionEffectiveListings
    {
        public string type = string.Empty;
        public List<int> packages = new List<int>();
        public List<int> categories = new List<int>();
    }
}
