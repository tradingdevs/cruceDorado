using Binance.Net.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace apiBinance.Entities
{
    public class ValidateTrading
    {
        public bool Value { get; set; }
        public PositionSide? PositionSide { get; set; }
        public decimal Quantity { get; set; }

        public ValidateTrading()
        {
            Value = false;
            Quantity = 0;
        }
    }
}
