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

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds.Auxiliary;
using QuantConnect.Lean.Engine.DataFeeds.Transport;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// Subscription data reader is a wrapper on the stream reader class to download, unpack and iterate over a data file.
    /// </summary>
    /// <remarks>The class accepts any subscription configuration and automatically makes it availble to enumerate</remarks>
    public class SubscriptionDataReader : IEnumerator<BaseData>
    {
        /// Source string to create memory stream:
        private SubscriptionDataSource _source;

        ///Default true to fillforward for this subscription, take the previous result and continue returning it till the next time barrier.
        private bool _isFillForward = true;

        ///Date of this source file.
        private DateTime _date = new DateTime();

        ///End of stream from the reader
        private bool _endOfStream = false;

        /// Internal stream reader for processing data line by line:
        private IStreamReader _reader = null;

        /// All streams done async via web protocols:
        private WebClient _web = new WebClient();

        /// Configuration of the data-reader:
        private SubscriptionDataConfig _config;

        /// Subscription Securities Access
        private Security _security;

        /// true if we can find a scale factor file for the security of the form: ..\Lean\Data\equity\market\factor_files\{SYMBOL}.csv
        private bool _hasScaleFactors = false;

        // Subscription is for a QC type:
        private bool _isDynamicallyLoadedData = false;

        //Symbol Mapping:
        private string _mappedSymbol = "";

        /// Location of the datafeed - the type of this data.
        private readonly DataFeedEndpoint _feedEndpoint;

        /// Object Activator - Fast create new instance of "Type":
        private readonly Func<object[], object> _objectActivator;

        ///Create a single instance to invoke all Type Methods:
        private readonly BaseData _dataFactory;

        /// Remember edge conditions as market enters/leaves open-closed.
        private BaseData _lastBarOfStream;
        private BaseData _lastBarOutsideMarketHours;

        //Start finish times of the backtest:
        private readonly DateTime _periodStart;
        private readonly DateTime _periodFinish;

        private readonly FactorFile _factorFile;
        private readonly MapFile _mapFile;

        // we set the price factor ratio when we encounter a dividend in the factor file
        // and on the next trading day we use this data to produce the dividend instance
        private decimal? _priceFactorRatio;

        // we set the split factor when we encounter a split in the factor file
        // and on the next trading day we use this data to produce the split instance
        private decimal? _splitFactor;

        // true if we're in live mode, false otherwise
        private readonly bool _isLiveMode;
        private readonly IResultHandler _resultHandler;
        private readonly IEnumerator<DateTime> _tradeableDates;

        /// <summary>
        /// Last read BaseData object from this type and source
        /// </summary>
        public BaseData Current
        {
            get;
            private set;
        }

        /// <summary>
        /// Explicit Interface Implementation for Current
        /// </summary>
        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Provides a means of exposing extra data related to this subscription.
        /// For now we expose dividend data for equities through here
        /// </summary>
        /// <remarks>
        /// It is currently assumed that whomever is pumping data into here is handling the
        /// time syncing issues. Dividends do this through the RefreshSource method
        /// </remarks>
        public Queue<BaseData> AuxiliaryData { get; private set; }

        /// <summary>
        /// Save an instance of the previous basedata we generated
        /// </summary>
        public BaseData Previous
        {
            get;
            private set;
        }

        /// <summary>
        /// Source has been completed, load up next stream or stop asking for data.
        /// </summary>
        public bool EndOfStream
        {
            get; 
            set;
        }

        /// <summary>
        /// Subscription data reader takes a subscription request, loads the type, accepts the data source and enumerate on the results.
        /// </summary>
        /// <param name="config">Subscription configuration object</param>
        /// <param name="security">Security asset</param>
        /// <param name="feed">Feed type enum</param>
        /// <param name="periodStart">Start date for the data request/backtest</param>
        /// <param name="periodFinish">Finish date for the data request/backtest</param>
        /// <param name="resultHandler"></param>
        /// <param name="tradeableDates">Defines the dates for which we'll request data, in order</param>
        public SubscriptionDataReader(SubscriptionDataConfig config, Security security, DataFeedEndpoint feed, DateTime periodStart, DateTime periodFinish, IResultHandler resultHandler, IEnumerable<DateTime> tradeableDates)
        {
            //Save configuration of data-subscription:
            _config = config;

            AuxiliaryData = new Queue<BaseData>();

            //Save access to fill foward flag:
            _isFillForward = config.FillDataForward;

            //Save Start and End Dates:
            _periodStart = periodStart;
            _periodFinish = periodFinish;

            //Save access to securities
            _security = security;
            _isDynamicallyLoadedData = security.IsDynamicallyLoadedData;
            _isLiveMode = _feedEndpoint == DataFeedEndpoint.LiveTrading;

            // do we have factor tables?
            _hasScaleFactors = FactorFile.HasScalingFactors(config.Symbol, config.Market);

            //Save the type of data we'll be getting from the source.
            _feedEndpoint = feed;

            //Create the dynamic type-activators:
            _objectActivator = ObjectActivator.GetActivator(config.Type);

            _resultHandler = resultHandler;
            _tradeableDates = tradeableDates.GetEnumerator();
            if (_objectActivator == null)
            {
                _resultHandler.ErrorMessage("Custom data type '" + config.Type.Name + "' missing parameterless constructor E.g. public " + config.Type.Name + "() { }");
                _endOfStream = true;
                return;
            }

            //Create an instance of the "Type":
            var userObj = _objectActivator.Invoke(new object[] { });
            _dataFactory = userObj as BaseData;

            //If its quandl set the access token in data factory:
            var quandl = _dataFactory as Quandl;
            if (quandl != null)
            {
                if (!Quandl.IsAuthCodeSet)
                {
                    Quandl.SetAuthCode(Config.Get("quandl-auth-token"));   
                }
            }

            //Load the entire factor and symbol mapping tables into memory, we'll start with some defaults
            _factorFile = new FactorFile(config.Symbol, new List<FactorFileRow>());
            _mapFile = new MapFile(config.Symbol, new List<MapFileRow>());
            try 
            {
                if (_hasScaleFactors)
                {
                    _factorFile = FactorFile.Read(config.Symbol, config.Market);
                    _mapFile = MapFile.Read(config.Symbol, config.Market);
                }
            } 
            catch (Exception err) 
            {
                Log.Error("SubscriptionDataReader(): Fetching Price/Map Factors: " + err.Message);
           }
        }
        
        #region New Implementation

        public bool MoveNext()
        {
            // yield the aux data first
            if (AuxiliaryData.Count != 0)
            {
                Previous = Current;
                Current = AuxiliaryData.Dequeue();
                return true;
            }

            Previous = Current;

            // get our reader, advancing to the next day if necessary
            var reader = ResolveReader();
            
            // if we were unable to resolve a reader it's because we're out of tradeable dates
            if (reader == null)
            {
                EndOfStream = true;
                return false;
            }

            // loop until we find data that passes all of our filters
            do
            {
                // if we've run out of data on our current reader then let's find the next one, this will advance our tradeable dates as well
                if (reader.EndOfStream)
                {
                    reader = ResolveReader();
                    // resolve reader will return null when we're finished with tradeable dates
                    if (reader == null)
                    {
                        EndOfStream = true;
                        return false;
                    }
                }

                // read in a line and then parse it using the data factory
                var line = reader.ReadLine();
                BaseData instance = null;
                try
                {
                    instance = _dataFactory.Reader(_config, line, _tradeableDates.Current, _isLiveMode);
                }
                catch (Exception err)
                {
                    // TODO: this should be an event, such as OnReaderError
                    _resultHandler.RuntimeError("Error invoking " + _config.Symbol + " data reader. Line: " + line + " Error: " + err.Message, err.StackTrace);
                }
                if (instance == null)
                {
                    continue;
                }

                if (instance.Time < _periodStart) // TODO : this check should probably be using EndTime
                {
                    // keep readnig until we get a value on or after the start
                    Previous = instance;
                    continue;
                }

                if (instance.Time > _periodFinish)
                {
                    // stop reading when we get a value after the end
                    EndOfStream = true;
                    return false;
                }

                // apply extra filters such as market hours and user filters
                try
                {
                    if (!_security.DataFilter.Filter(_security, instance))
                    {
                        // data has been filtered out by user code
                        continue;
                    }
                }
                catch (Exception err)
                {
                    // TODO : Eventually this type of filtering should be done later in the pipeline, after fill forward preferably (think of only wanting data at certain times of day, user may filter it out but fill forward will put it back in, but it will be a stale copy!)
                    Log.Error("SubscriptionDataReader.MoveNext(): Error applying filter: " + err.Message);
                    _resultHandler.RuntimeError("Runtime error applying data filter. Assuming filter pass: " + err.Message, err.StackTrace);
                }

                if (!Exchange.IsOpenDuringBar(instance.Time, instance.EndTime, _config.ExtendedMarketHours))
                {
                    // we're outside of market hours so we don't actually want to emit this data, but we've saved it off
                    // as previous so the data feed can have a recent value for fill forward logic
                    continue;
                }

                // we've made it past all of our filters, we're withing the requested start/end of the subscription,
                // we've satisfied user and market hour filters, so this data is good to go as current
                Current = instance;
                return true;
            }
            // keep looping, we control returning internally
            while (true);
        }

        private IStreamReader ResolveReader()
        {
            // if we still have data to emit, keep using this one
            if (_reader != null && !_reader.EndOfStream)
            {
                return _reader;
            }

            // if we're out of data and non-null, clean up old resources
            if (_reader != null)
            {
                _reader.Dispose();
            }

            DateTime date;
            // if don't have any more days to process then we're done
            if (!TryGetNextDate(out date))
            {
                return null;
            }

            _source = _dataFactory.GetSource(_config, date, _isLiveMode);

            // create our stream reader from our data source
            _reader = CreateStreamReader(_source);

            // if the created reader doesn't have any data, then advance to the next day to try again
            if (!(_reader != null && !_reader.EndOfStream))
            {
                // TODO: OnCreateStreamReaderError event
                Log.Error(string.Format("Failed to get StreamReader for data source({0}), symbol({1}). Skipping date({2}). Reader is null.", _source.Source, _mappedSymbol, date.ToShortDateString()));
                //Engine.ResultHandler.DebugMessage("We could not find the requested data. This may be an invalid data request, failed download of custom data, or a public holiday. Skipping date (" + date.ToShortDateString() + ").");
                if (_isDynamicallyLoadedData)
                {
                    _resultHandler.ErrorMessage(string.Format("We could not fetch the requested data. This may not be valid data, or a failed download of custom data. Skipping source ({0}).", _source));
                }
                return ResolveReader();
            }

            // we've finally found a reader with data!
            return _reader;
        }

        private IStreamReader CreateStreamReader(SubscriptionDataSource subscriptionDataSource)
        {
            IStreamReader reader;
            switch (subscriptionDataSource.TransportMedium)
            {
                case SubscriptionTransportMedium.LocalFile:
                    reader = HandleLocalFileSource(subscriptionDataSource.Source);
                    break;

                case SubscriptionTransportMedium.RemoteFile:
                    reader = HandleRemoteSourceFile(subscriptionDataSource.Source);
                    break;

                case SubscriptionTransportMedium.Rest:
                    reader = new RestSubscriptionStreamReader(subscriptionDataSource.Source);
                    break;

                default:
                    throw new InvalidEnumArgumentException("Unexpected SubscriptionTransportMedium specified: " + subscriptionDataSource.TransportMedium);
            }
            return reader;
        }

        private bool TryGetNextDate(out DateTime date)
        {
            while (_tradeableDates.MoveNext())
            {
                date = _tradeableDates.Current;
                if (!_mapFile.HasData(date))
                {
                    continue;
                }

                // check for dividends and split for this security
                CheckForDividend(date);
                CheckForSplit(date);

                // if we have factor files check to see if we need to update the scale factors
                if (_hasScaleFactors)
                {
                    // check to see if the symbol was remapped
                    _mappedSymbol = _mapFile.GetMappedSymbol(date);
                    _config.MappedSymbol = _mappedSymbol;

                    // update our price scaling factors in light of the normalization mode
                    UpdateScaleFactors(date);
                }

                // if the exchange is open then we should look for data for this data
                if (_security.Exchange.DateIsOpen(date))
                {
                    return true;
                }
            }

            date = DateTime.MaxValue.Date;
            return false;
        }

        #endregion

        /// <summary>
        /// For backwards adjusted data the price is adjusted by a scale factor which is a combination of splits and dividends. 
        /// This backwards adjusted price is used by default and fed as the current price.
        /// </summary>
        /// <param name="date">Current date of the backtest.</param>
        private void UpdateScaleFactors(DateTime date)
        {
            switch (_config.DataNormalizationMode)
            {
                case DataNormalizationMode.Raw:
                    return;
                
                case DataNormalizationMode.TotalReturn:
                case DataNormalizationMode.SplitAdjusted:
                    _config.PriceScaleFactor = _factorFile.GetSplitFactor(date);
                    break;

                case DataNormalizationMode.Adjusted:
                    _config.PriceScaleFactor = _factorFile.GetPriceScaleFactor(date);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Check if this time is open for this subscription.
        /// </summary>
        /// <param name="time">Date and time we're checking to see if the market is open</param>
        /// <returns>Boolean true on market open</returns>
        public bool IsMarketOpen(DateTime time) 
        {
            return _security.Exchange.DateTimeIsOpen(time);
        }

        /// <summary>
        /// Gets the associated exchange for this data reader/security
        /// </summary>
        public SecurityExchange Exchange
        {
            get { return _security.Exchange; }
        }

        /// <summary>
        /// Check if we're still in the extended market hours
        /// </summary>
        /// <param name="time">Time to scan</param>
        /// <returns>True on extended market hours</returns>
        public bool IsExtendedMarketOpen(DateTime time) 
        {
            return _security.Exchange.DateTimeIsExtendedOpen(time);
        }

        /// <summary>
        /// Reset the IEnumeration
        /// </summary>
        /// <remarks>Not used</remarks>
        public void Reset() 
        {
            throw new NotImplementedException("Reset method not implemented. Assumes loop will only be used once.");
        }

        /// <summary>
        /// Check for dividends and emit them into the aux data queue
        /// </summary>
        private void CheckForSplit(DateTime date)
        {
            if (_splitFactor != null)
            {
                var close = GetRawClose();
                var split = new Split(_config.Symbol, date, close, _splitFactor.Value);
                AuxiliaryData.Enqueue(split);
                _splitFactor = null;
            }

            decimal splitFactor;
            if (_factorFile.HasSplitEventOnNextTradingDay(date, out splitFactor))
            {
                _splitFactor = splitFactor;
            }
        }

        /// <summary>
        /// Check for dividends and emit them into the aux data queue
        /// </summary>
        private void CheckForDividend(DateTime date)
        {
            if (_priceFactorRatio != null)
            {
                var close = GetRawClose();
                var dividend = new Dividend(_config.Symbol, date, close, _priceFactorRatio.Value);
                // let the config know about it for normalization
                _config.SumOfDividends += dividend.Distribution;
                AuxiliaryData.Enqueue(dividend);
                _priceFactorRatio = null;
            }

            // check the factor file to see if we have a dividend event tomorrow
            decimal priceFactorRatio;
            if (_factorFile.HasDividendEventOnNextTradingDay(date, out priceFactorRatio))
            {
                _priceFactorRatio = priceFactorRatio;
            }
        }

        /// <summary>
        /// Un-normalizes the Previous.Value
        /// </summary>
        private decimal GetRawClose()
        {
            if (Previous == null) return 0m;

            var close = Previous.Value;

            switch (_config.DataNormalizationMode)
            {
                case DataNormalizationMode.Raw:
                    break;
                
                case DataNormalizationMode.SplitAdjusted:
                case DataNormalizationMode.Adjusted:
                    // we need to 'unscale' the price
                    close = close/_config.PriceScaleFactor;
                    break;
                
                case DataNormalizationMode.TotalReturn:
                    // we need to remove the dividends since we've been accumulating them in the price
                    close = (close - _config.SumOfDividends)/_config.PriceScaleFactor;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return close;
        }

        /// <summary>
        /// Dispose of the Stream Reader and close out the source stream and file connections.
        /// </summary>
        public void Dispose() 
        { 
            if (_reader != null) 
            {
                _reader.Close();
                _reader.Dispose();
            }

            if (_web != null) 
            {
                _web.Dispose();
            }
        }

        /// <summary>
        /// Opens up an IStreamReader for a local file source
        /// </summary>
        private IStreamReader HandleLocalFileSource(string source)
        {
            if (!File.Exists(source))
            {
                // the local uri doesn't exist, write an error and return null so we we don't try to get data for today
                Log.Trace("SubscriptionDataReader.GetReader(): Could not find QC Data, skipped: " + source);
                _resultHandler.SamplePerformance(_date.Date, 0);
                return null;
            }

            // handles zip or text files
            return new LocalFileSubscriptionStreamReader(source);
        }

        /// <summary>
        /// Opens up an IStreamReader for a remote file source
        /// </summary>
        private IStreamReader HandleRemoteSourceFile(string source)
        {
            // clean old files out of the cache
            if (!Directory.Exists(Constants.Cache)) Directory.CreateDirectory(Constants.Cache);
            foreach (var file in Directory.EnumerateFiles(Constants.Cache))
            {
                if (File.GetCreationTime(file) < DateTime.Now.AddHours(-24)) File.Delete(file);
            }

            try
            {
                // this will fire up a web client in order to download the 'source' file to the cache
                return new RemoteFileSubscriptionStreamReader(source, Constants.Cache);
            }
            catch (Exception err)
            {
                _resultHandler.ErrorMessage("Error downloading custom data source file, skipped: " + source + " Err: " + err.Message, err.StackTrace);
                _resultHandler.SamplePerformance(_date.Date, 0);
                return null;
            }
        }
    }
}