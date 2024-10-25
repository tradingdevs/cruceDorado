using Binance.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apiBinance.Entities
{
    public class OrderFuturesRequest
    {
        public string Symbol { get; set; }

        public OrderSide Side { get; set; }
        public PositionSide PositionSide { get; set; }

        public FuturesOrderType OrderType { get; set; }

        public TimeInForce? TimeInForce { get; set; }

        public decimal Quantity { get; set; }

        public decimal? Price { get; set; }

        public string NewClientOrderId { get; set; }

        public decimal? StopPrice { get; set; }

        public decimal? IcebergQuantity { get; set; }

        public int? RecvWindow { get; set; }
        public bool? ReduceOnly { get; set; }
        public bool? ClosePosition { get; set; }
    }
}
