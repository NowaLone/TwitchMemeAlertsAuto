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
	public class MemeAlertsService : IMemeAlertsService
	{
		private readonly ISettingsService settingsService;
		private readonly ILogger logger;

		private string token;
		private string streamerId;

		public MemeAlertsService(string token, ILogger<MemeAlertsService> logger)
		{
			this.token = token;
			this.logger = logger;
		}

		public MemeAlertsService(ISettingsService settingsService, ILogger<MemeAlertsService> logger) : this(settingsService.GetMemeAlertsTokenAsync().GetAwaiter().GetResult(), logger)
		{
			this.settingsService = settingsService;
		}

		public async Task<bool> CheckToken(string token, CancellationToken cancellationToken = default)
		{
			try
			{
				using var memeAlertsClient = GetHttpClient(token);
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

		public async Task<Current> GetCurrent(CancellationToken cancellationToken = default)
		{
			using var memeAlertsClient = GetHttpClient();
			using var request = new HttpRequestMessage(HttpMethod.Get, "https://memealerts.com/api/user/current");
			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return await JsonSerializer.DeserializeAsync(responseMessage.Content.ReadAsStream(cancellationToken), SerializationModeOptionsContext.Default.Current, cancellationToken).ConfigureAwait(false);
		}

		public async Task<List<Supporter>> GetSupportersAsync(CancellationToken cancellationToken = default)
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

		public async Task<bool> GiveBonusAsync(Supporter supporter, int value, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrWhiteSpace(streamerId))
			{
				streamerId = (await GetCurrent(cancellationToken).ConfigureAwait(false)).Id;
			}

			using var memeAlertsClient = GetHttpClient();
			using var request = new HttpRequestMessage(HttpMethod.Post, "https://memealerts.com/api/user/give-bonus");
			request.Content = new StringContent($"{{\"userId\":\"{supporter.SupporterId}\",\"streamerId\":\"{streamerId}\",\"value\":{value}}}", new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return (await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
		}

		private HttpClient GetHttpClient(string token)
		{
			if (string.IsNullOrWhiteSpace(token) && settingsService != null)
			{
				this.token = settingsService.GetMemeAlertsTokenAsync().GetAwaiter().GetResult();
				token = this.token;
			}

			ArgumentException.ThrowIfNullOrWhiteSpace(token);

			var memeAlertsClient = new HttpClient();
			memeAlertsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			memeAlertsClient.Timeout = TimeSpan.FromSeconds(10);
			return memeAlertsClient;
		}

		private HttpClient GetHttpClient()
		{			
			return GetHttpClient(token);
		}
	}
}