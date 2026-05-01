using System;

namespace Tebex.Headless
{
    /// <summary>
    /// A data container class for a single category.
    /// </summary>
    [Serializable]
    public class WrappedCategory
    {
        public Category data = new Category();
    }
}
