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
using System.Linq;
using Moq;
using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Tests.Engine.DataFeeds;
using QuantConnect.Tests.Research;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture]
    public class AlgorithmIndicatorsTests
    {
        private QCAlgorithm _algorithm;
        private Symbol _equity;
        private Symbol _option;

        [SetUp]
        public void Setup()
        {
            _algorithm = new AlgorithmStub();
            var historyProvider = new SubscriptionDataReaderHistoryProvider();
            historyProvider.Initialize(new HistoryProviderInitializeParameters(null, null,
                TestGlobals.DataProvider, TestGlobals.DataCacheProvider, TestGlobals.MapFileProvider, TestGlobals.FactorFileProvider,
                null, true, new DataPermissionManager(), _algorithm.ObjectStore, _algorithm.Settings));
            _algorithm.SetHistoryProvider(historyProvider);

            _algorithm.SetDateTime(new DateTime(2013, 10, 11, 15, 0, 0));
            _equity = _algorithm.AddEquity("SPY").Symbol;
            _option = Symbol.CreateOption("SPY", Market.USA, OptionStyle.American, OptionRight.Call, 450m, new DateTime(2023, 9, 1));
            _algorithm.AddOptionContract(_option);
            _algorithm.Settings.AutomaticIndicatorWarmUp = true;
        }

        [Test]
        public void IndicatorsPassSelectorToWarmUp()
        {
            var mockSelector = new Mock<Func<IBaseData, TradeBar>>();
            mockSelector.Setup(_ => _(It.IsAny<IBaseData>())).Returns<TradeBar>(_ => (TradeBar)_);

            var indicator = _algorithm.ABANDS(Symbols.SPY, 20, selector: mockSelector.Object);

            Assert.IsTrue(indicator.IsReady);
            mockSelector.Verify(_ => _(It.IsAny<IBaseData>()), Times.Exactly(indicator.WarmUpPeriod));
        }

        [Test]
        public void SharpeRatioIndicatorUsesAlgorithmsRiskFreeRateModelSetAfterIndicatorRegistration()
        {
            // Register indicator
            var sharpeRatio = _algorithm.SR(Symbols.SPY, 10);

            // Setup risk free rate model
            var interestRateProviderMock = new Mock<IRiskFreeInterestRateModel>();
            var reference = new DateTime(2023, 11, 21, 10, 0, 0);
            interestRateProviderMock.Setup(x => x.GetInterestRate(reference)).Verifiable();

            // Update indicator
            sharpeRatio.Update(new IndicatorDataPoint(Symbols.SPY, reference, 300m));

            // Our interest rate provider shouldn't have been called yet since it's hasn't been set to the algorithm
            interestRateProviderMock.Verify(x => x.GetInterestRate(reference), Times.Never);

            // Set the interest rate provider to the algorithm
            _algorithm.SetRiskFreeInterestRateModel(interestRateProviderMock.Object);

            // Update indicator
            sharpeRatio.Update(new IndicatorDataPoint(Symbols.SPY, reference, 300m));

            // Our interest rate provider should have been called once
            interestRateProviderMock.Verify(x => x.GetInterestRate(reference), Times.Once);
        }

        [TestCase("Span", Language.CSharp)]
        [TestCase("Count", Language.CSharp)]
        [TestCase("StartAndEndDate", Language.CSharp)]
        [TestCase("Span", Language.Python)]
        [TestCase("Count", Language.Python)]
        [TestCase("StartAndEndDate", Language.Python)]
        public void IndicatorsDataPoint(string testCase, Language language)
        {
            var indicator = new BollingerBands(10, 2);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            int dataCount;

            DataHistory<IndicatorValues> indicatorValues;
            if (language == Language.CSharp)
            {
                if (testCase == "StartAndEndDate")
                {
                    indicatorValues = _algorithm.Indicator(indicator, _equity, new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                }
                else if (testCase == "Span")
                {
                    indicatorValues = _algorithm.Indicator(indicator, _equity, TimeSpan.FromDays(5), Resolution.Minute);
                }
                else
                {
                    indicatorValues = _algorithm.Indicator(indicator, _equity, (int)(4 * 60 * 6.5), Resolution.Minute);
                }
                // BollingerBands, upper, lower, mid bands, std, band width, percentB, price
                Assert.AreEqual(8, indicatorValues.Count);
                dataCount = indicatorValues.First().Values.Count;
                foreach (var indicatorValue in indicatorValues)
                {
                    Assert.AreEqual(dataCount, indicatorValue.Values.Count);
                }
                dataCount = indicatorValues.SelectMany(x => x.Values).DistinctBy(y => y.EndTime).Count();
            }
            else
            {
                using (Py.GIL())
                {
                    if (testCase == "StartAndEndDate")
                    {
                        indicatorValues = _algorithm.Indicator(indicator.ToPython(), _equity.ToPython(), new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                    }
                    else if (testCase == "Span")
                    {
                        indicatorValues = _algorithm.Indicator(indicator.ToPython(), _equity.ToPython(), TimeSpan.FromDays(5), Resolution.Minute);
                    }
                    else
                    {
                        indicatorValues = _algorithm.Indicator(indicator.ToPython(), _equity.ToPython(), (int)(4 * 60 * 6.5), Resolution.Minute);
                    }
                    dataCount = QuantBookIndicatorsTests.GetDataFrameLength(indicatorValues.DataFrame);
                }
            }
            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1551, dataCount);
        }

        [TestCase("Span", Language.CSharp)]
        [TestCase("Count", Language.CSharp)]
        [TestCase("StartAndEndDate", Language.CSharp)]
        [TestCase("Span", Language.Python)]
        [TestCase("Count", Language.Python)]
        [TestCase("StartAndEndDate", Language.Python)]
        public void IndicatorsBar(string testCase, Language language)
        {
            var indicator = new AverageTrueRange(10);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            DataHistory<IndicatorValues > indicatorValues;
            int dataCount;
            if (language == Language.CSharp)
            {
                if (testCase == "StartAndEndDate")
                {
                    indicatorValues = _algorithm.Indicator(indicator, _equity, new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                }
                else if (testCase == "Span")
                {
                    indicatorValues = _algorithm.Indicator(indicator, _equity, TimeSpan.FromDays(5), Resolution.Minute);
                }
                else
                {
                    indicatorValues = _algorithm.Indicator(indicator, _equity, (int)(4 * 60 * 6.5), Resolution.Minute);
                }
                // the TrueRange & the AVGTrueRange
                Assert.AreEqual(2, indicatorValues.Count);
                dataCount = indicatorValues.First().Values.Count;
                Assert.AreEqual(dataCount, indicatorValues.Skip(1).First().Values.Count);

                dataCount = indicatorValues.SelectMany(x => x.Values).DistinctBy(y => y.EndTime).Count();
            }
            else
            {
                using (Py.GIL())
                {
                    if (testCase == "StartAndEndDate")
                    {
                        indicatorValues = _algorithm.Indicator(indicator.ToPython(), _equity.ToPython(), new DateTime(2013, 10, 07), new DateTime(2013, 10, 11), Resolution.Minute);
                    }
                    else if (testCase == "Span")
                    {
                        indicatorValues = _algorithm.Indicator(indicator.ToPython(), _equity.ToPython(), TimeSpan.FromDays(5), Resolution.Minute);
                    }
                    else
                    {
                        indicatorValues = _algorithm.Indicator(indicator.ToPython(), _equity.ToPython(), (int)(4 * 60 * 6.5), Resolution.Minute);
                    }
                    dataCount = QuantBookIndicatorsTests.GetDataFrameLength(indicatorValues.DataFrame);
                }
            }
            Assert.IsTrue(indicator.IsReady);
            Assert.AreEqual(1551, dataCount);
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void IndicatorMultiSymbol(Language language)
        {
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new Beta(_equity, referenceSymbol, 10);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            int dataCount;
            if (language == Language.CSharp)
            {
                var indicatorValues = _algorithm.Indicator(indicator, new[] { _equity, referenceSymbol }, TimeSpan.FromDays(5));
                Assert.AreEqual(1, indicatorValues.Count);
                dataCount = indicatorValues.First().Values.Count;
            }
            else
            {
                using (Py.GIL())
                {
                    var pandasFrame = _algorithm.Indicator(indicator.ToPython(), (new[] { _equity, referenceSymbol }).ToPython(), TimeSpan.FromDays(5));
                    dataCount = QuantBookIndicatorsTests.GetDataFrameLength(pandasFrame.DataFrame);
                }
            }
            Assert.AreEqual(1549, dataCount);
            Assert.IsTrue(indicator.IsReady);
        }

        [Test]
        public void BetaCalculation()
        {
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new Beta(_equity, referenceSymbol, 10);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            var indicatorValues = _algorithm.Indicator(indicator, new[] { _equity, referenceSymbol }, TimeSpan.FromDays(50), Resolution.Daily);
            Assert.AreEqual(0.676480102032563m, indicatorValues.Last().Values.Last().Price);
        }

        [TestCase(Language.Python)]
        [TestCase(Language.CSharp)]
        public void IndicatorsPassingHistory(Language language)
        {
            var referenceSymbol = Symbol.Create("IBM", SecurityType.Equity, Market.USA);
            var indicator = new Beta(_equity, referenceSymbol, 10);
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));

            var history = _algorithm.History(new[] { _equity, referenceSymbol }, TimeSpan.FromDays(5), Resolution.Minute);
            int dataCount;
            if (language == Language.CSharp)
            {
                var indicatorValues = _algorithm.Indicator(indicator, history);
                Assert.AreEqual(1, indicatorValues.Count);
                dataCount = indicatorValues.First().Values.Count;
            }
            else
            {
                using (Py.GIL())
                {
                    var pandasFrame = _algorithm.Indicator(indicator.ToPython(), history);
                    dataCount = QuantBookIndicatorsTests.GetDataFrameLength(pandasFrame.DataFrame);
                }
            }
            Assert.AreEqual(1549, dataCount);
            Assert.IsTrue(indicator.IsReady);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void PythonCustomIndicator(int testCases)
        {
            _algorithm.SetDateTime(new DateTime(2013, 10, 11));
            using (Py.GIL())
            {
                PyModule module;
                if (testCases == 1)
                {
                    module = PyModule.FromString("PythonCustomIndicator",
                        @"
from AlgorithmImports import *
class GoodCustomIndicator(PythonIndicator):
    def __init__(self):
        self.Value = 0
    def Update(self, input):
        self.Value = input.Value
        return True");
                }
                else
                {
                    module = PyModule.FromString("PythonCustomIndicator",
                        @"
from AlgorithmImports import *
class GoodCustomIndicator:
    def __init__(self):
        self.IsReady = True
        self.Value = 0
    def Update(self, input):
        self.Value = input.Value
        return True");
                }

                var goodIndicator = module.GetAttr("GoodCustomIndicator").Invoke();
                var pandasFrame = _algorithm.Indicator(goodIndicator, _equity.ToPython(), TimeSpan.FromDays(5), Resolution.Minute);
                var dataCount = QuantBookIndicatorsTests.GetDataFrameLength(pandasFrame.DataFrame);

                Assert.IsTrue((bool)((dynamic)goodIndicator).IsReady);
                Assert.AreEqual(1559, dataCount);
            }
        }
    }
}
