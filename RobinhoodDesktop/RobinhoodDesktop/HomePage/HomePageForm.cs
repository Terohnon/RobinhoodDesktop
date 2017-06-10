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

            StockChart plot = new StockChart();
            plot.SetChartData(GenerateExampleData());
            this.Controls.Add(plot.Canvas);
        }

        private static System.Data.DataTable GenerateExampleData()
        {
			System.Data.DataTable dt = new System.Data.DataTable();
			dt.Columns.Add("Time", typeof(DateTime));
			dt.Columns.Add("Price", typeof(float));
;
			try
			{
				var rh = new RobinhoodClient();
				var history = rh.DownloadHistory("AMD", "5minute", "week").Result;

				foreach (var p in history.HistoricalInfo)
				{
					dt.Rows.Add(p.BeginsAt, (float)p.OpenPrice);
				}
			}
			catch
			{
				Environment.Exit(1);
			}

            return dt;
        }
    }
}
