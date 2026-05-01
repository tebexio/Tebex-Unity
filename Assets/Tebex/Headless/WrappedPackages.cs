using System;
using System.Collections.Generic;

namespace Tebex.Headless
{
    /// <summary>
    /// A data container class for a list of packages.
    /// </summary>
    [Serializable]
    public class WrappedPackages
    {
        public List<Package> data = new List<Package>();
    }
}
