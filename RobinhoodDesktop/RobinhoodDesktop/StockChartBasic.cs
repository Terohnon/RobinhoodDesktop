using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NPlot;
using System.Drawing;

namespace RobinhoodDesktop
{
    public class StockChartBasic
    {
        public StockChartBasic()
        {
            DataTable emptyTable = new DataTable();
            emptyTable.Columns.Add("Time", typeof(DateTime));
            emptyTable.Columns.Add("Price", typeof(float));

            // Create the price line for the plot
            priceLine = new LinePlot();
            priceLine.DataSource = emptyTable;
            priceLine.AbscissaData = TIME_DATA_TAG;
            priceLine.OrdinateData = PRICE_DATA_TAG;
            priceLine.Color = PRICE_COLOR_POSITIVE;

            // Create the origin open price line
            openLine = new LinePlot();
            openLine.DataSource = emptyTable;
            openLine.AbscissaData = TIME_DATA_TAG;
            openLine.OrdinateData = PRICE_DATA_TAG;
            openLine.Pen = new System.Drawing.Pen(GUIDE_COLOR);
            openLine.Pen.DashPattern = new float[] { 2.0f, 2.0f };
            openLine.Pen.DashCap = System.Drawing.Drawing2D.DashCap.Round;
            openLine.Pen.Width = 1.5f;

            // Create the surface used to draw the plot
            stockPricePlot = new NPlot.Swf.InteractivePlotSurface2D();
            stockPricePlot.Add(priceLine);
            stockPricePlot.Add(openLine);
            stockPricePlot.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            stockPricePlot.ShowCoordinates = false;
            stockPricePlot.XAxis1.HideTickText = true;
            //stockPricePlot.XAxis1.NumberFormat = " h";
            stockPricePlot.XAxis1.AxisColor = System.Drawing.Color.Transparent;
            TradingDateTimeAxis tradeAxis = new TradingDateTimeAxis(stockPricePlot.XAxis1);
            tradeAxis.StartTradingTime = new TimeSpan(9, 30, 0);
            tradeAxis.EndTradingTime = new TimeSpan(16, 0, 0);
            stockPricePlot.XAxis1 = tradeAxis;
            stockPricePlot.YAxis1.HideTickText = true;
            stockPricePlot.YAxis1.Color = System.Drawing.Color.Transparent;
            stockPricePlot.PlotBackColor = BACKGROUND_COLOR;
            stockPricePlot.SurfacePadding = 5;

            stockPricePlot.Refresh();
        }

        /// <summary>
        /// Sets the data for the chart to use
        /// </summary>
        /// <param name="data">A table of data which contains Time and Price columns</param>
        public void SetChartData(DataTable data)
        {
            this.Source = data;
            if((data != null) && Canvas.IsHandleCreated) Canvas.BeginInvoke((Action)(() => { UpdateChartData(); }));
        }


        /// <summary>
        /// Updates and re-draws the chart based on the current data set
        /// </summary>
        protected virtual void UpdateChartData()
        {
            if(priceLine.DataSource != Source)
            {
                TradingDateTimeAxis tradeAxis = (TradingDateTimeAxis)stockPricePlot.XAxis1;
                priceLine.DataSource = Source;

                // Set the initial view of the data to the last trading day
                tradeAxis.WorldMax = (double)((DateTime)Source.Rows[Source.Rows.Count - 1][TIME_DATA_TAG]).Date.AddHours(16).Ticks;
                tradeAxis.WorldMin = tradeAxis.SparseWorldAdd((double)(new DateTime((long)tradeAxis.WorldMax)).Ticks, -(tradeAxis.EndTradingTime - tradeAxis.StartTradingTime).Ticks * 1.5);

                // Create a data table which acts as a reference point for each day
                DailyData = new DataTable();
                DailyData.Columns.Add("Time", typeof(DateTime));
                DailyData.Columns.Add("Price", typeof(float));
                DailyData.Columns.Add("Min", typeof(float));
                DailyData.Columns.Add("Max", typeof(float));
                float dayPrice = (float)Source.Rows[0][PRICE_DATA_TAG];
                for(DateTime time = ((DateTime)Source.Rows[0][TIME_DATA_TAG]).Date.AddHours(16); time <= ((DateTime)Source.Rows[Source.Rows.Count - 1][TIME_DATA_TAG]).Date.AddHours(16); time = time.AddDays(1))
                {
                    int idx = GetTimeIndex(time);
                    if(((DateTime)Source.Rows[idx][TIME_DATA_TAG]).Date == time.Date)
                    {
                        // Mark the time as the last trading time for the next day
                        DailyData.Rows.Add(time.Date.AddHours(9.5), dayPrice, 0, 0);
                        DailyData.Rows.Add(time.Date.AddHours(16), dayPrice, 0, 0);

                        // Set the ending price of this day as the reference point of the next day
                        dayPrice = (float)Source.Rows[idx][PRICE_DATA_TAG];
                    }
                }
                openLine.DataSource = DailyData;
            }

            // Refresh the chart
            UpdatePriceMinMax();
            stockPricePlot.Refresh();
        }

        #region Constants
        /// <summary>
        /// The color of the price information when it is positive
        /// </summary>
        public static readonly System.Drawing.Color PRICE_COLOR_POSITIVE = System.Drawing.Color.FromArgb(255, 0, 173, 145);

        /// <summary>
        /// The color of text
        /// </summary>
        public static readonly System.Drawing.Color TEXT_COLOR = System.Drawing.Color.FromArgb(255, 255, 255, 255);

        /// <summary>
        /// The color of guidelines and text
        /// </summary>
        public static readonly System.Drawing.Color GUIDE_COLOR = System.Drawing.Color.FromArgb(255, 56, 66, 71);

        /// <summary>
        /// The background color
        /// </summary>
        public static readonly System.Drawing.Color BACKGROUND_COLOR = System.Drawing.Color.FromArgb(255, 17, 27, 32);

        /// <summary>
        /// The tag identifying a time entry in a data table
        /// </summary>
        public static readonly string TIME_DATA_TAG = "Time"; 

        /// <summary>
        /// The tag identifying a price entry in a data table
        /// </summary>
        public static readonly string PRICE_DATA_TAG = "Price";
        #endregion

        #region Types
        #endregion

        #region Variables
        /// <summary>
        /// The source data that is being plotted
        /// </summary>
        public DataTable Source;

        /// <summary>
        /// The data table used to draw the reference price line for each day
        /// (which is the previous closing price)
        /// </summary>
        public DataTable DailyData;

        /// <summary>
        /// The amount of margin to apply between the maximum stock price and the top of the chart
        /// </summary>
        public double MarginMax = 1.01;

        /// <summary>
        /// The amount of margin to apply between the minimum stock price and the bottom of the chart
        /// </summary>
        public double MarginMin = 0.99;

        /// <summary>
        /// The plot surface used to display the chart
        /// </summary>
        protected NPlot.Swf.InteractivePlotSurface2D stockPricePlot;

        /// <summary>
        /// The line representing the stock price
        /// </summary>
        protected NPlot.LinePlot priceLine;

        /// <summary>
        /// The line representing the reference price for each day
        /// </summary>
        protected NPlot.LinePlot openLine;
        #endregion

        #region Properties
        /// <summary>
        /// Accesses the canvas object for the chart
        /// </summary>
        public System.Windows.Forms.Control Canvas
        {
            get { return stockPricePlot.Canvas; }
        }
        #endregion

        #region Utility Functions
        /// <summary>
        /// Returns the index corresponding to the given time (or -1 if no match is found)
        /// </summary>
        /// <param name="time">The time to get the point for</param>
        /// <param name="src">The source data to access</param>
        /// <returns>The index of the point in the source data</returns>
        public int GetTimeIndex(DateTime time, DataTable src)
        {
            int idx = -1;
            if(src != null)
            {
                int min = 0;
                int max = src.Rows.Count;

                while(true)
                {
                    int mid = (max + min) / 2;
                    DateTime checkTime = (DateTime)src.Rows[mid]["Time"];
                    if((min + 1) >= max)
                    {
                        idx = min;
                        break;
                    }
                    else if(checkTime > time)
                    {
                        max = mid;
                    }
                    else
                    {
                        min = mid;
                    }
                }
            }

            return idx;
        }

        /// <summary>
        /// Returns the index corresponding to the given time (or -1 if no match is found)
        /// </summary>
        /// <param name="time">The time to get the point for</param>
        /// <param name="src">The source data to access</param>
        /// <returns>The index of the point in the source data</returns>
        public int GetTimeIndex(DateTime time)
        {
            return GetTimeIndex(time, Source);
        }

        /// <summary>
        /// Recalculates the minimum and maximum price to chart based on the visible data
        /// </summary>
        protected void UpdatePriceMinMax()
        {
            // Find the min and max over the draw range
            DateTime start = new DateTime((long)stockPricePlot.XAxis1.WorldMin);
            DateTime end = new DateTime((long)stockPricePlot.XAxis1.WorldMax);
            float min = float.PositiveInfinity;
            float max = 0;
            int startIdx = GetTimeIndex(start);
            int endIdx = GetTimeIndex(end);
            for(int idx = startIdx; (idx >= 0) && (idx < endIdx); idx++)
            {
                float price = (float)Source.Rows[idx][PRICE_DATA_TAG];
                if(price < min) min = price;
                if(price > max) max = price;
            }


            // Determine the min and max based on the visible range
            if((endIdx > startIdx) && (endIdx > 0))
            {
                stockPricePlot.YAxis1.WorldMax = max * MarginMax;
                stockPricePlot.YAxis1.WorldMin = min * MarginMin;
            }
        }
        #endregion
    }
}
