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

namespace Screenshotter
{
    /// <summary>
    /// TweetWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetWindow : Window
    {
        private Drawing.Bitmap screenshot;

        public TweetWindow(Drawing.Bitmap screenshot)
        {
            this.screenshot = screenshot;

            InitializeComponent();
            Keyboard.Focus(this.Message);
        }

        private async void TweetAsync(object sender, RoutedEventArgs e)
        {
            ShowStatus("ツイートしています...");

            if (this.screenshot == null)
            {
                MessageBox.Show("スクリーンショットが失われたため、ツイートできません。");
                Close();
            }

            using (var stream = new MemoryStream())
            {
                this.screenshot.Save(stream, Drawing.Imaging.ImageFormat.Jpeg);
                await MainWindow.token.Statuses.UpdateWithMediaAsync(
                          this.Message.Text, stream.ToArray()
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
                this.Message.Text,
                @"s?https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+",
                "01234567890123"
            ).Replace(Environment.NewLine, "1");

            this.Count.Content = 140 - temp.Length - 24;
            if ((int)this.Count.Content < 0)
            {
                this.Count.Foreground = Brushes.Red;
                this.TweetButton.IsEnabled = false;
            }
            else
            {
                this.Count.Foreground = Brushes.Black;
                this.TweetButton.IsEnabled = true;
            }
        }

        private void ParseKeyboardShortcut(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key != Key.Enter) return;
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.None) return;
            if ((int)this.Count.Content < 0) return;

            e.Handled = true;
            TweetAsync(null, null);
        }

        private void ShowStatus(string message, bool isLoader = true)
        {
            this.Status.Content = message;
            this.Loader.Visibility = (isLoader) ? Visibility.Visible
                                           : Visibility.Collapsed;
            this.Success.Visibility = (isLoader) ? Visibility.Collapsed
                                            : Visibility.Visible;

            var sb = FindResource("DialogShowAnimation") as Storyboard;
            Storyboard.SetTarget(sb, this.StatusGrid);
            sb.Completed += (s, a) => this.StatusGrid.IsHitTestVisible = true;
            sb.Begin();
        }

        private void CloseStatus(Action doAfterClose = null)
        {
            var sb = FindResource("DialogCloseAnimation") as Storyboard;
            Storyboard.SetTarget(sb, this.StatusGrid);
            sb.Completed += (s, a) =>
            {
                this.StatusGrid.IsHitTestVisible = false;
                doAfterClose?.Invoke();
            };
            sb.Begin();
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            this.screenshot?.Dispose();
        }
    }
}