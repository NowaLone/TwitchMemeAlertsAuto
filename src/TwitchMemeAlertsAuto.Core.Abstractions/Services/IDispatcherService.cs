using System;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core.Services
{
	public interface IDispatcherService
	{
		Task<string> CallMemeAlertsAsync();
		void CallWithDispatcher(Action action);
		TResult CallWithDispatcher<TResult>(Func<TResult> action);
		System.Threading.Tasks.Task<TResult> CallWithDispatcherAsync<TResult>(Func<TResult> action);
		void CheckForUpdates();
		void ShowMessage(string message);
		void Shutdown();
	}
}