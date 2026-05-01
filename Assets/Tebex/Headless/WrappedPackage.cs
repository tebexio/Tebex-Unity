using System;

namespace Tebex.Headless
{
    /// <summary>
    /// A data container class for a single package.
    /// </summary>
    [Serializable]
    public class WrappedPackage
    {
        public Package data = new Package();
    }
}
