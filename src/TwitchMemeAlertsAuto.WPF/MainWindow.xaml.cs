using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using TwitchLib.Api.Core;
using TwitchMemeAlertsAuto.Core;

namespace TwitchMemeAlertsAuto.WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly ISettingsService settingsService;
		private readonly ITwitchOAuthService twitchOAuthService;
		private readonly ITwitchMemeAlertsAutoService twitchMemeAlertsAutoService;
		private readonly ILogger logger;
		private CancellationTokenSource cancellationTokenSource;
		private Task<int> work;
		private ObservableCollection<string> Log;

		public MainWindow()
		{
			this.settingsService = App.Host.Services.GetRequiredService<ISettingsService>();
			this.twitchOAuthService = App.Host.Services.GetRequiredService<ITwitchOAuthService>();
			this.twitchMemeAlertsAutoService = App.Host.Services.GetRequiredService<ITwitchMemeAlertsAutoService>();
			twitchMemeAlertsAutoService.OnMemesReceived += TwitchMemeAlertsAutoService_OnMemesReceived;
			twitchMemeAlertsAutoService.OnMemesNotReceived += TwitchMemeAlertsAutoService_OnMemesNotReceived;
			twitchMemeAlertsAutoService.OnSupporterNotFound += TwitchMemeAlertsAutoService_OnSupporterNotFound;
			twitchMemeAlertsAutoService.OnSupporterLoaded += TwitchMemeAlertsAutoService_OnSupporterLoaded;
			twitchMemeAlertsAutoService.OnSupporterLoading += TwitchMemeAlertsAutoService_OnSupporterLoading;
			this.logger = App.Host.Services.GetRequiredService<ILogger<MainWindow>>();

			Log = new ObservableCollection<string>();
			InitializeComponent();
		}

		private void TwitchMemeAlertsAutoService_OnSupporterNotFound(string username)
		{ Dispatcher.Invoke(new Action(() => Log.Add($"Саппортёр {username} не найден")));
		}

		private void TwitchMemeAlertsAutoService_OnSupporterLoading()
		{
			Dispatcher.Invoke(new Action(() => Log.Add("Обновление саппортёров...")));
		}

		private void TwitchMemeAlertsAutoService_OnSupporterLoaded(int count)
		{
			Dispatcher.Invoke(new Action(() => Log.Add($"Загружено {count} саппортёров")));
		}

		private void TwitchMemeAlertsAutoService_OnMemesNotReceived(string username)
		{
			Dispatcher.Invoke(new Action(() => Log.Add($"Мемы для {username} не выданы")));
		}

		private void TwitchMemeAlertsAutoService_OnMemesReceived(string username)
		{
			Dispatcher.Invoke(new Action(() => Log.Add($"Мемы для {username} успешно выданы")));
		}

		private async void ConnectMemeAlertsButton_Click(object sender, RoutedEventArgs e)
		{
			var MaToken = await settingsService.GetMemeAlertsTokenAsync();

			if (string.IsNullOrWhiteSpace(MaToken) || !(await twitchMemeAlertsAutoService.CheckToken(MaToken)))
			{
				using (var webView2 = new WebView2 { Source = new Uri("https://memealerts.com/") })
				{
					var window = new Window
					{
						Content = webView2,
						Owner = this,
					};

					var result = window.ShowDialog();

					if (result.HasValue)
					{
						var cook1 = await webView2.CoreWebView2.ExecuteScriptWithResultAsync("localStorage.getItem('accessToken')");
						if (cook1.Succeeded)
						{
							cook1.TryGetResultAsString(out var stringResult, out var value);

							await settingsService.SetMemeAlertsTokenAsync(stringResult);
							await CheckMemeAlertsAsync();
						}
						else
						{
							logger.LogError("Не удалось получить токен MemeAlerts!");
							throw new ApplicationException("Не удалось получить токен MemeAlerts!");
						}
					}
				}
			}
		}

		private async void ConnectTwitchButton_Click(object sender, RoutedEventArgs e)
		{
			var oauthToken = await settingsService.GetTwitchOAuthTokenAsync();

			if (string.IsNullOrWhiteSpace(oauthToken))
			{
				await twitchOAuthService.AuthenticateAsync();
				await CheckTwitchTokenAsync();
			}
		}

		private async Task<string> CheckTwitchTokenAsync(CancellationToken cancellationToken = default)
		{
			var oauthToken = await settingsService.GetTwitchOAuthTokenAsync(cancellationToken);
			var refreshToken = await settingsService.GetTwitchRefreshTokenAsync(cancellationToken);
			var expiresIn = await settingsService.GetTwitchExpiresInAsync(cancellationToken);
			var userId = await settingsService.GetTwitchUserIdAsync(cancellationToken);

			if (!string.IsNullOrWhiteSpace(oauthToken))
			{
				var isTokenValid = await twitchOAuthService.ValidateTokenAsync(oauthToken, cancellationToken);

				if (isTokenValid != null)
				{
					var client = new TwitchLib.Api.TwitchAPI(App.Host.Services.GetRequiredService<ILoggerFactory>(), null, new ApiSettings { AccessToken = oauthToken, ClientId = "mysd83coqn8u0sf40aev6nvsqqlyjy" });
					var rewards = await client.Helix.ChannelPoints.GetCustomRewardAsync(userId);
					RewardsListView.ItemsSource = rewards.Data.OrderBy(r => r.Cost);

					ConnectTwitchButtonStausRun.Foreground = Brushes.Green;
					ConnectTwitchButtonStausRun.Text = "✔";
					return oauthToken;
				}
				else
				{
					var result = await twitchOAuthService.AuthenticateAsync(refreshToken, cancellationToken);

					if (result != null)
					{
						return await CheckTwitchTokenAsync(cancellationToken);
					}
					else
					{
						logger.LogWarning("OAuth token is invalid or expired.");
						await settingsService.SetTwitchOAuthTokenAsync(string.Empty, cancellationToken);
					}
				}
			}

			ConnectTwitchButtonStausRun.Foreground = Brushes.Red;
			ConnectTwitchButtonStausRun.Text = "❌";
			return null;
		}

		private async Task<string> CheckMemeAlertsAsync(CancellationToken cancellationToken = default)
		{
			var MaToken = await settingsService.GetMemeAlertsTokenAsync(cancellationToken);

			if (!string.IsNullOrWhiteSpace(MaToken) && (await twitchMemeAlertsAutoService.CheckToken(MaToken, cancellationToken)))
			{
				ConnectMemeAlertsButtonStatusRun.Foreground = Brushes.Green;
				ConnectMemeAlertsButtonStatusRun.Text = "✔";
				return MaToken;
			}
			else
			{
				ConnectMemeAlertsButtonStatusRun.Foreground = Brushes.Red;
				ConnectMemeAlertsButtonStatusRun.Text = "❌";
				return null;
			}
		}

		private async void Window_Loaded(object sender, RoutedEventArgs e)
		{
			cancellationTokenSource = new CancellationTokenSource();
			var TwToken = await CheckTwitchTokenAsync(cancellationTokenSource.Token);
			var MaToken = await CheckMemeAlertsAsync(cancellationTokenSource.Token);

			if (!string.IsNullOrWhiteSpace(TwToken) && !string.IsNullOrWhiteSpace(MaToken))
			{
				var client = new TwitchLib.Api.TwitchAPI(App.Host.Services.GetRequiredService<ILoggerFactory>(), null, new ApiSettings { AccessToken = TwToken, ClientId = "mysd83coqn8u0sf40aev6nvsqqlyjy" });
				var userId = await settingsService.GetTwitchUserIdAsync(cancellationTokenSource.Token);

				var channelInformationResponse = await client.Helix.Channels.GetChannelInformationAsync(userId);

				var rewards = string.Empty;
				using (var scope = App.Host.Services.CreateAsyncScope())
				{
					using (var context = scope.ServiceProvider.GetRequiredService<TmaaDbContext>())
					{
						rewards = string.Join(",", context.Settings.Where(s => s.Key.StartsWith("Reward:")).Select(r => r.Key.Replace("Reward:", string.Empty) + ":" + r.Value));
					}
				}

				LogListView.ItemsSource = Log;
				twitchMemeAlertsAutoService.Work(channelInformationResponse.Data.First().BroadcasterLogin, MaToken, rewards, cancellationTokenSource.Token);

			}

			e.Handled = true;
		}
	}
}