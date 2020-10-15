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

using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Data.Auxiliary;
using QuantConnect.Data.Custom.Quiver;
using QuantConnect.Interfaces;
using QuantConnect.Logging;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace QuantConnect.ToolBox.QuiverDataDownloader
{

    public class QuiverHouseDataDownloader : QuiverDataDownloader
    {
        private readonly string _destinationFolder;
        private readonly MapFileResolver _mapFileResolver;

        /// <summary>
        /// Creates a new instance of <see cref="QuiverHouseDataDownloader"/>
        /// </summary>
        /// <param name="destinationFolder">The folder where the data will be saved</param>
        public QuiverHouseDataDownloader(string destinationFolder)
        {
            _destinationFolder = Path.Combine(destinationFolder, "house");
            _mapFileResolver = Composer.Instance.GetExportedValueByTypeName<IMapFileProvider>(Config.Get("map-file-provider", "LocalDiskMapFileProvider"))
                .Get(Market.USA);

            Directory.CreateDirectory(_destinationFolder);
        }

        /// <summary>
        /// Runs the instance of the object.
        /// </summary>
        /// <returns>True if process all downloads successfully</returns>
        public override bool Run()
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var companies = GetCompanies().Result.DistinctBy(x => x.Ticker).ToList();
                var count = companies.Count;
                var currentPercent = 0.05;
                var percent = 0.05;
                var i = 0;

                Log.Trace($"QuiverHouseDataDownloader.Run(): Start processing {count.ToStringInvariant()} companies");

                var tasks = new List<Task>();

                foreach (var company in companies)
                {
                    // Include tickers that are "defunct".
                    // Remove the tag because it cannot be part of the API endpoint.
                    // This is separate from the NormalizeTicker(...) method since
                    // we don't convert tickers with `-`s into the format we can successfully
                    // index mapfiles with.
                    var quiverTicker = company.Ticker;
                    string ticker;


                    if (!TryNormalizeDefunctTicker(quiverTicker, out ticker))
                    {
                        Log.Error($"QuiverHouseDataDownloader(): Defunct ticker {quiverTicker} is unable to be parsed. Continuing...");
                        continue;
                    }

                    // Begin processing ticker with a normalized value
                    Log.Trace($"QuiverHouseDataDownloader.Run(): Processing {ticker}");

                    // Makes sure we don't overrun Quiver rate limits accidentally
                    System.Threading.Thread.Sleep(1);
                    IndexGate.WaitToProceed();

                    tasks.Add(
                        HttpRequester($"historical/housetrading/{ticker}")
                            .ContinueWith(
                                y =>
                                {
                                    i++;

                                    if (y.IsFaulted)
                                    {
                                        Log.Error($"QuiverHouseDataDownloader.Run(): Failed to get data for {company}");
                                        return;
                                    }

                                    var result = y.Result;
                                    if (string.IsNullOrEmpty(result))
                                    {
                                        // We've already logged inside HttpRequester
                                        return;
                                    }
                                    Log.Trace(result);

                                    var followers = JsonConvert.DeserializeObject<List<QuiverHouse>>(result, JsonSerializerSettings);

                                    foreach (var kvp in followers)
                                    {
                                        var csvContents = new string[] {
                                            $"{kvp.Date.ToStringInvariant("M/d/yyyy h:mm:ss tt")}," +
                                            $"{kvp.Ticker}," +
                                            $"{kvp.Representative}," +
                                            $"{kvp.Transaction}," +
                                            $"{kvp.Amount}"
                                        };
                                        SaveContentToFile(_destinationFolder, kvp.Ticker, csvContents);
                                    }

                                    var percentageDone = i / count;
                                    if (percentageDone >= currentPercent)
                                    {
                                        Log.Trace($"QuiverHouseDataDownloader.Run(): {percentageDone.ToStringInvariant("P2")} complete");
                                        currentPercent += percent;
                                    }
                                }
                            )
                    );
                }

                Task.WaitAll(tasks.ToArray());
            }
            catch (Exception e)
            {
                Log.Error(e);
                return false;
            }

            Log.Trace($"QuiverHouseDataDownloader.Run(): Finished in {stopwatch.Elapsed.ToStringInvariant(null)}");
            return true;
        }
    }
}