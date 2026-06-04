using IrcNet;
using IrcNet.Client;
using IrcNet.Client.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using ProfanityFilter.Interfaces;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Windows;
using TwitchChat.Client;
using TwitchChat.Parser;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchLib.EventSub.Websockets.Extensions;
using TwitchMemeAlertsAuto.Core;
using TwitchMemeAlertsAuto.Core.Logging;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels;
using TwitchMemeAlertsAuto.WPF.Services;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace TwitchMemeAlertsAuto.WPF
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private IHost host;

		private static Mutex mutex;

		protected override void OnStartup(StartupEventArgs e)
		{
			var fileVersionInfo = FileVersionInfo.GetVersionInfo(Environment.ProcessPath);

			var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileVersionInfo.CompanyName, fileVersionInfo.ProductName);
			Directory.CreateDirectory(dbPath);

			// Try to gain exclusive ownership of the named mutex
			var mutex = new Mutex(true, new Guid(Encoding.UTF8.GetBytes(dbPath, 0, 16)).ToString(), out bool isNewInstance);

			if (!isNewInstance)
			{
				// Another instance is already running; warn and exit immediately
				MessageBox.Show("The application is already running.", "Instance Check", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				Application.Current.Shutdown(1);
				return;
			}
			else
			{
				App.mutex = mutex;
			}

			var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(e.Args);

#if DEBUG
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
			builder.Logging.AddDebug();
#endif

			// Add custom WPF logger provider to capture logs with EventId
			builder.Logging.AddProvider(new WpfLoggerProvider(LogLevel.Information));

			if (builder.Configuration.GetSection("Logging:LogLevel").GetChildren().Any(c => Enum.TryParse<LogLevel>(c.Value, true, out var result) && result < LogLevel.Information))
			{
				builder.Logging.AddFile(o =>
				{
					o.RootPath = builder.Environment.ContentRootPath;
					o.Files = [new Karambolo.Extensions.Logging.File.LogFileOptions { DateFormat = "yyyyMMdd", Path = "<date>-<counter>.log" }];
				});
			}

			builder.Services.AddDbContextFactory<TmaaDbContext>((o) => o.UseSqlite($"Data Source={Path.Join(dbPath, "tmaa.db")}", b => b.MigrationsAssembly(typeof(Core.Migrations.Sqlite.Migrations.InitialCreate).Assembly.GetName().Name)))
				.AddSingleton<IRewardsService, RewardsService>()
				.AddSingleton<IDispatcherService, DispatcherService>()
				.AddTransient<ISettingsService, SettingsService>()
				.AddTransient<ITwitchOAuthService, TwitchOAuthService>()
				.AddTransient<IMemeAlertsService, MemeAlertsService>()
				.AddTransient<IProfanityFilter, ProfanityFilter.ProfanityFilter>(sp =>
				{
					var filter = new ProfanityFilter.ProfanityFilter();
					filter.AddProfanity(new string[] { "нигер", "ниггер", "нига", "нигга", "нага", "черножопый", "черномазый", "пидор", "педик", "пидорас", "гомосек", "гомик", "петух", "хохол", "русня", "хач", "жид", "чурка", "даун", "симп", "инцел", "куколд", "хайль" });
					return filter;
				})
				.AddTransient<MainWindowViewModel>()
				.AddTransient<RewardViewModel>()
				.AddTransient<ConnectionViewModel>()
				.AddTransient<RewardsViewModel>()
				.AddTransient<LogViewModel>()
				.AddTransient<MainMenuViewModel>()
				.AddTransient<AllRewardViewModel>()
				.AddTransient<SupporterViewModel>()
				.AddTransient<SupportersViewModel>()
				.AddTwitchLibEventSubWebsockets()
				.AddTransient<ITwitchAPI, TwitchAPI>(sp =>
				{
					var api = new TwitchAPI(sp.GetRequiredService<ILoggerFactory>());
					api.Settings.ClientId = "mysd83coqn8u0sf40aev6nvsqqlyjy";

					using (var scope = sp.CreateScope())
					{
						var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
						api.Settings.AccessToken = settingsService.GetTwitchOAuthTokenAsync().GetAwaiter().GetResult();
					}

					return api;
				})
				.AddTransient<IIrcParser<TwitchMessage>, TwitchParser>()
				.AddTransient(s => s.GetRequiredService<IOptions<IrcClientWebSocket.Options>>().Value)
				.AddIrcWebSocketClient(o => o.Uri = new Uri(TwitchClient.Options.wssUrlSSL))
				.AddSingleton<ITwitchClient, TwitchClient>((sp) =>
				{
					string token, nickname;
					using (var scope = sp.CreateScope())
					{
						var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
						token = settingsService.GetTwitchOAuthTokenAsync().GetAwaiter().GetResult();
						nickname = settingsService.GetTwitchUsernameAsync().GetAwaiter().GetResult();
					}
					var options = sp.GetRequiredService<IOptionsMonitor<TwitchClient.Options>>();
					options.CurrentValue.OAuthToken = token;
					options.CurrentValue.Nickname = nickname;
					return new TwitchClient(sp.GetRequiredService<IIrcClientWebSocket>(), sp.GetRequiredService<IIrcParser<TwitchMessage>>(), options, sp.GetRequiredService<ILogger<TwitchClient>>());
				})
				.AddTransient<IWebsocketHostedService, WebsocketHostedService>()
				.AddHostedService<TwitchTokenRefreshHostedService>()
				.AddHttpClient(nameof(MemeAlertsService), async (sp, client) =>
				{
					client.Timeout = TimeSpan.FromSeconds(30);
					client.BaseAddress = new Uri("https://memealerts.com");

					using (var scope = sp.CreateAsyncScope())
					{
						var settingsService = sp.GetRequiredService<ISettingsService>();
						client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await settingsService.GetMemeAlertsTokenAsync().ConfigureAwait(false));
					}
				})
				.SetHandlerLifetime(TimeSpan.FromMinutes(5))
				.AddPolicyHandler(HttpPolicyExtensions.HandleTransientHttpError()
					.Or<TimeoutRejectedException>()
					.WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

			host = builder.Build();

			base.OnStartup(e);
		}

		protected override void OnExit(ExitEventArgs e)
		{
			if (mutex != null)
			{
				mutex.ReleaseMutex();
				mutex.Dispose();
			}

			base.OnExit(e);
		}

		private async void Application_Startup(object sender, StartupEventArgs e)
		{
			var mainWindowViewModel = host.Services.GetRequiredService<MainWindowViewModel>();

			var mainWindow = new MainWindow
			{
				DataContext = mainWindowViewModel
			};

			MainWindow = mainWindow;

			if (!e.Args.Contains("--silent"))
			{
				mainWindow.Show();
			}

			ShowTray();

			using (var scope = host.Services.CreateAsyncScope())
			{
				using (var context = scope.ServiceProvider.GetRequiredService<TmaaDbContext>())
				{
					await context.Database.MigrateAsync().ConfigureAwait(false);
				}
			}

			mainWindowViewModel.IsActive = true;

			await host.StartAsync();
		}

		private void ShowTray()
		{
			var contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
			contextMenuStrip.Items.Add(Core.Properties.Resources.ExitMenu, null, (s, e) => Application.Current.Shutdown());
			var icon = new NotifyIcon
			{
				Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/favicon-96x96.ico")).Stream),
				Visible = true,
				Text = MainWindow.Title,
				ContextMenuStrip = contextMenuStrip
			};

			icon.DoubleClick += (s, e) =>
			{
				if (Current.MainWindow.IsVisible)
				{
					Current.MainWindow.Hide();
				}
				else
				{
					Current.MainWindow.Show();
				}
			};
		}

		private async void Application_Exit(object sender, ExitEventArgs e)
		{
			if (host != null)
			{
				using (host)
				{
					await host.StopAsync(TimeSpan.FromSeconds(3));
				}
			}
		}

		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			if (this.MainWindow != null)
			{
				MessageBox.Show(this.MainWindow, e.Exception.Message, e.Exception.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
			}
			else
			{
				MessageBox.Show(e.Exception.Message, e.Exception.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
			}

			e.Handled = true;
		}
	}
}