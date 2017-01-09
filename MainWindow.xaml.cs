using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Forms = System.Windows.Forms;

namespace Screenshotter
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isMouseDown;
        private Point startedPosition;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Init(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Width = Forms.Screen.PrimaryScreen.Bounds.Width;
            LayoutRoot.Height = Forms.Screen.PrimaryScreen.Bounds.Height;
            Screenshot.Source = ScreenshotUtil.GetScreenshot().ToBitmapSource();
        }

        private void OnDragStart(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            startedPosition = e.GetPosition(LayoutRoot);
            LayoutRoot.CaptureMouse();
                    
            Canvas.SetLeft(SelectedArea, startedPosition.X);
            Canvas.SetTop(SelectedArea, startedPosition.Y);

            SelectedArea.Width = 0;
            SelectedArea.Height = 0;
            SelectedArea.Visibility = Visibility.Visible;
        }

        private void OnDragFinish(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            SelectedArea.Visibility = Visibility.Collapsed;
            LayoutRoot.ReleaseMouseCapture();

            Point finishPosition = e.GetPosition(LayoutRoot);
        }

        private void OnDraging(object sender, MouseEventArgs e)
        {
            if (!isMouseDown) return;
            Point nowPosition = e.GetPosition(LayoutRoot);

            if (startedPosition.X < nowPosition.X)
            {
                Canvas.SetLeft(SelectedArea, startedPosition.X);
                SelectedArea.Width = nowPosition.X - startedPosition.X;
            }
            else
            {
                Canvas.SetLeft(SelectedArea, nowPosition.X);
                SelectedArea.Width = startedPosition.X - nowPosition.X;
            }

            if (startedPosition.Y < nowPosition.Y)
            {
                Canvas.SetTop(SelectedArea, startedPosition.Y);
                SelectedArea.Height = nowPosition.Y - startedPosition.Y;
            }
            else
            {
                Canvas.SetTop(SelectedArea, nowPosition.Y);
                SelectedArea.Height = startedPosition.Y - nowPosition.Y;
            }
        }


        #region アクティブなウィンドウにしない

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            SetWindowLong(
                helper.Handle, GWL_EXSTYLE,
                GetWindowLong(helper.Handle, GWL_EXSTYLE) | WS_EX_NOACTIVATE
            );
        }

        #endregion
    }
}