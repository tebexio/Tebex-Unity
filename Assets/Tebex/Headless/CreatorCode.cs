using System;

namespace Tebex.Headless
{
    /// <summary>
    /// A code that triggers affiliation of the basket with a platform creator.
    /// </summary>
    [Serializable]
    public class CreatorCode
    {
        public string code;

        public CreatorCode(string c)
        {
            code = c;
        }
    }
}
