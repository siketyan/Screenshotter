using Kennedy.ManagedHooks;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Screenshotter
{
    public static class App
    {
        [STAThread]
        public static void Main()
        {
            new MainWindow().ShowDialog();
        }
    }
}