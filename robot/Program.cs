using System;

namespace robot
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (MainWindow window = new MainWindow())
                window.Run();
        }
    }
}
