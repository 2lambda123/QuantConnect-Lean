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

using NUnit.Framework;
using Python.Runtime;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Securities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantConnect.Tests.Algorithm.Framework.Portfolio
{
    [TestFixture]
    public class LongOnlyInsightWeightingPortfolioConstructionModelTests : InsightWeightingPortfolioConstructionModelTests
    {
        private const double _weight = 0.01;

        //public override double? Weight => Algorithm.Securities.Count == 0 ? default(double) : 1d / Algorithm.Securities.Count;

        public override PortfolioBias PortfolioBias => PortfolioBias.Long;

        public override IPortfolioConstructionModel GetPortfolioConstructionModel(Language language, dynamic paramenter = null)
        {
            if (language == Language.CSharp)
            {
                return InsightWeightingPortfolioConstructionModel.LongOnly(paramenter);
            }

            using (Py.GIL())
            {
                const string name = nameof(InsightWeightingPortfolioConstructionModel);
                var instance = Py.Import(name).GetAttr(name).GetAttr("LongOnly").Invoke(((object)paramenter).ToPython());
                return new PortfolioConstructionModelPythonWrapper(instance);
            }
        }

        public override List<IPortfolioTarget> GetTargetsForSPY()
        {
            return new List<IPortfolioTarget> { PortfolioTarget.Percent(Algorithm, Symbols.SPY, 0m) };
        }
    }
}