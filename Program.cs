using ScrcpyController.Core;
using ScrcpyController.UI;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace ScrcpyController
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Console.WriteLine("Program.Main start");

            ApplicationConfiguration.Initialize();
            Console.WriteLine("ApplicationConfiguration.Initialize done");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Console.WriteLine("Visual styles enabled");

            // Set up global exception handling
            Application.ThreadException += new ThreadExceptionEventHandler(Application_ThreadException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            try
            {
                if (!CheckScrcpyInstallation())
                {
                    Console.WriteLine("Scrcpy installation not found or invalid");
                    return;
                }

                Console.WriteLine("About to run Application.Run");
                Application.Run(new MainForm());
                Console.WriteLine("Application.Run exited");
            }
            catch (Exception ex)
            {
                HandleFatalException(ex, "An unrecoverable error occurred during application startup.");
            }
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleFatalException(e.Exception, "An unhandled error occurred in the UI thread.");
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleFatalException(e.ExceptionObject as Exception, "An unhandled error occurred in the application.");
        }

        private static void HandleFatalException(Exception? ex, string message)
        {
            string errorMessage = $"{message}\n\nDetails: {ex?.Message ?? "No details available."}\n\nStack Trace: {ex?.StackTrace ?? "No stack trace available."}";
            MessageBox.Show(errorMessage, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine($"FATAL ERROR: {errorMessage}");
            Environment.Exit(1); // Terminate the application
        }

        static bool CheckScrcpyInstallation()
        {
            var configManager = new ConfigManager();
            var validator = new ScrcpyValidator();
            
            // Try to load existing configuration
            string configuredPath = string.Empty;
            try
            {
                configManager.LoadConfig();
                if (configManager.Config != null)
                {
                    configuredPath = configManager.Config.ScrcpyPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                // Continue without configured path if loading fails
            }
            
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
            
            // Try to find Scrcpy automatically, including checking /scrcpy subfolder
            var autoResult = validator.FindAndValidateScrcpyPath();
            if (autoResult.IsValid)
            {
                try
                {
                    configManager.Set("ScrcpyPath", autoResult.ScrcpyPath);
                    configManager.SaveConfig();
                    Console.WriteLine($"Auto-detected Scrcpy installation at: {autoResult.ScrcpyPath}");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving auto-detected Scrcpy path: {ex.Message}");
                    // Continue to setup dialog if saving fails
                }
            }
            
            // Show setup dialog
            using var setupDialog = new ScrcpySetupDialog();
            if (setupDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = setupDialog.SelectedPath;
                var validationResult = validator.ValidateScrcpyPath(selectedPath);
                
                if (validationResult.IsValid)
                {
                    try
                    {
                        configManager.Set("ScrcpyPath", selectedPath);
                        configManager.SaveConfig();
                        Console.WriteLine($"Scrcpy installation configured at: {selectedPath}");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error saving configured Scrcpy path: {ex.Message}");
                        MessageBox.Show(
                            $"Failed to save Scrcpy path: {ex.Message}",
                            "Configuration Save Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return false;
                    }
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