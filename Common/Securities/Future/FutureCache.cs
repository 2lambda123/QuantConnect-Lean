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

using QuantConnect.Data;

namespace QuantConnect.Securities.Future
{
    /// <summary>
    /// Future specific caching support
    /// </summary>
    /// <remarks>Class is virtually empty and scheduled to be made obsolete. Potentially could be used for user data storage.</remarks>
    /// <seealso cref="SecurityCache"/>
    public class FutureCache : SecurityCache
    {
        /// <summary>
        /// The current settlement price
        /// </summary>
        public decimal SettlementPrice { get; set; }

        /// <summary>
        /// Will consume the given data point updating the cache state and it's properties
        /// </summary>
        /// <param name="data">The data point to process</param>
        /// <param name="cacheByType">True if this data point should be cached by type</param>
        protected override void ProcessDataPoint(BaseData data, bool cacheByType)
        {
            base.ProcessDataPoint(data, cacheByType);

            SettlementPrice = Price;
        }
    }
}
