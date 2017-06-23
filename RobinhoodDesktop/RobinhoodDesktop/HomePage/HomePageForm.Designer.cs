namespace RobinhoodDesktop.HomePage
{
    partial class HomePageForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.stockListHome = new RobinhoodDesktop.StockList();
            this.SuspendLayout();
            // 
            // stockListHome
            // 
            this.stockListHome.Location = new System.Drawing.Point(60, 364);
            this.stockListHome.Name = "stockListHome";
            this.stockListHome.Size = new System.Drawing.Size(952, 343);
            this.stockListHome.TabIndex = 0;
            // 
            // HomePageForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1064, 730);
            this.Controls.Add(this.stockListHome);
            this.Name = "HomePageForm";
            this.Text = "Robinhood Desktop";
            this.ResumeLayout(false);

        }

        #endregion

        private StockList stockListHome;
    }
}

