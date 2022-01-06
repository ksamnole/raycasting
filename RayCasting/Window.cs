using System;
using System.Collections.Generic;
using System.Drawing;
using System.Timers;
using System.Windows;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace RayCasting
{
    public class Window : GameWindow
    {
        private float rotation = -1.0f;

        public Window(int width, int height) : base(width, height) { }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            base.OnResize(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            // Очищаем окно
            GL.ClearColor(OpenTK.Graphics.Color4.CornflowerBlue);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var cameraOrigin = new Vector3(0.0f, 0.0f, -1.5f);
            GL.Begin(PrimitiveType.Points);
            for (var x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var _x = (2 * x - Width) / (float)Width;
                    var _y = (2 * y - Height) / (float)Height;
                    var cameraDirection = Vector3.Normalize(new Vector3(_x, _y, 1.0f));
                    var col = CastRay(cameraOrigin, cameraDirection);
                    col.X = (float)Math.Pow(col.X, 0.45);
                    col.Y = (float)Math.Pow(col.Y, 0.45);
                    col.Z = (float)Math.Pow(col.Z, 0.45);
                    GL.Color3(col);
                    GL.Vertex3(_x, _y, 0);
                }
            GL.End();

            rotation += 0.5f;

            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }

        private Vector3 CastRay(Vector3 origin, Vector3 direction)
        {
            var spherePos = new Vector3(0.0f, 0.0f, 0.0f);
            var it = SphereIntersect(origin - spherePos, direction, 0.5f);
            if (it.X < 0.0)
                return new Vector3(0.3f, 0.6f, 1.0f);
            var itPos = origin + direction * it.X;
            var light = Vector3.Normalize(new Vector3((float)Math.Cos(rotation), 0.75f, -0.5f));
            var diffuse = Math.Max(0.0f, Vector3.Dot(light, itPos)) * 0.5f + 0.1f;
            return new Vector3(diffuse);
        }

        private float PlaneIntersect(Vector3 origin, Vector3 direction, Vector4 p)
        {
            return -(Vector3.Dot(origin, p.Xyz) + p.W) / Vector3.Dot(direction, p.Xyz);
        }

        private Vector2 SphereIntersect(Vector3 origin, Vector3 direction, float radius)
        {
            var b = Vector3.Dot(origin, direction);
            var c = Vector3.Dot(origin, origin) - radius * radius;
            var h = b * b - c;
            if (h < 0.0f)
                return new Vector2(-1.0f, 0.0f);
            h = (float)Math.Sqrt(h);
            return new Vector2(-b - h, -b + h);
        }
    }
}
