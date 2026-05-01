using System;

namespace Tebex.Headless
{
    /// <summary>
    /// An option for a package's variable_data variables.
    /// </summary>
    [Serializable]
    public class PackageVariableOption
    {
        public int id = -1;
        public string name = string.Empty;
        public string value = string.Empty;
        public double price;
        public double percentage;
    }
}
