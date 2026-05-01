using System;
using System.Collections.Generic;

namespace Tebex.Headless
{
    /// <summary>
    /// The variable_data variables associated with a package.
    /// </summary>
    [Serializable]
    public class PackageVariable
    {
        public int id = -1;
        public string identifier = string.Empty;
        public string description = string.Empty;
        public int min_length;
        public int max_length;
        public string type = string.Empty;
        public bool allow_colors;
        public List<PackageVariableOption> options = new List<PackageVariableOption>();
    }
}
