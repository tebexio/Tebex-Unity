using System;
using System.Collections.Generic;

#nullable enable

namespace Tebex.Headless
{
    /// <summary>
    /// Request data for adding a package to a basket.
    /// </summary>
    [Serializable]
    public class AddPackagePayload
    {
        public int package_id;
        public int quantity;
        public Dictionary<string, string>? variable_data;

        public AddPackagePayload(int packageId, int qty = 1, Dictionary<string, string>? variableData = null)
        {
            package_id = packageId;
            quantity = qty;
            variable_data = variableData;
        }
    }
}
