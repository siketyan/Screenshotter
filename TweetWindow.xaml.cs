using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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
        }

        private async void TweetAsync(object sender, RoutedEventArgs e)
        {
            ShowStatus("ツイートしています...");

            if (!File.Exists(path))
            {
                MessageBox.Show("スクリーンショットが失われたため、ツイートできません。");
                Close();
            }

            await MainWindow.token.Statuses.UpdateWithMediaAsync(
                      Message.Text, new FileStream(path, FileMode.Open)
            );

            File.Delete(path);

            CloseStatus(async () =>
            {
                ShowStatus("ツイートを送信しました。", false);
                await Task.Run(() => Thread.Sleep(1000));
                CloseStatus();
                Close();
            });
        }

        private void ShowStatus(string message, bool isLoader = true)
        {
            Status.Content = message;
            Loader.Visibility = (isLoader) ? Visibility.Visible
                                           : Visibility.Collapsed;
            Success.Visibility = (isLoader) ? Visibility.Collapsed
                                            : Visibility.Visible;

            Storyboard sb = FindResource("DialogShowAnimation") as Storyboard;
            Storyboard.SetTarget(sb, StatusGrid);
            sb.Completed += (s, a) => StatusGrid.IsHitTestVisible = true;
            sb.Begin();
        }

        private void CloseStatus(Action doAfterClose = null)
        {
            Storyboard sb = FindResource("DialogCloseAnimation") as Storyboard;
            Storyboard.SetTarget(sb, StatusGrid);
            sb.Completed += (s, a) =>
            {
                StatusGrid.IsHitTestVisible = false;
                doAfterClose?.Invoke();
            };
            sb.Begin();
        }
    }
}