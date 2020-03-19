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

namespace RobinhoodDesktop
{
    public class DataChart<T> where T : struct, StockData
    {
        public DataChart(Dictionary<string, List<StockDataSet<T>>> dataSets, StockDataFile file, StockSession session)
        {
            this.DataSets = dataSets;
            this.File = file;
            this.Session = session;
            this.Start = File.Start;
            this.End = File.End;

            // Load the dummy data
            var dummySrc = DataSets.First().Value[0];
            dummySrc.Load(session);
            DataSets.First().Value[1].Load(session);    // Work-around so that the processing state is reset if this symbol is loaded again in the future
            DummyData = new StockDataSet<T>(dummySrc.Symbol, dummySrc.Start, dummySrc.File);
            DummyData.DataSet.Initialize(dummySrc.DataSet.InternalArray);

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
            Plot.PlotBackColor =  GuiStyle.DARK_GREY;
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
            TimeAxis.WorldMin = (double)(file.Start).Ticks;
            TimeAxis.WorldMax = (double)(file.End).Ticks;
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
                get { return Plot.Color; }
                set { Plot.Color = value; }
            }

            /// <summary>
            /// The actual plot being drawn to the chart
            /// </summary>
            public PlotContainer Plot;

            /// <summary>
            /// Callback to evaluate the expression on the specified data set
            /// </summary>
            public MethodDelegate GetValue;

            /// <summary>
            /// Indicates if the plot line is locked and should not be updated with the rest when a global change would take place (ex: setting the symbol)
            /// </summary>
            public bool Locked = false;

            public PlotLine(DataChart<T> source, string symbol, string expression)
            {
                this.Symbol = symbol;

                // Set the expression and create the corresponding plot line
                SetExpression(source, expression);
            }

            /// <summary>
            /// Sets a new expression for the plot line
            /// </summary>
            /// <param name="source">The data source</param>
            /// <param name="expression">The new expression to set</param>
            public void SetExpression(DataChart<T> source, string expression)
            {
                System.Drawing.Color c;
                if(Plot != null)
                {
                    c = this.Color;
                    Plot.Remove(source);
                    Plot = null;
                } else
                {
                    c = source.PlotLineColors.Dequeue();
                    source.PlotLineColors.Enqueue(c);   // Add the color to the end of the list so that it can be re-used if needed
                }

                this.Expression = expression;
                this.GetValue = getExpressionEvaluator(expression);
                Generate(source);

                PlotLineCreator creator;
                if(!LineCreators.TryGetValue(source.getExpressionType(Expression, GetValue), out creator))
                {
                    creator = DefaultCreator;
                }
                bool preserveAxisPosition = (source.Plot.XAxis1 != null);
                double worldMin = preserveAxisPosition ? source.Plot.XAxis1.WorldMin : 0;
                double worldMax = preserveAxisPosition ? source.Plot.XAxis1.WorldMax : 0;
                Plot = creator(source, this);
                this.Color = c;
                if(preserveAxisPosition)
                {
                    source.Plot.XAxis1.WorldMin = worldMin;
                    source.Plot.XAxis1.WorldMax = worldMax;
                }
            }

            /// <summary>
            /// Generates the data to use in the plot line
            /// </summary>
            /// <param name="source"></param>
            public void Generate(DataChart<T> source)
            {
                this.Data = new DataTable();
                Data.Columns.Add(source.XAxis, source.getExpressionType(source.XAxis, source.XAxisGetValue));
                Data.Columns.Add(Expression, source.getExpressionType(Expression, GetValue));

                List<string> symbols = source.GetSymbolList(Symbol);
                foreach(var s in symbols)
                {
                    List<StockDataSet<T>> sources;
                    if(!source.DataSets.TryGetValue(s, out sources)) return;

                    // Create a table of each data point in the specified range
                    for(int i = 0; i < sources.Count; i++)
                    {
                        if(sources[i].Start >= source.Start)
                        {
                            sources[i].Load(source.Session);
                            for(int j = 0; j < sources[i].Count; j++)
                            {
                                if(sources[i].Time(j) <= source.End)
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
                if(Plot != null)
                {
                    Plot.SetData(Data);
                }
            }

            /// <summary>
            /// Determines the text representation of the value at the specified index
            /// </summary>
            /// <param name="dataIndex">The requested data index</param>
            /// <returns>The string representation of the value</returns>
            public string PrintValue(int dataIndex)
            {
                string val = "";
                if(Data.Columns.Contains(Expression))
                {
                    val = NPlot.Utils.ToDouble(Data.Rows[dataIndex][Expression]).ToString();
                }
                return val;
            }

            #region Plot Creators
            public abstract class PlotContainer
            {
                protected IPlot plotInterface;
                public Axis PlotYAxis;
                protected string Expression;
                protected DataChart<T> Chart;
                public virtual System.Drawing.Color Color
                {
                    get { return System.Drawing.Color.White; }
                    set { }
                }

                public PlotContainer(DataChart<T> chart)
                {
                    this.Chart = chart;
                }

                private PlotContainer()
                {

                }

                public virtual void UpdateDataMinMax(DataTable table)
                {
                    if(Chart.Plot.XAxis1 == null) return;
                    const double PWR = 2;
                    double avg = 0;
                    double stdDev = 0;
                    double min = double.MaxValue;
                    double max = double.MinValue;
                    double dev = 0;
                    int start = Chart.GetDataIndex(Chart.Plot.XAxis1.WorldMin);
                    int end = Chart.GetDataIndex(Chart.Plot.XAxis1.WorldMax);
                    int step = Math.Max((end - start) / 4096, 1);
                    int numSteps = (((end - start) + (step - 1)) / step);
                    int count = 0;
                    if(numSteps > 0)
                    {
                        // Calculate the average first
                        for(int i = start; i <= end; i += step)
                        {
                            double val = NPlot.Utils.ToDouble(table.Rows[i][Expression]);
                            if(double.IsNaN(val)) continue;
                            avg += (val - avg) / ++count;
                            dev += Math.Pow((val - avg), 2);
                            min = (val < min) ? val : min;
                            max = (val > max) ? val : max;
                        }
                        dev = Math.Sqrt(dev / count);
                        PlotYAxis.WorldMin = Math.Max(avg - ((avg - min) * 1.1), avg - (dev * 4));
                        PlotYAxis.WorldMax = Math.Min(avg + ((max - avg) * 1.1), avg + (dev * 4));
                    }
                }
                public abstract void SetData(DataTable table);
                public abstract void Remove(DataChart<T> source);
                public virtual NPlot.Axis SuggestXAxis()
                {
                    return plotInterface.SuggestXAxis();
                }
            }

            /// <summary>
            /// Creates an appropriate type of 
            /// </summary>
            /// <returns>The created plot line</returns>
            protected delegate PlotContainer PlotLineCreator(DataChart<T> source, PlotLine line);

            /// <summary>
            /// Creates an appropriate type of 
            /// </summary>
            /// <returns>The created plot line</returns>
            protected static PlotContainer DefaultCreator(DataChart<T> source, PlotLine line)
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

                public LinePlotContainer(DataChart<T> source, PlotLine line) : base(source)
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

                public override void Remove(DataChart<T> source)
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

                public PointPlotContainer(DataChart<T> source, PlotLine line) : base(source)
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

                public override void Remove(DataChart<T> source)
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
        public Dictionary<string, List<StockDataSet<T>>> DataSets;

        /// <summary>
        /// The file data is being pulled from
        /// </summary>
        public StockDataFile File;

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
        public MethodDelegate XAxisGetValue;

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
        protected List<PlotLine> Lines = new List<PlotLine>();

        /// <summary>
        /// Stores a list of public fields in the data set type
        /// </summary>
        protected static List<string> FieldNames = typeof(T).GetFields().ToList().ConvertAll((f) => { return f.Name; });

        /// <summary>
        /// A dummy data set that can be used for evaluating expressions
        /// </summary>
        protected StockDataSet<T> DummyData;

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
        #endregion

        /// <summary>
        /// Returns the available fields in the data file
        /// </summary>
        /// <returns>The list of available fields</returns>
        public static List<string> GetFields()
        {
            return FieldNames;
        }

        /// <summary>
        /// Sets the field used to determine the X axis
        /// </summary>
        /// <param name="expression">The name of the parameter to comprise the X axis</param>
        public void SetXAxis(string expression)
        {
            this.XAxis = expression;
            this.XAxisGetValue = getExpressionEvaluator(expression);
            foreach(var l in Lines)
            {
                l.Plot.Remove(this);
            }
            Plot.XAxis1 = null;
            Plot.YAxis1 = null;

            // Re-generate all of the data
            foreach(var l in Lines)
            {
                l.SetExpression(this, l.Expression);
                l.Generate(this);
            }

            // Select the proper X axis and refresh the plot
            if(XAxis.Equals("Time")) Plot.XAxis1 = TimeAxis;
            Refresh();
        }

        /// <summary>
        /// Adds a new set of points to the plot
        /// </summary>
        /// <param name="expression">The name of the paramter to determine the Y axis points</param>
        public PlotLine AddPlot(string symbol, string expression)
        {
            PlotLine newPlot = null;

            // Avoid duplicates
            if(Lines.Where((l) => { return l.Expression.Equals(expression); }).Count() == 0)
            {
                newPlot = new PlotLine(this, symbol, expression);
                Lines.Add(newPlot);
                Refresh();
            }

            return newPlot;
        }

        /// <summary>
        /// Re-draws the plot, pulling any any new data or axis changes
        /// </summary>
        public void Refresh()
        {
            foreach(var l in Lines)
            {
                l.Generate(this);
            }

            Plot.Refresh();
        }

        /// <summary>
        /// Sets the time range to pull data from
        /// </summary>
        /// <param name="start">The start time</param>
        /// <param name="end">The end time</param>
        public void SetDataRange(DateTime start, DateTime end)
        {
            Start = (start >= File.Start) ? start : File.Start;
            End = (end <= File.End) ? end : File.End;
        }

        /// <summary>
        /// Updates the minimum and maximum shown values for each line
        /// </summary>
        public void UpdateMinMax()
        {
            foreach(var l in Lines)
            {
                l.Plot.UpdateDataMinMax(l.Data);
            }
        }

        /// <summary>
        /// Sets the stock symbol for all plot lines
        /// </summary>
        /// <param name="symbol">The symbol to set</param>
        protected void SetPlotLineSymbol(string symbol)
        {
            foreach(var l in Lines)
            {
                if(!l.Locked) l.Symbol = symbol;
            }
        }

        /// <summary>
        /// Returns a list of symbols based on the expression
        /// </summary>
        /// <param name="symbolExpression">The expression defining the symbols</param>
        /// <returns>A list of stock symbols</returns>
        public List<string> GetSymbolList(string symbolExpression)
        {
            List<string> symbols = symbolExpression.Split(',').ToList();
            for(int i = 0; i < symbols.Count; i++)
            {
                bool remove = symbols[i].Contains('!');
                if(symbols[i].Contains('-'))
                {
                    var exp = symbols[i].Split('-');
                    symbols.RemoveAt(i);
                    i--;

                    var rangeSym = DataSets.Keys.Where((s) => { return (s.CompareTo(exp[0]) >= 0) && (s.CompareTo(exp[1]) <= 0); });
                    if(remove)
                    {
                        foreach(var s in rangeSym)
                        {
                            var sIdx = symbols.IndexOf(s);
                            symbols.RemoveAt(sIdx);
                            if(sIdx <= i) i--;
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
                    if(remove)
                    {
                        var sIdx = symbols.IndexOf(symbols[i].Replace("!", ""));
                        symbols.RemoveAt(sIdx);
                        if(sIdx <= i) i--;
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
            if((Lines.Count > 0) && (Lines[0].Data != null))
            {
                var src = Lines[0].Data;
                int min = 0;
                int max = src.Rows.Count;

                while(max > min)
                {
                    int mid = (max + min) / 2;
                    double check = NPlot.Utils.ToDouble(src.Rows[mid][XAxis]);
                    if((min + 1) >= max)
                    {
                        idx = min;
                        break;
                    }
                    else if(check > val)
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
        /// Compiles a script to evaluate the specified expression
        /// </summary>
        /// <returns></returns>
        protected static MethodDelegate getExpressionEvaluator(string expression)
        {
            MethodDelegate accessor = null;

            // Check for the special case of requesting the time
            if(expression.Equals("Time"))
            {
                accessor = new MethodDelegate((object[] p) =>
                {
                    StockDataSet<T> data = (StockDataSet<T>)p[0];
                    int index = (int)p[1];
                    return data.Time(index);
                });
            }
            else
            {
                // Order the list based on the lame length
                var fields = GetFields().ToList();
                fields.Sort((f1, f2) => { return f2.Length.CompareTo(f1.Length); });

                // First replace the fields with an index to prevent names within a name from getting messed up
                string src = expression;
                for(int i = 0; i < fields.Count; i++)
                {
                    src = src.Replace(fields[i], string.Format("<={0}>", i));
                }

                // Next pre-pend the data set to the field names
                for(int i = 0; i < fields.Count; i++)
                {
                    src = src.Replace(string.Format("<={0}>", i), string.Format("data[updateIndex].{0}", fields[i]));
                }

                // Build the expression into an accessor function
                src = "namespace RobinhoodDesktop.Script { public class ExpressionAccessor{ public static object GetValue(StockDataSet<" + typeof(T).Name + "> data, int updateIndex) { return " + src + ";} } }";
                string assemblyFile = System.Reflection.Assembly.GetAssembly(typeof(T)).Location;
                var script = CSScript.LoadCode(src, assemblyFile);
                accessor = script.GetStaticMethod("RobinhoodDesktop.Script.ExpressionAccessor.GetValue", new StockDataSet<T>("", DateTime.Now, null), 0);
            }
            return accessor;
        }

        /// <summary>
        /// Returns the type of the expression
        /// </summary>
        /// <param name="expression">The expression to get the type of</param>
        /// <param name="getValue">Callback used to get the expression value</param>
        /// <returns>The expression type</returns>
        protected Type getExpressionType(string expression, MethodDelegate getValue)
        {
            Type expType = typeof(double);
            if(expression.Equals("Time"))
            {
                expType = typeof(DateTime);
            }
            else if(getValue != null)
            {
                object val = getValue(DummyData, 0);
                expType = val.GetType();
            }
            return expType;
        }
    }
}
