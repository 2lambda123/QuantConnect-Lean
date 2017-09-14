from datetime import timedelta

class BasicTemplateFuturesAlgorithm(QCAlgorithm):
    '''This example demonstrates how to add futures for a given underlying.
It also shows how you can prefilter contracts easily based on expirations.
It also shows how you can inspect the futures chain to pick a specific contract to trade.'''

    def Initialize(self):
        self.SetStartDate(2013, 10, 8)
        self.SetEndDate(2013, 10, 9)
        self.SetCash(1000000)

        # Subscribe and set our expiry filter for the futures chain
        # find the front contract expiring no earlier than in 90 days
        futureES = self.AddFuture(Futures.Indices.SP500EMini, Resolution.Minute)
        futureES.SetFilter(timedelta(0), timedelta(182))

        futureGC = self.AddFuture(Futures.Metals.Gold, Resolution.Minute)
        futureGC.SetFilter(timedelta(0), timedelta(182))


    def OnData(self,slice):
        if not self.Portfolio.Invested:
            for chain in slice.FutureChains:
                 # Get contracts expiring no earlier than in 90 days
                contracts = filter(lambda x: x.Expiry > self.Time + timedelta(90), chain.Value)

                # if there is any contract, trade the front contract
                if len(contracts) == 0: continue
                front = sorted(contracts, key = lambda x: x.Expiry, reverse=True)[0]
                self.MarketOrder(front.Symbol , 1)
        else:
            self.Liquidate()

    def OnOrderEvent(self, orderEvent):
    	# Order fill event handler. On an order fill update the resulting information is passed to this method.
        # Order event details containing details of the events
        self.Log(str(orderEvent))

    def OnSecuritiesChanged(self, changes):
    	self._changes = changes
        if self._changes == SecurityChanges.None: return
        for change in self._changes.AddedSecurities:
			history = self.History(change.Symbol, 1, Resolution.Minute)
			history = history.sortlevel(['time'], ascending=False)[:1]

			self.Log("History: " + str(history.index.get_level_values('symbol').values[0])
						+ ": " + str(history.index.get_level_values('time').values[0])
						+ " > " + str(history['close'].values))
