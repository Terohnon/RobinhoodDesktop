using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop
{
    public class DataTableCache : DataAccessor.DataAccessorInterface
    {
        public DataTableCache(DataAccessor.DataAccessorInterface source)
        {
            this.Source = source;
        }

        #region Variables
        /// <summary>
        /// The data source
        /// </summary>
        public DataAccessor.DataAccessorInterface Source;

        /// <summary>
        /// The cached data
        /// </summary>
        public Dictionary<string, DataTable> Cache = new Dictionary<string, DataTable>();
        #endregion

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
            DataTable data;
            if(!Cache.TryGetValue(symbol, out data))
            {
                Source.GetPriceHistory(symbol, start, end, interval, (rx) =>
                {
                    if(!Cache.ContainsKey(symbol))
                    {
                        Cache.Add(symbol, rx);
                    }
                    callback(rx);
                });
            }
            else if((data.Rows.Count == 0) || ((DateTime)data.Rows[0][StockChart.TIME_DATA_TAG] > start) || ((DateTime)data.Rows[data.Rows.Count - 1][StockChart.TIME_DATA_TAG] < end))
            {
                DateTime newStart = new DateTime(Math.Min(((DateTime)data.Rows[0][StockChart.TIME_DATA_TAG]).Ticks, start.Ticks));
                DateTime newEnd = new DateTime(Math.Max(((DateTime)data.Rows[0][StockChart.TIME_DATA_TAG]).Ticks, end.Ticks));
                Source.GetPriceHistory(symbol, newStart, newEnd, interval, (rx) =>
                {
                    int cacheIdx = 0;
                    for(int i = 0; (rx != null) && i < rx.Rows.Count; i++)
                    {
                        bool shouldAdd = true;
                        DateTime newTime = (DateTime)rx.Rows[i][StockChart.TIME_DATA_TAG];
                        for(; cacheIdx < data.Rows.Count; cacheIdx++)
                        {
                            DateTime cacheTime = (DateTime)data.Rows[cacheIdx][StockChart.TIME_DATA_TAG];
                            if(cacheTime >= newTime)
                            {
                                shouldAdd = (cacheTime != newTime);
                                break;
                            }
                        }
                        if(shouldAdd)
                        {
                            DataRow newRow = data.NewRow();
                            newRow.ItemArray = rx.Rows[i].ItemArray;
                            data.Rows.InsertAt(newRow, cacheIdx);
                            cacheIdx++;
                        }
                    }

                    // Pass the cached data to the requesting object
                    callback(data);
                });
            }
            else
            {
                callback(data);
            }

        }

        /// <summary>
        /// Searches for stocks based on the symbol string
        /// </summary>
        /// <param name="symbol">The symbol (or portion of) to search for</param>
        /// <param name="callback">Callback executed once the search is complete</param>
        public void Search(string symbol, DataAccessor.SearchCallback callback)
        {
            Source.Search(symbol, callback);
        }
    }
}
