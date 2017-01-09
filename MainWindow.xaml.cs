using Kennedy.ManagedHooks;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Screenshotter
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool isMouseDown;
        private bool isTrimMode;
        private string location;
        private Point startPosition;
        private KeyboardHook globalHook;
        private Drawing.Bitmap screenshot;

        public MainWindow()
        {
            InitializeComponent();
            location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        ~MainWindow()
        {
            globalHook.UninstallHook();
            globalHook.Dispose();
        }

        private void Init(object sender, RoutedEventArgs e)
        {
            globalHook = new KeyboardHook();
            globalHook.KeyboardEvent += new KeyboardHook.KeyboardEventHandler(OnKeyDown);
            globalHook.InstallHook();
        }

        private void OnKeyDown(KeyboardEvents e, Forms.Keys k)
        {
            if (isTrimMode ||
                e != KeyboardEvents.KeyDown ||
                k != Forms.Keys.PrintScreen) return;

            LayoutRoot.Width = Forms.Screen.PrimaryScreen.Bounds.Width;
            LayoutRoot.Height = Forms.Screen.PrimaryScreen.Bounds.Height;

            screenshot = ScreenshotUtil.GetScreenshot();
            Screenshot.Source = screenshot.ToBitmapSource();

            isTrimMode = true;
            Enable();
        }

        private void OnDragStart(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            startPosition = e.GetPosition(LayoutRoot);
            LayoutRoot.CaptureMouse();
                    
            Canvas.SetLeft(SelectedArea, startPosition.X);
            Canvas.SetTop(SelectedArea, startPosition.Y);

            SelectedArea.Width = 0;
            SelectedArea.Height = 0;
            SelectedArea.Visibility = Visibility.Visible;
        }

        private void OnDragFinish(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            SelectedArea.Visibility = Visibility.Collapsed;
            LayoutRoot.ReleaseMouseCapture();
            Disable();

            Point finishPosition = e.GetPosition(LayoutRoot);
            int left, top, width, height;

            if (startPosition.X < finishPosition.X)
            {
                left = (int)startPosition.X;
                width = (int)(finishPosition.X - startPosition.X);
            }
            else
            {
                left = (int)finishPosition.X;
                width = (int)(startPosition.X - finishPosition.X);
            }

            if (startPosition.Y < finishPosition.Y)
            {
                top = (int)startPosition.Y;
                height = (int)(finishPosition.Y - startPosition.Y);
            }
            else
            {
                top = (int)finishPosition.Y;
                height = (int)(startPosition.Y - finishPosition.Y);
            }

            var rect = new Drawing.Rectangle(left, top, width, height);
            var trimmed = screenshot.Clone(rect, screenshot.PixelFormat);

            if (!Directory.Exists(location + @"\temp"))
                Directory.CreateDirectory(location + @"\temp");

            trimmed.Save(
                location + @"\~$Capture-"
                    + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                    + ".temp.png",
                Drawing.Imaging.ImageFormat.Png
            );
            trimmed.Dispose();

            isTrimMode = false;
        }

        private void OnDraging(object sender, MouseEventArgs e)
        {
            if (!isMouseDown) return;
            Point nowPosition = e.GetPosition(LayoutRoot);

            if (startPosition.X < nowPosition.X)
            {
                Canvas.SetLeft(SelectedArea, startPosition.X);
                SelectedArea.Width = nowPosition.X - startPosition.X;
            }
            else
            {
                Canvas.SetLeft(SelectedArea, nowPosition.X);
                SelectedArea.Width = startPosition.X - nowPosition.X;
            }

            if (startPosition.Y < nowPosition.Y)
            {
                Canvas.SetTop(SelectedArea, startPosition.Y);
                SelectedArea.Height = nowPosition.Y - startPosition.Y;
            }
            else
            {
                Canvas.SetTop(SelectedArea, nowPosition.Y);
                SelectedArea.Height = startPosition.Y - nowPosition.Y;
            }
        }

        private void Enable()
        {
            Opacity = 1f;
            IsHitTestVisible = true;
            TrimNotify.Visibility = Visibility.Visible;
        }

        private void Disable()
        {
            Opacity = 0f;
            IsHitTestVisible = false;
            TrimNotify.Visibility = Visibility.Collapsed;
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