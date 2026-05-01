using System;

namespace Tebex.Headless
{
    /// <summary>
    /// A Basket's external links for payment and checkout.
    /// </summary>
    [Serializable]
    public class BasketLinks
    {
        public string payment = string.Empty;
        public string checkout = string.Empty;
    }
}
