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

using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Data.Consolidators;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Alpha model that uses an EMA cross to create insights
    /// </summary>
    public class EmaCrossAlphaModel : AlphaModel
    {
        private readonly int _fastPeriod;
        private readonly int _slowPeriod;
        private readonly Resolution _resolution;
        private readonly int _predictionInterval;
        protected readonly Dictionary<Symbol, SymbolData> _symbolDataBySymbol;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmaCrossAlphaModel"/> class
        /// </summary>
        /// <param name="fastPeriod">The fast EMA period</param>
        /// <param name="slowPeriod">The slow EMA period</param>
        /// <param name="resolution">The resolution of data sent into the EMA indicators</param>
        public EmaCrossAlphaModel(
            int fastPeriod = 12,
            int slowPeriod = 26,
            Resolution resolution = Resolution.Daily
            )
        {
            _fastPeriod = fastPeriod;
            _slowPeriod = slowPeriod;
            _resolution = resolution;
            _predictionInterval = fastPeriod;
            _symbolDataBySymbol = new Dictionary<Symbol, SymbolData>();
            Name = $"{nameof(EmaCrossAlphaModel)}({fastPeriod},{slowPeriod},{resolution})";
        }

        /// <summary>
        /// Updates this alpha model with the latest data from the algorithm.
        /// This is called each time the algorithm receives data for subscribed securities
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="data">The new data available</param>
        /// <returns>The new insights generated</returns>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            var insights = new List<Insight>();
            foreach (var symbolData in _symbolDataBySymbol.Values)
            {
                if (symbolData.Fast.IsReady && symbolData.Slow.IsReady)
                {
                    var insightPeriod = _resolution.ToTimeSpan().Multiply(_predictionInterval);
                    if (symbolData.FastIsOverSlow)
                    {
                        if (symbolData.Slow > symbolData.Fast)
                        {
                            insights.Add(Insight.Price(symbolData.Symbol, insightPeriod, InsightDirection.Down));
                        }
                    }
                    else if (symbolData.SlowIsOverFast)
                    {
                        if (symbolData.Fast > symbolData.Slow)
                        {
                            insights.Add(Insight.Price(symbolData.Symbol, insightPeriod, InsightDirection.Up));
                        }
                    }
                }

                symbolData.FastIsOverSlow = symbolData.Fast > symbolData.Slow;
            }

            return insights;
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                SymbolData symbolData;
                if (!_symbolDataBySymbol.TryGetValue(added.Symbol, out symbolData))
                {
                    _symbolDataBySymbol[added.Symbol] = new SymbolData(added, _fastPeriod, _slowPeriod, algorithm, _resolution);
                }
                else
                {
                    // a security that was already initialized was re-added, reset the indicators
                    symbolData.Fast.Reset();
                    symbolData.Slow.Reset();
                }
            }

            foreach (var removed in changes.RemovedSecurities)
            {
                SymbolData data;
                if (_symbolDataBySymbol.TryGetValue(removed.Symbol, out data))
                {
                    // clean up our consolidators
                    algorithm.SubscriptionManager.RemoveConsolidator(data.Security.Symbol, data.FastConsolidator);
                    algorithm.SubscriptionManager.RemoveConsolidator(data.Security.Symbol, data.SlowConsolidator);
                    _symbolDataBySymbol.Remove(removed.Symbol);
                }
            }
        }

        /// <summary>
        /// Contains data specific to a symbol required by this model
        /// </summary>
        public class SymbolData
        {
            public readonly Security Security;
            public readonly IDataConsolidator FastConsolidator;
            public readonly IDataConsolidator SlowConsolidator;
            public Symbol Symbol => Security.Symbol;
            public ExponentialMovingAverage Fast;
            public ExponentialMovingAverage Slow;

            /// <summary>
            /// True if the fast is above the slow, otherwise false.
            /// This is used to prevent emitting the same signal repeatedly
            /// </summary>
            public bool FastIsOverSlow { get; set; }
            public bool SlowIsOverFast => !FastIsOverSlow;

            public SymbolData(
                Security security,
                int fastPeriod,
                int slowPeriod,
                QCAlgorithm algorithm,
                Resolution resolution)
            {
                Security = security;

                FastConsolidator = algorithm.ResolveConsolidator(security.Symbol, resolution);
                SlowConsolidator = algorithm.ResolveConsolidator(security.Symbol, resolution);

                algorithm.SubscriptionManager.AddConsolidator(security.Symbol, FastConsolidator);
                algorithm.SubscriptionManager.AddConsolidator(security.Symbol, SlowConsolidator);

                // create fast/slow EMAs
                Fast = new ExponentialMovingAverage(security.Symbol, fastPeriod, ExponentialMovingAverage.SmoothingFactorDefault(fastPeriod));
                Slow = new ExponentialMovingAverage(security.Symbol, slowPeriod, ExponentialMovingAverage.SmoothingFactorDefault(slowPeriod));

                algorithm.RegisterIndicator(Security.Symbol, Fast, FastConsolidator);
                algorithm.RegisterIndicator(Security.Symbol, Slow, SlowConsolidator);
                algorithm.RegisterIndicator(security.Symbol, Fast, FastConsolidator);
                algorithm.RegisterIndicator(security.Symbol, Slow, SlowConsolidator);

                algorithm.WarmUpIndicator(security.Symbol, Fast, resolution);
                algorithm.WarmUpIndicator(security.Symbol, Slow, resolution);
            }
        }
    }
}
