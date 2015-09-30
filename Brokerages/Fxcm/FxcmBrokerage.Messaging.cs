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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using com.fxcm.external.api.transport;
using com.fxcm.fix;
using com.fxcm.fix.other;
using com.fxcm.fix.posttrade;
using com.fxcm.fix.pretrade;
using com.fxcm.fix.trade;
using com.fxcm.messaging;
using QuantConnect.Logging;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.Fxcm
{
    public partial class FxcmBrokerage
    {
        private IGateway _gateway;

        private readonly object _locker = new object();
        private string _currentRequest;
        private const int MaximumWaitingTime = 2500;

        private readonly Dictionary<string, string> _mapInstrumentSymbols = new Dictionary<string, string>();
        private readonly Dictionary<string, TradingSecurity> _fxcmInstruments = new Dictionary<string, TradingSecurity>();
        private readonly Dictionary<string, CollateralReport> _accounts = new Dictionary<string, CollateralReport>();
        private readonly Dictionary<string, MarketDataSnapshot> _rates = new Dictionary<string, MarketDataSnapshot>();
        private readonly Dictionary<string, ExecutionReport> _orders = new Dictionary<string, ExecutionReport>();
        private readonly Dictionary<string, PositionReport> _openPositions = new Dictionary<string, PositionReport>();

        private readonly Dictionary<string, Order> _mapRequestsToOrders = new Dictionary<string, Order>();
        private readonly Dictionary<string, Order> _mapFxcmOrderIdsToOrders = new Dictionary<string, Order>();
        private readonly Dictionary<string, AutoResetEvent> _mapRequestsToAutoResetEvents = new Dictionary<string, AutoResetEvent>();

        private string _fxcmAccountCurrency = "USD";

        private void LoadInstruments()
        {
            // Note: requestTradingSessionStatus() MUST be called just after login

            lock (_locker)
            {
                _currentRequest = _gateway.requestTradingSessionStatus();
                _mapRequestsToAutoResetEvents[_currentRequest] = new AutoResetEvent(false);
            }
            _mapRequestsToAutoResetEvents[_currentRequest].WaitOne(MaximumWaitingTime);
        }

        private void LoadAccounts()
        {
            lock (_locker)
            {
                _accounts.Clear();
                _currentRequest = _gateway.requestAccounts();
                _mapRequestsToAutoResetEvents[_currentRequest] = new AutoResetEvent(false);
            }
            _mapRequestsToAutoResetEvents[_currentRequest].WaitOne(MaximumWaitingTime);

            // Hedging MUST be disabled on all accounts
            if (_accounts.Values.Any(account => account.getParties().getFXCMPositionMaintenance() != "N"))
            {
                throw new NotSupportedException("The Lean engine does not support accounts with Hedging enabled. Please contact FXCM support to disable Hedging.");
            }
        }

        private void LoadOpenOrders()
        {
            lock (_locker)
            {
                _orders.Clear();
                _currentRequest = _gateway.requestOpenOrders();
                _mapRequestsToAutoResetEvents[_currentRequest] = new AutoResetEvent(false);
            }
            _mapRequestsToAutoResetEvents[_currentRequest].WaitOne(MaximumWaitingTime);
        }

        private void LoadOpenPositions()
        {
            lock (_locker)
            {
                _openPositions.Clear();
                _currentRequest = _gateway.requestOpenPositions();
                _mapRequestsToAutoResetEvents[_currentRequest] = new AutoResetEvent(false);
            }
            _mapRequestsToAutoResetEvents[_currentRequest].WaitOne(MaximumWaitingTime);
        }

        /// <summary>
        /// Gets the quotes for the symbol
        /// </summary>
        public List<MarketDataSnapshot> GetQuotes(List<string> symbols)
        {
            // get current quotes for the instrument
            var request = new MarketDataRequest();
            request.setMDEntryTypeSet(MarketDataRequest.MDENTRYTYPESET_ALL);
            request.setSubscriptionRequestType(SubscriptionRequestTypeFactory.SNAPSHOT);
            foreach (var symbol in symbols)
            {
                request.addRelatedSymbol(_fxcmInstruments[symbol]);
            }

            lock (_locker)
            {
                _currentRequest = _gateway.sendMessage(request);
                _mapRequestsToAutoResetEvents[_currentRequest] = new AutoResetEvent(false);
            }
            _mapRequestsToAutoResetEvents[_currentRequest].WaitOne(MaximumWaitingTime);

            return _rates.Where(x => symbols.Contains(x.Key)).Select(x => x.Value).ToList();
        }

        /// <summary>
        /// Gets the current conversion rate into USD
        /// </summary>
        /// <remarks>Synchronous, blocking</remarks>
        private decimal GetUsdConversion(string currency)
        {
            if (currency == "USD")
                return 1m;

            // determine the correct symbol to choose
            var normalSymbol = currency + "/USD";
            var invertedSymbol = "USD/" + currency;
            var isInverted = _fxcmInstruments.ContainsKey(invertedSymbol);
            var symbol = isInverted ? invertedSymbol : normalSymbol;

            // get current quotes for the instrument
            var quotes = GetQuotes(new List<string> { symbol });

            var rate = (decimal)(quotes[0].getBidClose() + quotes[0].getAskClose()) / 2;

            return isInverted ? 1 / rate : rate;
        }

        #region IGenericMessageListener implementation

        /// <summary>
        /// Receives generic messages from the FXCM API
        /// </summary>
        /// <param name="message">Generic message received</param>
        public void messageArrived(ITransportable message)
        {
            // Dispatch message to specific handler

            lock (_locker)
            {
                if (message is TradingSessionStatus)
                    OnTradingSessionStatus((TradingSessionStatus)message);

                else if (message is CollateralReport)
                    OnCollateralReport((CollateralReport)message);

                else if (message is MarketDataSnapshot)
                    OnMarketDataSnapshot((MarketDataSnapshot)message);

                else if (message is ExecutionReport)
                    OnExecutionReport((ExecutionReport)message);

                else if (message is RequestForPositionsAck)
                    OnRequestForPositionsAck((RequestForPositionsAck)message);

                else if (message is PositionReport)
                    OnPositionReport((PositionReport)message);

                else if (message is OrderCancelReject)
                    OnOrderCancelReject((OrderCancelReject)message);

                else if (message is UserResponse || message is CollateralInquiryAck || message is MarketDataRequestReject || message is SecurityStatus)
                {
                    // Unused messages, no handler needed
                }

                else
                {
                    // Should never get here, if it does log and ignore message
                    // New messages added in future api updates should be added to the unused list above
                    Log.Trace(string.Format("FxcmBrokerage.messageArrived(): Unknown message: {0}\n", message));
                }
            }
        }

        /// <summary>
        /// TradingSessionStatus message handler
        /// </summary>
        private void OnTradingSessionStatus(TradingSessionStatus message)
        {
            if (message.getRequestID() == _currentRequest)
            {
                // load instrument list into a dictionary
                var securities = message.getSecurities();
                while (securities.hasMoreElements())
                {
                    var security = (TradingSecurity)securities.nextElement();
                    _fxcmInstruments[security.getSymbol()] = security;
                }

                // create map from QuantConnect symbols to FXCM symbols
                foreach (var fxcmSymbol in _fxcmInstruments.Keys)
                {
                    var symbol = ConvertFxcmSymbolToSymbol(fxcmSymbol);
                    _mapInstrumentSymbols[symbol] = fxcmSymbol;
                }

                // get account base currency
                _fxcmAccountCurrency = message.getParameter("BASE_CRNCY").getValue();

                _mapRequestsToAutoResetEvents[_currentRequest].Set();
                _mapRequestsToAutoResetEvents.Remove(_currentRequest);
            }
        }

        /// <summary>
        /// CollateralReport message handler
        /// </summary>
        private void OnCollateralReport(CollateralReport message)
        {
            // add the trading account to the account list
            _accounts[message.getAccount()] = message;

            if (message.getRequestID() == _currentRequest)
            {
                // set the state of the request to be completed only if this is the last collateral report requested
                if (message.isLastRptRequested())
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// MarketDataSnapshot message handler
        /// </summary>
        private void OnMarketDataSnapshot(MarketDataSnapshot message)
        {
            // update the current prices for the instrument
            _rates[message.getInstrument().getSymbol()] = message;

            if (message.getRequestID() == _currentRequest)
            {
                if (message.getFXCMContinuousFlag() == IFixValueDefs.__Fields.FXCMCONTINUOUS_END)
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// ExecutionReport message handler
        /// </summary>
        private void OnExecutionReport(ExecutionReport message)
        {
            var orderId = message.getOrderID();
            var orderStatus = message.getFXCMOrdStatus();
            //Debug.Print(message.toString());

            if (orderId != "NONE")
            {
                _orders[orderId] = message;

                Order order;
                if (_mapFxcmOrderIdsToOrders.TryGetValue(orderId, out order))
                {
                    // existing order
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0) { Status = ConvertOrderStatus(orderStatus) });
                }
                else if (_mapRequestsToOrders.TryGetValue(message.getRequestID(), out order))
                {
                    // new order
                    OnOrderEvent(new OrderEvent(order, DateTime.UtcNow, 0) { Status = ConvertOrderStatus(orderStatus) });
                    _mapFxcmOrderIdsToOrders[orderId] = order;
                }
            }

            if (message.getRequestID() == _currentRequest)
            {
                if (message.isLastRptRequested())
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// RequestForPositionsAck message handler
        /// </summary>
        private void OnRequestForPositionsAck(RequestForPositionsAck message)
        {
            if (message.getRequestID() == _currentRequest)
            {
                if (message.getTotalNumPosReports() == 0)
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// PositionReport message handler
        /// </summary>
        private void OnPositionReport(PositionReport message)
        {
            _openPositions[message.getCurrency()] = message;

            if (message.getRequestID() == _currentRequest)
            {
                if (message.isLastRptRequested())
                {
                    _mapRequestsToAutoResetEvents[_currentRequest].Set();
                    _mapRequestsToAutoResetEvents.Remove(_currentRequest);
                }
            }
        }

        /// <summary>
        /// OrderCancelReject message handler
        /// </summary>
        private void OnOrderCancelReject(OrderCancelReject message)
        {
            if (message.getRequestID() == _currentRequest)
            {
                _mapRequestsToAutoResetEvents[_currentRequest].Set();
                _mapRequestsToAutoResetEvents.Remove(_currentRequest);
            }
        }

        #endregion

        #region IStatusMessageListener implementation

        /// <summary>
        /// Receives status messages from the FXCM API
        /// </summary>
        /// <param name="message">Status message received</param>
        public void messageArrived(ISessionStatus message)
        {
            switch (message.getStatusCode())
            {
                case ISessionStatus.__Fields.STATUSCODE_DISCONNECTED:
                    _isConnected = false;
                    break;
            }
        }

        #endregion

    }
}
