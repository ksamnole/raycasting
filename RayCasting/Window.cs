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

            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }

        private float PlateIntersect(Vector3 origin, Vector3 direction, Vector4 p)
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
