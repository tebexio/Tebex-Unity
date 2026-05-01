using System;

namespace Tebex.Headless
{
    /// <summary>
    /// Request data for updating a tier.
    /// </summary>
    [Serializable]
    public class UpdateTierPayload
    {
        public int package_id;

        public UpdateTierPayload(int packageId)
        {
            package_id = packageId;
        }
    }
}
