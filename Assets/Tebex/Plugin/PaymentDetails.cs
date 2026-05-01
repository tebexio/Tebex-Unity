using System;
using System.Collections.Generic;

#nullable enable

namespace Tebex.Plugin
{
    [Serializable]
    public class PaymentDetails
    {
        public int id;
        public double amount;
        public DateTime date;
        public Currency currency = new Currency();
        public GatewayInfo gateway = new GatewayInfo();
        public string status = string.Empty;
        public string email = string.Empty;
        public PaidPlayer player = new PaidPlayer();
        public List<PackageInfo> packages = new List<PackageInfo>();
        public List<NoteInfo> notes = new List<NoteInfo>();
        public string? creator_code;
    }
}
