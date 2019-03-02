using System;
using TradingClient.Data.Contracts;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class OrderItem : TradeItem, IOrderItem
    {
        #region Members

        private Order _order;
        private decimal _sl;
        private decimal _tp;
        private decimal _filledQty;

        #endregion //Members

        public OrderItem(Order order)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
            Profit = 0;
        }

        #region Properties

        public Order Order
        {
            get => _order;
            private set => SetPropertyValue(ref _order, value, nameof(Order));
        }
        
        public decimal SL
        {
            get => _sl;
            set => SetPropertyValue(ref _sl, value, nameof(SL));
        }

        public decimal TP
        {
            get => _tp;
            set => SetPropertyValue(ref _tp, value, nameof(TP));
        }

        public decimal FilledQty
        {
            get => _filledQty;
            set => SetPropertyValue(ref _filledQty, value, nameof(FilledQty));
        }
    
        #endregion //Properties
    }
}