using CoreTweet;
using Kennedy.ManagedHooks;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Screenshotter.Objects;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Screenshotter.Windows
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow
    {
        private bool _isMouseDown;
        private bool _isTrimMode;
        private Point _startPosition;
        private KeyboardHook _globalHook;
        private Drawing.Bitmap _screenshot;
        private Forms.NotifyIcon _notifyIcon;

        public static string Location;
        public static Tokens Token;

        public MainWindow()
        {
            InitializeComponent();
            Location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            AppDomain.CurrentDomain.FirstChanceException += OnExceptionThrow;
        }

        ~MainWindow()
        {
            _globalHook.UninstallHook();
            _globalHook.Dispose();
            _notifyIcon.Dispose();
        }

        private async void InitAsync(object sender, RoutedEventArgs e)
        {
            ReAuth:

            if (!File.Exists(Location + @"\credentials.json"))
            {
                new AuthorizeWindow().ShowDialog();
                goto ReAuth;
            }

            using (var reader = new StreamReader(Location + @"\credentials.json"))
            {
                try
                {
                    await Task.Run(() =>
                    {
                        var json = reader.ReadToEnd();
                        var credentials = JsonConvert.DeserializeObject<Credentials>(json);

                        Token = Tokens.Create(
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
                _notifyIcon = new Forms.NotifyIcon
                {
                    Text = @"Screenshotter",
                    Icon = new Drawing.Icon(iconStream.Stream),
                    Visible = true,
                    BalloonTipTitle = @"Screenshotter",
                    BalloonTipIcon = Forms.ToolTipIcon.Info,
                    BalloonTipText = @"PrintScreenキーを押すとスクリーンショットを取得できます。",
                    ContextMenu = new Forms.ContextMenu()
                };

                _notifyIcon.ContextMenu.MenuItems.Add("終了", (s, a) => Close());
                _notifyIcon.ShowBalloonTip(2000);
            }

            _globalHook = new KeyboardHook();
            _globalHook.KeyboardEvent += OnKeyDown;
            _globalHook.InstallHook();
        }

        private void OnKeyDown(KeyboardEvents e, Forms.Keys k)
        {
            if (_isTrimMode && e == KeyboardEvents.KeyDown && k == Forms.Keys.Escape)
            {
                Disable();
                _isTrimMode = false;
            }

            if (_isTrimMode && e == KeyboardEvents.KeyDown && k == Forms.Keys.Enter)
            {
                Disable();
                _globalHook.UninstallHook();
                _isTrimMode = false;

                new TweetWindow(_screenshot).ShowDialog();

                _globalHook.InstallHook();
                return;
            }

            if (_isTrimMode ||
                e != KeyboardEvents.KeyDown ||
                k != Forms.Keys.PrintScreen) return;

            LayoutRoot.Width = Forms.Screen.PrimaryScreen.Bounds.Width;
            LayoutRoot.Height = Forms.Screen.PrimaryScreen.Bounds.Height;

            _screenshot = ScreenshotUtil.GetScreenshot();
            Screenshot.Source = _screenshot.ToBitmapSource();

            _isTrimMode = true;
            Enable();
        }

        private void OnDragStart(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = true;
            _startPosition = e.GetPosition(LayoutRoot);
            LayoutRoot.CaptureMouse();
                    
            Canvas.SetLeft(SelectedArea, _startPosition.X);
            Canvas.SetTop(SelectedArea, _startPosition.Y);

            SelectedArea.Width = 0;
            SelectedArea.Height = 0;
            SelectedArea.Visibility = Visibility.Visible;
        }

        private void OnDragFinish(object sender, MouseButtonEventArgs e)
        {
            _isMouseDown = false;
            SelectedArea.Visibility = Visibility.Collapsed;
            LayoutRoot.ReleaseMouseCapture();
            _globalHook.UninstallHook();

            var finishPosition = e.GetPosition(LayoutRoot);
            int left, top, width, height;

            if (_startPosition.X < finishPosition.X)
            {
                left = (int)_startPosition.X;
                width = (int)(finishPosition.X - _startPosition.X);
            }
            else
            {
                left = (int)finishPosition.X;
                width = (int)(_startPosition.X - finishPosition.X);
            }

            if (_startPosition.Y < finishPosition.Y)
            {
                top = (int)_startPosition.Y;
                height = (int)(finishPosition.Y - _startPosition.Y);
            }
            else
            {
                top = (int)finishPosition.Y;
                height = (int)(_startPosition.Y - finishPosition.Y);
            }

            if (width < 1 || height < 1) return;
            Disable();

            var rect = new Drawing.Rectangle(left, top, width, height);
            var trimmed = _screenshot.Clone(rect, _screenshot.PixelFormat);
            _screenshot.Dispose();

            new TweetWindow(trimmed).ShowDialog();

            _globalHook.InstallHook();
            _isTrimMode = false;
        }

        private void OnDraging(object sender, MouseEventArgs e)
        {
            if (!_isMouseDown) return;
            var nowPosition = e.GetPosition(LayoutRoot);

            if (_startPosition.X < nowPosition.X)
            {
                Canvas.SetLeft(SelectedArea, _startPosition.X);
                SelectedArea.Width = nowPosition.X - _startPosition.X;
            }
            else
            {
                Canvas.SetLeft(SelectedArea, nowPosition.X);
                SelectedArea.Width = _startPosition.X - nowPosition.X;
            }

            if (_startPosition.Y < nowPosition.Y)
            {
                Canvas.SetTop(SelectedArea, _startPosition.Y);
                SelectedArea.Height = nowPosition.Y - _startPosition.Y;
            }
            else
            {
                Canvas.SetTop(SelectedArea, nowPosition.Y);
                SelectedArea.Height = _startPosition.Y - nowPosition.Y;
            }
        }

        private static void OnExceptionThrow(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception.Source == "PresentationCore" ||
                e.Exception.Source == "System.Xaml") return;

            var msg = "予期しない例外が発生したため、Screenshotterを終了します。\n"
                    + "以下のレポートを開発者に報告してください。\n"
                    + "※OKボタンをクリックするとクリップボードにレポートをコピーして終了します。\n"
                    + "※キャンセルボタンをクリックするとそのまま終了します。\n\n"
                    + e.Exception.GetType() + "\n"
                    + e.Exception.Message + "\n"
                    + e.Exception.StackTrace + "\n"
                    + e.Exception.Source;

            if (e.Exception.InnerException != null)
            {
                msg += "\nInner: "
                     + e.Exception.InnerException.GetType() + "\n"
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
                var thread = new Thread(() =>
                {
                    Clipboard.SetDataObject(msg, true);
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
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

        private const int GwlExstyle = -20;
        private const int WsExNoactivate = 0x08000000;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var helper = new WindowInteropHelper(this);
            SetWindowLong(
                helper.Handle, GwlExstyle,
                GetWindowLong(helper.Handle, GwlExstyle) | WsExNoactivate
            );
        }

        #endregion
    }
}