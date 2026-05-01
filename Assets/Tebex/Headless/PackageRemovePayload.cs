using System;

namespace Tebex.Headless
{
    /// <summary>
    /// Request data for removing a package from a basket.
    /// </summary>
    [Serializable]
    public class PackageRemovePayload
    {
        public int package_id;

        public PackageRemovePayload(int packageId)
        {
            package_id = packageId;
        }
    }
}
