using ScrcpyController.Core;
using System.ComponentModel;

namespace ScrcpyController.UI
{
    /// <summary>
    /// Main application form
    /// </summary>
    public partial class MainForm : Form, IProcessEventListener, IDeviceConnectionListener, IDisposable
    {
        // Save all current config from UI to config.json
        private void SaveAllConfigToFile()
        {
            if (_configManager == null) return;

            // Device selection
            if (_deviceComboBox?.SelectedItem != null)
                _configManager.Set("LastSelectedDevice", _deviceComboBox.SelectedItem.ToString());

            // Port
            var portTextBox = _deviceGroupBox.Controls.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "PortTextBox");
            if (portTextBox != null)
                _configManager.Set("Port", portTextBox.Text);

            // IP
            var ipTextBox = _deviceGroupBox.Controls.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "IpTextBox");
            if (ipTextBox != null)
                _configManager.Set("DeviceIpAddress", ipTextBox.Text);

            // Video settings
            if (_bitrateTextBox != null)
                _configManager.Set("Bitrate", _bitrateTextBox.Text);
            if (_framerateNumericUpDown != null)
                _configManager.Set("Framerate", (int)_framerateNumericUpDown.Value);
            if (_fullscreenCheckBox != null)
                _configManager.Set("FullscreenEnabled", _fullscreenCheckBox.Checked);
            if (_noControlCheckBox != null)
                _configManager.Set("NoControlEnabled", _noControlCheckBox.Checked);
            if (_resolutionComboBox?.SelectedItem != null)
                _configManager.Set("VideoResolution", _resolutionComboBox.SelectedItem.ToString());

            // Audio settings
            if (_audioComboBox?.SelectedItem != null)
                _configManager.Set("AudioSource", _audioComboBox.SelectedItem.ToString());

            // Other settings (add as needed)
            // ...

            // Save to file
            _configManager.SaveConfig();
        }
        // Event handler untuk tombol Connect
        private void ConnectButton_Click(object? sender, EventArgs e)
        {
            // Cari TextBox IP dan port
            var ipTextBox = _deviceGroupBox.Controls.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "IpTextBox");
            var portTextBox = _deviceGroupBox.Controls.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "PortTextBox");
            if (ipTextBox == null || portTextBox == null)
            {
                MessageBox.Show("IP address or port input not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string ip = ipTextBox.Text.Trim();
            string portStr = portTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show("Please enter a valid IP address.", "Invalid IP", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Invalid port number. Please enter a value between 1 and 65535.", "Invalid Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Jalankan perintah adb connect ip:port
            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "adb";
                process.StartInfo.Arguments = $"connect {ip}:{port}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                bool exited = process.WaitForExit(5000); // 5 seconds timeout
                string output = string.Empty;
                string error = string.Empty;
                if (exited)
                {
                    try
                    {
                        output = process.StandardOutput.ReadToEnd();
                        error = process.StandardError.ReadToEnd();
                    }
                    catch (Exception ioex)
                    {
                        MessageBox.Show($"Failed to read adb output: {ioex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (process.ExitCode == 0)
                    {
                        MessageBox.Show($"ADB connect to {ip}:{port} success.\n{output}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to connect to device.\n{error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    try { process.Kill(); } catch { }
                    MessageBox.Show($"Connection to {ip}:{port} timed out after 5 seconds.", "Connection Timeout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error running adb: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private readonly ConfigManager _configManager;
        private readonly DeviceManager _deviceManager;
        private readonly ProcessManager _processManager;
        private bool _isRunning = false;
        private string? _lastConnectedDevice;
        private bool _disposed = false;

        // UI Controls
        private GroupBox _deviceGroupBox = null!;
        private ComboBox _deviceComboBox = null!;
        private Button _refreshButton = null!;
        private Label _deviceStatusLabel = null!;

        private GroupBox _videoGroupBox = null!;
        private Label _bitrateLabel = null!;
        private TextBox _bitrateTextBox = null!;
        private Label _framerateLabel = null!;
            // Event handler untuk tombol Set port number
            private void SetPortButton_Click(object? sender, EventArgs e)
            {
                // Cari TextBox port
                var portTextBox = _deviceGroupBox.Controls.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "PortTextBox");
                if (portTextBox == null)
                {
                    MessageBox.Show("Port number input not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string portStr = portTextBox.Text.Trim();
                if (!int.TryParse(portStr, out int port) || port < 1 || port > 65535)
                {
                    MessageBox.Show("Invalid port number. Please enter a value between 1 and 65535.", "Invalid Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Jalankan perintah adb tcpip <port>
                try
                {
                    var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "adb";
                    process.StartInfo.Arguments = $"tcpip {port}";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        MessageBox.Show($"ADB TCP/IP set to port {port}.\n{output}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Failed to set ADB TCP/IP.\n{error}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error running adb: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        private NumericUpDown _framerateNumericUpDown = null!;
        private CheckBox _fullscreenCheckBox = null!;
        private CheckBox _autoReconnectCheckBox = null!;
        private CheckBox _noControlCheckBox = null!;
        private Label _resolutionLabel = null!;
        private ComboBox _resolutionComboBox = null!;

        private GroupBox _audioGroupBox = null!;
        private ComboBox _audioComboBox = null!;

        private GroupBox _controlGroupBox = null!;
        private Button _startStopButton = null!;
        private Label _statusLabel = null!;

        public MainForm()
        {
            Console.WriteLine("MainForm constructor start");

            // Initialize managers
            _configManager = new ConfigManager();
            _deviceManager = new DeviceManager();
            _processManager = new ProcessManager(deviceId => _deviceManager.IsDeviceConnected(deviceId));

            // Add listeners
            _deviceManager.AddListener(this);
            _processManager.AddListener(this);

            Console.WriteLine("Managers initialized");

            InitializeComponent();
            Console.WriteLine("InitializeComponent done");

            LoadConfiguration();
            Console.WriteLine("LoadConfiguration done");

            // Start device monitoring
            _deviceManager.StartMonitoring();
            Console.WriteLine("StartMonitoring done");
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            // Form settings
            Text = "Scrcpy Controller";
            Size = new Size(400, 793); // 610 + 30% = 793
            MinimumSize = new Size(400, 793);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            // The icon is already set by the ApplicationIcon property in the project file
            // No need to manually load it here

            // Create UI sections
            CreateDeviceSection();
            CreateVideoSection();
            CreateAudioSection();
            CreateControlSection();

            // Layout sections
            LayoutSections();

            ResumeLayout(false);
            PerformLayout();
        }

        private void CreateDeviceSection()
        {
            _deviceGroupBox = new GroupBox
            {
                Text = "Device Selection",
                Location = new Point(20, 15), // Center horizontally with more margin
                Size = new Size(340, 170), // Slightly narrower and taller for balance
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _deviceComboBox = new ComboBox
            {
                Location = new Point(20, 25),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _deviceComboBox.DrawItem += ComboBox_DrawItem;
            _deviceComboBox.SelectedIndexChanged += DeviceComboBox_SelectedIndexChanged;

            _refreshButton = new Button
            {
                Text = "Refresh",
                Location = new Point(230, 25),
                Size = new Size(90, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _refreshButton.Click += RefreshButton_Click;

            _deviceStatusLabel = new Label
            {
                Text = "Scanning for devices...",
                Location = new Point(10, 60), // Move lower for visibility
                Size = new Size(320, 32), // Increase height and width for longer messages
                ForeColor = Color.Red,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            // Wireless Debugging Section
            // Layout agar rata tengah dan rapi
            int portRowY = 100; // Move port row lower for more space
            int groupWidth = _deviceGroupBox.Size.Width;
            int labelWidth = 110;
            int textWidth = 70;
            int buttonWidth = 110;
            int spacing = 8;
            int totalWidth = labelWidth + spacing + textWidth + spacing + buttonWidth;
            int startX = (_deviceGroupBox.Size.Width - totalWidth) / 2;

            var portLabel = new Label
            {
                Text = "ADB TCP/IP Port:",
                Location = new Point(startX, portRowY),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            var portTextBox = new TextBox
            {
                Name = "PortTextBox",
                Location = new Point(startX + labelWidth + spacing, portRowY),
                Size = new Size(textWidth, 23),
                Text = "5037",
                TextAlign = HorizontalAlignment.Center
            };
            portTextBox.TextChanged += (s, e) => {
                if (_configManager != null)
                    _configManager.Config.PortNumber = portTextBox.Text;
            };

            var setPortButton = new Button
            {
                Text = "Set port number",
                Location = new Point(startX + labelWidth + spacing + textWidth + spacing, portRowY),
                Size = new Size(buttonWidth, 23)
            };
            setPortButton.Click += SetPortButton_Click;

            _deviceGroupBox.Controls.AddRange(new Control[] {
                _deviceComboBox, _refreshButton, _deviceStatusLabel,
                portLabel, portTextBox, setPortButton
            });
            // IP Address and Connect Button Section
            int ipRowY = portRowY + 40; // Increase vertical gap between port and IP rows
            var ipLabel = new Label
            {
                Text = "Device IP Address:",
                Location = new Point(startX, ipRowY),
                Size = new Size(labelWidth, 23),
                TextAlign = ContentAlignment.MiddleRight
            };

            var ipTextBox = new TextBox
            {
                Name = "IpTextBox",
                Location = new Point(startX + labelWidth + spacing, ipRowY),
                Size = new Size(textWidth, 23),
                Text = "192.168.1.100",
                TextAlign = HorizontalAlignment.Center
            };
            ipTextBox.TextChanged += (s, e) => {
                if (_configManager != null)
                    _configManager.Config.DeviceIpAddress = ipTextBox.Text;
            };

            var connectButton = new Button
            {
                Text = "Connect",
                Location = new Point(startX + labelWidth + spacing + textWidth + spacing, ipRowY),
                Size = new Size(buttonWidth, 23)
            };
            connectButton.Click += ConnectButton_Click;

            _deviceGroupBox.Controls.AddRange(new Control[] { ipLabel, ipTextBox, connectButton });
            Controls.Add(_deviceGroupBox);
        }

        private void CreateVideoSection()
        {
            _videoGroupBox = new GroupBox
            {
                Text = "Video Settings",
                Location = new Point(20, 200), // Center horizontally below device section
                Size = new Size(340, 210), // Match device section width
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Resolution section
            // Margin atas agar tidak terpotong
            int marginTop = 40;
            int spacingY = 20;
            // Resolution (label dan ComboBox) satu baris sendiri
            _resolutionLabel = new Label
            {
                Text = "Resolution",
                Location = new Point(20, marginTop),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _resolutionComboBox = new ComboBox
            {
                Location = new Point(130, marginTop),
                Size = new Size(210, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed
            };

            // Bitrate dan Max FPS di satu baris, di bawah Resolution
            int bitrateRowY = marginTop + 35 + spacingY;
            _bitrateLabel = new Label
            {
                Text = "Bitrate (Mbps)",
                Location = new Point(20, bitrateRowY),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _bitrateTextBox = new TextBox
            {
                Location = new Point(130, bitrateRowY),
                Size = new Size(60, 23),
                Text = "20",
                TextAlign = HorizontalAlignment.Center
            };
            _bitrateTextBox.TextChanged += BitrateTextBox_TextChanged;

            _framerateLabel = new Label
            {
                Text = "Max FPS",
                Location = new Point(210, bitrateRowY),
                Size = new Size(60, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            _framerateNumericUpDown = new NumericUpDown
            {
                Location = new Point(280, bitrateRowY),
                Size = new Size(60, 23),
                Minimum = 1,
                Maximum = 240,
                Value = 60,
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(0)
            };
            _resolutionComboBox.Items.AddRange(new[] { "Device Resolution", "720p", "1080p", "4K" });
            _resolutionComboBox.SelectedIndex = 0;
            _resolutionComboBox.DrawItem += ComboBox_DrawItem;
            _resolutionComboBox.SelectedIndexChanged += ResolutionComboBox_SelectedIndexChanged;

            // Bitrate section
            _bitrateLabel = new Label
            {
                Text = "Bitrate (Mbps)",
                Location = new Point(60, 80),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _bitrateTextBox = new TextBox
            {
                Location = new Point(60, 105),
                Size = new Size(80, 23),
                Text = "20",
                TextAlign = HorizontalAlignment.Center
            };
            _bitrateTextBox.TextChanged += BitrateTextBox_TextChanged;

            // Framerate section
            _framerateLabel = new Label
            {
                Text = "Max FPS",
                Location = new Point(220, 80),
                Size = new Size(60, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _framerateNumericUpDown = new NumericUpDown
            {
                Location = new Point(220, 105),
                Size = new Size(60, 23),
                Minimum = 1,
                Maximum = 240,
                Value = 60,
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(0)
            };

            // Hide the up-down arrows completely
            if (_framerateNumericUpDown.Controls.Count > 0)
            {
                var buttons = _framerateNumericUpDown.Controls[0];
                buttons.Size = new Size(0, 0);
                buttons.Visible = false;
                buttons.Enabled = false;
            }

            // Ensure proper text centering with layout event
            _framerateNumericUpDown.Layout += (s, e) =>
            {
                // Hide buttons again in case they reappear
                if (_framerateNumericUpDown.Controls.Count > 0)
                {
                    var btns = _framerateNumericUpDown.Controls[0];
                    btns.Size = new Size(0, 0);
                    btns.Visible = false;
                    btns.Enabled = false;
                }

                // Position the TextBox to take full client area with precise centering
                if (_framerateNumericUpDown.Controls.Count > 1 && _framerateNumericUpDown.Controls[1] is TextBox tb)
                {
                    tb.TextAlign = HorizontalAlignment.Center;
                    tb.Location = new Point(0, 0);
                    tb.Size = new Size(_framerateNumericUpDown.ClientSize.Width, _framerateNumericUpDown.ClientSize.Height);
                    tb.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
                    tb.Margin = new Padding(0);
                    tb.Padding = new Padding(0);
                    // Force the text box to redraw with centered text
                    tb.Invalidate();
                }
            };

            // Force initial layout
            _framerateNumericUpDown.PerformLayout();

            _framerateNumericUpDown.ValueChanged += FramerateNumericUpDown_ValueChanged;

            _fullscreenCheckBox = new CheckBox
            {
                Text = "Fullscreen Mode",
                Location = new Point(30, 130),
                Size = new Size(120, 20)
            };
            _fullscreenCheckBox.CheckedChanged += FullscreenCheckBox_CheckedChanged;

            _autoReconnectCheckBox = new CheckBox
            {
                Text = "Auto Reconnect",
                Location = new Point(160, 130),
                Size = new Size(120, 20)
            };
            _autoReconnectCheckBox.CheckedChanged += AutoReconnectCheckBox_CheckedChanged;

            _noControlCheckBox = new CheckBox
            {
                Text = "Disable Control",
                Location = new Point(30, 155),
                Size = new Size(120, 20)
            };
            _noControlCheckBox.CheckedChanged += NoControlCheckBox_CheckedChanged;

            _videoGroupBox.Controls.AddRange(new Control[]
            {
                _bitrateLabel, _bitrateTextBox, _framerateLabel, _framerateNumericUpDown,
                _resolutionLabel, _resolutionComboBox,
                _fullscreenCheckBox, _autoReconnectCheckBox, _noControlCheckBox
            });

            Controls.Add(_videoGroupBox);
        }

        private void CreateAudioSection()
        {
            _audioGroupBox = new GroupBox
            {
                Text = "Audio Settings",
                Location = new Point(20, 420), // Center horizontally below video section
                Size = new Size(340, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var audioLabel = new Label
            {
                Text = "Audio Source",
                Location = new Point(130, 15), // Centered horizontally (360/2 - 100/2 = 130)
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _audioComboBox = new ComboBox
            {
                Location = new Point(80, 40), // Centered below the label
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            _audioComboBox.Items.AddRange(new[] { "Audio Playback", "Microphone", "No audio" });
            _audioComboBox.SelectedIndex = 0;
            _audioComboBox.DrawItem += ComboBox_DrawItem;
            _audioComboBox.SelectedIndexChanged += AudioComboBox_SelectedIndexChanged;

            _audioGroupBox.Controls.AddRange(new Control[] { audioLabel, _audioComboBox });
            Controls.Add(_audioGroupBox);
        }

        private void CreateControlSection()
        {
            _controlGroupBox = new GroupBox
            {
                Text = "",
                Location = new Point(20, 510), // Center horizontally below audio section
                Size = new Size(340, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _startStopButton = new Button
            {
                Text = "Start Mirror",
                Location = new Point(10, 25),
                Size = new Size(340, 60),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _startStopButton.FlatAppearance.BorderSize = 0;
            _startStopButton.Click += StartStopButton_Click;

            _statusLabel = new Label
            {
                Text = "Ready to start mirroring",
                Location = new Point(10, 85),
                Size = new Size(340, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Black,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _controlGroupBox.Controls.AddRange(new Control[] { _startStopButton, _statusLabel });
            Controls.Add(_controlGroupBox);
        }

        private void LayoutSections()
        {
            // All sections are positioned in CreateXXXSection methods
            // This method can be used for additional layout adjustments if needed
        }

        private void LoadConfiguration()
        {
            try
            {
                bool configLoaded = _configManager?.LoadConfig() ?? false;
                var config = _configManager?.Config;
                if (config == null)
                {
                    System.Diagnostics.Debug.WriteLine("Config is null, using defaults");
                    RefreshDevices();
                    return;
                }

                // Load port number from config
                var portTextBox = _deviceGroupBox.Controls.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "PortTextBox");
                if (portTextBox != null)
                    portTextBox.Text = config.PortNumber ?? "5037";

                // Load UI from configuration with null checks
                if (_bitrateTextBox != null)
                    _bitrateTextBox.Text = config.Bitrate ?? "20";
                
                if (_framerateNumericUpDown != null)
                    _framerateNumericUpDown.Value = Math.Max(1, Math.Min(240, config.Framerate));
                
                if (_fullscreenCheckBox != null)
                    _fullscreenCheckBox.Checked = config.FullscreenEnabled;

                if (_autoReconnectCheckBox != null)
                    _autoReconnectCheckBox.Checked = config.AutoReconnectEnabled;

                if (_noControlCheckBox != null)
                    _noControlCheckBox.Checked = config.NoControlEnabled;

                if (_resolutionComboBox != null)
                {
                    int index = _resolutionComboBox.Items.IndexOf(config.VideoResolution);
                    if (index >= 0)
                        _resolutionComboBox.SelectedIndex = index;
                }

                // Set audio source safely
                if (_audioComboBox != null)
                {
                    int audioIndex = (config.AudioSource ?? "Audio Playback") switch
                    {
                        "Audio Playback" => 0,
                        "Microphone" => 1,
                        "No audio" => 2,
                        _ => 0
                    };
                    
                    // Ensure index is within bounds
                    if (audioIndex >= 0 && audioIndex < _audioComboBox.Items.Count)
                        _audioComboBox.SelectedIndex = audioIndex;
                }

                // Configure auto-reconnect safely
                _processManager?.EnableAutoReconnect(config.AutoReconnectEnabled, config.ReconnectMaxAttempts, config.ReconnectDelay);

                // Try to restore last selected device
                if (!string.IsNullOrEmpty(config.LastSelectedDevice))
                {
                    _lastConnectedDevice = config.LastSelectedDevice;
                }

                // Load IP address from config
                var ipTextBox = _deviceGroupBox.Controls.OfType<TextBox>().FirstOrDefault(tb => tb.Name == "IpTextBox");
                if (ipTextBox != null)
                    ipTextBox.Text = config.DeviceIpAddress ?? "192.168.1.100";

                RefreshDevices();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
                // Continue with defaults
                RefreshDevices();
            }
        }

        private async void RefreshDevices()
        {
            try
            {
                // Ensure thread safety for UI updates
                if (InvokeRequired)
                {
                    Invoke(new Action(() => _ = RefreshDevicesAsync()));
                    return;
                }

                await RefreshDevicesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in RefreshDevices: {ex.Message}");
                // Ensure thread safety for error updates
                if (InvokeRequired)
                {
                    Invoke(new Action(() => 
                    {
                        _deviceStatusLabel.Text = $"Error scanning devices: {ex.Message}";
                        _deviceStatusLabel.ForeColor = Color.Red;
                    }));
                }
                else
                {
                    _deviceStatusLabel.Text = $"Error scanning devices: {ex.Message}";
                    _deviceStatusLabel.ForeColor = Color.Red;
                }
            }
        }

        private async Task RefreshDevicesAsync()
        {
            try
            {
                _deviceStatusLabel.Text = "Scanning for devices...";
                _deviceStatusLabel.ForeColor = Color.Gray;

                await _deviceManager.RefreshDevicesAsync();
                
                UpdateDeviceListUI();
            }
            catch (Exception ex)
            {
                _deviceStatusLabel.Text = $"Error scanning devices: {ex.Message}";
                _deviceStatusLabel.ForeColor = Color.Red;
                throw; // Re-throw for caller handling
            }
        }

        private void UpdateDeviceListUI()
        {
            try
            {
                // Ensure thread safety for UI updates
                if (InvokeRequired)
                {
                    Invoke(new Action(UpdateDeviceListUI));
                    return;
                }

                var devices = _deviceManager?.CurrentDevices;
                if (devices == null)
                {
                    _deviceStatusLabel.Text = "Device manager not available";
                    _deviceStatusLabel.ForeColor = Color.Red;
                    return;
                }

                var connectedDevices = devices.Where(d => d.IsConnected).ToList();
                
                // Remember current selection
                string? currentSelection = GetSelectedDeviceId();

                _deviceComboBox.Items.Clear();

                if (connectedDevices.Any())
                {
                    // Add devices directly without header
                    foreach (var device in connectedDevices)
                    {
                        _deviceComboBox.Items.Add(device.ToString());
                    }

                    _deviceStatusLabel.Text = $"Found {connectedDevices.Count} device(s)";
                    _deviceStatusLabel.ForeColor = Color.Green;

                    // Implement automatic device selection logic
                    bool selectionMade = PerformAutomaticDeviceSelection(connectedDevices, currentSelection);
                    
                    if (!selectionMade && connectedDevices.Count > 0)
                    {
                        // Fallback to first device instead of header
                        _deviceComboBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    // According to user memory: empty field with no placeholder text when no devices
                    _deviceComboBox.Items.Clear(); // Completely empty as per user preference
                    _deviceStatusLabel.Text = "No devices connected." + Environment.NewLine + "Enable USB debugging and connect device.";
                    _deviceStatusLabel.ForeColor = Color.Red;
                    // Don't set any selected index - leave completely empty
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => 
                        {
                            _deviceStatusLabel.Text = $"Error updating device list: {ex.Message}";
                            _deviceStatusLabel.ForeColor = Color.Red;
                        }));
                    }
                    else
                    {
                        _deviceStatusLabel.Text = $"Error updating device list: {ex.Message}";
                        _deviceStatusLabel.ForeColor = Color.Red;
                    }
                }
                catch
                {
                    // Ignore nested exception handling to prevent crashes
                    System.Diagnostics.Debug.WriteLine($"Critical error in UpdateDeviceListUI: {ex.Message}");
                }
            }
        }

        private bool PerformAutomaticDeviceSelection(List<DeviceInfo> connectedDevices, string? currentSelection)
        {
            try
            {
                // Rule 1: If only 1 device connected, automatically select it
                if (connectedDevices.Count == 1)
                {
                    var singleDevice = connectedDevices[0];
                    _deviceComboBox.SelectedItem = singleDevice.ToString();
                    System.Diagnostics.Debug.WriteLine($"Auto-selected single device: {singleDevice.DeviceId}");
                    return true;
                }

                // Rule 2: If current selection is still available, keep it
                if (!string.IsNullOrEmpty(currentSelection))
                {
                    var currentDevice = connectedDevices.FirstOrDefault(d => d.DeviceId == currentSelection);
                    if (currentDevice != null)
                    {
                        _deviceComboBox.SelectedItem = currentDevice.ToString();
                        System.Diagnostics.Debug.WriteLine($"Keeping current selection: {currentDevice.DeviceId}");
                        return true;
                    }
                }

                // Rule 3: If multiple devices and current selection not available,
                // try to restore last connected device from previous session
                if (!string.IsNullOrEmpty(_lastConnectedDevice))
                {
                    var lastDevice = connectedDevices.FirstOrDefault(d => d.DeviceId == _lastConnectedDevice);
                    if (lastDevice != null)
                    {
                        _deviceComboBox.SelectedItem = lastDevice.ToString();
                        System.Diagnostics.Debug.WriteLine($"Restored last connected device: {lastDevice.DeviceId}");
                        return true;
                    }
                }

                // Rule 4: If multiple devices and no previous preference,
                // automatically select the first device (skip "Connected Devices:" header)
                if (connectedDevices.Count > 1)
                {
                    var firstDevice = connectedDevices[0];
                    _deviceComboBox.SelectedItem = firstDevice.ToString();
                    System.Diagnostics.Debug.WriteLine($"Auto-selected first device: {firstDevice.DeviceId}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in automatic device selection: {ex.Message}");
                return false;
            }
        }

        private bool ValidateVideoSettings()
        {
            var (isValid, errorMessage) = _configManager.ValidateField("Bitrate", _bitrateTextBox.Text);
            if (!isValid)
            {
                MessageBox.Show($"Invalid bitrate: {errorMessage}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private string? GetSelectedDeviceId()
        {
            try
            {
                if (_deviceComboBox?.SelectedItem == null || 
                    _deviceComboBox.Items.Count == 0)
                    return null;

                var selectedText = _deviceComboBox.SelectedItem.ToString();
                if (string.IsNullOrEmpty(selectedText))
                    return null;

                var devices = _deviceManager?.CurrentDevices?.Where(d => d?.IsConnected == true).ToList();
                if (devices == null || !devices.Any())
                    return null;
                
                foreach (var device in devices)
                {
                    if (device?.ToString() == selectedText)
                        return device.DeviceId;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting selected device ID: {ex.Message}");
                return null;
            }
        }

        #region Event Handlers

        private void DeviceComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                var deviceId = GetSelectedDeviceId();
                if (!string.IsNullOrEmpty(deviceId) && _configManager != null)
                {
                    _configManager.Set("LastSelectedDevice", deviceId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in device selection change: {ex.Message}");
            }
        }

        private void RefreshButton_Click(object? sender, EventArgs e)
        {
            try
            {
                RefreshDevices();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in refresh button click: {ex.Message}");
                MessageBox.Show($"Error refreshing devices: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BitrateTextBox_TextChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_configManager != null && _bitrateTextBox != null)
                {
                    _configManager.Set("Bitrate", _bitrateTextBox.Text ?? "");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating bitrate: {ex.Message}");
            }
        }

        private void FramerateNumericUpDown_ValueChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_configManager != null && _framerateNumericUpDown != null)
                {
                    _configManager.Set("Framerate", (int)_framerateNumericUpDown.Value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating framerate: {ex.Message}");
            }
        }

        private void FullscreenCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_configManager != null && _fullscreenCheckBox != null)
                {
                    _configManager.Set("FullscreenEnabled", _fullscreenCheckBox.Checked);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating fullscreen setting: {ex.Message}");
            }
        }

        private void AutoReconnectCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_configManager != null && _autoReconnectCheckBox != null && _processManager != null)
                {
                    bool enabled = _autoReconnectCheckBox.Checked;
                    _configManager.Set("AutoReconnectEnabled", enabled);
                    _processManager.EnableAutoReconnect(enabled, 0); // Unlimited attempts
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating auto-reconnect setting: {ex.Message}");
            }
        }

        private void NoControlCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_configManager != null && _noControlCheckBox != null)
                {
                    _configManager.Set("NoControlEnabled", _noControlCheckBox.Checked);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating no-control setting: {ex.Message}");
            }
        }

        private void ResolutionComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_configManager != null && _resolutionComboBox != null && _resolutionComboBox.SelectedItem != null)
                {
                    _configManager.Set("VideoResolution", _resolutionComboBox.SelectedItem.ToString()!);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating video resolution: {ex.Message}");
            }
        }

        private void AudioComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                if (_audioComboBox?.SelectedItem != null && _configManager != null)
                {
                    var selectedAudio = _audioComboBox.SelectedItem.ToString();
                    if (!string.IsNullOrEmpty(selectedAudio))
                    {
                        _configManager.Set("AudioSource", selectedAudio);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating audio source: {ex.Message}");
            }
        }

        private void ComboBox_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                e.DrawBackground();
                
                if (e.Index >= 0 && e.Index < comboBox.Items.Count)
                {
                    string text = comboBox.Items[e.Index]?.ToString() ?? string.Empty;
                    TextRenderer.DrawText(e.Graphics, text, e.Font, e.Bounds, e.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                
                e.DrawFocusRectangle();
            }
        }

        private async void StartStopButton_Click(object? sender, EventArgs e)
        {
            if (_isRunning)
            {
                await StopScrcpyAsync();
            }
            else
            {
                await StartScrcpyAsync();
            }
        }

        #endregion

        #region Scrcpy Control

        private async Task StartScrcpyAsync()
        {
            if (_isRunning)
                return;

            try
            {
                var deviceId = GetSelectedDeviceId();
                var availableDevices = _deviceManager.GetDeviceIds();

                // Validate device selection
                var (isValid, errorMsg) = DeviceValidator.ValidateDeviceSelection(deviceId, availableDevices);
                if (!isValid)
                {
                    MessageBox.Show(errorMsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Validate video settings
                if (!ValidateVideoSettings())
                    return;

                // Store last connected device for auto-reconnect
                _lastConnectedDevice = deviceId;
                _configManager.Set("LastSelectedDevice", deviceId!);

                // Get audio source
                string audioSourceInternal = _configManager.Config.GetAudioSourceInternal();

                // Create scrcpy configuration
                var config = new ScrcpyConfig(deviceId!)
                {
                    Bitrate = $"{_bitrateTextBox.Text}M",
                    Framerate = (int)_framerateNumericUpDown.Value,
                    Fullscreen = _fullscreenCheckBox.Checked,
                    NoControl = _noControlCheckBox.Checked,
                    VideoResolution = _resolutionComboBox?.SelectedItem?.ToString() ?? "Original Device Resolution",
                    AudioSource = audioSourceInternal
                };

                // Check if skip process check is enabled
                bool skipCheck = _configManager.Config.SkipProcessCheck;

                // Try to start the process
                await _processManager.StartProcessAsync(config, skipProcessCheck: skipCheck);
            }
            catch (InvalidOperationException ex)
            {
                // Handle process detection errors
                var result = MessageBox.Show(
                    $"{ex.Message}\n\nChoose an option:\n" +
                    "• Yes: Force start (kill existing processes)\n" +
                    "• No: Skip process check and start anyway\n" +
                    "• Cancel: Don't start",
                    "Scrcpy Process Detected",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        // Force start
                        var deviceId = GetSelectedDeviceId();
                        string audioSourceInternal = _configManager.Config.GetAudioSourceInternal();
                        
                        var config = new ScrcpyConfig(deviceId!)
                        {
                            Bitrate = $"{_bitrateTextBox.Text}M",
                            Framerate = (int)_framerateNumericUpDown.Value,
                            Fullscreen = _fullscreenCheckBox.Checked,
                            NoControl = _noControlCheckBox.Checked,
                            VideoResolution = _resolutionComboBox?.SelectedItem?.ToString() ?? "Original Device Resolution",
                            AudioSource = audioSourceInternal
                        };

                        await _processManager.StartProcessAsync(config, forceStart: true);
                    }
                    catch (Exception forceEx)
                    {
                        MessageBox.Show($"Failed to force start: {forceEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (result == DialogResult.No)
                {
                    try
                    {
                        // Skip process check
                        _configManager.Set("SkipProcessCheck", true);
                        
                        var deviceId = GetSelectedDeviceId();
                        string audioSourceInternal = _configManager.Config.GetAudioSourceInternal();
                        
                        var config = new ScrcpyConfig(deviceId!)
                        {
                            Bitrate = $"{_bitrateTextBox.Text}M",
                            Framerate = (int)_framerateNumericUpDown.Value,
                            Fullscreen = _fullscreenCheckBox.Checked,
                            NoControl = _noControlCheckBox.Checked,
                            VideoResolution = _resolutionComboBox?.SelectedItem?.ToString() ?? "Original Device Resolution",
                            AudioSource = audioSourceInternal
                        };

                        await _processManager.StartProcessAsync(config, skipProcessCheck: true);
                    }
                    catch (Exception skipEx)
                    {
                        MessageBox.Show($"Failed to start: {skipEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start mirroring: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task StopScrcpyAsync(bool isFormClosing = false)
        {
            try
            {
                // Check if we're in auto-reconnect mode
                bool wasInAutoReconnect = _isRunning && _configManager.Config.AutoReconnectEnabled;
                
                await _processManager.StopProcessAsync();
                
                // If we manually stopped during auto-reconnect, reset UI completely
                if (wasInAutoReconnect)
                {
                    _isRunning = false;
                    if (!isFormClosing) // Only update UI if not closing the form
                    {
                        _startStopButton.Text = "Start Mirror";
                        _startStopButton.BackColor = Color.FromArgb(37, 99, 235);
                        _statusLabel.Text = "Auto-reconnect stopped by user";
                        _statusLabel.ForeColor = Color.Black;
                        
                        // Clear status message after 3 seconds
                        var timer = new System.Windows.Forms.Timer { Interval = 3000 };
                        timer.Tick += (s, e) =>
                        {
                            timer.Stop();
                            timer.Dispose();
                            if (!_isRunning)
                            {
                                _statusLabel.Text = "Ready to start mirroring";
                            }
                        };
                        timer.Start();
                    }
                }
                else if (!isFormClosing) // Only update UI if not closing the form
                {
                    _isRunning = false;
                    _startStopButton.Text = "Start Mirror";
                    _startStopButton.BackColor = Color.FromArgb(37, 99, 235);
                    _statusLabel.Text = "Ready to start mirroring";
                    _statusLabel.ForeColor = Color.Black;
                }
            }
            catch (Exception ex)
            {
                if (!isFormClosing) // Only show message box if not closing the form
                {
                    MessageBox.Show($"Error stopping mirroring: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task StopScrcpyAsync()
        {
            await StopScrcpyAsync(false);
        }

        #endregion

        #region IProcessEventListener Implementation

        public void OnProcessStarted(ScrcpyConfig config)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnProcessStarted(config)));
                return;
            }

            _isRunning = true;
            _startStopButton.Text = "Stop Mirror";
            _startStopButton.BackColor = Color.FromArgb(220, 38, 38);
            _statusLabel.Text = $"Mirroring Active • Device: {config.DeviceId} • {config.Bitrate} • {config.Framerate}FPS";
            _statusLabel.ForeColor = Color.Green;
        }

        public void OnProcessStopped(int? exitCode)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnProcessStopped(exitCode)));
                return;
            }

            bool autoReconnectEnabled = _configManager.Config.AutoReconnectEnabled;

            // If exit code is 0 (normal termination) or auto-reconnect is disabled, reset UI
            if (exitCode == 0 || !autoReconnectEnabled || string.IsNullOrEmpty(_lastConnectedDevice))
            {
                _isRunning = false;
                _startStopButton.Text = "Start Mirror";
                _startStopButton.BackColor = Color.FromArgb(37, 99, 235);

                if (exitCode == 0)
                {
                    _statusLabel.Text = "Ready to start mirroring (window closed by user)";
                }
                else
                {
                    _statusLabel.Text = "Ready to start mirroring";
                }
                _statusLabel.ForeColor = Color.Black;
            }
            else
            {
                // Process crashed and auto-reconnect is enabled - keep running state but allow stopping
                // Keep the Stop button functional to allow canceling auto-reconnect
                _statusLabel.Text = $"Process stopped unexpectedly (exit code: {exitCode}) - auto-reconnect will attempt to restart";
                _statusLabel.ForeColor = Color.Orange;
            }
        }

        public void OnProcessError(Exception error)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnProcessError(error)));
                return;
            }

            bool autoReconnectEnabled = _configManager.Config.AutoReconnectEnabled;
            if (!autoReconnectEnabled)
            {
                MessageBox.Show($"Process error: {error.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                _statusLabel.Text = $"Process error during auto-reconnect: {error.Message}";
                _statusLabel.ForeColor = Color.Red;
            }
        }

        public void OnReconnectAttempt(int attempt)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnReconnectAttempt(attempt)));
                return;
            }

            _statusLabel.Text = $"Auto-reconnecting... (attempt {attempt})";
            _statusLabel.ForeColor = Color.Orange;
        }

        public void OnReconnectSuccess()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnReconnectSuccess()));
                return;
            }

            _statusLabel.Text = "Device reconnected successfully!";
            _statusLabel.ForeColor = Color.Green;

            // Ensure UI is in running state
            _isRunning = true;
            _startStopButton.Text = "Stop Mirror";
            _startStopButton.BackColor = Color.FromArgb(220, 38, 38);

            // Clear status after 3 seconds
            var timer = new System.Windows.Forms.Timer { Interval = 3000 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
                if (_isRunning && !string.IsNullOrEmpty(_lastConnectedDevice))
                {
                    _statusLabel.Text = $"Mirroring Active • Device: {_lastConnectedDevice}";
                    _statusLabel.ForeColor = Color.Green;
                }
            };
            timer.Start();
        }

        #endregion

        #region IDeviceConnectionListener Implementation

        public void OnDevicesChanged(List<string> devices)
        {
            // Always update UI when devices change (for automatic refresh)
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDevicesChanged(devices)));
                return;
            }

            // Check if currently selected device was disconnected
            string? currentlySelected = GetSelectedDeviceId();
            bool currentDeviceDisconnected = !string.IsNullOrEmpty(currentlySelected) && !devices.Contains(currentlySelected);
            
            if (currentDeviceDisconnected)
            {
                System.Diagnostics.Debug.WriteLine($"Currently selected device {currentlySelected} disconnected, will auto-select next available device");
            }

            // Update the device list in UI (this will trigger automatic selection)
            UpdateDeviceListUI();

            // Check if last connected device was disconnected during mirroring
            if (_isRunning && !string.IsNullOrEmpty(_lastConnectedDevice) && !devices.Contains(_lastConnectedDevice))
            {
                System.Diagnostics.Debug.WriteLine($"Device {_lastConnectedDevice} disconnected during mirroring");
                // Auto-reconnect will be handled by ProcessManager
            }
        }

        public void OnDeviceConnected(string deviceId)
        {
            // If this is the device we were waiting for, auto-reconnect should handle it
            if (_lastConnectedDevice == deviceId && !_isRunning && _configManager.Config.AutoReconnectEnabled)
            {
                System.Diagnostics.Debug.WriteLine($"Previously connected device {deviceId} reconnected");
            }
        }

        public void OnDeviceDisconnected(string deviceId)
        {
            if (deviceId == _lastConnectedDevice && _isRunning)
            {
                System.Diagnostics.Debug.WriteLine($"Currently used device {deviceId} disconnected");
            }
        }

        #endregion

        #region Form Events

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (_isRunning)
                {
                    // Cancel the closing event temporarily to allow async operation
                    e.Cancel = true;
                    
                    // Run the stop process asynchronously
                    await StopScrcpyAsync(true); // Pass true to indicate form is closing
                }
                
                // Properly unsubscribe from all events to prevent memory leaks
                if (_deviceManager != null)
                {
                    _deviceManager.RemoveListener(this);
                    _deviceManager.StopMonitoring();
                }
                
                if (_processManager != null)
                {
                    _processManager.RemoveListener(this);
                }
                
                // Save config safely
                try
                {
                    _configManager?.SaveConfig();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving config during close: {ex.Message}");
                }
                
                // Dispose of resources
                Dispose(true);
                
                // Now allow the form to close
                e.Cancel = false;
                base.OnFormClosing(e);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during form closing: {ex.Message}");
                // Don't prevent closing even if there's an error
                e.Cancel = false;
                base.OnFormClosing(e);
            }
        }

        /// <summary>
        /// Properly dispose of all resources and prevent memory leaks
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    // Unsubscribe from events first
                    if (_deviceManager != null)
                    {
                        _deviceManager.RemoveListener(this);
                        _deviceManager.StopMonitoring();
                        _deviceManager.Dispose();
                    }
                    
                    if (_processManager != null)
                    {
                        _processManager.RemoveListener(this);
                        _processManager.Dispose();
                    }
                    
                    // Dispose UI components
                    _deviceGroupBox?.Dispose();
                    _deviceComboBox?.Dispose();
                    _refreshButton?.Dispose();
                    _deviceStatusLabel?.Dispose();
                    _videoGroupBox?.Dispose();
                    _bitrateLabel?.Dispose();
                    _bitrateTextBox?.Dispose();
                    _framerateLabel?.Dispose();
                    _framerateNumericUpDown?.Dispose();
                    _fullscreenCheckBox?.Dispose();
                    _autoReconnectCheckBox?.Dispose();
                    _resolutionLabel?.Dispose();
                    _resolutionComboBox?.Dispose();
                    _audioGroupBox?.Dispose();
                    _audioComboBox?.Dispose();
                    _controlGroupBox?.Dispose();
                    _startStopButton?.Dispose();
                    _statusLabel?.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during disposal: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
            
            base.Dispose(disposing);
        }

        #endregion
    }
}
