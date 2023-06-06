# WiiDesktopVR

This is effectively a fork of Johnny Chung Lee's WiiDesktopVR Wii Remote head tracking code.

Since the [original code](http://johnnylee.net/projects/wii/) was originally uploaded without source control, the first commits to this repository were for checking in the original code into source control.


## Planned Improvements

My intention with this repository was to adapt the original wiimote head tracking code into something that could be passed as a controller/headset to Steam/[Proton](https://www.protondb.com/) on Linux for playing Beat Saber.

Because this code was originally posted in the late 2000's and was written using DirectX for Windows, there is currently work being done (in the `cross-platform` branch) to convert the directx-dependent code to the no-longer-developed SharpDX (which should allow it to be run on linux and then subsequently converted to something currently supported and cross-platform like OpenGL. not sure though as I havent gotten that far yet.)


I have already converted most of the stuff that is relatively easy and amounts to effectively just API changes from DirectX to sharpDX (things like method parameters being rearranged, rectangles being changed to x,y,width, height rather than position and size etc. see my commits on the  `cross-platform` branch for details)


### other versions
- A potentially more stable/Vista/64-bit compatible version has been created by Andrea Leganza (updated/not broken link): https://www.leganza.it/2008/03/09/wiidesktopvr-wiimotelib-1-2-1-visual-c-2008-express-x64-env-works-also-for-x86/
