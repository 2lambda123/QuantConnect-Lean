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
using System.Collections.Generic;
using System.Linq;
using Python.Runtime;

namespace QuantConnect.Data.UniverseSelection
{
    /// <summary>
    /// Provides a functional implementation of <see cref="Universe"/>
    /// </summary>
    /// <typeparam name="T">The BaseData type to provide to the user defined universe filter</typeparam>
    public class FuncUniverse<T> : Universe
        where T : BaseData
    {
        private readonly UniverseSettings _universeSettings;
        private readonly Func<IEnumerable<T>, IEnumerable<Symbol>> _universeSelector;

        /// <summary>
        /// Gets the settings used for subscriptons added for this universe
        /// </summary>
        public override UniverseSettings UniverseSettings
        {
            get { return _universeSettings; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncUniverse{T}"/> class
        /// </summary>
        /// <param name="configuration">The configuration used to resolve the data for universe selection</param>
        /// <param name="universeSettings">The settings used for new subscriptions generated by this universe</param>
        /// <param name="universeSelector">Returns the symbols that should be included in the universe</param>
        public FuncUniverse(SubscriptionDataConfig configuration, UniverseSettings universeSettings, Func<IEnumerable<T>, IEnumerable<Symbol>> universeSelector)
            : base(configuration)
        {
            _universeSelector = universeSelector;
            _universeSettings = universeSettings;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FuncUniverse{T}"/> class for a filter function loaded from Python
        /// </summary>
        /// <param name="configuration">The configuration used to resolve the data for universe selection</param>
        /// <param name="universeSettings">The settings used for new subscriptions generated by this universe</param>
        /// <param name="universeSelector">Function that returns the symbols that should be included in the universe</param>
        public FuncUniverse(SubscriptionDataConfig configuration, UniverseSettings universeSettings, PyObject universeSelector)
            : this(configuration, universeSettings, universeSelector.ConvertPythonUniverseFilterFunction<T>())
        {
        }

        /// <summary>
        /// Performs an initial, coarse filter
        /// </summary>
        /// <param name="utcTime">The current utc time</param>
        /// <param name="data">The coarse fundamental data</param>
        /// <returns>The data that passes the filter</returns>
        public override IEnumerable<Symbol> SelectSymbols(DateTime utcTime, BaseDataCollection data)
        {
            return _universeSelector(data.Data.Cast<T>());
        }
    }
    
    /// <summary>
    /// Provides a functional implementation of <see cref="Universe"/>
    /// </summary>
    public class FuncUniverse : FuncUniverse<BaseData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FuncUniverse"/> class
        /// </summary>
        /// <param name="configuration">The configuration used to resolve the data for universe selection</param>
        /// <param name="universeSettings">The settings used for new subscriptions generated by this universe</param>
        /// <param name="universeSelector">Returns the symbols that should be included in the universe</param>
        public FuncUniverse(SubscriptionDataConfig configuration, UniverseSettings universeSettings, Func<IEnumerable<BaseData>, IEnumerable<Symbol>> universeSelector)
            : base(configuration, universeSettings, universeSelector)
        {
        }
     
        /// <summary>
        /// Initializes a new instance of the <see cref="FuncUniverse"/> class
        /// </summary>
        /// <param name="configuration">The configuration used to resolve the data for universe selection</param>
        /// <param name="universeSettings">The settings used for new subscriptions generated by this universe</param>
        /// <param name="universeSelector">Returns the symbols that should be included in the universe</param>
        public FuncUniverse(SubscriptionDataConfig configuration, UniverseSettings universeSettings, PyObject universeSelector)
            : base(configuration, universeSettings, universeSelector)
        {
        }
    }
}
