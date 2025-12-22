using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace dyncompressor
{
    internal static class Program
    {

        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.Run(new Form1());
        }
    }
}