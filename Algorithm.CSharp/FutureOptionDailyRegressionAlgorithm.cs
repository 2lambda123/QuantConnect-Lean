/*
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This regression algorithm tests using FutureOptions daily resolution
    /// </summary>
    public class FutureOptionDailyRegressionAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        protected OrderTicket Ticket;
        protected Symbol DcOption;
        protected virtual Resolution Resolution => Resolution.Daily;

        public override void Initialize()
        {
            SetStartDate(2012, 1, 3);
            SetEndDate(2012, 1, 4);

            // Add our underlying future contract
            var dc = AddFutureContract(
                QuantConnect.Symbol.CreateFuture(
                    Futures.Dairy.ClassIIIMilk,
                    Market.CME,
                    new DateTime(2012, 4, 1)),
                Resolution).Symbol;

            // Attempt to fetch a specific future option contract
            DcOption = OptionChainProvider.GetOptionContractList(dc, Time)
                .Where(x => x.ID.StrikePrice == 17m && x.ID.OptionRight == OptionRight.Call)
                .Select(x => AddFutureOptionContract(x, Resolution).Symbol)
                .FirstOrDefault();

            // Validate it is the expected contract
            var expectedContract = QuantConnect.Symbol.CreateOption(dc, Market.CME, OptionStyle.American,
                OptionRight.Call, 17m,
                new DateTime(2012, 4, 01));

            if (DcOption != expectedContract)
            {
                throw new Exception($"Contract {DcOption} was not the expected contract {expectedContract}");
            }

            ScheduleBuySell();
        }

        protected virtual void ScheduleBuySell()
        {
            // Schedule a purchase of this contract tomorrow at 10AM when the market is open
            Schedule.On(DateRules.Tomorrow, TimeRules.At(10,0,0), () =>
            {
                Ticket = MarketOrder(DcOption, 1);
            });

            // Schedule liquidation tomorrow at 2PM when the market is open
            Schedule.On(DateRules.Tomorrow, TimeRules.At(14,0,0), () =>
            {
                Liquidate();
            });
        }

        public override void OnData(Slice slice)
        {
            // Assert we are only getting data at 7PM (12AM UTC)
            if (slice.Time.Hour != 19)
            {
                throw new ArgumentException($"Expected data at 7PM each day; instead was {slice.Time}");
            }
        }

        /// <summary>
        /// Ran at the end of the algorithm to ensure the algorithm has no holdings
        /// </summary>
        /// <exception cref="Exception">The algorithm has holdings</exception>
        public override void OnEndOfAlgorithm()
        {
            if (Portfolio.Invested)
            {
                throw new Exception($"Expected no holdings at end of algorithm, but are invested in: {string.Join(", ", Portfolio.Keys)}");
            }

            if (Ticket.Status != OrderStatus.Filled)
            {
                throw new Exception("Future option order failed to fill correctly");
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public virtual bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public virtual Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// Data Points count of all timeslices of algorithm
        /// </summary>
        public virtual long DataPoints => 35;

        /// <summary>
        /// Data Points count of the algorithm history
        /// </summary>
        public virtual int AlgorithmHistoryDataPoints => 0;

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public virtual Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "2"},
            {"Average Win", "0%"},
            {"Average Loss", "-0.82%"},
            {"Compounding Annual Return", "-66.144%"},
            {"Drawdown", "0.800%"},
            {"Expectancy", "-1"},
            {"Starting Equity", "100000"},
            {"Ending Equity", "99175.06"},
            {"Net Profit", "-0.825%"},
            {"Sharpe Ratio", "-7.069"},
            {"Sortino Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "100%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0.403"},
            {"Beta", "-7.512"},
            {"Annual Standard Deviation", "0.093"},
            {"Annual Variance", "0.009"},
            {"Information Ratio", "-7.581"},
            {"Tracking Error", "0.105"},
            {"Treynor Ratio", "0.087"},
            {"Total Fees", "$4.94"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", "DC V5E8P9VAH3IC|DC V5E8P9SH0U0X"},
            {"Portfolio Turnover", "1.39%"},
            {"OrderListHash", "4a190a2b77e2603e8756788acb4d49b4"}
        };
    }
}

