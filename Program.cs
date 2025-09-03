using ScrcpyController.Core;
using ScrcpyController.UI;
using System.IO;
using System.Windows.Forms;

namespace ScrcpyController
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Program.Main start");

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Console.WriteLine("ApplicationConfiguration.Initialize done");

            // Enable visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Console.WriteLine("Visual styles enabled");

            // Check for Scrcpy installation before running main form
            if (!CheckScrcpyInstallation())
            {
                Console.WriteLine("Scrcpy installation not found or invalid");
                return;
            }

            // Run the main form
            Console.WriteLine("About to run Application.Run");
            Application.Run(new MainForm());
            Console.WriteLine("Application.Run exited");
        }

        static bool CheckScrcpyInstallation()
        {
            var configManager = new ConfigManager();
            var validator = new ScrcpyValidator();
            
            // Try to load existing configuration
            configManager.LoadConfig();
            string configuredPath = configManager.Config.ScrcpyPath;
            
            // Validate configured path
            if (!string.IsNullOrWhiteSpace(configuredPath))
            {
                var result = validator.ValidateScrcpyPath(configuredPath);
                if (result.IsValid)
                {
                    Console.WriteLine($"Valid Scrcpy installation found at: {configuredPath}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Configured Scrcpy path is invalid: {configuredPath}");
                }
            }
            
            // Try to find Scrcpy automatically
            var autoResult = validator.FindAndValidateScrcpyPath();
            if (autoResult.IsValid)
            {
                configManager.Set("ScrcpyPath", autoResult.ScrcpyPath);
                configManager.SaveConfig();
                Console.WriteLine($"Auto-detected Scrcpy installation at: {autoResult.ScrcpyPath}");
                return true;
            }
            
            // Show setup dialog
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            using var setupDialog = new ScrcpySetupDialog();
            if (setupDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = setupDialog.SelectedPath;
                var validationResult = validator.ValidateScrcpyPath(selectedPath);
                
                if (validationResult.IsValid)
                {
                    configManager.Set("ScrcpyPath", selectedPath);
                    configManager.SaveConfig();
                    Console.WriteLine($"Scrcpy installation configured at: {selectedPath}");
                    return true;
                }
                else
                {
                    MessageBox.Show(
                        "The selected directory does not contain a valid Scrcpy installation. Please ensure all required files are present.",
                        "Invalid Scrcpy Installation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return false;
                }
            }
            
            // User cancelled setup
            return false;
        }
    }
}