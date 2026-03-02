using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core.Services;

namespace TwitchMemeAlertsAuto.Core.Tests.Services;

[TestClass]
public class TwitchOAuthServiceTests
{
	private readonly IFixture fixture;

	public TwitchOAuthServiceTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	private class StubMessageHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, HttpResponseMessage> responder;

		public StubMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
		{
			this.responder = responder;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			=> Task.FromResult(responder(request));
	}

	private TwitchOAuthService CreateService(
		out Mock<ILogger<TwitchOAuthService>> loggerMock,
		out Mock<ISettingsService> settingsMock,
		Func<HttpRequestMessage, HttpResponseMessage> responder)
	{
		settingsMock = new Mock<ISettingsService>();
		loggerMock = new Mock<ILogger<TwitchOAuthService>>();

		var service = new TwitchOAuthService(settingsMock.Object, loggerMock.Object);

		// Replace internal HttpClient with one using our stub handler
		var handler = new StubMessageHandler(responder);
		var httpClient = new HttpClient(handler);

		var httpClientField = typeof(TwitchOAuthService)
			.GetField("httpClient", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

		httpClientField!.SetValue(service, httpClient);

		return service;
	}

	#region AuthenticateAsync

	[TestMethod]
	[TestCategory(nameof(TwitchOAuthService))]
	[TestCategory(nameof(TwitchOAuthService.AuthenticateAsync))]
	public async Task AuthenticateAsync_UsesExistingValidToken_WhenValidationSucceeds()
	{
		// Arrange
		var token = fixture.Create<string>();
		var login = fixture.Create<string>();

		var service = CreateService(
			out var loggerMock,
			out var settingsMock,
			request =>
			{
				// Respond only to validate endpoint
				if (request.RequestUri!.AbsoluteUri.Contains("/oauth2/validate", StringComparison.OrdinalIgnoreCase))
				{
					var json = $$"""{"client_id":"id","login":"{{login}}","user_id":"123","expires_in":3600,"scopes":[]}""";
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(json)
					};
				}

				return new HttpResponseMessage(HttpStatusCode.NotFound);
			});

		settingsMock
			.Setup(s => s.GetTwitchOAuthTokenAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(token);

		settingsMock
			.Setup(s => s.GetTwitchExpiresInAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(DateTimeOffset.UtcNow.AddMinutes(10));

		// Act
		var result = await service.AuthenticateAsync(CancellationToken.None);

		// Assert
		Assert.AreEqual(token, result);
		loggerMock.Verify(
			l => l.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Using existing valid token")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()),
			Times.Once);
	}

	[TestMethod]
	[TestCategory(nameof(TwitchOAuthService))]
	[TestCategory(nameof(TwitchOAuthService.AuthenticateAsync))]
	public async Task AuthenticateAsync_Throws_WhenDeviceFlowCannotObtainToken()
	{
		// Arrange: no stored tokens so service will go through device flow,
		// but our handler will simulate an immediate error during token polling.
		var service = CreateService(
			out _,
			out var settingsMock,
			request =>
			{
				if (request.RequestUri!.AbsoluteUri.Contains("/oauth2/device", StringComparison.OrdinalIgnoreCase))
				{
					// Minimal valid device response
					var json = """{"device_code":"code","user_code":"user","verification_uri":"http://localhost","expires_in":1,"interval":1}""";
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(json)
					};
				}

				if (request.RequestUri!.AbsoluteUri.Contains("/oauth2/token", StringComparison.OrdinalIgnoreCase))
				{
					// Return an error that will cause AuthenticateAsync to throw
					var json = """{"status":400,"message":"access_denied"}""";
					return new HttpResponseMessage(HttpStatusCode.BadRequest)
					{
						Content = new StringContent(json)
					};
				}

				return new HttpResponseMessage(HttpStatusCode.NotFound);
			});

		settingsMock
			.Setup(s => s.GetTwitchOAuthTokenAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(string.Empty);

		settingsMock
			.Setup(s => s.GetTwitchRefreshTokenAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(string.Empty);

		// Act & Assert
		try
		{
			await service.AuthenticateAsync(CancellationToken.None);
			Assert.Fail("Expected authentication to throw when device flow cannot obtain token.");
		}
		catch (Exception)
		{
			// Expected
		}
	}

	#endregion
}

