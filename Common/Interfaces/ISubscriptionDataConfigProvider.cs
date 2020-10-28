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

using System.Collections.Generic;
using QuantConnect.Data;

namespace QuantConnect.Interfaces
{
    /// <summary>
    /// Reduced interface which provides access to registered <see cref="SubscriptionDataConfig"/>
    /// </summary>
    public interface ISubscriptionDataConfigProvider
    {
        /// <summary>
        /// Gets a list of all registered <see cref="SubscriptionDataConfig"/> for a given <see cref="Symbol"/>
        /// </summary>
        /// <remarks>Will not return internal subscriptions by default</remarks>
        IEnumerable<SubscriptionDataConfig> GetSubscriptionDataConfigs(Symbol symbol, bool includeInternalConfigs = false);
    }
}
