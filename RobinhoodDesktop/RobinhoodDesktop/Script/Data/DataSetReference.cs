using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobinhoodDesktop.Script
{
    /// <summary>
    /// Can store custom data to associate with each DataSet
    /// </summary>
    public partial class DataPerSet
    {


    }

    public partial struct StockDataSink
    {
        /// <summary>
        /// The dataset this belongs to
        /// </summary>
        [NonSerialized]
        public StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> DataSet;

        /// <summary>
        /// The index of the data set in the overall list
        /// </summary>
        [NonSerialized]
        public int DataSetIndex;


        /// <summary>
        /// The index in the data set
        /// </summary>
        [NonSerialized]
        public int DataPointIndex;

        /// <summary>
        /// The custom data 
        /// </summary>
        [NonSerialized]
        public DataPerSet DataSetData;

        /// <summary>
        /// Time timestamp of this data point
        /// </summary>
        public DateTime Time
        {
            get { return DataSet.Time(DataPointIndex); }
        }


        /// <summary>
        /// Determines the amount of trading time that elapsed between the two 
        /// </summary>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        /// <returns></returns>
        public static TimeSpan GetElapsedTradingTime(DateTime start, DateTime end)
        {
            long ticksStart = toTradingTime(start);
            long ticksEnd = toTradingTime(end);
            return new TimeSpan(ticksEnd - ticksStart);
        }

        /// <summary>
        /// Determines the amount of trading time that elapsed between the two 
        /// </summary>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        /// <returns></returns>
        public static float GetElapsedDays(DateTime start, DateTime end)
        {
            return (float)(GetElapsedTradingTime(start, end).TotalHours / 6.5);
        }

        /// <summary>
        /// Converts the specified time to a trading-hours time (all non-trading time stripped out)
        /// </summary>
        /// <param name="time">The time to convert</param>
        /// <returns>The trading-only time</returns>
        private static long toTradingTime(DateTime time)
        {
            const long startTradingTime = (TimeSpan.TicksPerHour * 9) + (TimeSpan.TicksPerMinute * 30);
            const long endTradingTime = (TimeSpan.TicksPerHour * 16);
            long ticks = time.Ticks;
            long whole_days = ticks / TimeSpan.TicksPerDay;
            long ticks_in_last_day = ticks % TimeSpan.TicksPerDay;
            long full_weeks = whole_days / 7;
            long days_in_last_week = whole_days % 7;
            if(days_in_last_week >= 5)
            {
                days_in_last_week = 5;
                ticks_in_last_day = 0;
            }
            if(ticks_in_last_day < startTradingTime)
            {
                ticks_in_last_day = startTradingTime;
            }
            else if(ticks_in_last_day > endTradingTime)
            {
                ticks_in_last_day = endTradingTime;
            }
            ticks_in_last_day -= startTradingTime;

            long whole_working_days = ((full_weeks * 5) + days_in_last_week);
            long working_ticks = whole_working_days * (endTradingTime - startTradingTime);
            return working_ticks + ticks_in_last_day;
        }

        /// <summary>
        /// The main update function which sets all of the member variables based on other source data
        /// </summary>
        /// <param name="data">The available source data</param>
        /// <param name="updateIndex">The index into the data that should be used as the source for this</param>
        partial void DataSetReference_Update(StockDataSetDerived<StockDataSink, StockDataSource, StockProcessingState> data, int updateIndex)
        {
            this.DataSet = data;
            this.DataSetIndex = data.ProcessingState.DataSetIndex;
            this.DataPointIndex = updateIndex;

            if(DataPointIndex == 0)
            {
                DataSetData = new DataPerSet();
            }
            else
            {
                DataSetData = data[0].DataSetData;
            }
        }
    }
}
