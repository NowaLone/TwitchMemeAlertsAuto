using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public class TwitchOAuthService : ITwitchOAuthService
	{
		private readonly ISettingsService settingsService;
		private readonly ILogger<TwitchOAuthService> logger;
		private readonly HttpClient httpClient;
		private bool disposed = false;

		// Twitch OAuth endpoints
		private const string TwitchDeviceUrl = "https://id.twitch.tv/oauth2/device";
		private const string TwitchTokenUrl = "https://id.twitch.tv/oauth2/token";
		private const string TwitchValidateUrl = "https://id.twitch.tv/oauth2/validate";

		// OAuth configuration
		private const string clientId = "mysd83coqn8u0sf40aev6nvsqqlyjy";
		private const int TokenExpiryBufferSeconds = 60; // Refresh if token expires within 60 seconds

		public TwitchOAuthService(ISettingsService settingsService, ILogger<TwitchOAuthService> logger)
		{
			this.settingsService = settingsService;
			this.logger = logger;
			this.httpClient = new HttpClient();
		}

		/// <summary>
		/// Authenticates and returns a valid access token.
		/// Loads token from settings, validates it, refreshes if needed, or starts device code flow if necessary.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Valid access token</returns>
		/// <exception cref="Exception">Thrown if authentication fails</exception>
		public async Task<string> AuthenticateAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				// Step 1: Check if we have a valid token in settings
				var accessToken = await settingsService.GetTwitchOAuthTokenAsync(cancellationToken);
				var expiryTime = await settingsService.GetTwitchExpiresInAsync(cancellationToken);

				if (!string.IsNullOrWhiteSpace(accessToken) && expiryTime > DateTimeOffset.UtcNow.AddSeconds(TokenExpiryBufferSeconds))
				{
					// Token exists and hasn't expired (with buffer), validate it
					var validation = await ValidateTokenAsync(accessToken, cancellationToken);
					if (validation != null)
					{
						logger.LogInformation("Using existing valid token for user: {Login}", validation.Login);
						return accessToken;
					}
					else
					{
						logger.LogWarning("Existing token is invalid, will attempt refresh or re-authentication");
					}
				}
				else if (!string.IsNullOrWhiteSpace(accessToken))
				{
					logger.LogInformation("Token expired or about to expire, attempting refresh");
				}

				// Step 2: Try to refresh token if refresh token exists
				var refreshToken = await settingsService.GetTwitchRefreshTokenAsync(cancellationToken);
				if (!string.IsNullOrWhiteSpace(refreshToken))
				{
					try
					{
						var refreshedToken = await RefreshTokenAsync(refreshToken, cancellationToken);
						if (!string.IsNullOrWhiteSpace(refreshedToken))
						{
							logger.LogInformation("Token refreshed successfully");
							return refreshedToken;
						}
					}
					catch (Exception ex)
					{
						logger.LogWarning(ex, "Token refresh failed, will start new authentication flow");
					}
				}

				// Step 3: No valid token or refresh failed, start device code flow
				var newToken = await StartDeviceCodeFlowAsync(cancellationToken);
				if (string.IsNullOrWhiteSpace(newToken))
				{
					throw new Exception("Authentication failed: Unable to obtain access token through device code flow");
				}

				return newToken;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during authentication");
				throw;
			}
		}

		private async Task<string> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
		{
			var requestBody = new Dictionary<string, string>
			{
				["client_id"] = clientId,
				["refresh_token"] = refreshToken,
				["grant_type"] = "refresh_token"
			};

			var content = new FormUrlEncodedContent(requestBody);
			var response = await httpClient.PostAsync(TwitchTokenUrl, content, cancellationToken);
			var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

			if (!response.IsSuccessStatusCode)
			{
				var errorResponse = JsonSerializer.Deserialize<TwitchErrorResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
				});

				logger.LogError("Token refresh failed. Status: {Status}, Message: {Message}", response.StatusCode, errorResponse?.Message);
				throw new Exception($"Token refresh failed: {errorResponse?.Message ?? "Unknown error"}");
			}

			var tokenResponse = JsonSerializer.Deserialize<TwitchTokenResponse>(responseContent, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
			});

			if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
			{
				throw new Exception("Token refresh failed: Invalid response from Twitch");
			}

			logger.LogInformation("Token refresh successful");
			return await ValidateAndSave(tokenResponse, cancellationToken);
		}

		private async Task<string> StartDeviceCodeFlowAsync(CancellationToken cancellationToken)
		{
			var scopes = new[] { "chat:read", "chat:edit", "channel:manage:redemptions" };

			// Step 1: Request device code
			var deviceResponse = await RequestDeviceCodeAsync(scopes, cancellationToken);
			if (deviceResponse == null)
			{
				throw new Exception("Failed to obtain device code from Twitch");
			}

			// Step 2: Display user code and verification URL
			try
			{
				Process.Start(new ProcessStartInfo(deviceResponse.VerificationUri) { UseShellExecute = true });
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Unable to automatically open verification URL");
			}

			DisplayUserInstructions(deviceResponse);

			// Step 3: Poll for token
			var tokenResponse = await PollForTokenAsync(deviceResponse, cancellationToken);
			if (tokenResponse == null)
			{
				throw new Exception("Device code flow failed: Unable to obtain access token");
			}

			return await ValidateAndSave(tokenResponse, cancellationToken);
		}

		private async Task<TwitchValidationResponse> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken)
		{
			try
			{
				httpClient.DefaultRequestHeaders.Clear();
				httpClient.DefaultRequestHeaders.Add("Authorization", $"OAuth {accessToken}");

				var response = await httpClient.GetAsync(TwitchValidateUrl, cancellationToken);
				var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					logger.LogWarning("Token validation failed. Status: {Status}, Response: {Response}", response.StatusCode, responseContent);
					return null;
				}

				var validationResponse = JsonSerializer.Deserialize<TwitchValidationResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
				});

				if (validationResponse != null)
				{
					logger.LogInformation("Token valid for user: {Login}, Client ID: {ClientId}, Expires in: {ExpiresIn} seconds",
						validationResponse.Login, validationResponse.ClientId, validationResponse.ExpiresIn);
				}

				return validationResponse;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error validating token");
				return null;
			}
		}

		private async Task<TwitchDeviceResponse> RequestDeviceCodeAsync(string[] scopes, CancellationToken cancellationToken)
		{
			try
			{
				var requestBody = new Dictionary<string, string>
				{
					["client_id"] = clientId,
					["scopes"] = string.Join(" ", scopes)
				};

				var content = new FormUrlEncodedContent(requestBody);
				var response = await httpClient.PostAsync(TwitchDeviceUrl, content, cancellationToken);
				var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					logger.LogError("Device code request failed. Status: {Status}, Response: {Response}", response.StatusCode, responseContent);
					return null;
				}

				var deviceResponse = JsonSerializer.Deserialize<TwitchDeviceResponse>(responseContent, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
				});

				return deviceResponse;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error requesting device code");
				throw;
			}
		}

		private void DisplayUserInstructions(TwitchDeviceResponse deviceResponse)
		{
			Console.WriteLine();
			Console.WriteLine("========================================");
			Console.WriteLine("        TWITCH AUTHENTICATION");
			Console.WriteLine("========================================");
			Console.WriteLine();
			Console.WriteLine($"1. Open your web browser and go to:");
			Console.WriteLine($"   {deviceResponse.VerificationUri}");
			Console.WriteLine();
			Console.WriteLine($"2. Enter this code when prompted:");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"   {deviceResponse.UserCode}");
			Console.ResetColor();
			Console.WriteLine();
			Console.WriteLine("3. Wait for authentication to complete...");
			Console.WriteLine("   (This window will update automatically)");
			Console.WriteLine();
			Console.WriteLine("========================================");
			Console.WriteLine();

			logger.LogInformation("User code: {UserCode}, Verification URL: {VerificationUri}", deviceResponse.UserCode, deviceResponse.VerificationUri);
		}

		private async Task<TwitchTokenResponse> PollForTokenAsync(TwitchDeviceResponse deviceResponse, CancellationToken cancellationToken)
		{
			var interval = TimeSpan.FromSeconds(deviceResponse.Interval);
			var expiresAt = DateTime.UtcNow.AddSeconds(deviceResponse.ExpiresIn);

			logger.LogInformation("Starting token polling with {Interval}s interval, expires in {ExpiresIn}s",
				deviceResponse.Interval, deviceResponse.ExpiresIn);

			while (DateTime.UtcNow < expiresAt && !cancellationToken.IsCancellationRequested)
			{
				try
				{
					var requestBody = new Dictionary<string, string>
					{
						["client_id"] = clientId,
						["device_code"] = deviceResponse.DeviceCode,
						["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
					};

					var content = new FormUrlEncodedContent(requestBody);
					var response = await httpClient.PostAsync(TwitchTokenUrl, content, cancellationToken);
					var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

					if (response.IsSuccessStatusCode)
					{
						var tokenResponse = JsonSerializer.Deserialize<TwitchTokenResponse>(responseContent, new JsonSerializerOptions
						{
							PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
						});

						logger.LogInformation("Authentication successful!");
						return tokenResponse;
					}

					// Handle error responses
					var errorResponse = JsonSerializer.Deserialize<TwitchErrorResponse>(responseContent, new JsonSerializerOptions
					{
						PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
					});

					switch (errorResponse?.Message)
					{
						case "authorization_pending":
							// User hasn't completed authorization yet, continue polling
							break;

						case "slow_down":
							// Increase polling interval
							interval = interval.Add(TimeSpan.FromSeconds(5));
							logger.LogInformation("Slowing down polling interval to {Interval}s", interval.TotalSeconds);
							break;

						case "access_denied":
							logger.LogError("Authentication was denied by user");
							throw new Exception("Authentication was denied by user");

						case "expired_token":
							logger.LogError("Authentication code expired");
							throw new Exception("Authentication code expired. Please try again");

						default:
							logger.LogError("Authentication error: {Message}", errorResponse?.Message);
							throw new Exception($"Authentication error: {errorResponse?.Message ?? "Unknown error"}");
					}
				}
				catch (Exception ex)
				{
					// Re-throw authentication errors, log and re-throw other errors
					logger.LogError(ex, "Error during token polling");
					throw;
				}

				// Wait before next poll
				await Task.Delay(interval, cancellationToken);
			}

			logger.LogError("Authentication timed out");
			throw new Exception("Authentication timed out. Please try again");
		}

		private async Task<string> ValidateAndSave(TwitchTokenResponse tokenResponse, CancellationToken cancellationToken)
		{
			if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
			{
				throw new Exception("Invalid token response from Twitch");
			}

			logger.LogInformation("Successfully obtained access token");

			// Validate token
			var twitchValidationResponse = await ValidateTokenAsync(tokenResponse.AccessToken, cancellationToken);
			if (twitchValidationResponse == null)
			{
				throw new Exception("Token validation failed after obtaining new token");
			}

			logger.LogInformation("Token validation successful");

			// Save tokens and metadata
			await settingsService.SetTwitchOAuthTokenAsync(tokenResponse.AccessToken, cancellationToken);
			if (!string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
			{
				await settingsService.SetTwitchRefreshTokenAsync(tokenResponse.RefreshToken, cancellationToken);
			}
			await settingsService.SetTwitchExpiresInAsync(DateTimeOffset.UtcNow.AddSeconds(twitchValidationResponse.ExpiresIn), cancellationToken);
			await settingsService.SetTwitchUserIdAsync(twitchValidationResponse.UserId, cancellationToken);

			return tokenResponse.AccessToken;
		}

		public void Dispose()
		{
			if (!disposed)
			{
				httpClient?.Dispose();
				disposed = true;
			}
		}
	}

	public class TwitchDeviceResponse
	{
		public string DeviceCode { get; set; } = string.Empty;
		public string UserCode { get; set; } = string.Empty;
		public string VerificationUri { get; set; } = string.Empty;
		public int ExpiresIn { get; set; }
		public int Interval { get; set; }
	}

	public class TwitchErrorResponse
	{
		public int Status { get; set; }
		public string Message { get; set; } = string.Empty;
	}

	public class TwitchTokenResponse
	{
		public string AccessToken { get; set; } = string.Empty;
		public string RefreshToken { get; set; } = string.Empty;
		public int ExpiresIn { get; set; }
		public string[] Scope { get; set; } = Array.Empty<string>();
		public string TokenType { get; set; } = string.Empty;
	}

	public class TwitchValidationResponse
	{
		public string ClientId { get; set; } = string.Empty;
		public string Login { get; set; } = string.Empty;
		public string[] Scopes { get; set; } = Array.Empty<string>();
		public string UserId { get; set; } = string.Empty;
		public int ExpiresIn { get; set; }
	}
}
