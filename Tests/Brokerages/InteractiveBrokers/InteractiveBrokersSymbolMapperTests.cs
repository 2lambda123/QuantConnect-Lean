﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using NUnit.Framework;
using QuantConnect.Brokerages.InteractiveBrokers;

namespace QuantConnect.Tests.Brokerages.InteractiveBrokers
{
    [TestFixture]
    class InteractiveBrokersSymbolMapperTests
    {
        [Test]
        public void ReturnsCorrectLeanSymbol()
        {
            var mapper = new InteractiveBrokersSymbolMapper();

            var symbol = mapper.GetLeanSymbol("EURUSD", SecurityType.Forex, Market.FXCM);
            Assert.AreEqual("EURUSD", symbol.Value);
            Assert.AreEqual(SecurityType.Forex, symbol.ID.SecurityType);
            Assert.AreEqual(Market.FXCM, symbol.ID.Market);

            symbol = mapper.GetLeanSymbol("AAPL", SecurityType.Equity, Market.USA);
            Assert.AreEqual("AAPL", symbol.Value);
            Assert.AreEqual(SecurityType.Equity, symbol.ID.SecurityType);
            Assert.AreEqual(Market.USA, symbol.ID.Market);

            symbol = mapper.GetLeanSymbol("AAPL", SecurityType.Option, Market.USA,new DateTime(2016,05,20),108,OptionRight.Put, OptionStyle.American);
            Assert.AreEqual("AAPL  160520P00108000", symbol.Value);
            Assert.AreEqual("AAPL", symbol.ID.Symbol);  // IB Relies on this for ConvertOrder
            Assert.AreEqual(SecurityType.Option, symbol.ID.SecurityType);
            Assert.AreEqual(Market.USA, symbol.ID.Market);
            Assert.AreEqual(OptionRight.Put,symbol.ID.OptionRight);
            Assert.AreEqual(OptionStyle.American,symbol.ID.OptionStyle);
            Assert.AreEqual(108,symbol.ID.StrikePrice);
            Assert.AreEqual(new DateTime(2016, 05, 20),symbol.ID.Date);
        }

        [Test]
        public void ReturnsCorrectBrokerageSymbol()
        {
            var mapper = new InteractiveBrokersSymbolMapper();

            var symbol = Symbol.Create("EURUSD", SecurityType.Forex, Market.FXCM);
            var brokerageSymbol = mapper.GetBrokerageSymbol(symbol);
            Assert.AreEqual("EURUSD", brokerageSymbol);

            symbol = Symbol.Create("AAPL", SecurityType.Equity, Market.USA);
            brokerageSymbol = mapper.GetBrokerageSymbol(symbol);
            Assert.AreEqual("AAPL", brokerageSymbol);

            symbol = Symbol.CreateOption("AAPL",Market.USA,OptionStyle.American, OptionRight.Put, 108, new DateTime(2016, 05, 20));
            brokerageSymbol = mapper.GetBrokerageSymbol(symbol);
            Assert.AreEqual("AAPL  160520P00108000", brokerageSymbol);
            Assert.AreEqual("AAPL", symbol.ID.Symbol);  // IB Relies on this for ConvertOrder

        }

        [Test]
        public void ThrowsOnNullOrEmptyOrInvalidSymbol()
        {
            var mapper = new InteractiveBrokersSymbolMapper();

            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol(null, SecurityType.Forex, Market.FXCM));

            Assert.Throws<ArgumentException>(() => mapper.GetLeanSymbol("", SecurityType.Forex, Market.FXCM));

            var symbol = Symbol.Empty;
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));

            symbol = null;
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));

            symbol = Symbol.Create("", SecurityType.Forex, Market.FXCM);
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));

            symbol = Symbol.Create("ABC_XYZ", SecurityType.Forex, Market.FXCM);
            Assert.Throws<ArgumentException>(() => mapper.GetBrokerageSymbol(symbol));
        }

    }
}
