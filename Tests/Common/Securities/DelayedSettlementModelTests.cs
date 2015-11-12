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
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Common.Securities
{
    [TestFixture]
    public class DelayedSettlementModelTests
    {
        private static readonly DateTime Noon = new DateTime(2015, 11, 2, 12, 0, 0);
        private static readonly TimeKeeper TimeKeeper = new TimeKeeper(Noon.ConvertToUtc(TimeZones.NewYork), new[] { TimeZones.NewYork });
        private const string Symbol = "SPY";

        [Test]
        public void SellOnMondaySettleOnThursday()
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            // settlement at T+3, 8:00 AM
            var model = new DelayedSettlementModel(3, TimeSpan.FromHours(8));
            var config = CreateTradeBarConfig(Symbol);
            var security = new Security(SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(), config, 1);

            portfolio.SetCash(3000);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            // Sell on Monday
            var timeUtc = Noon.ConvertToUtc(TimeZones.NewYork);
            model.ApplyFunds(portfolio, security, timeUtc, "USD", 1000);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Tuesday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Wednesday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Thursday at 7:55 AM, still unsettled
            timeUtc = timeUtc.AddDays(1).AddHours(-4).AddMinutes(-5);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Thursday at 8 AM, now unsettled
            timeUtc = timeUtc.AddMinutes(5);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(4000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);
        }

        [Test]
        public void SellOnThursdaySettleOnTuesday()
        {
            var securities = new SecurityManager(TimeKeeper);
            var transactions = new SecurityTransactionManager(securities);
            var portfolio = new SecurityPortfolioManager(securities, transactions);
            // settlement at T+3, 8:00 AM
            var model = new DelayedSettlementModel(3, TimeSpan.FromHours(8));
            var config = CreateTradeBarConfig(Symbol);
            var security = new Security(SecurityExchangeHoursTests.CreateUsEquitySecurityExchangeHours(), config, 1);

            portfolio.SetCash(3000);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);

            // Sell on Thursday
            var timeUtc = Noon.AddDays(3).ConvertToUtc(TimeZones.NewYork);
            model.ApplyFunds(portfolio, security, timeUtc, "USD", 1000);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Friday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Saturday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Sunday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Monday, still unsettled
            timeUtc = timeUtc.AddDays(1);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Tuesday at 7:55 AM, still unsettled
            timeUtc = timeUtc.AddDays(1).AddHours(-4).AddMinutes(-5);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(3000, portfolio.Cash);
            Assert.AreEqual(1000, portfolio.UnsettledCash);

            // Tuesday at 8 AM, now unsettled
            timeUtc = timeUtc.AddMinutes(5);
            portfolio.ScanForCashSettlement(timeUtc);
            Assert.AreEqual(4000, portfolio.Cash);
            Assert.AreEqual(0, portfolio.UnsettledCash);
        }

        private SubscriptionDataConfig CreateTradeBarConfig(string symbol)
        {
            return new SubscriptionDataConfig(typeof(TradeBar), SecurityType.Equity, symbol, Resolution.Minute, "usa", TimeZones.NewYork, true, true, false);
        }

    }
}
