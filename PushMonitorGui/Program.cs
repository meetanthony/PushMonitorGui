using System;
using Application = System.Windows.Forms.Application;

namespace PushMonitorGui
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            var app = new TrayApp();
            if (app.Init())
                Application.Run(app);
        }
    }
}