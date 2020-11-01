//--------------------------------------------------------------------------------------
// File: DXMUTData.cs
//
// DirectX SDK Managed Direct3D sample framework data class
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using SharpDX.Direct3D9;

namespace Microsoft.Samples.DirectX.UtilityToolkit
{
    #region Framework Interfaces and Eventargs classes

    /// <summary>Interface that the framework will use to call into samples</summary>
    public interface IFrameworkCallback
    {
        void OnFrameMove(Device device, double totalTime, float elapsedTime);
        void OnFrameRender(Device device, double totalTime, float elapsedTime);
    }

    /// <summary>Interface that the framework will use to determine if a device is acceptable</summary>
    public interface IDeviceCreation
    {
        bool IsDeviceAcceptable(Caps caps, Format adapterFormat, Format backBufferFormat, bool isWindowed);
        void ModifyDeviceSettings(DeviceSettings settings, Caps caps);
    }

    /// <summary>Event arguments for device creation/reset</summary>
    public class DeviceEventArgs : EventArgs
    {
        public SurfaceDescription BackBufferDescription;

        // Class data
        public Device Device;

        public DeviceEventArgs(Device d, SurfaceDescription desc)
        {
            Device = d;
            BackBufferDescription = desc;
        }
    }

    /// <summary>Event Handler delegate for device creation/reset</summary>
    public delegate void DeviceEventHandler(object sender, DeviceEventArgs e);

    #endregion

    #region Device Settings

    /// <summary>
    ///     Holds the settings for creating a device
    /// </summary>
    public class DeviceSettings : ICloneable
    {
        public Format AdapterFormat;
        public uint AdapterOrdinal;
        public CreateFlags BehaviorFlags;
        public DeviceType DeviceType;
        public PresentParameters presentParams;

        #region ICloneable Members

        /// <summary>Clone this object</summary>
        public DeviceSettings Clone()
        {
            var clonedObject = new DeviceSettings();
            clonedObject.presentParams = (PresentParameters) presentParams.Clone();
            clonedObject.AdapterFormat = AdapterFormat;
            clonedObject.AdapterOrdinal = AdapterOrdinal;
            clonedObject.BehaviorFlags = BehaviorFlags;
            clonedObject.DeviceType = DeviceType;

            return clonedObject;
        }

        /// <summary>Clone this object</summary>
        object ICloneable.Clone()
        {
            throw new NotSupportedException("Use the strongly typed overload instead.");
        }

        #endregion
    }

    #endregion

    #region User Timers

    /// <summary>Stores timer callback information</summary>
    public struct TimerData
    {
        public TimerCallback callback;
        public float TimeoutInSecs;
        public float Countdown;
        public bool IsEnabled;
    }

    #endregion

    #region Callback methods

    public delegate IntPtr WndProcCallback(IntPtr hWnd, NativeMethods.WindowMessage msg, IntPtr wParam, IntPtr lParam,
        ref bool NoFurtherProcessing);

    public delegate void TimerCallback(uint eventId);

    #endregion

    #region Matching Options

    /// <summary>
    ///     Used when finding valid device settings
    /// </summary>
    public enum MatchType
    {
        IgnoreInput, // Use the closest valid value to a default 
        PreserveInput, // Use input without change, but may cause no valid device to be found
        ClosestToInput // Use the closest valid value to the input 
    }

    /// <summary>
    ///     Options on how to match items
    /// </summary>
    public struct MatchOptions
    {
        public MatchType AdapterOrdinal;
        public MatchType DeviceType;
        public MatchType Windowed;
        public MatchType AdapterFormat;
        public MatchType VertexProcessing;
        public MatchType Resolution;
        public MatchType BackBufferFormat;
        public MatchType BackBufferCount;
        public MatchType MultiSample;
        public MatchType SwapEffect;
        public MatchType DepthFormat;
        public MatchType StencilFormat;
        public MatchType PresentFlags;
        public MatchType RefreshRate;
        public MatchType PresentInterval;
    }

    #endregion

    #region Framework's data

    /// <summary>
    ///     Holds data for the Framework class, and all of the properties
    /// </summary>
    internal class FrameworkData
    {
        /// <summary>
        ///     Initialize data
        /// </summary>
        public FrameworkData()
        {
            // Set some initial data
            OverrideStartX = -1;
            OverrideStartY = -1;
            OverrideAdapterOrdinal = -1;
            CanAutoChangeAdapter = true;
            IsShowingMsgBoxOnError = true;
            IsActive = true;
            DefaultStartingLocation = FormStartPosition.WindowsDefaultLocation;
        }

        #region Instance Data

        #endregion

        #region Properties

        public Device Device { get; set; }

        public DeviceSettings CurrentDeviceSettings { get; set; }

        public SurfaceDescription BackBufferSurfaceDesc { get; set; }

        public Caps Caps { get; set; }

        public System.Windows.Forms.Control WindowFocus { get; set; }

        public System.Windows.Forms.Control WindowDeviceFullScreen { get; set; }

        public System.Windows.Forms.Control WindowDeviceWindowed { get; set; }

        public IntPtr AdapterMonitor { get; set; }

        public double CurrentTime { get; set; }

        public float ElapsedTime { get; set; }

        public FormStartPosition DefaultStartingLocation { get; set; }

        public Rectangle ClientRectangle { get; set; }

        public Rectangle FullScreenClientRectangle { get; set; }

        public Rectangle WindowBoundsRectangle { get; set; }

        public Point ClientLocation { get; set; }

        public MainMenu Menu { get; set; }

        public double LastStatsUpdateTime { get; set; }

        public uint LastStatsUpdateFrames { get; set; }

        public float CurrentFrameRate { get; set; }

        public int CurrentFrameNumber { get; set; }

        public bool IsHandlingDefaultHotkeys { get; set; }

        public bool IsShowingMsgBoxOnError { get; set; }

        public bool AreStatsHidden { get; set; }

        public bool IsCursorClippedWhenFullScreen { get; set; }

        public bool IsShowingCursorWhenFullScreen { get; set; }

        public bool IsUsingConstantFrameTime { get; set; }

        public float TimePerFrame { get; set; }

        public bool IsInWireframeMode { get; set; }

        public bool CanAutoChangeAdapter { get; set; }

        public bool IsWindowCreatedWithDefaultPositions { get; set; }

        public int ApplicationExitCode { get; set; }

        public bool IsInited { get; set; }

        public bool WasWindowCreated { get; set; }

        public bool WasDeviceCreated { get; set; }

        public bool WasInitCalled { get; set; }

        public bool WasWindowCreateCalled { get; set; }

        public bool WasDeviceCreateCalled { get; set; }

        public bool AreDeviceObjectsCreated { get; set; }

        public bool AreDeviceObjectsReset { get; set; }

        public bool IsInsideDeviceCallback { get; set; }

        public bool IsInsideMainloop { get; set; }

        public bool IsActive { get; set; }

        public bool IsTimePaused { get; set; }

        public bool IsRenderingPaused { get; set; }

        public int PauseRenderingCount { get; set; }

        public int PauseTimeCount { get; set; }

        public bool IsDeviceLost { get; set; }

        public bool IsMinimized { get; set; }

        public bool IsMaximized { get; set; }

        public bool AreSizeChangesIgnored { get; set; }

        public bool IsNotifiedOnMouseMove { get; set; }

        public int OverrideAdapterOrdinal { get; set; }

        public bool IsOverridingWindowed { get; set; }

        public bool IsOverridingFullScreen { get; set; }

        public int OverrideStartX { get; set; }

        public int OverrideStartY { get; set; }

        public int OverrideWidth { get; set; }

        public int OverrideHeight { get; set; }

        public bool IsOverridingForceHardware { get; set; }

        public bool IsOverridingForceReference { get; set; }

        public bool IsOverridingForcePureHardwareVertexProcessing { get; set; }

        public bool IsOverridingForceHardwareVertexProcessing { get; set; }

        public bool IsOverridingForceSoftwareVertexProcessing { get; set; }

        public bool IsOverridingConstantFrameTime { get; set; }

        public float OverrideConstantTimePerFrame { get; set; }

        public int OverrideQuitAfterFrame { get; set; }

        public IDeviceCreation DeviceCreationInterface { get; set; }

        public IFrameworkCallback CallbackInterface { get; set; }

        public WndProcCallback WndProcFunction { get; set; }

        public SettingsDialog Settings { get; set; }

        public bool IsD3DSettingsDialogShowing { get; set; }

        public ArrayList Timers { get; set; } = new ArrayList();

        public string StaticFrameStats { get; set; }

        public string FrameStats { get; set; }

        public string DeviceStats { get; set; }

        public string WindowTitle { get; set; }

        #endregion
    }

    #endregion

    #region Framework's Default Window

    /// <summary>
    ///     The main window that will be used for the sample framework
    /// </summary>
    public class GraphicsWindow : Form
    {
        private readonly Framework frame;

        public GraphicsWindow(Framework f)
        {
            frame = f;
            MinimumSize = Framework.MinWindowSize;
        }

        /// <summary>
        ///     Will call into the sample framework's window proc
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            frame.WindowsProcedure(ref m);
            base.WndProc(ref m);
        }
    }

    #endregion
}