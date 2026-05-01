using System;
using System.Collections.Generic;

namespace Tebex.Headless
{
    /// <summary>
    /// A data container class for a list of categories.
    /// </summary>
    [Serializable]
    public class WrappedCategories
    {
        public List<Category> data = new List<Category>();
    }
}
