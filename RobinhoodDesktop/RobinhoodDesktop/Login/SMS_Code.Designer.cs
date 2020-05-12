namespace RobinhoodDesktop
{
	partial class SMS_Code
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
			this.txt_SMS_Code = new System.Windows.Forms.TextBox();
			this.btn_Verify = new System.Windows.Forms.Button();
			this.btn_Cancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.smsVerifyPanel = new System.Windows.Forms.Panel();
			this.smsVerifyPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// txt_SMS_Code
			// 
			this.txt_SMS_Code.Location = new System.Drawing.Point(734, 219);
			this.txt_SMS_Code.MaxLength = 6;
			this.txt_SMS_Code.Name = "txt_SMS_Code";
			this.txt_SMS_Code.Size = new System.Drawing.Size(100, 31);
			this.txt_SMS_Code.TabIndex = 0;
			// 
			// btn_Verify
			// 
			this.btn_Verify.Location = new System.Drawing.Point(599, 274);
			this.btn_Verify.Name = "btn_Verify";
			this.btn_Verify.Size = new System.Drawing.Size(129, 45);
			this.btn_Verify.TabIndex = 1;
			this.btn_Verify.Text = "Verify";
			this.btn_Verify.UseVisualStyleBackColor = true;
			// 
			// btn_Cancel
			// 
			this.btn_Cancel.Location = new System.Drawing.Point(433, 274);
			this.btn_Cancel.Name = "btn_Cancel";
			this.btn_Cancel.Size = new System.Drawing.Size(129, 45);
			this.btn_Cancel.TabIndex = 2;
			this.btn_Cancel.Text = "Cancel";
			this.btn_Cancel.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(295, 219);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(433, 25);
			this.label1.TabIndex = 3;
			this.label1.Text = "Enter six digit code received on your phone:";
			// 
			// smsVerifyPanel
			// 
			this.smsVerifyPanel.Controls.Add(this.btn_Verify);
			this.smsVerifyPanel.Controls.Add(this.label1);
			this.smsVerifyPanel.Controls.Add(this.txt_SMS_Code);
			this.smsVerifyPanel.Controls.Add(this.btn_Cancel);
			this.smsVerifyPanel.Location = new System.Drawing.Point(466, 311);
			this.smsVerifyPanel.Name = "smsVerifyPanel";
			this.smsVerifyPanel.Size = new System.Drawing.Size(1148, 610);
			this.smsVerifyPanel.TabIndex = 4;
			// 
			// SMS_Code
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(2072, 1248);
			this.Controls.Add(this.smsVerifyPanel);
			this.Name = "SMS_Code";
			this.Text = "SMS_Code";
			this.smsVerifyPanel.ResumeLayout(false);
			this.smsVerifyPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.Label label1;
		public System.Windows.Forms.Panel smsVerifyPanel;
		public System.Windows.Forms.TextBox txt_SMS_Code;
		public System.Windows.Forms.Button btn_Verify;
		public System.Windows.Forms.Button btn_Cancel;
	}
}