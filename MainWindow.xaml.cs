using CoreTweet;
using Kennedy.ManagedHooks;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
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
        private Point startPosition;
        private KeyboardHook globalHook;
        private Drawing.Bitmap screenshot;
        private Forms.NotifyIcon notifyIcon;

        public static string location;
        public static Tokens token;

        public MainWindow()
        {
            InitializeComponent();
            location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AppDomain.CurrentDomain.FirstChanceException += OnExceptionThrow;
        }

        ~MainWindow()
        {
            globalHook.UninstallHook();
            globalHook.Dispose();
            notifyIcon.Dispose();
        }

        private async void InitAsync(object sender, RoutedEventArgs e)
        {
            ReAuth:

            if (!File.Exists(location + @"\credentials.json"))
            {
                new AuthorizeWindow().ShowDialog();
                goto ReAuth;
            }

            using (var reader = new StreamReader(location + @"\credentials.json"))
            {
                try
                {
                    await Task.Run(() =>
                    {
                        var json = reader.ReadToEnd();
                        var credentials = JsonConvert.DeserializeObject<Credentials>(json);

                        token = Tokens.Create(
                            __Private.ConsumerKey,
                            __Private.ConsumerSecret,
                            credentials.AccessToken,
                            credentials.AccessSecret
                        );
                    });
                }
                catch
                {
                    MessageBox.Show("Twitterにログインできませんでした。Screenshotterを終了します。");
                    Close();
                }
            }

            var iconStream = Application.GetResourceStream(
                new Uri("pack://application:,,,/Screenshotter;component/Resources/notifyicon.ico")
            );

            if (iconStream != null)
            {

                notifyIcon = new Forms.NotifyIcon()
                {
                    Text = @"Screenshotter",
                    Icon = new Drawing.Icon(iconStream.Stream),
                    Visible = true,
                    BalloonTipTitle = @"Screenshotter",
                    BalloonTipIcon = Forms.ToolTipIcon.Info,
                    BalloonTipText = @"PrintScreenキーを押すとスクリーンショットを取得できます。",
                    ContextMenu = new Forms.ContextMenu()
                };

                notifyIcon.ContextMenu.MenuItems.Add("終了", new EventHandler((s, a) => Close()));
                notifyIcon.ShowBalloonTip(2000);
            }

            globalHook = new KeyboardHook();
            globalHook.KeyboardEvent += new KeyboardHook.KeyboardEventHandler(OnKeyDown);
            globalHook.InstallHook();
        }

        private void OnKeyDown(KeyboardEvents e, Forms.Keys k)
        {
            if (isTrimMode && e == KeyboardEvents.KeyDown && k == Forms.Keys.Escape)
            {
                Disable();
                isTrimMode = false;
            }

            if (isTrimMode && e == KeyboardEvents.KeyDown && k == Forms.Keys.Enter)
            {
                Disable();
                globalHook.UninstallHook();
                isTrimMode = false;

                var path = location + @"\~$Capture-"
                           + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                           + ".temp.png";

                screenshot.Save(path, Drawing.Imaging.ImageFormat.Png);
                screenshot.Dispose();

                new TweetWindow(path).ShowDialog();

                globalHook.InstallHook();
                isTrimMode = true;
                return;
            }

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
            globalHook.UninstallHook();

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

            if (width < 1 || height < 1) return;
            Disable();

            var rect = new Drawing.Rectangle(left, top, width, height);
            var trimmed = screenshot.Clone(rect, screenshot.PixelFormat);
            var path = location + @"\~$Capture-"
                           + DateTime.Now.ToString("yyyyMMddHHmmssfff")
                           + ".temp.png";

            trimmed.Save(path, Drawing.Imaging.ImageFormat.Png);
            trimmed.Dispose();
            screenshot.Dispose();

            new TweetWindow(path).ShowDialog();

            globalHook.InstallHook();
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

        private void OnExceptionThrow(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception.Source == "PresentationCore"
             || e.Exception.InnerException.Source == "PresentationCore") return;

            var msg = "予期しない例外が発生したため、Screenshotterを終了します。\n"
                    + "以下のレポートを開発者に報告してください。\n"
                    + "※OKボタンをクリックするとクリップボードにレポートをコピーして終了します。\n"
                    + "※キャンセルボタンをクリックするとそのまま終了します。\n\n"
                    + e.Exception.GetType().ToString() + "\n"
                    + e.Exception.Message + "\n"
                    + e.Exception.StackTrace + "\n"
                    + e.Exception.Source;

            if (e.Exception.InnerException != null)
            {
                msg += "\nInner: "
                     + e.Exception.InnerException.GetType().ToString() + "\n"
                     + e.Exception.InnerException.Message + "\n"
                     + e.Exception.InnerException.StackTrace + "\n"
                     + e.Exception.InnerException.Source;
            }

            var result = MessageBox.Show(
                            msg, "Error - Screenshotter",
                            MessageBoxButton.OKCancel, MessageBoxImage.Error
                         );
            if (result == MessageBoxResult.OK)
            {
                Clipboard.SetDataObject(msg, true);
            }

            Environment.Exit(0);
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