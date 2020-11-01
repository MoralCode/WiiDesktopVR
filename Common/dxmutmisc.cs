//--------------------------------------------------------------------------------------
// File: DXMUTMisc.cs
//
// Shortcut and helper functions for using DX Code
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using SharpDX;
using SharpDX.DirectInput;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;
using Font = SharpDX.Direct3D9.Font;
using Matrix = SharpDX.Matrix;
using Point = SharpDX.Point;
using Rectangle = SharpDX.Rectangle;
using Effect = SharpDX.Direct3D9.Effect;
using Device = SharpDX.Direct3D9.Device;
using Microsoft.Win32;

namespace Microsoft.Samples.DirectX.UtilityToolkit
{
    #region RefWarningDialog Form

    internal class SwitchRefDialog : Form
    {
        internal const string KeyLocation = @"Software\Microsoft\DirectX 9.0 SDK\ManagedSamples";
        internal const string KeyValueName = "SkipWarning";

        public SwitchRefDialog(string title)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            // Use the 'question' icon
            pictureBox1.Image = SystemIcons.Question.ToBitmap();
            // Include text
            lblInfo.Text =
                "Switching to the Direct3D reference rasterizer, a software device that implements the entire Direct3D feature set, but runs very slowly.\r\nDo you wish to continue?";
            // UPdate title
            Text = title;
        }

        /// <summary>
        ///     Dialog is being dismissed, either continue the application, or shutdown.
        ///     Save setting if required.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            // Is the box checked?
            if (chkShowAgain.Checked)
                using (var key = Registry.CurrentUser.CreateSubKey(KeyLocation))
                {
                    key.SetValue(KeyValueName, 1);
                }
        }

        #region Windows Form Designer generated code

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.CheckBox chkShowAgain;
        private System.Windows.Forms.Button btnYes;
        private System.Windows.Forms.Button btnNo;

        /// <summary>
        ///     Required method for Designer support - do not modify
        ///     the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblInfo = new System.Windows.Forms.Label();
            this.chkShowAgain = new System.Windows.Forms.CheckBox();
            this.btnYes = new System.Windows.Forms.Button();
            this.btnNo = new System.Windows.Forms.Button();
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(16, 16);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(32, 32);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // lblInfo
            // 
            this.lblInfo.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.lblInfo.Location = new System.Drawing.Point(64, 16);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(328, 48);
            this.lblInfo.TabIndex = 99;
            // 
            // chkShowAgain
            // 
            this.chkShowAgain.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.chkShowAgain.Location = new System.Drawing.Point(8, 104);
            this.chkShowAgain.Name = "chkShowAgain";
            this.chkShowAgain.Size = new System.Drawing.Size(224, 16);
            this.chkShowAgain.TabIndex = 2;
            this.chkShowAgain.Text = "&Don\'t show again";
            // 
            // btnYes
            // 
            this.btnYes.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnYes.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnYes.Location = new System.Drawing.Point(117, 72);
            this.btnYes.Name = "btnYes";
            this.btnYes.Size = new System.Drawing.Size(80, 24);
            this.btnYes.TabIndex = 0;
            this.btnYes.Text = "&Yes";
            this.btnYes.Click += new EventHandler(OnYes);
            // 
            // btnNo
            // 
            this.btnNo.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnNo.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnNo.Location = new System.Drawing.Point(205, 72);
            this.btnNo.Name = "btnNo";
            this.btnNo.Size = new System.Drawing.Size(80, 24);
            this.btnNo.TabIndex = 1;
            this.btnNo.Text = "&No";
            this.btnNo.Click += new EventHandler(OnNo);
            // 
            // SwitchRefDialog
            // 
            this.AcceptButton = this.btnYes;
            this.CancelButton = this.btnNo;
            this.ClientSize = new System.Drawing.Size(402, 134);
            this.Controls.Add(this.btnNo);
            this.Controls.Add(this.btnYes);
            this.Controls.Add(this.chkShowAgain);
            this.Controls.Add(this.lblInfo);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SwitchRefDialog";
            this.Text = "SampleName";
            this.ResumeLayout(false);
        }

        /// <summary>
        ///     Fired when the 'Yes' button is clicked
        /// </summary>
        private void OnYes(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }

        /// <summary>
        ///     Fired when the 'No' button is clicked
        /// </summary>
        private void OnNo(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Close();
        }

        #endregion
    }

    #endregion

    #region Native Methods

    /// <summary>
    ///     Will hold native methods which are interop'd
    /// </summary>
    public class NativeMethods
    {
        #region Win32 User Messages / Structures

        /// <summary>Window messages</summary>
        public enum WindowMessage : uint
        {
            // Misc messages
            Destroy = 0x0002,
            Close = 0x0010,
            Quit = 0x0012,
            Paint = 0x000F,
            SetCursor = 0x0020,
            ActivateApplication = 0x001C,
            EnterMenuLoop = 0x0211,
            ExitMenuLoop = 0x0212,
            NonClientHitTest = 0x0084,
            PowerBroadcast = 0x0218,
            SystemCommand = 0x0112,
            GetMinMax = 0x0024,

            // Keyboard messages
            KeyDown = 0x0100,
            KeyUp = 0x0101,
            Character = 0x0102,
            SystemKeyDown = 0x0104,
            SystemKeyUp = 0x0105,
            SystemCharacter = 0x0106,

            // Mouse messages
            MouseMove = 0x0200,
            LeftButtonDown = 0x0201,
            LeftButtonUp = 0x0202,
            LeftButtonDoubleClick = 0x0203,
            RightButtonDown = 0x0204,
            RightButtonUp = 0x0205,
            RightButtonDoubleClick = 0x0206,
            MiddleButtonDown = 0x0207,
            MiddleButtonUp = 0x0208,
            MiddleButtonDoubleClick = 0x0209,
            MouseWheel = 0x020a,
            XButtonDown = 0x020B,
            XButtonUp = 0x020c,
            XButtonDoubleClick = 0x020d,
            MouseFirst = LeftButtonDown, // Skip mouse move, it happens a lot and there is another message for that
            MouseLast = XButtonDoubleClick,

            // Sizing
            EnterSizeMove = 0x0231,
            ExitSizeMove = 0x0232,
            Size = 0x0005
        }

        /// <summary>Mouse buttons</summary>
        public enum MouseButtons
        {
            Left = 0x0001,
            Right = 0x0002,
            Middle = 0x0010,
            Side1 = 0x0020,
            Side2 = 0x0040
        }

        /// <summary>Windows Message</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct Message
        {
            public IntPtr hWnd;
            public WindowMessage msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point p;
        }

        /// <summary>MinMax Info structure</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MinMaxInformation
        {
            public Point reserved;
            public Point MaxSize;
            public Point MaxPosition;
            public Point MinTrackSize;
            public Point MaxTrackSize;
        }

        /// <summary>Monitor Info structure</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MonitorInformation
        {
            public uint Size; // Size of this structure
            public Rectangle MonitorRectangle;
            public Rectangle WorkRectangle;
            public uint Flags; // Possible flags
        }

        #endregion

        #region Windows API calls

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("winmm.dll")]
        public static extern IntPtr timeBeginPeriod(uint period);

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("kernel32")]
        public static extern bool QueryPerformanceFrequency(ref long PerformanceFrequency);

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("kernel32")]
        public static extern bool QueryPerformanceCounter(ref long PerformanceCount);

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hWnd, ref MonitorInformation info);

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern short GetAsyncKeyState(uint key);

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetCapture(IntPtr handle);

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ReleaseCapture();

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern int GetCaretBlinkTime();

        [SuppressUnmanagedCodeSecurity] // We won't use this maliciously
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin,
            uint messageFilterMax, uint flags);

        #endregion

        #region Class Methods

        private NativeMethods()
        {
        } // No creation

        /// <summary>Returns the low word</summary>
        public static short LoWord(uint l)
        {
            return (short) (l & 0xffff);
        }

        /// <summary>Returns the high word</summary>
        public static short HiWord(uint l)
        {
            return (short) (l >> 16);
        }

        /// <summary>Makes two shorts into a long</summary>
        public static uint MakeUInt32(short l, short r)
        {
            return (uint) ((l & 0xffff) | ((r & 0xffff) << 16));
        }

        /// <summary>Is this key down right now</summary>
        public static bool IsKeyDown(Key key)
        {
            return (GetAsyncKeyState((int) Key.LeftShift) & 0x8000) != 0;
        }

        #endregion
    }

    #endregion

    #region Timer

    public class FrameworkTimer
    {
        /// <summary>
        ///     Returns true if timer stopped
        /// </summary>
        public static bool IsStopped { get; private set; }

        /// <summary>
        ///     Resets the timer
        /// </summary>
        public static void Reset()
        {
            if (!isUsingQPF)
                return; // Nothing to do

            // Get either the current time or the stop time
            long time = 0;
            if (stopTime != 0)
                time = stopTime;
            else
                NativeMethods.QueryPerformanceCounter(ref time);

            baseTime = time;
            lastElapsedTime = time;
            stopTime = 0;
            IsStopped = false;
        }

        /// <summary>
        ///     Starts the timer
        /// </summary>
        public static void Start()
        {
            if (!isUsingQPF)
                return; // Nothing to do

            // Get either the current time or the stop time
            long time = 0;
            if (stopTime != 0)
                time = stopTime;
            else
                NativeMethods.QueryPerformanceCounter(ref time);

            if (IsStopped)
                baseTime += time - stopTime;
            stopTime = 0;
            lastElapsedTime = time;
            IsStopped = false;
        }

        /// <summary>
        ///     Stop (or pause) the timer
        /// </summary>
        public static void Stop()
        {
            if (!isUsingQPF)
                return; // Nothing to do

            if (!IsStopped)
            {
                // Get either the current time or the stop time
                long time = 0;
                if (stopTime != 0)
                    time = stopTime;
                else
                    NativeMethods.QueryPerformanceCounter(ref time);

                stopTime = time;
                lastElapsedTime = time;
                IsStopped = true;
            }
        }

        /// <summary>
        ///     Advance the timer a tenth of a second
        /// </summary>
        public static void Advance()
        {
            if (!isUsingQPF)
                return; // Nothing to do

            stopTime += ticksPerSecond / 10;
        }

        /// <summary>
        ///     Get the absolute system time
        /// </summary>
        public static double GetAbsoluteTime()
        {
            if (!isUsingQPF)
                return -1.0; // Nothing to do

            // Get either the current time or the stop time
            long time = 0;
            if (stopTime != 0)
                time = stopTime;
            else
                NativeMethods.QueryPerformanceCounter(ref time);

            var absolueTime = time / (double) ticksPerSecond;
            return absolueTime;
        }

        /// <summary>
        ///     Get the current time
        /// </summary>
        public static double GetTime()
        {
            if (!isUsingQPF)
                return -1.0; // Nothing to do

            // Get either the current time or the stop time
            long time = 0;
            if (stopTime != 0)
                time = stopTime;
            else
                NativeMethods.QueryPerformanceCounter(ref time);

            var appTime = (time - baseTime) / (double) ticksPerSecond;
            return appTime;
        }

        /// <summary>
        ///     get the time that elapsed between GetElapsedTime() calls
        /// </summary>
        public static double GetElapsedTime()
        {
            if (!isUsingQPF)
                return -1.0; // Nothing to do

            // Get either the current time or the stop time
            long time = 0;
            if (stopTime != 0)
                time = stopTime;
            else
                NativeMethods.QueryPerformanceCounter(ref time);

            var elapsedTime = (time - lastElapsedTime) / (double) ticksPerSecond;
            lastElapsedTime = time;
            return elapsedTime;
        }

        #region Instance Data

        private static readonly bool isUsingQPF;
        private static readonly long ticksPerSecond;
        private static long stopTime;
        private static long lastElapsedTime;
        private static long baseTime;

        #endregion

        #region Creation

        private FrameworkTimer()
        {
        } // No creation

        /// <summary>
        ///     Static creation routine
        /// </summary>
        static FrameworkTimer()
        {
            IsStopped = true;
            ticksPerSecond = 0;
            stopTime = 0;
            lastElapsedTime = 0;
            baseTime = 0;
            // Use QueryPerformanceFrequency to get frequency of the timer
            isUsingQPF = NativeMethods.QueryPerformanceFrequency(ref ticksPerSecond);
        }

        #endregion
    }

    #endregion

    #region Resource Cache

    /// <summary>Information about a cached texture</summary>
    internal struct CachedTexture
    {
        public string Source; // Data source
        public int Width;
        public int Height;
        public int Depth;
        public int MipLevels;
        public Usage Usage;
        public Format Format;
        public Pool Pool;
        public ResourceType Type;
    }

    /// <summary>Information about a cached effect</summary>
    internal struct CachedEffect
    {
        public string Source; // Data source
        public ShaderFlags Flags;
    }

    /// <summary>
    ///     Will be a resource cache for any resources that may be required by a sample
    ///     This class will be 'static'
    /// </summary>
    public class ResourceCache
    {
        protected Hashtable effectCache = new Hashtable(); // Cache of effects
        protected Hashtable fontCache = new Hashtable(); // Cache of fonts

        protected Hashtable textureCache = new Hashtable(); // Cache of textures

        #region Creation

        private ResourceCache()
        {
        } // Don't allow creation

        private static ResourceCache localObject;

        public static ResourceCache GetGlobalInstance()
        {
            if (localObject == null)
                localObject = new ResourceCache();

            return localObject;
        }

        #endregion

        #region Cache Creation Methods

        /// <summary>Create a texture from a file</summary>
        public Texture CreateTextureFromFile(Device device, string filename)
        {
            return CreateTextureFromFileEx(device, filename, D3DX.Default, D3DX.Default, D3DX.Default, Usage.None,
                Format.Unknown, Pool.Managed, (Filter) D3DX.Default, (Filter) D3DX.Default, 0);
        }

        /// <summary>Create a texture from a file</summary>
        public Texture CreateTextureFromFileEx(Device device, string filename, int w, int h, int mip, Usage usage,
            Format fmt, Pool pool, Filter filter, Filter mipfilter, int colorkey)
        {
            // Search the cache first
            foreach (CachedTexture ct in textureCache.Keys)
                if (string.Compare(ct.Source, filename, true) == 0 &&
                    ct.Width == w &&
                    ct.Height == h &&
                    ct.MipLevels == mip &&
                    ct.Usage == usage &&
                    ct.Format == fmt &&
                    ct.Pool == pool &&
                    ct.Type == ResourceType.Textures)
                    // A match was found, return that
                    return textureCache[ct] as Texture;

            // No matching entry, load the resource and add it to the cache
            Texture t = Texture.FromFile(device, filename, w, h, mip, usage, fmt, pool, filter, mipfilter,
                colorkey);
            var entry = new CachedTexture();
            entry.Source = filename;
            entry.Width = w;
            entry.Height = h;
            entry.MipLevels = mip;
            entry.Usage = usage;
            entry.Format = fmt;
            entry.Pool = pool;
            entry.Type = ResourceType.Texture;

            textureCache.Add(entry, t);

            return t;
        }

        /// <summary>Create a cube texture from a file</summary>
        public CubeTexture CreateCubeTextureFromFile(Device device, string filename)
        {
            return CreateCubeTextureFromFileEx(device, filename, D3DX.Default, D3DX.Default, Usage.None,
                Format.Unknown, Pool.Managed, (Filter) D3DX.Default, (Filter) D3DX.Default, 0);
        }

        /// <summary>Create a cube texture from a file</summary>
        public CubeTexture CreateCubeTextureFromFileEx(Device device, string filename, int size, int mip, Usage usage,
            Format fmt, Pool pool, Filter filter, Filter mipfilter, int colorkey)
        {
            // Search the cache first
            foreach (CachedTexture ct in textureCache.Keys)
                if (string.Compare(ct.Source, filename, true) == 0 &&
                    ct.Width == size &&
                    ct.MipLevels == mip &&
                    ct.Usage == usage &&
                    ct.Format == fmt &&
                    ct.Pool == pool &&
                    ct.Type == ResourceType.CubeTexture)
                    // A match was found, return that
                    return textureCache[ct] as CubeTexture;

            // No matching entry, load the resource and add it to the cache
            CubeTexture t = Texture.FromCubeFile(device, filename, size, mip, usage, fmt, pool, filter, mipfilter,
                colorkey);
            var entry = new CachedTexture();
            entry.Source = filename;
            entry.Width = size;
            entry.MipLevels = mip;
            entry.Usage = usage;
            entry.Format = fmt;
            entry.Pool = pool;
            entry.Type = ResourceType.CubeTexture;

            textureCache.Add(entry, t);

            return t;
        }

        /// <summary>Create a volume texture from a file</summary>
        public VolumeTexture CreateVolumeTextureFromFile(Device device, string filename)
        {
            return CreateVolumeTextureFromFileEx(device, filename, D3DX.Default, D3DX.Default, D3DX.Default,
                D3DX.Default, Usage.None,
                Format.Unknown, Pool.Managed, (Filter) D3DX.Default, (Filter) D3DX.Default, 0);
        }

        /// <summary>Create a volume texture from a file</summary>
        public VolumeTexture CreateVolumeTextureFromFileEx(Device device, string filename, int w, int h, int d, int mip,
            Usage usage, Format fmt, Pool pool, Filter filter, Filter mipfilter, int colorkey)
        {
            // Search the cache first
            foreach (CachedTexture ct in textureCache.Keys)
                if (string.Compare(ct.Source, filename, true) == 0 &&
                    ct.Width == w &&
                    ct.Height == h &&
                    ct.Depth == d &&
                    ct.MipLevels == mip &&
                    ct.Usage == usage &&
                    ct.Format == fmt &&
                    ct.Pool == pool &&
                    ct.Type == ResourceType.VolumeTexture)
                    // A match was found, return that
                    return textureCache[ct] as VolumeTexture;

            // No matching entry, load the resource and add it to the cache
            VolumeTexture t = Texture.FromVolumeFile(device, filename, w, h, d, mip, usage, fmt, pool, filter,
                mipfilter, colorkey);
            var entry = new CachedTexture();
            entry.Source = filename;
            entry.Width = w;
            entry.Height = h;
            entry.Depth = d;
            entry.MipLevels = mip;
            entry.Usage = usage;
            entry.Format = fmt;
            entry.Pool = pool;
            entry.Type = ResourceType.VolumeTexture;

            textureCache.Add(entry, t);

            return t;
        }

        /// <summary>Create an effect from a file</summary>
        public Effect CreateEffectFromFile(Device device, string filename, Macro[] defines, Include includeFile,
            ShaderFlags flags, EffectPool effectPool, out string errors)
        {
            // No errors at first!
            errors = string.Empty;
            // Search the cache first
            foreach (CachedEffect ce in effectCache.Keys)
                if (string.Compare(ce.Source, filename, true) == 0 &&
                    ce.Flags == flags)
                    // A match was found, return that
                    return effectCache[ce] as Effect;

            // Nothing found in the cache
            Effect e = Effect.FromFile(device, filename, defines, includeFile, null, flags, effectPool);
            // Add this to the cache
            var entry = new CachedEffect();
            entry.Flags = flags;
            entry.Source = filename;
            effectCache.Add(entry, e);

            // Return the new effect
            return e;
        }

        /// <summary>Create an effect from a file</summary>
        public Effect CreateEffectFromFile(Device device, string filename, Macro[] defines, Include includeFile,
            ShaderFlags flags, EffectPool effectPool)
        {
            string temp;
            return CreateEffectFromFile(device, filename, defines, includeFile, flags, effectPool, out temp);
        }

        /// <summary>Create a font object</summary>
        public Font CreateFont(Device device, int height, int width, FontWeight weight, int mip, bool italic,
            CharacterSet charSet, Precision outputPrecision, FontQuality quality, PitchAndFamily pandf, string fontName)
        {
            // Create the font description
            FontDescription desc = new FontDescription();
            desc.Height = height;
            desc.Width = width;
            desc.Weight = weight;
            desc.MipLevels = mip;
            desc.Italic = italic;
            desc.CharacterSet = charSet;
            desc.OutputPrecision = outputPrecision;
            desc.Quality = quality;
            desc.PitchAndFamily = pandf;
            desc.FaceName = fontName;

            // return the font
            return CreateFont(device, desc);
        }

        /// <summary>Create a font object</summary>
        public Font CreateFont(Device device, FontDescription desc)
        {
            // Search the cache first
            foreach (FontDescription fd in fontCache.Keys)
                if (string.Compare(fd.FaceName, desc.FaceName, true) == 0 &&
                    fd.CharacterSet == desc.CharacterSet &&
                    fd.Height == desc.Height &&
                    fd.Italic == desc.Italic &&
                    fd.MipLevels == desc.MipLevels &&
                    fd.OutputPrecision == desc.OutputPrecision &&
                    fd.PitchAndFamily == desc.PitchAndFamily &&
                    fd.Quality == desc.Quality &&
                    fd.Weight == desc.Weight &&
                    fd.Width == desc.Width)
                    // A match was found, return that
                    return fontCache[fd] as Font;

            // Couldn't find anything in the cache, create one
            Font f = new Font(device, desc);
            // Create a new entry
            fontCache.Add(desc, f);

            // return the new font
            return f;
        }

        #endregion

        #region Device event callbacks

        /// <summary>
        ///     Called when the device is created
        /// </summary>
        public void OnCreateDevice(Device device)
        {
        } // Nothing to do on device create

        /// <summary>
        ///     Called when the device is reset
        /// </summary>
        public void OnResetDevice(Device device)
        {
            // Call OnResetDevice on all effect and font objects
            foreach (Font f in fontCache.Values)
                f.OnResetDevice();
            foreach (Effect e in effectCache.Values)
                e.OnResetDevice();
        }

        /// <summary>
        ///     Clear any resources that need to be lost
        /// </summary>
        public void OnLostDevice()
        {
            foreach (Font f in fontCache.Values)
                f.OnLostDevice();
            foreach (Effect e in effectCache.Values)
                e.OnLostDevice();

            // Search the texture cache 
            foreach (CachedTexture ct in textureCache.Keys)
                if (ct.Pool == Pool.Default)
                    // A match was found, get rid of it
                    switch (ct.Type)
                    {
                        case ResourceType.Te:
                            (textureCache[ct] as Texture).Dispose();
                            break;
                        case ResourceType.CubeTexture:
                            (textureCache[ct] as CubeTexture).Dispose();
                            break;
                        case ResourceType.VolumeTexture:
                            (textureCache[ct] as VolumeTexture).Dispose();
                            break;
                    }
        }

        /// <summary>
        ///     Destroy any resources and clear the caches
        /// </summary>
        public void OnDestroyDevice()
        {
            // Cleanup the fonts
            foreach (Font f in fontCache.Values)
                f.Dispose();

            // Cleanup the effects
            foreach (Effect e in effectCache.Values)
                e.Dispose();

            // Dispose of any items in the caches
            foreach (BaseTexture texture in textureCache.Values)
                if (texture != null)
                    texture.Dispose();

            // Clear all of the caches
            textureCache.Clear();
            fontCache.Clear();
            effectCache.Clear();
        }

        #endregion
    }

    #endregion

    #region Arcball

    /// <summary>
    ///     Class holds arcball data
    /// </summary>
    public class ArcBall
    {
        // Class methods

        /// <summary>
        ///     Create new instance of the arcball class
        /// </summary>
        public ArcBall()
        {
            Reset();
            //changed to zero from empty
            downPt = Vector3.Zero;
            currentPt = Vector3.Zero;

            var active = Form.ActiveForm;
            if (active != null)
            {
                var rect = active.ClientRectangle;
                SetWindow(rect.Width, rect.Height);
            }
        }

        /// <summary>
        ///     Resets the arcball
        /// </summary>
        public void Reset()
        {
            downQuat = Quaternion.Identity;
            nowQuat = Quaternion.Identity;
            rotation = Matrix.Identity;
            translation = Matrix.Identity;
            translationDelta = Matrix.Identity;
            isDragging = false;
            radius = 1.0f;
            radiusTranslation = 1.0f;
        }

        /// <summary>
        ///     Convert a screen point to a vector
        /// </summary>
        public Vector3 ScreenToVector(float screenPointX, float screenPointY)
        {
            var x = -(screenPointX - width / 2.0f) / (radius * width / 2.0f);
            var y = (screenPointY - height / 2.0f) / (radius * height / 2.0f);
            var z = 0.0f;
            var mag = x * x + y * y;

            if (mag > 1.0f)
            {
                var scale = 1.0f / (float) Math.Sqrt(mag);
                x *= scale;
                y *= scale;
            }
            else
            {
                z = (float) Math.Sqrt(1.0f - mag);
            }

            return new Vector3(x, y, z);
        }

        /// <summary>
        ///     Set window paramters
        /// </summary>
        public void SetWindow(int w, int h, float r)
        {
            width = w;
            height = h;
            radius = r;
            center = new Vector2(w / 2.0f, h / 2.0f);
        }

        public void SetWindow(int w, int h)
        {
            SetWindow(w, h, 0.9f); // default radius
        }

        /// <summary>
        ///     Computes a quaternion from ball points
        /// </summary>
        public static Quaternion QuaternionFromBallPoints(Vector3 from, Vector3 to)
        {
            float dot = Vector3.Dot(from, to);
            Vector3 part = Vector3.Cross(from, to);
            return new Quaternion(part.X, part.Y, part.Z, dot);
        }

        /// <summary>
        ///     Begin the arcball 'dragging'
        /// </summary>
        public void OnBegin(int x, int y)
        {
            isDragging = true;
            downQuat = nowQuat;
            downPt = ScreenToVector(x, y);
        }

        /// <summary>
        ///     The arcball is 'moving'
        /// </summary>
        public void OnMove(int x, int y)
        {
            if (isDragging)
            {
                currentPt = ScreenToVector(x, y);
                nowQuat = downQuat * QuaternionFromBallPoints(downPt, currentPt);
            }
        }

        /// <summary>
        ///     Done dragging the arcball
        /// </summary>
        public void OnEnd()
        {
            isDragging = false;
        }

        /// <summary>
        ///     Handle messages from the window
        /// </summary>
        public bool HandleMessages(IntPtr hWnd, NativeMethods.WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            // Current mouse position
            var mouseX = NativeMethods.LoWord((uint) lParam.ToInt32());
            var mouseY = NativeMethods.HiWord((uint) lParam.ToInt32());

            switch (msg)
            {
                case NativeMethods.WindowMessage.LeftButtonDown:
                case NativeMethods.WindowMessage.LeftButtonDoubleClick:
                    // Set capture
                    NativeMethods.SetCapture(hWnd);
                    OnBegin(mouseX, mouseY);
                    return true;
                case NativeMethods.WindowMessage.LeftButtonUp:
                    // Release capture
                    NativeMethods.ReleaseCapture();
                    OnEnd();
                    return true;

                case NativeMethods.WindowMessage.RightButtonDown:
                case NativeMethods.WindowMessage.RightButtonDoubleClick:
                case NativeMethods.WindowMessage.MiddleButtonDown:
                case NativeMethods.WindowMessage.MiddleButtonDoubleClick:
                    // Set capture
                    NativeMethods.SetCapture(hWnd);
                    // Store off the position of the cursor
                    lastMousePosition = new Point(mouseX, mouseY);
                    return true;

                case NativeMethods.WindowMessage.RightButtonUp:
                case NativeMethods.WindowMessage.MiddleButtonUp:
                    // Release capture
                    NativeMethods.ReleaseCapture();
                    return true;

                case NativeMethods.WindowMessage.MouseMove:
                    var buttonState = NativeMethods.LoWord((uint) wParam.ToInt32());
                    var leftButton = (buttonState & (short) NativeMethods.MouseButtons.Left) != 0;
                    var rightButton = (buttonState & (short) NativeMethods.MouseButtons.Right) != 0;
                    var middleButton = (buttonState & (short) NativeMethods.MouseButtons.Middle) != 0;

                    if (leftButton)
                    {
                        OnMove(mouseX, mouseY);
                    }
                    else if (rightButton || middleButton)
                    {
                        // Normalize based on size of window and bounding sphere radius
                        var deltaX = (lastMousePosition.X - mouseX) * radiusTranslation / width;
                        var deltaY = (lastMousePosition.Y - mouseY) * radiusTranslation / height;

                        if (rightButton)
                        {
                            translationDelta = Matrix.Translation(-2 * deltaX, 2 * deltaY, 0.0f);
                            translation *= translationDelta;
                        }
                        else // Middle button
                        {
                            translationDelta = Matrix.Translation(0.0f, 0.0f, 5 * deltaY);
                            translation *= translationDelta;
                        }

                        // Store off the position of the cursor
                        lastMousePosition = new Point(mouseX, mouseY);
                    }

                    return true;
            }

            return false;
        }

        #region Instance Data

        protected Matrix rotation; // Matrix for arc ball's orientation
        protected Matrix translation; // Matrix for arc ball's position
        protected Matrix translationDelta; // Matrix for arc ball's position

        protected int width; // arc ball's window width
        protected int height; // arc ball's window height
        protected Vector2 center; // center of arc ball 
        protected float radius; // arc ball's radius in screen coords
        protected float radiusTranslation; // arc ball's radius for translating the target

        protected Quaternion downQuat; // Quaternion before button down
        protected Quaternion nowQuat; // Composite quaternion for current drag
        protected bool isDragging; // Whether user is dragging arc ball

        protected Point lastMousePosition; // position of last mouse point
        protected Vector3 downPt; // starting point of rotation arc
        protected Vector3 currentPt; // current point of rotation arc

        #endregion

        #region Simple Properties

        /// <summary>Gets the rotation matrix</summary>
        public Matrix RotationMatrix
        {
            get { return rotation = Matrix.RotationQuaternion(nowQuat); }
        }

        /// <summary>Gets the translation matrix</summary>
        public Matrix TranslationMatrix => translation;

        /// <summary>Gets the translation delta matrix</summary>
        public Matrix TranslationDeltaMatrix => translationDelta;

        /// <summary>Gets the dragging state</summary>
        public bool IsBeingDragged => isDragging;

        /// <summary>Gets or sets the current quaternion</summary>
        public Quaternion CurrentQuaternion
        {
            get => nowQuat;
            set => nowQuat = value;
        }

        #endregion
    }

    #endregion

    #region Cameras

    /// <summary>
    ///     Used to map keys to the camera
    /// </summary>
    public enum CameraKeys : byte
    {
        StrafeLeft,
        StrafeRight,
        MoveForward,
        MoveBackward,
        MoveUp,
        MoveDown,
        Reset,
        ControlDown,
        MaxKeys,
        Unknown = 0xff
    }

    /// <summary>
    ///     Mouse button mask values
    /// </summary>
    [Flags]
    public enum MouseButtonMask : byte
    {
        None = 0,
        Left = 0x01,
        Middle = 0x02,
        Right = 0x04,
        Wheel = 0x08
    }

    /// <summary>
    ///     Simple base camera class that moves and rotates.  The base class
    ///     records mouse and keyboard input for use by a derived class, and
    ///     keeps common state.
    /// </summary>
    public abstract class Camera
    {
        /// <summary>
        ///     Constructor for the base camera class (Sets up camera defaults)
        /// </summary>
        protected Camera()
        {
            // Create the keys
            keys = new bool[(int) CameraKeys.MaxKeys];

            // Set attributes for the view matrix
            //converted from empty to zero
            eye = Vector3.Zero;
            lookAt = new Vector3(0.0f, 0.0f, 1.0f);

            // Setup the view matrix
            SetViewParameters(eye, lookAt);

            // Setup the projection matrix
            SetProjectionParameters((float) Math.PI / 4, 1.0f, 1.0f, 1000.0f);

            // Store mouse information
            lastMousePosition = Cursor.Position;
            isMouseLButtonDown = false;
            isMouseRButtonDown = false;
            isMouseMButtonDown = false;
            mouseWheelDelta = 0;
            currentButtonMask = 0;

            // Setup camera rotations
            cameraYawAngle = 0.0f;
            cameraPitchAngle = 0.0f;

            dragRectangle = new Rectangle(0, 0, int.MaxValue, int.MaxValue);
            //vectors changed to zero from empty
            velocity = Vector3.Zero;
            isMovementDrag = false;
            velocityDrag = Vector3.Zero;
            dragTimer = 0.0f;
            totalDragTimeToZero = 0.25f;
            rotationVelocity = Vector2.Zero;
            rotationScaler = 0.1f;
            moveScaler = 5.0f;
            isInvertPitch = false;
            isEnableYAxisMovement = true;
            isEnablePositionMovement = true;
            mouseDelta = Vector2.Zero;
            framesToSmoothMouseData = 2.0f;
            isClipToBoundary = false;
            minBoundary = new Vector3(-1.0f, -1.0f, -1.0f);
            maxBoundary = new Vector3(1, 1, 1);
            isResetCursorAfterMove = false;
        }

        /// <summary>
        ///     Maps NativeMethods.WindowMessage.Key* msg to a camera key
        /// </summary>
        protected static CameraKeys MapKey(IntPtr param)
        {
            var key = (Key) param.ToInt32();
            switch (key)
            {
                case Key.LeftControl: return CameraKeys.ControlDown;
                case Key.Left: return CameraKeys.StrafeLeft;
                case Key.Right: return CameraKeys.StrafeRight;
                case Key.Up: return CameraKeys.MoveForward;
                case Key.Down: return CameraKeys.MoveBackward;
                case Key.PageUp: return CameraKeys.MoveUp;
                case Key.PageDown: return CameraKeys.MoveDown;

                case Key.A: return CameraKeys.StrafeLeft;
                case Key.D: return CameraKeys.StrafeRight;
                case Key.W: return CameraKeys.MoveForward;
                case Key.S: return CameraKeys.MoveBackward;
                case Key.Q: return CameraKeys.MoveUp;
                case Key.E: return CameraKeys.MoveDown;

                case Key.NumberPad4: return CameraKeys.StrafeLeft;
                case Key.NumberPad6: return CameraKeys.StrafeRight;
                case Key.NumberPad8: return CameraKeys.MoveForward;
                case Key.NumberPad2: return CameraKeys.MoveBackward;
                case Key.NumberPad9: return CameraKeys.MoveUp;
                case Key.NumberPad3: return CameraKeys.MoveDown;

                case Key.Home: return CameraKeys.Reset;
            }

            // No idea
            return (CameraKeys) byte.MaxValue;
        }

        /// <summary>
        ///     Abstract method to control camera during frame move
        /// </summary>
        public abstract void FrameMove(float elapsedTime);

        /// <summary>
        ///     Call this from your message proc so this class can handle window messages
        /// </summary>
        public virtual bool HandleMessages(IntPtr hWnd, NativeMethods.WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                // Handle the keyboard
                case NativeMethods.WindowMessage.KeyDown:
                    var mappedKeyDown = MapKey(wParam);
                    if (mappedKeyDown != (CameraKeys) byte.MaxValue)
                        // Valid key was pressed, mark it as 'down'
                        keys[(int) mappedKeyDown] = true;
                    break;
                case NativeMethods.WindowMessage.KeyUp:
                    var mappedKeyUp = MapKey(wParam);
                    if (mappedKeyUp != (CameraKeys) byte.MaxValue)
                        // Valid key was let go, mark it as 'up'
                        keys[(int) mappedKeyUp] = false;
                    break;

                // Handle the mouse
                case NativeMethods.WindowMessage.LeftButtonDoubleClick:
                case NativeMethods.WindowMessage.LeftButtonDown:
                case NativeMethods.WindowMessage.RightButtonDoubleClick:
                case NativeMethods.WindowMessage.RightButtonDown:
                case NativeMethods.WindowMessage.MiddleButtonDoubleClick:
                case NativeMethods.WindowMessage.MiddleButtonDown:
                {
                    // Compute the drag rectangle in screen coord.
                    var cursor = new Point(
                        NativeMethods.LoWord((uint) lParam.ToInt32()),
                        NativeMethods.HiWord((uint) lParam.ToInt32()));

                    // Update the variable state
                    if ((msg == NativeMethods.WindowMessage.LeftButtonDown ||
                         msg == NativeMethods.WindowMessage.LeftButtonDoubleClick)
                        && dragRectangle.Contains(cursor.X, cursor.Y))
                    {
                        isMouseLButtonDown = true;
                        currentButtonMask |= (int) MouseButtonMask.Left;
                    }

                    if ((msg == NativeMethods.WindowMessage.MiddleButtonDown ||
                         msg == NativeMethods.WindowMessage.MiddleButtonDoubleClick)
                        && dragRectangle.Contains(cursor.X, cursor.Y))
                    {
                        isMouseMButtonDown = true;
                        currentButtonMask |= (int) MouseButtonMask.Middle;
                    }

                    if ((msg == NativeMethods.WindowMessage.RightButtonDown ||
                         msg == NativeMethods.WindowMessage.RightButtonDoubleClick)
                        && dragRectangle.Contains(cursor.X, cursor.Y))
                    {
                        isMouseRButtonDown = true;
                        currentButtonMask |= (int) MouseButtonMask.Right;
                    }

                    // Capture the mouse, so if the mouse button is 
                    // released outside the window, we'll get the button up messages
                    NativeMethods.SetCapture(hWnd);

                    lastMousePosition = Cursor.Position;
                    return true;
                }
                case NativeMethods.WindowMessage.LeftButtonUp:
                case NativeMethods.WindowMessage.RightButtonUp:
                case NativeMethods.WindowMessage.MiddleButtonUp:
                {
                    // Update member var state
                    if (msg == NativeMethods.WindowMessage.LeftButtonUp)
                    {
                        isMouseLButtonDown = false;
                        currentButtonMask &= ~(int) MouseButtonMask.Left;
                    }

                    if (msg == NativeMethods.WindowMessage.RightButtonUp)
                    {
                        isMouseRButtonDown = false;
                        currentButtonMask &= ~(int) MouseButtonMask.Right;
                    }

                    if (msg == NativeMethods.WindowMessage.MiddleButtonUp)
                    {
                        isMouseMButtonDown = false;
                        currentButtonMask &= ~(int) MouseButtonMask.Middle;
                    }

                    // Release the capture if no mouse buttons are down
                    if (!isMouseLButtonDown && !isMouseMButtonDown && !isMouseRButtonDown)
                        NativeMethods.ReleaseCapture();
                }
                    break;

                // Handle the mouse wheel
                case NativeMethods.WindowMessage.MouseWheel:
                    mouseWheelDelta = NativeMethods.HiWord((uint) wParam.ToInt32()) / 120;
                    break;
            }

            return false;
        }


        /// <summary>
        ///     Reset the camera's position back to the default
        /// </summary>
        public virtual void Reset()
        {
            SetViewParameters(defaultEye, defaultLookAt);
        }

        /// <summary>
        ///     Client can call this to change the position and direction of camera
        /// </summary>
        public virtual unsafe void SetViewParameters(Vector3 eyePt, Vector3 lookAtPt)
        {
            // Store the data
            defaultEye = eye = eyePt;
            defaultLookAt = lookAt = lookAtPt;

            // Calculate the view matrix
            viewMatrix = Matrix.LookAtLH(eye, lookAt, UpDirection);

            // Get the inverted matrix
            Matrix inverseView = Matrix.Invert(viewMatrix);

            // The axis basis vectors and camera position are stored inside the 
            // position matrix in the 4 rows of the camera's world matrix.
            // To figure out the yaw/pitch of the camera, we just need the Z basis vector
            Vector3* pZBasis = (Vector3*) &inverseView.M31;
            cameraYawAngle = (float) Math.Atan2(pZBasis->X, pZBasis->Z);
            var len = (float) Math.Sqrt(pZBasis->Z * pZBasis->Z + pZBasis->X * pZBasis->X);
            cameraPitchAngle = -(float) Math.Atan2(pZBasis->Y, len);
        }

        /// <summary>
        ///     Calculates the projection matrix based on input params
        /// </summary>
        public virtual void SetProjectionParameters(float fov, float aspect, float near, float far)
        {
            // Set attributes for the projection matrix
            fieldOfView = fov;
            aspectRatio = aspect;
            nearPlane = near;
            farPlane = far;

            projMatrix = Matrix.PerspectiveFovLH(fov, aspect, near, far);
        }

        /// <summary>
        ///     Figure out the mouse delta based on mouse movement
        /// </summary>
        protected void UpdateMouseDelta(float elapsedTime)
        {
            // Get the current mouse position
            var current = Cursor.Position;

            // Calculate how far it's moved since the last frame
            var delta = new Point(current.X - lastMousePosition.X,
                current.Y - lastMousePosition.Y);

            // Record the current position for next time
            lastMousePosition = current;

            if (isResetCursorAfterMove)
            {
                // Set position of camera to center of desktop, 
                // so it always has room to move.  This is very useful
                // if the cursor is hidden.  If this isn't done and cursor is hidden, 
                // then invisible cursor will hit the edge of the screen 
                // and the user can't tell what happened
                var activeScreen = Screen.PrimaryScreen;
                var center = new Point(activeScreen.Bounds.Width / 2,
                    activeScreen.Bounds.Height / 2);
                Cursor.Position = center;
                lastMousePosition = center;
            }

            // Smooth the relative mouse data over a few frames so it isn't 
            // jerky when moving slowly at low frame rates.
            var percentOfNew = 1.0f / framesToSmoothMouseData;
            var percentOfOld = 1.0f - percentOfNew;
            mouseDelta.X = mouseDelta.X * percentOfNew + delta.X * percentOfNew;
            mouseDelta.Y = mouseDelta.Y * percentOfNew + delta.Y * percentOfNew;

            rotationVelocity = mouseDelta * rotationScaler;
        }

        /// <summary>
        ///     Figure out the velocity based on keyboard input & drag if any
        /// </summary>
        protected void UpdateVelocity(float elapsedTime)
        {
            Vector3 accel = Vector3.Empty;

            if (isEnablePositionMovement)
            {
                // Update acceleration vector based on keyboard state
                if (keys[(int) CameraKeys.MoveForward])
                    accel.Z += 1.0f;
                if (keys[(int) CameraKeys.MoveBackward])
                    accel.Z -= 1.0f;
                if (isEnableYAxisMovement)
                {
                    if (keys[(int) CameraKeys.MoveUp])
                        accel.Y += 1.0f;
                    if (keys[(int) CameraKeys.MoveDown])
                        accel.Y -= 1.0f;
                }

                if (keys[(int) CameraKeys.StrafeRight])
                    accel.X += 1.0f;
                if (keys[(int) CameraKeys.StrafeLeft])
                    accel.X -= 1.0f;
            }

            // Normalize vector so if moving 2 dirs (left & forward), 
            // the camera doesn't move faster than if moving in 1 dir
            accel.Normalize();
            // Scale the acceleration vector
            accel *= moveScaler;

            if (isMovementDrag)
            {
                // Is there any acceleration this frame?
                if (accel.LengthSquared() > 0)
                {
                    // If so, then this means the user has pressed a movement key
                    // so change the velocity immediately to acceleration 
                    // upon keyboard input.  This isn't normal physics
                    // but it will give a quick response to keyboard input
                    velocity = accel;
                    dragTimer = totalDragTimeToZero;
                    velocityDrag = accel * (1 / dragTimer);
                }
                else
                {
                    // If no key being pressed, then slowly decrease velocity to 0
                    if (dragTimer > 0)
                    {
                        velocity -= velocityDrag * elapsedTime;
                        dragTimer -= elapsedTime;
                    }
                    else
                    {
                        // Zero velocity
                        //changed from Empty. this effectively confirms that empty and zero are the same between DirectX and SharpDX.
                        velocity = Vector3.Zero;
                    }
                }
            }
            else
            {
                // No drag, so immediately change the velocity
                velocity = accel;
            }
        }

        /// <summary>
        ///     Clamps V to lie inside boundaries
        /// </summary>
        protected void ConstrainToBoundary(ref Vector3 v)
        {
            // Constrain vector to a bounding box 
            v.X = Math.Max(v.X, minBoundary.X);
            v.Y = Math.Max(v.Y, minBoundary.Y);
            v.Z = Math.Max(v.Z, minBoundary.Z);

            v.X = Math.Min(v.X, maxBoundary.X);
            v.Y = Math.Min(v.Y, maxBoundary.Y);
            v.Z = Math.Min(v.Z, maxBoundary.Z);
        }


        #region Instance Data

        protected Matrix viewMatrix; // View Matrix
        protected Matrix projMatrix; // Projection matrix

        protected Point lastMousePosition; // Last absolute position of mouse cursor
        protected bool isMouseLButtonDown; // True if left button is down 
        protected bool isMouseMButtonDown; // True if middle button is down 
        protected bool isMouseRButtonDown; // True if right button is down 
        protected int currentButtonMask; // mask of which buttons are down
        protected int mouseWheelDelta; // Amount of middle wheel scroll (+/-) 
        protected Vector2 mouseDelta; // Mouse relative delta smoothed over a few frames
        protected float framesToSmoothMouseData; // Number of frames to smooth mouse data over

        protected Vector3 defaultEye; // Default camera eye position
        protected Vector3 defaultLookAt; // Default LookAt position
        protected Vector3 eye; // Camera eye position
        protected Vector3 lookAt; // LookAt position
        protected float cameraYawAngle; // Yaw angle of camera
        protected float cameraPitchAngle; // Pitch angle of camera

        protected Rectangle dragRectangle; // Rectangle within which a drag can be initiated.
        protected Vector3 velocity; // Velocity of camera

        protected bool
            isMovementDrag; // If true, then camera movement will slow to a stop otherwise movement is instant

        protected Vector3 velocityDrag; // Velocity drag force
        protected float dragTimer; // Countdown timer to apply drag
        protected float totalDragTimeToZero; // Time it takes for velocity to go from full to 0
        protected Vector2 rotationVelocity; // Velocity of camera

        protected float fieldOfView; // Field of view
        protected float aspectRatio; // Aspect ratio
        protected float nearPlane; // Near plane
        protected float farPlane; // Far plane

        protected float rotationScaler; // Scaler for rotation
        protected float moveScaler; // Scaler for movement

        protected bool isInvertPitch; // Invert the pitch axis
        protected bool isEnablePositionMovement; // If true, then the user can translate the camera/model 
        protected bool isEnableYAxisMovement; // If true, then camera can move in the y-axis

        protected bool isClipToBoundary; // If true, then the camera will be clipped to the boundary
        protected Vector3 minBoundary; // Min point in clip boundary
        protected Vector3 maxBoundary; // Max point in clip boundary

        protected bool
            isResetCursorAfterMove; // If true, the class will reset the cursor position so that the cursor always has space to move 

        // State of the input
        protected bool[] keys;
        public static readonly Vector3 UpDirection = new Vector3(0, 1, 0);

        #endregion

        #region Simple Properties

        /// <summary>Is the camera being 'dragged' at all?</summary>
        public bool IsBeingDragged => isMouseLButtonDown || isMouseMButtonDown || isMouseRButtonDown;

        /// <summary>Is the left mouse button down</summary>
        public bool IsMouseLeftButtonDown => isMouseLButtonDown;

        /// <summary>Is the right mouse button down</summary>
        public bool IsMouseRightButtonDown => isMouseRButtonDown;

        /// <summary>Is the middle mouse button down</summary>
        public bool IsMouseMiddleButtonDown => isMouseMButtonDown;

        /// <summary>Returns the view transformation matrix</summary>
        public Matrix ViewMatrix => viewMatrix;

        /// <summary>Returns the projection transformation matrix</summary>
        public Matrix ProjectionMatrix => projMatrix;

        /// <summary>Returns the location of the eye</summary>
        public Vector3 EyeLocation => eye;

        /// <summary>Returns the look at point of the camera</summary>
        public Vector3 LookAtPoint => lookAt;

        /// <summary>Is position movement enabled</summary>
        public bool IsPositionMovementEnabled
        {
            get => isEnablePositionMovement;
            set => isEnablePositionMovement = value;
        }

        #endregion
    }

    /// <summary>
    ///     Simple first person camera class that moves and rotates.
    ///     It allows yaw and pitch but not roll.  It uses keyboard and
    ///     cursor to respond to keyboard and mouse input and updates the
    ///     view matrix based on input.
    /// </summary>
    public class FirstPersonCamera : Camera
    {
        // Mask to determine which button to enable for rotation
        protected int activeButtonMask = (int) (MouseButtonMask.Left | MouseButtonMask.Middle | MouseButtonMask.Right);

        // World matrix of the camera (inverse of the view matrix)
        protected Matrix cameraWorld;

        /// <summary>
        ///     Update the view matrix based on user input & elapsed time
        /// </summary>
        public override void FrameMove(float elapsedTime)
        {
            // Reset the camera if necessary
            if (keys[(int) CameraKeys.Reset])
                Reset();

            // Get the mouse movement (if any) if the mouse buttons are down
            if ((activeButtonMask & currentButtonMask) != 0)
                UpdateMouseDelta(elapsedTime);

            // Get amount of velocity based on the keyboard input and drag (if any)
            UpdateVelocity(elapsedTime);

            // Simple euler method to calculate position delta
            Vector3 posDelta = velocity * elapsedTime;

            // If rotating the camera 
            if ((activeButtonMask & currentButtonMask) != 0)
            {
                // Update the pitch & yaw angle based on mouse movement
                float yawDelta = rotationVelocity.X;
                float pitchDelta = rotationVelocity.Y;

                // Invert pitch if requested
                if (isInvertPitch)
                    pitchDelta = -pitchDelta;

                cameraPitchAngle += pitchDelta;
                cameraYawAngle += yawDelta;

                // Limit pitch to straight up or straight down
                cameraPitchAngle = Math.Max(-(float) Math.PI / 2.0f, cameraPitchAngle);
                cameraPitchAngle = Math.Min(+(float) Math.PI / 2.0f, cameraPitchAngle);
            }

            // Make a rotation matrix based on the camera's yaw & pitch
            Matrix cameraRotation = Matrix.RotationYawPitchRoll(cameraYawAngle, cameraPitchAngle, 0);

            // Transform vectors based on camera's rotation matrix
            Vector3 localUp = new Vector3(0, 1, 0);
            Vector3 localAhead = new Vector3(0, 0, 1);
            Vector3 worldUp = Vector3.TransformCoordinate(localUp, cameraRotation);
            Vector3 worldAhead = Vector3.TransformCoordinate(localAhead, cameraRotation);

            // Transform the position delta by the camera's rotation 
            Vector3 posDeltaWorld = Vector3.TransformCoordinate(posDelta, cameraRotation);
            if (!isEnableYAxisMovement)
                posDeltaWorld.Y = 0.0f;

            // Move the eye position 
            eye += posDeltaWorld;
            if (isClipToBoundary)
                ConstrainToBoundary(ref eye);

            // Update the lookAt position based on the eye position 
            lookAt = eye + worldAhead;

            // Update the view matrix
            viewMatrix = Matrix.LookAtLH(eye, lookAt, worldUp);
            cameraWorld = Matrix.Invert(viewMatrix);
        }

        /// <summary>
        ///     Enable or disable each of the mouse buttons for rotation drag.
        /// </summary>
        public void SetRotationButtons(bool left, bool middle, bool right)
        {
            activeButtonMask = (left ? (int) MouseButtonMask.Left : 0) |
                               (middle ? (int) MouseButtonMask.Middle : 0) |
                               (right ? (int) MouseButtonMask.Right : 0);
        }
    }

    /// <summary>
    ///     Simple model viewing camera class that rotates around the object.
    /// </summary>
    public class ModelViewerCamera : Camera
    {
        /// <summary>
        ///     Creates new instance of the model viewer camera
        /// </summary>
        public ModelViewerCamera()
        {
            world = Matrix.Identity;
            modelRotation = Matrix.Identity;
            lastModelRotation = Matrix.Identity;
            lastCameraRotation = Matrix.Identity;
            modelCenter = Vector3.Empty;
            radius = 5.0f;
            defaultRadius = 5.0f;
            minRadius = 1.0f;
            maxRadius = float.MaxValue;
            isPitchLimited = false;
            isEnablePositionMovement = false;
            attachCameraToModel = false;

            // Set button masks
            rotateModelButtonMask = (int) MouseButtonMask.Left;
            zoomButtonMask = (int) MouseButtonMask.Wheel;
            rotateCameraButtonMask = (int) MouseButtonMask.Right;
        }

        /// <summary>
        ///     Update the view matrix based on user input & elapsed time
        /// </summary>
        public override unsafe void FrameMove(float elapsedTime)
        {
            // Reset the camera if necessary
            if (keys[(int) CameraKeys.Reset])
                Reset();

            // Get the mouse movement (if any) if the mouse buttons are down
            if (currentButtonMask != 0)
                UpdateMouseDelta(elapsedTime);

            // Get amount of velocity based on the keyboard input and drag (if any)
            UpdateVelocity(elapsedTime);

            // Simple euler method to calculate position delta
            Vector3 posDelta = velocity * elapsedTime;

            // Change the radius from the camera to the model based on wheel scrolling
            if (mouseWheelDelta != 0 && zoomButtonMask == (int) MouseButtonMask.Wheel)
                radius -= mouseWheelDelta * radius * 0.1f;
            radius = Math.Min(maxRadius, radius);
            radius = Math.Max(minRadius, radius);
            mouseWheelDelta = 0;

            // Get the inverse of the arcball's rotation matrix
            Matrix cameraRotation = Matrix.Invert(viewArcball.RotationMatrix);

            // Transform vectors based on camera's rotation matrix
            Vector3 localUp = new Vector3(0, 1, 0);
            Vector3 localAhead = new Vector3(0, 0, 1);
            Vector3 worldUp = Vector3.TransformCoordinate(localUp, cameraRotation);
            Vector3 worldAhead = Vector3.TransformCoordinate(localAhead, cameraRotation);

            // Transform the position delta by the camera's rotation 
            Vector3 posDeltaWorld = Vector3.TransformCoordinate(posDelta, cameraRotation);

            // Move the lookAt position 
            lookAt += posDeltaWorld;
            if (isClipToBoundary)
                ConstrainToBoundary(ref lookAt);

            // Update the eye point based on a radius away from the lookAt position
            eye = lookAt - worldAhead * radius;

            // Update the view matrix
            viewMatrix = Matrix.LookAtLH(eye, lookAt, worldUp);
            Matrix invView = Matrix.Invert(viewMatrix);
            invView.M41 = invView.M42 = invView.M43 = 0;
            Matrix modelLastRotInv = Matrix.Invert(lastModelRotation);

            // Accumulate the delta of the arcball's rotation in view space.
            // Note that per-frame delta rotations could be problematic over long periods of time.
            Matrix localModel = worldArcball.RotationMatrix;
            modelRotation *= viewMatrix * modelLastRotInv * localModel * invView;
            if (viewArcball.IsBeingDragged && attachCameraToModel && !keys[(int) CameraKeys.ControlDown])
            {
                // Attah camera to model by inverse of the model rotation
                Matrix cameraRotInv = Matrix.Invert(lastCameraRotation);
                Matrix delta = cameraRotInv * cameraRotation; // local to world matrix
                modelRotation *= delta;
            }

            lastCameraRotation = cameraRotation;
            lastModelRotation = localModel;

            // Since we're accumulating delta rotations, we need to orthonormalize 
            // the matrix to prevent eventual matrix skew
            fixed (float* pxBasis = &modelRotation.M11)
            {
                fixed (float* pyBasis = &modelRotation.M21)
                {
                    fixed (float* pzBasis = &modelRotation.M31)
                    {
                        Vector3 x = new Vector3(*pxBasis);
                        x.Normalize();
                        Vector3 y = new Vector3(*pyBasis);
                        Vector3 z = new Vector3(*pzBasis);
                        Vector3.Cross(ref y, ref z, out x);
                        y.Normalize();
                        Vector3.Cross(ref z, ref x, out y);
                    }
                }
            }

            // Translate the rotation matrix to the same position as the lookAt position
            modelRotation.M41 = lookAt.X;
            modelRotation.M42 = lookAt.Y;
            modelRotation.M43 = lookAt.Z;

            // Translate world matrix so its at the center of the model
            Matrix trans = Matrix.Translation(-modelCenter.X, -modelCenter.Y, -modelCenter.Z);
            world = trans * modelRotation;
        }

        /// <summary>
        ///     Reset the camera's position back to the default
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            world = Matrix.Identity;
            modelRotation = Matrix.Identity;
            lastModelRotation = Matrix.Identity;
            lastCameraRotation = Matrix.Identity;
            radius = defaultRadius;
            worldArcball.Reset();
            viewArcball.Reset();
        }

        /// <summary>
        ///     Override for setting the view parameters
        /// </summary>
        public override void SetViewParameters(Vector3 eyePt, Vector3 lookAtPt)
        {
            // Call base first
            base.SetViewParameters(eyePt, lookAtPt);

            // Propogate changes to the member arcball
            Matrix rotation = Matrix.LookAtLH(eyePt, lookAtPt, UpDirection);
            viewArcball.CurrentQuaternion = Quaternion.RotationMatrix(rotation);

            // Set the radius according to the distance
            Vector3 eyeToPoint = lookAtPt - eyePt;
            SetRadius(eyeToPoint.Length());
        }

        /// <summary>
        ///     Call this from your message proc so this class can handle window messages
        /// </summary>
        public override bool HandleMessages(IntPtr hWnd, NativeMethods.WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            // Call base first
            base.HandleMessages(hWnd, msg, wParam, lParam);

            if ((msg == NativeMethods.WindowMessage.LeftButtonDown ||
                 msg == NativeMethods.WindowMessage.LeftButtonDoubleClick) &&
                (rotateModelButtonMask & (int) MouseButtonMask.Left) != 0 ||
                (msg == NativeMethods.WindowMessage.RightButtonDown ||
                 msg == NativeMethods.WindowMessage.RightButtonDoubleClick) &&
                (rotateModelButtonMask & (int) MouseButtonMask.Right) != 0 ||
                (msg == NativeMethods.WindowMessage.MiddleButtonDown ||
                 msg == NativeMethods.WindowMessage.MiddleButtonDoubleClick) &&
                (rotateModelButtonMask & (int) MouseButtonMask.Middle) != 0)
            {
                // Current mouse position
                var mouseX = NativeMethods.LoWord((uint) lParam.ToInt32());
                var mouseY = NativeMethods.HiWord((uint) lParam.ToInt32());
                worldArcball.OnBegin(mouseX, mouseY);
            }

            if ((msg == NativeMethods.WindowMessage.LeftButtonDown ||
                 msg == NativeMethods.WindowMessage.LeftButtonDoubleClick) &&
                (rotateCameraButtonMask & (int) MouseButtonMask.Left) != 0 ||
                (msg == NativeMethods.WindowMessage.RightButtonDown ||
                 msg == NativeMethods.WindowMessage.RightButtonDoubleClick) &&
                (rotateCameraButtonMask & (int) MouseButtonMask.Right) != 0 ||
                (msg == NativeMethods.WindowMessage.MiddleButtonDown ||
                 msg == NativeMethods.WindowMessage.MiddleButtonDoubleClick) &&
                (rotateCameraButtonMask & (int) MouseButtonMask.Middle) != 0)
            {
                // Current mouse position
                var mouseX = NativeMethods.LoWord((uint) lParam.ToInt32());
                var mouseY = NativeMethods.HiWord((uint) lParam.ToInt32());
                viewArcball.OnBegin(mouseX, mouseY);
            }

            if (msg == NativeMethods.WindowMessage.MouseMove)
            {
                // Current mouse position
                var mouseX = NativeMethods.LoWord((uint) lParam.ToInt32());
                var mouseY = NativeMethods.HiWord((uint) lParam.ToInt32());
                worldArcball.OnMove(mouseX, mouseY);
                viewArcball.OnMove(mouseX, mouseY);
            }

            if (msg == NativeMethods.WindowMessage.LeftButtonUp &&
                (rotateModelButtonMask & (int) MouseButtonMask.Left) != 0 ||
                msg == NativeMethods.WindowMessage.RightButtonUp &&
                (rotateModelButtonMask & (int) MouseButtonMask.Right) != 0 ||
                msg == NativeMethods.WindowMessage.MiddleButtonUp &&
                (rotateModelButtonMask & (int) MouseButtonMask.Middle) != 0)
                worldArcball.OnEnd();

            if (msg == NativeMethods.WindowMessage.LeftButtonUp &&
                (rotateCameraButtonMask & (int) MouseButtonMask.Left) != 0 ||
                msg == NativeMethods.WindowMessage.RightButtonUp &&
                (rotateCameraButtonMask & (int) MouseButtonMask.Right) != 0 ||
                msg == NativeMethods.WindowMessage.MiddleButtonUp &&
                (rotateCameraButtonMask & (int) MouseButtonMask.Middle) != 0)
                viewArcball.OnEnd();

            return false;
        }

        #region Instance Data

        protected ArcBall worldArcball = new ArcBall();
        protected ArcBall viewArcball = new ArcBall();
        protected Vector3 modelCenter;
        protected Matrix lastModelRotation; // Last arcball rotation matrix for model 
        protected Matrix lastCameraRotation; // Last rotation matrix for camera
        protected Matrix modelRotation; // Rotation matrix of model
        protected Matrix world; // World Matrix of model

        protected int rotateModelButtonMask;
        protected int zoomButtonMask;
        protected int rotateCameraButtonMask;

        protected bool isPitchLimited;
        protected float radius; // Distance from the camera to model 
        protected float defaultRadius; // Distance from the camera to model 
        protected float minRadius; // Min radius
        protected float maxRadius; // Max radius
        protected bool attachCameraToModel;

        #endregion

        #region Simple Properties/Set Methods

        /// <summary>The minimum radius</summary>
        public float MinimumRadius
        {
            get => minRadius;
            set => minRadius = value;
        }

        /// <summary>The maximum radius</summary>
        public float MaximumRadius
        {
            get => maxRadius;
            set => maxRadius = value;
        }

        /// <summary>Gets the world matrix</summary>
        public Matrix WorldMatrix => world;

        /// <summary>Sets the world quat</summary>
        public void SetWorldQuat(Quaternion q)
        {
            worldArcball.CurrentQuaternion = q;
        }

        /// <summary>Sets the view quat</summary>
        public void SetViewQuat(Quaternion q)
        {
            viewArcball.CurrentQuaternion = q;
        }

        /// <summary>Sets whether the pitch is limited or not</summary>
        public void SetIsPitchLimited(bool limit)
        {
            isPitchLimited = limit;
        }

        /// <summary>Sets the model's center</summary>
        public void SetModelCenter(Vector3 c)
        {
            modelCenter = c;
        }

        /// <summary>Sets radius</summary>
        public void SetRadius(float r, float min, float max)
        {
            radius = defaultRadius = r;
            minRadius = min;
            maxRadius = max;
        }

        /// <summary>Sets radius</summary>
        public void SetRadius(float r)
        {
            defaultRadius = r;
            minRadius = 1.0f;
            maxRadius = float.MaxValue;
        }

        /// <summary>Sets arcball window</summary>
        public void SetWindow(int w, int h, float r)
        {
            worldArcball.SetWindow(w, h, r);
            viewArcball.SetWindow(w, h, r);
        }

        /// <summary>Sets arcball window</summary>
        public void SetWindow(int w, int h)
        {
            worldArcball.SetWindow(w, h, 0.9f);
            viewArcball.SetWindow(w, h, 0.9f);
        }

        /// <summary>Sets button masks</summary>
        public void SetButtonMasks(int rotateModel, int zoom, int rotateCamera)
        {
            rotateCameraButtonMask = rotateCamera;
            zoomButtonMask = zoom;
            rotateModelButtonMask = rotateModel;
        }

        /// <summary>Is the camera attached to a model</summary>
        public bool IsAttachedToModel
        {
            get => attachCameraToModel;
            set => attachCameraToModel = value;
        }

        #endregion
    }

    #endregion

    #region Text Helper

    /// <summary>
    ///     Manages the intertion point when drawing text
    /// </summary>
    public struct TextHelper
    {
        private Font textFont; // Used to draw the text
        private Sprite textSprite; // Used to cache the drawn text
        private int color; // Color to draw the text
        private Point point; // Where to draw the text
        private readonly int lineHeight; // Height of the lines

        /// <summary>
        ///     Create a new instance of the text helper class
        /// </summary>
        public TextHelper(Font f, Sprite s, int l)
        {
            textFont = f;
            textSprite = s;
            lineHeight = l;
            color = unchecked((int) 0xffffffff);
            point = Point.Empty;
        }

        /// <summary>
        ///     Draw a line of text
        /// </summary>
        public void DrawTextLine(string text)
        {
            if (textFont == null)
                throw new InvalidOperationException("You cannot draw text.  There is no font object.");
            // Create the rectangle to draw to
            var rect = new Rectangle(point, Size.Empty);
            textFont.DrawText(textSprite, text, rect, DrawTextFormat.NoClip, color);

            // Increase the line height
            point.Y += lineHeight;
        }

        /// <summary>
        ///     Draw a line of text
        /// </summary>
        public void DrawTextLine(string text, params object[] args)
        {
            // Simply format the string and pass it on
            DrawTextLine(string.Format(text, args));
        }

        /// <summary>
        ///     Insertion point of the text
        /// </summary>
        public void SetInsertionPoint(Point p)
        {
            point = p;
        }

        public void SetInsertionPoint(int x, int y)
        {
            point.X = x;
            point.Y = y;
        }

        /// <summary>
        ///     The color of the text
        /// </summary>
        public void SetForegroundColor(int c)
        {
            color = c;
        }

        public void SetForegroundColor(Color c)
        {
            color = c.ToArgb();
        }

        /// <summary>
        ///     Begin the sprite rendering
        /// </summary>
        public void Begin()
        {
            if (textSprite != null) textSprite.Begin(SpriteFlags.AlphaBlend | SpriteFlags.SortTexture);
        }

        /// <summary>
        ///     End the sprite
        /// </summary>
        public void End()
        {
            if (textSprite != null) textSprite.End();
        }
    }

    #endregion

    #region Utility

    /// <summary>
    ///     Misc utility functionality
    /// </summary>
    public class Utility
    {
        // Constants for search folders
        private const string CurrentFolder = @".\";
        private const string MediaPath = @"Media\";

        // Typical folder locations
        //      .\
        //      ..\
        //      ..\..\
        //      %EXE_DIR%\
        //      %EXE_DIR%\..\
        //      %EXE_DIR%\..\..\
        //      %EXE_DIR%\..\%EXE_NAME%
        //      %EXE_DIR%\..\..\%EXE_NAME%
        //      DXSDK media path
        private static readonly string[] TypicalFolders =
        {
            CurrentFolder, @"..\",
            @"..\..\", @"{0}\", @"{0}\..\", @"{0}\..\..\", @"{0}\..\{1}\", @"{0}\..\..\{1}\"
        };

        private static bool firstTime = true;

        private Utility()
        {
            /* Private Constructor */
        }

        /// <summary>
        ///     Returns a valid path to a DXSDK media file
        /// </summary>
        /// <param name="path">Initial path to search</param>
        /// <param name="filename">Filename we're searching for</param>
        /// <returns>Full path to the file</returns>
        public static string FindMediaFile(string filename)
        {
            // Find out the executing assembly information
            var executingAssembly = Assembly.GetExecutingAssembly();
            // Now check the typical folders, before you can do that you'll need to get 
            // the executable name
            var exeName = Path.GetFileNameWithoutExtension(executingAssembly.Location);
            // And the executable folder
            var exeFolder = Path.GetDirectoryName(executingAssembly.Location);

            string filePath;
            // Now you can search the typical folders
            if (SearchTypicalFolders(filename, exeFolder, exeName, out filePath)) return filePath;

            // The file wasn't found again, search the folders with \media on them
            // Now you can search the typical folders
            if (SearchTypicalFolders(filename + MediaPath, exeFolder, exeName, out filePath)) return filePath;

            // We still haven't found the file yet, we should search the parents folders now
            if (SearchParentFolders(filename, CurrentFolder, "", out filePath)) return filePath;
            // We still haven't found the file yet, now search from the exe folder
            if (SearchParentFolders(filename, exeFolder, exeName, out filePath)) return filePath;

            // We still haven't found the file yet, we should search the parents folders now, but append media
            if (SearchParentFolders(filename, CurrentFolder, MediaPath, out filePath)) return filePath;
            // We still haven't found the file yet, now search from the exe folder and append media
            if (SearchParentFolders(filename, exeFolder, AppendDirectorySeparator(exeName) + MediaPath, out filePath))
                return filePath;


            // We still haven't found the file yet, the built samples are prefixed with 'cs', so see if that's the case
            if (exeName.ToLower().StartsWith("cs"))
            {
                // Build the new exe name by stripping off the 'cs' prefix and doing the searches again
                var newExeName = exeName.Substring(2, exeName.Length - 2);
                if (SearchParentFolders(filename, exeFolder, newExeName, out filePath)) return filePath;
                // We still haven't found the file yet, now search from the exe folder and append media
                if (SearchParentFolders(filename, exeFolder, AppendDirectorySeparator(newExeName) + MediaPath,
                    out filePath)) return filePath;
            }

            // Before throwing an exception, girst check to see if the file exists as is
            if (File.Exists(filename)) return filename;

            throw new MediaNotFoundException();
        }

        /// <summary>
        ///     Will search the typical list of folders for the file first
        /// </summary>
        /// <param name="filename">File we are looking for</param>
        /// <param name="exeFolder">Folder of the executable</param>
        /// <param name="exeName">Name of the executable</param>
        /// <param name="fullPath">Returned path if file is found.</param>
        /// <returns>true if the file was found; false otherwise</returns>
        private static bool SearchTypicalFolders(string filename, string exeFolder, string exeName, out string fullPath)
        {
            // First scan through each typical folder and see if we found the file
            for (var i = 0; i < TypicalFolders.Length; i++)
                try
                {
                    var info = new FileInfo(string.Format(TypicalFolders[i], exeFolder, exeName) + filename);
                    if (info.Exists)
                    {
                        fullPath = info.FullName;
                        return true;
                    }
                }
                catch (NotSupportedException)
                {
                    // This exception will be fired if the filename is not supported
                }

            // We never found any of the files
            fullPath = string.Empty;
            return false;
        }

        /// <summary>
        ///     Will search the parents of files looking for media
        /// </summary>
        /// <param name="filename">File we are looking for</param>
        /// <param name="rootNode">Folder of the executable</param>
        /// <param name="leafName">Name of the executable</param>
        /// <param name="fullPath">Returned path if file is found.</param>
        /// <returns>true if the file was found; false otherwise</returns>
        private static bool SearchParentFolders(string filename, string rootNode, string leafName, out string fullPath)
        {
            // Set the out parameter first
            fullPath = string.Empty;
            try
            {
                // Search the root node first
                var info = new FileInfo(AppendDirectorySeparator(rootNode) + AppendDirectorySeparator(leafName) +
                                        filename);
                if (info.Exists)
                {
                    fullPath = info.FullName;
                    return true;
                }
            }
            catch (NotSupportedException)
            {
                // The arguments passed in are not supported, fail now
                return false;
            }

            // Are we in the root yet?
            var dir = new DirectoryInfo(rootNode);
            if (dir.Parent != null)
                return SearchParentFolders(filename, dir.Parent.FullName, leafName, out fullPath);
            return false;
        }

        /// <summary>
        ///     Returns a valid string with a directory separator at the end.
        /// </summary>
        public static string AppendDirectorySeparator(string pathName)
        {
            if (!pathName.EndsWith(@"\"))
                return pathName + @"\";

            return pathName;
        }

        /// <summary>Returns the view matrix for a cube map face</summary>
        public static Matrix GetCubeMapViewMatrix(CubeMapFace face)
        {
            Vector3 vEyePt = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 vLookDir = new Vector3();
            Vector3 vUpDir = new Vector3();

            switch (face)
            {
                case CubeMapFace.PositiveX:
                    vLookDir = new Vector3(1.0f, 0.0f, 0.0f);
                    vUpDir = new Vector3(0.0f, 1.0f, 0.0f);
                    break;
                case CubeMapFace.NegativeX:
                    vLookDir = new Vector3(-1.0f, 0.0f, 0.0f);
                    vUpDir = new Vector3(0.0f, 1.0f, 0.0f);
                    break;
                case CubeMapFace.PositiveY:
                    vLookDir = new Vector3(0.0f, 1.0f, 0.0f);
                    vUpDir = new Vector3(0.0f, 0.0f, -1.0f);
                    break;
                case CubeMapFace.NegativeY:
                    vLookDir = new Vector3(0.0f, -1.0f, 0.0f);
                    vUpDir = new Vector3(0.0f, 0.0f, 1.0f);
                    break;
                case CubeMapFace.PositiveZ:
                    vLookDir = new Vector3(0.0f, 0.0f, 1.0f);
                    vUpDir = new Vector3(0.0f, 1.0f, 0.0f);
                    break;
                case CubeMapFace.NegativeZ:
                    vLookDir = new Vector3(0.0f, 0.0f, -1.0f);
                    vUpDir = new Vector3(0.0f, 1.0f, 0.0f);
                    break;
            }

            // Set the view transform for this cubemap surface
            Matrix matView = Matrix.LookAtLH(vEyePt, vLookDir, vUpDir);
            return matView;
        }

        /// <summary>Returns the view matrix for a cube map face</summary>
        public static Matrix GetCubeMapViewMatrix(int face)
        {
            return GetCubeMapViewMatrix((CubeMapFace) face);
        }

        /// <summary>
        ///     Displays the switching to ref device warning, and allows user to quit if they don't want to
        /// </summary>
        public static void DisplaySwitchingToRefWarning(Framework framework, string sampleTitle)
        {
            if (framework.IsShowingMsgBoxOnError)
            {
                // Read the registry key to see if the warning should be skipped
                var skipWarning = 0;
                try
                {
                    using (var key = Registry.CurrentUser.CreateSubKey(SwitchRefDialog.KeyLocation))
                    {
                        skipWarning = (int) key.GetValue(SwitchRefDialog.KeyValueName, 0);
                    }
                }
                catch
                {
                } // Ignore any errors

                if (skipWarning == 0 && firstTime) // Show dialog
                {
                    firstTime = false;
                    using (var dialog = new SwitchRefDialog(sampleTitle))
                    {
                        Application.Run(dialog);
                        if (dialog.DialogResult == DialogResult.Cancel)
                            // Shutdown the application
                            framework.Dispose();
                    }
                }
            }
        }
    }

    #endregion

    #region Widgets

    #region Direction Widget

    /// <summary>Widget for controlling direction</summary>
    public class DirectionWidget
    {
        /// <summary>Handle messages from the window</summary>
        public bool HandleMessages(IntPtr hWnd, NativeMethods.WindowMessage msg, IntPtr wParam, IntPtr lParam)
        {
            // Current mouse position
            var mouseX = NativeMethods.LoWord((uint) lParam.ToInt32());
            var mouseY = NativeMethods.HiWord((uint) lParam.ToInt32());

            switch (msg)
            {
                case NativeMethods.WindowMessage.LeftButtonDown:
                case NativeMethods.WindowMessage.MiddleButtonDown:
                case NativeMethods.WindowMessage.RightButtonDown:
                {
                    if ((RotateButtonMask & MouseButtonMask.Left) == MouseButtonMask.Left &&
                        msg == NativeMethods.WindowMessage.LeftButtonDown ||
                        (RotateButtonMask & MouseButtonMask.Right) == MouseButtonMask.Right &&
                        msg == NativeMethods.WindowMessage.RightButtonDown ||
                        (RotateButtonMask & MouseButtonMask.Middle) == MouseButtonMask.Middle &&
                        msg == NativeMethods.WindowMessage.MiddleButtonDown)
                    {
                        arc.OnBegin(mouseX, mouseY);
                        NativeMethods.SetCapture(hWnd);
                    }

                    return true;
                }
                case NativeMethods.WindowMessage.MouseMove:
                {
                    if (arc.IsBeingDragged)
                    {
                        arc.OnMove(mouseX, mouseY);
                        UpdateLightDirection();
                    }

                    return true;
                }
                case NativeMethods.WindowMessage.LeftButtonUp:
                case NativeMethods.WindowMessage.RightButtonUp:
                case NativeMethods.WindowMessage.MiddleButtonUp:
                {
                    if ((RotateButtonMask & MouseButtonMask.Left) == MouseButtonMask.Left &&
                        msg == NativeMethods.WindowMessage.LeftButtonUp ||
                        (RotateButtonMask & MouseButtonMask.Right) == MouseButtonMask.Right &&
                        msg == NativeMethods.WindowMessage.RightButtonUp ||
                        (RotateButtonMask & MouseButtonMask.Middle) == MouseButtonMask.Middle &&
                        msg == NativeMethods.WindowMessage.MiddleButtonUp)
                    {
                        arc.OnEnd();
                        NativeMethods.ReleaseCapture();
                    }

                    UpdateLightDirection();
                    return true;
                }
            }

            // Didn't handle the message
            return false;
        }

        /// <summary>Updates the light direction</summary>
        private unsafe void UpdateLightDirection()
        {
            Matrix invView = Matrix.Invert(viewMatrix);
            invView.M41 = invView.M42 = invView.M43 = 0;

            Matrix lastRotationInv = Matrix.Invert(rotationSnapshot);
            Matrix rot = arc.RotationMatrix;
            rotationSnapshot = rot;

            // Accumulate the delta of the arcball's rotation in view space.
            // Note that per-frame delta rotations could be problematic over long periods of time.
            rotation *= viewMatrix * lastRotationInv * rot * invView;


            // Since we're accumulating delta rotations, we need to orthonormalize 
            // the matrix to prevent eventual matrix skew
            fixed (float* pxBasis = &rotation.M11)
            {
                fixed (float* pyBasis = &rotation.M21)
                {
                    fixed (float* pzBasis = &rotation.M31)
                    {
                        Vector3 x = new Vector3(*pxBasis);
                        x.Normalize();
                        Vector3 y = new Vector3(*pyBasis);
                        Vector3 z = new Vector3(*pzBasis);
                        Vector3.Cross(ref y, ref z, out x);
                        y.Normalize();
                        Vector3.Cross(ref z, ref x, out y);
                    }
                }
            }

            // Transform the default direction vector by the light's rotation matrix
            currentDir = Vector3.TransformNormal(defaultDir, rotation);
        }

        /// <summary>Render the light widget</summary>
        public unsafe void OnRender(Color color, Matrix view, Matrix proj, Vector3 eye)
        {
            // Store the view matrix
            viewMatrix = view;

            // Render the light arrows so the user can visually see the light direction
            effect.Technique = "RenderWith1LightNoTexture";
            effect.SetValue("g_MaterialDiffuseColor", color);
            Vector3 eyePt = Vector3.Normalize(eye);

            // Set the light direction value
            effect.SetValue("g_LightDir", &eyePt, sizeof(Vector3));

            // Rotate arrow model to point towards origin
            Vector3 at = Vector3.Empty;
            Vector3 up = new Vector3(0, 1, 0);
            Matrix rotateB = Matrix.RotationX((float) Math.PI);
            Matrix rotateA = Matrix.LookAtLH(currentDir, at, up);
            rotateA.Invert();
            Matrix rotate = rotateB * rotateA;
            Vector3 l = currentDir * Radius * 1.0f;
            Matrix trans = Matrix.Translation(l);
            Matrix scale = Matrix.Scaling(Radius * 0.2f, Radius * 0.2f, Radius * 0.2f);

            Matrix world = rotate * scale * trans;
            Matrix worldViewProj = world * viewMatrix * proj;

            effect.SetValue("g_mWorldViewProjection", worldViewProj);
            effect.SetValue("g_mWorld", world);

            // Render the arrows
            for (var subset = 0; subset < 2; subset++)
            {
                int passes = effect.Begin(0);
                for (var pass = 0; pass < passes; pass++)
                {
                    effect.BeginPass(pass);
                    mesh.DrawSubset(subset);
                    effect.EndPass();
                }

                effect.End();
            }
        }

        #region Class level data (Instance/Static)

        // Instance members
        private readonly ArcBall arc = new ArcBall();
        private Vector3 defaultDir = new Vector3(0, 1, 0);
        private Vector3 currentDir = new Vector3(0, 1, 0);
        private Matrix viewMatrix = Matrix.Identity;
        private Matrix rotation = Matrix.Identity;
        private Matrix rotationSnapshot = Matrix.Identity;

        // Static members
        private static Device device = null;
        private static Effect effect = null;
        private static Mesh mesh = null;

        #endregion

        #region Properties

        /// <summary>Radius of this widget</summary>
        public float Radius { get; set; } = 1.0f;

        /// <summary>Light direction of this widget</summary>
        public Vector3 LightDirection
        {
            get => currentDir;
            set => currentDir = defaultDir = value;
        }

        /// <summary>Is this widget being dragged</summary>
        public bool IsBeingDragged => arc.IsBeingDragged;

        /// <summary>Rotation button mask</summary>
        public MouseButtonMask RotateButtonMask { get; set; } = MouseButtonMask.Right;

        #endregion

        #region Device handlers

        /// <summary>Called when the device has been created</summary>
        public static void OnCreateDevice(Device device)
        {
            // Store the device
            DirectionWidget.device = device;

            // Read the effect file
            var path = Utility.FindMediaFile("UI\\DXUTShared.fx");

            // If this fails, there should be debug output as to 
            // why the .fx file failed to compile (assuming you have dbmon running).
            // If you do not, you can turn on unmanaged debugging for this project.
            effect = Effect.FromFile(device, path, null, null, ShaderFlags.NotCloneable, null);

            // Load the mesh with D3DX and get back a Mesh.  For this
            // sample we'll ignore the X file's embedded materials since we know 
            // exactly the model we're loading.  See the mesh samples such as
            // "OptimizedMesh" for a more generic mesh loading example.
            path = Utility.FindMediaFile("UI\\arrow.x");
            mesh = Mesh.FromFile(path, MeshFlags.Managed, device);

            // Optimize the mesh for this graphics card's vertex cache 
            // so when rendering the mesh's triangle list the vertices will 
            // cache hit more often so it won't have to re-execute the vertex shader 
            // on those vertices so it will improve perf.     
            var adj = new int[mesh.NumberFaces * 3];
            mesh.GenerateAdjacency(1e-6f, adj);
            mesh.OptimizeInPlace(MeshFlags.OptimizeVertexCache, adj);
        }

        /// <summary>Called when the device has been reset</summary>
        public void OnResetDevice(SurfaceDescription desc)
        {
            arc.SetWindow(desc.Width, desc.Height);
        }

        /// <summary>Called when the device has been lost</summary>
        public static void OnLostDevice()
        {
            if (effect != null)
                effect.OnLostDevice();
        }

        /// <summary>Called when the device has been destroyed</summary>
        public static void OnDestroyDevice()
        {
            if (effect != null)
                effect.Dispose();
            if (mesh != null)
                mesh.Dispose();
            effect = null;
            mesh = null;
        }

        #endregion
    }

    #endregion

    #endregion
}