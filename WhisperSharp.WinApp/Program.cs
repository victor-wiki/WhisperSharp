using System.Runtime.InteropServices;

namespace WhisperSharp.WinApp
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            LoadLibraries();            

            Application.Run(new frmMain());
        }

        static void LoadLibraries()
        {
            try
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtimes", "win-x64", "native");

                if (Directory.Exists(folder))
                {
                    var files = new DirectoryInfo(folder).GetFiles("*.dll");

                    foreach (var file in files)
                    {
                        NativeLibrary.Load(file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                               
            }
        }
    }
}