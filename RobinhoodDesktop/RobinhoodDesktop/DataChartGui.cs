using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

using RobinhoodDesktop.Script;
using NPlot;

namespace RobinhoodDesktop
{
    public class DataChartGui : DataChart
    {
        public DataChartGui(Dictionary<string, List<StockDataInterface>> dataSets, StockSession session) : base(dataSets, session)
        {
            GuiPanel = new Panel();
            GuiPanel.Size = new System.Drawing.Size(600, 300);
            GuiPanel.AutoSize = false;
            GuiPanel.BackColor = GuiStyle.BACKGROUND_COLOR;
            GuiPanel.BorderStyle = BorderStyle.FixedSingle;
            EventHandler resizeHandler = (sender, e) =>
            {
                GuiPanel.Size = new System.Drawing.Size(GuiPanel.Parent.Width - 50, GuiPanel.Parent.Height - GuiPanel.Top - 50);
                SymbolTextbox.Location = new System.Drawing.Point((GuiPanel.Width / 2) - (SymbolTextbox.Width / 2), 5);
                int intervalBtnPos = SymbolTextbox.Right + 5;
                foreach(var pair in IntervalButtons)
                {
                    pair.Item1.Location = new Point(intervalBtnPos, SymbolTextbox.Top);
                    intervalBtnPos = pair.Item1.Right + 5;
                }
                ReloadButton.Location = new System.Drawing.Point(GuiPanel.Width - (ReloadButton.Width / 2) - 5, 5);
                XAxisTextbox.Location = new System.Drawing.Point((GuiPanel.Width / 2) - (SymbolTextbox.Width / 2), GuiPanel.Height - XAxisTextbox.Height - 10);
            };
            GuiPanel.ParentChanged += (sender, e) =>
            {
                GuiPanel.Parent.Resize += resizeHandler;
                resizeHandler(sender, e);
            };

            // Create the symbol search textbox
            SymbolTextbox = new TextBox();
            SymbolTextbox.BackColor = GuiStyle.DARK_GREY;
            SymbolTextbox.ForeColor = GuiStyle.TEXT_COLOR;
#if false
            SymbolTextbox.AutoCompleteMode = AutoCompleteMode.Suggest;
            SymbolTextbox.AutoCompleteSource = AutoCompleteSource.CustomSource;
            SymbolTextbox.TextChanged += (sender, e) =>
            {
                TextBox t = (TextBox)sender;
                if(t.Text.Length >= 1)
                {
                    // Get the available symbols matching the search string
                    AutoCompleteStringCollection collection = new AutoCompleteStringCollection();
                    collection.AddRange(SuggestSymbols(t.Text));

                    t.AutoCompleteCustomSource = collection;
                }
            };
#endif
            SymbolTextbox.KeyDown += (s, eventArgs) =>
            {
                if(eventArgs.KeyCode == Keys.Enter)
                {
                    SetSymbolInterval();
                    SetPlotLineSymbol(SymbolTextbox.Text);
                    this.Refresh();
                }
            };
            GuiPanel.Controls.Add(SymbolTextbox);

            XAxisTextbox = SymbolTextbox.Clone();
            XAxisTextbox.Text = "Time";
            XAxisTextbox.TextChanged += (sender, e) =>
            {
                TextBox t = (TextBox)sender;
                if(t.Text.Length >= 1)
                {
                    // Get the available symbols matching the search string
                    AutoCompleteStringCollection collection = new AutoCompleteStringCollection();
                    collection.AddRange(SuggestExpressions(t.Text));

                    t.AutoCompleteCustomSource = collection;
                }
            };
            XAxisTextbox.KeyDown += (s, eventArgs) =>
            {
                if(eventArgs.KeyCode == Keys.Enter)
                {
                    try
                    {
                        SetXAxis(XAxisTextbox.Text);
                        ErrorMessageLabel.Visible = false;
                    }
                    catch(Exception ex)
                    {
                        ErrorMessageLabel.Text = ex.ToString();
                        ErrorMessageLabel.Visible = true;
                    }
                }
            };
            GuiPanel.Controls.Add(XAxisTextbox);
            XAxisValue = new Label();
            XAxisValue.Font = GuiStyle.Font;
            XAxisValue.ForeColor = GuiStyle.TEXT_COLOR;
            XAxisTextbox.LocationChanged += (sender, e) =>
            {
                XAxisValue.Location = new System.Drawing.Point(XAxisTextbox.Right + 5, XAxisTextbox.Top);
            };
            XAxisValue.Size = new System.Drawing.Size(250, XAxisValue.Height);
            XAxisValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            GuiPanel.Controls.Add(XAxisValue);

            AddPlotButton = new PictureBox();
            AddPlotButton.Image = System.Drawing.Bitmap.FromFile("Content/GUI/Button_Add.png");
            AddPlotButton.Click += (sender, e) =>
            {
                AddPlotLine();
            };
            GuiPanel.Controls.Add(AddPlotButton);

            // Add the interactive chart controls
            Plot.AddInteraction(new PlotDrag(true, true));
            Plot.AddInteraction(new HoverInteraction(this));
            LineDrawer = new LineInteraction(this);
            Plot.AddInteraction(LineDrawer);

            var plotCanvas = base.Canvas;
            GuiPanel.Resize += (sender, e) =>
            {
                plotCanvas.Size = new System.Drawing.Size(GuiPanel.Width - 250, GuiPanel.Height - 75);
                plotCanvas.Location = new System.Drawing.Point(GuiPanel.Width - plotCanvas.Width - 5, (GuiPanel.Height - plotCanvas.Height) / 2);
            };
            GuiPanel.Controls.Add(plotCanvas);

            ErrorMessageLabel = new Label();
            ErrorMessageLabel.ForeColor = GuiStyle.PRICE_COLOR_NEGATIVE;
            ErrorMessageLabel.BackColor = GuiStyle.DARK_GREY;
            ErrorMessageLabel.AutoSize = false;
            ErrorMessageLabel.Visible = false;
            ErrorMessageLabel.Width = plotCanvas.Width - 10;
            ErrorMessageLabel.Height = plotCanvas.Height - 10;
            plotCanvas.Resize += (sender, e) =>
            {
                ErrorMessageLabel.Width = plotCanvas.Width - 10;
                ErrorMessageLabel.Height = plotCanvas.Height - 10;
            };
            ErrorMessageLabel.Location = new Point(plotCanvas.Left + 5, plotCanvas.Bottom - ErrorMessageLabel.Height - 5);
            plotCanvas.LocationChanged += (sender, e) =>
            {
                ErrorMessageLabel.Location = new Point(plotCanvas.Left + 5, plotCanvas.Top + 5);
            };
            GuiPanel.Controls.Add(ErrorMessageLabel);
            ErrorMessageLabel.BringToFront();

            foreach(var pair in IntervalButtons)
            {
                pair.Item1.SetImage(GuiButton.ButtonImage.GREEN_TRANSPARENT);
                pair.Item1.Click += (sender, e) =>
                {
                    // Update the selected button (first)
                    foreach(var p in IntervalButtons)
                    {
                        p.Item1.SetImage(GuiButton.ButtonImage.GREEN_TRANSPARENT);
                    }
                    pair.Item1.SetImage(GuiButton.ButtonImage.GREEN_WHITE);

                    // Set the new interval
                    SetSymbolInterval();
                    foreach (var l in Lines)
                    {
                        l.Generate(this);
                    }
                    Refresh();
                };
                GuiPanel.Controls.Add(pair.Item1);
            }
            IntervalButtons[0].Item1.SetImage(GuiButton.ButtonImage.GREEN_WHITE);

            // Add the button to reload the session
            ReloadButton = new GuiButton("Reload");
            ReloadButton.Location = new System.Drawing.Point(GuiPanel.Width - (ReloadButton.Width / 2) - 5, 5);
            ReloadButton.Click += (sender, e) =>
            {
                Session.Reload();
            };
            GuiPanel.Controls.Add(ReloadButton);

            ChartChanged += () =>
            {
                XAxisTextbox.Text = XAxis;
                foreach (var l in Lines)
                {
                    if (!l.Locked)
                    {
                        SymbolTextbox.Text = l.Symbol;
                        break;
                    }
                }
            };

            // Start with one line
            AddPlotLine("Price");

            // Reload the chart when the session is reloaded
            session.OnReload += ReloadData;
        }

#region Variables
        /// <summary>
        /// The panel containing the GUI elements
        /// </summary>
        public Panel GuiPanel;

        /// <summary>
        /// The textbox used to select the stock symbol
        /// </summary>
        public TextBox SymbolTextbox;

        /// <summary>
        /// The textbox used to set the X axis expression
        /// </summary>
        public TextBox XAxisTextbox;
        public Label XAxisValue;

        /// <summary>
        /// Displays an error message
        /// </summary>
        public Label ErrorMessageLabel;

        /// <summary>
        /// Button to add a new plot line
        /// </summary>
        public PictureBox AddPlotButton;

        /// <summary>
        /// Accesses the canvas object for the chart
        /// </summary>
        public new System.Windows.Forms.Control Canvas
        {
            get { return GuiPanel; }
        }

        /// <summary>
        /// Interface for adding additional lines to the chart
        /// </summary>
        public LineInteraction LineDrawer;

        /// <summary>
        /// The list of textboxes corresponding to the plot lines
        /// </summary>
        private List<TextBox> PlotLineTextboxes = new List<TextBox>();

        /// <summary>
        /// The list of labels corresponding to the plot lines
        /// </summary>
        private List<Label> PlotLineLabels = new List<Label>();

        /// <summary>
        /// Contains the buttons used to select the desired data interval
        /// </summary>
        private List<Tuple<GuiButton, TimeSpan>> IntervalButtons = new List<Tuple<GuiButton, TimeSpan>>()
        {
            { new Tuple<GuiButton, TimeSpan>(new GuiButton("1 min."), new TimeSpan(0, 1, 0)) },
            { new Tuple<GuiButton, TimeSpan>(new GuiButton("5 min."), new TimeSpan(0, 5, 0)) },
            { new Tuple<GuiButton, TimeSpan>(new GuiButton("10 min."), new TimeSpan(0, 10, 0)) },
            { new Tuple<GuiButton, TimeSpan>(new GuiButton("1 hr."), new TimeSpan(1, 0, 0)) },
            { new Tuple<GuiButton, TimeSpan>(new GuiButton("1 day."), new TimeSpan(24, 0, 0)) },
        };

        /// <summary>
        /// Button to reload the script and refresh the chart
        /// </summary>
        private GuiButton ReloadButton;
#endregion

#region Types
        private class HoverInteraction : NPlot.Interaction
        {
            public HoverInteraction(DataChartGui chart)
            {
                this.Chart = chart;
                Lines = new LineDrawer();
                //this.Chart.stockPricePlot.Add(Lines);
                //Lines.Canvas.Size = Chart.stockPricePlot.Canvas.Size;
                Lines.Canvas.Image = new System.Drawing.Bitmap(Chart.Plot.Canvas.Size.Width, Chart.Plot.Canvas.Size.Height);
                Lines.Canvas.BackColor = System.Drawing.Color.Transparent;
                Lines.Canvas.Size = Chart.Plot.Canvas.Size;
                Lines.Canvas.Enabled = false;
                Chart.Plot.Canvas.Controls.Add(Lines.Canvas);

                Chart.Plot.Canvas.Resize += (object sender, System.EventArgs e) =>
                {
                    int width = Chart.Plot.Canvas.Size.Width;
                    int height = Chart.Plot.Canvas.Size.Height;
                    Lines.Canvas.Image = new System.Drawing.Bitmap(width, height);
                    Lines.Canvas.Size = Chart.Plot.Canvas.Size;
                };
            }

            private class LineDrawer
            {
                /// <summary>
                /// The pen used to draw the time line
                /// </summary>
                public System.Drawing.Pen TimePen = new System.Drawing.Pen(GuiStyle.PRICE_COLOR_POSITIVE, 2.0f);

                /// <summary>
                /// The pen used to draw the price lines
                /// </summary>
                public System.Drawing.Pen PricePen = new System.Drawing.Pen(GuiStyle.GUIDE_COLOR, 1.5f);

                /// <summary>
                /// The canvas used to draw additional overlay lines
                /// </summary>
                public System.Windows.Forms.PictureBox Canvas = new PictureBox();
            }

            /// <summary>
            /// The chart the interaction should update
            /// </summary>
            public DataChartGui Chart;

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

                // Could clear the text when the mouse leaves the chart
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
                if ((Chart.Lines.Count == 0) || (Chart.Plot.PhysicalXAxis1Cache == null)) return false;
                double mouseVal = Chart.Plot.PhysicalXAxis1Cache.PhysicalToWorld(new System.Drawing.Point(X, Y), false);
                
                // Set the text values based on the cursor position
                if(Chart.XAxis.Equals("Time"))
                {
                    if(!Chart.Lines[0].DataMutex.WaitOne(5000)) return false;
                    int idx = Chart.GetDataIndex(mouseVal);
                    if (idx >= 0)
                    {
                        Chart.XAxisValue.Text = string.Format("{0:t} {0:MMM d} '{0:yy}", Chart.Lines[0].Data.Rows[idx][Chart.XAxis]);
                    }
                    Chart.Lines[0].DataMutex.ReleaseMutex();
                    for (int i = 0; i < Chart.Lines.Count; i++)
                    {
                        Chart.PlotLineLabels[i].Text = Chart.Lines[i].PrintValue(idx);
                    }
                        
                }
                else
                {
                    Chart.XAxisValue.Text = mouseVal.ToString();
                    for (int i = 0; i < Chart.Lines.Count; i++)
                    {
                        if (Chart.Lines[i].Plot != null)
                        {
                            Chart.PlotLineLabels[i].Text = Chart.Lines[i].Plot.PlotYAxis.PhysicalToWorld(new System.Drawing.Point(X, Y),
                                                                                                         Chart.Plot.PhysicalYAxis1Cache.PhysicalMin,
                                                                                                         Chart.Plot.PhysicalYAxis1Cache.PhysicalMax, false).ToString();
                        }
                    }
                }

                // Draw the line on the chart to show the cursor position
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(Lines.Canvas.Image))
                {
                    PhysicalAxis xAxis = Chart.Plot.PhysicalXAxis1Cache;
                    PhysicalAxis yAxis = Chart.Plot.PhysicalYAxis1Cache;
                    g.Clear(System.Drawing.Color.Transparent);

                    // Draw the line
                    g.DrawLine(Lines.TimePen, X, yAxis.PhysicalMin.Y, X, yAxis.PhysicalMax.Y);
                    g.DrawLine(Lines.PricePen, xAxis.PhysicalMin.X, Y, xAxis.PhysicalMax.X, Y);
                }

                // Refresh the canvas to display the updated lines
                Chart.Plot.Canvas.Refresh();

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
                if(Hovering && (Chart.Lines.Count > 0))
                {
                    double percentChange = ((direction > 0) ? (1 / 1.2) : (1.2));
                    bool isXAxis = !keys.HasFlag(Modifier.Control);
                    var physAxis = (isXAxis ? Chart.Plot.PhysicalXAxis1Cache : Chart.Plot.PhysicalYAxis1Cache);
                    double anchor = physAxis.PhysicalToWorld(new System.Drawing.Point(X, Y), false);

                    if(isXAxis)
                    {
                        double ratio = ((double)X / physAxis.PhysicalLength);
                        if((direction < 0) && (Chart.Plot.XAxis1.WorldMax > NPlot.Utils.ToDouble(Chart.Lines[0].Data.Rows[Chart.Lines[0].Data.Rows.Count - 1][Chart.XAxis])))
                        {
                            ratio = 1.0;
                        }
                        Chart.Plot.XAxis1.IncreaseRange(percentChange - 1.0, ratio);

                        Chart.UpdateMinMax();
                    }
                    else
                    {
                        double ratio = 1.0 - ((double)Y / physAxis.PhysicalLength);
                        for(int i = 0; i < Chart.Lines.Count; i++)
                        {
                            Chart.Lines[i].Plot.PlotYAxis.IncreaseRange(percentChange - 1.0, ratio);
                        }
                    }
                    Chart.Plot.Refresh();
                }

                return false;
            }
        }

        public class LineInteraction : NPlot.Interaction
        {
            public LineInteraction(DataChartGui chart)
            {
                this.Chart = chart;
                Canvas.Image = new System.Drawing.Bitmap(Chart.Plot.Canvas.Size.Width, Chart.Plot.Canvas.Size.Height);
                Canvas.BackColor = System.Drawing.Color.Transparent;
                Canvas.Size = Chart.Plot.Canvas.Size;
                Canvas.Enabled = false;
                Chart.Plot.Canvas.Controls.Add(Canvas);

                Chart.Plot.Canvas.Resize += (object sender, System.EventArgs e) =>
                {
                    int width = Chart.Plot.Canvas.Size.Width;
                    int height = Chart.Plot.Canvas.Size.Height;
                    if ((width > 0) && (height > 0))
                    {
                        Canvas.Image = new System.Drawing.Bitmap(width, height);
                        Canvas.Size = Chart.Plot.Canvas.Size;
                    }
                };
            }

            /// <summary>
            /// The chart the interaction should update
            /// </summary>
            public DataChartGui Chart;

            /// <summary>
            /// The canvas used to draw additional overlay lines
            /// </summary>
            public System.Windows.Forms.PictureBox Canvas = new PictureBox();

            /// <summary>
            /// Clears all lines from the canvas
            /// </summary>
            public void Clear()
            {
                using(System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(Canvas.Image))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                }
            }

            /// <summary>
            /// Adds lines to the chart
            /// </summary>
            /// <param name="pen">The pen to draw with</param>
            /// <param name="lines">The lines to draw (start and end points)</param>
            public void AddLine(System.Drawing.Pen pen, float startX, float startY, float endX, float endY)
            {
                // Draw the line on the chart
                using(System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(Canvas.Image))
                {
                    PhysicalAxis xAxis = Chart.Plot.PhysicalXAxis1Cache;
                    PhysicalAxis yAxis = Chart.Plot.PhysicalYAxis1Cache;

                    // Draw the line
                    if(xAxis != null && yAxis != null)
                    {
                        g.DrawLine(pen, xAxis.WorldToPhysical(startX, true).X, yAxis.WorldToPhysical(startY, true).Y, xAxis.WorldToPhysical(endX, true).X, yAxis.WorldToPhysical(endY, true).Y);
                    }
                }
            }
        }

        public class BorderTextBox : TextBox
        {
            public System.Drawing.Color BorderColor
            {
                get { return ColorPanel.BackColor; }
                set { ColorPanel.BackColor = value; }
            }
            public Panel ColorPanel;
            private Control PrevParent;
            public BorderTextBox()
                : base()
            {
                ColorPanel = new Panel();
                ColorPanel.Size = new Size(5, this.Height);
                ColorPanel.Location = new Point(this.Right, this.Top);

                Resize += (sender, e) =>
                {
                    ColorPanel.Location = new Point(this.Right, this.Top);
                };
                LocationChanged += (sender, e) =>
                {
                    ColorPanel.Location = new Point(this.Right, this.Top);
                };
                ParentChanged += (sender, e) =>
                {
                    if(this.Parent != null)
                    {
                        this.Parent.Controls.Add(ColorPanel);
                        this.PrevParent = Parent;
                    }
                    else
                    {
                        this.PrevParent.Controls.Remove(ColorPanel);
                    }
                };
            }
        }
#endregion

        /// <summary>
        /// Gets a list of suggested symbols based on the search string
        /// </summary>
        /// <param name="search">The portion of the symbol to search</param>
        /// <returns>A list of strings matching the search pattern</returns>
        private string[] SuggestSymbols(string search)
        {
            var lastSeparator = search.LastIndexOfAny(new char[] { ',', '-', '!' }) + 1;
            var symSearch = search.Substring(lastSeparator);
            var symList = search.Substring(0, lastSeparator);
            return DataSets.Keys.Where((s) => { return s.StartsWith(symSearch); }).ToList().ConvertAll((s) => { return symList + s; }).ToArray();
        }

        /// <summary>
        /// Gets a list of suggested expressions based on the search string
        /// </summary>
        /// <param name="search">The portion of the expression to search</param>
        /// <returns>A list of strings matching the search pattern</returns>
        private string[] SuggestExpressions(string search)
        {
            int startIdx = Math.Max(search.LastIndexOfAny(new char[] { ' ', ',', '+', '-', '*', '/', '(', ')', '.', '!', '^', '&', '|' }) + 1, 0);
            string fieldName = search.Substring(startIdx);
            Type dataType = getDataType();
            var fields = new List<string>();
            fields.AddRange(dataType.GetFields().ToList().ConvertAll((f) => { return f.Name; }));
            fields.AddRange(dataType.GetProperties().ToList().ConvertAll((f) => { return f.Name; }));
            fields.AddRange(dataType.GetMethods().ToList().ConvertAll((f) => { return f.Name; }));
            var possibleFields = fields.Where((s) => { return s.StartsWith(fieldName) && (startIdx < search.Length); }).ToList();
            var suggestions = possibleFields.ConvertAll((s) => { return search.Substring(0, startIdx) + s; }).ToArray();
            return suggestions;
        }

        /// <summary>
        /// Packs the plot textboxes so they are next to each other vertically
        /// </summary>
        private void PackPlotTextboxes()
        {
            int spacing = SymbolTextbox.Height + 5;
            for(int i = 0; i < PlotLineTextboxes.Count; i++)
            {
                PlotLineTextboxes[i].Location = new System.Drawing.Point(5, SymbolTextbox.Bottom + 5 + (spacing * i));
            }
            AddPlotButton.Location = new System.Drawing.Point(5, SymbolTextbox.Bottom + 5 + (spacing * PlotLineTextboxes.Count));
        }

        /// <summary>
        /// Updates the interval for the current symbol
        /// </summary>
        private void SetSymbolInterval()
        {
            TimeSpan span = IntervalButtons.Where((i) => { return i.Item1.Image == GuiButton.ButtonImages[(int)GuiButton.ButtonImage.GREEN_WHITE]; }).First().Item2;
            List<StockDataInterface> symbolDataSets;
            List<string> symbols = GetSymbolList(SymbolTextbox.Text);
            foreach(var symbol in symbols)
            {
                if(DataSets.TryGetValue(symbol, out symbolDataSets))
                {
                    foreach(var s in symbolDataSets)
                    {
                        s.SetInterval(span);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new line to the plot
        /// </summary>
        public void AddPlotLine(string expression = "")
        {
            ErrorMessageLabel.Visible = false;
            try { 
                var plot = this.AddPlot(SymbolTextbox.Text, expression);
                BorderTextBox plotTextbox = new BorderTextBox();
                plotTextbox.BorderColor = plot.Color;
                plotTextbox.Text = plot.Expression;
                plotTextbox.BackColor = GuiStyle.DARK_GREY;
                plotTextbox.ForeColor = GuiStyle.TEXT_COLOR;
                plotTextbox.BorderStyle = BorderStyle.Fixed3D;
                plotTextbox.KeyDown += (s, eventArgs) =>
                {
                    if(eventArgs.KeyCode == Keys.Enter)
                    {
                        TextBox t = (TextBox)s;
                        if(t.Text.Length == 0)
                        {
                            // Remove a plot line if it's expression is erased
                            if(plot.Plot != null) plot.Plot.Remove(this);
                            this.Lines.Remove(plot);
                            this.PlotLineTextboxes.Remove(t);
                            this.GuiPanel.Controls.Remove(t);
                            PackPlotTextboxes();
                        }
                        else
                        {
                            try {
                                plot.SetExpression(this, plotTextbox.Text);
                                ErrorMessageLabel.Visible = false;
                                plotTextbox.BorderColor = plot.Color;
                            } catch(Exception ex) {
                                ErrorMessageLabel.Text = ex.ToString();
                                ErrorMessageLabel.Visible = true;
                            }
                        }
                        Plot.Refresh();
                    }
                };
                plotTextbox.GotFocus += (sender, e) => {
                    plotTextbox.Width = 800;
                    plotTextbox.BringToFront();
                };
                plotTextbox.LostFocus += (sender, e) => { plotTextbox.Width = 125; };
                plotTextbox.Width = 125;
                plotTextbox.Focus();
#if false
                plotTextbox.AutoCompleteMode = AutoCompleteMode.Suggest;
                plotTextbox.AutoCompleteSource = AutoCompleteSource.CustomSource;
                plotTextbox.TextChanged += (s, eventArgs) =>
                {
                    if(plotTextbox.Text.Length >= 1)
                    {
                        // Get the available expressions matching the search string
                        AutoCompleteStringCollection collection = new AutoCompleteStringCollection();
                        collection.AddRange(SuggestExpressions(plotTextbox.Text));

                        plotTextbox.AutoCompleteCustomSource = collection;
                    }
                };
#endif
                plotTextbox.ColorPanel.MouseClick += (sender, e) => {
                    if(e.Button == MouseButtons.Left)
                    {
                        // Cycle to the next color
                        PlotLineColors.Enqueue(plot.Color); // Add the previous plot color to the list so it can be re-used
                        plot.Color = PlotLineColors.Dequeue();
                        PlotLineColors.Enqueue(plot.Color);   // Add the color to the end of the list so that it can be re-used if needed
                        plotTextbox.BorderColor = plot.Color;
                        plot.Locked = false;
                        plotTextbox.ColorPanel.BorderStyle = BorderStyle.None;
                    }
                    else if(e.Button == MouseButtons.Right)
                    {
                        // Cycle through transparency
                        byte[] alphaLevels = { 255, 128, 64, 16 };
                        for(int i = 0; i < alphaLevels.Length; i++)
                        {
                            if(alphaLevels[i] == plot.Color.A)
                            {
                                i = (i + 1) % alphaLevels.Length;
                                plot.Color = Color.FromArgb(alphaLevels[i], plot.Color);
                                break;
                            }
                        }
                    }
                    Plot.Refresh();
                };
                plotTextbox.ColorPanel.MouseDoubleClick += (sender, e) =>
                {
                    if(e.Button == MouseButtons.Left)
                    {
                        // Restore the previous color
                        plot.Color = PlotLineColors.ElementAt(PlotLineColors.Count - 2);
                        plotTextbox.BorderColor = plot.Color;
                        plot.Locked = true;
                        plotTextbox.ColorPanel.BorderStyle = BorderStyle.FixedSingle;
                        Plot.Refresh();
                    }
                };
                this.PlotLineTextboxes.Add(plotTextbox);
                this.GuiPanel.Controls.Add(plotTextbox);
                Label plotLabel = new Label();
                plotLabel.ForeColor = GuiStyle.TEXT_COLOR;
                plotLabel.Font = GuiStyle.Font;
                plotLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
                plotLabel.Size = new Size(110, plotLabel.Height);
                plotTextbox.ParentChanged += (sender, e) =>
                {
                    if(plotTextbox.Parent == null)
                    {
                        this.PlotLineLabels.Remove(plotLabel);
                        this.GuiPanel.Controls.Remove(plotLabel);
                    }
                };
                EventHandler updateLabelLocation = (sender, e) =>
                {
                    plotLabel.Location = new System.Drawing.Point(plotTextbox.Right + 5, plotTextbox.Top);
                };
                plotTextbox.LocationChanged += updateLabelLocation;
                plotTextbox.Resize += updateLabelLocation;
                updateLabelLocation(null, null);
                this.PlotLineLabels.Add(plotLabel);
                this.GuiPanel.Controls.Add(plotLabel);
                plotLabel.BringToFront();

                // Register a callback to update the GUI if the plot's expression is changed via a script
                plot.ExpressionChanged += (line) => {
                    plotTextbox.Text = line.Expression;
                };

                PackPlotTextboxes();
                this.Refresh();
            }
            catch(Exception ex)
            {
                ErrorMessageLabel.Text = ex.ToString();
                ErrorMessageLabel.Visible = true;
            }
        }

        /// <summary>
        /// Clears all lines from the plot
        /// </summary>
        public override void Clear()
        {
            foreach(var t in this.PlotLineTextboxes)
            {
                this.GuiPanel.Controls.Remove(t);
            }
            this.PlotLineTextboxes.Clear();
            this.SymbolTextbox.Text = "";
            base.Clear();
            LineDrawer.Clear();

            PackPlotTextboxes();
            this.Refresh();
        }

        /// <summary>
        /// Sets the stock symbol for all plot lines
        /// </summary>
        /// <param name="symbol">The symbol to set</param>
        public override void SetPlotLineSymbol(string symbol)
        {
            this.SymbolTextbox.Text = symbol;
            base.SetPlotLineSymbol(symbol);
        }
    }

    public static class ControlExtensions
    {
        public static T Clone<T>(this T controlToClone)
            where T : Control
        {
            System.Reflection.PropertyInfo[] controlProperties = typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            T instance = Activator.CreateInstance<T>();

            foreach(var propInfo in controlProperties)
            {
                if(propInfo.CanWrite)
                {
                    if(propInfo.Name != "WindowTarget")
                        propInfo.SetValue(instance, propInfo.GetValue(controlToClone, null), null);
                }
            }

            return instance;
        }
    }
}
