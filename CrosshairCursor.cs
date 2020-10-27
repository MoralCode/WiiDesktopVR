using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;


namespace WiiDesktopVR
{
    class CrosshairCursor
    {
        public float lastX = 0;
        public float lastY = 0;
        public bool wasDown = false;
        public bool isDown = false;

        public float X = 0;
        public float Y = 0;
        
        VertexBuffer cursorBuffer = null;
        int circleSegments = 10;
        public CrosshairCursor(Device dev, int color, float size)
        {

            cursorBuffer = new VertexBuffer(typeof(CustomVertex.PositionColored), 4 + 6 * circleSegments, dev, 0, CustomVertex.PositionColored.Format, Pool.Managed);

            CustomVertex.PositionColored[] verts;
            verts = (CustomVertex.PositionColored[])cursorBuffer.Lock(0, 0); // Lock the buffer (which will return our structs)
            verts[0].Position = new Vector3(-size, 0.0f, 0.0f);
            verts[0].Color = color;
            verts[1].Position = new Vector3(size, 0.0f, 0.0f);
            verts[1].Color = color;
            verts[2].Position = new Vector3(0.0f, -size, 0.0f);
            verts[2].Color = color;
            verts[3].Position = new Vector3(0.0f, size, 0.0f);
            verts[3].Color = color;
            int vertCounter = 4;
            float scale = .5f;
            for (int i = 0; i < circleSegments; i++)
            {
                verts[vertCounter].Position = new Vector3(size * scale * (float)Math.Cos(i * 2 * Math.PI / circleSegments), size * scale * (float)Math.Sin(i * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
                verts[vertCounter].Position = new Vector3(size * scale * (float)Math.Cos((i + 1) * 2 * Math.PI / circleSegments), size * scale * (float)Math.Sin((i + 1) * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
            }
            scale /= 2;
            for (int i = 0; i < circleSegments; i++)
            {
                verts[vertCounter].Position = new Vector3(size * scale * (float)Math.Cos(i * 2 * Math.PI / circleSegments), size * scale * (float)Math.Sin(i * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
                verts[vertCounter].Position = new Vector3(size * scale * (float)Math.Cos((i + 1) * 2 * Math.PI / circleSegments), size * scale * (float)Math.Sin((i + 1) * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
            }
            scale *= 3;
            for (int i = 0; i < circleSegments; i++)
            {
                verts[vertCounter].Position = new Vector3(size * scale * (float)Math.Cos(i * 2 * Math.PI / circleSegments), size * scale * (float)Math.Sin(i * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
                verts[vertCounter].Position = new Vector3(size * scale * (float)Math.Cos((i + 1) * 2 * Math.PI / circleSegments), size * scale * (float)Math.Sin((i + 1) * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
            }

            cursorBuffer.Unlock();

        }

        public void Render(Device device)
        {
            device.Transform.World = Matrix.Translation(new Vector3(X, Y, 0.0f));
            device.SetStreamSource(0, cursorBuffer, 0);
            device.VertexFormat = CustomVertex.PositionColored.Format;
            device.DrawPrimitives(PrimitiveType.LineList, 0, 2 + 3*circleSegments);
        }

        public void set(float x, float y)
        {
            X = x;
            Y = y;
        }

        public void setUp(float x, float y)
        {
            isDown = false;
            set(x, y);

        }

        public void setDown(float x, float y)
        {
            isDown = true;
            lastX = x;
            lastY = y;
            set(x, y);
        }
    }
}
