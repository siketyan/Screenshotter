using Kennedy.ManagedHooks;
using System;
using System.Windows.Forms;

namespace Screenshotter
{
    public static class App
    {
        [STAThread]
        public static void Main()
        {
            var globalHook = new KeyboardHook();
            globalHook.KeyboardEvent += new KeyboardHook.KeyboardEventHandler(OnKeyDown);
            globalHook.InstallHook();

            Console.ReadLine();

            globalHook.UninstallHook();
            globalHook.Dispose();
        }

        private static void OnKeyDown(KeyboardEvents e, Keys k)
        {
            if (e != KeyboardEvents.KeyDown ||
                k != Keys.PrintScreen) return;

            new MainWindow().ShowDialog();
        }
    }
}