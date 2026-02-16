using ScrcpyController.Core;
using System.ComponentModel;

namespace ScrcpyController.UI
{
    /// <summary>
    /// Main application form
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public partial class MainForm : Form, IProcessEventListener, IDeviceConnectionListener, IDisposable
    {
        // Save all current config from UI to config.json
        private void SaveAllConfigToFile()
        {
            if (_configManager == null) return;

            // Device selection
            if (_deviceComboBox?.SelectedItem != null)
                _configManager.Set("LastSelectedDevice", _deviceComboBox.SelectedItem.ToString());

            // Video settings
            if (_bitrateTextBox != null)
                _configManager.Set("Bitrate", _bitrateTextBox.Text);
            if (_framerateTextBox != null)
            {
                if (!int.TryParse(_framerateTextBox.Text, out int f)) f = 60;
                f = Math.Max(1, Math.Min(240, f));
                _configManager.Set("Framerate", f);
            }
            if (_fullscreenCheckBox != null)
                _configManager.Set("FullscreenEnabled", _fullscreenCheckBox.Checked);
            if (_noControlCheckBox != null)
                _configManager.Set("NoControlEnabled", !_noControlCheckBox.Checked);
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

        // Event handler untuk tombol Pair
        private void PairButton_Click(object? sender, EventArgs e)
        {
            if (_configManager == null) return;
            using (var dialog = new AdbPairDialog(_configManager.Config))
            {
                dialog.ShowDialog(this);
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
        private TextBox _framerateTextBox = null!;
        

        
        private CheckBox _fullscreenCheckBox = null!;
        private CheckBox _autoReconnectCheckBox = null!;
        private CheckBox _noControlCheckBox = null!;
        private ComboBox _resolutionComboBox = null!;

        private GroupBox _audioGroupBox = null!;
        private ComboBox _audioComboBox = null!;

        private Panel _controlGroupBox = null!;
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
            Size = new Size(400, 793);
            MinimumSize = new Size(400, 793);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.FromArgb(241, 243, 244);
            ForeColor = Color.FromArgb(32, 33, 36);
            Font = new Font("Segoe UI", 9);
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
                Location = new Point(20, 15),
                Size = new Size(340, 170),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.FromArgb(25, 103, 210) // Darker Google Blue for better contrast
            };

            _deviceComboBox = new ComboBox
            {
                Location = new Point(20, 25),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                FlatStyle = FlatStyle.Flat
            };
            _deviceComboBox.DrawItem += ComboBox_DrawItem;
            _deviceComboBox.SelectedIndexChanged += DeviceComboBox_SelectedIndexChanged;

            _refreshButton = new Button
            {
                Text = "Refresh",
                Location = new Point(230, 25),
                Size = new Size(90, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                FlatStyle = FlatStyle.Flat
            };
            _refreshButton.FlatAppearance.BorderSize = 1;
            _refreshButton.FlatAppearance.BorderColor = Color.FromArgb(32, 33, 36);
            _refreshButton.Click += RefreshButton_Click;

            _deviceStatusLabel = new Label
            {
                Text = "Scanning for devices...",
                Location = new Point(10, 60), // Move lower for visibility
                Size = new Size(320, 32), // Increase height and width for longer messages
                ForeColor = Color.FromArgb(217, 48, 37), // Google Red for errors
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };

            // Wireless Debugging Section
            int groupWidth = _deviceGroupBox.Size.Width;

            // Row 1: Pair Button (Main)
            int row1Y = 100;
            int pButtonWidth = (int)(150 * 1.5);
            var pairMainButton = new Button 
            { 
                Text = "🔗 Pair ADB Wireless", 
                Location = new Point((groupWidth - pButtonWidth) / 2, row1Y), 
                Size = new Size(pButtonWidth, 35),
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            pairMainButton.FlatAppearance.BorderSize = 0;
            pairMainButton.Click += PairButton_Click;

            _deviceGroupBox.Controls.AddRange(new Control[] {
                _deviceComboBox, _refreshButton, _deviceStatusLabel,
                pairMainButton
            });

            // Adjust GroupBox size
            _deviceGroupBox.Size = new Size(340, 160);

            Controls.Add(_deviceGroupBox);
        }

        private void CreateVideoSection()
        {
            _videoGroupBox = new GroupBox
            {
                Text = "Video Settings",
                Location = new Point(20, 190), // Fixed Y for consistent spacing (175 + 15)
                Size = new Size(340, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.FromArgb(25, 103, 210) // Darker Google Blue for better contrast
            };

            // Resolution section (combo only; label removed)
            // Margin atas agar tidak terpotong
            int marginTop = 40;
            int spacingY = 20;
            // Center the resolution combo box within the group (group width = 340)
            _resolutionComboBox = new ComboBox
            {
                Location = new Point(65, marginTop),
                Size = new Size(210, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                FlatStyle = FlatStyle.Flat
            };

            // Bitrate and Max FPS on the same row, with labels above inputs, centered within group
            int bitrateRowY = marginTop + 35 + spacingY;
            int labelHeight = 16;
            int inputY = bitrateRowY + labelHeight + 6;
            int inputWidth = 80;
            int spacingX = 40; // space between the two input columns
            int totalWidth = inputWidth * 2 + spacingX;
            int startX = (_videoGroupBox.Size.Width - totalWidth) / 2;
            int bitrateLabelWidth = 120; // wider so "Bitrate (Mbps)" fits
            int bitrateLabelX = startX - (bitrateLabelWidth - inputWidth) / 2;

            // Bitrate: label above textbox, centered to the textbox width
            _bitrateLabel = new Label
            {
                Text = "Bitrate (Mbps)",
                Location = new Point(bitrateLabelX, bitrateRowY),
                Size = new Size(bitrateLabelWidth, labelHeight),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 64, 67)
            };

            _bitrateTextBox = new TextBox
            {
                Location = new Point(startX, inputY),
                Size = new Size(inputWidth, 26),
                Text = "20",
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36)
            };
            _bitrateTextBox.TextChanged += BitrateTextBox_TextChanged;


            // Max FPS: label above textbox, centered to the textbox width
            _framerateLabel = new Label
            {
                Text = "Max FPS",
                Location = new Point(startX + inputWidth + spacingX - (inputWidth / 4), bitrateRowY),
                Size = new Size(inputWidth + (inputWidth / 2), labelHeight),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 64, 67)
            };
            _framerateTextBox = new TextBox
            {
                Location = new Point(startX + inputWidth + spacingX + (inputWidth / 8), inputY),
                Size = new Size(inputWidth, 26),
                Text = "0",
                TextAlign = HorizontalAlignment.Center,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36)
            };
            _framerateTextBox.KeyPress += FramerateTextBox_KeyPress;
            _framerateTextBox.Leave += FramerateTextBox_Leave;

            _resolutionComboBox.Items.AddRange(new[] { "Device Resolution", "720p", "1080p", "4K" });
            _resolutionComboBox.SelectedIndex = 0;
            _resolutionComboBox.DrawItem += ComboBox_DrawItem;
            _resolutionComboBox.SelectedIndexChanged += ResolutionComboBox_SelectedIndexChanged;
            // Position checkboxes below the inputs and stack them vertically
            int checkboxY = inputY + 36;
            int checkboxSpacingY = 28;
            int checkboxX = startX; // align with inputs
            int checkboxWidth = inputWidth * 2 + spacingX; // span both columns for clarity

            _fullscreenCheckBox = new CheckBox
            {
                Text = "Fullscreen Mode",
                Location = new Point(checkboxX, checkboxY),
                Size = new Size(checkboxWidth, 20),
                ForeColor = Color.FromArgb(32, 33, 36)
            };
            _fullscreenCheckBox.CheckedChanged += FullscreenCheckBox_CheckedChanged;

            _autoReconnectCheckBox = new CheckBox
            {
                Text = "Auto Reconnect",
                Location = new Point(checkboxX, checkboxY + checkboxSpacingY),
                Size = new Size(checkboxWidth, 20),
                ForeColor = Color.FromArgb(32, 33, 36)
            };
            _autoReconnectCheckBox.CheckedChanged += AutoReconnectCheckBox_CheckedChanged;

            _noControlCheckBox = new CheckBox
            {
                Text = "Control",
                Location = new Point(checkboxX, checkboxY + checkboxSpacingY * 2),
                Size = new Size(checkboxWidth, 20),
                ForeColor = Color.FromArgb(32, 33, 36)
            };
            _noControlCheckBox.CheckedChanged += NoControlCheckBox_CheckedChanged;

            _videoGroupBox.Controls.AddRange(new Control[]
            {
                _bitrateLabel, _bitrateTextBox, _framerateLabel, _framerateTextBox,
                _resolutionComboBox,
                _fullscreenCheckBox, _autoReconnectCheckBox, _noControlCheckBox
            });

            Controls.Add(_videoGroupBox);
        }

        private void CreateAudioSection()
        {
            _audioGroupBox = new GroupBox
            {
                Text = "Audio Settings",
                Location = new Point(20, _videoGroupBox.Location.Y + _videoGroupBox.Size.Height + 15), // Consistent 15px spacing
                Size = new Size(340, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.FromArgb(25, 103, 210) // Darker Google Blue for better contrast
            };

            var audioLabel = new Label
            {
                Text = "Audio Source",
                Location = new Point(130, 15),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(60, 64, 67)
            };

            _audioComboBox = new ComboBox
            {
                Location = new Point(80, 40),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                FlatStyle = FlatStyle.Flat
            };
            _audioComboBox.Items.AddRange(new[] { "Audio Playback", "Audio Playback Duplication", "Microphone", "No audio" });
            _audioComboBox.SelectedIndex = 0;
            _audioComboBox.DrawItem += ComboBox_DrawItem;
            _audioComboBox.SelectedIndexChanged += AudioComboBox_SelectedIndexChanged;

            _audioGroupBox.Controls.AddRange(new Control[] { audioLabel, _audioComboBox });
            Controls.Add(_audioGroupBox);
        }

        private void CreateControlSection()
        {
            _controlGroupBox = new Panel
            {
                Location = new Point(20, _audioGroupBox.Location.Y + _audioGroupBox.Size.Height + 15), // Consistent 15px spacing
                Size = new Size(340, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _startStopButton = new Button
            {
                Text = "Start Mirror",
                Location = new Point(10, 20),
                Size = new Size(320, 60),
                BackColor = Color.FromArgb(26, 115, 232),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _startStopButton.FlatAppearance.BorderSize = 0;
            _startStopButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 101, 216);
            _startStopButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(18, 89, 190);
            _startStopButton.Click += StartStopButton_Click;

            _statusLabel = new Label
            {
                Text = "Ready to start mirroring",
                Location = new Point(10, 85),
                Size = new Size(320, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(60, 64, 67), // Dark gray for better visibility
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
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

                // Load UI from configuration with null checks
                if (_bitrateTextBox != null)
                    _bitrateTextBox.Text = config.Bitrate ?? "20";
                
                if (_framerateTextBox != null)
                    _framerateTextBox.Text = Math.Max(1, Math.Min(240, config.Framerate)).ToString();
                
                if (_fullscreenCheckBox != null)
                    _fullscreenCheckBox.Checked = config.FullscreenEnabled;

                if (_autoReconnectCheckBox != null)
                    _autoReconnectCheckBox.Checked = config.AutoReconnectEnabled;

                if (_noControlCheckBox != null)
                    _noControlCheckBox.Checked = !config.NoControlEnabled;

                if (_resolutionComboBox != null)
                {
                    int index = _resolutionComboBox.Items.IndexOf(config.VideoResolution);
                    if (index >= 0)
                        _resolutionComboBox.SelectedIndex = index;
                }

                // Set audio source safely
                if (_audioComboBox != null)
                {
                        // Determine audio selection and duplication
                        int audioIndex = 0;
                        if (config.AudioDuplicationEnabled)
                        {
                            audioIndex = _audioComboBox.Items.IndexOf("Audio Playback Duplication");
                        }
                        else
                        {
                            audioIndex = (config.AudioSource ?? "Audio Playback") switch
                            {
                                "Audio Playback" => _audioComboBox.Items.IndexOf("Audio Playback"),
                                "Microphone" => _audioComboBox.Items.IndexOf("Microphone"),
                                "No audio" => _audioComboBox.Items.IndexOf("No audio"),
                                _ => _audioComboBox.Items.IndexOf("Audio Playback")
                            };
                        }

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

        private void FramerateTextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Allow digits and control keys only
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void FramerateTextBox_Leave(object? sender, EventArgs e)
        {
            try
            {
                if (_configManager == null || _framerateTextBox == null) return;
                if (!int.TryParse(_framerateTextBox.Text, out int val))
                    val = 60;
                val = Math.Max(1, Math.Min(240, val));
                _framerateTextBox.Text = val.ToString();
                _configManager.Set("Framerate", val);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating framerate: {ex.Message}");
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
                    _configManager.Set("NoControlEnabled", !_noControlCheckBox.Checked);
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
                        if (selectedAudio == "Audio Playback Duplication")
                        {
                            _configManager.Set("AudioSource", "Audio Playback");
                            _configManager.Set("AudioDuplicationEnabled", true);
                        }
                        else
                        {
                            _configManager.Set("AudioSource", selectedAudio);
                            _configManager.Set("AudioDuplicationEnabled", false);
                        }
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
            if (e.Index < 0) return;
            if (sender is ComboBox comboBox)
            {
                Color backColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected 
                    ? Color.FromArgb(26, 115, 232) 
                    : Color.White;
                Color foreColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected 
                    ? Color.White 
                    : Color.FromArgb(32, 33, 36);

                using (var brush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }

                // Draw a subtle border around items to make them visible on light theme
                using (var pen = new Pen(Color.FromArgb(218, 220, 224))) // Light border color
                {
                    var borderRect = e.Bounds;
                    borderRect.Width -= 1;
                    borderRect.Height -= 1;
                    e.Graphics.DrawRectangle(pen, borderRect);
                }
                
                string text = comboBox.Items[e.Index]?.ToString() ?? string.Empty;
                TextRenderer.DrawText(e.Graphics, text, e.Font, e.Bounds, foreColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                
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
                    Framerate = (int.TryParse(_framerateTextBox?.Text, out var __f1) ? Math.Max(1, Math.Min(240, __f1)) : 60),
                    Fullscreen = _fullscreenCheckBox.Checked,
                    NoControl = !_noControlCheckBox.Checked,
                    VideoResolution = _resolutionComboBox?.SelectedItem?.ToString() ?? "Original Device Resolution",
                    AudioSource = audioSourceInternal
                };

                // Add audio duplication argument if enabled in configuration
                try
                {
                    if (_configManager.Config.AudioDuplicationEnabled)
                        config.AdditionalArgs.Add("--audio-dup");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error applying audio duplication flag: {ex.Message}");
                }

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
                            Framerate = (int.TryParse(_framerateTextBox?.Text, out var __f2) ? Math.Max(1, Math.Min(240, __f2)) : 60),
                            Fullscreen = _fullscreenCheckBox.Checked,
                            NoControl = !_noControlCheckBox.Checked,
                            VideoResolution = _resolutionComboBox?.SelectedItem?.ToString() ?? "Original Device Resolution",
                            AudioSource = audioSourceInternal
                        };

                        // Add audio duplication argument if enabled in configuration
                        try
                        {
                            if (_configManager.Config.AudioDuplicationEnabled)
                                config.AdditionalArgs.Add("--audio-dup");
                        }
                        catch (Exception dbgEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error applying audio duplication flag: {dbgEx.Message}");
                        }

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
                            Framerate = (int.TryParse(_framerateTextBox?.Text, out var __f3) ? Math.Max(1, Math.Min(240, __f3)) : 60),
                            Fullscreen = _fullscreenCheckBox.Checked,
                            NoControl = !_noControlCheckBox.Checked,
                            VideoResolution = _resolutionComboBox?.SelectedItem?.ToString() ?? "Original Device Resolution",
                            AudioSource = audioSourceInternal
                        };

                        // Add audio duplication argument if enabled in configuration
                        try
                        {
                            if (_configManager.Config.AudioDuplicationEnabled)
                                config.AdditionalArgs.Add("--audio-dup");
                        }
                        catch (Exception dbgEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error applying audio duplication flag: {dbgEx.Message}");
                        }

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
            _statusLabel.Text = $"Mirroring Active • Device: {config.DeviceId} • {config.Bitrate}";
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
                    _framerateTextBox?.Dispose();
                    _fullscreenCheckBox?.Dispose();
                    _autoReconnectCheckBox?.Dispose();
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
