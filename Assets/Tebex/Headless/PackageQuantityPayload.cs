using System;

namespace Tebex.Headless
{
    /// <summary>
    /// Request data for setting a package's quantity.
    /// </summary>
    [Serializable]
    public class PackageQuantityPayload
    {
        public int quantity;

        public PackageQuantityPayload(int qty)
        {
            quantity = qty;
        }
    }
}
