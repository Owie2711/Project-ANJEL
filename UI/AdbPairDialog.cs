using System;
using System.Drawing;
using System.Windows.Forms;
using ScrcpyController.Core;

namespace ScrcpyController.UI
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class AdbPairDialog : Form
    {
        private TextBox? _addressTextBox;
        private TextBox? _codeTextBox;
        private Button? _pairButton;
        private Button? _cancelButton;
        private Label? _statusLabel;
        private AppConfig? _config;

        public AdbPairDialog(AppConfig? config = null)
        {
            _config = config;
            InitializeComponent();
            if (_config != null && _addressTextBox != null)
            {
                // Note: PairAddress was removed from AppConfig. 
                // We could add it back if persistent pairing address is desired.
            }
        }

        private void InitializeComponent()
        {
            this.Text = "ADB Wireless Pairing";
            this.Size = new System.Drawing.Size(400, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(241, 243, 244);
            this.ForeColor = Color.FromArgb(32, 33, 36);

            var toolTip = new ToolTip();

            Label headerLabel = new Label
            {
                Text = "Pair New Device",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(350, 30),
                ForeColor = Color.FromArgb(25, 103, 210) // Darker blue #1967D2
            };

            Label addressLabel = new Label
            {
                Text = "Pairing Address (IP:Port):",
                Location = new Point(20, 65),
                Size = new Size(150, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _addressTextBox = new TextBox
            {
                Location = new Point(180, 65),
                Size = new Size(180, 25),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                BorderStyle = BorderStyle.FixedSingle
            };
            toolTip.SetToolTip(_addressTextBox, "Example: 192.168.1.100:37000");

            Label codeLabel = new Label
            {
                Text = "Pairing Code:",
                Location = new Point(20, 100),
                Size = new Size(150, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _codeTextBox = new TextBox
            {
                Location = new Point(180, 100),
                Size = new Size(180, 25),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                BorderStyle = BorderStyle.FixedSingle
            };
            toolTip.SetToolTip(_codeTextBox, "6-digit pairing code shown on device");

            _statusLabel = new Label
            {
                Text = "Ready",
                Location = new Point(20, 135),
                Size = new Size(340, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };

            _pairButton = new Button
            {
                Text = "Pair Device",
                Location = new Point(150, 170),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(25, 103, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _pairButton.FlatAppearance.BorderSize = 0;
            _pairButton.Click += PairButton_Click;

            _cancelButton = new Button
            {
                Text = "Close",
                Location = new Point(260, 170),
                Size = new Size(100, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                FlatStyle = FlatStyle.Flat
            };
            _cancelButton.FlatAppearance.BorderSize = 1;
            _cancelButton.FlatAppearance.BorderColor = Color.FromArgb(32, 33, 36);
            _cancelButton.Click += (s, e) => this.Close();

            this.Controls.Add(headerLabel);
            this.Controls.Add(addressLabel);
            this.Controls.Add(_addressTextBox);
            this.Controls.Add(codeLabel);
            this.Controls.Add(_codeTextBox);
            this.Controls.Add(_statusLabel);
            this.Controls.Add(_pairButton);
            this.Controls.Add(_cancelButton);
        }

        private async void PairButton_Click(object? sender, EventArgs e)
        {
            if (_addressTextBox == null || _codeTextBox == null) return;

            string address = _addressTextBox.Text.Trim();
            string code = _codeTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Please enter both Address and Pairing Code.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_pairButton != null) _pairButton.Enabled = false;
            if (_statusLabel != null)
            {
                _statusLabel.Text = "Pairing...";
                _statusLabel.ForeColor = Color.FromArgb(26, 115, 232);
            }

            try
            {
                string adbPath = ADBPathResolver.FindAdbExecutable();
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = adbPath;
                process.StartInfo.Arguments = $"pair {address} {code}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                await System.Threading.Tasks.Task.Run(() => {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    this.Invoke((MethodInvoker)delegate {
                        if (process.ExitCode == 0)
                        {
                            if (_statusLabel != null)
                            {
                                _statusLabel.Text = "Success!";
                                _statusLabel.ForeColor = Color.FromArgb(24, 128, 56);
                            }
                            // if (_config != null) _config.PairAddress = address; // Property removed
                            MessageBox.Show($"Successfully paired with {address}\n\n{output}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            if (_statusLabel != null)
                            {
                                _statusLabel.Text = "Failed";
                                _statusLabel.ForeColor = Color.FromArgb(217, 48, 37);
                            }
                            MessageBox.Show($"Failed to pair: {error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running adb pair: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (_pairButton != null) _pairButton.Enabled = true;
            }
        }
    }
}
