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
using QuantConnect.Data;
using QuantConnect.Data.Fundamental;
using QuantConnect.Data.Market;
using QuantConnect.Util;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace QuantConnect.Python
{
    /// <summary>
    /// Organizes a list of data to create pandas.DataFrames
    /// </summary>
    public class PandasData
    {
        private const string Open = "open";
        private const string High = "high";
        private const string Low = "low";
        private const string Close = "close";
        private const string Volume = "volume";

        private const string AskOpen = "askopen";
        private const string AskHigh = "askhigh";
        private const string AskLow = "asklow";
        private const string AskClose = "askclose";
        private const string AskPrice = "askprice";
        private const string AskSize = "asksize";

        private const string BidOpen = "bidopen";
        private const string BidHigh = "bidhigh";
        private const string BidLow = "bidlow";
        private const string BidClose = "bidclose";
        private const string BidPrice = "bidprice";
        private const string BidSize = "bidsize";

        private const string LastPrice = "lastprice";
        private const string Quantity = "quantity";
        private const string Exchange = "exchange";
        private const string Suspicious = "suspicious";
        private const string OpenInterest = "openinterest";

        #region OptionContract Members Handling

        // TODO: In the future, excluding, adding, renaming and unwrapping members (like the Greeks case)
        // should be handled generically: we could define attributes so that class members can be marked as
        // excluded, or to be renamed and/ or unwrapped (much like how Json attributes work)

        private static readonly string[] _optionContractExcludedMembers = new[]
        {
            nameof(OptionContract.ID),
        };

        private static readonly string[] _greeksMemberNames = new[]
        {
            nameof(Greeks.Delta),
            nameof(Greeks.Gamma),
            nameof(Greeks.Vega),
            nameof(Greeks.Theta),
            nameof(Greeks.Rho),
        };

        private static readonly MemberInfo[] _greeksMembers = typeof(Greeks)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => (x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property) && _greeksMemberNames.Contains(x.Name))
            .ToArray();

        #endregion

        // we keep these so we don't need to ask for them each time
        private static PyString _empty;
        private static PyObject _pandas;
        private static PyObject _seriesFactory;
        private static PyObject _dataFrameFactory;
        private static PyObject _multiIndexFactory;

        private static PyList _defaultNames;
        private static PyList _level1Names;
        private static PyList _level2Names;
        private static PyList _level3Names;

        private readonly static HashSet<string> _baseDataProperties = typeof(BaseData).GetProperties().ToHashSet(x => x.Name.ToLowerInvariant());
        private readonly static ConcurrentDictionary<Type, IEnumerable<MemberInfo>> _membersByType = new();
        private readonly static IReadOnlyList<string> _standardColumns = new string[]
        {
                Open,    High,    Low,    Close, LastPrice,  Volume,
            AskOpen, AskHigh, AskLow, AskClose,  AskPrice, AskSize, Quantity, Suspicious,
            BidOpen, BidHigh, BidLow, BidClose,  BidPrice, BidSize, Exchange, OpenInterest
        };

        private readonly Symbol _symbol;
        private readonly bool _isFundamentalType;
        private readonly Dictionary<string, Serie> _series;

        private readonly IEnumerable<MemberInfo> _members = Enumerable.Empty<MemberInfo>();

        /// <summary>
        /// Gets true if this is a custom data request, false for normal QC data
        /// </summary>
        public bool IsCustomData { get; }

        /// <summary>
        /// Implied levels of a multi index pandas.Series (depends on the security type)
        /// </summary>
        public int Levels { get; } = 2;

        /// <summary>
        /// Initializes an instance of <see cref="PandasData"/>
        /// </summary>
        public PandasData(object data)
        {
            if (_pandas == null)
            {
                using (Py.GIL())
                {
                    // Use our PandasMapper class that modifies pandas indexing to support tickers, symbols and SIDs
                    _pandas = Py.Import("PandasMapper");
                    _seriesFactory = _pandas.GetAttr("Series");
                    _dataFrameFactory = _pandas.GetAttr("DataFrame");
                    using var multiIndex = _pandas.GetAttr("MultiIndex");
                    _multiIndexFactory = multiIndex.GetAttr("from_tuples");
                    _empty = new PyString(string.Empty);

                    var time = new PyString("time");
                    var symbol = new PyString("symbol");
                    var expiry = new PyString("expiry");
                    _defaultNames = new PyList(new PyObject[] { expiry, new PyString("strike"), new PyString("type"), symbol, time });
                    _level1Names = new PyList(new PyObject[] { symbol });
                    _level2Names = new PyList(new PyObject[] { symbol, time });
                    _level3Names = new PyList(new PyObject[] { expiry, symbol, time });
                }
            }

            var baseData = data as IBaseData;

            // in the case we get a list/collection of data we take the first data point to determine the type
            // but it's also possible to get a data which supports enumerating we don't care about those cases
            if (baseData == null && data is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    data = item;
                    baseData = data as IBaseData;
                    break;
                }
            }

            var type = data.GetType();
            _isFundamentalType = type == typeof(Fundamental);
            _symbol = ((ISymbolProvider)data).Symbol;
            IsCustomData = Extensions.IsCustomDataType(_symbol, type);

            if (baseData == null)
            {
                Levels = 1;
            }
            else if (_symbol.SecurityType == SecurityType.Future)
            {
                Levels = 3;
            }
            else if (_symbol.SecurityType.IsOption())
            {
                Levels = 5;
            }

            IEnumerable<string> columns = _standardColumns;

            if (IsCustomData || baseData == null || baseData.DataType == MarketDataType.Auxiliary)
            {
                var keys = (data as DynamicData)?.GetStorageDictionary()
                    // if this is a PythonData instance we add in '__typename' which we don't want into the data frame
                    .Where(x => !x.Key.StartsWith("__", StringComparison.InvariantCulture)).ToHashSet(x => x.Key);

                // C# types that are not DynamicData type
                if (keys == null)
                {
                    if (_membersByType.TryGetValue(type, out _members))
                    {
                        keys = _members.ToHashSet(x => x.Name.ToLowerInvariant());
                    }
                    else
                    {
                        var members = type
                            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
                            .Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property);

                        // TODO: Avoid hard-coded especial cases by using something like attributes to change
                        // pandas conversion behavior
                        if (type.IsAssignableTo(typeof(OptionContract)))
                        {
                            members = members.Where(x => !_optionContractExcludedMembers.Contains(x.Name));
                        }

                        members = members.ToList();

                        var duplicateKeys = members.GroupBy(x => x.Name.ToLowerInvariant()).Where(x => x.Count() > 1).Select(x => x.Key);
                        foreach (var duplicateKey in duplicateKeys)
                        {
                            throw new ArgumentException($"PandasData.ctor(): {Messages.PandasData.DuplicateKey(duplicateKey, type.FullName)}");
                        }

                        // If the custom data derives from a Market Data (e.g. Tick, TradeBar, QuoteBar), exclude its keys
                        keys = members.ToHashSet(x => x.Name.ToLowerInvariant());
                        keys.ExceptWith(_baseDataProperties);
                        keys.ExceptWith(GetPropertiesNames(typeof(QuoteBar), type));
                        keys.ExceptWith(GetPropertiesNames(typeof(TradeBar), type));
                        keys.ExceptWith(GetPropertiesNames(typeof(Tick), type));
                        keys.Add("value");

                        _members = members.Where(x => keys.Contains(x.Name.ToLowerInvariant())).ToList();
                        _membersByType.TryAdd(type, _members);
                    }

                    // Make sure to add the greeks member names to the series so they can be added to the data frame
                    if (_members.Any(x => GetMemberType(x).IsAssignableTo(typeof(Greeks))))
                    {
                        foreach (var greekMemberName in _greeksMemberNames)
                        {
                            keys.Add(greekMemberName.ToLowerInvariant());
                        }
                    }
                }

                var customColumns = new HashSet<string>(columns) { "value" };
                customColumns.UnionWith(keys);

                columns = customColumns;
            }

            _series = columns.ToDictionary(k => k, v => new Serie());
        }

        /// <summary>
        /// Adds security data object to the end of the lists
        /// </summary>
        /// <param name="baseData"><see cref="IBaseData"/> object that contains security data</param>
        public void Add(object baseData)
        {
            var endTime = default(DateTime);
            if (baseData is IBaseData iBaseData)
            {
                endTime = iBaseData.EndTime;
            }

            foreach (var member in _members)
            {
                var memberType = GetMemberType(member);
                if (!memberType.IsAssignableTo(typeof(Greeks)))
                {
                    AddMemberToSeries(baseData, endTime, member);
                }
                else
                {
                    var greeks = member switch
                    {
                        PropertyInfo property => property.GetValue(baseData),
                        FieldInfo field => field.GetValue(baseData),
                        // Should not happen
                        _ => throw new InvalidOperationException($"Unexpected member type: {member.MemberType}")
                    };

                    if (greeks != null)
                    {
                        foreach (var greekMember in _greeksMembers)
                        {
                            AddMemberToSeries(greeks, endTime, greekMember);
                        }
                    }
                }
            }

            var storage = (baseData as DynamicData)?.GetStorageDictionary();
            if (storage != null)
            {
                var value = ((IBaseData)baseData).Value;
                AddToSeries("value", endTime, value);

                foreach (var kvp in storage.Where(x => x.Key != "value"
                    // if this is a PythonData instance we add in '__typename' which we don't want into the data frame
                    && !x.Key.StartsWith("__", StringComparison.InvariantCulture)))
                {
                    AddToSeries(kvp.Key, endTime, kvp.Value);
                }
            }
            else if (baseData is Tick tick)
            {
                AddTick(tick);
            }
            else if (baseData is TradeBar tradeBar)
            {
                Add(tradeBar, null);
            }
            else if (baseData is QuoteBar quoteBar)
            {
                Add(null, quoteBar);
            }
        }

        private static Type GetMemberType(MemberInfo member)
        {
            return member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                // Should not happen
                _ => throw new InvalidOperationException($"Unexpected member type: {member.MemberType}")
            };
        }

        private void AddMemberToSeries(object baseData, DateTime endTime, MemberInfo member)
        {
            // TODO field/property.GetValue is expensive
            var key = member.Name.ToLowerInvariant();
            if (member is PropertyInfo property)
            {
                var propertyValue = property.GetValue(baseData);
                if (_isFundamentalType && property.PropertyType.IsAssignableTo(typeof(FundamentalTimeDependentProperty)))
                {
                    propertyValue = ((FundamentalTimeDependentProperty)propertyValue).Clone(new FixedTimeProvider(endTime));
                }
                AddToSeries(key, endTime, propertyValue);
            }
            else if (member is FieldInfo field)
            {
                AddToSeries(key, endTime, field.GetValue(baseData));
            }
        }

        /// <summary>
        /// Adds Lean data objects to the end of the lists
        /// </summary>
        /// <param name="tradeBar"><see cref="TradeBar"/> object that contains trade bar information of the security</param>
        /// <param name="quoteBar"><see cref="QuoteBar"/> object that contains quote bar information of the security</param>
        public void Add(TradeBar tradeBar, QuoteBar quoteBar)
        {
            if (tradeBar != null)
            {
                var time = tradeBar.EndTime;
                GetSerie(Open).Add(time, tradeBar.Open);
                GetSerie(High).Add(time, tradeBar.High);
                GetSerie(Low).Add(time, tradeBar.Low);
                GetSerie(Close).Add(time, tradeBar.Close);
                GetSerie(Volume).Add(time, tradeBar.Volume);
            }
            if (quoteBar != null)
            {
                var time = quoteBar.EndTime;
                if (tradeBar == null)
                {
                    GetSerie(Open).Add(time, quoteBar.Open);
                    GetSerie(High).Add(time, quoteBar.High);
                    GetSerie(Low).Add(time, quoteBar.Low);
                    GetSerie(Close).Add(time, quoteBar.Close);
                }
                if (quoteBar.Ask != null)
                {
                    GetSerie(AskOpen).Add(time, quoteBar.Ask.Open);
                    GetSerie(AskHigh).Add(time, quoteBar.Ask.High);
                    GetSerie(AskLow).Add(time, quoteBar.Ask.Low);
                    GetSerie(AskClose).Add(time, quoteBar.Ask.Close);
                    GetSerie(AskSize).Add(time, quoteBar.LastAskSize);
                }
                if (quoteBar.Bid != null)
                {
                    GetSerie(BidOpen).Add(time, quoteBar.Bid.Open);
                    GetSerie(BidHigh).Add(time, quoteBar.Bid.High);
                    GetSerie(BidLow).Add(time, quoteBar.Bid.Low);
                    GetSerie(BidClose).Add(time, quoteBar.Bid.Close);
                    GetSerie(BidSize).Add(time, quoteBar.LastBidSize);
                }
            }
        }

        /// <summary>
        /// Adds a tick data point to this pandas collection
        /// </summary>
        /// <param name="tick"><see cref="Tick"/> object that contains tick information of the security</param>
        public void AddTick(Tick tick)
        {
            if (tick == null)
            {
                return;
            }

            var time = tick.EndTime;

            // We will fill some series with null for tick types that don't have a value for that series, so that we make sure
            // the indices are the same for every tick series.

            if (tick.TickType == TickType.Quote)
            {
                GetSerie(AskPrice).Add(time, tick.AskPrice);
                GetSerie(AskSize).Add(time, tick.AskSize);
                GetSerie(BidPrice).Add(time, tick.BidPrice);
                GetSerie(BidSize).Add(time, tick.BidSize);
            }
            else
            {
                // Trade and open interest ticks don't have these values, so we'll fill them with null.
                GetSerie(AskPrice).Add(time, null);
                GetSerie(AskSize).Add(time, null);
                GetSerie(BidPrice).Add(time, null);
                GetSerie(BidSize).Add(time, null);
            }

            GetSerie(Exchange).Add(time, tick.Exchange);
            GetSerie(Suspicious).Add(time, tick.Suspicious);
            GetSerie(Quantity).Add(time, tick.Quantity);

            if (tick.TickType == TickType.OpenInterest)
            {
                GetSerie(OpenInterest).Add(time, tick.Value);
                GetSerie(LastPrice).Add(time, null);
            }
            else
            {
                GetSerie(LastPrice).Add(time, tick.Value);
                GetSerie(OpenInterest).Add(time, null);
            }
        }

        /// <summary>
        /// Get the pandas.DataFrame of the current <see cref="PandasData"/> state
        /// </summary>
        /// <param name="levels">Number of levels of the multi index</param>
        /// <param name="filterMissingValueColumns">If false, make sure columns with "missing" values only are still added to the dataframe</param>
        /// <returns>pandas.DataFrame object</returns>
        public PyObject ToPandasDataFrame(int levels = 2, bool filterMissingValueColumns = true)
        {
            List<PyObject> list;
            var symbol = _symbol.ToPython();

            // Create the index labels
            var names = _defaultNames;

            if (levels == 1)
            {
                names = _level1Names;
                list = new List<PyObject> { symbol };
            }
            else if (levels == 2)
            {
                // symbol, time
                names = _level2Names;
                list = new List<PyObject> { symbol, _empty };
            }
            else if (levels == 3)
            {
                // expiry, symbol, time
                names = _level3Names;
                list = new List<PyObject> { _symbol.ID.Date.ToPython(), symbol, _empty };
            }
            else
            {
                list = new List<PyObject> { _empty, _empty, _empty, symbol, _empty };
                if (_symbol.SecurityType == SecurityType.Future)
                {
                    list[0] = _symbol.ID.Date.ToPython();
                }
                else if (_symbol.SecurityType.IsOption())
                {
                    list[0] = _symbol.ID.Date.ToPython();
                    list[1] = _symbol.ID.StrikePrice.ToPython();
                    list[2] = _symbol.ID.OptionRight.ToString().ToPython();
                }
            }

            // creating the pandas MultiIndex is expensive so we keep a cash
            var indexCache = new Dictionary<List<DateTime>, PyObject>(new ListComparer<DateTime>());
            // Returns a dictionary keyed by column name where values are pandas.Series objects
            using var pyDict = new PyDict();
            foreach (var kvp in _series)
            {
                if (filterMissingValueColumns && kvp.Value.ShouldFilter) continue;

                if (!indexCache.TryGetValue(kvp.Value.Times, out var index))
                {
                    using var tuples = kvp.Value.Times.Select(time => CreateTupleIndex(time, list)).ToPyListUnSafe();
                    using var namesDic = Py.kw("names", names);

                    indexCache[kvp.Value.Times] = index = _multiIndexFactory.Invoke(new[] { tuples }, namesDic);

                    foreach (var pyObject in tuples)
                    {
                        pyObject.Dispose();
                    }
                }

                // Adds pandas.Series value keyed by the column name
                using var pyvalues = new PyList();
                for (var i = 0; i < kvp.Value.Values.Count; i++)
                {
                    using var pyObject = kvp.Value.Values[i].ToPython();
                    pyvalues.Append(pyObject);
                }
                using var series = _seriesFactory.Invoke(pyvalues, index);
                pyDict.SetItem(kvp.Key, series);
            }
            _series.Clear();
            foreach (var kvp in indexCache)
            {
                kvp.Value.Dispose();
            }

            for (var i = 0; i < list.Count; i++)
            {
                DisposeIfNotEmpty(list[i]);
            }

            // Create the DataFrame
            var result = _dataFrameFactory.Invoke(pyDict);

            foreach (var item in pyDict)
            {
                item.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Only dipose of the PyObject if it was set to something different than empty
        /// </summary>
        private static void DisposeIfNotEmpty(PyObject pyObject)
        {
            if (!ReferenceEquals(pyObject, _empty))
            {
                pyObject.Dispose();
            }
        }

        /// <summary>
        /// Create a new tuple index
        /// </summary>
        private static PyTuple CreateTupleIndex(DateTime index, List<PyObject> list)
        {
            if (list.Count > 1)
            {
                DisposeIfNotEmpty(list[list.Count - 1]);
                list[list.Count - 1] = index.ToPython();
            }
            return new PyTuple(list.ToArray());
        }

        /// <summary>
        /// Adds data to dictionary
        /// </summary>
        /// <param name="key">The key of the value to get</param>
        /// <param name="time"><see cref="DateTime"/> object to add to the value associated with the specific key</param>
        /// <param name="input"><see cref="Object"/> to add to the value associated with the specific key. Can be null.</param>
        private void AddToSeries(string key, DateTime time, object input)
        {
            var serie = GetSerie(key);
            serie.Add(time, input);
        }

        private Serie GetSerie(string key)
        {
            if (!_series.TryGetValue(key, out var value))
            {
                throw new ArgumentException($"PandasData.GetSerie(): {Messages.PandasData.KeyNotFoundInSeries(key)}");
            }
            return value;
        }

        /// <summary>
        /// Get the lower-invariant name of properties of the type that a another type is assignable from
        /// </summary>
        /// <param name="baseType">The type that is assignable from</param>
        /// <param name="type">The type that is assignable by</param>
        /// <returns>List of string. Empty list if not assignable from</returns>
        private static IEnumerable<string> GetPropertiesNames(Type baseType, Type type)
        {
            return baseType.IsAssignableFrom(type)
                ? baseType.GetProperties().Select(x => x.Name.ToLowerInvariant())
                : Enumerable.Empty<string>();
        }

        private class Serie
        {
            private static readonly IFormatProvider InvariantCulture = CultureInfo.InvariantCulture;
            public bool ShouldFilter { get; set; } = true;
            public List<DateTime> Times { get; set; } = new();
            public List<object> Values { get; set; } = new();

            public void Add(DateTime time, object input)
            {
                var value = input is decimal ? Convert.ToDouble(input, InvariantCulture) : input;
                if (ShouldFilter)
                {
                    // we need at least 1 valid entry for the series not to get filtered
                    if (value is double)
                    {
                        if (!((double)value).IsNaNOrZero())
                        {
                            ShouldFilter = false;
                        }
                    }
                    else if (value is string)
                    {
                        if (!string.IsNullOrWhiteSpace((string)value))
                        {
                            ShouldFilter = false;
                        }
                    }
                    else if (value is bool)
                    {
                        if ((bool)value)
                        {
                            ShouldFilter = false;
                        }
                    }
                    else if (value != null)
                    {
                        ShouldFilter = false;
                    }
                }

                Values.Add(value);
                Times.Add(time);
            }

            public void Add(DateTime time, decimal input)
            {
                var value = Convert.ToDouble(input, InvariantCulture);
                if (ShouldFilter && !value.IsNaNOrZero())
                {
                    ShouldFilter = false;
                }

                Values.Add(value);
                Times.Add(time);
            }
        }

        private class FixedTimeProvider : ITimeProvider
        {
            private readonly DateTime _time;
            public DateTime GetUtcNow() => _time;
            public FixedTimeProvider(DateTime time)
            {
                _time = time;
            }
        }
    }
}
