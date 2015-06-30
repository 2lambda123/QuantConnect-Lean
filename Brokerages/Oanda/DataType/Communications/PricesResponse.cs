﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages.Oanda.DataType;

namespace OANDARestLibrary.TradeLibrary.DataTypes.Communications
{
    public class PricesResponse
    {
        public long time;
        public List<Price> prices;
    }
}
