using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ScrcpyController.UI
{
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
                // The up/down buttons are usually at Controls[0]
                if (this.Controls.Count > 0)
                {
                    var btn = this.Controls[0];
                    btn.Visible = false;
                    btn.Enabled = false;
                    btn.Size = new Size(0, 0);
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
                    tb.Location = new Point(0, 0);
                    tb.Size = new Size(this.ClientSize.Width, this.ClientSize.Height);
                    tb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                    tb.Margin = new Padding(0);
                    tb.Padding = new Padding(0);
                }
            }
            catch { }
        }
    }
}
