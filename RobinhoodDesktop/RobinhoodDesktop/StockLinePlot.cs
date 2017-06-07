using System;
using System.Collections.Generic;
using System.Drawing;

namespace NPlot
{
    public class StockLinePlot : BaseSequencePlot, IPlot, ISequencePlot
    {
        /// <summary>
		/// Default constructor
		/// </summary>
		public StockLinePlot()
        {
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dataSource">The data source to associate with this plot</param>
        public StockLinePlot(object dataSource)
        {
            this.DataSource = dataSource;
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ordinateData">the ordinate data to associate with this plot.</param>
        /// <param name="abscissaData">the abscissa data to associate with this plot.</param>
        public StockLinePlot(object ordinateData, object abscissaData)
        {
            this.OrdinateData = ordinateData;
            this.AbscissaData = abscissaData;
        }

        /// <summary>
		/// Draws the line plot on a GDI+ surface against the provided x and y axes.
		/// </summary>
		/// <param name="g">The GDI+ surface on which to draw.</param>
		/// <param name="xAxis">The X-Axis to draw against.</param>
		/// <param name="yAxis">The Y-Axis to draw against.</param>
		/// <param name="drawShadow">If true draw the shadow for the line. If false, draw line.</param>
		public void DrawLineOrShadow(Graphics g, PhysicalAxis xAxis, PhysicalAxis yAxis, bool drawShadow)
        {
            Pen shadowPen = null;
            if(drawShadow)
            {
                shadowPen = (Pen)this.Pen.Clone();
                shadowPen.Color = this.ShadowColor;
            }

            SequenceAdapter data =
                new SequenceAdapter(this.DataSource, this.DataMember, this.OrdinateData, this.AbscissaData);

            int numberPoints = data.Count;

            if(data.Count == 0)
            {
                return;
            }

            // clipping is now handled assigning a clip region in the
            // graphic object before this call
            if(numberPoints == 1)
            {
                PointF physical = Transform2D.GetTransformer(xAxis, yAxis).Transform(data[0]);

                if(drawShadow)
                {
                    g.DrawLine(shadowPen,
                        physical.X - 0.5f + this.ShadowOffset.X,
                        physical.Y + this.ShadowOffset.Y,
                        physical.X + 0.5f + this.ShadowOffset.X,
                        physical.Y + this.ShadowOffset.Y);
                }
                else
                {
                    g.DrawLine(Pen, physical.X - 0.5f, physical.Y, physical.X + 0.5f, physical.Y);
                }
            }
            else
            {
                // prepare for clipping
                double leftCutoff = xAxis.PhysicalToWorld(xAxis.PhysicalMin, false);
                double rightCutoff = xAxis.PhysicalToWorld(xAxis.PhysicalMax, false);
                if(leftCutoff > rightCutoff)
                {
                    double temp = leftCutoff;
                    leftCutoff = rightCutoff;
                    rightCutoff = temp;
                }
                if(drawShadow)
                {
                    // correct cut-offs
                    double shadowCorrection =
                        xAxis.PhysicalToWorld(ShadowOffset, false) - xAxis.PhysicalToWorld(new Point(0, 0), false);
                    leftCutoff -= shadowCorrection;
                    rightCutoff -= shadowCorrection;
                }

                // determine which points to plot
                double tradingTimeOffset = 0;
                List<PointD> plotPoints = new List<PointD>(numberPoints);
                PointD d1 = data[0];
                PointD d2 = d1;
                if((d1.X >= leftCutoff) && (d1.X <= rightCutoff)) plotPoints.Add(d1);
                DateTime prevTime = (DateTime)((System.Data.DataTable)this.DataSource).Rows[0][(string)this.AbscissaData];
                for(int i = 1; i < numberPoints; ++i)
                {
                    // check to see if any values null. If so, then continue.
                    d1 = d2;
                    d2 = data[i];
                    if(Double.IsNaN(d1.X) || Double.IsNaN(d1.Y) ||
                        Double.IsNaN(d2.X) || Double.IsNaN(d2.Y))
                    {
                        continue;
                    }

                    // Get the X axis offset to strip out the non-trading time
                    d1.X -= tradingTimeOffset;
                    DateTime nextTime = (DateTime)((System.Data.DataTable)this.DataSource).Rows[i][(string)this.AbscissaData];
                    if(nextTime.TimeOfDay < prevTime.TimeOfDay)
                    {
                        tradingTimeOffset += (double)(nextTime - prevTime).Ticks;
                    }
                    prevTime = nextTime;
                    d2.X -= tradingTimeOffset;

                    // do horizontal clipping here, to speed up
                    if((d1.X < leftCutoff && d2.X < leftCutoff) ||
                        (rightCutoff < d1.X && rightCutoff < d2.X))
                    {
                        continue;
                    }

                    // Add a point to plot
                    plotPoints.Add(d2);
                }

                // create a transform, which takes into account the skipped time
                PhysicalAxis shunkAxis = new PhysicalAxis(new DateTimeAxis(xAxis.Axis.WorldMin, xAxis.Axis.WorldMax - tradingTimeOffset), xAxis.PhysicalMin, xAxis.PhysicalMax);
                ITransform2D t = Transform2D.GetTransformer(shunkAxis, yAxis);

                // plot those points
                for(int i = 1; i < plotPoints.Count; i++)
                {
                    // else draw line.	
                    PointF p1 = t.Transform(plotPoints[i - 1]);
                    PointF p2 = t.Transform(plotPoints[i]);

                    // when very far zoomed in, points can fall ontop of each other,
                    // and g.DrawLine throws an overflow exception
                    if(p1.Equals(p2))
                    {
                        continue;
                    }

                    if(drawShadow)
                    {
                        g.DrawLine(shadowPen,
                            p1.X + ShadowOffset.X,
                            p1.Y + ShadowOffset.Y,
                            p2.X + ShadowOffset.X,
                            p2.Y + ShadowOffset.Y);
                    }
                    else
                    {
                        g.DrawLine(Pen, p1.X, p1.Y, p2.X, p2.Y);
                    }
                }
            }
        }

        /// <summary>
		/// Draws the line plot on a GDI+ surface against the provided x and y axes.
		/// </summary>
		/// <param name="g">The GDI+ surface on which to draw.</param>
		/// <param name="xAxis">The X-Axis to draw against.</param>
		/// <param name="yAxis">The Y-Axis to draw against.</param>
		public void Draw(Graphics g, PhysicalAxis xAxis, PhysicalAxis yAxis)
        {
            if(this.Shadow)
            {
                this.DrawLineOrShadow(g, xAxis, yAxis, true);
            }

            this.DrawLineOrShadow(g, xAxis, yAxis, false);
        }

        /// <summary>
		/// Returns an x-axis that is suitable for drawing this plot.
		/// </summary>
		/// <returns>A suitable x-axis.</returns>
		public Axis SuggestXAxis()
        {
            SequenceAdapter data_ =
                new SequenceAdapter(this.DataSource, this.DataMember, this.OrdinateData, this.AbscissaData);

            return data_.SuggestXAxis();
        }


        /// <summary>
        /// Returns a y-axis that is suitable for drawing this plot.
        /// </summary>
        /// <returns>A suitable y-axis.</returns>
        public Axis SuggestYAxis()
        {
            SequenceAdapter data_ =
                new SequenceAdapter(this.DataSource, this.DataMember, this.OrdinateData, this.AbscissaData);

            return data_.SuggestYAxis();
        }


        /// <summary>
        /// If true, draw a shadow under the line.
        /// </summary>
        public bool Shadow
        {
            get
            {
                return shadow_;
            }
            set
            {
                shadow_ = value;
            }
        }
        private bool shadow_ = false;


        /// <summary>
        /// Color of line shadow if drawn. Use Shadow method to turn shadow on and off.
        /// </summary>
        public Color ShadowColor
        {
            get
            {
                return shadowColor_;
            }
            set
            {
                shadowColor_ = value;
            }
        }
        private Color shadowColor_ = Color.FromArgb(100, 100, 100);


        /// <summary>
        /// Offset of shadow line from primary line.
        /// </summary>
        public Point ShadowOffset
        {
            get
            {
                return shadowOffset_;
            }
            set
            {
                shadowOffset_ = value;
            }
        }
        private Point shadowOffset_ = new Point(1, 1);


        /// <summary>
        /// Draws a representation of this plot in the legend.
        /// </summary>
        /// <param name="g">The graphics surface on which to draw.</param>
        /// <param name="startEnd">A rectangle specifying the bounds of the area in the legend set aside for drawing.</param>
        public virtual void DrawInLegend(Graphics g, Rectangle startEnd)
        {
            g.DrawLine(pen_, startEnd.Left, (startEnd.Top + startEnd.Bottom) / 2,
                startEnd.Right, (startEnd.Top + startEnd.Bottom) / 2);
        }


        /// <summary>
        /// The pen used to draw the plot
        /// </summary>
        public System.Drawing.Pen Pen
        {
            get
            {
                return pen_;
            }
            set
            {
                pen_ = value;
            }
        }
        private System.Drawing.Pen pen_ = new Pen(Color.Black);


        /// <summary>
        /// The color of the pen used to draw lines in this plot.
        /// </summary>
        public System.Drawing.Color Color
        {
            set
            {
                if(pen_ != null)
                {
                    pen_.Color = value;
                }
                else
                {
                    pen_ = new Pen(value);
                }
            }
            get
            {
                return pen_.Color;
            }
        }
    }
}
