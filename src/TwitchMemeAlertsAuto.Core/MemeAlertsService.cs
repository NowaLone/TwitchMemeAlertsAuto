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
		private readonly IHttpClientFactory httpClientFactory;
		private readonly ILogger logger;

		private string streamerId;

		public MemeAlertsService(IHttpClientFactory httpClientFactory, ILogger<MemeAlertsService> logger)
		{
			this.httpClientFactory = httpClientFactory;
			this.logger = logger;
		}

		public async Task<bool> CheckToken(string token, CancellationToken cancellationToken = default)
		{
			try
			{
				using var memeAlertsClient = httpClientFactory.CreateClient(nameof(MemeAlertsService));
				memeAlertsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

				using var request = new HttpRequestMessage(HttpMethod.Get, "api/user/current");
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
			using var memeAlertsClient = httpClientFactory.CreateClient(nameof(MemeAlertsService));
			using var request = new HttpRequestMessage(HttpMethod.Get, "api/user/current");
			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return await JsonSerializer.DeserializeAsync(responseMessage.Content.ReadAsStream(cancellationToken), SerializationModeOptionsContext.Default.Current, cancellationToken).ConfigureAwait(false);
		}

		public async Task<List<Supporter>> GetSupportersAsync(CancellationToken cancellationToken = default)
		{
			logger.LogInformation(EventIds.Loading, "Обновление саппортёров...");

			using var memeAlertsClient = httpClientFactory.CreateClient(nameof(MemeAlertsService));

			var supporters = new List<Supporter>();
			for (int limit = 100, total = 100, skip = 0; limit > 0 && limit + skip <= total; skip += limit, limit = total - skip)
			{
				using var request = new HttpRequestMessage(HttpMethod.Post, "api/supporters");
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

			using var memeAlertsClient = httpClientFactory.CreateClient(nameof(MemeAlertsService));
			using var request = new HttpRequestMessage(HttpMethod.Post, "api/user/give-bonus");
			request.Content = new StringContent($"{{\"userId\":\"{supporter.SupporterId}\",\"streamerId\":\"{streamerId}\",\"value\":{value}}}", new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

			using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			responseMessage.EnsureSuccessStatusCode();
			return (await responseMessage.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false)).Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase);
		}

		public async Task<List<Event>> GetEventsAsync(CancellationToken cancellationToken = default)
		{
			using var memeAlertsClient = httpClientFactory.CreateClient(nameof(MemeAlertsService));

			var supporters = new List<Event>();
			for (int limit = 100, total = 100, skip = 0; limit > 0 && limit + skip <= total; skip += limit, limit = total - skip)
			{
				using var request = new HttpRequestMessage(HttpMethod.Post, "api/event/period");
				request.Content = new StringContent($"{{\"period\":30,\"skip\":{skip},\"limit\":{limit},\"filters\":[2,3,4],\"date\":null}}", new MediaTypeHeaderValue(MediaTypeNames.Application.Json));

				using var responseMessage = await memeAlertsClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
				responseMessage.EnsureSuccessStatusCode();
				var response = await JsonSerializer.DeserializeAsync(responseMessage.Content.ReadAsStream(cancellationToken), SerializationModeOptionsContext.Default.Events, cancellationToken).ConfigureAwait(false);
				supporters.AddRange(response.Data);
				total = response.Total;
			}

			return supporters;
		}
	}
}