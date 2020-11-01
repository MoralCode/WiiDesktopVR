using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Samples.DirectX.UtilityToolkit;
using WiimoteLib;
using Direct3D = Microsoft.DirectX.Direct3D;

//for reading config file


namespace WiiDesktopVR
{
    internal class Point2D
    {
        public float x;
        public float y;

        public void set(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    public class WiiDesktopVR : Form
    {
        private VertexBuffer backgroundBuffer = null;
        private readonly string backgroundFilename = "stad_2.png";
        private readonly int backgroundStepCount = 10;
        private Texture backgroundtexture = null;

        private bool badConfigFile = false;
        private readonly float boxdepth = 8;
        private bool cameraIsAboveScreen; //has no affect until zeroing and then is set automatically.
        private float cameraVerticaleAngle; //begins assuming the camera is point straight forward

        // Our global variables for this project
        private Device device = null; // Our rendering device
        private readonly bool doFullscreen = true;

//        float dotDistanceInMM = 5.75f*25.4f;
        private float dotDistanceInMM = 8.5f * 25.4f; //width of the wii sensor bar

        private readonly bool doWiiCursors = true;


        //wiimote stuff
        private bool doWiimote = true;
        private bool doWiimote2;

        private readonly float fogdepth = 5;
        private int frameCount;
        private float frameRate;

        private VertexBuffer gridBuffer = null;

        private readonly int gridColor = 0xCCCCCC;
        private float headDist = 2;


        //headposition
        private float headX;
        private float headY;

        private bool isReady;

        private bool isRendering;
        private int lastFrameTick;

        private int lastKey;
        private int leftCursor = 1; //needed for rotation stabilization when two points appear
        private VertexBuffer lineBuffer = null;
        private readonly int lineColor = 0xFFFFFF;
        private readonly int lineDepth = -200;
        private readonly int m_dwHeight = 768;
        private readonly int m_dwWidth = 1024;

        private CrosshairCursor mouseCursor;
        private bool mouseDown;
        private readonly float movementScaling = 1.0f;

        private readonly int numGridlines = 10;
        private readonly int numInFront = 3;

        private readonly int numTargets = 10;

        private PresentParameters presentParams = new PresentParameters();

        private readonly float
            radiansPerPixel = (float) (Math.PI / 4) / 1024.0f; //45 degree field of view with a 1024x768 camera

        private readonly Random random = new Random();
        private float relativeVerticalAngle; //current head position view angle
        private Wiimote remote;
        private Wiimote remote2;

        //cube rotation
        private float rotX;
        private float rotY;
        private float rotZ;
        private float screenAspect;
        private float screenHeightinMM = 20 * 25.4f;


        private bool showBackground;


        private bool showGrid = true;
        private bool showHelp;
        private bool showLines = true;
        private bool showMouseCursor;
        private bool showTargets = true;
        private Microsoft.DirectX.Direct3D.Font statsFont = null; // Font for drawing text
        private VertexBuffer targetBuffer = null;
        private Vector3[] targetPositions;
        private readonly float targetScale = .065f;
        private Vector3[] targetSizes;

        private readonly Vertex[] targetVertices =
        {
            new Vertex(-1.0f, 1.0f, .0f, 0.0f, 0.0f),
            new Vertex(1.0f, 1.0f, .0f, 1.0f, 0.0f),
            new Vertex(-1.0f, -1.0f, .0f, 0.0f, 1.0f),
            new Vertex(1.0f, -1.0f, .0f, 1.0f, 1.0f)
        };

        private Sprite textSprite = null; // Sprite for batching text calls


        private Texture texture = null;
        private readonly string textureFilename = "target.png";
        private CrosshairCursor wiiCursor1;
        private CrosshairCursor wiiCursor2;
        private CrosshairCursor wiiCursor3;
        private CrosshairCursor wiiCursor4;
        private readonly int[] wiimotePointIDMap = new int[4];

        private readonly Point2D[] wiimotePointsNormalized = new Point2D[4];

        private readonly Matrix worldTransform = Matrix.Identity;

        public WiiDesktopVR()
        {
            // Set the initial size of our form
            ClientSize = new Size(m_dwWidth, m_dwHeight);

            loadConfigurationData();

            if (screenAspect == 0) //only override if it's emtpy
                screenAspect = m_dwWidth / (float) m_dwHeight;
            Text = "Wiimote Desktop VR";

            //add event handlers
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
            MouseMove += OnMouseMove;
            KeyDown += OnKeyPress;
            FormClosing += OnFormClosing;

            for (var i = 0; i < 4; i++)
            {
                wiimotePointsNormalized[i] = new Point2D();
                wiimotePointIDMap[i] = i;
            }
        }

        public void loadConfigurationData()
        {
            // create reader & open file
            try
            {
                TextReader tr = new StreamReader("config.dat");
                char[] seps = {':'};
                string line;
                string[] values;

                line = tr.ReadLine();
                values = line.Split(seps);
                screenHeightinMM = float.Parse(values[1]);

                line = tr.ReadLine();
                values = line.Split(seps);
                dotDistanceInMM = float.Parse(values[1]);

                line = tr.ReadLine();
                values = line.Split(seps);
                screenAspect = float.Parse(values[1]);

                line = tr.ReadLine();
                values = line.Split(seps);
                cameraIsAboveScreen = bool.Parse(values[1]);

                line = tr.ReadLine();
                values = line.Split(seps);
                doWiimote2 = bool.Parse(values[1]);

                // close the stream
                tr.Close();
            }
            catch (NullReferenceException)
            {
            }
            catch (FormatException)
            {
                //bad config, ignore
                throw new Exception("Config file is mal-formatted.");
            }
            catch (FileNotFoundException)
            {
                //no prexsting config, create one with the deafult values

                TextWriter tw = new StreamWriter("config.dat");

                // write a line of text to the file
                tw.WriteLine("screenHieght(mm):" + screenHeightinMM);
                tw.WriteLine("sensorBarWidth(mm):" + dotDistanceInMM);
                tw.WriteLine("screenAspect(width/height):" + screenAspect);
                tw.WriteLine("cameraIsAboveScreen(true/false):" + cameraIsAboveScreen);
                tw.WriteLine("cnnectToSecondWiimote(true/false):" + doWiimote2);

                // close the stream
                tw.Close();
            }
        }


        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            isReady = false; //set the flag to stop the rendering call driven by incoming wiimote reports
            Cursor.Show();
        }

        private void OnKeyPress(object sender, KeyEventArgs e)
        {
            lastKey = (int) e.KeyCode;
            if ((byte) e.KeyCode == (int) Keys.Escape)
            {
                isReady = false;
                Cursor.Show(); //set the flag to stop the rendering call driven by incoming wiimote reports
                Dispose(); // Esc was pressed
                return;
            }

            if ((byte) e.KeyCode == (int) Keys.Space)
            {
                //zeros the head position and computes the camera tilt
                var angle = Math.Acos(.5 / headDist) - Math.PI / 2; //angle of head to screen
                if (!cameraIsAboveScreen)
                    angle = -angle;
                cameraVerticaleAngle = (float) (angle - relativeVerticalAngle); //absolute camera angle 
            }

            if ((byte) e.KeyCode == 'C') cameraIsAboveScreen = !cameraIsAboveScreen;
            if ((byte) e.KeyCode == 'B')
                showBackground = !showBackground;
            if ((byte) e.KeyCode == 'G')
                showGrid = !showGrid;
            if ((byte) e.KeyCode == 'R')
                InitTargets();
            if ((byte) e.KeyCode == 'H')
                showHelp = !showHelp;
            if ((byte) e.KeyCode == 'T')
                showTargets = !showTargets;
            if ((byte) e.KeyCode == 'L')
                showLines = !showLines;
            if ((byte) e.KeyCode == 'M')
                showMouseCursor = !showMouseCursor;
            if ((byte) e.KeyCode == (int) Keys.Up)
            {
            }

            if ((byte) e.KeyCode == (int) Keys.Down)
            {
            }

            if ((byte) e.KeyCode == (int) Keys.Left)
            {
            }

            if ((byte) e.KeyCode == (int) Keys.Right)
            {
            }
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    mouseDown = true;
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    break;
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            mouseCursor.setDown(screenAspect * (e.X / (float) m_dwWidth - .5f) + .0f, .5f - e.Y / (float) m_dwHeight);

            if (mouseDown) //is dragging
            {
                //rotX += mouseCursor.X - mouseCursor.lastX;
                ///rotY += mouseCursor.Y - mouseCursor.lastY;
                //rotation = Matrix.RotationX(100 * rotX) * Matrix.RotationY(100 * rotY);

//                rotX = e.X/100.0f;
                //              rotY = e.Y/100.0f;
                //            rotation = Matrix.RotationX(rotX) * Matrix.RotationY(rotY);
            }
        }


        private void OnMouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    mouseDown = false;
                    break;
                case MouseButtons.Right:
                    break;
                case MouseButtons.Middle:
                    break;
            }
        }


        private void RenderText()
        {
            var txtHelper = new TextHelper(statsFont, textSprite, 15);
            txtHelper.Begin();
            txtHelper.SetInsertionPoint(5, 5);

            // Output statistics
            txtHelper.SetForegroundColor(Color.Yellow);
            txtHelper.DrawTextLine("Stats---------------");

            frameCount++;
            if (frameCount == 100)
            {
                frameRate = 100 * 1000.0f / (Environment.TickCount - lastFrameTick);
                lastFrameTick = Environment.TickCount;
                frameCount = 0;
            }

            txtHelper.DrawTextLine("Avg Framerate: " + frameRate);
            if (remote != null)
                txtHelper.DrawTextLine("Wii IR dots:" + remote.WiimoteState.IRState.IRSensors[0].Found + " "
                                       + remote.WiimoteState.IRState.IRSensors[1].Found + " "
                                       + remote.WiimoteState.IRState.IRSensors[2].Found + " "
                                       + remote.WiimoteState.IRState.IRSensors[3].Found);
            txtHelper.DrawTextLine("Last Key Pressed: " + lastKey);
            txtHelper.DrawTextLine("Mouse X-Y: " + mouseCursor.X + ", " + mouseCursor.Y);
            txtHelper.DrawTextLine("Est Head X-Y (mm): " + headX * screenHeightinMM + ", " + headY * screenHeightinMM);
            txtHelper.DrawTextLine("Est Head Dist (mm): " + headDist * screenHeightinMM);
            txtHelper.DrawTextLine("Camera Vert Angle (rad): " + cameraVerticaleAngle);
            if (cameraIsAboveScreen)
                txtHelper.DrawTextLine("Camera Position: Above Screen");
            else
                txtHelper.DrawTextLine("Camera Position: Below Screen");
            txtHelper.DrawTextLine("Screen Height (mm) : " + screenHeightinMM);
            txtHelper.DrawTextLine("IR Dot Width (mm) : " + dotDistanceInMM);
            txtHelper.DrawTextLine("");

            txtHelper.DrawTextLine("Controls -----");
            txtHelper.DrawTextLine("Space - calibrate camera angle/center view");
            txtHelper.DrawTextLine("R - Reposition the targets");
            txtHelper.DrawTextLine("C - Toggle camera position above/below screen");
            txtHelper.DrawTextLine("esc - Quit");
            txtHelper.DrawTextLine("");

            txtHelper.DrawTextLine("Show--------");
            txtHelper.DrawTextLine("T - Targets");
            txtHelper.DrawTextLine("L - Lines");
            txtHelper.DrawTextLine("B - Background");
            txtHelper.DrawTextLine("M - Mouse Cursor");
            txtHelper.DrawTextLine("G - Grid");
            txtHelper.DrawTextLine("H - Help Text");
            txtHelper.DrawTextLine("");

            txtHelper.End();
        }

        public bool InitializeGraphics()
        {
            try
            {
                FormBorderStyle = FormBorderStyle.None; //this is the bug that kept thing crashing C# on vista

                AdapterInformation ai = Manager.Adapters.Default;
                Caps caps = Manager.GetDeviceCaps(ai.Adapter, DeviceType.Hardware);

                Cursor.Hide();
                presentParams.Windowed = !doFullscreen;
                presentParams.SwapEffect = SwapEffect.Discard; // Discard the frames 
                presentParams.EnableAutoDepthStencil = true; // Turn on a Depth stencil
                presentParams.AutoDepthStencilFormat = DepthFormat.D16; // And the stencil format

                presentParams.BackBufferWidth = m_dwWidth; //screen width
                presentParams.BackBufferHeight = m_dwHeight; //screen height
                presentParams.BackBufferFormat = Format.R5G6B5; //color depth
                presentParams.MultiSample = MultiSampleType.None; //anti-aliasing
                presentParams.PresentationInterval = PresentInterval.Immediate; //don't wait... draw right away

                device = new Device(0, DeviceType.Hardware, this, CreateFlags.SoftwareVertexProcessing,
                    presentParams); //Create a device
                device.DeviceReset += new EventHandler(OnResetDevice);
                this.OnCreateDevice(device, null);
                this.OnResetDevice(device, null);
                return true;
            }
            catch (DirectXException)
            {
                // Catch any errors and return a failure
                return false;
            }
        }


        public void InitTargets()
        {
            if (targetPositions == null)
                targetPositions = new Vector3[numTargets];
            if (targetSizes == null)
                targetSizes = new Vector3[numTargets];
            var depthStep = boxdepth / 2.0f / numTargets;
            var startDepth = numInFront * depthStep;
            for (var i = 0; i < numTargets; i++)
            {
                targetPositions[i] = new Vector3(.7f * screenAspect * (random.Next(1000) / 1000.0f - .5f),
                    .7f * (random.Next(1000) / 1000.0f - .5f),
                    startDepth - i * depthStep);
                if (i < numInFront
                ) //pull in the ones out in front of the display closer the center so they stay in frame
                {
                    targetPositions[i].X *= .5f;
                    targetPositions[i].Y *= .5f;
                }

                targetSizes[i] = new Vector3(targetScale, targetScale, targetScale);
            }
        }

        public void CreateGridGeometry(Device dev)
        {
            var step = m_dwWidth / numGridlines;
            gridBuffer = new VertexBuffer(typeof(CustomVertex.PositionColored), 4 * (numGridlines + 2), dev, 0,
                CustomVertex.PositionColored.Format, Pool.Default);

            CustomVertex.PositionColored[] verts2;
            verts2 = (CustomVertex.PositionColored[]) gridBuffer.Lock(0,
                0); // Lock the buffer (which will return our structs)
            var vertIndex = 0;
            for (var i = 0; i <= numGridlines * 2; i += 2)
            {
                verts2[vertIndex].Position = new Vector3(i * step / 2.0f / m_dwWidth, 0.0f, 0.0f);
                verts2[vertIndex].Color = gridColor;
                vertIndex++;
                verts2[vertIndex].Position = new Vector3(i * step / 2.0f / m_dwWidth, 1.0f, 0.0f);
                verts2[vertIndex].Color = gridColor;
                vertIndex++;
            }

            for (var i = 0; i <= numGridlines * 2; i += 2)
            {
                verts2[vertIndex].Position = new Vector3(0.0f, i * step / 2.0f / m_dwWidth, 0.0f);
                verts2[vertIndex].Color = gridColor;
                vertIndex++;
                verts2[vertIndex].Position = new Vector3(1.0f, i * step / 2.0f / m_dwWidth, 0.0f);
                verts2[vertIndex].Color = gridColor;
                vertIndex++;
            }

            gridBuffer.Unlock();
        }

        private void LoadTexture()
        {
            try
            {
                texture = TextureLoader.FromFile(device, textureFilename);
            }
            catch
            {
                // We must be running from within Visual Studio. Relocate the 
                // current directory and try again.
                Directory.SetCurrentDirectory(
                    Application.StartupPath + @"\..\..\");

                texture = TextureLoader.FromFile(device, textureFilename);
            }

            device.SamplerState[0].MinFilter = TextureFilter.Linear;
            device.SamplerState[0].MagFilter = TextureFilter.Linear;
        }

        private void LoadBackground()
        {
            try
            {
                backgroundtexture = TextureLoader.FromFile(device, backgroundFilename);
            }
            catch
            {
                // We must be running from within Visual Studio. Relocate the 
                // current directory and try again.
                Directory.SetCurrentDirectory(
                    Application.StartupPath + @"\..\..\");

                backgroundtexture = TextureLoader.FromFile(device, backgroundFilename);
            }

            device.SamplerState[0].MinFilter = TextureFilter.Linear;
            device.SamplerState[0].MagFilter = TextureFilter.Linear;
        }

        public void CreateTargetGeometry(Device dev)
        {
            targetBuffer = new VertexBuffer(typeof(Vertex),
                targetVertices.Length, dev,
                Usage.Dynamic | Usage.WriteOnly,
                Vertex.FVF_Flags,
                Pool.Default);

            GraphicsStream gStream = targetBuffer.Lock(0, 0, LockFlags.None);

            // Now, copy the vertex data into the vertex buffer
            gStream.Write(targetVertices);
            targetBuffer.Unlock();


            lineBuffer = new VertexBuffer(typeof(CustomVertex.PositionColored),
                2,
                dev,
                Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionColored.Format,
                Pool.Default);

            CustomVertex.PositionColored[] verts;
            verts = (CustomVertex.PositionColored[]) lineBuffer.Lock(0,
                0); // Lock the buffer (which will return our structs)
            verts[0].Position = new Vector3(0.0f, 0.0f, 0.0f);
            verts[0].Color = lineColor;

            verts[1].Position = new Vector3(0.0f, 0.0f, lineDepth);
            verts[1].Color = lineColor;

            lineBuffer.Unlock();
        }

        public void CreateBackgroundGeometry(Device dev)
        {
            backgroundBuffer = new VertexBuffer(typeof(CustomVertex.PositionTextured), 2 * (backgroundStepCount + 1),
                dev, 0, CustomVertex.PositionTextured.Format, Pool.Default);

            CustomVertex.PositionTextured[] verts;
            verts = (CustomVertex.PositionTextured[]) backgroundBuffer.Lock(0,
                0); // Lock the buffer (which will return our structs)
            var angleStep = (float) (Math.PI / backgroundStepCount);
            for (var i = 0; i <= backgroundStepCount; i++)
            {
                verts[2 * i].Position =
                    new Vector3((float) Math.Cos(angleStep * i), -1, -(float) Math.Sin(angleStep * i));
                verts[2 * i].Tu = i / (float) backgroundStepCount;
                verts[2 * i].Tv = 1;

                verts[2 * i + 1].Position =
                    new Vector3((float) Math.Cos(angleStep * i), 1, -(float) Math.Sin(angleStep * i));
                verts[2 * i + 1].Tu = i / (float) backgroundStepCount;
                verts[2 * i + 1].Tv = 0;
            }

            backgroundBuffer.Unlock();
        }

        public void OnCreateVertexBuffer(object sender, EventArgs e)
        {
            VertexBuffer vb = (VertexBuffer) sender;
            // Create a vertex buffer (100 customervertex)
            CustomVertex.PositionNormalTextured[]
                verts = (CustomVertex.PositionNormalTextured[]) vb.Lock(0,
                    0); // Lock the buffer (which will return our structs)
            for (var i = 0; i < 50; i++)
            {
                // Fill up our structs
                var theta = (float) (2 * Math.PI * i) / 49;
                verts[2 * i].Position = new Vector3((float) Math.Sin(theta), -1, (float) Math.Cos(theta));
                verts[2 * i].Normal = new Vector3((float) Math.Sin(theta), 0, (float) Math.Cos(theta));
                verts[2 * i].Tu = (float) i / (50 - 1);
                verts[2 * i].Tv = 1.0f;
                verts[2 * i + 1].Position = new Vector3((float) Math.Sin(theta), 1, (float) Math.Cos(theta));
                verts[2 * i + 1].Normal = new Vector3((float) Math.Sin(theta), 0, (float) Math.Cos(theta));
                verts[2 * i + 1].Tu = (float) i / (50 - 1);
                verts[2 * i + 1].Tv = 0.0f;
            }

            // Unlock (and copy) the data
            vb.Unlock();
        }

        public void OnCreateDevice(object sender, EventArgs e)
        {
            Device dev = (Device) sender;
            textSprite = new Sprite(dev);
            statsFont = ResourceCache.GetGlobalInstance().CreateFont(dev, 15, 0, FontWeight.Bold, 1, false,
                CharacterSet.Default, Precision.Default, FontQuality.Default,
                PitchAndFamily.FamilyDoNotCare | PitchAndFamily.DefaultPitch, "Arial");

            //init cursors
            mouseCursor = new CrosshairCursor(dev, 0x00ff00, .04f);
            wiiCursor1 = new CrosshairCursor(dev, 0x00ff00, .04f);
            wiiCursor2 = new CrosshairCursor(dev, 0x0000ff, .04f);
            wiiCursor3 = new CrosshairCursor(dev, 0xff0000, .04f);
            wiiCursor4 = new CrosshairCursor(dev, 0xffff00, .04f);

            CreateGridGeometry(dev);
            CreateBackgroundGeometry(dev);
            CreateTargetGeometry(dev);
            InitTargets();
            LoadTexture();
            LoadBackground();

            if (doWiimote)
                try
                {
                    remote = new Wiimote();
                    remote.Connect();
                    remote.SetReportType(InputReport.IRAccel, true);
                    remote.SetLEDs(true, false, false, false);
                    remote.WiimoteChanged += wm_OnWiimoteChanged;
                }
                catch (Exception x)
                {
                    MessageBox.Show("Cannot find a wii remote: " + x.Message);
                    doWiimote = false;
                }
        }

        private void wm_OnWiimoteChanged(object sender, WiimoteChangedEventArgs args)
        {
            ParseWiimoteData();
            if (isReady)
                Render(); //wiimote triggered wiimote thread
        }

        public void ParseWiimoteData()
        {
            if (remote.WiimoteState == null)
                return;

            var firstPoint = new Point2D();
            var secondPoint = new Point2D();
            var numvisible = 0;

            if (remote.WiimoteState.IRState.IRSensors[0].Found)
            {
                wiimotePointsNormalized[0].x = 1.0f - remote.WiimoteState.IRState.IRSensors[0].RawPosition.X / 768.0f;
                wiimotePointsNormalized[0].y = remote.WiimoteState.IRState.IRSensors[0].RawPosition.Y / 768.0f;
                wiiCursor1.isDown = true;
                firstPoint.x = remote.WiimoteState.IRState.IRSensors[0].RawPosition.X;
                firstPoint.y = remote.WiimoteState.IRState.IRSensors[0].RawPosition.Y;
                numvisible = 1;
            }
            else
            {
                //not visible
                wiiCursor1.isDown = false;
            }

            if (remote.WiimoteState.IRState.IRSensors[1].Found)
            {
                wiimotePointsNormalized[1].x = 1.0f - remote.WiimoteState.IRState.IRSensors[1].RawPosition.X / 768.0f;
                wiimotePointsNormalized[1].y = remote.WiimoteState.IRState.IRSensors[1].RawPosition.Y / 768.0f;
                wiiCursor2.isDown = true;
                if (numvisible == 0)
                {
                    firstPoint.x = remote.WiimoteState.IRState.IRSensors[1].RawPosition.X;
                    firstPoint.y = remote.WiimoteState.IRState.IRSensors[1].RawPosition.Y;
                    numvisible = 1;
                }
                else
                {
                    secondPoint.x = remote.WiimoteState.IRState.IRSensors[1].RawPosition.X;
                    secondPoint.y = remote.WiimoteState.IRState.IRSensors[1].RawPosition.Y;
                    numvisible = 2;
                }
            }
            else
            {
                //not visible
                wiiCursor2.isDown = false;
            }

            if (remote.WiimoteState.IRState.IRSensors[2].Found)
            {
                wiimotePointsNormalized[2].x = 1.0f - remote.WiimoteState.IRState.IRSensors[2].RawPosition.X / 768.0f;
                wiimotePointsNormalized[2].y = remote.WiimoteState.IRState.IRSensors[2].RawPosition.Y / 768.0f;
                wiiCursor3.isDown = true;
                if (numvisible == 0)
                {
                    firstPoint.x = remote.WiimoteState.IRState.IRSensors[2].RawPosition.X;
                    firstPoint.y = remote.WiimoteState.IRState.IRSensors[2].RawPosition.Y;
                    numvisible = 1;
                }
                else if (numvisible == 1)
                {
                    secondPoint.x = remote.WiimoteState.IRState.IRSensors[2].RawPosition.X;
                    secondPoint.y = remote.WiimoteState.IRState.IRSensors[2].RawPosition.Y;
                    numvisible = 2;
                }
            }
            else
            {
                //not visible
                wiiCursor3.isDown = false;
            }

            if (remote.WiimoteState.IRState.IRSensors[3].Found)
            {
                wiimotePointsNormalized[3].x = 1.0f - remote.WiimoteState.IRState.IRSensors[3].RawPosition.X / 768.0f;
                wiimotePointsNormalized[3].y = remote.WiimoteState.IRState.IRSensors[3].RawPosition.Y / 768.0f;
                wiiCursor4.isDown = true;
                if (numvisible == 1)
                {
                    secondPoint.x = remote.WiimoteState.IRState.IRSensors[3].RawPosition.X;
                    secondPoint.y = remote.WiimoteState.IRState.IRSensors[3].RawPosition.Y;
                    numvisible = 2;
                }
            }
            else
            {
                //not visible
                wiiCursor4.isDown = false;
            }

            if (numvisible == 2)
            {
                var dx = firstPoint.x - secondPoint.x;
                var dy = firstPoint.y - secondPoint.y;
                var pointDist = (float) Math.Sqrt(dx * dx + dy * dy);

                var angle = radiansPerPixel * pointDist / 2;
                //in units of screen hieght since the box is a unit cube and box hieght is 1
                headDist = movementScaling * (float) (dotDistanceInMM / 2 / Math.Tan(angle)) / screenHeightinMM;


                var avgX = (firstPoint.x + secondPoint.x) / 2.0f;
                var avgY = (firstPoint.y + secondPoint.y) / 2.0f;


                //should  calaculate based on distance

                headX = (float) (movementScaling * Math.Sin(radiansPerPixel * (avgX - 512)) * headDist);

                relativeVerticalAngle = (avgY - 384) * radiansPerPixel; //relative angle to camera axis

                if (cameraIsAboveScreen)
                    headY = .5f + (float) (movementScaling * Math.Sin(relativeVerticalAngle + cameraVerticaleAngle) *
                                           headDist);
                else
                    headY = -.5f + (float) (movementScaling * Math.Sin(relativeVerticalAngle + cameraVerticaleAngle) *
                                            headDist);
            }


            //position the graphical cursors at the 3rd and 4th ir points if they exist
            if (wiiCursor1.isDown)
                wiiCursor1.setDown(wiimotePointsNormalized[0].x, wiimotePointsNormalized[0].y);
            if (wiiCursor2.isDown)
                wiiCursor2.setDown(wiimotePointsNormalized[1].x, wiimotePointsNormalized[1].y);
            if (wiiCursor3.isDown)
                wiiCursor3.setDown(wiimotePointsNormalized[2].x, wiimotePointsNormalized[2].y);
            if (wiiCursor4.isDown)
                wiiCursor4.setDown(wiimotePointsNormalized[3].x, wiimotePointsNormalized[3].y);


            wiiCursor1.wasDown = wiiCursor1.isDown;
            wiiCursor2.wasDown = wiiCursor2.isDown;
            wiiCursor3.wasDown = wiiCursor3.isDown;
            wiiCursor4.wasDown = wiiCursor4.isDown;
        }

        public void OnResetDevice(object sender, EventArgs e)
        {
            Device dev = (Device) sender;
            // Turn off culling, so we see the front and back of the triangle
            dev.RenderState.CullMode = Cull.None;
            // Turn off D3D lighting
            dev.RenderState.Lighting = false;
            // Turn on the ZBuffer
            dev.RenderState.ZBufferEnable = true;
        }

        private void SetupMatrices()
        {
            device.Transform.World = Matrix.Identity;

            // Set up our view matrix. A view matrix can be defined given an eye point,
            // a point to lookat, and a direction for which way is up. Here, we set the
            // eye five units back along the z-axis and up three units, look at the
            // origin, and define "up" to be in the y-direction.
//            device.Transform.View = Matrix.LookAtLH(new Vector3(mouseCursor.X, mouseCursor.Y, -5.0f), new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f));
            device.Transform.View = Matrix.LookAtLH(new Vector3(headX, headY, headDist), new Vector3(headX, headY, 0),
                new Vector3(0.0f, 1.0f, 0.0f));

            // For the projection matrix, we set up a perspective transform (which
            // transforms geometry from 3D view space to 2D viewport space, with
            // a perspective divide making objects smaller in the distance). To build
            // a perpsective transform, we need the field of view (1/4 pi is common),
            // the aspect ratio, and the near and far clipping planes (which define at
            // what distances geometry should be no longer be rendered).

            //compute the near plane so that the camera stays fixed to -.5f*screenAspect, .5f*screenAspect, -.5f,.5f
            //compting a closer plane rather than simply specifying xmin,xmax,ymin,ymax allows things to float in front of the display
            var nearPlane = .05f;
            device.Transform.Projection = Matrix.PerspectiveOffCenterLH(
                nearPlane * (-.5f * screenAspect + headX) / headDist,
                nearPlane * (.5f * screenAspect + headX) / headDist,
                nearPlane * (-.5f - headY) / headDist,
                nearPlane * (.5f - headY) / headDist,
                nearPlane, 100);
        }

        private void Render()
        {
            if (isRendering)
                return;
            isRendering = true;

            if (device == null)
                return;
            //Clear the backbuffer to a blue color 
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            //Begin the scene
            device.BeginScene();
            // Setup the world, view, and projection matrices

            SetupMatrices();

            device.RenderState.FogColor = Color.Black;
            device.RenderState.FogStart = headDist;
            device.RenderState.FogEnd = headDist + fogdepth;
            device.RenderState.FogVertexMode = FogMode.Linear;
            device.RenderState.FogEnable = true;

            if (doWiimote)
            {
                if (doWiiCursors)
                {
                    //draw the cursors
//                    if (wiiCursor1.isDown)
                    //                      wiiCursor1.Render(device);
                    //                if (wiiCursor2.isDown)
                    //                  wiiCursor2.Render(device);
                    //            if (wiiCursor3.isDown)
                    //              wiiCursor3.Render(device);
                    //        if (wiiCursor4.isDown)
                    //          wiiCursor4.Render(device);
                }

                device.Transform.World = worldTransform;
            }

            if (showGrid)
            {
                device.TextureState[0].ColorOperation = TextureOperation.Disable;

                device.RenderState.AlphaBlendEnable = false;
                device.RenderState.AlphaTestEnable = false;

                //back
                device.Transform.World = Matrix.Translation(new Vector3(-.5f, -.5f, -1 * boxdepth / 2));
                device.Transform.World *= Matrix.Scaling(new Vector3(screenAspect, 1, 1));
                device.SetStreamSource(0, gridBuffer, 0);
                device.VertexFormat = CustomVertex.PositionColored.Format;
                device.DrawPrimitives(PrimitiveType.LineList, 0, 2 * (numGridlines + 2));

                //left and right
                device.Transform.World = Matrix.Translation(new Vector3(-.5f, -.5f, 0));
                device.Transform.World *= Matrix.Scaling(new Vector3(1 * boxdepth / 2, 1, 1));
                device.Transform.World *= Matrix.RotationY((float) (Math.PI / 2));
                device.Transform.World *= Matrix.Translation(new Vector3(0.5f * screenAspect, 0, -.5f * boxdepth / 2));
                device.DrawPrimitives(PrimitiveType.LineList, 0, 2 * (numGridlines + 2));
                device.Transform.World *= Matrix.Translation(new Vector3(-1.0f * screenAspect, 0, 0));
                device.DrawPrimitives(PrimitiveType.LineList, 0, 2 * (numGridlines + 2));


                //floor and ceiling
                device.Transform.World = Matrix.Translation(new Vector3(-.5f, -.5f, 0));
                device.Transform.World *= Matrix.Scaling(new Vector3(screenAspect, 1 * boxdepth / 2, 1));
                device.Transform.World *= Matrix.RotationX((float) (Math.PI / 2));
                device.Transform.World *= Matrix.Translation(new Vector3(0, 0.5f, -.5f * boxdepth / 2));
                device.DrawPrimitives(PrimitiveType.LineList, 0, 2 * (numGridlines + 2));
                device.Transform.World *= Matrix.Translation(new Vector3(0, -1.0f, 0));
                device.DrawPrimitives(PrimitiveType.LineList, 0, 2 * (numGridlines + 2));
            }

            if (showTargets) //draw targets
            {
                device.SetTexture(0, texture);
                //Render States
                device.RenderState.AlphaBlendEnable = true;
                device.RenderState.AlphaFunction = Compare.Greater;
                device.RenderState.AlphaTestEnable = true;
                device.RenderState.DestinationBlend = Blend.InvSourceAlpha;
                device.RenderState.SourceBlend = Blend.SourceAlpha;
                device.RenderState.DiffuseMaterialSource = ColorSource.Material;

                //Color blending ops
                device.TextureState[0].ColorOperation = TextureOperation.Modulate;
                device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
                device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;

                //set the first alpha stage to texture alpha
                device.TextureState[0].AlphaOperation = TextureOperation.SelectArg1;
                device.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;


                device.VertexFormat = Vertex.FVF_Flags;
                device.SetStreamSource(0, targetBuffer, 0);
                for (var i = 0; i < numTargets; i++)
                {
                    device.Transform.World = Matrix.Scaling(targetSizes[i]);
                    device.Transform.World *= Matrix.Translation(targetPositions[i]);
                    device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
                }
            }

            if (showLines)
            {
                device.VertexFormat = CustomVertex.PositionColored.Format;
                device.SetStreamSource(0, lineBuffer, 0);
                device.TextureState[0].ColorOperation = TextureOperation.Disable;
                device.RenderState.AlphaBlendEnable = false;
                device.RenderState.AlphaTestEnable = false;
                for (var i = 0; i < numTargets; i++)
                {
                    device.Transform.World = Matrix.Scaling(targetSizes[i]);
                    device.Transform.World *= Matrix.Translation(targetPositions[i]);
                    device.DrawPrimitives(PrimitiveType.LineList, 0, 1);
                }
            }


            if (showBackground)
            {
                device.RenderState.FogEnable = false;
                device.Transform.World = Matrix.Scaling(new Vector3(3, 2, 3));
                device.SetTexture(0, backgroundtexture);
                //Render States
                device.RenderState.AlphaBlendEnable = false;
                device.RenderState.AlphaTestEnable = false;
                device.TextureState[0].ColorOperation = TextureOperation.Modulate;


                device.VertexFormat = CustomVertex.PositionTextured.Format;

                device.SetStreamSource(0, backgroundBuffer, 0);
                device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, backgroundStepCount * 2);
            }

            if (showMouseCursor)
            {
                device.TextureState[0].ColorOperation = TextureOperation.Disable;
                device.RenderState.AlphaBlendEnable = false;
                device.RenderState.AlphaTestEnable = false;

                device.Transform.World = Matrix.Identity;
                mouseCursor.Render(device);
            }

            if (showHelp)
                RenderText();

            //End the scene
            device.EndScene();

            // Update the screen
            device.Present();
            isRendering = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            isReady = true; //rendering triggered by wiimote is waiting for this.
        }

        protected override void OnResize(EventArgs e)
        {
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            using (var frm = new WiiDesktopVR())
            {
                if (!frm.InitializeGraphics()) // Initialize Direct3D
                {
                    MessageBox.Show("Could not initialize Direct3D.  This tutorial will exit.");
                    return;
                }

                frm.Show();

                // While the form is still valid, render and process messages
                while (frm.Created)
                {
                    Application.DoEvents();
                    if (!frm.doWiimote)
                        frm.Render();
                }

                Cursor.Show();
            }
        }

        private struct Vertex
        {
            private float x, y, z;
            private float tu, tv;

            public Vertex(float _x, float _y, float _z, float _tu, float _tv)
            {
                x = _x;
                y = _y;
                z = _z;
                tu = _tu;
                tv = _tv;
            }

            public static readonly VertexFormats FVF_Flags = VertexFormats.Position | VertexFormats.Texture1;
        }
    }
}