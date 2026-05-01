using System;

namespace Tebex.Headless
{
    /// <summary>
    /// Qualities about a package within a basket.
    /// </summary>
    [Serializable]
    public class InnerPackageMeta
    {
        public int quantity = -1;
        public float price = -1.0f;
        public string gift_username_id = string.Empty;
        public string gift_username = string.Empty;
        public string gift_image = string.Empty;
    }
}
