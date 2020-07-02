using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using NPlot;
using System.Windows.Forms;

using RobinhoodDesktop.Script;
using CSScriptLibrary;
using System.Threading;

namespace RobinhoodDesktop
{
    public class DataChart
    {
        public DataChart(Dictionary<string, List<StockDataInterface>> dataSets, StockSession session)
        {
            this.DataSets = session.Data;
            this.Session = session;
            this.Start = session.SinkFile.Start;
            this.End = session.SinkFile.End;

            // Create the surface used to draw the plot
            Plot = new NPlot.Swf.InteractivePlotSurface2D();
            Plot.SurfacePadding = 0;
            Plot.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Plot.ShowCoordinates = false;
            Plot.XAxis1 = new Axis();
            Plot.YAxis1 = new Axis();
            Plot.XAxis1.HideTickText = true;
            //stockPricePlot.XAxis1.NumberFormat = " h";
            Plot.XAxis1.AxisColor = System.Drawing.Color.Transparent;
            Plot.YAxis1.HideTickText = true;
            Plot.YAxis1.Color = System.Drawing.Color.Transparent;
            Plot.PlotBackColor = GuiStyle.DARK_GREY;
            Plot.Canvas.HandleCreated += (sender, e) =>
            {
                Plot.Canvas.BackColor = Plot.Canvas.Parent.BackColor;
                //SetChartData(Source);
            };

            // Set the time axis as default
            this.XAxis = "Time";
            this.XAxisGetValue = getExpressionEvaluator(this.XAxis);
            this.TimeAxis = new TradingDateTimeAxis(Plot.XAxis1);
            TimeAxis.StartTradingTime = new TimeSpan(9, 30, 0);
            TimeAxis.EndTradingTime = new TimeSpan(16, 0, 0);
            TimeAxis.WorldMin = (double)(Start).Ticks;
            TimeAxis.WorldMax = (double)(End).Ticks;
            Plot.XAxis1 = TimeAxis;

            Plot.Refresh();
        }

        #region Types
        public class PlotLine
        {
            /// <summary>
            /// The stock symbol being plotted
            /// </summary>
            public string Symbol;

            /// <summary>
            /// The expression being plotted
            /// </summary>
            public string Expression;

            /// <summary>
            /// The vertical scale to apply to the plot
            /// </summary>
            public float Scale;

            /// <summary>
            /// The data points being plotted
            /// </summary>
            public DataTable Data;

            /// <summary>
            /// The color of the plot line
            /// </summary>
            public System.Drawing.Color Color
            {
                get { return plotColor; }
                set { 
                    if(Plot != null) Plot.Color = value;
                    plotColor = value;
                }
            }
            private System.Drawing.Color plotColor = System.Drawing.Color.Black;

            /// <summary>
            /// The actual plot being drawn to the chart
            /// </summary>
            public PlotContainer Plot;

            /// <summary>
            /// Callback to evaluate the expression on the specified data set
            /// </summary>
            public Func<StockDataInterface, int, object> GetValue;

            /// <summary>
            /// Indicates if the plot line is locked and should not be updated with the rest when a global change would take place (ex: setting the symbol)
            /// </summary>
            public bool Locked = false;

            /// <summary>
            /// Mutex used to control access to the data
            /// </summary>
            public Mutex DataMutex = new Mutex(false);

            /// <summary>
            /// Delegate type that notifies another object when the line's expression has changed
            /// </summary>
            /// <param name="line">The line that changed</param>
            public delegate void ExpressionChangedCallback(PlotLine line);

            /// <summary>
            /// Callback when the expression changes
            /// </summary>
            public ExpressionChangedCallback ExpressionChanged;

            public PlotLine(DataChart source, string symbol, string expression)
            {
                this.Symbol = symbol;

                // Get a color for the plot line
                Color = source.PlotLineColors.Dequeue();
                source.PlotLineColors.Enqueue(Color);   // Add the color to the end of the list so that it can be re-used if needed

                // Set the expression and create the corresponding plot line
                SetExpression(source, expression);
            }

            /// <summary>
            /// Sets a new expression for the plot line
            /// </summary>
            /// <param name="source">The data source</param>
            /// <param name="expression">The new expression to set</param>
            public void SetExpression(DataChart source, string expression)
            {
                this.Expression = expression;
                if (!string.IsNullOrEmpty(expression))
                {
                    // Attempt to generate the data table for the given expression
                    this.GetValue = source.getExpressionEvaluator(expression);
                    Generate(source);
                }
                if (ExpressionChanged != null) ExpressionChanged(this);
            }

            /// <summary>
            /// Creates a plot line instance (if necessary)
            /// </summary>
            /// <param name="source">The data chart the line is a part of</param>
            private void CreatePlotLine(DataChart source)
            {
                // Ensure the data has been generated
                if (Data.Columns.Contains(Expression))
                {
                    // Remove any previous plot
                    if (Plot != null)
                    {
                        Plot.Remove(source);
                        Plot = null;
                    }

                    // Create a new plot
                    PlotLineCreator creator;
                    Type expressionType = Data.Columns[Expression].DataType;
                    if (!LineCreators.TryGetValue(expressionType, out creator))
                    {
                        creator = DefaultCreator;
                    }
                    bool preserveAxisPosition = (source.Plot.XAxis1 != null);
                    double worldMin = preserveAxisPosition ? source.Plot.XAxis1.WorldMin : 0;
                    double worldMax = preserveAxisPosition ? source.Plot.XAxis1.WorldMax : 0;
                    Plot = creator(source, this);
                    Plot.SetData(Data);
                    Plot.Color = Color;
                    if (preserveAxisPosition)
                    {
                        source.Plot.XAxis1.WorldMin = worldMin;
                        source.Plot.XAxis1.WorldMax = worldMax;
                    }
                }
            }

            /// <summary>
            /// Generates the data to use in the plot line
            /// </summary>
            /// <param name="source"></param>
            public void Generate(DataChart source)
            {
                DataMutex.WaitOne();
                List<string> symbols = source.GetSymbolList(Symbol);
                this.Data = new DataTable();

                foreach (var s in symbols)
                {
                    List<StockDataInterface> sources;
                    if (!source.DataSets.TryGetValue(s, out sources) || (GetValue == null)) break;

                    // Load the sources first (need to load to the end before accessing the data since loading later sources could backfill data) */
                    for (int i = 0; i < sources.Count; i++) sources[i].Load(source.Session);

                    // Ensure columns are added for the data types
                    if (Data.Columns.Count < 2)
                    {
                        Data.Columns.Add(source.XAxis, source.XAxisGetValue(sources[0], 0).GetType());
                        Data.Columns.Add(Expression, GetValue(sources[0], 0).GetType());
                    }

                    // Create a table of each data point in the specified range
                    for (int i = 0; i < sources.Count; i++)
                    {
                        string symbol;
                        DateTime start;
                        TimeSpan interval;
                        sources[i].GetInfo(out symbol, out start, out interval);
                        if (start >= source.Start)
                        {
                            // Add the data set to the table
                            for (int j = 0; j < sources[i].GetCount(); j++)
                            {
                                if (start.AddSeconds(interval.TotalSeconds * j) <= source.End)
                                {
                                    // Add the point to the table
                                    Data.Rows.Add(source.XAxisGetValue(sources[i], j), GetValue(sources[i], j));
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

                // Update the line plot data
                CreatePlotLine(source);
                if (Plot != null)
                {
                    Plot.SetData(Data);
                }
                DataMutex.ReleaseMutex();
            }

            /// <summary>
            /// Determines the text representation of the value at the specified index
            /// </summary>
            /// <param name="dataIndex">The requested data index</param>
            /// <returns>The string representation of the value</returns>
            public string PrintValue(int dataIndex)
            {
                string val = "";
                DataMutex.WaitOne();
                if ((Data != null) && Data.Columns.Contains(Expression) && (Data.Rows.Count > 0))
                {
                    val = NPlot.Utils.ToDouble(Data.Rows[dataIndex][Expression]).ToString();
                }
                DataMutex.ReleaseMutex();
                return val;
            }

            #region Plot Creators
            public abstract class PlotContainer
            {
                protected IPlot plotInterface;
                public Axis PlotYAxis;
                public string Expression;
                protected DataChart Chart;
                public virtual System.Drawing.Color Color
                {
                    get { return System.Drawing.Color.White; }
                    set { }
                }

                public PlotContainer(DataChart chart)
                {
                    this.Chart = chart;
                }

                private PlotContainer()
                {

                }

                public virtual void UpdateDataMinMax(DataTable table)
                {
                    if (Chart.Plot.XAxis1 == null) return;
                    const double PWR = 2;
                    double avg = 0;
                    double stdDev = 0;
                    double min = double.MaxValue;
                    double max = double.MinValue;
                    double dev = 0;
                    int start = Chart.GetDataIndex(Chart.Plot.XAxis1.WorldMin);
                    int end = Chart.GetDataIndex(Chart.Plot.XAxis1.WorldMax);
                    int step = Math.Max((end - start) / 8192, 1);
                    int numSteps = (((end - start) + (step - 1)) / step);
                    int count = 0;
                    if (numSteps > 0)
                    {
                        // Calculate the average, std dev, min, and max
                        for (int i = start; (table.Rows.Count > i) && (i <= end); i += step)
                        {
                            double val = NPlot.Utils.ToDouble(table.Rows[i][Expression]);
                            if (double.IsNaN(val)) continue;
                            avg += (val - avg) / ++count;
                            dev += Math.Pow((val - avg), 2);
                            min = (val < min) ? val : min;
                            max = (val > max) ? val : max;
                        }

                        // Set the chart to show the entire data range
                        PlotYAxis.WorldMin = avg - ((avg - min) * 1.1);
                        PlotYAxis.WorldMax = avg + ((max - avg) * 1.1);

                        // When plotting time, hide some of the outliers
                        if (Chart.XAxis.Equals("Time"))
                        {
                            dev = Math.Sqrt(dev / count);
                            PlotYAxis.WorldMin = Math.Max(PlotYAxis.WorldMin, avg - (dev * 6));
                            PlotYAxis.WorldMax = Math.Min(PlotYAxis.WorldMax, avg + (dev * 6));
                        }
                    }
                }
                public abstract void SetData(DataTable table);
                public abstract void Remove(DataChart source);
                public virtual NPlot.Axis SuggestXAxis()
                {
                    return plotInterface.SuggestXAxis();
                }
            }

            /// <summary>
            /// Creates an appropriate type of 
            /// </summary>
            /// <returns>The created plot line</returns>
            protected delegate PlotContainer PlotLineCreator(DataChart source, PlotLine line);

            /// <summary>
            /// Creates an appropriate type of 
            /// </summary>
            /// <returns>The created plot line</returns>
            protected static PlotContainer DefaultCreator(DataChart source, PlotLine line)
            {
                return (source.XAxis.Equals("Time") ? (PlotContainer)new LinePlotContainer(source, line) : new PointPlotContainer(source, line));
            }

            /// <summary>
            /// Relates a type to the appropriate plot format
            /// </summary>
            protected Dictionary<Type, PlotLineCreator> LineCreators = new Dictionary<Type, PlotLineCreator>()
            {
                { typeof(float), DefaultCreator },
                { typeof(int), DefaultCreator },
                { typeof(TimeSpan), DefaultCreator },
            };

            protected class LinePlotContainer : PlotContainer
            {
                public LinePlot Plot;
                public override System.Drawing.Color Color
                {
                    get { return Plot.Color; }
                    set { Plot.Color = value; }
                }

                public LinePlotContainer(DataChart source, PlotLine line) : base(source)
                {
                    Plot = new LinePlot();
                    Plot.DataSource = line.Data;
                    Plot.AbscissaData = source.XAxis;
                    Plot.OrdinateData = line.Expression;

                    this.Expression = line.Expression;
                    plotInterface = Plot;
                    PlotYAxis = new Axis();
                    source.Plot.Add(Plot, XAxisPosition.Bottom, YAxisPosition.Left, 0, null, PlotYAxis);
                }

                public override void SetData(DataTable table)
                {
                    Plot.DataSource = table;
                    base.UpdateDataMinMax(table);
                }

                public override void Remove(DataChart source)
                {
                    source.Plot.Remove(Plot, false);
                }
            }

            protected class PointPlotContainer : PlotContainer
            {
                public NPlot.PointPlot Plot;
                public override System.Drawing.Color Color
                {
                    get { return Plot.Marker.Color; }
                    set { Plot.Marker.Color = value; }
                }

                public PointPlotContainer(DataChart source, PlotLine line) : base(source)
                {
                    Plot = new NPlot.PointPlot();
                    Plot.DataSource = line.Data;
                    Plot.AbscissaData = source.XAxis;
                    Plot.OrdinateData = line.Expression;
                    Plot.Marker.Type = Marker.MarkerType.Circle;

                    this.Expression = line.Expression;
                    plotInterface = Plot;
                    PlotYAxis = new Axis();
                    source.Plot.Add(Plot, XAxisPosition.Bottom, YAxisPosition.Left, 0, null, PlotYAxis);
                }

                public override void SetData(DataTable table)
                {
                    Plot.DataSource = table;
                    base.UpdateDataMinMax(table);
                }

                public override void Remove(DataChart source)
                {
                    source.Plot.Remove(Plot, false);
                }
            }
            #endregion
        }
        #endregion

        #region Variables
        /// <summary>
        /// The data sets associated with the chart
        /// </summary>
        public Dictionary<string, List<StockDataInterface>> DataSets;

        /// <summary>
        /// The session the chart is part of
        /// </summary>
        public StockSession Session;

        /// <summary>
        /// The starting time to pull data from 
        /// </summary>
        public DateTime Start;

        /// <summary>
        /// The end time to pull data from
        /// </summary>
        public DateTime End;

        /// <summary>
        /// The parameter determining the X axis
        /// </summary>
        public string XAxis;

        /// <summary>
        /// Callback to get the X axis value
        /// </summary>
        public Func<StockDataInterface, int, object> XAxisGetValue;

        /// <summary>
        /// The plot surface used to display the chart
        /// </summary>
        protected NPlot.Swf.InteractivePlotSurface2D Plot;

        /// <summary>
        /// The axis used when X is the DateTime
        /// </summary>
        protected TradingDateTimeAxis TimeAxis;

        /// <summary>
        /// The lines being plotted
        /// </summary>
        public List<PlotLine> Lines = new List<PlotLine>();

        /// <summary>
        /// The colors supported for the plot lines
        /// </summary>
        protected Queue<System.Drawing.Color> PlotLineColors = new Queue<System.Drawing.Color>(new List<System.Drawing.Color>()
        {
            GuiStyle.PRICE_COLOR_POSITIVE,
            GuiStyle.NOTIFICATION_COLOR,
            GuiStyle.TEXT_COLOR,
            GuiStyle.PRICE_COLOR_NEGATIVE,
            System.Drawing.Color.Violet,
            System.Drawing.Color.DeepSkyBlue
        });

        /// <summary>
        /// Accesses the canvas object for the chart
        /// </summary>
        public System.Windows.Forms.Control Canvas
        {
            get { return Plot.Canvas; }
        }

        /// <summary>
        /// Callback executed when a main component of the plot is changed
        /// </summary>
        public Action ChartChanged;
        #endregion

        /// <summary>
        /// Sets the field used to determine the X axis
        /// </summary>
        /// <param name="expression">The name of the parameter to comprise the X axis</param>
        public void SetXAxis(string expression)
        {
            this.XAxis = expression;
            this.XAxisGetValue = getExpressionEvaluator(expression);
            foreach (var l in Lines)
            {
                if(l.Plot != null) l.Plot.Remove(this);
            }
            Plot.XAxis1 = null;
            Plot.YAxis1 = null;

            // Re-generate all of the data
            foreach (var l in Lines)
            {
                l.Generate(this);
            }

            // Select the proper X axis and refresh the plot
            if (XAxis.Equals("Time")) Plot.XAxis1 = TimeAxis;
            UpdateMinMax();
            Refresh();
            if (ChartChanged != null) ChartChanged();
        }

        /// <summary>
        /// Adds a new set of points to the plot
        /// </summary>
        /// <param name="expression">The name of the paramter to determine the Y axis points</param>
        public PlotLine AddPlot(string symbol, string expression)
        {
            PlotLine newPlot = null;

            // Avoid duplicates
            if (Lines.Where((l) => { return l.Expression.Equals(expression); }).Count() == 0)
            {
                newPlot = new PlotLine(this, symbol, expression);
                Lines.Add(newPlot);
            }

            return newPlot;
        }

        /// <summary>
        /// Re-draws the plot, pulling any any new data or axis changes
        /// </summary>
        public void Refresh()
        {
            if (Canvas.IsHandleCreated)
            {
                Canvas.BeginInvoke((Action)(() =>
                {
                    Plot.Refresh();
                }));
            }
        }

        /// <summary>
        /// Can be called when the data loaded into the session has changed
        /// </summary>
        public void ReloadData()
        {
            this.DataSets = Session.Data;
            if(Lines.Count > 0)
            {
                string prevSymbols = Lines[0].Symbol;

                // Re-load the line and XAxis accessor callbacks
                foreach(var l in Lines)
                {
                    l.Symbol = "";
                    l.SetExpression(this, l.Expression);
                }
                this.SetXAxis(XAxis);

                // Restore the symbols to re-load the data
                SetPlotLineSymbol(prevSymbols);
            }
        }

        /// <summary>
        /// Sets the time range to pull data from
        /// </summary>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        public void SetDataRange(DateTime start, DateTime end)
        {
            Start = (start >= Session.SinkFile.Start) ? start : Session.SinkFile.Start;
            End = (end <= Session.SinkFile.End) ? end : Session.SinkFile.End;
        }

        /// <summary>
        /// Updates the minimum and maximum shown values for each line
        /// </summary>
        public void UpdateMinMax()
        {
            if (Lines[0].Plot == null) return;
            foreach (var l in Lines)
            {
                if (l.Plot != null)
                {
                    l.DataMutex.WaitOne();
                    l.Plot.UpdateDataMinMax(l.Data);
                    l.DataMutex.ReleaseMutex();
                }
            }

            List<Tuple<List<PlotLine>, double, double>> groups = new List<Tuple<List<PlotLine>, double, double>>
            {
                { new Tuple<List<PlotLine>, double, double>(new List<PlotLine>() { Lines[0]}, Lines[0].Plot.PlotYAxis.WorldMin, Lines[0].Plot.PlotYAxis.WorldMax) }
            };
            for (int i = 1; i < Lines.Count; i++)
            {
                for (int j = 0; (Lines[i].Plot != null) && (j < groups.Count); j++)
                {
                    double groupAvg = (groups[j].Item2 + groups[j].Item3) / 2;
                    double groupRange = (groups[j].Item3 - groups[j].Item2) / 2;
                    double tolerance = 2.0f;
                    if (((Lines[i].Plot.PlotYAxis.WorldMin > (groupAvg - (groupRange * tolerance))) &&
                         (Lines[i].Plot.PlotYAxis.WorldMax < (groupAvg + (groupRange * tolerance)))) ||
                        (Lines[i].Locked && groups[j].Item1[0].Locked))
                    {
                        var l = groups[j].Item1;
                        l.Add(Lines[i]);
                        groups[j] = new Tuple<List<PlotLine>, double, double>(l,
                            Math.Min(Lines[i].Plot.PlotYAxis.WorldMin, groups[j].Item2),
                            Math.Max(Lines[i].Plot.PlotYAxis.WorldMax, groups[j].Item3));
                        for (int k = 0; k < l.Count; k++)
                        {
                            l[k].Plot.PlotYAxis.WorldMin = groups[j].Item2;
                            l[k].Plot.PlotYAxis.WorldMax = groups[j].Item3;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the stock symbol for all plot lines
        /// </summary>
        /// <param name="symbol">The symbol to set</param>
        public void SetPlotLineSymbol(string symbol)
        {
            foreach (var l in Lines)
            {
                if (!l.Locked) l.Symbol = symbol;
            }
            this.SetXAxis(XAxis);
        }

        /// <summary>
        /// Returns a list of symbols based on the expression
        /// </summary>
        /// <param name="symbolExpression">The expression defining the symbols</param>
        /// <returns>A list of stock symbols</returns>
        public List<string> GetSymbolList(string symbolExpression)
        {
            List<string> symbols = symbolExpression.Split(',').ToList();
            for (int i = 0; i < symbols.Count; i++)
            {
                bool remove = symbols[i].Contains('!');
                if (symbols[i].Contains('-'))
                {
                    var exp = symbols[i].Split('-');
                    symbols.RemoveAt(i);
                    i--;

                    var rangeSym = DataSets.Keys.Where((s) => { return (s.CompareTo(exp[0]) >= 0) && (s.CompareTo(exp[1]) <= 0); });
                    if (remove)
                    {
                        foreach (var s in rangeSym)
                        {
                            var sIdx = symbols.IndexOf(s);
                            symbols.RemoveAt(sIdx);
                            if (sIdx <= i) i--;
                        }
                    }
                    else
                    {
                        symbols.AddRange(rangeSym);
                    }
                    continue;
                }
                else
                {
                    if (remove)
                    {
                        var sIdx = symbols.IndexOf(symbols[i].Replace("!", ""));
                        symbols.RemoveAt(sIdx);
                        if (sIdx <= i) i--;
                        symbols.RemoveAt(i);
                        i--;
                    }
                }
            }

            return symbols;
        }

        /// <summary>
        /// Returns the index corresponding to the given value/time (or -1 if no match is found)
        /// </summary>
        /// <param name="val">The value to get the point for</param>
        /// <returns>The index of the point in the X axis of the source data</returns>
        public int GetDataIndex(double val)
        {
            int idx = -1;
            if ((Lines.Count > 0) && (Lines[0].Data != null) && Lines[0].DataMutex.WaitOne())
            {
                var src = Lines[0].Data;
                int min = 0;
                int max = src.Rows.Count;

                while (max > min)
                {
                    int mid = (max + min) / 2;
                    double check = NPlot.Utils.ToDouble(src.Rows[mid][XAxis]);
                    if ((min + 1) >= max)
                    {
                        idx = min;
                        break;
                    }
                    else if (check > val)
                    {
                        max = mid;
                    }
                    else
                    {
                        min = mid;
                    }
                }
                Lines[0].DataMutex.ReleaseMutex();
            }

            return idx;
        }

        /// <summary>
        /// Compiles a script to evaluate the specified expression
        /// </summary>
        /// <param name="expression">The expression to get a value from the dataset</param>
        /// <returns>The delegate used to get the desired value from a dataset</returns>
        protected Func<StockDataInterface, int, object> getExpressionEvaluator(string expression)
        {
            return DataSets.First().Value.First().GetExpressionEvaluator(expression);
        }

        /// <summary>
        /// Compiles a script to evaluate the specified expression
        /// </summary>
        /// <returns></returns>
        protected Type getDataType()
        {
            return DataSets.First().Value.First().GetDataType();
        }
    }
}
