using System;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Networking.Connectivity;

namespace AutoPortal.Pages;

public sealed partial class NetworkCheckPage : Page
{
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };

    private Border? _localNetCard;
    private Border? _portalCard;
    private Border? _internetCard;
    private Border? _dnsCard;

    public NetworkCheckPage()
    {
        InitializeComponent();
        NavigationCacheMode = Microsoft.UI.Xaml.Navigation.NavigationCacheMode.Enabled;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ResultsPanel.Children.Count == 0)
        {
            InitializeCards();
        }
    }

    private void InitializeCards()
    {
        _localNetCard = CreateCheckCard("本地网络连接", "\uE909");
        _portalCard = CreateCheckCard("Portal 服务器", "\uE909");
        _internetCard = CreateCheckCard("互联网连接", "\uE12B");
        _dnsCard = CreateCheckCard("DNS 解析", "\uE12A");

        ResultsPanel.Children.Add(_localNetCard);
        ResultsPanel.Children.Add(_portalCard);
        ResultsPanel.Children.Add(_internetCard);
        ResultsPanel.Children.Add(_dnsCard);
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isRunning) return;

            _isRunning = true;
            _cts = new CancellationTokenSource();

            StartButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Visible;

            ResetCard(_localNetCard);
            ResetCard(_portalCard);
            ResetCard(_internetCard);
            ResetCard(_dnsCard);

            var checks = new (Border? card, Func<Task<(bool, string)>> check)[]
            {
                (_localNetCard, CheckLocalNetworkAsync),
                (_portalCard, CheckPortalServerAsync),
                (_internetCard, CheckInternetAsync),
                (_dnsCard, CheckDnsAsync),
            };

            try
            {
                for (int i = 0; i < checks.Length; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();
                    var (card, check) = checks[i];

                    if (card == null) continue;

                    try
                    {
                        var (success, msg) = await check();
                        UpdateCard(card, success, msg);
                    }
                    catch (Exception ex)
                    {
                        UpdateCard(card, false, ex.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _isRunning = false;
                _cts?.Dispose();
                _cts = null;
                StartButton.Visibility = Visibility.Visible;
                CancelButton.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = ex.Message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    private static Border CreateCheckCard(string name, string icon)
    {
        var leftPanel = new StackPanel { Spacing = 4 };

        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, VerticalAlignment = VerticalAlignment.Center };

        headerPanel.Children.Add(new FontIcon
        {
            Glyph = icon,
            FontSize = 20,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 0, 120, 212))
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = name,
            FontSize = 15,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });

        var errorText = new TextBlock
        {
            FontSize = 12,
            Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 200, 0, 0)),
            Visibility = Visibility.Collapsed,
            TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap
        };

        leftPanel.Children.Add(headerPanel);
        leftPanel.Children.Add(errorText);

        var progressRing = new ProgressRing
        {
            Width = 24,
            Height = 24,
            IsActive = false,
            Visibility = Visibility.Collapsed
        };

        var statusIcon = new FontIcon
        {
            Glyph = "\uE73E",
            FontSize = 20,
            Visibility = Visibility.Collapsed
        };

        var rightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        rightPanel.Children.Add(progressRing);
        rightPanel.Children.Add(statusIcon);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(leftPanel);
        Grid.SetColumn(leftPanel, 0);

        grid.Children.Add(rightPanel);
        Grid.SetColumn(rightPanel, 1);

        var border = new Border
        {
            Padding = new Thickness(20, 16, 20, 16),
            CornerRadius = new CornerRadius(12),
            Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            BorderThickness = new Thickness(1),
            Child = grid
        };

        border.Tag = errorText;

        return border;
    }

    private static void ResetCard(Border? card)
    {
        if (card == null) return;

        var grid = card.Child as Grid;
        if (grid == null) return;

        var rightPanel = grid.Children[1] as StackPanel;
        var errorText = card.Tag as TextBlock;

        if (rightPanel != null)
        {
            var progressRing = rightPanel.Children[0] as ProgressRing;
            var statusIcon = rightPanel.Children[1] as FontIcon;

            if (progressRing != null)
            {
                progressRing.IsActive = true;
                progressRing.Visibility = Visibility.Visible;
            }

            if (statusIcon != null)
                statusIcon.Visibility = Visibility.Collapsed;
        }

        if (errorText != null)
        {
            errorText.Text = "";
            errorText.Visibility = Visibility.Collapsed;
        }

        card.BorderBrush = (Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"];
    }

    private static void UpdateCard(Border card, bool success, string msg)
    {
        var grid = card.Child as Grid;
        if (grid == null) return;

        var rightPanel = grid.Children[1] as StackPanel;
        var errorText = card.Tag as TextBlock;

        if (rightPanel != null)
        {
            var progressRing = rightPanel.Children[0] as ProgressRing;
            var statusIcon = rightPanel.Children[1] as FontIcon;

            if (progressRing != null)
                progressRing.Visibility = Visibility.Collapsed;

            if (statusIcon != null)
            {
                statusIcon.Visibility = Visibility.Visible;
                statusIcon.Glyph = success ? "\uE73E" : "\uE7BA";
                statusIcon.Foreground = success
                    ? new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 16, 124, 16))
                    : new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 200, 0, 0));
            }
        }

        if (!success && errorText != null)
        {
            errorText.Text = msg;
            errorText.Visibility = Visibility.Visible;
        }
    }

    private Task<(bool, string)> CheckLocalNetworkAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var profile = NetworkInformation.GetInternetConnectionProfile();
                if (profile == null)
                    return (false, "未检测到网络连接");

                var level = profile.GetNetworkConnectivityLevel();
                if (level == NetworkConnectivityLevel.InternetAccess)
                    return (true, "正常");

                return (false, $"连接级别：{level}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        });
    }

    private async Task<(bool, string)> CheckPortalServerAsync()
    {
        try
        {
            using var tcp = new System.Net.Sockets.TcpClient();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var connectTask = tcp.ConnectAsync("10.189.108.11", 80);

            if (await Task.WhenAny(connectTask, Task.Delay(3000)) == connectTask)
            {
                sw.Stop();
                return (true, $"成功，延迟 {sw.ElapsedMilliseconds}ms");
            }

            return (false, "连接超时");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool, string)> CheckInternetAsync()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var resp = await _http.GetAsync("https://www.baidu.com");
            sw.Stop();
            return resp.IsSuccessStatusCode
                ? (true, $"正常，延迟 {sw.ElapsedMilliseconds}ms")
                : (false, $"HTTP {resp.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private async Task<(bool, string)> CheckDnsAsync()
    {
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var resp = await _http.GetAsync("https://www.baidu.com");
            sw.Stop();
            return resp.IsSuccessStatusCode
                ? (true, $"正常，延迟 {sw.ElapsedMilliseconds}ms")
                : (false, "DNS解析失败");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
