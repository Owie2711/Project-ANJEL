using System;
using System.Drawing;
using System.Windows.Forms;
using ScrcpyController.Core;
using System.Threading.Tasks;

namespace ScrcpyController.UI
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public partial class ScrcpySetupDialog : Form
    {
        private ScrcpyValidator _validator = null!;
        private TextBox _pathTextBox = null!;
        private Button _browseButton = null!;
        private Button _okButton = null!;
        private Button _cancelButton = null!;
        private Label _statusLabel = null!;
        private ProgressBar _loadingBar = null!;
        private System.Windows.Forms.Timer _loadingTimer = null!;
        private int _loadingProgress; // Consider removing if truly unused, but keeping for now as it's harmless or might be used later for non-marquee style

        public string SelectedPath { get; private set; } = "";

        public ScrcpySetupDialog()
        {
            _validator = new ScrcpyValidator();
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Form settings
            Text = "Scrcpy Setup";
            Size = new Size(500, 250);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            this.BackColor = Color.FromArgb(241, 243, 244);
            this.ForeColor = Color.FromArgb(32, 33, 36);
            Font = new Font("Segoe UI", 9);

            // Create controls
            CreateControls();

            // Layout controls
            // LayoutControls(); // Method is empty
        }

        private void CreateControls()
        {
            // Path label
            var pathLabel = new Label
            {
                Text = "Please select the Scrcpy installation directory:",
                Location = new Point(20, 20),
                Size = new Size(400, 24),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 103, 210) // Darker blue #1967D2
            };

            // Path text box
            _pathTextBox = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(350, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                BorderStyle = BorderStyle.FixedSingle
            };
            _pathTextBox.TextChanged += PathTextBox_TextChanged;

            // Browse button
            _browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(380, 50),
                Size = new Size(80, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                FlatStyle = FlatStyle.Flat
            };
            _browseButton.FlatAppearance.BorderSize = 1;
            _browseButton.FlatAppearance.BorderColor = Color.FromArgb(32, 33, 36);
            _browseButton.Click += BrowseButton_Click;

            // Status label
            _statusLabel = new Label
            {
                Text = "Please locate your Scrcpy installation folder.",
                Location = new Point(20, 90),
                Size = new Size(440, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.FromArgb(32, 33, 36)
            };

            // Loading bar (initially hidden)
            _loadingBar = new ProgressBar
            {
                Location = new Point(20, 90),
                Size = new Size(440, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false,
                Style = ProgressBarStyle.Marquee
            };

            // OK button
            _okButton = new Button
            {
                Text = "OK",
                Location = new Point(300, 150),
                Size = new Size(80, 30),
                Enabled = false,
                BackColor = Color.FromArgb(25, 103, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _okButton.FlatAppearance.BorderSize = 0;
            _okButton.Click += OkButton_Click;

            // Cancel button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(390, 150),
                Size = new Size(80, 30),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(32, 33, 36),
                FlatStyle = FlatStyle.Flat
            };
            _cancelButton.FlatAppearance.BorderSize = 1;
            _cancelButton.FlatAppearance.BorderColor = Color.FromArgb(32, 33, 36);
            _cancelButton.Click += CancelButton_Click;

            // Add controls to form
            Controls.AddRange(new Control[]
            {
                pathLabel,
                _pathTextBox,
                _browseButton,
                _statusLabel,
                _loadingBar,
                _okButton,
                _cancelButton
            });

            // Create loading timer
            _loadingTimer = new System.Windows.Forms.Timer();
            _loadingTimer.Interval = 50;
            _loadingTimer.Tick += LoadingTimer_Tick;
        }

        private void PathTextBox_TextChanged(object? sender, EventArgs e)
        {
            ValidatePath(_pathTextBox.Text);
        }

        private void BrowseButton_Click(object? sender, EventArgs e)
        {
            using var folderBrowser = new FolderBrowserDialog
            {
                Description = "Select Scrcpy Installation Directory",
                ShowNewFolderButton = false
            };

            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                _pathTextBox.Text = folderBrowser.SelectedPath;
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            SelectedPath = _pathTextBox.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void LoadingTimer_Tick(object? sender, EventArgs e)
        {
            _loadingProgress = (_loadingProgress + 1) % 100;
            // For marquee style, we don't need to update the value
        }

        private async void ValidatePath(string path)
        {
            ShowLoadingIndicator();
            ScrcpyValidationResult validationResult;

            try
            {
                validationResult = await Task.Run(() => _validator.ValidateScrcpyPath(path));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during Scrcpy path validation: {ex.Message}");
                validationResult = new ScrcpyValidationResult { IsValid = false, MissingFiles = new() { "Validation error" } };
                _statusLabel.Text = $"An error occurred during validation: {ex.Message}";
                _statusLabel.ForeColor = Color.FromArgb(217, 48, 37);
                _okButton.Enabled = false;
                HideLoadingIndicator();
                return;
            }
            finally
            {
                HideLoadingIndicator();
            }

            if (validationResult.IsValid)
            {
                _statusLabel.Text = "Valid Scrcpy installation found!";
                _statusLabel.ForeColor = Color.FromArgb(24, 128, 56);
                _okButton.Enabled = true;
            }
            else if (string.IsNullOrWhiteSpace(path))
            {
                _statusLabel.Text = "Please locate your Scrcpy installation folder.";
                _statusLabel.ForeColor = Color.FromArgb(32, 33, 36);
                _okButton.Enabled = false;
            }
            else if (validationResult.MissingFiles.Contains("Directory does not exist")) // Rely on validator for directory existence
            {
                _statusLabel.Text = "Directory does not exist.";
                _statusLabel.ForeColor = Color.FromArgb(217, 48, 37);
                _okButton.Enabled = false;
            }
            else
            {
                string missingFiles = string.Join(", ", validationResult.MissingFiles);
                _statusLabel.Text = $"Missing files: {missingFiles}";
                _statusLabel.ForeColor = Color.FromArgb(217, 48, 37);
                _okButton.Enabled = false;
            }
        }

        private void ShowLoadingIndicator()
        {
            _loadingBar.Visible = true;
            _statusLabel.Visible = false;
            _loadingTimer.Start();
            _okButton.Enabled = false;
            _browseButton.Enabled = false;
        }

        private void HideLoadingIndicator()
        {
            _loadingTimer.Stop();
            _loadingBar.Visible = false;
            _statusLabel.Visible = true;
            _browseButton.Enabled = true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loadingTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}