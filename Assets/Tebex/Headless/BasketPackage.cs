using System;

namespace Tebex.Headless
{
    /// <summary>
    /// A package currently in a Basket.
    /// </summary>
    [Serializable]
    public class BasketPackage
    {
        public int id = -1;
        public string name = string.Empty;
        public string description = string.Empty;
        public InnerPackageMeta in_basket = new InnerPackageMeta();
    }
}
