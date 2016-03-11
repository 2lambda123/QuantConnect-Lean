﻿# QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
# Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
# 
# Licensed under the Apache License, Version 2.0 (the "License"); 
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
# 
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import clr
clr.AddReference("System")
clr.AddReference("QuantConnect.Algorithm")
clr.AddReference("QuantConnect.Indicators")
clr.AddReference("QuantConnect.Common")

from System import *
from QuantConnect import *
from QuantConnect.Algorithm import *
from QuantConnect.Indicators import *


class ParameterizedAlgorithm(QCAlgorithm):
    def __init__(self):
        # we place attributes on top of our fields or properties that should 
        # receive their values from the job. The values 100 and 200 are just
        # default values that or only used if the parameters do not exist
        self.Fast = None
        self.Slow = None
        self.FastPeriod = 100
        self.SlowPeriod = 200
        self.GetParameters()


    def GetParameters(self):
        '''Get parameters from config.json'''
        import json
        with open('config.json') as file:
            for line in file:
                line = line.rstrip().replace(',', '')
                if "ema-fast" in line : self.FastPeriod = json.loads('{' + line + '}')["ema-fast"]
                if "ema-slow" in line : self.SlowPeriod = json.loads('{' + line + '}')["ema-slow"]


    def Initialize(self):
        '''Initialise the data and resolution required, as well as the cash and start-end dates for your algorithm. All algorithms must initialized.'''
        
        self.SetStartDate(2013, 10, 07)  #Set Start Date
        self.SetEndDate(2013, 10, 11)    #Set End Date
        self.SetCash(100000)           #Set Strategy Cash
        # Find more symbols here: http://quantconnect.com/data
        self.AddSecurity(SecurityType.Equity, "SPY")

        self.Fast = self.EMA("SPY", self.FastPeriod);
        self.Slow = self.EMA("SPY", self.SlowPeriod);

        
    def OnData(self, data):
        '''OnData event is the primary entry point for your algorithm. Each new data point will be pumped in here.
        
        Arguments:
            data: TradeBars IDictionary object with your stock data
        '''
        
        # wait for our indicators to ready
        if not self.Fast.IsReady or not self.Slow.IsReady:
            return

        if self.Fast.Current.Value > self.Slow.Current.Value * 1.001:
            self.SetHoldings("SPY", 1)
        elif self.Fast.Current.Value < self.Slow.Current.Value * 0.999:
            self.Liquidate("SPY")