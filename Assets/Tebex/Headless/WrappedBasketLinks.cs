using System;
using System.Collections.Generic;

namespace Tebex.Headless
{
    /// <summary>
    /// An array of authorization links for a Basket.
    /// </summary>
    [Serializable]
    public class WrappedBasketLinks
    {
        public List<BasketAuthLink> data = new List<BasketAuthLink>();
    }
}
