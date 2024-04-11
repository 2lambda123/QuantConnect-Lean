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
using Python.Runtime;
using QuantConnect.Benchmarks;
using QuantConnect.Brokerages;
using QuantConnect.Data.Market;
using QuantConnect.Data.Shortable;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Orders.Slippage;
using QuantConnect.Securities;

namespace QuantConnect.Python
{
    /// <summary>
    /// Provides an implementation of <see cref="IBrokerageModel"/> that wraps a <see cref="PyObject"/> object
    /// </summary>
    public class BrokerageModelPythonWrapper : BasePythonWrapper<IBrokerageModel>, IBrokerageModel
    {
        /// <summary>
        /// Constructor for initialising the <see cref="BrokerageModelPythonWrapper"/> class with wrapped <see cref="PyObject"/> object
        /// </summary>
        /// <param name="model">Models brokerage transactions, fees, and order</param>
        public BrokerageModelPythonWrapper(PyObject model)
            : base(model)
        {
        }

        /// <summary>
        /// Gets or sets the account type used by this model
        /// </summary>
        public AccountType AccountType
        {
            get
            {
                return GetProperty<AccountType>(nameof(AccountType));
            }
        }

        /// <summary>
        /// Gets the brokerages model percentage factor used to determine the required unused buying power for the account.
        /// From 1 to 0. Example: 0 means no unused buying power is required. 0.5 means 50% of the buying power should be left unused.
        /// </summary>
        public decimal RequiredFreeBuyingPowerPercent
        {
            get
            {
                return GetProperty<decimal>(nameof(RequiredFreeBuyingPowerPercent));
            }
        }

        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public IReadOnlyDictionary<SecurityType, string> DefaultMarkets
        {
            get
            {
                using (Py.GIL())
                {
                    var markets = GetProperty<PyObject>(nameof(DefaultMarkets)) as dynamic;
                    if ((markets as PyObject).TryConvert(out IReadOnlyDictionary<SecurityType, string> csharpDic))
                    {
                        return csharpDic;
                    }

                    var dic = new Dictionary<SecurityType, string>();
                    foreach (var item in markets)
                    {
                        using var pyItem = item as PyObject;
                        var market = pyItem.As<SecurityType>();
                        dic[market] = markets[item];
                    }

                    (markets as PyObject).Dispose();
                    return dic;
                }
            }
        }

        /// <summary>
        /// Applies the split to the specified order ticket
        /// </summary>
        /// <param name="tickets">The open tickets matching the split event</param>
        /// <param name="split">The split event data</param>
        public void ApplySplit(List<OrderTicket> tickets, Split split)
        {
            InvokeMethod(nameof(ApplySplit), tickets, split);
        }

        /// <summary>
        /// Returns true if the brokerage would be able to execute this order at this time assuming
        /// market prices are sufficient for the fill to take place. This is used to emulate the
        /// brokerage fills in backtesting and paper trading. For example some brokerages may not perform
        /// executions during extended market hours. This is not intended to be checking whether or not
        /// the exchange is open, that is handled in the Security.Exchange property.
        /// </summary>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to test for execution</param>
        /// <returns>True if the brokerage would be able to perform the execution, false otherwise</returns>
        public bool CanExecuteOrder(Security security, Order order)
        {
            return InvokeMethod<bool>(nameof(CanExecuteOrder), security, order);
        }

        /// <summary>
        /// Returns true if the brokerage could accept this order. This takes into account
        /// order type, security type, and order size limits.
        /// </summary>
        /// <remarks>
        /// For example, a brokerage may have no connectivity at certain times, or an order rate/size limit
        /// </remarks>
        /// <param name="security">The security being ordered</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            using (Py.GIL())
            {
                message = null;
                using var result = InvokeMethod(nameof(CanSubmitOrder), security, order, message);
                // Since pythonnet does not support out parameters, the methods return
                // a tuple where the out parameter comes after the other returned values
                if (!PyTuple.IsTupleType(result))
                {
                    throw new ArgumentException($@"{(Instance as dynamic).__class__.__name__}.CanSubmitOrder(): Must return a tuple value where the first value is a bool and the second a BrokerageMessageEvent");
                }

                message = result[1].As<BrokerageMessageEvent>();
                return result[0].As<bool>();
            }
        }

        /// <summary>
        /// Returns true if the brokerage would allow updating the order as specified by the request
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested updated to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>True if the brokerage would allow updating the order, false otherwise</returns>
        public bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            using (Py.GIL())
            {
                message = null;
                using var result = InvokeMethod(nameof(CanUpdateOrder), security, order, request, message);
                // Since pythonnet does not support out parameters, the methods return
                // a tuple where the out parameter comes after the other returned values
                if (!PyTuple.IsTupleType(result))
                {
                    throw new ArgumentException($@"{(Instance as dynamic).__class__.__name__}.CanUpdateOrder(): Must return a tuple value where the first value is a bool and the second a BrokerageMessageEvent");
                }

                message = result[1].As<BrokerageMessageEvent>();
                return result[0].As<bool>();
            }
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public IBenchmark GetBenchmark(SecurityManager securities)
        {
            using (Py.GIL())
            {
                var benchmark = InvokeMethod(nameof(GetBenchmark), securities);
                if (benchmark.TryConvert<IBenchmark>(out var csharpBenchmark))
                {
                    return csharpBenchmark;
                }
                return new BenchmarkPythonWrapper(benchmark);
            }
        }

        /// <summary>
        /// Gets a new fee model that represents this brokerage's fee structure
        /// </summary>
        /// <param name="security">The security to get a fee model for</param>
        /// <returns>The new fee model for this brokerage</returns>
        public IFeeModel GetFeeModel(Security security)
        {
            using (Py.GIL())
            {
                var feeModel = InvokeMethod(nameof(GetFeeModel), security);
                if (feeModel.TryConvert<IFeeModel>(out var csharpFeeModel))
                {
                    return csharpFeeModel;
                }
                return new FeeModelPythonWrapper(feeModel);
            }
        }

        /// <summary>
        /// Gets a new fill model that represents this brokerage's fill behavior
        /// </summary>
        /// <param name="security">The security to get fill model for</param>
        /// <returns>The new fill model for this brokerage</returns>
        public IFillModel GetFillModel(Security security)
        {
            using (Py.GIL())
            {
                var fillModel = InvokeMethod(nameof(GetFillModel), security);
                if (fillModel.TryConvert<IFillModel>(out var csharpFillModel))
                {
                    return csharpFillModel;
                }
                return new FillModelPythonWrapper(fillModel);
            }
        }

        /// <summary>
        /// Gets the brokerage's leverage for the specified security
        /// </summary>
        /// <param name="security">The security's whose leverage we seek</param>
        /// <returns>The leverage for the specified security</returns>
        public decimal GetLeverage(Security security)
        {
            return InvokeMethod<decimal>(nameof(GetLeverage), security);
        }

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <returns>The settlement model for this brokerage</returns>
        public ISettlementModel GetSettlementModel(Security security)
        {
            using (Py.GIL())
            {
                var settlementModel = InvokeMethod(nameof(GetSettlementModel), security);
                if (settlementModel.TryConvert<ISettlementModel>(out var csharpSettlementModel))
                {
                    return csharpSettlementModel;
                }
                return new SettlementModelPythonWrapper(settlementModel);
            }
        }

        /// <summary>
        /// Gets a new settlement model for the security
        /// </summary>
        /// <param name="security">The security to get a settlement model for</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The settlement model for this brokerage</returns>
        [Obsolete("Flagged deprecated and will remove December 1st 2018")]
        public ISettlementModel GetSettlementModel(Security security, AccountType accountType)
        {
            return InvokeMethod<ISettlementModel>(nameof(GetSettlementModel), security, accountType);
        }

        /// <summary>
        /// Gets a new slippage model that represents this brokerage's fill slippage behavior
        /// </summary>
        /// <param name="security">The security to get a slippage model for</param>
        /// <returns>The new slippage model for this brokerage</returns>
        public ISlippageModel GetSlippageModel(Security security)
        {
            using (Py.GIL())
            {
                var slippageModel = InvokeMethod(nameof(GetSlippageModel), security);
                if (slippageModel.TryConvert<ISlippageModel>(out var csharpSlippageModel))
                {
                    return csharpSlippageModel;
                }
                return new SlippageModelPythonWrapper(slippageModel);
            }
        }

        /// <summary>
        /// Determine if this symbol is shortable
        /// </summary>
        /// <param name="algorithm">The algorithm running</param>
        /// <param name="symbol">The symbol to short</param>
        /// <param name="quantity">The amount to short</param>
        /// <returns></returns>
        public bool Shortable(IAlgorithm algorithm, Symbol symbol, decimal quantity)
        {
            return InvokeMethod<bool>(nameof(Shortable), algorithm, symbol, quantity);
        }

        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            using (Py.GIL())
            {
                var buyingPowerModel = InvokeMethod(nameof(GetBuyingPowerModel), security);
                if (buyingPowerModel.TryConvert<IBuyingPowerModel>(out var csharpBuyingPowerModel))
                {
                    return csharpBuyingPowerModel;
                }
                return new BuyingPowerModelPythonWrapper(buyingPowerModel);
            }
        }

        /// <summary>
        /// Gets a new buying power model for the security
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <param name="accountType">The account type</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        [Obsolete("Flagged deprecated and will remove December 1st 2018")]
        public IBuyingPowerModel GetBuyingPowerModel(Security security, AccountType accountType)
        {
            return InvokeMethod<IBuyingPowerModel>(nameof(GetBuyingPowerModel), security, accountType);
        }

        /// <summary>
        /// Gets the shortable provider
        /// </summary>
        /// <returns>Shortable provider</returns>
        public IShortableProvider GetShortableProvider(Security security)
        {
            using (Py.GIL())
            {
                var shortableProvider = InvokeMethod(nameof(GetShortableProvider), security);
                if (shortableProvider.TryConvert<IShortableProvider>(out var csharpShortableProvider))
                {
                    return csharpShortableProvider;
                }
                return new ShortableProviderPythonWrapper(shortableProvider);
            }
        }

        /// <summary>
        /// Convenience method to get the underlying <see cref="IBrokerageModel"/> object from the wrapper.
        /// </summary>
        /// <returns>Underlying <see cref="IBrokerageModel"/> object</returns>
        public IBrokerageModel GetModel()
        {
            using (Py.GIL())
            {
                return Instance.AsManagedObject(typeof(IBrokerageModel)) as IBrokerageModel;
            }
        }

        /// <summary>
        /// Gets a new margin interest rate model for the security
        /// </summary>
        /// <param name="security">The security to get a margin interest rate model for</param>
        /// <returns>The margin interest rate model for this brokerage</returns>
        public IMarginInterestRateModel GetMarginInterestRateModel(Security security)
        {
            using (Py.GIL())
            {
                var marginInterestRateModel = InvokeMethod(nameof(GetMarginInterestRateModel), security);
                if (marginInterestRateModel.TryConvert<IMarginInterestRateModel>(out var csharpBuyingPowerModel))
                {
                    return csharpBuyingPowerModel;
                }
                return new MarginInterestRateModelPythonWrapper(marginInterestRateModel);
            }
        }
    }
}
