using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Drawing = System.Drawing;

namespace Screenshotter.Windows
{
    /// <summary>
    /// TweetWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetWindow
    {
        private readonly Drawing.Bitmap _screenshot;

        public TweetWindow(Drawing.Bitmap screenshot)
        {
            _screenshot = screenshot;

            InitializeComponent();
            Keyboard.Focus(Message);
        }

        private async void TweetAsync(object sender, RoutedEventArgs e)
        {
            ShowStatus("ツイートしています...");

            if (_screenshot == null)
            {
                MessageBox.Show("スクリーンショットが失われたため、ツイートできません。");
                Close();
            }

            using (var stream = new MemoryStream())
            {
                _screenshot.Save(stream, Drawing.Imaging.ImageFormat.Jpeg);
                await MainWindow.Token.Statuses.UpdateWithMediaAsync(
                          Message.Text, stream.ToArray()
                );
            }

            CloseStatus(async () =>
            {
                ShowStatus("ツイートを送信しました。", false);
                await Task.Run(() => Thread.Sleep(1000));
                CloseStatus();
                Close();
            });
        }

        private void CountText(object sender, TextChangedEventArgs e)
        {
            var temp = Regex.Replace(
                Message.Text,
                @"s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+",
                "01234567890123"
            ).Replace(Environment.NewLine, "1");

            Count.Content = 140 - temp.Length - 24;
            if ((int)Count.Content < 0)
            {
                Count.Foreground = Brushes.Red;
                TweetButton.IsEnabled = false;
            }
            else
            {
                Count.Foreground = Brushes.Black;
                TweetButton.IsEnabled = true;
            }
        }

        private void ParseKeyboardShortcut(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key != Key.Enter) return;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None) return;
            if ((int)Count.Content < 0) return;

            e.Handled = true;
            TweetAsync(null, null);
        }

        private void ShowStatus(string message, bool isLoader = true)
        {
            Status.Content = message;
            Loader.Visibility = isLoader ? Visibility.Visible
                                         : Visibility.Collapsed;
            Success.Visibility = isLoader ? Visibility.Collapsed
                                          : Visibility.Visible;

            var sb = FindResource("DialogShowAnimation") as Storyboard;
            Storyboard.SetTarget(sb, StatusGrid);
            sb.Completed += (s, a) => StatusGrid.IsHitTestVisible = true;
            sb.Begin();
        }

        private void CloseStatus(Action doAfterClose = null)
        {
            var sb = FindResource("DialogCloseAnimation") as Storyboard;
            Storyboard.SetTarget(sb, StatusGrid);
            sb.Completed += (s, a) =>
            {
                StatusGrid.IsHitTestVisible = false;
                doAfterClose?.Invoke();
            };
            sb.Begin();
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            _screenshot?.Dispose();
        }
    }
}