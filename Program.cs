using ScrcpyController.Core;
using ScrcpyController.UI;

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

            // Run the main form
            Console.WriteLine("About to run Application.Run");
            Application.Run(new MainForm());
            Console.WriteLine("Application.Run exited");
        }
    }
}