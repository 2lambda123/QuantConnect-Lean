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
 *
*/

using QLNet;
using System;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Market;

namespace QuantConnect.Securities.Option
{
    using Logging;
    using PricingEngineFunc = Func<GeneralizedBlackScholesProcess, IPricingEngine>;
    using PricingEngineFuncEx = Func<Symbol, GeneralizedBlackScholesProcess, IPricingEngine>;

    /// <summary>
    /// Provides QuantLib(QL) implementation of <see cref="IOptionPriceModel"/> to support major option pricing models, available in QL.
    /// </summary>
    public class QLOptionPriceModel : IOptionPriceModel
    {
        private static readonly OptionStyle[] _defaultAllowedOptionStyles = new[] { OptionStyle.European, OptionStyle.American };

        private readonly IQLUnderlyingVolatilityEstimator _underlyingVolEstimator;
        private readonly IQLRiskFreeRateEstimator _riskFreeRateEstimator;
        private readonly IQLDividendYieldEstimator _dividendYieldEstimator;
        private readonly PricingEngineFuncEx _pricingEngineFunc;

        /// <summary>
        /// When enabled, approximates Greeks if corresponding pricing model didn't calculate exact numbers.
        /// The default value is true.
        /// </summary>
        public bool EnableGreekApproximation { get; set; } = true;

        /// <summary>
        /// True if volatility model is warmed up, i.e. has generated volatility value different from zero, otherwise false.
        /// </summary>
        public bool VolatilityEstimatorWarmedUp => _underlyingVolEstimator.IsReady;

        /// <summary>
        /// List of option styles supported by the pricing model.
        /// By default, both American and European option styles are supported.
        /// </summary>
        public OptionStyle[] AllowedOptionStyles { get; }

        /// <summary>
        /// Method constructs QuantLib option price model with necessary estimators of underlying volatility, risk free rate, and underlying dividend yield
        /// </summary>
        /// <param name="pricingEngineFunc">Function modeled stochastic process, and returns new pricing engine to run calculations for that option</param>
        /// <param name="underlyingVolEstimator">The underlying volatility estimator</param>
        /// <param name="riskFreeRateEstimator">The risk free rate estimator</param>
        /// <param name="dividendYieldEstimator">The underlying dividend yield estimator</param>
        /// <param name="allowedOptionStyles">List of option styles supported by the pricing model. It defaults to both American and European option styles</param>
        public QLOptionPriceModel(PricingEngineFunc pricingEngineFunc, IQLUnderlyingVolatilityEstimator underlyingVolEstimator, IQLRiskFreeRateEstimator riskFreeRateEstimator, IQLDividendYieldEstimator dividendYieldEstimator, OptionStyle[] allowedOptionStyles = null)
            : this((option, process) => pricingEngineFunc(process), underlyingVolEstimator, riskFreeRateEstimator, dividendYieldEstimator, allowedOptionStyles)
        {}
        /// <summary>
        /// Method constructs QuantLib option price model with necessary estimators of underlying volatility, risk free rate, and underlying dividend yield
        /// </summary>
        /// <param name="pricingEngineFunc">Function takes option and modeled stochastic process, and returns new pricing engine to run calculations for that option</param>
        /// <param name="underlyingVolEstimator">The underlying volatility estimator</param>
        /// <param name="riskFreeRateEstimator">The risk free rate estimator</param>
        /// <param name="dividendYieldEstimator">The underlying dividend yield estimator</param>
        /// <param name="allowedOptionStyles">List of option styles supported by the pricing model. It defaults to both American and European option styles</param>
        public QLOptionPriceModel(PricingEngineFuncEx pricingEngineFunc, IQLUnderlyingVolatilityEstimator underlyingVolEstimator, IQLRiskFreeRateEstimator riskFreeRateEstimator, IQLDividendYieldEstimator dividendYieldEstimator, OptionStyle[] allowedOptionStyles = null)
        {
            _pricingEngineFunc = pricingEngineFunc;
            _underlyingVolEstimator = underlyingVolEstimator ?? new ConstantQLUnderlyingVolatilityEstimator();
            _riskFreeRateEstimator = riskFreeRateEstimator ?? new ConstantQLRiskFreeRateEstimator();
            _dividendYieldEstimator = dividendYieldEstimator ?? new ConstantQLDividendYieldEstimator();

            AllowedOptionStyles = allowedOptionStyles ?? _defaultAllowedOptionStyles;
        }

        /// <summary>
        /// Evaluates the specified option contract to compute a theoretical price, IV and greeks
        /// </summary>
        /// <param name="security">The option security object</param>
        /// <param name="slice">The current data slice. This can be used to access other information
        /// available to the algorithm</param>
        /// <param name="contract">The option contract to evaluate</param>
        /// <returns>An instance of <see cref="OptionPriceModelResult"/> containing the theoretical
        /// price of the specified option contract</returns>
        public OptionPriceModelResult Evaluate(Security security, Slice slice, OptionContract contract)
        {
            if (!AllowedOptionStyles.Contains(contract.Symbol.ID.OptionStyle))
            {
               throw new ArgumentException($"{contract.Symbol.ID.OptionStyle} style options are not supported by option price model '{this.GetType().Name}'");
            }

            try
            {
                // expired options have no price
                if (contract.Time.Date > contract.Expiry.Date)
                {
                    return OptionPriceModelResult.None;
                }

                // setting up option pricing parameters
                var calendar = new UnitedStates();
                var dayCounter = new Actual365Fixed();
                var optionSecurity = (Option)security;

                var securityExchangeHours = security.Exchange.Hours;
                var settlementDate = AddDays(contract.Time.Date, Option.DefaultSettlementDays, securityExchangeHours);
                var evaluationDate = contract.Time.Date;
                // TODO: static variable
                Settings.setEvaluationDate(evaluationDate);
                var maturityDate = AddDays(contract.Expiry.Date, Option.DefaultSettlementDays, securityExchangeHours);
                var spot = (double)optionSecurity.Underlying.Price;
                var underlyingQuoteValue = new SimpleQuote(spot);

                var dividendYieldValue = new SimpleQuote(_dividendYieldEstimator.Estimate(security, slice, contract));
                var dividendYield = new Handle<YieldTermStructure>(new FlatForward(0, calendar, dividendYieldValue, dayCounter));

                var riskFreeRateValue = new SimpleQuote(_riskFreeRateEstimator.Estimate(security, slice, contract));
                var riskFreeRate = new Handle<YieldTermStructure>(new FlatForward(0, calendar, riskFreeRateValue, dayCounter));

                var underlyingVolValue = new SimpleQuote(_underlyingVolEstimator.Estimate(security, slice, contract));
                var underlyingVol = new Handle<BlackVolTermStructure>(new BlackConstantVol(0, calendar, new Handle<Quote>(underlyingVolValue), dayCounter));

                if (!_underlyingVolEstimator.IsReady)
                {
                    return OptionPriceModelResult.None;
                }

                // preparing stochastic process and payoff functions
                var stochasticProcess = new BlackScholesMertonProcess(new Handle<Quote>(underlyingQuoteValue), dividendYield, riskFreeRate, underlyingVol);
                var payoff = new PlainVanillaPayoff(contract.Right == OptionRight.Call ? QLNet.Option.Type.Call : QLNet.Option.Type.Put, (double)contract.Strike);

                // creating option QL object
                var option = contract.Symbol.ID.OptionStyle == OptionStyle.American ?
                            new VanillaOption(payoff, new AmericanExercise(settlementDate, maturityDate)) :
                            new VanillaOption(payoff, new EuropeanExercise(maturityDate));

                // preparing pricing engine QL object
                option.setPricingEngine(_pricingEngineFunc(contract.Symbol, stochasticProcess));

                // running calculations
                // can return negative value in neighbourhood of 0
                var npv = Math.Max(0, EvaluateOption(option));

                // Calculate Implied Volatility
                var impliedVol = option.impliedVolatility((double)optionSecurity.Price, stochasticProcess);

                // Update the Black Vol Term Structure with the Implied Volatility for correct Greeks calculation
                underlyingVolValue.setValue(impliedVol);

                var blackVolatilityMaturity = underlyingVol.link.dayCounter()
                    .yearFraction(underlyingVol.link.referenceDate(), maturityDate);
                var riskFreeRateMaturity = riskFreeRate.link.dayCounter()
                    .yearFraction(riskFreeRate.link.referenceDate(), maturityDate);

                BlackCalculator blackLazy = null;

                // function extracts QL greeks catching exception if greek is not generated by the pricing engine and reevaluates option to get numerical estimate of the seisitivity
                decimal tryGetGreekOrReevaluate(Func<double> greek, Func<BlackCalculator, double> black)
                {
                    try
                    {
                        return (decimal)greek();
                    }
                    catch (Exception)
                    {
                        try
                        {
                            if (blackLazy == null)
                            {
                                // Define Black Calculator to calculate Greeks that are not defined by the option object
                                var dividendDiscount = dividendYield.link.discount(maturityDate);
                                var riskFreeDiscount = riskFreeRate.link.discount(maturityDate);
                                var forwardPrice = spot * dividendDiscount / riskFreeDiscount;
                                var variance = underlyingVol.link.blackVariance(maturityDate, (double)contract.Strike);
                                blackLazy = new BlackCalculator(payoff, forwardPrice, Math.Sqrt(variance), riskFreeDiscount);
                            }

                            if (EnableGreekApproximation)
                            {
                                return black(blackLazy).SafeDecimalCast();
                            }

                            throw new Exception("Greeks approximation was not allowed.");
                        }
                        catch(Exception exception)
                        {
                            Log.Error($"No valid Greek value returned from Black Calculator: {exception}");
                            // return zero if no default Greek value in the Option object and no valid Greek value was calculated in the Black Calculator
                            return 0.0m;
                        }
                    }
                }

                // producing output with lazy calculations of greeks

                return new OptionPriceModelResult((decimal)npv,
                            () => impliedVol.SafeDecimalCast(),
                            () => new Greeks(() => tryGetGreekOrReevaluate(() => option.delta(), (black) => black.delta(spot)),
                                            () => tryGetGreekOrReevaluate(() => option.gamma(), (black) => black.gamma(spot)),
                                            () => tryGetGreekOrReevaluate(() => option.vega(), (black) => black.vega(blackVolatilityMaturity)) / 100,   // per cent
                                            () => tryGetGreekOrReevaluate(() => option.thetaPerDay(), (black) => black.thetaPerDay(spot, blackVolatilityMaturity)),
                                            () => tryGetGreekOrReevaluate(() => option.rho(), (black) => black.rho(riskFreeRateMaturity)) / 100,        // per cent
                                            () => tryGetGreekOrReevaluate(() => option.elasticity(), (black) => black.elasticity(spot))));
            }
            catch (Exception err)
            {
                Log.Debug($"QLOptionPriceModel.Evaluate() error: {err.Message}");
                return OptionPriceModelResult.None;
            }
        }

        /// <summary>
        /// Runs option evaluation and logs exceptions
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        private static double EvaluateOption(VanillaOption option)
        {
            try
            {
                var npv = option.NPV();

                if (double.IsNaN(npv) ||
                    double.IsInfinity(npv))
                    npv = 0.0;

                return npv;
            }
            catch (Exception err)
            {
                Log.Debug($"QLOptionPriceModel.EvaluateOption() error: {err.Message}");
                return 0.0;
            }
        }

        private static DateTime AddDays(DateTime date, int days, SecurityExchangeHours marketHours)
        {
            var forwardDate = date.AddDays(days);

            if (!marketHours.IsDateOpen(forwardDate))
            {
                forwardDate = marketHours.GetNextTradingDay(forwardDate);
            }

            return forwardDate;
        }
    }
}