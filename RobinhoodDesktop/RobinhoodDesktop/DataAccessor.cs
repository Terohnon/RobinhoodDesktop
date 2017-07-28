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
        }
        #endregion

        #region Variables
        /// <summary>
        /// The instance used to perform the actions
        /// </summary>
        private static DataAccessorInterface Accessor;
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
        #endregion
    }
}
