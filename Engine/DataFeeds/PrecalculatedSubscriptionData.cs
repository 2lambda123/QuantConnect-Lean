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
using QuantConnect.Data;
using QuantConnect.Securities;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// DTO for storing data and the time at which it should be synchronized
    /// </summary>
    public class PrecalculatedSubscriptionData : SubscriptionData
    {
        private BaseData _adjustedData;
        private SubscriptionDataConfig _config;

        /// <summary>
        /// Gets the data
        /// </summary>
        public override BaseData Data
        {
            get
            {
                switch (_config.DataNormalizationMode)
                {
                    case DataNormalizationMode.Raw:
                        return _data;

                    // the price scale factor will be set accordingly based on the mode in update scale factors
                    case DataNormalizationMode.Adjusted:
                    case DataNormalizationMode.SplitAdjusted:
                        return _adjustedData;

                    default:
                        return _data;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrecalculatedSubscriptionData"/> class
        /// </summary>
        /// <param name="configuration">The subscription's configuration</param>
        /// <param name="rawData">The base data</param>
        /// <param name="adjustedData">The adjusted data</param>
        /// <param name="emitTimeUtc">The emit time for the data</param>
        public PrecalculatedSubscriptionData(SubscriptionDataConfig configuration, BaseData rawData, BaseData adjustedData, DateTime emitTimeUtc)
            : base(rawData, emitTimeUtc)
        {
            _config = configuration;
            _adjustedData = adjustedData;
        }
    }
}