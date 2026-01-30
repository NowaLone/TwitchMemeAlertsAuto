using IrcNet.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using TwitchChat.Client;
using TwitchChat.Parser;
using TwitchLib.Api;
using TwitchLib.Api.Interfaces;
using TwitchMemeAlertsAuto.Core;
using TwitchMemeAlertsAuto.Core.Logging;
using TwitchMemeAlertsAuto.Core.ViewModels;
using TwitchMemeAlertsAuto.WPF.Services;

namespace TwitchMemeAlertsAuto.WPF
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		public static IHost Host;

		protected override void OnStartup(StartupEventArgs e)
		{
			var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(e.Args);

#if DEBUG
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
			builder.Logging.AddDebug();
			//builder.Logging.SetMinimumLevel(LogLevel.Debug);
#endif
			var assembly = Assembly.GetExecutingAssembly();
			var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);

			var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), fileVersionInfo.CompanyName, fileVersionInfo.ProductName);
			Directory.CreateDirectory(dbPath);

			// Add custom WPF logger provider to capture logs with EventId
			builder.Logging.AddProvider(new WpfLoggerProvider(LogLevel.Information));

			builder.Services.AddDbContextFactory<TmaaDbContext>((o) => o.UseSqlite($"Data Source={Path.Join(dbPath, "tmaa.db")}", b => b.MigrationsAssembly(typeof(Core.Migrations.Sqlite.Migrations.InitialCreate).Assembly.GetName().Name)))
				.AddSingleton<IRewardsService, RewardsService>()
				.AddSingleton<IDispatcherService, DispatcherService>()
				.AddTransient<ISettingsService, SettingsService>()
				.AddTransient<ITwitchOAuthService, TwitchOAuthService>()
				.AddTransient<ITwitchMemeAlertsAutoService, TwitchMemeAlertsAutoService>()
				.AddTransient<MainWindowViewModel>()
				.AddTransient<RewardViewModel>()
				.AddTransient<ConnectionViewModel>()
				.AddTransient<RewardsViewModel>()
				.AddTransient<LogViewModel>()
				.AddTransient<MainMenuViewModel>()
				.AddTransient<AllRewardViewModel>()
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
				.AddTransient<ITwitchClient, TwitchClient>((sp) => new TwitchClient(new IrcClientWebSocket(new IrcClientWebSocket.Options() { Uri = new Uri(TwitchClient.Options.wssUrlSSL) }, sp.GetRequiredService<ILogger<IrcClientWebSocket>>()), new TwitchParser(), new OptionsMonitor<TwitchClient.Options>(new OptionsFactory<TwitchClient.Options>(new List<IConfigureOptions<TwitchClient.Options>>(), new List<IPostConfigureOptions<TwitchClient.Options>>()), new List<IOptionsChangeTokenSource<TwitchClient.Options>>(), new OptionsCache<TwitchClient.Options>()), sp.GetRequiredService<ILogger<TwitchClient>>()));

			Host = builder.Build();

			using (var scope = Host.Services.CreateAsyncScope())
			{
				using (var context = scope.ServiceProvider.GetRequiredService<TmaaDbContext>())
				{
					context.Database.Migrate();
				}
			}

			base.OnStartup(e);
		}

		private async void Application_Startup(object sender, StartupEventArgs e)
		{
			var mainWindowViewModel = Host.Services.GetRequiredService<MainWindowViewModel>();

			var mainWindow = new MainWindow
			{
				DataContext = mainWindowViewModel
			};

			MainWindow = mainWindow;
			mainWindow.Show();
			mainWindowViewModel.IsActive = true;

			await Host.StartAsync();
		}

		private async void Application_Exit(object sender, ExitEventArgs e)
		{
			using (Host)
			{
				await Host.StopAsync(TimeSpan.FromSeconds(3));
			}
		}

		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(this.MainWindow, e.Exception.Message, e.Exception.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}
	}
}