using System;
using System.Drawing;
using System.Windows.Forms;
using ScrcpyController.Core;

namespace ScrcpyController.UI
{
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
        private int _loadingProgress;

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

            // Create controls
            CreateControls();

            // Layout controls
            LayoutControls();
        }

        private void CreateControls()
        {
            // Path label
            var pathLabel = new Label
            {
                Text = "Please select the Scrcpy installation directory:",
                Location = new Point(20, 20),
                Size = new Size(400, 20),
                Font = new Font(Font.FontFamily, 10, FontStyle.Bold)
            };

            // Path text box
            _pathTextBox = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(350, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pathTextBox.TextChanged += PathTextBox_TextChanged;

            // Browse button
            _browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(380, 50),
                Size = new Size(80, 23),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _browseButton.Click += BrowseButton_Click;

            // Status label
            _statusLabel = new Label
            {
                Text = "Please locate your Scrcpy installation folder.",
                Location = new Point(20, 90),
                Size = new Size(440, 40),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
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
                Enabled = false
            };
            _okButton.Click += OkButton_Click;

            // Cancel button
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(390, 150),
                Size = new Size(80, 30)
            };
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

        private void LayoutControls()
        {
            // All controls are positioned in CreateControls method
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

        private void ValidatePath(string path)
        {
            // Show loading indicator
            ShowLoadingIndicator();

            // Validate path in background
            var validationResult = _validator.ValidateScrcpyPath(path);

            // Hide loading indicator
            HideLoadingIndicator();

            if (validationResult.IsValid)
            {
                _statusLabel.Text = "Valid Scrcpy installation found!";
                _statusLabel.ForeColor = Color.Green;
                _okButton.Enabled = true;
            }
            else if (string.IsNullOrWhiteSpace(path))
            {
                _statusLabel.Text = "Please locate your Scrcpy installation folder.";
                _statusLabel.ForeColor = SystemColors.ControlText;
                _okButton.Enabled = false;
            }
            else if (!System.IO.Directory.Exists(path))
            {
                _statusLabel.Text = "Directory does not exist.";
                _statusLabel.ForeColor = Color.Red;
                _okButton.Enabled = false;
            }
            else
            {
                string missingFiles = string.Join(", ", validationResult.MissingFiles);
                _statusLabel.Text = $"Missing files: {missingFiles}";
                _statusLabel.ForeColor = Color.Red;
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