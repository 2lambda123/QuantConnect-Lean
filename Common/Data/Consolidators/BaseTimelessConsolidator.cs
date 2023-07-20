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

using Python.Runtime;
using System;
using QuantConnect.Data.Market;

namespace QuantConnect.Data.Consolidators
{
    /// <summary>
    /// Represents a timeless consolidator which depends on the given values. This consolidator
    /// is meant to consolidate data into bars that do not depend on time, e.g., RangeBar's.
    /// </summary>
    public abstract class BaseTimelessConsolidator : IDataConsolidator
    {
        protected Func<IBaseData, decimal> Selector;
        protected Func<IBaseData, decimal> VolumeSelector;
        protected DataConsolidatedHandler DataConsolidatedHandler;

        /// <summary>
        /// Bar being created
        /// </summary>
        protected virtual IBaseData CurrentBar {  get; set; }

        /// <summary>
        /// Gets the most recently consolidated piece of data. This will be null if this consolidator
        /// has not produced any data yet.
        /// </summary>
        public IBaseData Consolidated { get; protected set; }

        /// <summary>
        /// Gets a clone of the data being currently consolidated
        /// </summary>
        public abstract IBaseData WorkingData { get; }

        /// <summary>
        /// Gets the type consumed by this consolidator
        /// </summary>
        public Type InputType => typeof(IBaseData);

        /// <summary>
        /// Gets <see cref="IBaseData"/> which is the type emitted in the <see cref="IDataConsolidator.DataConsolidated"/> event.
        /// </summary>
        public virtual Type OutputType => typeof(IBaseData);

        /// <summary>
        /// Event handler that fires when a new piece of data is produced
        /// </summary>
        public event DataConsolidatedHandler DataConsolidated;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTimelessConsolidator" /> class.
        /// </summary>
        /// <param name="selector">Extracts the value from a data instance to be formed into a new bar which inherits from <see cref="IBaseData"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        protected BaseTimelessConsolidator(Func<IBaseData, decimal> selector, Func<IBaseData, decimal> volumeSelector = null)
        {
            Selector = selector ?? (x => x.Value);
            VolumeSelector = volumeSelector ?? (x => 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTimelessConsolidator" /> class.
        /// </summary>
        /// <param name="valueSelector">Extracts the value from a data instance to be formed into a new bar which inherits from <see cref="IBaseData"/>. The default
        /// value is (x => x.Value) the <see cref="IBaseData.Value"/> property on <see cref="IBaseData"/></param>
        /// <param name="volumeSelector">Extracts the volume from a data instance. The default value is null which does
        /// not aggregate volume per bar.</param>
        protected BaseTimelessConsolidator(PyObject valueSelector, PyObject volumeSelector = null)
        {
            Selector = TryToConvertSelector(valueSelector, nameof(valueSelector)) ?? (x => x.Value);
            VolumeSelector = TryToConvertSelector(volumeSelector, nameof(volumeSelector)) ?? (x => 0);
        }

        /// <summary>
        /// Tries to convert the given python selector to a C# one. If the conversion is not
        /// possible it returns null
        /// </summary>
        /// <param name="selector">The python selector to be converted</param>
        /// <param name="selectorName">The name of the selector to be used in case an exception is thrown</param>
        /// <exception cref="ArgumentException">This exception will be thrown if it's not possible to convert the
        /// given python selector to C#</exception>
        private Func<IBaseData, decimal> TryToConvertSelector(PyObject selector, string selectorName)
        {
            using (Py.GIL())
            {
                Func<IBaseData, decimal> resultSelector;
                if (selector != null && !selector.IsNone())
                {
                    if (!selector.TryConvertToDelegate(out resultSelector))
                    {
                        throw new ArgumentException(
                            $"Unable to convert parameter {selectorName} to delegate type Func<IBaseData, decimal>");
                    }
                }
                else
                {
                    resultSelector = null;
                }

                return resultSelector;
            }
        }

        /// <summary>
        /// Updates this consolidator with the specified data
        /// </summary>
        /// <param name="data">The new data for the consolidator</param>
        public void Update(IBaseData data)
        {
            var currentValue = Selector(data);
            var volume = VolumeSelector(data);

            // If we're already in a bar then update it
            if (CurrentBar != null)
            {
                UpdateBar(data.Time, currentValue, volume);
            }

            // The state of the CurrentBar could have changed after UpdateBar(),
            // then we might need to create a new bar
            if (CurrentBar == null)
            {
                CreateNewBar(data);
            }
        }

        /// <summary>
        /// Updates the current RangeBar being created with the given data.
        /// Additionally, if it's the case, it consolidates the current RangeBar
        /// </summary>
        /// <param name="time">Time of the given data</param>
        /// <param name="currentValue">Value of the given data</param>
        /// <param name="volume">Volume of the given data</param>
        protected abstract void UpdateBar(DateTime time, decimal currentValue, decimal volume);

        /// <summary>
        /// Creates a new bar with the given data
        /// </summary>
        /// <param name="data">The new data for the bar</param>
        protected abstract void CreateNewBar(IBaseData data);

        /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
            DataConsolidated = null;
            DataConsolidatedHandler = null;
        }

        /// <summary>
        /// Scans this consolidator to see if it should emit a bar due to time passing
        /// </summary>
        /// <param name="currentLocalTime">The current time in the local time zone (same as <see cref="BaseData.Time"/>)</param>
        public void Scan(DateTime currentLocalTime)
        {
        }
    }
}
