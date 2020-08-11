using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BasicallyMe.RobinhoodNet.DataTypes;

namespace RobinhoodDesktop
{
	public partial class LogInForm : Form
	{
		SMS_Code sms_gui = new SMS_Code();
		ChallengeInfo currChallenge;
		public string deviceToken;
		string challengeID = "";
		public LogInForm()
		{
			InitializeComponent();

			// Add logic for when the user interacts with the verify screen
			sms_gui.btn_Verify.MouseUp += (sender, e) =>
			{
				(var success, var challenge) = Broker.Instance.ChallengeResponse(currChallenge.challenge.id, sms_gui.txt_SMS_Code.Text);
				if (success)
				{
					if (challenge.challenge.status == "validated")
					{
						challengeID = challenge.challenge.id;
						GuiPanel.Controls.Remove(sms_gui.smsVerifyPanel);
						ErrorLabel.Text = "Success! You can now sign in!!!";
						GuiPanel.Refresh();
					}
					else
					{
						ErrorLabel.Text = "Error: Invalid pin: " + challenge.challenge.remaining_retries + " retries left";
						GuiPanel.Refresh();
					}
				}
				else
				{
					ErrorLabel.Text = "Error: Invalid pin: " + challenge.challenge.remaining_retries + " retries left";
					GuiPanel.Refresh();
				}
			};
		}

		private void LogIn_Load(object sender, EventArgs e)
		{

		}

		private void panel1_Paint(object sender, PaintEventArgs e)
		{

		}

		private void Login_Click(object sender, EventArgs e)
		{
			(var success, var challenge) = Broker.Instance.SignIn(Username.Text, Password.Text, deviceToken, challengeID);

			if (challenge == null)
			{
				ErrorLabel.Text = "Error: Robinhood appears to have changed their login API, or there is an error with ours";
				GuiPanel.Refresh();
			}
			else if (!success && challenge.challenge.status == "issued")
			{
				currChallenge = challenge;
				GuiPanel.Controls.Add(sms_gui.smsVerifyPanel);
				GuiPanel.Refresh();
			}
			else
			{
				Password.Text = "";
				if (!Broker.Instance.IsSignedIn())
				{
					ErrorLabel.Text = "Error: Invalid username or password";
					GuiPanel.Refresh();
				}
			}
		}
	}
}
