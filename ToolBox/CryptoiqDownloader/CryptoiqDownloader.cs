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
using System.Collections.Generic;
using System.Net;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using Newtonsoft.Json;
using System.Linq;

namespace QuantConnect.ToolBox.CryptoiqDownloader
{
    /// <summary>
    /// Cryptoiq Data Downloader class
    /// </summary>
    public class CryptoiqDownloader : IDataDownloader
    {
        private readonly string _exchange;

        /// <summary>
        /// Initializes a new instance of the <see cref="CryptoiqDownloader"/> class
        /// </summary>
        /// <param name="exchange">The bitcoin exchange</param>
        /// <param name="scaleFactor">Scale factor used to scale the data, useful for changing the BTC units</param>
        public CryptoiqDownloader(string exchange = Market.GDAX)
        {
            _exchange = exchange;
        }

        /// <summary>
        /// Get historical data enumerable for a single symbol, type and resolution given this start and end time (in UTC).
        /// </summary>
        /// <param name="dataDownloaderGetParameters">model class for passing in parameters for historical data</param>
        /// <returns>Enumerable of base data for this symbol</returns>
        public IEnumerable<BaseData> Get(DataDownloaderGetParameters dataDownloaderGetParameters)
        {
            var symbol = dataDownloaderGetParameters.Symbol;
            var resolution = dataDownloaderGetParameters.Resolution;
            var startUtc = dataDownloaderGetParameters.StartUtc;
            var endUtc = dataDownloaderGetParameters.EndUtc;
            var tickType = dataDownloaderGetParameters.TickType;

            if (tickType != TickType.Quote)
            {
                yield break;
            }
            if (resolution != Resolution.Tick)
            {
                throw new ArgumentException("Only tick data is currently supported.");
            }

            var hour = 1;
            var counter = startUtc;

            while (counter <= endUtc)
            {
                while (hour < 24)
                {
                    using var cl = new WebClient();

                    var request = $"http://cryptoiq.io/api/marketdata/ticker/{_exchange}/{symbol.Value}/{counter.ToStringInvariant("yyyy-MM-dd")}/{hour}";
                    var data = cl.DownloadString(request);

                    var mbtc = JsonConvert.DeserializeObject<List<CryptoiqBitcoin>>(data);
                    foreach (var item in mbtc.OrderBy(x => x.Time))
                    {
                        yield return new Tick
                        {
                            Time = item.Time,
                            Symbol = symbol,
                            Value = item.Last,
                            AskPrice = item.Ask,
                            BidPrice = item.Bid,
                            TickType = TickType.Quote
                        };
                    }
                    hour++;
                }
                counter = counter.AddDays(1);
                hour = 0;
            }
        }

    }
}
