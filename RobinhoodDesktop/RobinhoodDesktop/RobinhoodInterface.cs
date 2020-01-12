using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading;
using BasicallyMe.RobinhoodNet;

namespace RobinhoodDesktop
{
    public class RobinhoodInterface : DataAccessor.DataAccessorInterface, Broker.BrokerInterface
    {
        public RobinhoodInterface()
        {
            this.Client = new RobinhoodClient();
            this.Instruments = new InstrumentData(this);

            RobinhoodThread = new Thread(ThreadProcess);
            RobinhoodThread.Start();
        }

        #region Constants
        /// <summary>
        /// The amount of time to delay before processing a history request
        /// to allow multiple requests to be grouped together.
        /// </summary>
        private const double HISTORY_REQUEST_PROCESS_DELAY = 0.01;

        /// <summary>
        /// The supported time intervals for history data
        /// </summary>
        private static readonly Dictionary<TimeSpan, string> HISTORY_INTERVALS = new Dictionary<TimeSpan, string>()
        {
            { new TimeSpan(0, 5, 0), "5minute" },
            { new TimeSpan(1, 0, 0), "hour" },
            { new TimeSpan(24, 0, 0), "day" },
            { new TimeSpan(7, 0, 0, 0), "week" },
        };

        private static readonly Dictionary<TimeSpan, string> HISTORY_SPANS = new Dictionary<TimeSpan, string>()
        {
            { new TimeSpan(1, 0, 0, 0), "day" },
            { new TimeSpan(7, 0 ,0, 0), "week" },
            { new TimeSpan(365, 0, 0, 0), "year" },
            { new TimeSpan((365 * 5), 0, 0, 0), "5year" },
            { TimeSpan.MaxValue, "all" }
        };

        private class HISTORY_BOUNDS
        {
            public static readonly string EXTENDED = "extended";
            public static readonly string REGULAR = "regular";
            public static readonly string TRADING = "trading";
        };

        /// <summary>
        /// The time extended hours begins
        /// </summary>
        private static readonly DateTime EXTENDED_HOURS_OPEN = DateTime.Now.Date.AddHours(9);

        /// <summary>
        /// The time extended hours ends
        /// </summary>
        private static readonly DateTime EXTENDED_HOURS_CLOSE = DateTime.Now.Date.AddHours(24);
        #endregion

        #region Types
        private struct HistoryRequest
        {
            public string Symbol;
            public DateTime Start;
            public DateTime End;
            public TimeSpan Interval;
            public DataAccessor.PriceDataCallback Callback;
            public DateTime RequestTime;

            public HistoryRequest(string symbol, DateTime start, DateTime end, TimeSpan interval, DataAccessor.PriceDataCallback callback)
            {
                this.Symbol = symbol;
                this.Start = start;
                this.End = end;
                this.Interval = interval;
                this.Callback = callback;

                RequestTime = DateTime.Now;
            }
        }

        private class InstrumentData
        {
            public InstrumentData(RobinhoodInterface robinhood)
            {
                this.Robinhood = robinhood;
            }

            /// <summary>
            /// Reference to the robinhood interface
            /// </summary>
            private RobinhoodInterface Robinhood;

            /// <summary>
            /// Stores the instrument ID associated with a symbol
            /// </summary>
            private Dictionary<string, string> InstrumentCache = new Dictionary<string, string>();

            /// <summary>
            /// Looks up the instrument for a given symbol
            /// </summary>
            /// <param name="symbol">The stock symbol</param>
            /// <param name="source">The source data to use to create the association if one doesn't already exist</param>
            /// <returns>The first corresponding position instrument</returns>
            public string GetInstrument<T>(string symbol, T source)
            {
                if(!InstrumentCache.ContainsValue(symbol))
                {
                    associateInstruments(source);
                }

                string instrument = "No positions for symbol";
                var match = InstrumentCache.Where((a) => { return a.Value.Equals(symbol); });
                if(match.Count() > 0) instrument = match.First().Key;
                return instrument;
            }



            /// <summary>
            /// Looks up the symbol for a given instrument
            /// </summary>
            /// <param name="instrument">The position instrument ID</param>
            /// <param name="source">The source data to use to create the association if one doesn't already exist</param>
            /// <returns>The symbol</returns>
            public string GetSymbol<T>(string instrument, T source)
            {
                string symbol = "No instrument";
                instrument = instrument.TrimEnd(new char[] { '/' });
                string instrumentId = instrument.Substring(instrument.LastIndexOf("/") + 1);
                if(!InstrumentCache.TryGetValue(instrumentId, out symbol))
                {
                    associateInstruments(source);
                    InstrumentCache.TryGetValue(instrumentId, out symbol);
                }

                return symbol;
            }

            private void associateInstruments<T>(T source)
            {
                if(typeof(IList<Position>).Equals(typeof(T)))
                {
                    IList<Position> sourceList = (IList<Position>)source;
                    if(sourceList == null)
                    {
                        sourceList = Robinhood.Client.DownloadPositions(Robinhood.getAccount().PositionsUrl.ToString());
                    }
                    foreach(Position p in sourceList)
                    {
                        string url = p.InstrumentUrl.ToString().TrimEnd(new char[] { '/' });
                        InstrumentCache[url.Substring(url.LastIndexOf("/") + 1)] = Robinhood.Client.DownloadInstrument(url).Result.Symbol;
                    }
                }

                if(typeof(IList<OrderSnapshot>).Equals(typeof(T)))
                {
                    IList<OrderSnapshot> sourceList = (IList<OrderSnapshot>)source;
                    if(sourceList == null)
                    {
                        sourceList = Robinhood.Client.DownloadAllOrders().Result;
                    }
                    foreach(OrderSnapshot p in sourceList)
                    {
                        string instrument = p.InstrumentId;
                        InstrumentCache[instrument] = Robinhood.Client.DownloadInstrument("https://api.robinhood.com/instruments/" + instrument).Result.Symbol;
                    }
                }
            }
        }

        private class RobinhoodOrder : Broker.Order
        {
            public string OrderId;
            public DateTime UpdatedAt;
            public DateTime RefreshedAt;

            public RobinhoodOrder(string symbol, BasicallyMe.RobinhoodNet.OrderSnapshot s)
            {
                Dictionary<BasicallyMe.RobinhoodNet.OrderType, Broker.Order.OrderType> orderTypeLookup = new Dictionary<BasicallyMe.RobinhoodNet.OrderType, OrderType>()
                {
                    { BasicallyMe.RobinhoodNet.OrderType.Market, Broker.Order.OrderType.MARKET },
                    { BasicallyMe.RobinhoodNet.OrderType.Limit, Broker.Order.OrderType.LIMIT },
                    { BasicallyMe.RobinhoodNet.OrderType.StopLoss, Broker.Order.OrderType.STOP },
                };

                OrderId = s.OrderId;
                Type = orderTypeLookup[s.Type];
                BuySell = (s.Side == Side.Buy ? Broker.Order.BuySellType.BUY : Broker.Order.BuySellType.SELL);
                if((Type == OrderType.STOP) && (s.StopPrice.HasValue)) StopPrice = (decimal)s.StopPrice;
                Symbol = symbol;
                Update(s);
            }

            public void Update(BasicallyMe.RobinhoodNet.OrderSnapshot s)
            {
                if(s.AveragePrice.HasValue) AveragePrice = (decimal)s.AveragePrice;
                Quantity = s.Quantity;

                // Update the status
                UpdatedAt = s.UpdatedAt;
                RefreshedAt = DateTime.Now;
                switch(s.State)
                {
                    case ("filled"):
                        Status = OrderStatus.COMPLETE;
                        break;

                    case ("cancelled"):
                        Status = OrderStatus.CANCELLED;
                        break;

                    case ("failed"):
                        Status = OrderStatus.FAILED;
                        break;
                }
            }
        }
        #endregion

        #region Variables
        /// <summary>
        /// The client used to access the Robinhood API
        /// </summary>
        public RobinhoodClient Client;

        /// <summary>
        /// The name of the current signed-in user
        /// </summary>
        public string UserName;

        /// <summary>
        /// The thread used to process the requests
        /// </summary>
        public System.Threading.Thread RobinhoodThread;

        /// <summary>
        /// Variable used to shut down the task
        /// </summary>
        public bool Running = true;

        /// <summary>
        /// The user's active account
        /// </summary>
        private BasicallyMe.RobinhoodNet.Account RobinhoodAccount = null;

        /// <summary>
        /// The pending history requests
        /// </summary>
        private List<HistoryRequest> HistoryRequests = new List<HistoryRequest>();

        /// <summary>
        /// Mutex used to protect access to the pending history request list
        /// </summary>
        private Mutex RobinhoodThreadMutex = new Mutex();

        /// <summary>
        /// Mutex used to protect access to the active order list
        /// </summary>
        private Mutex ActiveOrderMutex = new Mutex();

        /// <summary>
        /// List of active position subscriptions
        /// </summary>
        private List<Broker.PositionSubscription> PositionSubscriptions = new List<Broker.PositionSubscription>();

        /// <summary>
        /// The stored position information
        /// </summary>
        private InstrumentData Instruments;

        /// <summary>
        /// The orders that are currently active
        /// </summary>
        private List<RobinhoodOrder> ActiveOrders = new List<RobinhoodOrder>();

        /// <summary>
        /// Stores the time at which the orders were last updated
        /// </summary>
        private DateTime OrderLastUpdated = DateTime.Now.AddSeconds(-55);
        #endregion

        /// <summary>
        /// Closes any ongoing connections and cleans up the object
        /// </summary>
        public void Close()
        {
            Running = false;
            RobinhoodThread.Join();
        }

        #region DataAccessor
        /// <summary>
        /// Requests price history data for a stock
        /// </summary>
        /// <param name="symbol">The stock symbol to request for</param>
        /// <param name="start">The start of the time range</param>
        /// <param name="end">The end of the time range</param>
        /// <param name="interval">The desired interval between price points in the data set</param>
        /// <param name="callback">A callback that is executed once the requested data is available</param>
        public void GetPriceHistory(string symbol, DateTime start, DateTime end, TimeSpan interval, DataAccessor.PriceDataCallback callback)
        {
            if(!string.IsNullOrEmpty(symbol))
            {
                RobinhoodThreadMutex.WaitOne();
                HistoryRequests.Add(new HistoryRequest(symbol, start, end, interval, callback));
                RobinhoodThreadMutex.ReleaseMutex();
            }
            else
            {
                throw new Exception("Received request for invalid symbol");
            }
        }

        /// <summary>
        /// Searches for stocks based on the symbol string
        /// </summary>
        /// <param name="symbol">The symbol (or portion of) to search for</param>
        /// <param name="callback">Callback executed once the search is complete</param>
        public void Search(string symbol, DataAccessor.SearchCallback callback)
        {
            Client.FindInstrument(symbol).ContinueWith((instrument) => 
                {
                    Dictionary<string, string> searchResult = new Dictionary<string, string>();
                    foreach(var stock in instrument.Result)
                    {
                        if(!searchResult.ContainsKey(stock.Symbol))
                        {
                            searchResult.Add(stock.Symbol, stock.Name);
                        }
                    }
                    callback(searchResult);
                });
        }

        /// <summary>
        /// Requests the current price for a list of stocks
        /// </summary>
        /// <param name="symbols">The symbols to get quotes for</param>
        /// <param name="callback">Callback executed with the results</param>
        public void GetQuote(List<string> symbols, DataAccessor.PriceDataCallback callback)
        {
            if((symbols.Count > 0) && (Client.isAuthenticated))
            {
                Client.DownloadQuote(symbols).ContinueWith((results) =>
                {
                    // Put the data into a table
                    DataTable dt = new DataTable();
                    dt.Columns.Add("Symbol", typeof(string));
                    dt.Columns.Add("Price", typeof(decimal));

                    if(!results.IsFaulted && results.IsCompleted)
                    {

                        foreach(Quote p in results.Result)
                        {
                            if(p != null) dt.Rows.Add(p.Symbol, (p.LastExtendedHoursTradePrice != null) ? p.LastExtendedHoursTradePrice : p.LastTradePrice);
                        }
                    }

                    // Send the results back
                    callback(dt);
                });
            } else
            {
                // Invalid request, but should still execute the callback
                callback(null);
            }
        }

        /// <summary>
        /// Retrieves information about a stock
        /// </summary>
        /// <param name="symbol">The symbol to retrieve the info for</param>
        /// <param name="callback">Callback executed after the information has been retrieved</param>
        public void GetStockInfo(string symbol, DataAccessor.StockInfoCallback callback)
        {
            Client.DownloadQuote(symbol).ContinueWith((results) =>
            {
                if(results.IsCompleted && !results.IsFaulted)
                {
                    // Put the data into a table
                    var response = results.Result;
                    DataAccessor.StockInfo info = new DataAccessor.StockInfo()
                    {
                        Ask = response.AskPrice,
                        AskVolume = response.AskSize,
                        Bid = response.BidPrice,
                        BidVolume = response.BidSize,
                        PreviousClose = response.AdjustedPreviousClose
                    };


                    // Send the results back
                    callback(info);
                }
                else callback(new DataAccessor.StockInfo());
            });
        }
        #endregion

        #region Broker
        /// <summary>
        /// Indicates if the interface is currently logged in to an account
        /// </summary>
        /// <returns>True if logged in to an account</returns>
        public bool IsSignedIn()
        {
            return Client.isAuthenticated;
        }

        /// <summary>
        /// Logs in to an account
        /// </summary>
        /// <param name="username">The account username (email address)</param>
        /// <param name="password">The account password</param>
        public void SignIn(string username, string password)
        {
            this.UserName = username;
            Client.Authenticate(username, password);
        }

        /// <summary>
        /// Signs in to an account based on a stored token
        /// </summary>
        /// <param name="token">The account session token</param>
        public void SignIn(string token)
        {
            Client.Authenticate(token);
        }

        /// <summary>
        /// Logs the user out of the brokerage account
        /// </summary>
        public void SignOut()
        {
            // Cannot sign out, so simply create a new client which isn't signed in
            Client = new RobinhoodClient();
        }

        /// <summary>
        /// Returns the authentication token associated with the current user's session
        /// </summary>
        /// <returns>The authentication token</returns>
        public string GetAuthenticationToken()
        {
            return Client.RefreshToken;
        }

        /// <summary>
        /// Returns the name of the currently authenticated user
        /// </summary>
        /// <returns>The current username</returns>
        public string GetUsername()
        {
            return UserName;
        }

        /// <summary>
        /// Retrieves the account information
        /// </summary>
        /// <param name="callback">Callback executed after the information has been retrieved</param>
        public void GetAccountInfo(Broker.AccountCallback callback)
        {
            Client.DownloadAllAccounts().ContinueWith((accounts) =>
            {
                if(accounts.IsCompleted && !accounts.IsFaulted)
                {
                    RobinhoodAccount = accounts.Result[0];
                    Broker.Account account = new Broker.Account();
                    account.BuyingPower = RobinhoodAccount.BuyingPower;
                    account.Cash = RobinhoodAccount.Cash;
                    account.CashAvailableForWithdrawal = RobinhoodAccount.Cash - accounts.Result[0].CashHeldForOrders;
                    account.CashHeldForOrders = RobinhoodAccount.CashHeldForOrders;
                    account.UnclearedDeposits = RobinhoodAccount.UnclearedDeposits;
                    account.UnsettledFunds = RobinhoodAccount.UnsettledFunds;

                    Client.DownloadSinglePortfolio(RobinhoodAccount.AccountNumber).ContinueWith((portfolio) =>
                    {
                        AccountPortfolio p = portfolio.Result;
                        account.TotalValue = p.Equity;
                        callback(account);
                    });
                } else
                {
                    callback(null);
                }
            });
        }

        /// <summary>
        /// Returns a list of all stocks currently owned
        /// </summary>
        /// <returns>A list of owned stocks</returns>
        public Dictionary<string, decimal> GetPositions()
        {
            IList<Position> positionsResults = Client.DownloadPositions(getAccount().PositionsUrl.ToString());

            /* Build the return value */
            Dictionary<string, decimal> positions = new Dictionary<string, decimal>();
            foreach(Position p in positionsResults)
            {
                if(p.Quantity != 0)
                {
                    positions.Add(Instruments.GetSymbol(p.InstrumentUrl.ToString(), positionsResults), p.Quantity);
                }
            }

            return positions;
        }

        /// <summary>
        /// Returns more information about a current position
        /// </summary>
        /// <param name="symbol">The symbol to request the position for</param>
        /// <param name="callback">The callback to execute with the requested data</param>
        public void GetPositionInfo(string symbol, Broker.PositionCallback callback)
        {
            Client.DownloadSinglePosition(getAccountId(), Instruments.GetInstrument<IList<Position>>(symbol, null)).ContinueWith((info) =>
            {
                Position pos = info.Result;
                if(pos != null)
                {
                    Broker.Position returnVal = new Broker.Position()
                    {
                        Symbol = symbol,
                        Shares = pos.Quantity,
                        AverageBuyPrice = pos.AverageBuyPrice
                    };
                    callback(returnVal);
                }
                else
                {
                    callback(new Broker.Position() { Symbol = symbol });
                }
            });
        }

        /// <summary>
        /// Returns information about a current position whenever there is a change
        /// </summary>
        /// <param name="symbol">The symbol to request the position for</param>
        /// <returns>The subscription instance</returns>
        public Broker.PositionSubscription SubscribeToPositionInfo(string symbol)
        {
            var subscription = new Broker.PositionSubscription(symbol);
            if(RobinhoodThreadMutex.WaitOne(100))
            {
                PositionSubscriptions.Add(subscription);
                RobinhoodThreadMutex.ReleaseMutex();
            }

            return subscription;
        }

        /// <summary>
        /// Ends the specified subscription
        /// </summary>
        /// <param name="subscription">The subscription to stop</param>
        public void UnsubscribePosition(Broker.PositionSubscription subscription)
        {
            if(RobinhoodThreadMutex.WaitOne(100))
            {
                PositionSubscriptions.Remove(subscription);
                RobinhoodThreadMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Retrives a list of the current orders
        /// </summary>
        /// <returns>The list of orders</returns>
        public List<Broker.Order> GetOrders()
        {
            ActiveOrderMutex.WaitOne();
            List<Broker.Order> activeOrders = ActiveOrders.Where((a) =>
            {
                return a.Status == Broker.Order.OrderStatus.PENDING;
            }).ToList<Broker.Order>();
            ActiveOrderMutex.ReleaseMutex();

            return activeOrders;
        }

        /// <summary>
        /// Submits an order to be executed by the broker
        /// </summary>
        /// <param name="order">The order to submit</param>
        public void SubmitOrder(Broker.Order order)
        {
            Dictionary<Broker.Order.OrderType, BasicallyMe.RobinhoodNet.OrderType> orderTypeLookup = new Dictionary<Broker.Order.OrderType, OrderType>()
            {
                { Broker.Order.OrderType.MARKET, BasicallyMe.RobinhoodNet.OrderType.Market },
                { Broker.Order.OrderType.LIMIT, BasicallyMe.RobinhoodNet.OrderType.Limit },
                { Broker.Order.OrderType.STOP, BasicallyMe.RobinhoodNet.OrderType.StopLoss },
                { Broker.Order.OrderType.STOP_LIMIT, BasicallyMe.RobinhoodNet.OrderType.StopLoss },
            };
            bool isStopOrder = ((order.Type == Broker.Order.OrderType.STOP) || (order.Type == Broker.Order.OrderType.STOP_LIMIT));

            NewOrderSingle newOrder = new NewOrderSingle()
            {
                AccountUrl = getAccount().AccountUrl,
                InstrumentUrl = Client.FindInstrument(order.Symbol).Result.First().InstrumentUrl,
                OrderType = orderTypeLookup[order.Type],
                Price = order.LimitPrice,
                Quantity = (int)order.Quantity,
                Side = ((order.BuySell == Broker.Order.BuySellType.BUY) ? Side.Buy : Side.Sell),
                StopPrice = (isStopOrder ? order.StopPrice : (decimal?)null),
                Symbol = order.Symbol,
                TimeInForce = TimeInForce.GoodForDay,
                Trigger = isStopOrder ? TriggerType.Stop : TriggerType.Immediate
            };
            
            Client.PlaceOrder(newOrder).ContinueWith((result) =>
            {
                OrderSnapshot orderResult = result.Result;
                ActiveOrders.Add(new RobinhoodOrder(order.Symbol, orderResult));
            });
            
        }

        /// <summary>
        /// Cancels an outstanding order for the specified symbol
        /// </summary>
        /// <param name="symbol">The symbol to cancel the order for</param>
        public void CancelOrder(string symbol)
        {
            var response = Client.DownloadAllOrders().Result;
            var order = response.Where((a) => { return a.InstrumentId.Equals(Instruments.GetSymbol<IList<OrderSnapshot>>(a.InstrumentId, response)); });
            if(order.Count() > 0)
            {
                Client.CancelOrder(order.First().CancelUrl);
            }
        }

        /// <summary>
        /// Returns a list of stocks being watched
        /// </summary>
        /// <returns>A watchlist registered with the broker</returns>
        public List<string> GetWatchlist()
        {
            
            return null;
        }

        /// <summary>
        /// Modifies a watchlist
        /// </summary>
        /// <param name="stock">The stock symbol to modify</param>
        /// <param name="action">Add -> Adds the stock to the watchlist
        ///                      Remove -> Deletes the stock from the watchlist
        ///                      Move[index] -> Moves the stock to the specified index</param>
        public void ModifyWatchlist(string stock, string action)
        {

        }
        #endregion

        #region Thread
        /// <summary>
        /// The function executed as the Robinhood interface
        /// </summary>
        private void ThreadProcess()
        {
            while(Running)
            {
                // Process the history requests
                if(Client.isAuthenticated &&
                    (HistoryRequests.Count > 0) &&
                    ((DateTime.Now - HistoryRequests[0].RequestTime).TotalSeconds) > HISTORY_REQUEST_PROCESS_DELAY)
                {
                    while((HistoryRequests.Count > 0) && Running)
                    {
                        // Put together a single request
                        RobinhoodThreadMutex.WaitOne();
                        DateTime start = HistoryRequests[0].Start;
                        DateTime end = HistoryRequests[0].End;
                        TimeSpan interval = getHistoryInterval(HistoryRequests[0].Interval, getHistoryTimeSpan(start, end));
                        List<string> symbols = new List<string>() { HistoryRequests[0].Symbol };
                        List<int> servicedIndices = new List<int>() { 0 };
                        for(int i = 1; i < HistoryRequests.Count; i++)
                        {
                            if(getHistoryInterval(HistoryRequests[i].Interval, getHistoryTimeSpan(HistoryRequests[i].Start, HistoryRequests[i].End)) == interval)
                            {
                                // Include this in the request
                                symbols.Add(HistoryRequests[i].Symbol);
                                servicedIndices.Add(i);
                                start = ((start < HistoryRequests[i].Start) ? start : HistoryRequests[i].Start);
                                end = ((end > HistoryRequests[i].End) ? end : HistoryRequests[i].End);
                            }
                        }
                        RobinhoodThreadMutex.ReleaseMutex();

                        // Make the request
                        if((getHistoryTimeSpan(start, end) >= HISTORY_SPANS.ElementAt(HISTORY_SPANS.Count - 1).Key) ||          // If requesting too far back in history
                            ((start.Date == end.Date) && ((end < EXTENDED_HOURS_OPEN) || (start >= EXTENDED_HOURS_CLOSE))))     // If requesting data outside of the available times
                        {
                            foreach(var s in servicedIndices) HistoryRequests[s].Callback(null);
                        }
                        else
                        {
                            var bounds = ((start.Date == end.Date) ? HISTORY_BOUNDS.EXTENDED : HISTORY_BOUNDS.REGULAR); // Can only get extended hours history for the current day
                            var request = Client.DownloadHistory(symbols, HISTORY_INTERVALS[interval], HISTORY_SPANS[getHistoryTimeSpan(start, end)], bounds);
                            request.Wait();
                            if(!request.IsCompleted || request.IsFaulted) continue;
                            var history = request.Result;

                            // Return the data to the reqesting sources
                            int servicedCount = 0;
                            foreach(var stock in history)
                            {
                                if(stock.HistoricalInfo.Count > 0)
                                {
                                    // Put the data into a table
                                    DataTable dt = new DataTable();
                                    dt.Columns.Add("Time", typeof(DateTime));
                                    dt.Columns.Add("Price", typeof(float));
                                    foreach(var p in stock.HistoricalInfo)
                                    {
                                        DateTime t = (interval < new TimeSpan(1, 0, 0, 0)) ? p.BeginsAt.ToLocalTime() : p.BeginsAt.AddHours(9.5);
                                        dt.Rows.Add(t, (float)p.OpenPrice);
                                    }

                                    // Add a final price
                                    dt.Rows.Add(stock.HistoricalInfo[stock.HistoricalInfo.Count - 1].BeginsAt.Add(interval).ToLocalTime(), stock.HistoricalInfo[stock.HistoricalInfo.Count - 1].ClosePrice);

                                    // Pass the table back to the caller
                                    if(!HistoryRequests[servicedIndices[servicedCount]].Symbol.Equals(stock.Symbol))
                                    {
                                        //throw new Exception("Response does not match the request");
                                    }
                                    HistoryRequests[servicedIndices[servicedCount]].Callback(dt);
                                    servicedCount++;
                                }
                            }
                        }

                        // Remove the processed requests from the queue
                        RobinhoodThreadMutex.WaitOne();
                        for(int i = servicedIndices.Count - 1; i >= 0; i--)
                        {
                            HistoryRequests.RemoveAt(servicedIndices[i]);
                        }
                        RobinhoodThreadMutex.ReleaseMutex();
                    }
                }

                // Process the position subscriptions
                if(RobinhoodThreadMutex.WaitOne(0))
                {
                    for(int i = 0; Client.isAuthenticated && (i < PositionSubscriptions.Count); i++)
                    {
                        Broker.PositionSubscription sub = PositionSubscriptions[i];
                        var orderInfo = ActiveOrders.Find((a) => { return a.Symbol.Equals(sub.PositionInfo.Symbol); });
                        if(((DateTime.Now - sub.LastUpdated).TotalSeconds > 5) &&
                            (sub.Dirty || ((orderInfo != null) && (orderInfo.RefreshedAt != sub.LastUpdated))))
                        {
                            sub.LastUpdated = DateTime.Now;
                            GetPositionInfo(sub.PositionInfo.Symbol, (info) =>
                            {
                                sub.PositionInfo = info;
                                sub.LastUpdated = ((orderInfo != null) ? orderInfo.RefreshedAt : DateTime.Now);
                                sub.Dirty = false;
                                if(sub.Notify != null) sub.Notify(sub);
                            });
                        }
                    }
                    RobinhoodThreadMutex.ReleaseMutex();
                }

                // Update the active orders
                RefreshActiveOrders();

                // Sleep so the thread doesn't run at 100% CPU
                System.Threading.Thread.Sleep(5);
            }
        }

        /// <summary>
        /// Updates the status of active orders
        /// </summary>
        private void RefreshActiveOrders()
        {
            // Periodically update the entire list of orders
            if((DateTime.Now - OrderLastUpdated).TotalSeconds > 60)
            {
                OrderLastUpdated = DateTime.MaxValue;
                System.Threading.Tasks.Task<IList<OrderSnapshot>> response;
                if(ActiveOrders.Count > 0) response = Client.DownloadOrders(ActiveOrders[0].UpdatedAt.AddDays(-1));
                else response = Client.DownloadAllOrders();

                response.ContinueWith((result) =>
                {
                    ActiveOrderMutex.WaitOne();
                    if((result.Status != System.Threading.Tasks.TaskStatus.Canceled) && result.IsCompleted && !result.IsFaulted)
                    {
                        foreach(OrderSnapshot s in result.Result)
                        {
                            RobinhoodOrder order = ActiveOrders.Find(a => a.OrderId.Equals(s.OrderId));
                            if(order == null)
                            {
                                order = new RobinhoodOrder(Client.DownloadInstrument(s.InstrumentId).Result.Symbol, s);
                                ActiveOrders.Add(order);
                            }
                            order.Update(s);
                        }
                    }
                    OrderLastUpdated = DateTime.Now;
                    ActiveOrderMutex.ReleaseMutex();
                });
            }
            else
            {
                // Update pending orders more frequently
                for(int orderIdx = 0; orderIdx < ActiveOrders.Count; orderIdx++)
                {
                    RobinhoodOrder order = ActiveOrders[orderIdx];
                    if((order.Status == Broker.Order.OrderStatus.PENDING) &&
                        ((DateTime.Now - order.RefreshedAt).TotalSeconds >= 5))
                    {
                        // Update the status of this order
                        Client.DownloadSingleOrder(order.OrderId).ContinueWith((result) =>
                        {
                            if(result.IsCompleted && !result.IsFaulted)
                            {
                                order.Update(result.Result);
                            }
                        });
                        break;
                    }
                }
            }
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Waits until the account information is available
        /// </summary>
        private Account getAccount()
        {
            bool accountReady = (RobinhoodAccount != null);
            if(!accountReady)
            {
                GetAccountInfo((account) => { accountReady = true; });
            }

            while(!accountReady) { /* Wait */ }

            return RobinhoodAccount;
        }

        /// <summary>
        /// Returns the ID of the account used in URL transactions
        /// </summary>
        /// <returns>The account ID string</returns>
        private string getAccountId()
        {
            string accountUrl = getAccount().AccountUrl.ToString().TrimEnd(new char[] { '/' });
            return accountUrl.Substring(accountUrl.LastIndexOf('/') + 1);
        }

        /// <summary>
        /// Returns the closest supported interval
        /// </summary>
        /// <param name="interval">The desired inerval</param>
        /// <param name="historyPeriod">The period over which history data will be requested</param>
        /// <returns>The closest supported interval</returns>
        private TimeSpan getHistoryInterval(TimeSpan interval, TimeSpan historyPeriod)
        {
            TimeSpan supportedInterval = HISTORY_INTERVALS.ElementAt(0).Key;

            // An API request of a year or more can not get request an interval smaller than a day
            if(historyPeriod >= new TimeSpan(365 * 5, 0, 0, 0)) interval = new TimeSpan(7, 0, 0, 0);
            else if(historyPeriod >= new TimeSpan(365, 0, 0, 0)) interval = new TimeSpan(1, 0, 0, 0);

            for(int idx = 0; idx < HISTORY_INTERVALS.Count; idx++)
            {
                if(interval <= HISTORY_INTERVALS.ElementAt(idx).Key)
                {
                    supportedInterval = HISTORY_INTERVALS.ElementAt(idx).Key;
                    break;
                }
            }

            return supportedInterval;
        }

        /// <summary>
        /// Returns the time span needed to retrieve the requested data
        /// </summary>
        /// <param name="start">The starting time for the request</param>
        /// <param name="end">The ending time for the request</param>
        /// <returns>The closest time span supported by the Robinhood API</returns>
        private TimeSpan getHistoryTimeSpan(DateTime start, DateTime end)
        {
            TimeSpan desired = (DateTime.Now - start);
            TimeSpan supported = HISTORY_SPANS.ElementAt(0).Key;
            for(int i = 0; i < HISTORY_SPANS.Count; i++)
            {
                if(desired <= HISTORY_SPANS.ElementAt(i).Key)
                {
                    supported = HISTORY_SPANS.ElementAt(i).Key;
                    break;
                }
            }

            return supported;
        }
        #endregion
    }
}
