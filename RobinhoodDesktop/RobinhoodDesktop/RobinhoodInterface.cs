using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading;
using BasicallyMe.RobinhoodNet;

namespace RobinhoodDesktop
{
    public class RobinhoodInterface : DataAccessor.DataAccessorInterface
    {
        public RobinhoodInterface()
        {
            this.Client = new RobinhoodClient();

            RobinhoodThread = new Thread(ThreadProcess);
            RobinhoodThread.Start();
        }

        #region Constants
        /// <summary>
        /// The amount of time to delay before processing a history request
        /// to allow multiple requests to be grouped together.
        /// </summary>
        private const double HISTORY_REQUEST_PROCESS_DELAY = 0.1;

        /// <summary>
        /// The supported time intervals for history data
        /// </summary>
        private static readonly Dictionary<TimeSpan, string> HISTORY_INTERVALS = new Dictionary<TimeSpan, string>()
        {
            { new TimeSpan(0, 5, 0), "5minute" },
            { new TimeSpan(1, 0, 0), "hour" },
            { new TimeSpan(24, 0, 0), "day" },
        };

        private static readonly Dictionary<TimeSpan, string> HISTORY_SPANS = new Dictionary<TimeSpan, string>()
        {
            { new TimeSpan(1, 0, 0, 0), "day" },
            { new TimeSpan(7, 0 ,0, 0), "week" },
            { new TimeSpan(365, 0, 0, 0), "year" },
            { new TimeSpan((365 * 5), 0, 0, 0), "5year" },
            { TimeSpan.MaxValue, "all" }
        };
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
        #endregion

        #region Variables
        /// <summary>
        /// The client used to access the Robinhood API
        /// </summary>
        public RobinhoodClient Client;

        /// <summary>
        /// The thread used to process the requests
        /// </summary>
        public System.Threading.Thread RobinhoodThread;

        /// <summary>
        /// The pending history requests
        /// </summary>
        private List<HistoryRequest> HistoryRequests = new List<HistoryRequest>();

        /// <summary>
        /// Mutex used to protect access to the pending history request list
        /// </summary>
        private Mutex HistoryMutex = new Mutex();
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
            HistoryMutex.WaitOne();
            HistoryRequests.Add(new HistoryRequest(symbol, start, end, interval, callback));
            HistoryMutex.ReleaseMutex();
        }

        /// <summary>
        /// The function executed as the Robinhood interface
        /// </summary>
        private void ThreadProcess()
        {
            while(true)
            {
                // Process the history requests
                if((HistoryRequests.Count > 0) &&
                    ((DateTime.Now - HistoryRequests[0].RequestTime).TotalSeconds) > HISTORY_REQUEST_PROCESS_DELAY)
                {
                    while(HistoryRequests.Count > 0)
                    {
                        // Put together a single request
                        HistoryMutex.WaitOne();
                        TimeSpan interval = getHistoryInterval(HistoryRequests[0].Interval);
                        List<string> symbols = new List<string>() { HistoryRequests[0].Symbol };
                        List<int> servicedIndices = new List<int>() { 0 };
                        DateTime start = HistoryRequests[0].Start;
                        DateTime end = HistoryRequests[0].End;
                        for(int i = 1; i < HistoryRequests.Count; i++)
                        {
                            if(getHistoryInterval(HistoryRequests[i].Interval) == interval)
                            {
                                // Include this in the request
                                symbols.Add(HistoryRequests[i].Symbol);
                                servicedIndices.Add(i);
                                start = ((start < HistoryRequests[i].Start) ? start : HistoryRequests[i].Start);
                                end = ((end > HistoryRequests[i].End) ? end : HistoryRequests[i].End);
                            }
                        }
                        HistoryMutex.ReleaseMutex();

                        // Make the request
                        var history = Client.DownloadHistory(symbols, HISTORY_INTERVALS[interval], HISTORY_SPANS[getHistoryTimeSpan(start, end)]).Result;

                        // Return the data to the reqesting sources
                        int servicedCount = 0;
                        foreach(var stock in history)
                        {
                            // Put the data into a table
                            DataTable dt = new DataTable();
                            dt.Columns.Add("Time", typeof(DateTime));
                            dt.Columns.Add("Price", typeof(float));
                            foreach(var p in stock.HistoricalInfo)
                            {
                                dt.Rows.Add(p.BeginsAt.ToLocalTime(), (float)p.OpenPrice);
                            }

                            // Pass the table back to the caller
                            if(!HistoryRequests[servicedIndices[servicedCount]].Symbol.Equals(stock.Symbol))
                            {
                                throw new Exception("Response does not match the request");
                            }
                            HistoryRequests[servicedIndices[servicedCount]].Callback(dt);
                            servicedCount++;
                        }

                        // Remove the processed requests from the queue
                        HistoryMutex.WaitOne();
                        for(int i = servicedIndices.Count - 1; i >= 0; i--)
                        {
                            HistoryRequests.RemoveAt(servicedIndices[i]);
                        }
                        HistoryMutex.ReleaseMutex();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the closest supported interval
        /// </summary>
        /// <param name="interval">The desired inerval</param>
        /// <returns>The closest supported interval</returns>
        private TimeSpan getHistoryInterval(TimeSpan interval)
        {
            TimeSpan supportedInterval = HISTORY_INTERVALS.ElementAt(0).Key;
            for(int idx = 0; idx < HISTORY_INTERVALS.Count; idx++)
            {
                if(interval <= HISTORY_INTERVALS.ElementAt(idx).Key)
                {
                    supportedInterval = HISTORY_INTERVALS.ElementAt(0).Key;
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
    }
}
