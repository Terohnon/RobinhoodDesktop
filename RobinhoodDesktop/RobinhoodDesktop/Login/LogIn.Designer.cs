namespace RobinhoodDesktop
{
	partial class LogInForm
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
			this.GuiPanel = new System.Windows.Forms.Panel();
			this.ErrorLabel = new System.Windows.Forms.Label();
			this.LogInButton = new System.Windows.Forms.Button();
			this.CancelButton = new System.Windows.Forms.Button();
			this.RememberLogIn = new System.Windows.Forms.CheckBox();
			this.Password = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.Username = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.GuiPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// GuiPanel
			// 
			this.GuiPanel.AutoSize = true;
			this.GuiPanel.Controls.Add(this.ErrorLabel);
			this.GuiPanel.Controls.Add(this.LogInButton);
			this.GuiPanel.Controls.Add(this.CancelButton);
			this.GuiPanel.Controls.Add(this.RememberLogIn);
			this.GuiPanel.Controls.Add(this.Password);
			this.GuiPanel.Controls.Add(this.label2);
			this.GuiPanel.Controls.Add(this.Username);
			this.GuiPanel.Controls.Add(this.label1);
			this.GuiPanel.Location = new System.Drawing.Point(12, 12);
			this.GuiPanel.Name = "GuiPanel";
			this.GuiPanel.Size = new System.Drawing.Size(776, 426);
			this.GuiPanel.TabIndex = 0;
			this.GuiPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.panel1_Paint);
			// 
			// ErrorLabel
			// 
			this.ErrorLabel.AutoSize = true;
			this.ErrorLabel.Location = new System.Drawing.Point(159, 351);
			this.ErrorLabel.Name = "ErrorLabel";
			this.ErrorLabel.Size = new System.Drawing.Size(0, 25);
			this.ErrorLabel.TabIndex = 9;
			// 
			// LogInButton
			// 
			this.LogInButton.Location = new System.Drawing.Point(411, 249);
			this.LogInButton.Name = "LogInButton";
			this.LogInButton.Size = new System.Drawing.Size(139, 55);
			this.LogInButton.TabIndex = 8;
			this.LogInButton.Text = "Login";
			this.LogInButton.UseVisualStyleBackColor = true;
			this.LogInButton.Click += new System.EventHandler(this.Login_Click);
			// 
			// CancelButton
			// 
			this.CancelButton.Location = new System.Drawing.Point(257, 250);
			this.CancelButton.Name = "CancelButton";
			this.CancelButton.Size = new System.Drawing.Size(139, 55);
			this.CancelButton.TabIndex = 7;
			this.CancelButton.Text = "Cancel";
			this.CancelButton.UseVisualStyleBackColor = true;
			// 
			// RememberLogIn
			// 
			this.RememberLogIn.AutoSize = true;
			this.RememberLogIn.Location = new System.Drawing.Point(222, 145);
			this.RememberLogIn.Name = "RememberLogIn";
			this.RememberLogIn.Size = new System.Drawing.Size(219, 29);
			this.RememberLogIn.TabIndex = 6;
			this.RememberLogIn.Text = "Remember Login?";
			this.RememberLogIn.UseVisualStyleBackColor = true;
			// 
			// Password
			// 
			this.Password.Location = new System.Drawing.Point(235, 78);
			this.Password.Name = "Password";
			this.Password.Size = new System.Drawing.Size(390, 31);
			this.Password.TabIndex = 5;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(57, 81);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(106, 25);
			this.label2.TabIndex = 4;
			this.label2.Text = "Password";
			// 
			// Username
			// 
			this.Username.Location = new System.Drawing.Point(235, 41);
			this.Username.Name = "Username";
			this.Username.Size = new System.Drawing.Size(390, 31);
			this.Username.TabIndex = 3;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(57, 44);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(172, 25);
			this.label1.TabIndex = 0;
			this.label1.Text = "UserName/Email";
			// 
			// LogInForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(800, 450);
			this.Controls.Add(this.GuiPanel);
			this.Name = "LogInForm";
			this.Text = "LogIn";
			this.Load += new System.EventHandler(this.LogIn_Load);
			this.GuiPanel.ResumeLayout(false);
			this.GuiPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.TextBox Password;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox Username;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label ErrorLabel;
		public System.Windows.Forms.Button LogInButton;
		public System.Windows.Forms.Button CancelButton;
		public System.Windows.Forms.Panel GuiPanel;
		public System.Windows.Forms.CheckBox RememberLogIn;
	}
}