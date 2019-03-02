using System;

namespace TradingClient.Data.Contracts
{
    public class PortfolioActionEventArgs : EventArgs
    {
        public string PortfolioName { get; set; }

        public object Portfolio { get; set; }

        public string Error { get; set; }

        public bool IsRemoving { get; set; }
    }
}