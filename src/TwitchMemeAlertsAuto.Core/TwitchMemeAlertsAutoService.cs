using IrcNet;
using IrcNet.Parser.V3;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwitchChat.Client;
using TwitchChat.EventsArgs;

namespace TwitchMemeAlertsAuto.Core
{
	public class TwitchMemeAlertsAutoService : ITwitchMemeAlertsAutoService
	{
		private readonly ITwitchClient twitchClient;
		private readonly ILogger<TwitchMemeAlertsAutoService> logger;

		private HttpClient memeAlertsClient;

		public TwitchMemeAlertsAutoService(ITwitchClient twitchClient, ILogger<TwitchMemeAlertsAutoService> logger)
		{
			this.twitchClient = twitchClient;
			this.logger = logger;
		}

		public event Action<string> OnMemesReceived;

		public event Action<string> OnMemesNotReceived;

		public event Action<string> OnSupporterNotFound;

		public event Action<string> OnSupporterLoading;

		public event Action<string> OnSupporterLoaded;

		public async Task<int> Work(string channel, string token, string rewards, CancellationToken cancellationToken = default)
		{
			var rwrds = rewards.Contains(":") ? rewards.Split(',').ToDictionary(d => d.Split(':')[0], d => int.Parse(d.Split(":")[1])) : new Dictionary<string, int>();
			memeAlertsClient = new HttpClient();
			memeAlertsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			memeAlertsClient.Timeout = TimeSpan.FromSeconds(10);

			var current = await GetMemeAlertsId(memeAlertsClient, cancellationToken).ConfigureAwait(false);
			var data = await GetDataAsync(memeAlertsClient, cancellationToken).ConfigureAwait(false);

			twitchClient.JoinChannel(channel);

			twitchClient.OnMessageReceived += new EventHandler<MessageReceivedEventArgs>(async (s, e) =>
			{
				if (e.Message is IrcV3Message ircV3Message && ircV3Message.Command == IrcCommand.PRIVMSG && ircV3Message.Parameters.ElementAt(0) == $"#{channel}" && ircV3Message.Tags.TryGetValue("custom-reward-id", out string customRewardId))
				{
					if (rwrds.TryGetValue(customRewardId, out var value))
					{
						var username = ircV3Message.Parameters.ElementAt(1);

						var dataItem = data.FirstOrDefault(d => d.SupporterName.Equals(username.TrimStart(':'), StringComparison.OrdinalIgnoreCase));

						if (dataItem == null)
						{
							data = await GetDataAsync(memeAlertsClient, cancellationToken).ConfigureAwait(false);
							dataItem = data.FirstOrDefault(d => d.SupporterName.Equals(username.TrimStart(':'), StringComparison.OrdinalIgnoreCase));
						}

						if (dataItem != default)
						{
							if (await AddMemesAsync(memeAlertsClient, dataItem.SupporterId, current.Id, value, cancellationToken).ConfigureAwait(false))
							{
								logger.LogInformation("Мемы для {username} успешно выданы в кол-ве {value} шт.", username, value);
								OnMemesReceived?.Invoke($"Мемы для {username} успешно выданы в кол-ве {value} шт.");
							}
							else
							{
								logger.LogError("Мемы для {username} не выданы", username);
								OnMemesNotReceived?.Invoke($"Мемы для {username} не выданы");
							}
						}
						else
						{
							logger.LogWarning("Саппортёр {username} не найден", username);
							OnSupporterNotFound?.Invoke($"Саппортёр {username} не найден");
						}
					}
					else
					{
						logger.LogTrace("У сообщения не найден тег custom-reward-id");
					}
				}
			});

			await twitchClient.ConnectAsync(cancellationToken).ConfigureAwait(false);

			try
			{
				await Task.Delay(-1, cancellationToken).ConfigureAwait(false);
			}
			catch (TaskCanceledException)
			{
				await twitchClient.DisconnectAsync().ConfigureAwait(false);
			}

			return 0;
		}

		public async Task<bool> CheckToken(string token, CancellationToken cancellationToken = default)
		{
			var memeAlertsClient = new HttpClient();
			memeAlertsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			memeAlertsClient.Timeout = TimeSpan.FromSeconds(10);

			try
			{
				await GetMemeAlertsId(memeAlertsClient, cancellationToken).ConfigureAwait(false);
			}
			catch (HttpRequestException ex)
			{
				if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
				{
					return false;
				}
				else
				{
					logger.LogError(ex, "Ошибка при проверке токена");
					return false;
				}
			}

			return true;
		}

		public async Task RewardAllAsync(int value, CancellationToken cancellationToken = default)
		{
			var current = await GetMemeAlertsId(memeAlertsClient, cancellationToken).ConfigureAwait(false);
			var supporters = await GetDataAsync(memeAlertsClient, cancellationToken).ConfigureAwait(false);

			foreach (var supporter in supporters)
			{
				if (await AddMemesAsync(memeAlertsClient, supporter.SupporterId, current.Id, value, cancellationToken).ConfigureAwait(false))
				{
					logger.LogInformation("Мемы для {username} успешно выданы в кол-ве {value} шт.", supporter.SupporterName, value);
					OnMemesReceived?.Invoke($"Мемы для {supporter.SupporterName} успешно выданы в кол-ве {value} шт.");
				}
				else
				{
					logger.LogError("Мемы для {username} не выданы", supporter.SupporterName);
					OnMemesNotReceived?.Invoke($"Мемы для {supporter.SupporterName} не выданы");
				}

				await Task.Delay(500, cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task<Current> GetMemeAlertsId(HttpClient memeAlertsClient, CancellationToken cancellationToken = default)
		{
			using var request = new HttpRequestMessage(HttpMethod.Get, "https://memealerts.com/api/user/current");

			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return await JsonSerializer.DeserializeAsync(responseMessage.Content.ReadAsStream(cancellationToken), SerializationModeOptionsContext.Default.Current, cancellationToken).ConfigureAwait(false);
		}

		private async Task<List<Supporter>> GetDataAsync(HttpClient memeAlertsClient, CancellationToken cancellationToken = default)
		{
			logger.LogInformation("Обновление саппортёров...");
			OnSupporterLoading?.Invoke("Обновление саппортёров...");

			var supporters = new List<Supporter>();
			for (int limit = 100, total = 100, skip = 0; limit > 0 && limit + skip <= total; skip += limit, limit = total - skip)
			{
				using var request = new HttpRequestMessage(HttpMethod.Post, "https://memealerts.com/api/supporters");
				request.Content = new StringContent($"{{\"limit\":{limit},\"skip\":{skip},\"query\":\"\",\"filters\":[0]}}", new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

				using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				responseMessage.EnsureSuccessStatusCode();
				var response = await JsonSerializer.DeserializeAsync(responseMessage.Content.ReadAsStream(cancellationToken), SerializationModeOptionsContext.Default.Supporters, cancellationToken).ConfigureAwait(false);
				supporters.AddRange(response.Data);
				total = response.Total;
			}

			logger.LogInformation("Загружено {count} саппортёров", supporters.Count);
			OnSupporterLoaded?.Invoke($"Загружено {supporters.Count} саппортёров");

			return supporters;
		}

		private async Task<bool> AddMemesAsync(HttpClient memeAlertsClient, string userId, string streamerId, int value, CancellationToken cancellationToken = default)
		{
			using var request = new HttpRequestMessage(HttpMethod.Post, "https://memealerts.com/api/user/give-bonus");
			request.Content = new StringContent($"{{\"userId\":\"{userId}\",\"streamerId\":\"{streamerId}\",\"value\":{value}}}", new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return (await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
		}
	}
}