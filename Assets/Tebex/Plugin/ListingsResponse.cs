using System;
using System.Collections.Generic;

namespace Tebex.Plugin
{
    /// <summary>
    /// Response to /listing on Plugin API.
    /// </summary>
    [Serializable]
    public class ListingsResponse
    {
        public List<Category> categories = new List<Category>();
    }
}
