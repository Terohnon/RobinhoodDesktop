using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BasicallyMe.RobinhoodNet;

namespace RobinhoodDesktop.HomePage
{
    public partial class HomePageForm : Form
    {
        public HomePageForm()
        {
            InitializeComponent();

            //HomePage.AccountSummaryChart accountChart = new HomePage.AccountSummaryChart();
            //accountChart.Size = new Size(this.Width - 20, this.Height - 20);
            //this.Controls.Add(accountChart);

            Plot = new StockChart();
            Plot.SetChartData(GenerateExampleData());
            this.Controls.Add(Plot.Canvas);

            this.ResizeEnd += HomePageForm_ResizeEnd;
            HomePageForm_ResizeEnd(this, EventArgs.Empty);
        }

        #region Variables
        public StockChart Plot;
        #endregion

        private static System.Data.DataTable GenerateExampleData()
        {
            System.Data.DataTable dt = new System.Data.DataTable();
            dt.Columns.Add("Time", typeof(DateTime));
            dt.Columns.Add("Price", typeof(float));

            try
            {
                var rh = new RobinhoodClient();
                var history = rh.DownloadHistory("AMD", "5minute", "week").Result;

                foreach(var p in history.HistoricalInfo)
                {
                    dt.Rows.Add(p.BeginsAt.ToLocalTime(), (float)p.OpenPrice);
                }
            }
            catch(Exception ex)
            {
                Environment.Exit(1);
            }

            return dt;
        }

        private void HomePageForm_ResizeEnd(object sender, System.EventArgs e)
        {
            Plot.Canvas.Size = new Size(this.Width - 20, 300);
        }
    }
}
