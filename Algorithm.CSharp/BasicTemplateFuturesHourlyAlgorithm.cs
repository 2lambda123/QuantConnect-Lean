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
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Interfaces;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This example demonstrates how to add futures with hourly resolution.
    /// </summary>
    /// <meta name="tag" content="using data" />
    /// <meta name="tag" content="benchmarks" />
    /// <meta name="tag" content="futures" />
    public class BasicTemplateFuturesHourlyAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _contractSymbol;

        // S&P 500 EMini futures
        private const string RootSP500 = Futures.Indices.SP500EMini;

        // Gold futures
        private const string RootGold = Futures.Metals.Gold;

        /// <summary>
        /// Initialize your algorithm and add desired assets.
        /// </summary>
        public override void Initialize()
        {
            SetStartDate(2013, 10, 08);
            SetEndDate(2013, 10, 10);
            SetCash(1000000);

            var futureSP500 = AddFuture(RootSP500, Resolution.Hour);
            var futureGold = AddFuture(RootGold, Resolution.Hour);

            // set our expiry filter for this futures chain
            // SetFilter method accepts TimeSpan objects or integer for days.
            // The following statements yield the same filtering criteria 
            futureSP500.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(182));
            futureGold.SetFilter(0, 182);
        }

        /// <summary>
        /// Event - v3.0 DATA EVENT HANDLER: (Pattern) Basic template for user to override for receiving all subscription data in a single event
        /// </summary>
        /// <param name="slice">The current slice of data keyed by symbol string</param>
        public override void OnData(Slice slice)
        {
            if (!Portfolio.Invested)
            {
                foreach(var chain in slice.FutureChains)
                {
                    // find the front contract expiring no earlier than in 90 days
                    var contract = (
                        from futuresContract in chain.Value.OrderBy(x => x.Expiry)
                        where futuresContract.Expiry > Time.Date.AddDays(90)
                        select futuresContract
                    ).FirstOrDefault();

                    // if found, trade it
                    if (contract != null)
                    {
                        _contractSymbol = contract.Symbol;
                        MarketOrder(_contractSymbol, 1);
                    }
                }
            }
            else
            {
                Liquidate();
            }
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "140"},
            {"Average Win", "0.01%"},
            {"Average Loss", "-0.02%"},
            {"Compounding Annual Return", "-38.171%"},
            {"Drawdown", "0.400%"},
            {"Expectancy", "-0.369"},
            {"Net Profit", "-0.394%"},
            {"Sharpe Ratio", "-24.82"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "66%"},
            {"Win Rate", "34%"},
            {"Profit-Loss Ratio", "0.84"},
            {"Alpha", "0.42"},
            {"Beta", "-0.041"},
            {"Annual Standard Deviation", "0.01"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-65.112"},
            {"Tracking Error", "0.253"},
            {"Treynor Ratio", "6.024"},
            {"Total Fees", "$259.00"},
            {"Estimated Strategy Capacity", "$130000.00"},
            {"Lowest Capacity Asset", "GC VOFJUCDY9XNH"},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "-43.422"},
            {"Return Over Maximum Drawdown", "-100.459"},
            {"Portfolio Turnover", "4.716"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "320067074c8dd771f69602ab07001f1e"}
        };
    }
}
