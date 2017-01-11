using CoreTweet;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Screenshotter
{
    /// <summary>
    /// AuthorizeWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AuthorizeWindow : Window
    {
        private OAuth.OAuthSession session;

        public AuthorizeWindow()
        {
            InitializeComponent();
        }

        private async void InitAsync(object sender, RoutedEventArgs e)
        {
            session = await OAuth.AuthorizeAsync(
                __Private.ConsumerKey,
                __Private.ConsumerSecret
            );

            AuthorizeUrl.Text = session.AuthorizeUri.ToString();
        }

        private async void AuthorizeAsync(object sender, RoutedEventArgs e)
        {
            ShowStatus("認証しています...");

            var token = await session.GetTokensAsync(Pin.Text);
            var credentials = new Credentials()
            {
                AccessToken = token.AccessToken,
                AccessSecret = token.AccessTokenSecret
            };

            await Task.Run(() =>
            {
                var json = JsonConvert.SerializeObject(credentials);
                var writer = File.CreateText(MainWindow.location + @"\credentials.json");

                writer.Write(json);
                writer.Close();
            });

            CloseStatus(async () =>
            {
                ShowStatus("認証しました。", false);
                await Task.Run(() => Thread.Sleep(1000));
                CloseStatus();
                Close();
            });
        }

        private void CopyUrl(object sender, RoutedEventArgs e)
        {
            Clipboard.SetDataObject(AuthorizeUrl.Text, true);
        }

        private void OpenInBrowser(object sender, RoutedEventArgs e)
        {
            Process.Start(AuthorizeUrl.Text);
        }

        private void CheckPin(object sender, TextCompositionEventArgs e)
        {
            bool yesParse;

            var tmp = Pin.Text + e.Text;
            yesParse = float.TryParse(tmp, out float xx);

            e.Handled = !yesParse;
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