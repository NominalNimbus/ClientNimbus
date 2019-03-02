using TradingClient.Common;
using TradingClient.Data.Contracts;
using TradingClient.ViewModelInterfaces;

namespace TradingClient.ViewModels
{
    public class PlaceOrderItem : Observable
    {
        private decimal _quantity;

        public string Symbol { get; set; }

        public decimal Quantity
        {
            get => _quantity;
            set => SetPropertyValue(ref _quantity, value, nameof(Quantity));
        }

        public OrderType OrderType { get; set; }

        public Side OrderSide { get; set; }

        public decimal Price { get; set; }

        public decimal? SLOffset { get; set; }

        public decimal? TPOffset { get; set; }

        public TimeInForce TimeInForce { get; set; }
        
    }
}
