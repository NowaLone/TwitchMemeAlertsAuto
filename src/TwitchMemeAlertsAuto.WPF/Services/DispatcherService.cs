using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TwitchMemeAlertsAuto.Core;

namespace TwitchMemeAlertsAuto.WPF.Services
{
	public class DispatcherService : IDispatcherService
	{
		public void CallWithDispatcher(Action action)
		{
			if (action == null)
			{
				return;
			}

			Application.Current.Dispatcher.Invoke(action, DispatcherPriority.Normal);
		}

		public TResult CallWithDispatcher<TResult>(Func<TResult> action)
		{
			if (action == null)
			{
				return default;
			}

			return Application.Current.Dispatcher.Invoke(action, DispatcherPriority.Normal);
		}

		public Task<TResult> CallWithDispatcherAsync<TResult>(Func<TResult> action)
		{
			if (action == null)
			{
				return default;
			}

			return Application.Current.Dispatcher.InvokeAsync(action, DispatcherPriority.Normal).Task;
		}

		public async Task<string> CallMemeAlertsAsync()
		{
			var webView2 = new WebView2();
			var window = new Window
			{
				Content = webView2,
				Owner = Application.Current.MainWindow,
			};

			window.Show();
			var webView2Environment = await CoreWebView2Environment.CreateAsync();

			await webView2.EnsureCoreWebView2Async(webView2Environment);
			webView2.Source = new Uri("https://memealerts.com/");
			var result = await WaitForCookieAsync(window, webView2);
			window.Close();
			webView2.Dispose();
			return result;
		}

		private async Task<string> WaitForCookieAsync(Window window, WebView2 webView2)
		{
			string stringResult = null;
			var value = 0;
			var isClosed = false;
			EventHandler closedEvent = (s, e) => isClosed = true;
			window.Closed += closedEvent;

			do
			{
				await Task.Delay(300).ConfigureAwait(false);
				await CallWithDispatcher(async () =>
					{
						var cook1 = await webView2.CoreWebView2.ExecuteScriptWithResultAsync("localStorage.getItem('accessToken')");
						if (cook1.Succeeded)
						{
							cook1.TryGetResultAsString(out stringResult, out value);
						}
					}).ConfigureAwait(false);
			}
			while (value != 1 && !isClosed);
			window.Closed -= closedEvent;


			return stringResult;
		}

		public void Shutdown()
		{
			Application.Current.Shutdown();
		}

		public void ShowMessage(string message)
		{
			MessageBox.Show(message);
		}
	}
}