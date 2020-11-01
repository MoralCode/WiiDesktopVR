using System;
using SharpDX.Mathematics;
using SharpDX.Direct3D9;
using SharpDX;

namespace WiiDesktopVR
{
    internal class CrosshairCursor
    {
        private readonly int circleSegments = 10;

        private VertexBuffer cursorBuffer = null;
        public bool isDown;
        public float lastX;
        public float lastY;
        public bool wasDown = false;

        public float X;
        public float Y;

        public CrosshairCursor(Device dev, int color, float size)
        {
            cursorBuffer = new VertexBuffer(dev, 4 + 6 * circleSegments, typeof(CustomVertex.PositionColored), CustomVertex.PositionColored.Format, Pool.Managed);// 0,
      
            CustomVertex.PositionColored[] verts;
            verts = (CustomVertex.PositionColored[]) cursorBuffer.Lock(0,
                0, LockFlags.None); // Lock the buffer (which will return our structs)
            verts[0].Position = new Vector3(-size, 0.0f, 0.0f);
            verts[0].Color = color;
            verts[1].Position = new Vector3(size, 0.0f, 0.0f);
            verts[1].Color = color;
            verts[2].Position = new Vector3(0.0f, -size, 0.0f);
            verts[2].Color = color;
            verts[3].Position = new Vector3(0.0f, size, 0.0f);
            verts[3].Color = color;
            var vertCounter = 4;
            var scale = .5f;
            for (var i = 0; i < circleSegments; i++)
            {
                verts[vertCounter].Position =
                    new Vector3(size * scale * (float) Math.Cos(i * 2 * Math.PI / circleSegments),
                        size * scale * (float) Math.Sin(i * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
                verts[vertCounter].Position =
                    new Vector3(size * scale * (float) Math.Cos((i + 1) * 2 * Math.PI / circleSegments),
                        size * scale * (float) Math.Sin((i + 1) * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
            }

            scale /= 2;
            for (var i = 0; i < circleSegments; i++)
            {
                verts[vertCounter].Position =
                    new Vector3(size * scale * (float) Math.Cos(i * 2 * Math.PI / circleSegments),
                        size * scale * (float) Math.Sin(i * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
                verts[vertCounter].Position =
                    new Vector3(size * scale * (float) Math.Cos((i + 1) * 2 * Math.PI / circleSegments),
                        size * scale * (float) Math.Sin((i + 1) * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
            }

            scale *= 3;
            for (var i = 0; i < circleSegments; i++)
            {
                verts[vertCounter].Position =
                    new Vector3(size * scale * (float) Math.Cos(i * 2 * Math.PI / circleSegments),
                        size * scale * (float) Math.Sin(i * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
                verts[vertCounter].Position =
                    new Vector3(size * scale * (float) Math.Cos((i + 1) * 2 * Math.PI / circleSegments),
                        size * scale * (float) Math.Sin((i + 1) * 2 * Math.PI / circleSegments), 0.0f);
                verts[vertCounter].Color = color;
                vertCounter++;
            }

            cursorBuffer.Unlock();
        }

        public void Render(Device device)
        {
            device.SetTransform(TransformState.World, Matrix.Translation(new Vector3(X, Y, 0.0f)));
            device.SetStreamSource(0, cursorBuffer, 0);
            device.VertexFormat = CustomVertex.PositionColored.Format;
            device.DrawPrimitives(PrimitiveType.LineList, 0, 2 + 3 * circleSegments);
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