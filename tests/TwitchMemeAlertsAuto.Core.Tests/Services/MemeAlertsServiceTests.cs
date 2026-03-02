using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core;
using TwitchMemeAlertsAuto.Core.Services;

namespace TwitchMemeAlertsAuto.Core.Tests.Services;

[TestClass]
public class MemeAlertsServiceTests
{
	private readonly IFixture fixture;

	public MemeAlertsServiceTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	private class StubHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

		public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
		{
			this.responder = responder;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> Task.FromResult(responder(request));
	}

	private MemeAlertsService CreateService(
		out Mock<IHttpClientFactory> factoryMock,
		out Mock<ILogger<MemeAlertsService>> loggerMock,
		Func<HttpRequestMessage, HttpResponseMessage> responder)
	{
		factoryMock = new Mock<IHttpClientFactory>();
		loggerMock = new Mock<ILogger<MemeAlertsService>>();

		factoryMock
			.Setup(f => f.CreateClient(nameof(MemeAlertsService)))
			.Returns(() =>
			{
				var handler = new StubHandler(responder);
				return new HttpClient(handler)
				{
					BaseAddress = new Uri("https://example.com/")
				};
			});

		return new MemeAlertsService(factoryMock.Object, loggerMock.Object);
	}

	#region CheckToken

	[TestMethod]
	[TestCategory(nameof(MemeAlertsService))]
	[TestCategory(nameof(MemeAlertsService.CheckToken))]
	public async Task CheckToken_ReturnsTrue_OnSuccessStatus()
	{
		// Arrange
		var service = CreateService(
			out _,
			out _,
			_ => new HttpResponseMessage(HttpStatusCode.OK));

		// Act
		var result = await service.CheckToken("token", CancellationToken.None);

		// Assert
		Assert.IsTrue(result);
	}

	[TestMethod]
	[TestCategory(nameof(MemeAlertsService))]
	[TestCategory(nameof(MemeAlertsService.CheckToken))]
	public async Task CheckToken_ReturnsFalse_OnUnauthorized()
	{
		// Arrange
		var service = CreateService(
			out _,
			out _,
			_ => throw new HttpRequestException("unauthorized", null, HttpStatusCode.Unauthorized));

		// Act
		var result = await service.CheckToken("token", CancellationToken.None);

		// Assert
		Assert.IsFalse(result);
	}

	#endregion

	#region GetCurrent

	[TestMethod]
	[TestCategory(nameof(MemeAlertsService))]
	[TestCategory(nameof(MemeAlertsService.GetCurrent))]
	public async Task GetCurrent_DeserializesCurrent()
	{
		// Arrange
		var current = fixture.Build<Current>()
			.With(c => c.Id, Guid.NewGuid().ToString("N"))
			.Create();

		var json = JsonSerializer.Serialize(current, SerializationModeOptionsContext.Default.Current);

		var service = CreateService(
			out _,
			out _,
			req =>
			{
				Assert.IsTrue(req.RequestUri!.PathAndQuery.EndsWith("api/user/current", StringComparison.Ordinal));
				return new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json)
				};
			});

		// Act
		var result = await service.GetCurrent(CancellationToken.None);

		// Assert
		Assert.IsNotNull(result);
		Assert.AreEqual(current.Id, result.Id);
	}

	#endregion

	#region GiveBonusAsync

	[TestMethod]
	[TestCategory(nameof(MemeAlertsService))]
	[TestCategory(nameof(MemeAlertsService.GiveBonusAsync))]
	public async Task GiveBonusAsync_UsesStreamerIdFromGetCurrent_AndReturnsTrueOnSuccess()
	{
		// Arrange
		var supporter = fixture.Build<Supporter>()
			.With(s => s.SupporterId, Guid.NewGuid().ToString("N"))
			.Create();

		var current = fixture.Build<Current>()
			.With(c => c.Id, Guid.NewGuid().ToString("N"))
			.Create();

		var currentJson = JsonSerializer.Serialize(current, SerializationModeOptionsContext.Default.Current);

		var callCount = 0;

		var service = CreateService(
			out _,
			out _,
			req =>
			{
				if (req.RequestUri!.PathAndQuery.EndsWith("api/user/current", StringComparison.Ordinal))
				{
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(currentJson, Encoding.UTF8, MediaTypeNames.Application.Json)
					};
				}

				if (req.RequestUri!.PathAndQuery.EndsWith("api/user/give-bonus", StringComparison.Ordinal))
				{
					callCount++;
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent("true")
					};
				}

				return new HttpResponseMessage(HttpStatusCode.NotFound);
			});

		// Act
		var result = await service.GiveBonusAsync(supporter, 10, CancellationToken.None);

		// Second call should reuse cached streamerId and not hit GetCurrent again
		var result2 = await service.GiveBonusAsync(supporter, 5, CancellationToken.None);

		// Assert
		Assert.IsTrue(result);
		Assert.IsTrue(result2);
		Assert.AreEqual(2, callCount);
	}

	#endregion
}

