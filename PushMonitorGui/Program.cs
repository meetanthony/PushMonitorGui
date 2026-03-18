using System;
using Application = System.Windows.Forms.Application;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new TrayApp();
        app.Init();
        Application.Run(app);
    }
}