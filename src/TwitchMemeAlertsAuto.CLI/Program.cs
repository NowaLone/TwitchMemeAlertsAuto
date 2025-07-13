using IrcNet;
using IrcNet.Client;
using IrcNet.Parser.V3;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TwitchChat.Client;
using TwitchChat.EventsArgs;
using TwitchChat.Parser;

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

			rootCommand.SetAction((ParseResult parseResult, CancellationToken cancellationToken) => Work(parseResult.GetValue(channelOption), parseResult.GetValue(tokenOption), parseResult.GetValue(streamerIdOption), parseResult.GetValue(rewardsOption), cancellationToken));

			return rootCommand.Parse(args).InvokeAsync();
		}

		private static async Task<int> Work(string channel, string token, string streamerId, string rewards, CancellationToken cancellationToken = default)
		{
			var logger = GetLogger<Program>();
			var rwrds = rewards.Split(',').ToDictionary(d => d.Split(':')[0], d => int.Parse(d.Split(":")[1]));
			var memeAlertsClient = new HttpClient();
			memeAlertsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			memeAlertsClient.Timeout = TimeSpan.FromSeconds(10);

			var data = await GetDataAsync(memeAlertsClient, cancellationToken).ConfigureAwait(false);

			var client = new TwitchClient(new IrcClientWebSocket(new IrcClientWebSocket.Options() { Uri = new Uri(TwitchClient.Options.wssUrlSSL) }, GetLogger<IrcClientWebSocket>()), new TwitchParser(), new OptionsMonitor<TwitchClient.Options>(new OptionsFactory<TwitchClient.Options>([], []), [], new OptionsCache<TwitchClient.Options>()), GetLogger<TwitchClient>());

			client.JoinChannel(channel);

			client.OnMessageReceived += new EventHandler<MessageReceivedEventArgs>(async (s, e) =>
			{
				if (e.Message is IrcV3Message ircV3Message && ircV3Message.Command == IrcCommand.PRIVMSG && ircV3Message.Parameters.ElementAt(0) == $"#{channel}" && ircV3Message.Tags.TryGetValue("custom-reward-id", out string customRewardId))
				{
					if (rwrds.TryGetValue(customRewardId, out var value))
					{
						var username = ircV3Message.Parameters.ElementAt(1);

						var dataItem = data.Data.FirstOrDefault(d => d.SupporterName.Equals(username.TrimStart(':'), StringComparison.OrdinalIgnoreCase));
						if (dataItem != default)
						{
							if (await AddMemesAsync(memeAlertsClient, dataItem.SupporterId, streamerId, value, cancellationToken).ConfigureAwait(false))
							{
								logger.LogInformation($"Мемы для {username} успешно выданы");
							}
							else
							{
								logger.LogError($"Мемы для {username} не выданы");
							}
						}
						else
						{
							logger.LogWarning($"Саппортёр {username} не найден");
						}
					}
					else
					{
						logger.LogTrace("У сообщения не найден тег custom-reward-id");
					}
				}
			});

			await client.ConnectAsync(cancellationToken).ConfigureAwait(false);

			while (!cancellationToken.IsCancellationRequested)
			{
				try
				{
					await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
					data = await GetDataAsync(memeAlertsClient, cancellationToken).ConfigureAwait(false);
				}
				catch (TaskCanceledException) { }
			}

			return 0;
		}

		private static async Task<Supporters> GetDataAsync(HttpClient memeAlertsClient, CancellationToken cancellationToken = default)
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, "https://memealerts.com/api/supporters");
			request.Content = new StringContent("{\"limit\":100,\"skip\":0,\"query\":\"\",\"filters\":[0]}", new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return await JsonSerializer.DeserializeAsync<Supporters>(responseMessage.Content.ReadAsStream(cancellationToken), SerializationModeOptionsContext.Default.Supporters, cancellationToken).ConfigureAwait(false);
		}

		private static async Task<bool> AddMemesAsync(HttpClient memeAlertsClient, string userId, string streamerId, int value, CancellationToken cancellationToken = default)
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, "https://memealerts.com/api/user/give-bonus");
			request.Content = new StringContent($"{{\"userId\":\"{userId}\",\"streamerId\":\"{streamerId}\",\"value\":{value}}}", new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return (await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
		}

		private static ILogger<T> GetLogger<T>() => LoggerFactory.Create(c => c.AddSimpleConsole(c => c.TimestampFormat = "hh:mm:ss tt ").SetMinimumLevel(LogLevel.Information)).CreateLogger<T>();
	}

	public class Supporters
	{
		public IEnumerable<Supporter> Data { get; set; }
		public int Total { get; set; }
	}

	public class Supporter
	{
		public int Balance { get; set; }
		public string SupporterId { get; set; }
		public string SupporterName { get; set; }
	}

	[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, GenerationMode = JsonSourceGenerationMode.Metadata)]
	[JsonSerializable(typeof(Supporters))]
	internal partial class SerializationModeOptionsContext : JsonSerializerContext
	{
	}
}