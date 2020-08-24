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
 *
*/

using NUnit.Framework;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Securities;
using System;

namespace QuantConnect.Tests.Common.Util
{
    [TestFixture]
    public class SubscriptionDataConfigExtensionsTests
    {
        private SubscriptionDataConfig _config;

        [SetUp]
        public void Setup()
        {
            _config = new SubscriptionDataConfig(typeof(TradeBar),
                Symbols.SPY,
                Resolution.Daily,
                TimeZones.NewYork,
                TimeZones.NewYork,
                true,
                true,
                false);
        }

        [Test]
        public void AdjustTradeBar()
        {
            var tb = new TradeBar
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Period = TimeSpan.FromHours(1),
                Symbol = Symbols.SPY,
                Open = 100,
                High = 200,
                Low = 300,
                Close = 400
            };

            var factor = 0.5m;
            var adjustedTb = tb.Clone(tb.IsFillForward).Adjust(factor);

            Assert.AreEqual(tb.Open * factor, (adjustedTb as TradeBar).Open);
            Assert.AreEqual(tb.High * factor, (adjustedTb as TradeBar).High);
            Assert.AreEqual(tb.Low * factor, (adjustedTb as TradeBar).Low);
            Assert.AreEqual(tb.Close * factor, (adjustedTb as TradeBar).Close);
        }

        [Test]
        public void AdjustTick()
        {
            var tick = new Tick
            {
                Time = new DateTime(2020, 5, 21, 8, 9, 0),
                Symbol = Symbols.SPY,
                Value = 100
            };

            var factor = 0.5m;
            var adjustedTick = tick.Clone(tick.IsFillForward).Adjust(factor);

            Assert.AreEqual(tick.Value * factor, (adjustedTick as Tick).Value);
        }

        [Test]
        public void AdjustQuoteBar()
        {
            var qb = new QuoteBar(
                new DateTime(2018, 1, 1),
                _config.Symbol,
                new Bar(10, 10, 10, 10),
                100,
                new Bar(10, 10, 10, 10),
                100);

            var factor = 0.5m;
            var adjustedQb = qb.Clone(qb.IsFillForward).Adjust(factor);

            Assert.AreEqual(qb.Value, qb.Close);

            // bid
            Assert.AreEqual(qb.Bid.Open * factor, (adjustedQb as QuoteBar).Bid.Open);
            Assert.AreEqual(qb.Bid.Close * factor, (adjustedQb as QuoteBar).Bid.Close);
            Assert.AreEqual(qb.Bid.High * factor, (adjustedQb as QuoteBar).Bid.High);
            Assert.AreEqual(qb.Bid.Low * factor, (adjustedQb as QuoteBar).Bid.Low);
            // ask
            Assert.AreEqual(qb.Ask.Open * factor, (adjustedQb as QuoteBar).Ask.Open);
            Assert.AreEqual(qb.Ask.Close * factor, (adjustedQb as QuoteBar).Ask.Close);
            Assert.AreEqual(qb.Ask.High * factor, (adjustedQb as QuoteBar).Ask.High);
            Assert.AreEqual(qb.Ask.Low * factor, (adjustedQb as QuoteBar).Ask.Low);
        }
    }
}
