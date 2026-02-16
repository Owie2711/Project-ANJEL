using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows.Forms;

namespace ScrcpyController.UI
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class NoArrowNumericUpDown : NumericUpDown
    {
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            HideUpDownButtons();
            AdjustTextBox();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            AdjustTextBox();
        }

        private void HideUpDownButtons()
        {
            try
            {
                // Keep the inner TextBox visible and hide other controls (up/down buttons)
                var tb = this.Controls.OfType<TextBox>().FirstOrDefault();
                foreach (Control c in this.Controls)
                {
                    if (c == tb) continue;
                    try
                    {
                        c.Visible = false;
                        c.Enabled = false;
                        c.Size = new Size(0, 0);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void AdjustTextBox()
        {
            try
            {
                var tb = this.Controls.OfType<TextBox>().FirstOrDefault();
                if (tb != null)
                {
                    tb.TextAlign = HorizontalAlignment.Center;
                    tb.BorderStyle = BorderStyle.None;
                    tb.ReadOnly = false;
                    tb.Enabled = true;
                    tb.Visible = true;
                    tb.BringToFront();
                    // Use multiline textbox sized to full client area and apply top padding
                    tb.Multiline = true;
                    tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                    tb.Location = new Point(0, 0);
                    tb.Size = new Size(this.ClientSize.Width, this.ClientSize.Height);
                    int fontHeight = tb.Font.Height; // use font height for precise centering
                    int topPadding = Math.Max(0, (this.ClientSize.Height - fontHeight) / 2 - 1);
                    tb.Padding = new Padding(0, topPadding, 0, 0);
                    tb.Margin = new Padding(0);
                    tb.AcceptsReturn = false;
                }
            }
            catch { }
        }

        
    }
}
