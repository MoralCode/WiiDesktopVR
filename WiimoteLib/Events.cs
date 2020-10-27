//////////////////////////////////////////////////////////////////////////////////
//	Events.cs
//	Managed Wiimote Library
//	Written by Brian Peek (http://www.brianpeek.com/)
//  for MSDN's Coding4Fun (http://msdn.microsoft.com/coding4fun/)
//  Visit http://msdn.microsoft.com/coding4fun/hardware/article.aspx?articleid=1879033
//  for more information
//////////////////////////////////////////////////////////////////////////////////

using System;

namespace WiimoteLib
{
	/// <summary>
	/// Event to handle a state change on the Wiimote
	/// </summary>
	/// <param name="sender">Object sending the event</param>
	/// <param name="args">Current Wiimote state</param>
	public delegate void WiimoteChangedEventHandler(object sender, WiimoteChangedEventArgs args);

	/// <summary>
	/// Event to handle insertion/removal of an extension (Nunchuk/Classic Controller)
	/// </summary>
	/// <param name="sender">Object sending the event</param>
	/// <param name="args">Current extension status</param>
	public delegate void WiimoteExtensionChanged(object sender, WiimoteExtensionChangedEventArgs args);

	/// <summary>
	/// Argument sent through the WiimoteExtensionChangedEvent
	/// </summary>
	public class WiimoteExtensionChangedEventArgs: EventArgs
	{
		public ExtensionType ExtensionType;
		public bool Inserted;

		public WiimoteExtensionChangedEventArgs(ExtensionType type, bool inserted)
		{
			ExtensionType = type;
			Inserted = inserted;
		}
	}

	/// <summary>
	/// Argument sent through the WiimoteChangedEvent
	/// </summary>
	public class WiimoteChangedEventArgs: EventArgs
	{
		public WiimoteState WiimoteState;

		public WiimoteChangedEventArgs(WiimoteState ws)
		{
			WiimoteState = ws;
		}
	}
}
