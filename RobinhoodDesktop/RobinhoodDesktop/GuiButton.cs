using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobinhoodDesktop
{
    public class GuiButton : PictureBox
    {

        public GuiButton(string text)
        {
            this.Image = System.Drawing.Bitmap.FromFile("Content/GUI/Button.png");
            this.Size = Image.Size;

            this.Text = text;
            this.Font = GuiStyle.Font;
            this.Paint += (object sender, PaintEventArgs e) =>
            {
                SolidBrush brush = new SolidBrush(GuiStyle.PRICE_COLOR_POSITIVE);
                StringFormat format = new StringFormat();
                format.LineAlignment = StringAlignment.Center;
                format.Alignment = StringAlignment.Center;
                e.Graphics.DrawString(Text, Font, brush, new Point(Width / 2, Height / 2), format);
            };
        }

        #region Variables
        #endregion
    }
}
