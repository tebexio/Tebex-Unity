using System;

namespace Tebex.Plugin
{
    /// <summary>
    /// Summary store information object.
    /// </summary>
    [Serializable]
    public class Store
    {
        public Account account = new Account();
        public Server server = new Server();
    }
}
