using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public abstract class TradeItem : Observable, ITradeItem
    {
        #region Members

        protected decimal _currentPrice;
        protected decimal _currentPriceChange;
        protected decimal _profit;
        protected decimal _profitPips;
        protected bool _isServerSide;

        #endregion //Members

        #region Properties

        public Security Instrument { get; set; }

        public decimal CurrentPrice
        {
            get => _currentPrice;
            set => SetPropertyValue(ref _currentPrice, value, nameof(CurrentPrice));
        }

        public decimal CurrentPriceChange
        {
            get => _currentPriceChange;
            set => SetPropertyValue(ref _currentPriceChange, value, nameof(CurrentPriceChange));
        }

        public decimal Profit
        {
            get => _profit;
            set => SetPropertyValue(ref _profit, value, nameof(Profit));
        }

        public decimal ProfitPips
        {
            get => _profitPips;
            set => SetPropertyValue(ref _profitPips, value, nameof(ProfitPips));
        }

        public bool IsServerSide
        {
            get => _isServerSide;
            set => SetPropertyValue(ref _isServerSide, value, nameof(IsServerSide));
        }

        #endregion //Properties
    }
}
