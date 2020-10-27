//////////////////////////////////////////////////////////////////////////////////
//	Wiimote.cs
//	Managed Wiimote Library
//	Written by Brian Peek (http://www.brianpeek.com/)
//  for MSDN's Coding4Fun (http://msdn.microsoft.com/coding4fun/)
//  Visit http://msdn.microsoft.com/coding4fun/hardware/article.aspx?articleid=1879033
//  for more information
//////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32.SafeHandles;
using System.Threading;
using System.Collections;
//using System.Data;

namespace WiimoteLib
{
	/// <summary>
	/// Implementation of Wiimote
	/// </summary>
	public class Wiimote : IDisposable
	{
		public event WiimoteChangedEventHandler OnWiimoteChanged;
		public event WiimoteExtensionChanged OnWiimoteExtensionChanged;

		// VID = Nintendo, PID = Wiimote
		private const int VID = 0x057e;
		private const int PID = 0x0306;

        //device index for handling multiple remotes
        static private ArrayList connectedRemoteIDs;
        private int remoteID = -1;

		// sure, we could find this out the hard way using HID, but trust me, it's 22
		private const int REPORT_LENGTH = 22;
        private int readBytes = 0;

		// Wiimote input commands
		public enum InputReport : byte
		{
			Status				= 0x20,
			ReadData			= 0x21,
			Buttons				= 0x30,
			ButtonsAccel		= 0x31,
			IRAccel				= 0x33,
			ExtensionAccel		= 0x35,
			IRExtensionAccel	= 0x37,
		};

		// Wiimote output commands
		public enum OutputReport : byte
		{
			None			= 0x00,
			LEDs			= 0x11,
			Type			= 0x12,
			IR				= 0x13,
			Status			= 0x15,
			WriteMemory		= 0x16,
			ReadMemory		= 0x17,
			IR2				= 0x1a,
		};

		// Wiimote registers
		private const int REGISTER_IR				= 0x04b00030;
		private const int REGISTER_IR_SENSITIVITY_1	= 0x04b00000;
		private const int REGISTER_IR_SENSITIVITY_2	= 0x04b0001a;
		private const int REGISTER_IR_MODE			= 0x04b00033;

		private const int REGISTER_EXTENSION_INIT			= 0x04a40040;
		private const int REGISTER_EXTENSION_TYPE			= 0x04a400fe;
		private const int REGISTER_EXTENSION_CALIBRATION	= 0x04a40020;

		// read/write handle to the device
		private SafeFileHandle mHandle;

		// a pretty .NET stream to read/write from/to
		private FileStream mStream;

		// report buffer
		private byte[] mBuff = new byte[REPORT_LENGTH];

        //preparsed data buffer
        private byte[] preparsed = new byte[REPORT_LENGTH];

		// read data buffer
		private byte[] mReadBuff;

		// current state of controller
		private WiimoteState mWiimoteState = new WiimoteState();

		// event for read data processing
		private AutoResetEvent mReadDone = new AutoResetEvent(false);

		// current reporting type
		private InputReport mReportType;

		// use a different method to write reports
		private bool mAltWriteMethod;

        //network server
        NetworkServer server;
        

		public Wiimote()
		{
            //initialize the static index space if new
            if (connectedRemoteIDs == null)
                connectedRemoteIDs = new ArrayList();
        }

        [DllImport("Kernel32.dll")]
        public static extern bool Beep(UInt32 frequency, UInt32 duration);

        public void StartServer(int port)
        {
            server = new NetworkServer();
            server.startListening(port);
            server.OnDataRecievedEventHandlers += new OnDataRecievedEventHandler(srv_OnDataRecieved); 
        }

        void srv_OnDataRecieved(object sender, DataRecievedEventArgs args)
        {
            //network data should be ready to go the Wiimote
            char[] data = args.data;
            for (int i = 0; i < data.Length; i++)
            {
                mBuff[readBytes] = (byte)data[i];
                readBytes++;
                if (readBytes == REPORT_LENGTH)
                {
                    if(mBuff[0]==0x15)
                        Beep(500, 50);
                    WriteReport();
                    readBytes = 0;
                }
            }
        }


		public void Connect()
		{
			bool found = false;
			Guid guid;
			uint index = 0;
            // get the GUID of the HID class
			HIDImports.HidD_GetHidGuid(out guid);

			// get a handle to all devices that are part of the HID class
			// Fun fact:  DIGCF_PRESENT worked on my machine just fine.  I reinstalled Vista, and now it no longer finds the Wiimote with that parameter enabled...
			IntPtr hDevInfo = HIDImports.SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, HIDImports.DIGCF_DEVICEINTERFACE);// | HIDImports.DIGCF_PRESENT);

			// create a new interface data struct and initialize its size
			HIDImports.SP_DEVICE_INTERFACE_DATA diData = new HIDImports.SP_DEVICE_INTERFACE_DATA();
			diData.cbSize = Marshal.SizeOf(diData);

			// get a device interface to a single device (enumerate all devices)
			while(HIDImports.SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref guid, index, ref diData))
			{
				UInt32 size = 0;

				// get the buffer size for this device detail instance (returned in the size parameter)
				HIDImports.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, IntPtr.Zero, 0, out size, IntPtr.Zero);

				// create a detail struct and set its size
				HIDImports.SP_DEVICE_INTERFACE_DETAIL_DATA diDetail = new HIDImports.SP_DEVICE_INTERFACE_DETAIL_DATA();

				// yeah, yeah...well, see, on Win x86, cbSize must be 5 for some reason.  On x64, apparently 8 is what it wants.
				// someday I should figure this out.  Thanks to Paul Miller on this...
				if (IntPtr.Size == 8)
					diDetail.cbSize = 8;
				else
					diDetail.cbSize = 5;

				// actually get the detail struct
				if(HIDImports.SetupDiGetDeviceInterfaceDetail(hDevInfo, ref diData, ref diDetail, size, out size, IntPtr.Zero))
				{
					Debug.WriteLine(index + " " + diDetail.DevicePath + " " + Marshal.GetLastWin32Error());

					// open a read/write handle to our device using the DevicePath returned
					mHandle = HIDImports.CreateFile(diDetail.DevicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, HIDImports.EFileAttributes.Overlapped, IntPtr.Zero);
                    
					// create an attributes struct and initialize the size
					HIDImports.HIDD_ATTRIBUTES attrib = new HIDImports.HIDD_ATTRIBUTES();
					attrib.Size = Marshal.SizeOf(attrib);
					// get the attributes of the current device
					if(HIDImports.HidD_GetAttributes(mHandle.DangerousGetHandle(), ref attrib))
					{
						// if the vendor and product IDs match up
						if(attrib.VendorID == VID && attrib.ProductID == PID)
						{
                            if (!IsRemoteConnected((int)index))
                            {
                                Debug.WriteLine("Found it!");
                                found = true;
                                remoteID = (int)index;
                                connectedRemoteIDs.Add(remoteID);

                                // create a nice .NET FileStream wrapping the handle above
                                mStream = new FileStream(mHandle, FileAccess.ReadWrite, REPORT_LENGTH, true);

                                // start an async read operation on it
                                BeginAsyncRead();

                                // read the calibration info from the controller
                                ReadCalibration();
                                break;
                            }
						}
						else
						{
							// otherwise this isn't the controller, so close up the file handle
							mHandle.Close();
						}
					}
				}
				else
				{
					// failed to get the detail struct
					throw new Exception("SetupDiGetDeviceInterfaceDetail failed on index " + index);
				}

				// move to the next device
				index++;
            }


			// if we didn't find a Wiimote, throw an exception
			if(!found)
				throw new Exception("No available Wiimote found. Please check the bluetooth connection.");

			// clean up our list
			HIDImports.SetupDiDestroyDeviceInfoList(hDevInfo);
		}

		/// <summary>
		/// Disconnect from the controller and stop reading data from it
		/// </summary>
		public void Disconnect()
		{
            connectedRemoteIDs.Remove(remoteID);
            // close up the stream and handle
			if(mStream != null)
				mStream.Close();

			if(mHandle != null)
				mHandle.Close();
		}

		/// <summary>
		/// Start reading asynchronously from the controller
		/// </summary>
        private void BeginAsyncRead()
        {
			// if the stream is valid and ready
			if(mStream.CanRead)
			{
				// setup the read and the callback
				byte[] buff = new byte[REPORT_LENGTH];
				mStream.BeginRead(buff, 0, REPORT_LENGTH, new AsyncCallback(OnReadData), buff);
			}
        }

		/// <summary>
		/// Callback when data is ready to be processed
		/// </summary>
		/// <param name="ar">State information for the callback</param>
        private void OnReadData(IAsyncResult ar)
		{
			// grab the byte buffer
			byte[] buff = (byte[])ar.AsyncState;
			try
			{
				// end the current read
				mStream.EndRead(ar);

				// start reading again
				BeginAsyncRead();

				// parse it
				if(ParseInput(buff))
				{
					// post an event
					if(OnWiimoteChanged != null)
						OnWiimoteChanged(this, new WiimoteChangedEventArgs(mWiimoteState));
				}
			}
			catch(OperationCanceledException)
			{
				Debug.WriteLine("OperationCanceledException");
			}
		}

		/// <summary>
		/// Parse individual reports by type
		/// </summary>
		/// <param name="buff">Data buffer containing report</param>
		private bool ParseInput(byte[] buff)
		{
			InputReport type = (InputReport)buff[0];
			switch(type)
			{
				case InputReport.Buttons:
					ParseButtons(buff);
					break;
				case InputReport.ButtonsAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					break;
				case InputReport.IRAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					ParseIR(buff);
					break;
				case InputReport.IRExtensionAccel:
					ParseButtons(buff);
					ParseAccel(buff);
					ParseIR(buff);
					ParseExtension(DecryptBuffer(buff));
                    break;
				case InputReport.Status:
					ParseButtons(buff);
					mWiimoteState.Battery = buff[6];

					mWiimoteState.Extension = (buff[3] & 0x02) != 0;
					Debug.WriteLine("Extension: " + mWiimoteState.Extension);

                    if (mWiimoteState.Extension)
                    {
                        InitializeExtension();
                    }
                    else
                        mWiimoteState.ExtensionType = ExtensionType.None;

					if(OnWiimoteExtensionChanged != null)
						OnWiimoteExtensionChanged(this, new WiimoteExtensionChangedEventArgs(mWiimoteState.ExtensionType, mWiimoteState.Extension));

					break;
				case InputReport.ReadData:
					ParseButtons(buff);
					ParseReadData(buff);
					break;
				default:
					Debug.WriteLine("Unknown report type: " + type.ToString());
					return false;
			}
            
            //format pre-parsed data
            preparsed[0] = 0xff;//report type indicating preparsed
            preparsed[1] =  (byte)((mWiimoteState.ButtonState.A ? 0x01: 0) |
                            (mWiimoteState.ButtonState.B ? 0x02 : 0) |
                            (mWiimoteState.ButtonState.Down ? 0x04 : 0) |
                            (mWiimoteState.ButtonState.Up ? 0x08 : 0) |
                            (mWiimoteState.ButtonState.Left ? 0x10 : 0) |
                            (mWiimoteState.ButtonState.Right ? 0x20 : 0) |
                            (mWiimoteState.ButtonState.Plus ? 0x40: 0) |
                            (mWiimoteState.ButtonState.Minus ? 0x80: 0));
            preparsed[2] = (byte)((mWiimoteState.ButtonState.Home ? 0x01: 0) |
                            (mWiimoteState.ButtonState.One ? 0x02: 0) |
                            (mWiimoteState.ButtonState.Two ? 0x04: 0) |
                            (mWiimoteState.NunchukState.C ? 0x08: 0) |
                            (mWiimoteState.NunchukState.Z ? 0x10: 0) |
                            (mWiimoteState.Rumble ? 0x20:0)|
                            (mWiimoteState.Extension ? 0x40:0));
            preparsed[3] = mWiimoteState.AccelState.RawX;
            preparsed[4] = mWiimoteState.AccelState.RawY;
            preparsed[5] = mWiimoteState.AccelState.RawZ;
            preparsed[6] = (byte)(mWiimoteState.IRState.RawX1 >> 2);
            preparsed[7] = (byte)(mWiimoteState.IRState.RawX2 >> 2);
            preparsed[8] = (byte)(mWiimoteState.IRState.RawX3 >> 2);
            preparsed[9] = (byte)(mWiimoteState.IRState.RawX4 >> 2);
            preparsed[10] = (byte)(mWiimoteState.IRState.RawY1 >> 2);
            preparsed[11] = (byte)(mWiimoteState.IRState.RawY2 >> 2);
            preparsed[12] = (byte)(mWiimoteState.IRState.RawY3 >> 2);
            preparsed[13] = (byte)(mWiimoteState.IRState.RawY4 >> 2);
            preparsed[14] = (byte)(((mWiimoteState.IRState.RawX1 & 0x03) << 6) |
                            ((mWiimoteState.IRState.RawX2 & 0x03)<<4) |
                            ((mWiimoteState.IRState.RawX3 & 0x03)<<2) |
                            ((mWiimoteState.IRState.RawX4 & 0x03)));
            preparsed[15] = (byte)(((mWiimoteState.IRState.RawY1 & 0x03) << 6) |
                            ((mWiimoteState.IRState.RawY2 & 0x03)<<4) |
                            ((mWiimoteState.IRState.RawY3 & 0x03)<<2) |
                            ((mWiimoteState.IRState.RawY4 & 0x03)));
            preparsed[16] = mWiimoteState.NunchukState.AccelState.RawX;
            preparsed[17] = mWiimoteState.NunchukState.AccelState.RawY;
            preparsed[18] = mWiimoteState.NunchukState.AccelState.RawZ;
            preparsed[19] = mWiimoteState.NunchukState.RawX;
            preparsed[20] = mWiimoteState.NunchukState.RawY;
            preparsed[21] =  (byte)((mWiimoteState.LEDs.LED1 ? 0x01: 0) |
                                    (mWiimoteState.LEDs.LED2 ? 0x02: 0) |
                                    (mWiimoteState.LEDs.LED3 ? 0x04: 0) |
                                    (mWiimoteState.LEDs.LED4 ? 0x08: 0) |
                                    ((byte)mWiimoteState.ExtensionType << 4));

            SendDataOverNetwork(preparsed);

            //SendDataOverNetwork(buff);
			return true;
		}


        public void SendDataOverNetwork(byte[] buff)
        {
            if(server != null){
                server.Send(buff);
            }
        }

        /// <summary>
        /// Handles setting up an extension when plugged.  Currenlty only support the Nunchuk.
        /// </summary>
        private void InitializeExtension()
		{
			WriteData(REGISTER_EXTENSION_INIT, 0x00);

			byte[] buff = ReadData(REGISTER_EXTENSION_TYPE, 2);

			if(buff[0] == (byte)ExtensionType.Nunchuk && buff[1] == (byte)ExtensionType.Nunchuk)
				mWiimoteState.ExtensionType = ExtensionType.Nunchuk;
			else if(buff[0] == (byte)ExtensionType.ClassicController && buff[1] == (byte)ExtensionType.ClassicController)
				mWiimoteState.ExtensionType = ExtensionType.ClassicController;
			else
				throw new Exception("Unknown extension controller found: " + buff[0]);

			buff = DecryptBuffer(ReadData(REGISTER_EXTENSION_CALIBRATION, 16));

			switch(mWiimoteState.ExtensionType)
			{
				case ExtensionType.Nunchuk:
					mWiimoteState.NunchukState.CalibrationInfo.X0 = buff[0];
					mWiimoteState.NunchukState.CalibrationInfo.Y0 = buff[1];
					mWiimoteState.NunchukState.CalibrationInfo.Z0 = buff[2];
					mWiimoteState.NunchukState.CalibrationInfo.XG = buff[4];
					mWiimoteState.NunchukState.CalibrationInfo.YG = buff[5];
					mWiimoteState.NunchukState.CalibrationInfo.ZG = buff[6];
					mWiimoteState.NunchukState.CalibrationInfo.MaxX = buff[8];
					mWiimoteState.NunchukState.CalibrationInfo.MinX = buff[9];
					mWiimoteState.NunchukState.CalibrationInfo.MidX = buff[10];
					mWiimoteState.NunchukState.CalibrationInfo.MaxY = buff[11];
					mWiimoteState.NunchukState.CalibrationInfo.MinY = buff[12];
					mWiimoteState.NunchukState.CalibrationInfo.MidY = buff[13];
					break;
				case ExtensionType.ClassicController:
					mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXL = (byte)(buff[0] >> 2);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MinXL = (byte)(buff[1] >> 2);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MidXL = (byte)(buff[2] >> 2);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYL = (byte)(buff[3] >> 2);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MinYL = (byte)(buff[4] >> 2);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MidYL = (byte)(buff[5] >> 2);

					mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXR = (byte)(buff[6] >> 3);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MinXR = (byte)(buff[7] >> 3);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MidXR = (byte)(buff[8] >> 3);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYR = (byte)(buff[9] >> 3);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MinYR = (byte)(buff[10] >> 3);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MidYR = (byte)(buff[11] >> 3);

					// this doesn't seem right...
//					mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerL = (byte)(buff[12] >> 3);
//					mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerL = (byte)(buff[14] >> 3);
//					mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerR = (byte)(buff[13] >> 3);
//					mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerR = (byte)(buff[15] >> 3);
					mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerL = 0;
					mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerL = 31;
					mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerR = 0;
					mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerR = 31;

					break;
			}
		}

		/// <summary>
		/// Decrypts data sent from the extension to the Wiimote
		/// </summary>
		/// <param name="buff">Data buffer</param>
		/// <returns>Byte array containing decoded data</returns>
		private byte[] DecryptBuffer(byte[] buff)
		{
			for(int i = 0; i < buff.Length; i++)
				buff[i] = (byte)(((buff[i] ^ 0x17) + 0x17) & 0xff);

			return buff;
		}

		/// <summary>
		/// Parses a standard button report into the ButtonState struct
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseButtons(byte[] buff)
		{
			mWiimoteState.ButtonState.A		= (buff[2] & 0x08) != 0;
			mWiimoteState.ButtonState.B		= (buff[2] & 0x04) != 0;
			mWiimoteState.ButtonState.Minus	= (buff[2] & 0x10) != 0;
			mWiimoteState.ButtonState.Home	= (buff[2] & 0x80) != 0;
			mWiimoteState.ButtonState.Plus	= (buff[1] & 0x10) != 0;
			mWiimoteState.ButtonState.One	= (buff[2] & 0x02) != 0;
			mWiimoteState.ButtonState.Two	= (buff[2] & 0x01) != 0;
			mWiimoteState.ButtonState.Up	= (buff[1] & 0x08) != 0;
			mWiimoteState.ButtonState.Down	= (buff[1] & 0x04) != 0;
			mWiimoteState.ButtonState.Left	= (buff[1] & 0x01) != 0;
			mWiimoteState.ButtonState.Right	= (buff[1] & 0x02) != 0;
		}

		/// <summary>
		/// Parse accelerometer data
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseAccel(byte[] buff)
		{
			mWiimoteState.AccelState.RawX = buff[3];
			mWiimoteState.AccelState.RawY = buff[4];
			mWiimoteState.AccelState.RawZ = buff[5];

			mWiimoteState.AccelState.X = (float)((float)mWiimoteState.AccelState.RawX - mWiimoteState.CalibrationInfo.X0) / 
											((float)mWiimoteState.CalibrationInfo.XG - mWiimoteState.CalibrationInfo.X0);
			mWiimoteState.AccelState.Y = (float)((float)mWiimoteState.AccelState.RawY - mWiimoteState.CalibrationInfo.Y0) /
											((float)mWiimoteState.CalibrationInfo.YG - mWiimoteState.CalibrationInfo.Y0);
			mWiimoteState.AccelState.Z = (float)((float)mWiimoteState.AccelState.RawZ - mWiimoteState.CalibrationInfo.Z0) /
											((float)mWiimoteState.CalibrationInfo.ZG - mWiimoteState.CalibrationInfo.Z0);
		}

		/// <summary>
		/// Parse IR data from report
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseIR(byte[] buff)
		{
			mWiimoteState.IRState.RawX1 = buff[6]  | ((buff[8] >> 4) & 0x03) << 8;
            mWiimoteState.IRState.RawY1 = buff[7] | ((buff[8] >> 6) & 0x03) << 8;

			switch(mWiimoteState.IRState.Mode)
			{
				case IRMode.Basic:
					mWiimoteState.IRState.RawX2 = buff[9]  | ((buff[8] >> 0) & 0x03) << 8;
					mWiimoteState.IRState.RawY2 = buff[10] | ((buff[8] >> 2) & 0x03) << 8;

					mWiimoteState.IRState.Size1 = 0x00;
					mWiimoteState.IRState.Size2 = 0x00;

					mWiimoteState.IRState.Found1 = !(buff[6] == 0xff && buff[7] == 0xff);
					mWiimoteState.IRState.Found2 = !(buff[9] == 0xff && buff[10] == 0xff);
					break;
				case IRMode.Extended:
					mWiimoteState.IRState.RawX2 = buff[9]  | ((buff[11] >> 4) & 0x03) << 8;
                    mWiimoteState.IRState.RawY2 = buff[10] | ((buff[11] >> 6) & 0x03) << 8;

					mWiimoteState.IRState.Size1 = buff[8] & 0x0f;
					mWiimoteState.IRState.Size2 = buff[11] & 0x0f;

					mWiimoteState.IRState.Found1 = !(buff[6] == 0xff && buff[7] == 0xff && buff[8] == 0xff);
					mWiimoteState.IRState.Found2 = !(buff[9] == 0xff && buff[10] == 0xff && buff[11] == 0xff);

                    //a guess based on the structure of the 1st 2 dots
                    mWiimoteState.IRState.RawX3 = buff[12] | ((buff[14] >> 4) & 0x03) << 8;
                    mWiimoteState.IRState.RawY3 = buff[13] | ((buff[14] >> 6) & 0x03) << 8;
                    mWiimoteState.IRState.Size3 = buff[14] & 0x0f;
                    mWiimoteState.IRState.Found3 = !(buff[12] == 0xff && buff[13] == 0xff && buff[14] == 0xff);

                    mWiimoteState.IRState.RawX4 = buff[15] | ((buff[17] >> 4) & 0x03) << 8;
                    mWiimoteState.IRState.RawY4 = buff[16] | ((buff[17] >> 6) & 0x03) << 8;
                    mWiimoteState.IRState.Size4 = buff[17] & 0x0f;
                    mWiimoteState.IRState.Found4 = !(buff[15] == 0xff && buff[16] == 0xff && buff[17] == 0xff);

					break;
                case IRMode.Full:
                    break;
            }

			mWiimoteState.IRState.X1 = (float)(mWiimoteState.IRState.RawX1 / 1023.5f);
			mWiimoteState.IRState.X2 = (float)(mWiimoteState.IRState.RawX2 / 1023.5f);
            mWiimoteState.IRState.X3 = (float)(mWiimoteState.IRState.RawX3 / 1023.5f);
            mWiimoteState.IRState.X4 = (float)(mWiimoteState.IRState.RawX4 / 1023.5f);
            mWiimoteState.IRState.Y1 = (float)(mWiimoteState.IRState.RawY1 / 767.5f);
			mWiimoteState.IRState.Y2 = (float)(mWiimoteState.IRState.RawY2 / 767.5f);
            mWiimoteState.IRState.Y3 = (float)(mWiimoteState.IRState.RawY3 / 767.5f);
            mWiimoteState.IRState.Y4 = (float)(mWiimoteState.IRState.RawY4 / 767.5f);
        }

		/// <summary>
		/// Parse data from an extension.  Nunchuk support only.
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseExtension(byte[] buff)
		{
			switch(mWiimoteState.ExtensionType)
			{
				case ExtensionType.Nunchuk:
					mWiimoteState.NunchukState.C = (buff[21] & 0x02) == 0;
					mWiimoteState.NunchukState.Z = (buff[21] & 0x01) == 0;
					mWiimoteState.NunchukState.RawX = buff[16];
					mWiimoteState.NunchukState.RawY = buff[17];
					mWiimoteState.NunchukState.AccelState.RawX = buff[18];
					mWiimoteState.NunchukState.AccelState.RawY = buff[19];
					mWiimoteState.NunchukState.AccelState.RawZ = buff[20];

					mWiimoteState.NunchukState.AccelState.X = (float)((float)mWiimoteState.NunchukState.AccelState.RawX - mWiimoteState.NunchukState.CalibrationInfo.X0) / 
													((float)mWiimoteState.NunchukState.CalibrationInfo.XG - mWiimoteState.NunchukState.CalibrationInfo.X0);
					mWiimoteState.NunchukState.AccelState.Y = (float)((float)mWiimoteState.NunchukState.AccelState.RawY - mWiimoteState.NunchukState.CalibrationInfo.Y0) /
													((float)mWiimoteState.NunchukState.CalibrationInfo.YG - mWiimoteState.NunchukState.CalibrationInfo.Y0);
					mWiimoteState.NunchukState.AccelState.Z = (float)((float)mWiimoteState.NunchukState.AccelState.RawZ - mWiimoteState.NunchukState.CalibrationInfo.Z0) /
													((float)mWiimoteState.NunchukState.CalibrationInfo.ZG - mWiimoteState.NunchukState.CalibrationInfo.Z0);

					if(mWiimoteState.NunchukState.CalibrationInfo.MaxX != 0x00)
						mWiimoteState.NunchukState.X = (float)((float)mWiimoteState.NunchukState.RawX - mWiimoteState.NunchukState.CalibrationInfo.MidX) / 
												((float)mWiimoteState.NunchukState.CalibrationInfo.MaxX - mWiimoteState.NunchukState.CalibrationInfo.MinX);

					if(mWiimoteState.NunchukState.CalibrationInfo.MaxY != 0x00)
						mWiimoteState.NunchukState.Y = (float)((float)mWiimoteState.NunchukState.RawY - mWiimoteState.NunchukState.CalibrationInfo.MidY) / 
												((float)mWiimoteState.NunchukState.CalibrationInfo.MaxY - mWiimoteState.NunchukState.CalibrationInfo.MinY);

					break;
				case ExtensionType.ClassicController:
					mWiimoteState.ClassicControllerState.ButtonState.TriggerR	= (buff[20] & 0x02) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Plus		= (buff[20] & 0x04) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Home		= (buff[20] & 0x08) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Minus		= (buff[20] & 0x10) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.TriggerL	= (buff[20] & 0x20) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Down		= (buff[20] & 0x40) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Right		= (buff[20] & 0x80) == 0;

					mWiimoteState.ClassicControllerState.ButtonState.Up			= (buff[21] & 0x01) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Left		= (buff[21] & 0x02) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.ZR			= (buff[21] & 0x04) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.X			= (buff[21] & 0x08) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.A			= (buff[21] & 0x10) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.Y			= (buff[21] & 0x20) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.B			= (buff[21] & 0x40) == 0;
					mWiimoteState.ClassicControllerState.ButtonState.ZL			= (buff[21] & 0x80) == 0;

					mWiimoteState.ClassicControllerState.RawTriggerL = (byte)(((buff[18] & 0x60) >> 2) | (buff[19] >> 5));
					mWiimoteState.ClassicControllerState.RawTriggerR = (byte)(buff[19] & 0x1f);

					mWiimoteState.ClassicControllerState.RawXL = (byte)(buff[16] & 0x3f);
					mWiimoteState.ClassicControllerState.RawYL = (byte)(buff[17] & 0x3f);
					mWiimoteState.ClassicControllerState.RawXR = (byte)((buff[18] >> 7) | (buff[17] & 0xc0) >> 5 | (buff[16] & 0xc0) >> 3);
					mWiimoteState.ClassicControllerState.RawYR = (byte)(buff[18] & 0x1f);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXL != 0x00)
						mWiimoteState.ClassicControllerState.XL = (float)((float)mWiimoteState.ClassicControllerState.RawXL - mWiimoteState.ClassicControllerState.CalibrationInfo.MidXL) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXL - mWiimoteState.ClassicControllerState.CalibrationInfo.MinXL);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYL != 0x00)
						mWiimoteState.ClassicControllerState.YL = (float)((float)mWiimoteState.ClassicControllerState.RawYL - mWiimoteState.ClassicControllerState.CalibrationInfo.MidYL) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYL - mWiimoteState.ClassicControllerState.CalibrationInfo.MinYL);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXR != 0x00)
						mWiimoteState.ClassicControllerState.XR = (float)((float)mWiimoteState.ClassicControllerState.RawXR - mWiimoteState.ClassicControllerState.CalibrationInfo.MidXR) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxXR - mWiimoteState.ClassicControllerState.CalibrationInfo.MinXR);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYR != 0x00)
						mWiimoteState.ClassicControllerState.YR = (float)((float)mWiimoteState.ClassicControllerState.RawYR - mWiimoteState.ClassicControllerState.CalibrationInfo.MidYR) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxYR - mWiimoteState.ClassicControllerState.CalibrationInfo.MinYR);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerL != 0x00)
						mWiimoteState.ClassicControllerState.TriggerL = (mWiimoteState.ClassicControllerState.RawTriggerL) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerL - mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerL);

					if(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerR != 0x00)
						mWiimoteState.ClassicControllerState.TriggerR = (mWiimoteState.ClassicControllerState.RawTriggerR) / 
						(float)(mWiimoteState.ClassicControllerState.CalibrationInfo.MaxTriggerR - mWiimoteState.ClassicControllerState.CalibrationInfo.MinTriggerR);

					break;
			}
		}

		/// <summary>
		/// Parse data returned from a read report
		/// </summary>
		/// <param name="buff">Data buffer</param>
		private void ParseReadData(byte[] buff)
		{
			if((buff[3] & 0x08) != 0)
				throw new Exception("Error reading data from Wiimote: Bytes do not exist.");
			else if((buff[3] & 0x07) != 0)
				throw new Exception("Error reading data from Wiimote: Attempt to read from write-only registers.");
			else
			{
				int size = buff[3] >> 4;
				Array.Copy(buff, 6, mReadBuff, 0, size+1);
			}

			// set the event so the other thread will continie
			mReadDone.Set();
		}

		/// <summary>
		/// Returns whether rumble is currently enabled.
		/// </summary>
		/// <returns>Byte indicating true (0x01) or false (0x00)</returns>
		private byte GetRumbleBit()
		{
			return (byte)(mWiimoteState.Rumble ? 0x01 : 0x00);
		}

		/// <summary>
		/// Read calibration information stored on Wiimote
		/// </summary>
		private void ReadCalibration()
		{
			// this appears to change the report type to 0x31
			byte[] buff = ReadData(0x0016, 7);

			mWiimoteState.CalibrationInfo.X0 = buff[0];
			mWiimoteState.CalibrationInfo.Y0 = buff[1];
			mWiimoteState.CalibrationInfo.Z0 = buff[2];
			mWiimoteState.CalibrationInfo.XG = buff[4];
			mWiimoteState.CalibrationInfo.YG = buff[5];
			mWiimoteState.CalibrationInfo.ZG = buff[6];
		}

		/// <summary>
		/// Set Wiimote reporting mode
		/// </summary>
		/// <param name="type">Report type</param>
		/// <param name="continuous">Continuous data</param>
		public void SetReportType(InputReport type, bool continuous)
		{
			mReportType = type;

			switch(type)
			{
				case InputReport.IRAccel:
					EnableIR(IRMode.Extended);
					break;
				case InputReport.IRExtensionAccel:
                    EnableIR(IRMode.Basic);
					break;
				default:
					DisableIR();
					break;
			}

			ClearReport();
			mBuff[0] = (byte)OutputReport.Type;
			mBuff[1] = (byte)((continuous ? 0x04 : 0x00) | (byte)(mWiimoteState.Rumble ? 0x01 : 0x00));
			mBuff[2] = (byte)type;

			WriteReport();
		}

		/// <summary>
		/// Set the LEDs on the Wiimote
		/// </summary>
		/// <param name="led1">LED 1</param>
		/// <param name="led2">LED 2</param>
		/// <param name="led3">LED 3</param>
		/// <param name="led4">LED 4</param>
		public void SetLEDs(bool led1, bool led2, bool led3, bool led4)
		{
            mWiimoteState.LEDs.LED1 = led1;
            mWiimoteState.LEDs.LED2 = led2;
            mWiimoteState.LEDs.LED3 = led3;
            mWiimoteState.LEDs.LED4 = led4;					

            ClearReport();
			mBuff[0] = (byte)OutputReport.LEDs;
			mBuff[1] =	(byte)(
						(led1 ? 0x10 : 0x00) |
						(led2 ? 0x20 : 0x00) |
						(led3 ? 0x40 : 0x00) |
						(led4 ? 0x80 : 0x00) |
						GetRumbleBit());

			WriteReport();
		}

		/// <summary>
		/// Toggle rumble
		/// </summary>
		/// <param name="on">On or off</param>
		public void SetRumble(bool on)
		{
			mWiimoteState.Rumble = on;

			ClearReport();

			mBuff[0] = (byte)OutputReport.Status;
			mBuff[1] = (byte)(on ? 0x01 : 0x00);

			WriteReport();
		}

		/// <summary>
		/// Retrieve the current battery level into the state object
		/// </summary>
		public void GetBatteryLevel()
		{
			ClearReport();

			mBuff[0] = (byte)OutputReport.Status;
			mBuff[1] = GetRumbleBit();

			WriteReport();
		}

		/// <summary>
		/// Turn on the IR sensor
		/// </summary>
		/// <param name="mode">The data report mode</param>
		private void EnableIR(IRMode mode)
		{
			mWiimoteState.IRState.Mode = mode;

			ClearReport();
			mBuff[0] = (byte)OutputReport.IR;
			mBuff[1] = (byte)(0x04 | GetRumbleBit());
			WriteReport();

			ClearReport();
			mBuff[0] = (byte)OutputReport.IR2;
			mBuff[1] = (byte)(0x04 | GetRumbleBit());
			WriteReport();
			
            WriteData(REGISTER_IR, 0x08);
			//default
            //WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00, 0xc0});
			//WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] {0x40, 0x00});


            //super sensitive according to http://wiibrew.org/index.php?title=Wiimote#Sensitivity_Settings
            WriteData(REGISTER_IR_SENSITIVITY_1, 9, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00, 0x41 });
            WriteData(REGISTER_IR_SENSITIVITY_2, 2, new byte[] { 0x40, 0x00 });

            
            WriteData(REGISTER_IR_MODE, (byte)mode);
		}

		/// <summary>
		/// Disable the IR sensor
		/// </summary>
		private void DisableIR()
		{
			mWiimoteState.IRState.Mode = IRMode.Off;

			ClearReport();
			mBuff[0] = (byte)OutputReport.IR;
			mBuff[1] = GetRumbleBit();
			WriteReport();

			ClearReport();
			mBuff[0] = (byte)OutputReport.IR2;
			mBuff[1] = GetRumbleBit();
			WriteReport();
		}

		/// <summary>
		/// Initialize the report data buffer
		/// </summary>
		private void ClearReport()
		{
			Array.Clear(mBuff, 0, REPORT_LENGTH);
		}

		/// <summary>
		/// Write a report to the Wiimote
		/// </summary>
		private void WriteReport()
		{
			if(mAltWriteMethod)
				HIDImports.HidD_SetOutputReport(this.mHandle.DangerousGetHandle(), mBuff, (uint)mBuff.Length);
			else
				mStream.Write(mBuff, 0, REPORT_LENGTH);

			Thread.Sleep(100);
		}

        private bool IsRemoteConnected(int id)
        {
            //for (int i = 0; i < connectedRemoteIDs.Count; i++)
            //{
              return connectedRemoteIDs.Contains(id);
            //}
        }

        /// <summary>
        /// Read data or register from Wiimote
        /// </summary>
        /// <param name="address">Address to read</param>
        /// <param name="size">Length to read</param>
        /// <returns></returns>
        public byte[] ReadData(int address, short size)
		{
			ClearReport();

			mReadBuff = new byte[size];

			mBuff[0] = (byte)OutputReport.ReadMemory;
			mBuff[1] = (byte)(((address & 0xff000000) >> 24) | GetRumbleBit());
			mBuff[2] = (byte)((address & 0x00ff0000)  >> 16);
			mBuff[3] = (byte)((address & 0x0000ff00)  >>  8);
			mBuff[4] = (byte)(address & 0x000000ff);

			mBuff[5] = (byte)((size & 0xff00) >> 8);
			mBuff[6] = (byte)(size & 0xff);

			WriteReport();

			if(!mReadDone.WaitOne(2500, false))
				throw new Exception("Error reading data from Wiimote...is it connected?");

			return mReadBuff;
		}

		/// <summary>
		/// Write a single byte to the Wiimote
		/// </summary>
		/// <param name="address">Address to write</param>
		/// <param name="data">Byte to write</param>
		public void WriteData(int address, byte data)
		{
			WriteData(address, 1, new byte[] { data });
		}

		/// <summary>
		/// Write a byte array to a specified address
		/// </summary>
		/// <param name="address">Address to write</param>
		/// <param name="size">Length of buffer</param>
		/// <param name="buff">Data buffer</param>
		
		public void WriteData(int address, byte size, byte[] buff)
		{
			ClearReport();

			mBuff[0] = (byte)OutputReport.WriteMemory;
			mBuff[1] = (byte)(((address & 0xff000000) >> 24) | GetRumbleBit());
			mBuff[2] = (byte)((address & 0x00ff0000)  >> 16);
			mBuff[3] = (byte)((address & 0x0000ff00)  >>  8);
			mBuff[4] = (byte)(address & 0x000000ff);
			mBuff[5] = size;
			Array.Copy(buff, 0, mBuff, 6, size);

			WriteReport();

			Thread.Sleep(100);
		}

		/// <summary>
		/// Current Wiimote state
		/// </summary>
		public WiimoteState WiimoteState
		{
			get { return mWiimoteState; }
		}

        public int GetNumConnectRemotes()
        {
            return connectedRemoteIDs.Count;
        }

        public int GetRemoteID()
        {
            return remoteID;
        }

		public bool AltWriteMethod
		{
			get { return mAltWriteMethod; }
			set { mAltWriteMethod = value; }
		}

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			// close up our handles
			if(disposing)
				Disconnect();
		}
		#endregion
	}
}
