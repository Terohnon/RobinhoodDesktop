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
    public class StockChart
    {
        public StockChart() 
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
            stockPricePlot.XAxis1.TicksCrossAxis = false;
            stockPricePlot.XAxis1.TickTextNextToAxis = true;
            stockPricePlot.XAxis1.SmallTickSize = 0;
            stockPricePlot.XAxis1.LargeTickSize = 0;
            stockPricePlot.XAxis1.HideTickText = false;
            //stockPricePlot.XAxis1.NumberFormat = " h";
            stockPricePlot.XAxis1.TickTextColor = openLine.Pen.Color;
            stockPricePlot.XAxis1.TickTextFont = new System.Drawing.Font("monospace", 8.0f, System.Drawing.FontStyle.Bold);
            stockPricePlot.XAxis1.AxisColor = System.Drawing.Color.Transparent;
            stockPricePlot.XAxis1.TicksLabelAngle = (float)0;
            TradingDateTimeAxis tradeAxis = new TradingDateTimeAxis(stockPricePlot.XAxis1);
            tradeAxis.StartTradingTime = new TimeSpan(9, 30, 0);
            tradeAxis.EndTradingTime = new TimeSpan(16, 0, 0);
            stockPricePlot.XAxis1 = tradeAxis;
            stockPricePlot.YAxis1.HideTickText = false;
            stockPricePlot.YAxis1.Color = System.Drawing.Color.Transparent;
            stockPricePlot.YAxis1.TickTextNextToAxis = true;
            stockPricePlot.YAxis1.TicksIndependentOfPhysicalExtent = true;
            stockPricePlot.YAxis1.TickTextColor = openLine.Pen.Color;
            stockPricePlot.PlotBackColor = BACKGROUND_COLOR;
            stockPricePlot.SurfacePadding = 5;

            // Create the interaction for the chart
            stockPricePlot.AddInteraction(new PlotDrag(true, false));
            stockPricePlot.AddInteraction(new AxisDrag());
            stockPricePlot.AddInteraction(new HoverInteraction(this));

            // Create the text controls
            priceText = new Label();
            priceText.Location = new System.Drawing.Point((stockPricePlot.Canvas.Width - 100) / 2, 10);
            priceText.Font = new System.Drawing.Font("monoprice", 12.0f, System.Drawing.FontStyle.Regular);
            priceText.ForeColor = System.Drawing.Color.White;
            priceText.BackColor = System.Drawing.Color.Transparent;
            priceText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            stockPricePlot.Canvas.Controls.Add(priceText);
            priceText.BringToFront();
            changeText = new Label();
            changeText.Location = new Point(priceText.Location.X - 80, priceText.Location.Y + 25);
            changeText.Size = new Size(260, changeText.Size.Height);
            changeText.Font = new System.Drawing.Font("monoprice", 9.0f, System.Drawing.FontStyle.Regular);
            changeText.ForeColor = System.Drawing.Color.White;
            changeText.BackColor = System.Drawing.Color.Transparent;
            changeText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            stockPricePlot.Canvas.Controls.Add(changeText);
            changeText.BringToFront();

            stockPricePlot.Refresh();
        }

        /// <summary>
        /// Sets the data for the chart to use
        /// </summary>
        /// <param name="data">A table of data which contains Time and Price columns</param>
        public void SetChartData(DataTable data)
        {
            TradingDateTimeAxis tradeAxis = (TradingDateTimeAxis)stockPricePlot.XAxis1;
            this.Source = data;
            priceLine.DataSource = Source;

            // Set the initial view of the data to the last trading day
            tradeAxis.WorldMax = (double)((DateTime)data.Rows[data.Rows.Count - 1][TIME_DATA_TAG]).Date.AddHours(16).Ticks;
            tradeAxis.WorldMin = tradeAxis.SparseWorldAdd((double)(new DateTime((long)tradeAxis.WorldMax)).Ticks, -(tradeAxis.EndTradingTime - tradeAxis.StartTradingTime).Ticks * 1.5);

            // Create a data table which acts as a reference point for each day
            DailyData = new DataTable();
            DailyData.Columns.Add("Time", typeof(DateTime));
            DailyData.Columns.Add("Price", typeof(float));
            DailyData.Columns.Add("Min", typeof(float));
            DailyData.Columns.Add("Max", typeof(float));
            float dayPrice = (float)data.Rows[0][PRICE_DATA_TAG];
            for(DateTime time = ((DateTime)data.Rows[0][TIME_DATA_TAG]).Date.AddHours(16); time <= ((DateTime)data.Rows[data.Rows.Count - 1][TIME_DATA_TAG]).Date.AddHours(16); time = time.AddDays(1))
            {
                int idx = GetTimeIndex(time);
                if(((DateTime)data.Rows[idx][TIME_DATA_TAG]).Date == time.Date)
                {
                    // Mark the time as the last trading time for the next day
                    DailyData.Rows.Add(time.Date.AddHours(9.5), dayPrice, 0, 0);
                    DailyData.Rows.Add(time.Date.AddHours(16), dayPrice, 0, 0);

                    // Set the ending price of this day as the reference point of the next day
                    dayPrice = (float)data.Rows[idx][PRICE_DATA_TAG];
                }
            }
            openLine.DataSource = DailyData;

            // Update the price text
            UpdatePriceText((DateTime)Source.Rows[Source.Rows.Count - 1][TIME_DATA_TAG]);

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
        private class HoverInteraction : NPlot.Interaction
        {
            public HoverInteraction(StockChart chart)
            {
                this.Chart = chart;
                Lines = new LineDrawer();
                //this.Chart.stockPricePlot.Add(Lines);
                //Lines.Canvas.Size = Chart.stockPricePlot.Canvas.Size;
                Lines.Canvas.Image = new System.Drawing.Bitmap(Chart.stockPricePlot.Canvas.Size.Width, Chart.stockPricePlot.Canvas.Size.Height);
                Lines.Canvas.BackColor = System.Drawing.Color.Transparent;
                Lines.Canvas.Size = Chart.stockPricePlot.Canvas.Size;
                Lines.Canvas.Enabled = false;
                this.Chart.stockPricePlot.Canvas.Controls.Add(Lines.Canvas);
            }

            private class LineDrawer
            {
                /// <summary>
                /// The pen used to draw the time line
                /// </summary>
                public System.Drawing.Pen TimePen = new System.Drawing.Pen(PRICE_COLOR_POSITIVE, 2.0f);

                /// <summary>
                /// The pen used to draw the price lines
                /// </summary>
                public System.Drawing.Pen PricePen = new System.Drawing.Pen(GUIDE_COLOR, 1.5f);

                /// <summary>
                /// The canvas used to draw additional overlay lines
                /// </summary>
                public System.Windows.Forms.PictureBox Canvas = new PictureBox();
            }

            /// <summary>
            /// The chart the interaction should update
            /// </summary>
            public StockChart Chart;

            /// <summary>
            /// The percentage from the current price at which the min and max guidelines should be drawn
            /// </summary>
            public float GuideLinePercentage = 1.025f;

            /// <summary>
            /// Indicates if the mouse is currently hovering over the chart
            /// </summary>
            public bool Hovering = false;

            /// <summary>
            /// Draws lines on top of the chart
            /// </summary>
            private LineDrawer Lines;

            /// <summary>
            /// Handles the mouse enter event
            /// </summary>
            /// <param name="ps">The plot surface</param>
            /// <returns>false</returns>
            public override bool DoMouseEnter(InteractivePlotSurface2D ps)
            {
                Hovering = true;
                Lines.Canvas.Visible = true;
                return false;
            }

            /// <summary>
            /// Handles the mouse leave event
            /// </summary>
            /// <param name="ps">The plot surface</param>
            /// <returns>false</returns>
            public override bool DoMouseLeave(InteractivePlotSurface2D ps)
            {
                Hovering = false;
                Lines.Canvas.Visible = false;
                Chart.UpdatePriceText((DateTime)Chart.Source.Rows[Chart.Source.Rows.Count - 1][TIME_DATA_TAG]);
                return false;
            }

            /// <summary>
            /// Handles a move move event
            /// </summary>
            /// <param name="X">The X mouse coordinate</param>
            /// <param name="Y">The Y mouse coordinate</param>
            /// <param name="keys">The mouse buttons that are pressed</param>
            /// <param name="ps">The plot surface the mouse is moving over</param>
            /// <returns></returns>
            public override bool DoMouseMove(int X, int Y, Modifier keys, InteractivePlotSurface2D ps)
            {
                DateTime time = new DateTime((long)Chart.stockPricePlot.PhysicalXAxis1Cache.PhysicalToWorld(new System.Drawing.Point(X, Y), false));
                int idx = Chart.GetTimeIndex(time);
                if(idx >= 0)
                {
                    float price = (float)Chart.Source.Rows[idx]["Price"];
                    Chart.UpdatePriceText(time);
                    using(System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(Lines.Canvas.Image))
                    {
                        PhysicalAxis xAxis = Chart.stockPricePlot.PhysicalXAxis1Cache;
                        PhysicalAxis yAxis = Chart.stockPricePlot.PhysicalYAxis1Cache;
                        g.Clear(System.Drawing.Color.Transparent);

                        // Draw the time line
                        System.Drawing.PointF timePoint = xAxis.WorldToPhysical(time.Ticks, true);
                        g.DrawLine(Lines.TimePen, timePoint.X, yAxis.PhysicalMin.Y, timePoint.X, yAxis.PhysicalMax.Y);

                        // Draw the guide lines
                        System.Drawing.PointF minPoint = yAxis.WorldToPhysical(price / GuideLinePercentage, true);
                        System.Drawing.PointF maxPoint = yAxis.WorldToPhysical(price * GuideLinePercentage, true);
                        g.DrawLine(Lines.PricePen, xAxis.PhysicalMin.X, minPoint.Y, xAxis.PhysicalMax.X, minPoint.Y);
                        g.DrawLine(Lines.PricePen, xAxis.PhysicalMin.X, maxPoint.Y, xAxis.PhysicalMax.X, maxPoint.Y);
                    }

                    // Use this as a hook to update the minimum and maximum displayed prices
                    Chart.UpdatePriceMinMax();

                    // Refresh the canvas to display the updated lines
                    Chart.stockPricePlot.Canvas.Refresh();
                }
                return false;
            }

            /// <summary>
            /// Handles a mouse scroll wheel event
            /// </summary>
            /// <param name="X">The X coordinate of the mouse</param>
            /// <param name="Y">The Y coordinate of the mouse</param>
            /// <param name="direction">The mouse wheel movement</param>
            /// <param name="keys">The mouse buttons that are pressed</param>
            /// <param name="ps">The plot surface the mouse is scrolling over</param>
            /// <returns></returns>
            public override bool DoMouseScroll(int X, int Y, int direction, Modifier keys, InteractivePlotSurface2D ps)
            {
                double percentChange = ((direction > 0) ? (1 / 1.2) : (1.2));
                DateTime anchor = new DateTime((long)Chart.stockPricePlot.PhysicalXAxis1Cache.PhysicalToWorld(new System.Drawing.Point(X, Y), false));
                int anchorIdx = Chart.GetTimeIndex(anchor);
                int minIdx = Chart.GetTimeIndex(new DateTime((long)Chart.stockPricePlot.XAxis1.WorldMin));
                int maxIdx = Chart.GetTimeIndex(new DateTime((long)Chart.stockPricePlot.XAxis1.WorldMax));
                minIdx = anchorIdx + (int)Math.Round((minIdx - anchorIdx) * percentChange);
                maxIdx = anchorIdx + (int)Math.Round((maxIdx - anchorIdx) * percentChange);
                minIdx = Math.Max(minIdx, 0);
                maxIdx = Math.Min(maxIdx, Chart.Source.Rows.Count - 1);
                Chart.stockPricePlot.XAxis1.WorldMin = (double)((DateTime)Chart.Source.Rows[minIdx][TIME_DATA_TAG]).Ticks;
                Chart.stockPricePlot.XAxis1.WorldMax = (double)((DateTime)Chart.Source.Rows[maxIdx][TIME_DATA_TAG]).Ticks;
                //Chart.stockPricePlot.XAxis1.WorldMax = (double)anchor.AddTicks((long)((new DateTime((long)Chart.stockPricePlot.XAxis1.WorldMax) - anchor).Ticks * percentChange)).Ticks;
                //Chart.stockPricePlot.XAxis1.WorldMin = (double)anchor.AddTicks((long)((new DateTime((long)Chart.stockPricePlot.XAxis1.WorldMin) - anchor).Ticks * percentChange)).Ticks;
                Chart.UpdatePriceMinMax();
                Chart.stockPricePlot.Refresh();

                return false;
            }
        }
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

        private NPlot.Swf.InteractivePlotSurface2D stockPricePlot;

        private NPlot.LinePlot priceLine;

        private NPlot.LinePlot openLine;

        private Label priceText;

        private Label changeText;
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
                } else if(checkTime > time)
                {
                    max = mid;
                } else
                {
                    min = mid;
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
        private void UpdatePriceMinMax()
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
                stockPricePlot.YAxis1.WorldMax = max * 1.05;
                stockPricePlot.YAxis1.WorldMin = min * .97;
            }
        }

        /// <summary>
        /// Updates the text describing the price at the given time
        /// </summary>
        /// <param name="time">The time to describe</param>
        private void UpdatePriceText(DateTime time)
        {
            int idx = GetTimeIndex(time);
            if(idx >= 0)
            {
                float price = (float)Source.Rows[idx]["Price"];
                priceText.Text = String.Format("{0:c}", price);
                float basePrice = (float)DailyData.Rows[GetTimeIndex(time, DailyData)][PRICE_DATA_TAG];
                float percentChange = -1.0f + (price / basePrice);
                changeText.Text = String.Format("{0}{1:c}({0}{2:P2}) {3:t} {3:MMM d} '{3:yy}", ((percentChange >= 0) ? "+" : ""), (price - basePrice), percentChange, time);
            }
        }
        #endregion
    }
}
