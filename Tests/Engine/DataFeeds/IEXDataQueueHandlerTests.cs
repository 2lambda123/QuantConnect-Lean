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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NodaTime;
using NUnit.Framework;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.ToolBox.IEX;

namespace QuantConnect.Tests.Engine.DataFeeds
{
    [TestFixture]
    [Explicit("Tests are dependent on network and are long")]
    public class IEXDataQueueHandlerTests
    {
        private readonly string _apiKey = Config.Get("iex-cloud-api-key");

        private static readonly string[] HardCodedSymbolsSNP = {
            "AAPL", "MSFT", "AMZN", "FB", "GOOGL", "GOOG", "BRK.B", "JNJ", "PG", "NVDA", "V", "JPM", "HD", "UNH", "MA", "VZ", 
            "PYPL", "NFLX", "ADBE", "DIS", "CRM", "CMCSA", "PFE", "WMT", "MRK", "T", "INTC", "TMO", "ABT", "KO", "PEP", "BAC", 
            "COST", "MCD", "NKE", "CSCO", "DHR", "NEE", "QCOM", "ABBV", "AVGO", "XOM", "ACN", "MDT", "TXN", "CVX", "BMY", 
            "AMGN", "LOW", "UNP", "HON", "LIN", "UPS", "PM", "ORCL", "LLY", "SBUX", "AMT", "NOW", "IBM", "AMD", "MMM", "WFC", 
            "C", "LMT", "CHTR", "BLK", "INTU", "CAT", "RTX", "ISRG", "BA", "SPGI", "FIS", "TGT", "ZTS", "MDLZ", "PLD", "GILD", 
            "CVS", "DE", "ANTM", "MS", "MO", "ADP", "D", "DUK", "BDX", "SYK", "BKNG", "CCI", "EQIX", "CL", "GS", "FDX", "GE", 
            "TMUS", "TJX", "SO", "APD", "ATVI", "CB", "CI", "SCHW", "CSX", "AXP", "REGN", "SHW", "ITW", "TFC", "MU", "AMAT", 
            "VRTX", "ICE", "CME", "ADSK", "FISV", "PGR", "DG", "NSC", "HUM", "USB", "MMC", "LRCX", "EL", "NEM", "BSX", "GPN", 
            "PNC", "ECL", "ILMN", "EW", "NOC", "KMB", "AEP", "GM", "ADI", "AON", "DD", "MCO", "WM", "ETN", "TWTR", "DLR", 
            "BAX", "EXC", "BIIB", "CTSH", "EMR", "ROP", "IDXX", "XEL", "SRE", "EA", "GIS", "LHX", "PSA", "CMG", "DOW", "CNC", 
            "APH", "COF", "SNPS", "HCA", "SBAC", "EBAY", "ORLY", "CMI", "DXCM", "TEL", "TT", "JCI", "A", "WEC", "KLAC", "COP", 
            "ALGN", "TRV", "ROST", "CDNS", "F", "GD", "PPG", "INFO", "XLNX", "PEG", "ES", "TROW", "IQV", "PCAR", "BLL", "VRSK", 
            "MSCI", "MET", "YUM", "MNST", "SYY", "CTAS", "BK", "ADM", "CARR", "STZ", "ALL", "MSI", "ZBH", "AWK", "ROK", "AIG", 
            "MCHP", "PH", "APTV", "ANSS", "ED", "AZO", "SWK", "PAYX", "CLX", "ALXN", "TDG", "BBY", "RMD", "FCX", "KR", "PRU", 
            "MAR", "OTIS", "FAST", "GLW", "HPQ", "SWKS", "CTVA", "WBA", "MTD", "HLT", "WLTW", "DTE", "LUV", "KMI", "MCK", "WMB", 
            "WELL", "CPRT", "AFL", "DHI", "MKC", "AME", "VFC", "DLTR", "CERN", "EIX", "CHD", "PPL", "FRC", "WY", "FTV", "MKTX", 
            "STT", "HSY", "WST", "ETR", "PSX", "O", "SLB", "AEE", "EOG", "LEN", "DAL", "DFS", "AJG", "KEYS", "KHC", "SPG", 
            "AMP", "VRSN", "LH", "AVB", "VMC", "MXIM", "MPC", "LYB", "FLT", "RSG", "PAYC", "HOLX", "TTWO", "ODFL", "CMS", 
            "ARE", "CDW", "IP", "FE", "CAG", "CBRE", "TSN", "EFX", "AMCR", "KSU", "FITB", "MLM", "DGX", "NTRS", "INCY", "DOV", 
            "FTNT", "VIAC", "COO", "EQR", "ETSY", "BR", "GWW", "LVS", "K", "VAR", "ZBRA", "XYL", "TYL", "AKAM", "TSCO", "VLO", 
            "EXR", "STE", "TFX", "EXPD", "DPZ", "PEAK", "QRVO", "VTR", "TER", "CTLT", "SIVB", "GRMN", "KMX", "NUE", "POOL", 
            "PKI", "MAS", "CTXS", "WAT", "TIF", "NDAQ", "NVR", "HIG", "DRE", "ABC", "SYF", "HRL", "OKE", "PXD", "CE", "FMC", 
            "CAH", "LNT", "GPC", "IR", "EXPE", "ESS", "AES", "MAA", "MTB", "RF", "BF.B", "EVRG", "SJM", "IEX", "KEY", "URI", 
            "J", "NLOK", "BIO", "CHRW", "DRI", "CNP", "WHR", "AVY", "ULTA", "ABMD", "CFG", "TDY", "JKHY", "FBHS", "EMN", "WDC", 
            "PHM", "HPE", "ANET", "ATO", "IFF", "LDOS", "PKG", "CINF", "HAS", "STX", "WAB", "IT", "HBAN", "HAL", "JBHT", 
            "HES", "BXP", "AAP", "PFG", "ALB", "OMC", "NTAP", "XRAY", "UAL", "RCL", "WRK", "CPB", "LW", "RJF", "PNW", "BKR", 
            "ALLE", "HSIC", "LKQ", "FOXA", "UDR", "MGM", "BWA", "PWR", "CBOE", "WU", "WRB", "SNA", "NI", "CXO", "PNR", 
            "ROL", "LUMN", "UHS", "L", "RE", "FFIV", "TXT", "GL", "NRG", "OXY", "AIZ", "IRM", "HST", "MYL", "LB", "COG", 
            "AOS", "IPG", "LYV", "WYNN", "CCL", "HWM", "IPGP", "JNPR", "DVA", "NWL", "MOS", "TPR", "DISH", "SEE", "TAP", 
            "LNC", "CMA", "HII", "PRGO", "HBI", "CF", "RHI", "REG", "MHK", "AAL", "DISCK", "LEG", "ZION", "IVZ", "BEN", "NWSA", 
            "NLSN", "FRT", "VNO", "DXC", "FLIR", "AIV", "PBCT", "ALK", "PVH", "KIM", "FANG", "FOX", "NCLH", "GPS", "VNT", "FLS", 
            "UNM", "RL", "XRX", "DVN", "MRO", "DISCA", "NOV", "APA", "SLG", "HFC", "UAA", "UA", "FTI", "NWS"
        };

        [SetUp]
        public void Setup()
        {
            Log.DebuggingEnabled = Config.GetBool("debug-mode");
        }

        private void ProcessFeed(IEnumerator<BaseData> enumerator, Action<BaseData> callback = null)
        {
            Task.Run(() =>
            {
                try
                {
                    while (enumerator.MoveNext())
                    {
                        BaseData tick = enumerator.Current;
                        callback?.Invoke(tick);
                    }
                }
                catch (AssertionException)
                {
                    throw;
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }
            });
        }

        /// <summary>
        /// Subscribe to multiple symbols in a series of calls
        /// </summary>
        [Test]
        public void IEXCouldSubscribeManyTimes()
        {
            var iex = new IEXDataQueueHandler();

            var configs = new[] {
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("GOOG", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("FB", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("AAPL", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("MSFT", SecurityType.Equity, Market.USA), Resolution.Second)
            };

            Array.ForEach(configs, (c) =>
            {
                ProcessFeed(
                    iex.Subscribe(c, (s, e) => { }),
                    tick =>
                    {
                        if (tick != null)
                        {
                            Console.WriteLine(tick.ToString());
                        }
                    });
            });

            Thread.Sleep(20000);

            Log.Trace("Unsubscribing from all except AAPL");

            Array.ForEach(configs, (c) =>
            {
                if (!string.Equals(c.Symbol.Value, "AAPL"))
                {
                    iex.Unsubscribe(c);
                }
            });

            Thread.Sleep(20000);

            iex.Dispose();
        }

        [Test]
        public void IEXCouldSubscribeAndUnsubscribe()
        {
            // MBLY is the most liquid IEX instrument
            var iex = new IEXDataQueueHandler();
            var unsubscribed = false;
            Action<BaseData> callback = (tick) =>
            {
                if (tick == null)
                    return;

                Console.WriteLine(tick.ToString());
                if (unsubscribed && tick.Symbol.Value == "MBLY")
                {
                    Assert.Fail("Should not receive data for unsubscribed symbol");
                }
            };

            var configs = new[] {
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("MBLY", SecurityType.Equity, Market.USA), Resolution.Second),
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create("USO", SecurityType.Equity, Market.USA), Resolution.Second)
            };

            Array.ForEach(configs, (c) =>
            {
                ProcessFeed(
                    iex.Subscribe(c, (s, e) => { }),
                    callback);
            });

            Thread.Sleep(20000);

            iex.Unsubscribe(configs.First(c => string.Equals(c.Symbol.Value, "MBLY")));

            Console.WriteLine("Unsubscribing");
            Thread.Sleep(2000);
            // some messages could be inflight, but after a pause all MBLY messages must have beed consumed by ProcessFeed
            unsubscribed = true;

            Thread.Sleep(20000);
            iex.Dispose();
        }

        [Test]
        public void IEXCouldSubscribeMoreThan100Symbols()
        {
            var configs = HardCodedSymbolsSNP.Select(s =>
                GetSubscriptionDataConfig<TradeBar>(Symbol.Create(s, SecurityType.Equity, Market.USA),
                    Resolution.Second)).ToArray();
            
            using (var iex = new IEXDataQueueHandler())
            {
                Array.ForEach(configs, dataConfig => iex.Subscribe(dataConfig, (s, e) => {  }));
                Thread.Sleep(20000);
                Assert.IsTrue(iex.IsConnected);
            }
        }

        [Test]
        public void IEXEventSourceCollectionSubscribes()
        {
            // Send few consecutive requests to subscribe to a large amount of symbols (after the first request to change the subscription)
            // and make sure no exception will be thrown - if event-source-collection can't subscribe to all it throws after timeout 
            Assert.DoesNotThrow(() =>
            {
                using (var events = new IEXEventSourceCollection((o, args) => { }, _apiKey))
                {
                    var rnd = new Random();
                    for (var i = 0; i < 5; i++)
                    {
                        // Shuffle and select first random amount of symbol
                        // in range from 300 to 500 (snp symbols count)
                        var shuffled = HardCodedSymbolsSNP
                            .OrderBy(n => Guid.NewGuid())
                            .ToArray();

                        var selected = shuffled
                            .Take(rnd.Next(300, shuffled.Length))
                            .ToArray();

                        events.UpdateSubscription(selected);
                    }
                }
            });
        }

        private SubscriptionDataConfig GetSubscriptionDataConfig<T>(Symbol symbol, Resolution resolution)
        {
            return new SubscriptionDataConfig(
                typeof(T),
                symbol,
                resolution,
                TimeZones.Utc,
                TimeZones.Utc,
                true,
                true,
                false);
        }


        #region History provider tests

        public static TestCaseData[] TestParameters
        {
            get
            {
                return new[]
                {
                    // valid parameters
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(15), true, false),
                    new TestCaseData(Symbols.SPY, Resolution.Minute, TimeSpan.FromDays(3), true, false),

                    // invalid resolution == empty result.
                    new TestCaseData(Symbols.SPY, Resolution.Tick, TimeSpan.FromSeconds(15), false, false),
                    new TestCaseData(Symbols.SPY, Resolution.Second, Time.OneMinute, false, false),
                    new TestCaseData(Symbols.SPY, Resolution.Hour, Time.OneDay, false, false),

                    // invalid period == empty result
                    new TestCaseData(Symbols.SPY, Resolution.Minute, TimeSpan.FromDays(45), false, false), // beyond 30 days
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(-15), false, false), // date in future
                    new TestCaseData(Symbols.SPY, Resolution.Daily, TimeSpan.FromDays(365*5.5), false, false), // beyond 5 years

                    // invalid symbol: XYZ -> not found WebException
                    new TestCaseData(Symbol.Create("XYZ", SecurityType.Equity, Market.FXCM), Resolution.Daily, TimeSpan.FromDays(15), false, true),

                    // invalid security type, no exception, empty result
                    new TestCaseData(Symbols.EURUSD, Resolution.Daily, TimeSpan.FromDays(15), false, false)
                };
            }
        }

        [Test, TestCaseSource(nameof(TestParameters))]
        public void IEXCouldGetHistory(Symbol symbol, Resolution resolution, TimeSpan period, bool received, bool throwsException)
        {
            TestDelegate test = () =>
            {
                var historyProvider = new IEXDataQueueHandler();
                historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null, null, null, null, null, null, false, new DataPermissionManager()));

                var now = DateTime.UtcNow;

                var requests = new[]
                {
                    new HistoryRequest(now.Add(-period),
                                       now,
                                       typeof(QuoteBar),
                                       symbol,
                                       resolution,
                                       SecurityExchangeHours.AlwaysOpen(TimeZones.Utc),
                                       DateTimeZone.Utc,
                                       Resolution.Minute,
                                       false,
                                       false,
                                       DataNormalizationMode.Adjusted,
                                       TickType.Quote)
                };

                var history = historyProvider.GetHistory(requests, TimeZones.Utc);

                foreach (var slice in history)
                {
                    if (resolution == Resolution.Tick || resolution == Resolution.Second || resolution == Resolution.Hour)
                    {
                        Log.Trace($"IEXCouldGetHistory(): Invalid resolution {resolution}");

                        Assert.IsNull(slice);
                    }
                    else if (resolution == Resolution.Daily || resolution == Resolution.Minute)
                    {
                        Assert.IsNotNull(slice);

                        var bar = slice.Bars[symbol];

                        Log.Trace("{0}: {1} - O={2}, H={3}, L={4}, C={5}: {6}, {7}", bar.Time, bar.Symbol, bar.Open, bar.High, bar.Low, bar.Close, resolution, period);
                    }
                }

                Log.Trace("Data points retrieved: " + historyProvider.DataPointCount);
                if (received)
                {
                    Assert.IsTrue(historyProvider.DataPointCount > 0);
                }
                else
                {
                    Assert.IsTrue(historyProvider.DataPointCount == 0);
                }
            };

            if (throwsException)
            {
                Assert.That(test, Throws.Exception);
            }
            else
            {
                Assert.DoesNotThrow(test);
            }
        }

        #endregion

    }
}