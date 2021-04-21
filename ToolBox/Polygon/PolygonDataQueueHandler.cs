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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Securities;
using QuantConnect.Securities.Crypto;
using QuantConnect.Securities.Forex;
using QuantConnect.ToolBox.Polygon.WebSocket;
using QuantConnect.ToolBox.Polygon.History;
using QuantConnect.Util;
using HistoryRequest = QuantConnect.Data.HistoryRequest;
using static QuantConnect.StringExtensions;

namespace QuantConnect.ToolBox.Polygon
{
    /// <summary>
    /// An implementation of <see cref="IDataQueueHandler"/> and <see cref="IHistoryProvider"/> for Polygon.io
    /// </summary>
    public class PolygonDataQueueHandler : SynchronizingHistoryProvider, IDataQueueHandler
    {
        private const string HistoryBaseUrl = "https://api.polygon.io";
        private const int BaseAggregateBarsLimit = 50000;
        private readonly string _apiKey = Config.Get("polygon-api-key");

        private readonly IDataAggregator _dataAggregator = Composer.Instance.GetExportedValueByTypeName<IDataAggregator>(
            Config.Get("data-aggregator", "QuantConnect.Lean.Engine.DataFeeds.AggregationManager"));

        private readonly DataQueueHandlerSubscriptionManager _subscriptionManager;

        private readonly Dictionary<SecurityType, PolygonWebSocketClientWrapper> _webSocketClientWrappers = new Dictionary<SecurityType, PolygonWebSocketClientWrapper>();
        private readonly PolygonSymbolMapper _symbolMapper = new PolygonSymbolMapper();
        private readonly MarketHoursDatabase _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
        private readonly SymbolPropertiesDatabase _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();

        // exchange time zones by symbol
        private readonly Dictionary<Symbol, DateTimeZone> _symbolExchangeTimeZones = new Dictionary<Symbol, DateTimeZone>();

        // map Polygon exchange -> Lean market
        // Crypto exchanges from: https://api.polygon.io/v1/meta/crypto-exchanges?apiKey=xxx
        private readonly Dictionary<int, string> _cryptoExchangeMap = new Dictionary<int, string>
        {
            { 1, Market.GDAX },
            { 2, Market.Bitfinex }
        };

        private int _dataPointCount;

        /// <summary>
        /// Static constructor for the <see cref="PolygonDataQueueHandler"/> class
        /// </summary>
        static PolygonDataQueueHandler()
        {
            // Polygon.io requires TLS 1.2
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonDataQueueHandler"/> class
        /// </summary>
        public PolygonDataQueueHandler() : this(true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonDataQueueHandler"/> class
        /// </summary>
        public PolygonDataQueueHandler(bool streamingEnabled)
        {
            if (streamingEnabled)
            {
                foreach (var securityType in new[] { SecurityType.Equity /*, SecurityType.Forex, SecurityType.Crypto */})
                {
                    var client = new PolygonWebSocketClientWrapper(_apiKey, _symbolMapper, securityType, OnMessage);
                    _webSocketClientWrappers.Add(securityType, client);
                }
            }

            var subscriber = new EventBasedDataQueueHandlerSubscriptionManager(t => t.ToString());
            subscriber.SubscribeImpl += Subscribe;
            subscriber.UnsubscribeImpl += Unsubscribe;

            _subscriptionManager = subscriber;
        }

        #region IDataQueueHandler implementation

        /// <summary>
        /// Sets the job we're subscribing for
        /// </summary>
        /// <param name="job">Job we're subscribing for</param>
        public void SetJob(LiveNodePacket job)
        {
        }

        /// <summary>
        /// Indicates the connection is live.
        /// </summary>
        public bool IsConnected => _webSocketClientWrappers.Values.All(client => client.IsOpen);

        /// <summary>
        /// Subscribe to the specified configuration
        /// </summary>
        /// <param name="dataConfig">defines the parameters to subscribe to a data feed</param>
        /// <param name="newDataAvailableHandler">handler to be fired on new data available</param>
        /// <returns>The new enumerator for this subscription request</returns>
        public IEnumerator<BaseData> Subscribe(SubscriptionDataConfig dataConfig, EventHandler newDataAvailableHandler)
        {
            if (!CanSubscribe(dataConfig.Symbol))
            {
                return Enumerable.Empty<BaseData>().GetEnumerator();
            }

            var enumerator = _dataAggregator.Add(dataConfig, newDataAvailableHandler);
            _subscriptionManager.Subscribe(dataConfig);

            return enumerator;
        }

        /// <summary>
        /// Removes the specified configuration
        /// </summary>
        /// <param name="dataConfig">Subscription config to be removed</param>
        public void Unsubscribe(SubscriptionDataConfig dataConfig)
        {
            _subscriptionManager.Unsubscribe(dataConfig);
            _dataAggregator.Remove(dataConfig);
        }

        /// <summary>
        /// Adds the specified symbols to the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be added</param>
        /// <param name="tickType">Type of tick data</param>
        private bool Subscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            foreach (var symbol in symbols)
            {
                var webSocket = GetWebSocket(symbol.SecurityType);
                webSocket.Subscribe(symbol, tickType);
            }

            return true;
        }

        /// <summary>
        /// Removes the specified symbols from the subscription
        /// </summary>
        /// <param name="symbols">The symbols to be removed</param>
        /// <param name="tickType">Type of tick data</param> 
        private bool Unsubscribe(IEnumerable<Symbol> symbols, TickType tickType)
        {
            foreach (var symbol in symbols)
            {
                var webSocket = GetWebSocket(symbol.SecurityType);
                webSocket.Unsubscribe(symbol, tickType);
            }

            return true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _dataAggregator.Dispose();
        }

        #endregion

        #region IHistoryProvider implementation

        /// <summary>
        /// Gets the total number of data points emitted by this history provider
        /// </summary>
        public override int DataPointCount => _dataPointCount;

        /// <summary>
        /// Initializes this history provider to work for the specified job
        /// </summary>
        /// <param name="parameters">The initialization parameters</param>
        public override void Initialize(HistoryProviderInitializeParameters parameters)
        {
        }

        /// <summary>
        /// Gets the history for the requested securities
        /// </summary>
        /// <param name="requests">The historical data requests</param>
        /// <param name="sliceTimeZone">The time zone used when time stamping the slice instances</param>
        /// <returns>An enumerable of the slices of data covering the span specified in each request</returns>
        public override IEnumerable<Slice> GetHistory(IEnumerable<HistoryRequest> requests, DateTimeZone sliceTimeZone)
        {
            var tasks = requests.Select(request => Task.Run(async () => await GetHistoryAsync(request).ConfigureAwait(false)));
            var requestsAndHistory = Task.WhenAll(tasks).Result;
            
            // create subscription objects from the configs
            var subscriptions = 
                (from kvp in requestsAndHistory 
                let request = kvp.Key 
                let history = kvp.Value 
                select CreateSubscription(request, history)).ToList();

            /*
            var subscriptions = new List<Subscription>();
            foreach (var request in requests)
            {
                var history = GetHistory(request);
                var subscription = CreateSubscription(request, history);

                subscriptions.Add(subscription);
            }
            */

            return CreateSliceEnumerableFromSubscriptions(subscriptions, sliceTimeZone);
        }

        /// <summary>
        /// Gets the history for the requested security
        /// </summary>
        /// <param name="request">The historical data request</param>
        /// <returns>An enumerable of BaseData points</returns>
        public IEnumerable<BaseData> GetHistory(HistoryRequest request)
        {
            return ProcessHistoryRequest(request);
        }

        private Task<KeyValuePair<HistoryRequest, BaseData[]>> GetHistoryAsync(HistoryRequest request)
        {
            var tcs = new TaskCompletionSource<KeyValuePair<HistoryRequest, BaseData[]>>();
            Task.Run(() =>
            {
                var result = GetHistory(request).ToArray();
                var kvp = new KeyValuePair<HistoryRequest, BaseData[]> (request, result);
                tcs.SetResult(kvp);
            });

            return tcs.Task;
        }

        #endregion

        private IEnumerable<BaseData> ProcessHistoryRequest(HistoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Log.Error("PolygonDataQueueHandler.GetHistory(): History calls for Polygon.io require an API key.");
                yield break;
            }

            // check security type
            if (request.Symbol.SecurityType != SecurityType.Equity &&
                request.Symbol.SecurityType != SecurityType.Forex &&
                request.Symbol.SecurityType != SecurityType.Crypto)
            {
                Log.Error($"PolygonDataQueueHandler.ProcessHistoryRequests(): Unsupported security type: {request.Symbol.SecurityType}.");
                yield break;
            }

            // check tick type
            if (request.TickType != TickType.Trade && request.TickType != TickType.Quote)
            {
                Log.Error($"PolygonDataQueueHandler.ProcessHistoryRequests(): Unsupported tick type: {request.TickType}.");
                yield break;
            }

            // check unsupported security type/tick type combinations
            if (request.Symbol.SecurityType == SecurityType.Forex && request.TickType != TickType.Quote ||
                request.Symbol.SecurityType == SecurityType.Crypto && request.TickType != TickType.Trade)
            {
                Log.Error($"PolygonDataQueueHandler.ProcessHistoryRequests(): Unsupported history request: {request.Symbol.SecurityType}/{request.TickType}.");
                yield break;
            }
            
            Log.Trace("PolygonDataQueueHandler.ProcessHistoryRequests(): Submitting request: " +
                      Invariant($"{request.Symbol.SecurityType}-{request.TickType}-{request.Symbol.Value}: {request.Resolution} {request.StartTimeUtc}->{request.EndTimeUtc}"));

            switch (request.Resolution)
            {
                case Resolution.Tick:
                    if (request.TickType == TickType.Trade)
                    {
                        foreach (var tick in GetTradeTicks(request))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return tick;
                        }
                    }
                    else if (request.TickType == TickType.Quote)
                    {
                        foreach (var tick in GetQuoteTicks(request))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return tick;
                        }
                    }

                    break;

                case Resolution.Second:
                    if (request.TickType == TickType.Trade)
                    {
                        var ticks = GetTradeTicks(request);

                        foreach (var tradeBar in AggregateTradeTicks(request.Symbol, ticks, request.Resolution.ToTimeSpan()))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return tradeBar;
                        }
                    }
                    else if (request.TickType == TickType.Quote)
                    {
                        var ticks = GetQuoteTicks(request);

                        foreach (var quoteBar in AggregateQuoteTicks(request.Symbol, ticks, request.Resolution.ToTimeSpan()))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return quoteBar;
                        }
                    }

                    break;

                case Resolution.Minute:
                case Resolution.Hour:
                case Resolution.Daily:
                    if (request.TickType == TickType.Trade)
                    {
                        foreach (var tradeBar in GetTradeBars(request))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return tradeBar;
                        }
                    }
                    else if (request.TickType == TickType.Quote)
                    {
                        var ticks = GetQuoteTicks(request);

                        foreach (var quoteBar in AggregateQuoteTicks(request.Symbol, ticks, request.Resolution.ToTimeSpan()))
                        {
                            Interlocked.Increment(ref _dataPointCount);

                            yield return quoteBar;
                        }
                    }

                    break;
            }
        }

        private IEnumerable<Tick> GetQuoteTicks(HistoryRequest request)
        {
            switch (request.Symbol.SecurityType)
            {
                case SecurityType.Equity:
                    return GetEquityQuoteTicks(request);

                case SecurityType.Forex:
                    return GetForexQuoteTicks(request);

                default:
                    return Enumerable.Empty<Tick>();
            }
        }

        private IEnumerable<Tick> GetTradeTicks(HistoryRequest request)
        {
            switch (request.Symbol.SecurityType)
            {
                case SecurityType.Equity:
                    return GetEquityTradeTicks(request);

                case SecurityType.Crypto:
                    return GetCryptoTradeTicks(request);

                default:
                    return Enumerable.Empty<Tick>();
            }
        }

        private IEnumerable<Tick> GetForexQuoteTicks(HistoryRequest request)
        {
            // https://api.polygon.io/v1/historic/forex/EUR/USD/2020-08-24?apiKey=

            var start = request.StartTimeUtc;
            var end = request.EndTimeUtc;

            while (start <= end)
            {
                using (var client = new WebClient())
                {
                    string baseCurrency;
                    string quoteCurrency;
                    Forex.DecomposeCurrencyPair(request.Symbol.Value, out baseCurrency, out quoteCurrency);

                    var offset = Convert.ToInt64(Time.DateTimeToUnixTimeStampMilliseconds(start));
                    var url = $"{HistoryBaseUrl}/v1/historic/forex/{baseCurrency}/{quoteCurrency}/{start.Date:yyyy-MM-dd}?apiKey={_apiKey}&offset={offset}";

                    var response = client.DownloadString(url);

                    var obj = JObject.Parse(response);
                    var objTicks = obj["ticks"];
                    if (objTicks.Type == JTokenType.Null)
                    {
                        // current date finished, move to next day
                        start = start.Date.AddDays(1);
                        continue;
                    }

                    foreach (var objTick in objTicks)
                    {
                        var row = objTick.ToObject<ForexQuoteTickResponse>();

                        var utcTime = Time.UnixMillisecondTimeStampToDateTime(row.Timestamp);

                        if (utcTime < start)
                        {
                            continue;
                        }

                        start = utcTime.AddMilliseconds(1);

                        if (utcTime > end)
                        {
                            yield break;
                        }

                        var time = GetTickTime(request.Symbol, utcTime);

                        yield return new Tick(time, request.Symbol, row.Bid, row.Ask);
                    }
                }
            }
        }

        private IEnumerable<Tick> GetCryptoTradeTicks(HistoryRequest request)
        {
            // https://api.polygon.io/v1/historic/crypto/BTC/USD/2020-08-24?apiKey=

            var start = request.StartTimeUtc;
            var end = request.EndTimeUtc;

            while (start <= end)
            {
                using (var client = new WebClient())
                {
                    var symbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(
                        request.Symbol.ID.Market,
                        request.Symbol,
                        request.Symbol.SecurityType,
                        Currencies.USD);

                    string baseCurrency;
                    string quoteCurrency;
                    Crypto.DecomposeCurrencyPair(request.Symbol, symbolProperties, out baseCurrency, out quoteCurrency);

                    var offset = Convert.ToInt64(Time.DateTimeToUnixTimeStampMilliseconds(start));
                    var url = $"{HistoryBaseUrl}/v1/historic/crypto/{baseCurrency}/{quoteCurrency}/{start.Date:yyyy-MM-dd}?apiKey={_apiKey}&offset={offset}";

                    var response = client.DownloadString(url);

                    var obj = JObject.Parse(response);
                    var objTicks = obj["ticks"];
                    if (objTicks.Type == JTokenType.Null)
                    {
                        // current date finished, move to next day
                        start = start.Date.AddDays(1);
                        continue;
                    }

                    foreach (var objTick in objTicks)
                    {
                        var row = objTick.ToObject<CryptoTradeTickResponse>();

                        var utcTime = Time.UnixMillisecondTimeStampToDateTime(row.Timestamp);

                        if (utcTime < start)
                        {
                            continue;
                        }

                        start = utcTime.AddMilliseconds(1);

                        if (utcTime > end)
                        {
                            yield break;
                        }

                        var market = GetMarketFromCryptoExchangeId(row.Exchange);
                        if (market != request.Symbol.ID.Market)
                        {
                            continue;
                        }

                        var time = GetTickTime(request.Symbol, utcTime);

                        yield return new Tick(time, request.Symbol, string.Empty, string.Empty, row.Size, row.Price);
                    }
                }
            }
        }


        private IEnumerable<Tick> GetEquityQuoteTicks(HistoryRequest request)
        {
            // https://api.polygon.io/v2/ticks/stocks/nbbo/SPY/2020-08-24?apiKey=

            var start = request.StartTimeUtc;
            var end = request.EndTimeUtc;

            while (start <= end)
            {
                using (var client = new WebClient())
                {
                    var offset = Time.DateTimeToUnixTimeStampNanoseconds(start);
                    var url = $"{HistoryBaseUrl}/v2/ticks/stocks/nbbo/{request.Symbol.Value}/{start.Date:yyyy-MM-dd}?apiKey={_apiKey}&timestamp={offset}";

                    var response = client.DownloadString(url);

                    var obj = JObject.Parse(response);
                    var objTicks = obj["results"];
                    if (objTicks.Type == JTokenType.Null || !objTicks.Any())
                    {
                        // current date finished, move to next day
                        start = start.Date.AddDays(1);
                        continue;
                    }

                    foreach (var objTick in objTicks)
                    {
                        var row = objTick.ToObject<EquityQuoteTickResponse>();

                        var utcTime = Time.UnixNanosecondTimeStampToDateTime(row.ExchangeTimestamp);

                        if (utcTime < start)
                        {
                            continue;
                        }

                        start = utcTime.AddMilliseconds(1);

                        if (utcTime > end)
                        {
                            yield break;
                        }

                        var time = GetTickTime(request.Symbol, utcTime);

                        yield return new Tick(time, request.Symbol, string.Empty, string.Empty, row.BidSize, row.BidPrice, row.AskSize, row.AskPrice);
                    }
                }
            }
        }

        private IEnumerable<Tick> GetEquityTradeTicks(HistoryRequest request)
        {
            // https://api.polygon.io/v2/ticks/stocks/trades/SPY/2020-08-24?apiKey=

            var start = request.StartTimeUtc;
            var end = request.EndTimeUtc;
            var currentDate = start.Date;

            while (currentDate <= end.Date)
            {
                Log.Trace($"GetEquityTradeTicks(): Downloading ticks for the date {currentDate:yyyy-MM-dd}; symbol: {request.Symbol.ID.Symbol}");

                // If this is a very first iteration set offset exactly as request's start time.
                // Otherwise use date start as an offset.
                var offset = currentDate == start.Date
                    ? Time.DateTimeToUnixTimeStampNanoseconds(start)
                    : Time.DateTimeToUnixTimeStampNanoseconds(currentDate);

                var counter = 0;
                EquityTradeTickResponse lastTick = null;
                while (true)
                {
                    counter++;

                    using (var client = new WebClient())
                    {
                        var url = $"{HistoryBaseUrl}/v2/ticks/stocks/trades/{request.Symbol.ID.Symbol}/{currentDate:yyyy-MM-dd}?" +
                                  $"apiKey={_apiKey}&timestamp={offset}&limit={BaseAggregateBarsLimit}";

                        string response;
                        try
                        {
                            response = client.DownloadString(url);
                        }
                        catch (WebException ex)
                        {
                            Log.Error($"GetEquityTradeTicks(): No data for {currentDate:yyyy-MM-dd}. Server Response: " + ex.Message);

                            // If server returned an error most likely on this day there is no data we are going to the next cycle
                            break;
                        }

                        // Get ticks
                        var obj = JObject.Parse(response)["results"]?.ToString();
                        var tradeTicksList = JsonConvert.DeserializeObject<EquityTradeTickResponse[]>(obj).ToList();

                        // The first results of the next page will coincide with last results of previous page
                        // We distinguish these results by exchange timestamp, lets clear from repeating values
                        var lastTickExchangeTimeStamp = lastTick?.ExchangeTimestamp ?? 0;
                        tradeTicksList = tradeTicksList.Where(x => x.ExchangeTimestamp != lastTickExchangeTimeStamp).ToList();

                        // API will send at the end only such repeating ticks that coincide with last results of previous page
                        // If there are no other ticks other than these then we break
                        if (!tradeTicksList.Any())
                        {
                            break;
                        }

                        Log.Trace($"GetEquityTradeTicks(): Page # {counter}; " +
                                  $"from: {Time.UnixNanosecondTimeStampToDateTime(tradeTicksList.First().ExchangeTimestamp)}; " +
                                  $"to: {Time.UnixNanosecondTimeStampToDateTime(tradeTicksList.Last().ExchangeTimestamp)}");

                        foreach (var row in tradeTicksList)
                        {
                            var utcTime = Time.UnixNanosecondTimeStampToDateTime(row.ExchangeTimestamp);

                            if (utcTime > end)
                            {
                                yield break;
                            }

                            yield return new Tick(GetTickTime(request.Symbol, utcTime), request.Symbol, string.Empty, string.Empty, row.Size, row.Price);

                            // Save the values before to jump to the next iteration
                            offset = row.ExchangeTimestamp;
                            lastTick = row;
                        }

                        _dataPointCount += tradeTicksList.Count;
                    }
                }

                // Jump to the next iteration
                currentDate = currentDate.AddDays(1);
            }
        }

        private IEnumerable<TradeBar> GetTradeBars(HistoryRequest request)
        {
            var historyTimespan = GetHistoryTimespan(request.Resolution);

            string tickerPrefix;
            switch (request.Symbol.SecurityType)
            {
                case SecurityType.Forex:
                    tickerPrefix = "C:";
                    break;

                case SecurityType.Crypto:
                    tickerPrefix = "X:";
                    break;

                default:
                    tickerPrefix = string.Empty;
                    break;
            }
            
            var resolutionTimeSpan = request.Resolution.ToTimeSpan();
            var lastRequestedBarStartTime = request.EndTimeUtc.RoundDown(resolutionTimeSpan);
            var start = request.StartTimeUtc.Date;
            var end = lastRequestedBarStartTime;

            // Perform a check of the number of bars requested, this must not exceed a static limit
            var aggregatesCountPerResolution = GetArrgegatesCountPerResoltion(request.Resolution);
            var dataRequestedCount = (end - start).Ticks
                                     / resolutionTimeSpan.Ticks / aggregatesCountPerResolution;

            if (dataRequestedCount > BaseAggregateBarsLimit)
            {
                end = start + TimeSpan.FromTicks(resolutionTimeSpan.Ticks * BaseAggregateBarsLimit / aggregatesCountPerResolution);
                end = end.Date;
            }

            while (start < lastRequestedBarStartTime.Date)
            {
                using (var client = new WebClient())
                {
                    var url = $"{HistoryBaseUrl}/v2/aggs/ticker/{tickerPrefix}{request.Symbol.Value}/range/1/{historyTimespan}/{start.Date:yyyy-MM-dd}/{end.Date:yyyy-MM-dd}" +
                              $"?apiKey={_apiKey}&limit={BaseAggregateBarsLimit}";

                    var response = client.DownloadString(url);
                    var aggregatesResponse = JsonConvert.DeserializeObject<AggregatesResponse>(response);
                    var rows = aggregatesResponse.Results;

                    if (rows != null)
                    {
                        foreach (var row in rows)
                        {
                            var utcTime = Time.UnixMillisecondTimeStampToDateTime(row.Timestamp);

                            if (utcTime < request.StartTimeUtc)
                            {
                                continue;
                            }

                            if (utcTime > request.EndTimeUtc.Add(request.Resolution.ToTimeSpan()))
                            {
                                yield break;
                            }

                            var time = GetTickTime(request.Symbol, utcTime);

                            yield return new TradeBar(time, request.Symbol, row.Open, row.High, row.Low, row.Close, row.Volume);
                        }
                    }
                }

                start = end.AddDays(1);
                end += TimeSpan.FromTicks(resolutionTimeSpan.Ticks * BaseAggregateBarsLimit / aggregatesCountPerResolution);

                if (end > lastRequestedBarStartTime)
                {
                    end = lastRequestedBarStartTime;
                }

                end = end.Date;
            }
        }

        private static IEnumerable<TradeBar> AggregateTradeTicks(Symbol symbol, IEnumerable<Tick> ticks, TimeSpan period)
        {
            return
                from t in ticks
                group t by t.Time.RoundDown(period)
                into g
                select new TradeBar
                {
                    Symbol = symbol,
                    Time = g.Key,
                    Open = g.First().LastPrice,
                    High = g.Max(t => t.LastPrice),
                    Low = g.Min(t => t.LastPrice),
                    Close = g.Last().LastPrice,
                    Volume = g.Sum(t => t.Quantity),
                    Period = period
                };
        }

        private static IEnumerable<QuoteBar> AggregateQuoteTicks(Symbol symbol, IEnumerable<Tick> ticks, TimeSpan period)
        {
            return
                from t in ticks
                group t by t.Time.RoundDown(period)
                into g
                select new QuoteBar
                {
                    Symbol = symbol,
                    Time = g.Key,
                    Bid = new Bar
                    {
                        Open = g.First().BidPrice,
                        High = g.Max(b => b.BidPrice),
                        Low = g.Min(b => b.BidPrice),
                        Close = g.Last().BidPrice
                    },
                    Ask = new Bar
                    {
                        Open = g.First().AskPrice,
                        High = g.Max(b => b.AskPrice),
                        Low = g.Min(b => b.AskPrice),
                        Close = g.Last().AskPrice
                    },
                    Period = period
                };
        }

        private static string GetHistoryTimespan(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Daily:
                    return "day";

                case Resolution.Hour:
                    return "hour";

                case Resolution.Minute:
                    return "minute";

                default:
                    throw new Exception($"PolygonDataQueueHandler.GetHistoryTimespan(): unsupported resolution: {resolution}.");
            }
        }

        private PolygonWebSocketClientWrapper GetWebSocket(SecurityType securityType)
        {
            PolygonWebSocketClientWrapper client;
            if (!_webSocketClientWrappers.TryGetValue(securityType, out client))
            {
                throw new Exception($"Unsupported security type: {securityType}");
            }

            return client;
        }

        private static bool CanSubscribe(Symbol symbol)
        {
            var securityType = symbol.ID.SecurityType;

            if (symbol.Value.IndexOfInvariant("universe", true) != -1) return false;

            return
                securityType == SecurityType.Equity ||
                securityType == SecurityType.Forex ||
                securityType == SecurityType.Crypto;
        }

        private void OnMessage(string message)
        {
            foreach (var obj in JArray.Parse(message))
            {
                var eventType = obj["ev"].ToString();

                switch (eventType)
                {
                    case "T":
                        ProcessEquityTrade(obj.ToObject<EquityTradeMessage>());
                        break;

                    case "Q":
                        ProcessEquityQuote(obj.ToObject<EquityQuoteMessage>());
                        break;

                    case "C":
                        ProcessForexQuote(obj.ToObject<ForexQuoteMessage>());
                        break;

                    case "XT":
                        ProcessCryptoTrade(obj.ToObject<CryptoTradeMessage>());
                        break;

                    case "XQ":
                        ProcessCryptoQuote(obj.ToObject<CryptoQuoteMessage>());
                        break;
                }
            }
        }

        private void ProcessEquityTrade(EquityTradeMessage trade)
        {
            var symbol = _symbolMapper.GetLeanSymbol(trade.Symbol, SecurityType.Equity, Market.USA);
            var time = GetTickTime(symbol, trade.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Trade,
                Symbol = symbol,
                Time = time,
                Value = trade.Price,
                Quantity = trade.Size
            };

            _dataAggregator.Update(tick);
        }

        private void ProcessEquityQuote(EquityQuoteMessage quote)
        {
            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Equity, Market.USA);
            var time = GetTickTime(symbol, quote.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Quote,
                Symbol = symbol,
                Time = time,
                AskPrice = quote.AskPrice,
                BidPrice = quote.BidPrice,
                AskSize = quote.AskSize,
                BidSize = quote.BidSize,
                Value = (quote.AskPrice + quote.BidPrice) / 2m
            };

            _dataAggregator.Update(tick);
        }

        private void ProcessForexQuote(ForexQuoteMessage quote)
        {
            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Forex, Market.FXCM);
            var time = GetTickTime(symbol, quote.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Quote,
                Symbol = symbol,
                Time = time,
                AskPrice = quote.AskPrice,
                BidPrice = quote.BidPrice,
                Value = (quote.AskPrice + quote.BidPrice) / 2m
            };

            _dataAggregator.Update(tick);
        }

        private void ProcessCryptoTrade(CryptoTradeMessage trade)
        {
            var market = GetMarketFromCryptoExchangeId(trade.ExchangeId);
            if (string.IsNullOrWhiteSpace(market))
            {
                return;
            }

            var symbol = _symbolMapper.GetLeanSymbol(trade.Symbol, SecurityType.Crypto, market);
            var time = GetTickTime(symbol, trade.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Trade,
                Symbol = symbol,
                Time = time,
                Value = trade.Price,
                Quantity = trade.Size
            };

            _dataAggregator.Update(tick);
        }

        private void ProcessCryptoQuote(CryptoQuoteMessage quote)
        {
            var market = GetMarketFromCryptoExchangeId(quote.ExchangeId);
            if (string.IsNullOrWhiteSpace(market))
            {
                return;
            }

            var symbol = _symbolMapper.GetLeanSymbol(quote.Symbol, SecurityType.Crypto, market);
            var time = GetTickTime(symbol, quote.Timestamp);

            var tick = new Tick
            {
                TickType = TickType.Quote,
                Symbol = symbol,
                Time = time,
                AskPrice = quote.AskPrice,
                BidPrice = quote.BidPrice,
                AskSize = quote.AskSize,
                BidSize = quote.BidSize,
                Value = (quote.AskPrice + quote.BidPrice) / 2m
            };

            _dataAggregator.Update(tick);
        }

        private DateTime GetTickTime(Symbol symbol, long timestamp)
        {
            var utcTime = Time.UnixMillisecondTimeStampToDateTime(timestamp);

            return GetTickTime(symbol, utcTime);
        }

        private DateTime GetTickTime(Symbol symbol, DateTime utcTime)
        {
            DateTimeZone exchangeTimeZone;
            if (!_symbolExchangeTimeZones.TryGetValue(symbol, out exchangeTimeZone))
            {
                // read the exchange time zone from market-hours-database
                exchangeTimeZone = _marketHoursDatabase.GetExchangeHours(symbol.ID.Market, symbol, symbol.SecurityType).TimeZone;
                _symbolExchangeTimeZones.Add(symbol, exchangeTimeZone);
            }

            return utcTime.ConvertFromUtc(exchangeTimeZone);
        }

        private string GetMarketFromCryptoExchangeId(int exchangeId)
        {
            string market;
            return _cryptoExchangeMap.TryGetValue(exchangeId, out market) ? market : string.Empty;
        }

        private static int GetArrgegatesCountPerResoltion(Resolution resolution)
        {
            switch (resolution)
            {
                case Resolution.Minute:
                    return 1;
                case Resolution.Hour:
                    return 60;
                case Resolution.Daily:
                    return 1;
                default:
                    throw new NotSupportedException($"No data aggregation for {resolution} resolution");
            }
        }
    }
}
