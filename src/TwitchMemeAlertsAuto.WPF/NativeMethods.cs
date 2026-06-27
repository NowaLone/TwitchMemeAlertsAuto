using System;
using System.Runtime.InteropServices;

namespace TwitchMemeAlertsAuto.WPF
{
	internal static class NativeMethods
	{
		// Custom unique message ID registered across the OS session
		public const string ShowWindowMessageName = "WM_SHOW_MY_UNIQUE_WPF_APP_WINDOW";

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern uint RegisterWindowMessage(string lpString);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		// Constants used to broadcast the message to all top-level windows
		public static readonly IntPtr HWND_BROADCAST = (IntPtr)0xffff;

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		public const int SW_RESTORE = 9;
		public const int SW_SHOW = 5;
	}
}