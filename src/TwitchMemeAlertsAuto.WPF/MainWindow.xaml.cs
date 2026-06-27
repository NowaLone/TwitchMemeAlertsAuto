using System;
using System.Windows;
using System.Windows.Interop;

namespace TwitchMemeAlertsAuto.WPF
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private uint _customWindowMessage;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			// Get the unique message ID registered during App Startup
			_customWindowMessage = NativeMethods.RegisterWindowMessage(NativeMethods.ShowWindowMessageName);

			// Hook into the Win32 window message loop
			HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
			source?.AddHook(WndProc);
		}

		private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			// If the message matches our custom string registered in user32...
			if (msg == _customWindowMessage)
			{
				RestoreAndFocusWindow(hwnd);
				handled = true;
			}

			return IntPtr.Zero;
		}

		private void RestoreAndFocusWindow(IntPtr hwnd)
		{
			// Unhide from tray/background
			this.Show();

			// Native restore if minimized, otherwise native show
			if (this.WindowState == WindowState.Minimized)
			{
				NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
			}
			else
			{
				NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOW);
			}

			// Force Windows to focus the window
			NativeMethods.SetForegroundWindow(hwnd);

			// Standard WPF focus reinforcement
			this.Activate();
		}
	}
}