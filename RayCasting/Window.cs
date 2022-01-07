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
            // Пускаем лучи в каждый пиксель экрана
            for (var x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    // Берем координаты в нашем окне. Окно от -1 до 1, в то время как пиксели от 0 до ???
                    var _x = (2 * x - Width) / (float)Width;
                    var _y = (2 * y - Height) / (float)Height;
                    // Направление камеры
                    var cameraDirection = Vector3.Normalize(new Vector3(_x, _y, 1.0f));
                    // Цвет пикселя. Если попал в объект, то цвет объекта, иначе цвет неба.
                    var col = CastRay(cameraOrigin, cameraDirection);
                    // Цвета будут более реалистичные
                    col.X = (float)Math.Pow(col.X, 0.45);
                    col.Y = (float)Math.Pow(col.Y, 0.45);
                    col.Z = (float)Math.Pow(col.Z, 0.45);
                    // Красим пиксель
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
            var minIntersect = new Vector2(float.MaxValue);
            var color = new Vector3(0.5f, 0.5f, 1.0f);
            Vector3 n = Vector3.Zero;
            // Пересечение с 1 сферой
            var sphere1Pos = new Vector3(0.5f, 0.0f, 0.0f);
            var intersect1 = SphereIntersect(origin - sphere1Pos, direction, 0.5f);
            if (intersect1.X > 0.0f && intersect1.X < minIntersect.X)
            {
                minIntersect = intersect1;
                var itPos = origin + direction * intersect1.X;
                n = itPos - sphere1Pos;
                color = new Vector3(1.0f, 0.2f, 0.1f);
            }
            // Пересечение со 2 сферой
            var sphere2Pos = new Vector3(-0.75f, 0.0f, 0.5f);
            var intersect2 = SphereIntersect(origin - sphere2Pos, direction, 0.5f);
            if (intersect2.X > 0.0f && intersect2.X < minIntersect.X)
            {
                minIntersect = intersect2;
                var itPos = origin + direction * intersect2.X;
                n = itPos - sphere2Pos;
                color = new Vector3(255f / 255, 165f / 255, 201f / 255);
            }
            var plateNormal = new Vector3(0.0f, 0.5f, 0.0f);
            var intersect3 = new Vector2(PlaneIntersect(origin, direction, new Vector4(plateNormal, 1.0f)));
            if (intersect3.X > 0.0f && intersect3.X < minIntersect.X)
            {
                minIntersect = intersect3;
                n = plateNormal;
                color = new Vector3(0.5f, 0.5f, 1.0f);
            }
            // Проверка попал ли луч в объект, если нет то возвращаем цвет неба.
            if (minIntersect.X == float.MaxValue)
                return new Vector3(0.3f, 0.6f, 1.0f);
            // Направление освещения
            var light = Vector3.Normalize(new Vector3((float)Math.Cos(rotation), 1f, -0.75f));
            // Расчет освещения
            var diffuse = Math.Max(0.0f, Vector3.Dot(light, n)) * 0.5f + 0.1f;
            // Добавляем отражение
            var reflected = direction - 2.0f * Vector3.Dot(direction, n) * n;
            var specular = Math.Max(0.0f, Vector3.Dot(reflected, light));
            color *= new Vector3(diffuse + (float)specular);
            return color;
        }

        private float PlaneIntersect(Vector3 origin, Vector3 direction, Vector4 p)
        {
            // Метод пересечения луча с плоскостью
            return -(Vector3.Dot(origin, p.Xyz) + p.W) / Vector3.Dot(direction, p.Xyz);
        }

        private Vector2 SphereIntersect(Vector3 origin, Vector3 direction, float radius)
        {
            // Метод пересечения луча со сферой
            var b = Vector3.Dot(origin, direction);
            var c = Vector3.Dot(origin, origin) - radius * radius;
            var h = b * b - c;
            if (h < 0.0f)
                return new Vector2(-1.0f, 0.0f);
            h = (float)Math.Sqrt(h);
            return new Vector2(-b - h, -b + h);
        }

        private float gouIntersect(Vector3 ro, Vector3 rd, float ka, float kb)
        {
            var po = 1.0f;
            var rd2 = ro * ro;
            var rd3 = rd2 * rd;
            var ro2 = ro * ro;
            var ro3 = ro2 * ro;
            var k4 = Vector3.Dot(rd2, rd2);
            var k3 = Vector3.Dot(ro, rd3);
            var k2 = Vector3.Dot(ro2, rd2) - kb / 6.0f;
            var k1 = Vector3.Dot(ro3, rd) - kb * Vector3.Dot(rd, ro) / 2.0f;
            var k0 = Vector3.Dot(ro2, ro2) + ka - kb * Vector3.Dot(ro, ro);
            k3 /= k4;
            k2 /= k4;
            k1 /= k4;
            k0 /= k4;
            var c2 = k2 - k3 * k3;
            var c1 = k1 + k3 * (2.0f * k3 * k3 - 3.0f * k2);
            var c0 = k0 + k3 * (k3 * (c2 + k2) * 3.0f - 4.0f * k1);

            if (Math.Abs(c1) < 0.1f * Math.Abs(c2))
            {
                po = -1.0f;
                var tmp = k1;
                k1 = k3;
                k3 = tmp;
                k0 = 1.0f / k0;
                k1 = k1 * k0;
                k2 = k2 * k0;
                k3 = k3 * k0;
                c2 = k2 - k3 * (k3);
                c1 = k1 + k3 * (2.0f * k3 * k3 - 3.0f * k2);
                c0 = k0 + k3 * (k3 * (c2 + k2) * 3.0f - 4.0f * k1);
            }

            c0 /= 3.0f;
            var Q = c2 * c2 + c0;
            var R = c2 * c2 * c2 - 3.0f * c0 * c2 + c1 * c1;
            var h = R * R - Q * Q * Q;

            if (h > 0.0f) // 2 intersections
            {
                h = (float)Math.Sqrt(h);
                var s = Math.Sign(R + h) * Math.Pow(Math.Abs(R + h), 1.0f / 3.0f); // cube root
                var u = Math.Sign(R - h) * Math.Pow(Math.Abs(R - h), 1.0f / 3.0f); // cube root
                var x = s + u + 4.0 * c2;
                var y = s - u;
                var ks = x * x + y * y * 3.0f;
                var k = (float)Math.Sqrt(ks);
                var _t = -0.5f * po * Math.Abs(y) * Math.Sqrt(6.0f / (k + x)) - 2.0 * c1 * (k + x) / (ks + x * k) - k3;
                return (po < 0.0f) ? 1.0f / (float)_t : (float)_t;
            }

            // 4 intersections
            var sQ = (float)Math.Sqrt(Q);
            var w = sQ * Math.Cos(Math.Acos(-R / (sQ * Q)) / 3.0);
            var d2 = -w - c2;
            if (d2 < 0.0) return -1.0f; //no intersection
            var d1 = (float)Math.Sqrt(d2);
            var h1 = Math.Sqrt(w - 2.0 * c2 + c1 / d1);
            var h2 = Math.Sqrt(w - 2.0 * c2 - c1 / d1);
            var t1 = -d1 - h1 - k3; t1 = (po < 0.0) ? 1.0 / t1 : t1;
            var t2 = -d1 + h1 - k3; t2 = (po < 0.0) ? 1.0 / t2 : t2;
            var t3 = d1 - h2 - k3; t3 = (po < 0.0) ? 1.0 / t3 : t3;
            var t4 = d1 + h2 - k3; t4 = (po < 0.0) ? 1.0 / t4 : t4;
            var t = 1e20;
            if (t1 > 0.0) t = t1;
            if (t2 > 0.0) t = Math.Min(t, t2);
            if (t3 > 0.0) t = Math.Min(t, t3);
            if (t4 > 0.0) t = Math.Min(t, t4);
            return (float)t;
        }
    }
}
