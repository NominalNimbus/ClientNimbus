using System;
using TradingClient.ViewModelInterfaces;
using TradingClient.Data.Contracts;

namespace TradingClient.ViewModels
{
    public class PositionItem : TradeItem, IPositionItem
    {
        #region Members

        private Position _position;
        private decimal _avgPrice;
        private Side _side;
        private decimal _qty;
        private decimal _margin;

        #endregion //Members

        public PositionItem(Position position)
        {
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Profit = position.Profit;
            ProfitPips = position.ProfitPips;
            Side = position.Side;
            CurrentPrice = position.CurrentPrice;
            Margin = position.Margin;
        }

        #region Properties

        public Position Position
        {
            get => _position;
            set => SetPropertyValue(ref _position, value, nameof(Position));
        }

        public decimal AvgPrice
        {
            get => _avgPrice;
            set => SetPropertyValue(ref _avgPrice, value, nameof(AvgPrice));
        }

        public Side Side
        {
            get => _side;
            set => SetPropertyValue(ref _side, value, nameof(Side));
        }

        public decimal Qty
        {
            get => _qty;
            set => SetPropertyValue(ref _qty, value, nameof(Qty));
        }

        public decimal Margin
        {
            get => _margin;
            set => SetPropertyValue(ref _margin, value, nameof(Margin));
        }
        
        #endregion //Properties

    }
}