using System;

namespace Tebex.Plugin
{
    [Serializable]
    public class CustomerPackagePurchaseRecord
    {
        public string transaction_id = string.Empty;
        public DateTime date;
        public int quantity;
        public PurchaseRecordPackageInfo purchase_record_package = new PurchaseRecordPackageInfo();
    }
}
