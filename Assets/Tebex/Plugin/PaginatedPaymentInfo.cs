using System;
using System.Collections.Generic;

#nullable enable

namespace Tebex.Plugin
{
    [Serializable]
    public class PaginatedPaymentInfo
    {
        public int total = -1;
        public int per_page = -1;
        public int current_page = -1;
        public int last_page = -1;
        public string? next_page_url = string.Empty;
        public string? previous_page_url = string.Empty;
        public int from = -1;
        public int to = -1;
        public List<PaymentDetails> data = new List<PaymentDetails>();
    }
}
