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
using Newtonsoft.Json;
using QuantConnect.Util;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect
{
    /// <summary>
    /// Contains insight population run time statistics
    /// </summary>
    public class AlphaRuntimeStatistics
    {
        private decimal _portfolioTurnover;
        private decimal _returnOverMaxDrawdown;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <remarks>Required for proper deserialization</remarks>
        public AlphaRuntimeStatistics()
        {
        }

        /// <summary>
        /// Gets the total number of insights with an up direction
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long LongCount { get; set; }

        /// <summary>
        /// Gets the total number of insights with a down direction
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long ShortCount { get; set; }

        /// <summary>
        /// The ratio of <see cref="InsightDirection.Up"/> over <see cref="InsightDirection.Down"/>
        /// </summary>
        public decimal LongShortRatio => ShortCount == 0 ? 1m : LongCount / (decimal) ShortCount;

        /// <summary>
        /// Measurement of the strategies trading activity with respect to the portfolio value.
        /// Calculated as the sales volume with respect to the average total portfolio value.
        /// </summary>
        /// <remarks>For performance we only truncate when the value is gotten</remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore), JsonConverter(typeof(StringDecimalJsonConverter), true)]
        public decimal PortfolioTurnover
        {
            get
            {
                return _portfolioTurnover.TruncateTo3DecimalPlaces();
            }
            set
            {
                _portfolioTurnover = value;
            }
        }

        /// <summary>
        /// Provides a risk adjusted way to factor in the returns and drawdown of the strategy.
        /// It is calculated by dividing the Portfolio Annualized Return by the Maximum Drawdown seen during the backtest.
        /// </summary>
        /// <remarks>For performance we only truncate when the value is gotten</remarks>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore), JsonConverter(typeof(StringDecimalJsonConverter), true)]
        public decimal ReturnOverMaxDrawdown
        {
            get
            {
                return _returnOverMaxDrawdown.TruncateTo3DecimalPlaces();
            }
            set
            {
                _returnOverMaxDrawdown = value;
            }
        }

        /// <summary>
        /// The total number of insight signals generated by the algorithm
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long TotalInsightsGenerated { get; set; }

        /// <summary>
        /// The total number of insight signals generated by the algorithm
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long TotalInsightsClosed { get; set; }

        /// <summary>
        /// The total number of insight signals generated by the algorithm
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public long TotalInsightsAnalysisCompleted { get; set; }

        /// <summary>
        /// Creates a dictionary containing the statistics
        /// </summary>
        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                {
                    Messages.AlphaRuntimeStatistics.ReturnOverMaximumDrawdownKey,
                    Invariant(ReturnOverMaxDrawdown)
                },
                {
                    Messages.AlphaRuntimeStatistics.PortfolioTurnoverKey,
                    Invariant(PortfolioTurnover)
                },
                {
                    Messages.AlphaRuntimeStatistics.TotalInsightsGeneratedKey,
                    Invariant(TotalInsightsGenerated)
                },
                {
                    Messages.AlphaRuntimeStatistics.TotalInsightsClosedKey,
                    Invariant(TotalInsightsClosed)
                },
                {
                    Messages.AlphaRuntimeStatistics.TotalInsightsAnalysisCompletedKey,
                    Invariant(TotalInsightsAnalysisCompleted)
                },
                {
                    Messages.AlphaRuntimeStatistics.LongInsightCountKey,
                    Invariant(LongCount)
                },
                {
                    Messages.AlphaRuntimeStatistics.ShortInsightCountKey,
                    Invariant(ShortCount)
                },
                {
                    Messages.AlphaRuntimeStatistics.LongShortRatioKey,
                    $"{Invariant(Math.Round(100*LongShortRatio, 2))}%"
                },
            };
        }

        private static string Invariant(IConvertible obj)
        {
            return obj.ToStringInvariant();
        }
    }
}
