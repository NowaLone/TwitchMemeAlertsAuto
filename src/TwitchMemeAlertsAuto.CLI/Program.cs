using IrcNet.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using TwitchChat.Client;
using TwitchChat.Parser;
using TwitchMemeAlertsAuto.Core;

namespace TwitchMemeAlertsAuto.CLI
{
	internal class Program
	{
		public static Task<int> Main(string[] args)
		{
			using var cts = new CancellationTokenSource();

			var rootCommand = new RootCommand($"{AppDomain.CurrentDomain.FriendlyName} is the utility for auto reward memealerts supporters via twitch points.");

			var channelOption = new Option<string>("--channel", "-c") { Description = "Название канала с которого считывать заказы.", Required = true };
			var tokenOption = new Option<string>("--token", "-t") { Description = "Токен для работы с memealerts.", Required = true };
			var streamerIdOption = new Option<string>("--streamerId", "-s") { Description = "streamerId в memealerts.", Required = true };
			var rewardsOption = new Option<string>("--rewards", "-r") { Description = "id наград и их ценность в формате id1:value1,id2:value2...", Required = true };

			rootCommand.Add(channelOption);
			rootCommand.Add(tokenOption);
			rootCommand.Add(streamerIdOption);
			rootCommand.Add(rewardsOption);

			rootCommand.SetAction((ParseResult parseResult, CancellationToken cancellationToken) => new TwitchMemeAlertsAutoService(
				new TwitchClient(new IrcClientWebSocket(new IrcClientWebSocket.Options() { Uri = new Uri(TwitchClient.Options.wssUrlSSL) }, GetLogger<IrcClientWebSocket>()), new TwitchParser(), new OptionsMonitor<TwitchClient.Options>(new OptionsFactory<TwitchClient.Options>([], []), [], new OptionsCache<TwitchClient.Options>()), GetLogger<TwitchClient>()),
				GetLogger<TwitchMemeAlertsAutoService>()).Work(parseResult.GetValue(channelOption), parseResult.GetValue(tokenOption), parseResult.GetValue(rewardsOption), cancellationToken));

			try
			{
				return rootCommand.Parse(args).InvokeAsync(cts.Token);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				Console.ReadKey();
				return Task.FromResult(-1);
			}
		}

		private static ILogger<T> GetLogger<T>() => LoggerFactory.Create(c => c.AddSimpleConsole(c => c.TimestampFormat = "hh:mm:ss tt ").SetMinimumLevel(LogLevel.Information)).CreateLogger<T>();
	}
}