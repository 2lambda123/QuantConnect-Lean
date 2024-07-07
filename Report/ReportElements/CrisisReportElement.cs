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
using Deedle;
using Python.Runtime;
using QuantConnect.Packets;

namespace QuantConnect.Report.ReportElements
{
    internal sealed class CrisisReportElement : ChartReportElement
    {
        private const string _emptyChart =
            "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAABZ8AAAQPCAYAAAB/ZZXyAAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAAewgAAHsIBbtB1PgAAADh0RVh0U29mdHdhcmUAbWF0cGxvdGxpYiB2ZXJzaW9uMy4xLjIsIGh0dHA6Ly9tYXRwbG90bGliLm9yZy8li6FKAAAf+0lEQVR4nOzYwQkAIBDAMHX/nc8lCoIkE/TdPTOzAAAAAAAgdF4HAAAAAADwH/MZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkzGcAAAAAAHLmMwAAAAAAOfMZAAAAAICc+QwAAAAAQM58BgAAAAAgZz4DAAAAAJAznwEAAAAAyJnPAAAAAADkbjt2LAAAAAAwyN96EjsLI/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADs5DMAAAAAADv5DAAAAADATj4DAAAAALCTzwAAAAAA7OQzAAAAAAA7+QwAAAAAwE4+AwAAAACwk88AAAAAAOzkMwAAAAAAO/kMAAAAAMBOPgMAAAAAsJPPAAAAAADsAtkSDBqxWCsiAAAAAElFTkSuQmCC";
        private LiveResult _live;
        private BacktestResult _backtest;
        private string _template;

        /// <summary>
        /// Create a new array of crisis event plots
        /// </summary>
        /// <param name="name">Name of the widget</param>
        /// <param name="key">Location of injection</param>
        /// <param name="backtest">Backtest result object</param>
        /// <param name="live">Live result object</param>
        /// <param name="template">HTML template to use</param>
        public CrisisReportElement(
            string name,
            string key,
            BacktestResult backtest,
            LiveResult live,
            string template
        )
        {
            _live = live;
            _backtest = backtest;
            _template = template;
            Name = name;
            Key = key;
        }

        /// <summary>
        /// The generated output string to be injected
        /// </summary>
        public override string Render()
        {
            var backtestPoints = ResultsUtil.EquityPoints(_backtest);
            var backtestBenchmarkPoints = ResultsUtil.BenchmarkPoints(_backtest);

            var backtestSeries = new Series<DateTime, double>(
                backtestPoints.Keys,
                backtestPoints.Values
            );
            var backtestBenchmarkSeries = new Series<DateTime, double>(
                backtestBenchmarkPoints.Keys,
                backtestBenchmarkPoints.Values
            );

            var html = new List<string>();

            foreach (var crisisEvent in Crisis.Events)
            {
                using (Py.GIL())
                {
                    var crisis = crisisEvent.Value;
                    var data = new PyList();
                    var frame = Frame.CreateEmpty<DateTime, string>();

                    // The two following operations are equivalent to Pandas' `df.resample("D").sum()`
                    frame["Backtest"] = backtestSeries.ResampleEquivalence(
                        date => date.Date,
                        s => s.LastValue()
                    );
                    frame["Benchmark"] = backtestBenchmarkSeries.ResampleEquivalence(
                        date => date.Date,
                        s => s.LastValue()
                    );

                    var crisisFrame = frame.Where(kvp =>
                        kvp.Key >= crisis.Start && kvp.Key <= crisis.End
                    );
                    crisisFrame = crisisFrame.Join(
                        "BacktestPercent",
                        crisisFrame["Backtest"].CumulativeReturns()
                    );
                    crisisFrame = crisisFrame.Join(
                        "BenchmarkPercent",
                        crisisFrame["Benchmark"].CumulativeReturns()
                    );

                    // Pad out all missing values to start from 0 for nice plots
                    crisisFrame = crisisFrame.FillMissing(Direction.Forward).FillMissing(0.0);

                    data.Append(crisisFrame.RowKeys.ToList().ToPython());
                    data.Append(crisisFrame["BacktestPercent"].Values.ToList().ToPython());
                    data.Append(crisisFrame["BenchmarkPercent"].Values.ToList().ToPython());

                    var base64 = (string)
                        Charting.GetCrisisEventsPlots(
                            data,
                            crisis.Name.Replace("/", "").Replace(".", "").Replace(" ", "")
                        );

                    if (base64 == _emptyChart)
                    {
                        continue;
                    }

                    if (!crisisFrame.IsEmpty)
                    {
                        var contents = _template.Replace(
                            ReportKey.CrisisTitle,
                            crisis.ToString(
                                crisisFrame.GetRowKeyAt(0),
                                crisisFrame.GetRowKeyAt(crisisFrame.RowCount - 1)
                            )
                        );
                        contents = contents.Replace(ReportKey.CrisisContents, base64);

                        html.Add(contents);
                    }
                }
            }

            if (Key == ReportKey.CrisisPageStyle)
            {
                if (html.Count == 0)
                {
                    return "display: none;";
                }

                return string.Empty;
            }

            return string.Join("\n", html);
        }
    }
}
