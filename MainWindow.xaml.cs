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
            this.globalHook.UninstallHook();
            this.globalHook.Dispose();
            this.notifyIcon.Dispose();
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

                this.notifyIcon = new Forms.NotifyIcon()
                {
                    Text = @"Screenshotter",
                    Icon = new Drawing.Icon(iconStream.Stream),
                    Visible = true,
                    BalloonTipTitle = @"Screenshotter",
                    BalloonTipIcon = Forms.ToolTipIcon.Info,
                    BalloonTipText = @"PrintScreenキーを押すとスクリーンショットを取得できます。",
                    ContextMenu = new Forms.ContextMenu()
                };

                this.notifyIcon.ContextMenu.MenuItems.Add("終了", new EventHandler((s, a) => Close()));
                this.notifyIcon.ShowBalloonTip(2000);
            }

            this.globalHook = new KeyboardHook();
            this.globalHook.KeyboardEvent += new KeyboardHook.KeyboardEventHandler(OnKeyDown);
            this.globalHook.InstallHook();
        }

        private void OnKeyDown(KeyboardEvents e, Forms.Keys k)
        {
            if (this.isTrimMode && e == KeyboardEvents.KeyDown && k == Forms.Keys.Escape)
            {
                Disable();
                this.isTrimMode = false;
            }

            if (this.isTrimMode && e == KeyboardEvents.KeyDown && k == Forms.Keys.Enter)
            {
                Disable();
                this.globalHook.UninstallHook();
                this.isTrimMode = false;

                new TweetWindow(this.screenshot).ShowDialog();

                this.globalHook.InstallHook();
                this.isTrimMode = true;
                return;
            }

            if (this.isTrimMode ||
                e != KeyboardEvents.KeyDown ||
                k != Forms.Keys.PrintScreen) return;

            this.LayoutRoot.Width = Forms.Screen.PrimaryScreen.Bounds.Width;
            this.LayoutRoot.Height = Forms.Screen.PrimaryScreen.Bounds.Height;

            this.screenshot = ScreenshotUtil.GetScreenshot();
            this.Screenshot.Source = this.screenshot.ToBitmapSource();

            this.isTrimMode = true;
            Enable();
        }

        private void OnDragStart(object sender, MouseButtonEventArgs e)
        {
            this.isMouseDown = true;
            this.startPosition = e.GetPosition(this.LayoutRoot);
            this.LayoutRoot.CaptureMouse();
                    
            Canvas.SetLeft(this.SelectedArea, this.startPosition.X);
            Canvas.SetTop(this.SelectedArea, this.startPosition.Y);

            this.SelectedArea.Width = 0;
            this.SelectedArea.Height = 0;
            this.SelectedArea.Visibility = Visibility.Visible;
        }

        private void OnDragFinish(object sender, MouseButtonEventArgs e)
        {
            this.isMouseDown = false;
            this.SelectedArea.Visibility = Visibility.Collapsed;
            this.LayoutRoot.ReleaseMouseCapture();
            this.globalHook.UninstallHook();

            var finishPosition = e.GetPosition(this.LayoutRoot);
            int left, top, width, height;

            if (this.startPosition.X < finishPosition.X)
            {
                left = (int)this.startPosition.X;
                width = (int)(finishPosition.X - this.startPosition.X);
            }
            else
            {
                left = (int)finishPosition.X;
                width = (int)(this.startPosition.X - finishPosition.X);
            }

            if (this.startPosition.Y < finishPosition.Y)
            {
                top = (int)this.startPosition.Y;
                height = (int)(finishPosition.Y - this.startPosition.Y);
            }
            else
            {
                top = (int)finishPosition.Y;
                height = (int)(this.startPosition.Y - finishPosition.Y);
            }

            if (width < 1 || height < 1) return;
            Disable();

            var rect = new Drawing.Rectangle(left, top, width, height);
            var trimmed = this.screenshot.Clone(rect, this.screenshot.PixelFormat);
            this.screenshot.Dispose();

            new TweetWindow(trimmed).ShowDialog();

            this.globalHook.InstallHook();
            this.isTrimMode = false;
        }

        private void OnDraging(object sender, MouseEventArgs e)
        {
            if (!this.isMouseDown) return;
            var nowPosition = e.GetPosition(this.LayoutRoot);

            if (this.startPosition.X < nowPosition.X)
            {
                Canvas.SetLeft(this.SelectedArea, this.startPosition.X);
                this.SelectedArea.Width = nowPosition.X - this.startPosition.X;
            }
            else
            {
                Canvas.SetLeft(this.SelectedArea, nowPosition.X);
                this.SelectedArea.Width = this.startPosition.X - nowPosition.X;
            }

            if (this.startPosition.Y < nowPosition.Y)
            {
                Canvas.SetTop(this.SelectedArea, this.startPosition.Y);
                this.SelectedArea.Height = nowPosition.Y - this.startPosition.Y;
            }
            else
            {
                Canvas.SetTop(this.SelectedArea, nowPosition.Y);
                this.SelectedArea.Height = this.startPosition.Y - nowPosition.Y;
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
            this.Opacity = 1f;
            this.IsHitTestVisible = true;
            this.TrimNotify.Visibility = Visibility.Visible;
        }

        private void Disable()
        {
            this.Opacity = 0f;
            this.IsHitTestVisible = false;
            this.TrimNotify.Visibility = Visibility.Collapsed;
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