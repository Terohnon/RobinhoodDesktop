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
            this.Image = ButtonImages[(int)ButtonImage.GREEN_TRANSPARENT];
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

        #region Constants
        /// <summary>
        /// The supported button images
        /// </summary>
        public enum ButtonImage
        {
            GREEN_TRANSPARENT,
            GREEN_WHITE,
        };

        /// <summary>
        /// The button image files
        /// </summary>
        public static readonly System.Drawing.Image[] ButtonImages = new System.Drawing.Image[] {
            System.Drawing.Bitmap.FromFile("Content/GUI/Button.png"),       // ButtonImage.GREEN_TRANSPARENT
            System.Drawing.Bitmap.FromFile("Content/GUI/Button_White.png"), // ButtonImage.GREEN_WHITE
        };
        #endregion

        #region Variables
        /// <summary>
        /// The text displayed on the GUI Button
        /// </summary>
        public override string Text
        {
            get
            {
                return base.Text;
            }

            set
            {
                base.Text = value;
            }
        }
        #endregion

        /// <summary>
        /// Sets the button image
        /// </summary>
        /// <param name="img">The image to set</param>
        public void SetImage(ButtonImage img)
        {
            this.Image = ButtonImages[(int)img];
        }
    }
}
