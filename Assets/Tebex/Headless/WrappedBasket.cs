using System;

namespace Tebex.Headless
{
    /// <summary>
    /// A data container class that wraps a single instance of a Basket.
    /// </summary>
    [Serializable]
    public class WrappedBasket
    {
        public Basket data = new Basket();
    }
}
