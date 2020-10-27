//////////////////////////////////////////////////////////////////////////////////
//	DataTypes.cs
//	Managed Wiimote Library
//	Written by Brian Peek (http://www.brianpeek.com/)
//  for MSDN's Coding4Fun (http://msdn.microsoft.com/coding4fun/)
//  Visit http://msdn.microsoft.com/coding4fun/hardware/article.aspx?articleid=1879033
//  for more information
//////////////////////////////////////////////////////////////////////////////////

using System;

// if we're building the MSRS version, we need to bring in the MSRS Attributes
// if we're not doing the MSRS build then define some fake attribute classes for DataMember/DataContract
#if MSRS
	using Microsoft.Dss.Core.Attributes;
#else
	sealed class DataContract : Attribute
	{
	}

	sealed class DataMember: Attribute
	{
	}
#endif

namespace WiimoteLib
{
	/// <summary>
	/// Current overall state of the Wiimote and all attachments
	/// </summary>
	[DataContract()]
	public class WiimoteState
	{
		[DataMember]
		public CalibrationInfo CalibrationInfo = new CalibrationInfo();
		[DataMember]
		public ButtonState ButtonState = new ButtonState();
		[DataMember]
		public AccelState AccelState = new AccelState();
		[DataMember]
		public IRState IRState = new IRState();
		[DataMember]
		public byte Battery;
		[DataMember]
		public bool Rumble;
		[DataMember]
		public bool Extension;
		[DataMember]
		public ExtensionType ExtensionType; 
		[DataMember]
		public NunchukState NunchukState = new NunchukState();
		[DataMember]
		public ClassicControllerState ClassicControllerState = new ClassicControllerState();
		[DataMember]
		public LEDs LEDs;
	}

    [DataContract]
    public struct LEDs
    {
        [DataMember]
        public bool LED1, LED2, LED3, LED4;
    }

    [DataContract]
    public struct RumbleRequest
    {
        [DataMember]
        public bool Rumble;
    }


	/// <summary>
	/// Calibration information stored on the Nunchuk
	/// </summary>
	[DataContract()]
	public struct NunchukCalibrationInfo
	{
		[DataMember]
		public byte X0, Y0, Z0;
		[DataMember]
		public byte XG, YG, ZG;
		[DataMember]
		public byte MinX, MidX, MaxX;
		[DataMember]
		public byte MinY, MidY, MaxY;
	}

	/// <summary>
	/// Calibration information stored on the Classic Controller
	/// </summary>
	[DataContract()]	
	public struct ClassicControllerCalibrationInfo
	{
		[DataMember]
		public byte MinXL, MidXL, MaxXL;
		[DataMember]
		public byte MinYL, MidYL, MaxYL;
		[DataMember]
		public byte MinXR, MidXR, MaxXR;
		[DataMember]
		public byte MinYR, MidYR, MaxYR;
		[DataMember]
		public byte MinTriggerL, MaxTriggerL;
		[DataMember]
		public byte MinTriggerR, MaxTriggerR;
	}

	/// <summary>
	/// Current state of the Nunchuk extension
	/// </summary>
	[DataContract()]	
	public struct NunchukState
	{
		[DataMember]
		public NunchukCalibrationInfo CalibrationInfo;
		[DataMember]
		public AccelState AccelState;
		[DataMember]
		public byte RawX, RawY;
		[DataMember]
		public float X, Y;
		[DataMember]
		public bool C;
		[DataMember]
		public bool Z;
	}

	/// <summary>
	/// Curernt button state of the Classic Controller
	/// </summary>
	[DataContract()]
	public struct ClassicControllerButtonState
	{
		[DataMember]
		public bool A;
		[DataMember]
		public bool B;
		[DataMember]
		public bool Plus;
		[DataMember]
		public bool Home;
		[DataMember]
		public bool Minus;
		[DataMember]
		public bool Up;
		[DataMember]
		public bool Down;
		[DataMember]
		public bool Left;
		[DataMember]
		public bool Right;
		[DataMember]
		public bool X, Y;
		[DataMember]
		public bool ZL, ZR;
		[DataMember]
		public bool TriggerL, TriggerR;
	}

	/// <summary>
	/// Current state of the Classic Controller
	/// </summary>
	[DataContract()]
	public struct ClassicControllerState
	{
		[DataMember]
		public ClassicControllerCalibrationInfo CalibrationInfo;
		[DataMember]
		public ClassicControllerButtonState ButtonState;
		[DataMember]
		public byte RawXL, RawYL;
		[DataMember]
		public byte RawXR, RawYR;
		[DataMember]
		public float XL, YL, XR, YR;
		[DataMember]
		public byte RawTriggerL, RawTriggerR;
		[DataMember]
		public float TriggerL, TriggerR;
	}

	/// <summary>
	/// Current state of the IR camera
	/// </summary>
	[DataContract()]
	public struct IRState
	{
		[DataMember]
		public IRMode Mode;
		[DataMember]
        public int RawX1, RawX2, RawX3, RawX4;
		[DataMember]
        public int RawY1, RawY2, RawY3, RawY4;
		[DataMember]
        public int Size1, Size2, Size3, Size4;
		[DataMember]
        public bool Found1, Found2, Found3, Found4;
		[DataMember]
        public float X1, X2, X3, X4;
		[DataMember]
        public float Y1, Y2, Y3, Y4; 
	}

	/// <summary>
	/// Current state of the accelerometers
	/// </summary>
	[DataContract()]
	public struct AccelState
	{
		[DataMember]
		public byte RawX, RawY, RawZ;
		[DataMember]
		public float X, Y, Z;
	}

	/// <summary>
	/// Calibration information stored on the Wiimote
	/// </summary>
	[DataContract()]
	public struct CalibrationInfo
	{
		[DataMember]
		public byte X0, Y0, Z0;
		[DataMember]
		public byte XG, YG, ZG;
	}

	/// <summary>
	/// Current button state
	/// </summary>
	[DataContract()]
	public struct ButtonState
	{
		[DataMember]
		public bool A;
		[DataMember]
		public bool B;
		[DataMember]
		public bool Plus;
		[DataMember]
		public bool Home;
		[DataMember]
		public bool Minus;
		[DataMember]
		public bool One;
		[DataMember]
		public bool Two;
		[DataMember]
		public bool Up;
		[DataMember]
		public bool Down;
		[DataMember]
		public bool Left;
		[DataMember]
		public bool Right;
	}

	/// <summary>
	/// The extension plugged into the Wiimote
	/// </summary>
	[DataContract()]
	public enum ExtensionType : byte
	{
		None				= 0x00,
		Nunchuk				= 0xfe,
		ClassicController	= 0xfd,
	};

	/// <summary>
	/// The mode of data reported for the IR sensor
	/// </summary>
	[DataContract()]
	public enum IRMode : byte
	{
		Off			= 0x00,
		Basic		= 0x01,	// 10 bytes
		Extended	= 0x03,	// 12 bytes
		Full		= 0x05,	// 16 bytes * 2 (format unknown)
	};
}
