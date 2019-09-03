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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.ToolBox.AlgoSeekFuturesConverter;
using QuantConnect.ToolBox.AlgoSeekOptionsConverter;
using QuantConnect.ToolBox.BitfinexDownloader;
using QuantConnect.ToolBox.CoarseUniverseGenerator;
using QuantConnect.ToolBox.CoinApiDataConverter;
using QuantConnect.ToolBox.CryptoiqDownloader;
using QuantConnect.ToolBox.DukascopyDownloader;
using QuantConnect.ToolBox.EstimizeDataDownloader;
using QuantConnect.ToolBox.FxcmDownloader;
using QuantConnect.ToolBox.FxcmVolumeDownload;
using QuantConnect.ToolBox.GDAXDownloader;
using QuantConnect.ToolBox.IBDownloader;
using QuantConnect.ToolBox.IEX;
using QuantConnect.ToolBox.IQFeedDownloader;
using QuantConnect.ToolBox.IVolatilityEquityConverter;
using QuantConnect.ToolBox.KaikoDataConverter;
using QuantConnect.ToolBox.KrakenDownloader;
using QuantConnect.ToolBox.NseMarketDataConverter;
using QuantConnect.ToolBox.OandaDownloader;
using QuantConnect.ToolBox.PsychSignalDataConverter;
using QuantConnect.ToolBox.QuandlBitfinexDownloader;
using QuantConnect.ToolBox.QuantQuoteConverter;
using QuantConnect.ToolBox.RandomDataGenerator;
using QuantConnect.ToolBox.SECDataDownloader;
using QuantConnect.ToolBox.TradingEconomicsDataDownloader;
using QuantConnect.ToolBox.USTreasuryYieldCurve;
using QuantConnect.ToolBox.YahooDownloader;
using QuantConnect.Util;
using QuantConnect.ToolBox.SmartInsider;

namespace QuantConnect.ToolBox
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var optionsObject = ToolboxArgumentParser.ParseArguments(args);

            // if no console args supplied check if config.json has working info
            if (optionsObject.Count == 0)
            {
                optionsObject = LoadOptionsObjectFromConfigurationFile("config.json");
            }

            var targetApp = GetParameterOrExit(optionsObject, "app").ToLower();

            if (targetApp.Contains("download") || targetApp.EndsWith("dl"))
            {
                var fromDate = DateTime.ParseExact(GetParameterOrExit(optionsObject, "from-date"), "yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture);
                var resolution = optionsObject.ContainsKey("resolution") ? optionsObject["resolution"].ToString() : "";
                var tickers = optionsObject.ContainsKey("tickers")
                    ? (optionsObject["tickers"] as Dictionary<string, object>)?.Keys.ToList()
                    : new List<string>();
                var toDate = optionsObject.ContainsKey("to-date")
                    ? DateTime.ParseExact(optionsObject["to-date"].ToString(), "yyyyMMdd-HH:mm:ss", CultureInfo.InvariantCulture)
                    : DateTime.UtcNow;
                switch (targetApp)
                {
                    case "gdaxdl":
                    case "gdaxdownloader":
                        GDAXDownloaderProgram.GDAXDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "cdl":
                    case "cryptoiqdownloader":
                        CryptoiqDownloaderProgram.CryptoiqDownloader(tickers, GetParameterOrExit(optionsObject, "exchange"), fromDate, toDate);
                        break;
                    case "ddl":
                    case "dukascopydownloader":
                        DukascopyDownloaderProgram.DukascopyDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "fdl":
                    case "fxcmdownloader":
                        FxcmDownloaderProgram.FxcmDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "fvdl":
                    case "fxcmvolumedownload":
                        FxcmVolumeDownloadProgram.FxcmVolumeDownload(tickers, resolution, fromDate, toDate);
                        break;
                    case "ibdl":
                    case "ibdownloader":
                        IBDownloaderProgram.IBDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "iexdl":
                    case "iexdownloader":
                        IEXDownloaderProgram.IEXDownloader(tickers, resolution, fromDate, toDate, GetParameterOrExit(optionsObject, "api-key"));
                        break;
                    case "iqfdl":
                    case "iqfeeddownloader":
                        IQFeedDownloaderProgram.IQFeedDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "kdl":
                    case "krakendownloader":
                        KrakenDownloaderProgram.KrakenDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "odl":
                    case "oandadownloader":
                        OandaDownloaderProgram.OandaDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "qbdl":
                    case "quandlbitfinexdownloader":
                        QuandlBitfinexDownloaderProgram.QuandlBitfinexDownloader(fromDate, GetParameterOrExit(optionsObject, "api-key"));
                        break;
                    case "ydl":
                    case "yahoodownloader":
                        YahooDownloaderProgram.YahooDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "bfxdl":
                    case "bitfinexdownloader":
                        BitfinexDownloaderProgram.BitfinexDownloader(tickers, resolution, fromDate, toDate);
                        break;
                    case "secdl":
                    case "secdownloader":
                        SECDataDownloaderProgram.SECDataDownloader(
                            GetParameterOrExit(optionsObject, "destination-dir"),
                            fromDate,
                            toDate
                        );
                        break;
                    case "ecdl":
                    case "estimizeconsensusdownloader":
                        EstimizeConsensusDataDownloaderProgram.EstimizeConsensusDataDownloader();
                        break;
                    case "eedl":
                    case "estimizeestimatedownloader":
                        EstimizeEstimateDataDownloaderProgram.EstimizeEstimateDataDownloader();
                        break;
                    case "erdl":
                    case "estimizereleasedownloader":
                        EstimizeReleaseDataDownloaderProgram.EstimizeReleaseDataDownloader();
                        break;

                    case "psdl":
                    case "psychsignaldownloader":
                        PsychSignalDataConverterProgram.PsychSignalDataDownloader(
                            fromDate,
                            toDate,
                            GetParameterOrDefault(optionsObject, "destination-dir", Path.Combine(Globals.DataFolder, "alternative", "psychsignal", "raw-psychsignal")),
                            GetParameterOrExit(optionsObject, "api-key"),
                            GetParameterOrDefault(optionsObject, "data-source", "twitter_enhanced_withretweets,stocktwits"));
                        break;
                    case "ustycdl":
                    case "ustreasuryyieldcurvedownloader":
                        USTreasuryYieldCurveProgram.USTreasuryYieldCurveRateDownloader(
                            fromDate,
                            toDate,
                            GetParameterOrExit(optionsObject, "destination-dir")
                        );
                        break;

                    default:
                        PrintMessageAndExit(1, "ERROR: Unrecognized --app value");
                        break;
                }
            }
            else
            {
                switch (targetApp)
                {
                    case "asfc":
                    case "algoseekfuturesconverter":
                        AlgoSeekFuturesProgram.AlgoSeekFuturesConverter(GetParameterOrExit(optionsObject, "date"));
                        break;
                    case "asoc":
                    case "algoseekoptionsconverter":
                        AlgoSeekOptionsConverterProgram.AlgoSeekOptionsConverter(GetParameterOrExit(optionsObject, "date"));
                        break;
                    case "ivec":
                    case "ivolatilityequityconverter":
                        IVolatilityEquityConverterProgram.IVolatilityEquityConverter(GetParameterOrExit(optionsObject, "source-dir"),
                                                                                     GetParameterOrExit(optionsObject, "source-meta-dir"),
                                                                                     GetParameterOrExit(optionsObject, "destination-dir"),
                                                                                     GetParameterOrExit(optionsObject, "resolution"));
                        break;
                    case "kdc":
                    case "kaikodataconverter":
                        KaikoDataConverterProgram.KaikoDataConverter(GetParameterOrExit(optionsObject, "source-dir"),
                                                                     GetParameterOrExit(optionsObject, "date"),
                                                                     GetParameterOrDefault(optionsObject, "exchange", string.Empty));
                        break;
                    case "cadc":
                    case "coinapidataconverter":
                        CoinApiDataConverterProgram.CoinApiDataProgram(GetParameterOrExit(optionsObject, "date"), GetParameterOrExit(optionsObject, "market"),
                            GetParameterOrExit(optionsObject, "source-dir"), GetParameterOrExit(optionsObject, "destination-dir"));
                        break;
                    case "nmdc":
                    case "nsemarketdataconverter":
                        NseMarketDataConverterProgram.NseMarketDataConverter(GetParameterOrExit(optionsObject, "source-dir"),
                                                                             GetParameterOrExit(optionsObject, "destination-dir"));
                        break;
                    case "qqc":
                    case "quantquoteconverter":
                        QuantQuoteConverterProgram.QuantQuoteConverter(GetParameterOrExit(optionsObject, "destination-dir"),
                                                                       GetParameterOrExit(optionsObject, "source-dir"),
                                                                       GetParameterOrExit(optionsObject, "resolution"));
                        break;
                    case "cug":
                    case "coarseuniversegenerator":
                        CoarseUniverseGeneratorProgram.CoarseUniverseGenerator();
                        break;
                    case "rdg":
                    case "randomdatagenerator":
                        RandomDataGeneratorProgram.RandomDataGenerator(
                            GetParameterOrExit(optionsObject, "start"),
                            GetParameterOrExit(optionsObject, "end"),
                            GetParameterOrExit(optionsObject, "symbol-count"),
                            GetParameterOrDefault(optionsObject, "market", null),
                            GetParameterOrDefault(optionsObject, "security-type", "Equity"),
                            GetParameterOrDefault(optionsObject, "resolution", "Minute"),
                            GetParameterOrDefault(optionsObject, "data-density", "Dense"),
                            GetParameterOrDefault(optionsObject, "include-coarse", "true"),
                            GetParameterOrDefault(optionsObject, "quote-trade-ratio", "1"),
                            GetParameterOrDefault(optionsObject, "random-seed", null),
                            GetParameterOrDefault(optionsObject, "ipo-percentage", "5.0"),
                            GetParameterOrDefault(optionsObject, "rename-percentage", "30.0"),
                            GetParameterOrDefault(optionsObject, "splits-percentage", "15.0"),
                            GetParameterOrDefault(optionsObject, "dividends-percentage", "60.0"),
                            GetParameterOrDefault(optionsObject, "dividend-every-quarter-percentage", "30.0")
                        );
                        break;
                    case "seccv":
                    case "secconverter":
                        var start = DateTime.ParseExact(GetParameterOrExit(optionsObject, "date"), "yyyyMMdd", CultureInfo.InvariantCulture);
                        SECDataDownloaderProgram.SECDataConverter(
                            GetParameterOrExit(optionsObject, "source-dir"),
                            GetParameterOrDefault(optionsObject, "destination-dir", Globals.DataFolder),
                            start);
                        break;
                    case "psdc":
                    case "psychsignaldataconverter":
                        PsychSignalDataConverterProgram.PsychSignalDataConverter(
                            GetParameterOrExit(optionsObject, "date"),
                            GetParameterOrExit(optionsObject, "source-dir"),
                            GetParameterOrExit(optionsObject, "destination-dir"));
                        break;
                    case "ustyccv":
                    case "ustreasuryyieldcurveconverter":
                        USTreasuryYieldCurveProgram.USTreasuryYieldCurveConverter(
                            GetParameterOrExit(optionsObject, "source-dir"),
                            GetParameterOrExit(optionsObject, "destination-dir"));
                        break;
                    case "sidc":
                    case "smartinsiderconverter":
                        SmartInsiderProgram.SmartInsiderConverter(
                            DateTime.ParseExact(GetParameterOrExit(optionsObject, "date"), "yyyyMMdd", CultureInfo.InvariantCulture),
                            GetParameterOrExit(optionsObject, "source-dir"),
                            GetParameterOrExit(optionsObject, "destination-dir"));
                        break;

                    default:
                        PrintMessageAndExit(1, "ERROR: Unrecognized --app value");
                        break;
                }
            }
        }

        private static void PrintMessageAndExit(int exitCode = 0, string message = "")
        {
            if (!message.IsNullOrEmpty())
            {
                Console.WriteLine("\n" + message);
            }
            Console.WriteLine("\nUse the '--help' parameter for more information");
            Console.WriteLine("Press any key to quit");
            Console.ReadLine();
            Environment.Exit(exitCode);
        }

        private static string GetParameterOrExit(IReadOnlyDictionary<string, object> optionsObject, string parameter)
        {
            if (!optionsObject.ContainsKey(parameter))
            {
                PrintMessageAndExit(1, "ERROR: REQUIRED parameter --" + parameter + "= is missing");
            }
            return optionsObject[parameter].ToString();
        }

        private static string GetParameterOrDefault(IReadOnlyDictionary<string, object> optionsObject, string parameter, string defaultValue)
        {
            object value;
            if (!optionsObject.TryGetValue(parameter, out value))
            {
                Console.WriteLine($"'{parameter}' was not specified. Using default value: '{defaultValue}'");
                return defaultValue;
            }

            return value.ToString();
        }

        /// <summary>
        /// Loads values from JSON text file
        /// </summary>
        private static Dictionary<string, object> LoadOptionsObjectFromConfigurationFile(string path)
        {
            var optionsObject = new Dictionary<string, object>();
            try
            {
                // Read the text from json and fill that in a specifal object that
                // will hold the values corresponding to json file contents
                var toolBoxOptionsConfig = JsonConvert.DeserializeObject<ToolBoxOptionsConfiguration>(File.ReadAllText(path));

                // iterate over the properties using reflection and check for non empty values to pupulate optionsObject.
                foreach (var prop in toolBoxOptionsConfig.GetType().GetProperties())
                {
                    if (prop.PropertyType == typeof(string))
                    {
                        // most of our values are strings, get value, if null or empty then continue; populate otherwise..
                        var valueAsString = (string) prop.GetValue(toolBoxOptionsConfig);
                        if (valueAsString.IsNullOrEmpty())
                            continue;

                        // be cautios of System.InvalidOperationException can happen here if there is no such attribute
                        var key = GetThePropertyNameFromJsonAttribute(prop);
                        optionsObject[key] = valueAsString;
                    }
                    else if(prop.PropertyType == typeof(string[]))
                    {
                        // if we reached here we are most probably working with 'tickers' array
                        var valueAsStringArray = (string[])prop.GetValue(toolBoxOptionsConfig);
                        if (valueAsStringArray.IsNullOrEmpty())
                            continue;

                        var key = GetThePropertyNameFromJsonAttribute(prop);

                        // I fully copy here a piece of code from ApplicationParser.cs that has to do with
                        // case CommandOptionType.MultipleValue - for the full compatibility!
                        var keyValuePairs = valueAsStringArray;
                        var subDictionary = new Dictionary<string, object>();
                        foreach (var keyValuePair in keyValuePairs)
                        {
                            var subKeys = keyValuePair.Split(':');
                            subDictionary[subKeys[0]] = ApplicationParser.ParseTypedArgument(subKeys.Length > 1 ? subKeys[1] : "");
                        }

                        optionsObject[key] = subDictionary;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return optionsObject;
        }

        private static string GetThePropertyNameFromJsonAttribute(PropertyInfo prop)
        {
            return prop.GetCustomAttributes(true)
                .Select(x => x as JsonPropertyAttribute)
                .Where(x => x != null)
                .Select(x => x.PropertyName).First();
        }
    }
}
