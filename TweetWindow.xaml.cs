using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Screenshotter
{
    /// <summary>
    /// TweetWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TweetWindow : Window
    {
        private string path;

        public TweetWindow(string path)
        {
            this.path = path;

            InitializeComponent();
            Keyboard.Focus(this.Message);
        }

        private async void TweetAsync(object sender, RoutedEventArgs e)
        {
            ShowStatus("ツイートしています...");

            if (!File.Exists(this.path))
            {
                MessageBox.Show("スクリーンショットが失われたため、ツイートできません。");
                Close();
            }

            await MainWindow.token.Statuses.UpdateWithMediaAsync(
                      this.Message.Text, new FileStream(this.path, FileMode.Open)
            );

            File.Delete(this.path);

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
    }
}