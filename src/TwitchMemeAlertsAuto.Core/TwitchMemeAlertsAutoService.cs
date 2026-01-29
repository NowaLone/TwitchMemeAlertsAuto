using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public class TwitchMemeAlertsAutoService : ITwitchMemeAlertsAutoService
	{
		private readonly string token;
		private readonly ILogger logger;

		private string streamerId = null;

		public TwitchMemeAlertsAutoService(string token, ILogger<TwitchMemeAlertsAutoService> logger)
		{
			this.token = token;
			this.logger = logger;
		}

		public TwitchMemeAlertsAutoService(ISettingsService settingsService, ILogger<TwitchMemeAlertsAutoService> logger) : this(settingsService.GetMemeAlertsTokenAsync().GetAwaiter().GetResult(), logger)
		{ }

		public async Task<bool> CheckToken(string token, CancellationToken cancellationToken = default)
		{
			try
			{
				using var memeAlertsClient = GetHttpClient();
				memeAlertsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
				using var request = new HttpRequestMessage(HttpMethod.Get, "https://memealerts.com/api/user/current");
				using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				responseMessage.EnsureSuccessStatusCode();
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

		public async Task<Current> GetMemeAlertsId(CancellationToken cancellationToken = default)
		{
			using var memeAlertsClient = GetHttpClient();
			using var request = new HttpRequestMessage(HttpMethod.Get, "https://memealerts.com/api/user/current");
			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return await JsonSerializer.DeserializeAsync(responseMessage.Content.ReadAsStream(cancellationToken), SerializationModeOptionsContext.Default.Current, cancellationToken).ConfigureAwait(false);
		}

		public async Task<List<Supporter>> GetDataAsync(CancellationToken cancellationToken = default)
		{
			logger.LogInformation(EventIds.Loading, "Обновление саппортёров...");

			using var memeAlertsClient = GetHttpClient();

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

			logger.LogInformation(EventIds.Loaded, "Загружено {count} саппортёров", supporters.Count);

			return supporters;
		}

		public async Task<bool> AddMemesAsync(Supporter supporter, int value, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(streamerId))
			{
				streamerId = (await GetMemeAlertsId(cancellationToken).ConfigureAwait(false)).Id;
			}

			using var memeAlertsClient = GetHttpClient();
			using var request = new HttpRequestMessage(HttpMethod.Post, "https://memealerts.com/api/user/give-bonus");
			request.Content = new StringContent($"{{\"userId\":\"{supporter.SupporterId}\",\"streamerId\":\"{streamerId}\",\"value\":{value}}}", new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return (await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
		}

		private HttpClient GetHttpClient()
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(token);

			var memeAlertsClient = new HttpClient();
			memeAlertsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			memeAlertsClient.Timeout = TimeSpan.FromSeconds(10);
			return memeAlertsClient;
		}
	}
}