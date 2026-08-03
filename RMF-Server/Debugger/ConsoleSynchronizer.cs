namespace RMF_Server.Debugger
{
    internal class ConsoleSynchronizer : IConsoleSynchronizer
    {
        // It is accessed directly via a memory address, bypassing the cache.
        // This should prevent desync... Probably
        private volatile bool _isAdminTyping;

        public bool isAdminTyping
        {
            get => this._isAdminTyping;
            set => this._isAdminTyping = value;
        }
    }
}
