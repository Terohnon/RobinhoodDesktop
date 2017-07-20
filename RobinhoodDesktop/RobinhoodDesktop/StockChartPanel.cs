using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    public class StockChartPanel : Panel
    {
        public StockChartPanel(Control chartPanel)
        {
            this.Resize += StockChartPanel_Resize;

            // Create the summary bar
            SummaryBarPanel = new Panel();
            SummaryBarPanel.Size = new System.Drawing.Size(600, 25);
            SummaryLabel = new Label();
            SummaryLabel.ForeColor = StockChart.TEXT_COLOR;
            SummaryLabel.Size = new System.Drawing.Size(200, 20);
            SummaryLabel.BackColor = System.Drawing.Color.Transparent;
            SummaryLabel.Location = new System.Drawing.Point(10, 5);
            SummaryBarPanel.Controls.Add(SummaryLabel);

            CollapseButton = new PictureBox();
            CollapseButton.Image = System.Drawing.Bitmap.FromFile("Content/GUI/Button_Collapse.png");
            CollapseButton.BackColor = System.Drawing.Color.Transparent;
            CollapseButton.Size = CollapseButton.Image.Size;
            CollapseButton.Location = new System.Drawing.Point(SummaryLabel.Location.X + SummaryLabel.Width, 5);
            CollapseButton.MouseUp += CollapseButton_MouseUp;
            SummaryBarPanel.Controls.Add(CollapseButton);

            CloseButton = new PictureBox();
            CloseButton.Image = System.Drawing.Bitmap.FromFile("Content/GUI/Button_Close.png");
            CloseButton.BackColor = System.Drawing.Color.Transparent;
            CloseButton.Size = CollapseButton.Image.Size;
            CloseButton.Location = new System.Drawing.Point(CollapseButton.Location.X + CollapseButton.Width, 5);
            CloseButton.MouseUp += (object sender, MouseEventArgs e) =>
            {

            };
            SummaryBarPanel.Controls.Add(CloseButton);

            this.Controls.Add(SummaryBarPanel);
            this.ChartPanel = chartPanel;
            ChartPanel.Location = new System.Drawing.Point(SummaryBarPanel.Location.X, SummaryBarPanel.Location.Y + SummaryBarPanel.Height + 5);
            this.Controls.Add(ChartPanel);
        }


        #region Variables
        /// <summary>
        /// Label used to provide a summary of the information in the panel
        /// </summary>
        public Label SummaryLabel;
        /// <summary>
        /// The button used to expand/collapse the panel
        /// </summary>
        public PictureBox CollapseButton;

        /// <summary>
        /// The button used to close the panel
        /// </summary>
        public PictureBox CloseButton;

        /// <summary>
        /// The panel containing the chart(s)
        /// </summary>
        public Control ChartPanel;

        /// <summary>
        /// The panel comprising the summary bar
        /// </summary>
        private Panel SummaryBarPanel;
        #endregion

        /// <summary>
        /// Handles the collapse action
        /// </summary>
        /// <param name="sender">The PictureBox object</param>
        /// <param name="e">The mouse information</param>
        private void CollapseButton_MouseUp(object sender, MouseEventArgs e)
        {
            if(Controls.Contains(ChartPanel))
            {
                Controls.Remove(ChartPanel);
                CollapseButton.Image.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
                CollapseButton.Refresh();
                this.Size = SummaryBarPanel.Size;
            }
            else
            {
                Controls.Add(ChartPanel);
                CollapseButton.Image.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
                CollapseButton.Refresh();
                this.Size = new System.Drawing.Size(SummaryBarPanel.Width, ChartPanel.Location.Y + ChartPanel.Height);
            }
        }

        /// <summary>
        /// Callback executed when this object is resized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StockChartPanel_Resize(object sender, System.EventArgs e)
        {
            SummaryBarPanel.Size = new System.Drawing.Size(this.Width, SummaryBarPanel.Height);
            CloseButton.Location = new System.Drawing.Point((SummaryBarPanel.Width - CloseButton.Width) - 10, CloseButton.Location.Y);
            CollapseButton.Location = new System.Drawing.Point((CloseButton.Location.X - CollapseButton.Width) - 5, CollapseButton.Location.Y);
            if(Controls.Contains(ChartPanel))
            {
                ChartPanel.Size = new System.Drawing.Size(this.Width, (this.Height - SummaryBarPanel.Height - 5));
            }
        }
    }
}
