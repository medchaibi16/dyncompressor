using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace dyncompressor
{
    internal static class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // ✅ Allocate console AFTER configuration
            AllocConsole();

            Application.Run(new Form1());
        }
    }
}