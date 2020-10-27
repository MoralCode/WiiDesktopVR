This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. Use of this software is entirely at your own risk. It is provided without any support beyond this document.  

The program was tested on Windows XP, with Service Pack 2, and .NET framework 2.0 installed.  You may have to download these to make the program run.

THIS PROGRAM IS PRIMARILY DISTRIBUTED TO INSTERESTED DEVELOPERS AS SAMPLE CODE.  IT IS NOT MEANT TO BE A ROBUST END-USER APPLICATION. MANY PARAMETERS ARE NOT MODIFIABLE WITHOUT PROGRAMMING KNOWLEDGE.


How to use 
-----------------------------------------------------
1. You MUST first connect your wiimote(s) to your PC via bluetooth before running  
the program.  You can follow this tutorial using the Blue Soleil Windows  
Driver: http://www.wiili.org/index.php/How_To:_BlueSoleil

Some users have reported this working with other bluetooth drivers. But, I have not tested it.

2.  The program needs to know a few parameters to calculate the correction projection.  In the "config.dat" file you can specify the vertical size of your display (in millimeters), the distance between your two dots (in millimeters, the wii sensor bar is 215mm), the aspect ratio of your screen (use 0 to auto calculate), if the camera is above screen (true) or below screen( false), and if you are connecting 2 wiimotes (true or false).  If you changed it, and it's not working.  Delete the file, and run the program.  It'll re-create a valid config.dat file with the default values.

3. Launch "WiiDesktopVR.exe" in the root directly of the archive. NOTE: The order in which the wiimotes or enumerated is somewhat magical to me, but consistent.  If you are try to connect to 2 wiimotes, but must be connected before running the program and the LEDs will indicate which is #1 and which is #2.  Headtracking is always done with #1.  #2 only move the green cursor.  No shooting or target destruction is implemented... sorry, no game.

4. position your head in the desired netural position, and press space.  This will calculte the verticle angle of the wiimote and establish vertical center.  Horzontal center is bound to the wiimote.  So, no correction is made there... but could if you change the code.


Controls
---------------------------------------
1. Space - to center your view and calibrate wiimote angle.
2. R - reposition the targets
3. C - toggle camera position from above/below screen
3. Esc - Exit

Changing the Config File
-----------------------------------------


TROUBLE SHOOTING
--------------------------------------------
"The program says it can't find the wiimote" - Check that you have gotten the  
wiimote already connected via bluetooth.  The WiimoteWhiteboard program does not do this for you.  Follow the instructions at: http://www.wiili.org/index.php/How_To:_BlueSoleil  
Some users have said it works with other bluetooth drivers.  But, I have not tested it.


DEVELOPER NOTES
----------------------------------
The interesting part of this code is the calculation of the offcenter projection.


//when space is pressed, the camera angle is calculated in OnKeyPress------------------------
if ((int)(byte)e.KeyCode == (int)Keys.Space)
{
//zeros the head position and computes the camera tilt
double angle = Math.Acos(.5 / headDist)-Math.PI / 2;//angle of head to screen
if (!cameraIsAboveScreen)
    angle  = -angle;
cameraVerticaleAngle = (float)((angle-relativeVerticalAngle));//absolute camera angle 
}


//here all the head parameters are calculated in ParseWiimoteData()------------------------------
float dx = firstPoint.x - secondPoint.x;
float dy = firstPoint.y - secondPoint.y;
float pointDist = (float)Math.Sqrt(dx * dx + dy * dy);

float angle = radiansPerPixel * pointDist / 2;
//in units of screen hieght since the box is a unit cube and box hieght is 1
headDist = movementScaling * (float)((dotDistanceInMM / 2) / Math.Tan(angle)) / screenHeightinMM;

float avgX = (firstPoint.x + secondPoint.x) / 2.0f;
float avgY = (firstPoint.y + secondPoint.y) / 2.0f;

headX = (float)(movementScaling *  Math.Sin(radiansPerPixel * (avgX - 512)) * headDist);

relativeVerticalAngle = (avgY - 384) * radiansPerPixel;//relative angle to camera axis

if(cameraIsAboveScreen)
    headY = .5f+(float)(movementScaling * Math.Sin(relativeVerticalAngle + cameraVerticaleAngle)  *headDist);
else
    headY = -.5f + (float)(movementScaling * Math.Sin(relativeVerticalAngle + cameraVerticaleAngle) * headDist);




//here is the projection in SetupMatrics()------------------------------------------
float nearPlane = .05f;
device.Transform.Projection = Matrix.PerspectiveOffCenterLH(    nearPlane*(-.5f * screenAspect + headX)/headDist, 
							    nearPlane*(.5f * screenAspect + headX)/headDist, 
							    nearPlane*(-.5f - headY)/headDist, 
							    nearPlane*(.5f - headY)/headDist, 
							    nearPlane, 100);


