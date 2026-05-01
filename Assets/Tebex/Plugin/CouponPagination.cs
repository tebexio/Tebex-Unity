using System;

#nullable enable

namespace Tebex.Plugin
{
    [Serializable]
    public class CouponPagination
    {
        public int total_results;
        public int current_page;
        public int last_page;
        public string? previous;
        public string? next;
    }
}
