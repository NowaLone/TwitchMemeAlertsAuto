using System;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public interface IDispatcherService
	{
		Task<string> CallMemeAlertsAsync();
		void CallWithDispatcher(Action action);
		TResult CallWithDispatcher<TResult>(Func<TResult> action);
		System.Threading.Tasks.Task<TResult> CallWithDispatcherAsync<TResult>(Func<TResult> action);
		void ShowMessage(string message);
		void Shutdown();
	}
}