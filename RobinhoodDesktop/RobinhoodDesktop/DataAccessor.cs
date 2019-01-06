using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace RobinhoodDesktop
{
    public class DataAccessor
    {
        #region Types
        /// <summary>
        /// A callback that is executed when data is available
        /// </summary>
        /// <param name="data"></param>
        public delegate void PriceDataCallback(DataTable data);

        /// <summary>
        /// Callback executed after retrieving the stock information
        /// </summary>
        /// <param name="info">Contains the stock information</param>
        public delegate void StockInfoCallback(StockInfo info);

        /// <summary>
        /// A callback that is executed to request data
        /// </summary>
        /// <param name="symbol">The stock symbol to request data for</param>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        /// <param name="interval">The requested interval between points in the returned data</param>
        /// <param name="callback">The callback to execute once the data has been retrieved</param>
        public delegate void DataRequest(string symbol, DateTime start, DateTime end, TimeSpan interval, DataAccessor.PriceDataCallback callback);

        /// <summary>
        /// A callback that is executed when a search is completed
        /// </summary>
        /// <param name="results">The results of the search, sent as a symbol and full name pair</param>
        public delegate void SearchCallback(Dictionary<string, string> results);

        /// <summary>
        /// A class that supports subscribing to a stock to get periodic price updates
        /// </summary>
        public class Subscription
        {
            #region Variables
            /// <summary>
            /// The symbol the subscription is for
            /// </summary>
            public string Symbol;

            /// <summary>
            /// The last received price 
            /// </summary>
            public decimal Price;

            /// <summary>
            /// The time at which the last update was received
            /// </summary>
            public DateTime LastUpdated;

            /// <summary>
            /// The rate at which the subscription should be updated
            /// </summary>
            public TimeSpan Period;

            /// <summary>
            /// The callbacks to execute on update
            /// </summary>
            public SubscriptionCallback Notify;
            #endregion


            #region Types
            /// <summary>
            /// A callback executed whenever the subscription is updated
            /// </summary>
            /// <param name="sub"></param>
            public delegate void SubscriptionCallback(Subscription sub);
            #endregion

            public Subscription(string symbol, TimeSpan period)
            {
                this.Symbol = symbol;
                this.Period = period;
            }
        }

        public struct StockInfo
        {
            public decimal Bid;
            public decimal BidVolume;
            public decimal Ask;
            public decimal AskVolume;
            public decimal PreviousClose;
        }
        #endregion

        #region Interface
        public interface DataAccessorInterface
        {
            /// <summary>
            /// Requests price history data for a stock
            /// </summary>
            /// <param name="symbol">The stock symbol to request for</param>
            /// <param name="start">The start of the time range</param>
            /// <param name="end">The end of the time range</param>
            /// <param name="interval">The desired interval between price points in the data set</param>
            /// <param name="callback">A callback that is executed once the requested data is available</param>
            void GetPriceHistory(string symbol, DateTime start, DateTime end, TimeSpan interval, PriceDataCallback callback);

            /// <summary>
            /// Searches for stocks based on the symbol string
            /// </summary>
            /// <param name="symbol">The symbol (or portion of) to search for</param>
            /// <param name="callback">Callback executed once the search is complete</param>
            void Search(string symbol, SearchCallback callback);

            /// <summary>
            /// Requests the current price for a list of stocks
            /// </summary>
            /// <param name="symbols">The symbols to get quotes for</param>
            /// <param name="callback">Callback executed with the results</param>
            void GetQuote(List<string> symbols, PriceDataCallback callback);

            /// <summary>
            /// Retrieves information about a stock
            /// </summary>
            /// <param name="symbol">The symbol to retrieve the info for</param>
            /// <param name="callback">Callback executed after the information has been retrieved</param>
            void GetStockInfo(string symbol, StockInfoCallback callback);
        }
        #endregion

        #region Constants
        /// <summary>
        /// Subscribe to updates every second
        /// </summary>
        public static readonly TimeSpan SUBSCRIBE_ONE_SEC = new TimeSpan(0, 0, 1);

        /// <summary>
        /// Subscribe to updates every five second
        /// </summary>
        public static readonly TimeSpan SUBSCRIBE_FIVE_SEC = new TimeSpan(0, 0, 5);

        /// <summary>
        /// Subscribe to updates every minute
        /// </summary>
        public static readonly TimeSpan SUBSCRIBE_ONE_MIN = new TimeSpan(0, 1, 0);
        #endregion

        #region Variables
        /// <summary>
        /// The instance used to perform the actions
        /// </summary>
        public static DataAccessorInterface Accessor;

        /// <summary>
        /// List of the current subscriptions
        /// </summary>
        private static Dictionary<string, List<Subscription>> Subscriptions = new Dictionary<string, List<Subscription>>();

        /// <summary>
        /// Keeps track of the current update tick
        /// </summary>
        private static UInt64 SubscriptionUpdateTick = 0;

        /// <summary>
        /// Timer that handles supdating the subscriptions
        /// </summary>
        private static System.Timers.Timer SubscriptionTimer = null;
        #endregion

        #region Static Functions
        /// <summary>
        /// Requests price history data for a stock
        /// </summary>
        /// <param name="symbol">The stock symbol to request for</param>
        /// <param name="start">The start of the time range</param>
        /// <param name="end">The end of the time range</param>
        /// <param name="interval">The desired interval between price points in the data set</param>
        /// <param name="callback">A callback that is executed once the requested data is available</param>
        public static void GetPriceHistory(string symbol, DateTime start, DateTime end, TimeSpan interval, PriceDataCallback callback)
        {
            Accessor.GetPriceHistory(symbol, start, end, interval, callback);
        }

        /// <summary>
        /// Searches for stocks based on the symbol string
        /// </summary>
        /// <param name="symbol">The symbol (or portion of) to search for</param>
        /// <param name="callback">Callback executed once the search is complete</param>
        public static void Search(string symbol, SearchCallback callback)
        {
            Accessor.Search(symbol, callback);
        }

        /// <summary>
        /// Sets the instance used to access stock data
        /// </summary>
        /// <param name="accessor">The acessor instance</param>
        public static void SetAccessor(DataAccessorInterface accessor)
        {
            Accessor = accessor;
        }

        /// <summary>
        /// Subscribes to updates for a stock price
        /// </summary>
        /// <param name="symbol">The stock to subscribe to</param>
        /// <param name="period">The rate at which to receive updates</param>
        /// <returns>The subscription method</returns>
        public static Subscription Subscribe(string symbol, TimeSpan period)
        {
            Subscription sub = null;

            List<Subscription> subList;
            if(!Subscriptions.TryGetValue(symbol, out subList))
            {
                subList = new List<Subscription>();
                Subscriptions[symbol] = subList;
            }

            sub = new Subscription(symbol, period);
            subList.Add(sub);

            // Check if a subscription timer exists
            if(SubscriptionTimer == null)
            {
                SubscriptionTimer = new System.Timers.Timer();
                SubscriptionTimer.Elapsed += UpdateSubscriptions;
                SubscriptionTimer.Interval = (int)1000;
                SubscriptionTimer.Start();
            }

            return sub;
        }

        /// <summary>
        /// Unsubscribes 
        /// </summary>
        /// <param name="sub"></param>
        public static void Unsubscribe(Subscription sub)
        {
            List<Subscription> subList;
            if(Subscriptions.TryGetValue(sub.Symbol, out subList))
            {
                subList.Remove(sub);
            }
        }

        /// <summary>
        /// Performs all actions necessary to prepare the DataAccessor to shut down
        /// </summary>
        public static void Close()
        {
            SubscriptionTimer.Stop();
        }

        /// <summary>
        /// Executed periodically to update the active subscriptions
        /// </summary>
        private static void UpdateSubscriptions(object sender, EventArgs e)
        {
            // Advance to the next tick
            SubscriptionUpdateTick++;

            // Get a list of symbols that need to be updated
            List<string> updateSymbols = new List<string>();
            foreach(var pair in Subscriptions)
            {
                foreach(var sub in pair.Value)
                {
                    if((SubscriptionUpdateTick % (UInt64)sub.Period.TotalSeconds) == 0)
                    {
                        if(!updateSymbols.Contains(sub.Symbol)) updateSymbols.Add(sub.Symbol);
                    }
                }
            }

            // Get the quotes
            if(updateSymbols.Count > 0)
            {
                Accessor.GetQuote(updateSymbols, (dt) =>
                {
                    foreach(DataRow stock in dt.Rows)
                    {
                        foreach(var sub in Subscriptions[(string)stock["Symbol"]])
                        {
                            if((SubscriptionUpdateTick % (UInt64)sub.Period.TotalSeconds) == 0)
                            {
                                sub.Price = (decimal)stock["Price"];
                                sub.LastUpdated = DateTime.Now;
                                sub.Notify(sub);
                            }
                        }
                    }
                });
            }
        }
        #endregion
    }
}
