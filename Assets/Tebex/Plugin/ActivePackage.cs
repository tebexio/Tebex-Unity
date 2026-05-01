using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class ActivePackage
    {
        public string txn_id = string.Empty;
        public DateTime date;
        public int quantity;
        public PackageInfo package = new PackageInfo();
    }
}
