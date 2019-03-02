using System;

namespace TradingClient.Data.Contracts
{
    public class Order : ICloneable
    {
        public string ID { get; private set; }  // Need to be UNIX ticks timestamp
        public string BrokerID { get; set; }
        public string Symbol { get; private set; }
        public decimal Quantity { get; set; }
        public decimal Profit { get; set; }
        public decimal FilledQuantity { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal ClosingQty { get; set; }
        public Status OrderStatus { get; set; }
        public OrderType OrderType { get; set; }
        public Side OrderSide { get; private set; }
        public decimal Commission { get; set; }
        public decimal Price { get; set; }
        public decimal? SLOffset { get; set; }
        public decimal? TPOffset { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime OpenDate { get; private set; }
        public TimeInForce TimeInForce { get; private set; }
        public string Notice { get; set; }
        public decimal ProfitPips { get; set; }
        public string BrokerName { get; set; }
        public string AccountId { get; set; }
        
        public bool ServerSide { get; set; }

        public Order(string id, string symbol)
        {
            ID = id;
            Symbol = symbol;
            OpenDate = DateTime.UtcNow;
            BrokerName = String.Empty;
            AccountId = String.Empty;
        }

        public Order(string id, string symbol, decimal quantity, OrderType orderType, Side orderSide, TimeInForce timeInForce)
            : this(id, symbol)
        {
            Quantity = quantity;
            OrderSide = orderSide;
            OrderType = orderType;
            TimeInForce = timeInForce;
        }

        public Order(string id,string symbol, decimal quantity, OrderType orderType, Side orderSide, decimal price, TimeInForce timeInForce)
            : this(id, symbol, quantity, orderType, orderSide, timeInForce)
        {
            Price = price;
        }

        public Order(string id, string symbol, decimal quantity, OrderType orderType, Side orderSide, decimal commission, decimal price, DateTime openTime, TimeInForce timeInForce)
            : this(id, symbol, quantity, orderType, orderSide, timeInForce)
        {
            Commission = commission;
            Price = price;
            OpenDate = openTime;
        }

        public object Clone() =>
            MemberwiseClone();


        public static string[] GetExportHeaders()
        {
            return new string[]
            {
                "Id",
                "Open Time",
                "Type",
                "Side",
                "Quantity",
                "Symbol",
                "Price",
                "Open Price",
                "S/L",
                "T/P",
                "Close Price",
                "Trade C.",
                "Status",
                "Notice"
            };
        }

        public object[] GetExportValues()
        {
            return new object[]
            {
                ID,
                OpenDate,
                OrderType.ToString(),
                OrderSide.ToString(),
                Quantity,
                Symbol,
                Price,
                Price,
                SLOffset,
                TPOffset,
                CurrentPrice,
                Commission,
                OrderStatus.ToString(),
                Notice
             };
        }
    }
}