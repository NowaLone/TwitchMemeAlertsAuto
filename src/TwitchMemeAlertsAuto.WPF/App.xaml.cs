using IrcNet.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Windows;
using TwitchChat.Client;
using TwitchChat.Parser;
using TwitchMemeAlertsAuto.Core;

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

			builder.Logging.AddDebug();
			builder.Logging.SetMinimumLevel(LogLevel.Debug);

			builder.Services.AddDbContextFactory<TmaaDbContext>((o) => o.UseSqlite($"Data Source={Path.Join(Directory.GetCurrentDirectory(), "tmaa.db")}"))
				.AddTransient<ISettingsService, SettingsService>()
				.AddTransient<ITwitchOAuthService, TwitchOAuthService>()
				.AddTransient<ITwitchMemeAlertsAutoService, TwitchMemeAlertsAutoService>()
				.AddTransient<ITwitchClient, TwitchClient>((sp) => new TwitchClient(new IrcClientWebSocket(new IrcClientWebSocket.Options() { Uri = new Uri(TwitchClient.Options.wssUrlSSL) }, sp.GetRequiredService<ILogger<IrcClientWebSocket>>()), new TwitchParser(), new OptionsMonitor<TwitchClient.Options>(new OptionsFactory<TwitchClient.Options>([], []), [], new OptionsCache<TwitchClient.Options>()), sp.GetRequiredService<ILogger<TwitchClient>>()));

			Host = builder.Build();

			using (var scope = Host.Services.CreateAsyncScope())
			{
				using (var context = scope.ServiceProvider.GetRequiredService<TmaaDbContext>())
				{
					context.Database.EnsureCreated();
				}
			}

			base.OnStartup(e);
		}

		private async void Application_Startup(object sender, StartupEventArgs e)
		{
			var mainWindow = new MainWindow();

			mainWindow.Show();
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