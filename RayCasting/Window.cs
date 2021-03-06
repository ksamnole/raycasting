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
        private float rotation = 1.0f;

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

            var cameraOrigin = new Vector3(-5.0f, 0.0f, 0.0f);
            GL.Begin(PrimitiveType.Points);
            // Пускаем лучи в каждый пиксель экрана
            for (var x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    // Берем координаты в нашем окне. Окно от -1 до 1, в то время как пиксели от 0 до ???
                    var _y = (2 * x - Width) / (float)Width;
                    var _z = (2 * y - Height) / (float)Height;
                    // Направление камеры
                    var cameraDirection = Vector3.Normalize(new Vector3(1.0f, _y, _z));
                    // Цвет пикселя. Если попал в объект, то цвет объекта, иначе цвет неба.
                    var col = CastRay(cameraOrigin, cameraDirection);
                    if (col.X == -1.0f)
                        col = new Vector3(0.3f, 0.6f, 1.0f);
                    //else if (CastRay(cameraOrigin, Vector3.Normalize(new Vector3(-0.5f, 0.75f, 1.0f))).X == -1.0f)
                    //    col *= 0.5f;
                    // Цвета будут более реалистичные
                    col.X = (float)Math.Pow(col.X, 0.45);
                    col.Y = (float)Math.Pow(col.Y, 0.45);
                    col.Z = (float)Math.Pow(col.Z, 0.45);
                    // Красим пиксель
                    GL.Color3(col);
                    GL.Vertex2(_y, _z);
                }
            GL.End();

            rotation += 0.5f;

            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }

        private Vector3 CastRay(Vector3 origin, Vector3 direction)
        {
            /* Координата X - это отдаление и приближение, Y - вправо,влево, Z - вверх, вниз
             * X = -5, Y = 1, Z = 1
             * Возле камеры, правее, сверху
             */
            // Направление освещения
            //var light = Vector3.Normalize(new Vector3(-0.5f, 0.75f, 1.0f));
            var light = Vector3.Normalize(new Vector3((float)Math.Cos(rotation), (float)Math.Sin(rotation), 1.0f));

            var minIntersect = new Vector2(float.MaxValue);
            var color = new Vector3(0.5f, 0.5f, 1.0f);
            Vector3 n = Vector3.Zero;
            // Пересечение с 1 сферой
            var sphere1Pos = new Vector3(0.0f, 2.0f, 0.5f);
            var intersect1 = SphereIntersect(origin - sphere1Pos, direction, 1.5f);
            if (intersect1.X > 0.0f && intersect1.X < minIntersect.X)
            {
                minIntersect = intersect1;
                var itPos = origin + direction * intersect1.X;
                n = itPos - sphere1Pos;
                color = new Vector3(1.0f, 0.2f, 0.1f);
            }
            // Пересечение со 2 сферой
            var sphere2Pos = new Vector3(-2.0f, 0.0f, 0.0f);
            var intersect2 = SphereIntersect(origin - sphere2Pos, direction, 1.0f);
            if (intersect2.X > 0.0f && intersect2.X < minIntersect.X)
            {
                minIntersect = intersect2;
                var itPos = origin + direction * intersect2.X;
                n = itPos - sphere2Pos;
                color = new Vector3(255f / 255, 165f / 255, 201f / 255);
            }
            // Пересечние с полом
            var plateNormal = new Vector3(0.0f, 0.0f, 1.0f);
            var intersect3 = new Vector2(PlaneIntersect(origin, direction, new Vector4(plateNormal, 1.0f)));
            if (intersect3.X > 0.0f && intersect3.X < minIntersect.X)
            {
                minIntersect = intersect3;
                n = plateNormal;
                color = new Vector3(0.5f, 0.5f, 1.0f);
                var planeOrigin = origin + direction * minIntersect.X;
                if (CastRay(planeOrigin, light).X != -1.0f)
                    color *= 0.5f;
            }
            // Пересеченик с BoxSphere
            var posboxSphere = new Vector3(0.0f, -2.0f, 0.5f);
            var intersect4 = new Vector2(BoxSphereIntersect(origin - posboxSphere, direction, 1.5f));
            if (intersect4.X > 0.0f && intersect4.X < minIntersect.X)
            {
                minIntersect = intersect4;
                var itPos = origin + direction * intersect4.X - posboxSphere;
                n = boxSphereNormal(itPos) - posboxSphere;
                color = new Vector3(0.7f, 0.1f, 0.3f);
            }
            // Проверка попал ли луч в объект, если нет то возвращаем цвет неба.
            if (minIntersect.X == float.MaxValue)
                return new Vector3(-1.0f);
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

        private float CapIntersect(Vector3 ro, Vector3 rd, Vector3 pa, Vector3 pb, float ra)
        {
            var ba = pb - pa;
            var oa = ro - pa;
            float baba = Vector3.Dot(ba, ba);
            float bard = Vector3.Dot(ba, rd);
            float baoa = Vector3.Dot(ba, oa);
            float rdoa = Vector3.Dot(rd, oa);
            float oaoa = Vector3.Dot(oa, oa);
            float a = baba - bard * bard;
            float b = baba * rdoa - baoa * bard;
            float c = baba * oaoa - baoa * baoa - ra * ra * baba;
            float h = b * b - a * c;
            if (h >= 0.0)
            {
                float t = (-b - (float)Math.Sqrt(h)) / a;
                float y = baoa + t * bard;
                // body
                if (y > 0.0 && y < baba) return t;
                // caps
                var oc = (y <= 0.0) ? oa : ro - pb;
                b = Vector3.Dot(rd, oc);
                c = Vector3.Dot(oc, oc) - ra * ra;
                h = b * b - c;
                if (h > 0.0) return (float)(-b - Math.Sqrt(h));
            }
            return -1.0f;
        }

        private Vector2 BoxIntersect(Vector3 ro, Vector3 rd, Vector3 boxSize)
        {
            var m = new Vector3(1.0f / rd.X, 1.0f / rd.Y, 1.0f / rd.Z); // can precompute if traversing a set of aligned boxes
            var n = m * ro;   // can precompute if traversing a set of aligned boxes
            var k = new Vector3(Math.Abs(m.X), Math.Abs(m.Y), Math.Abs(m.Z)) * boxSize;
            var t1 = -n - k;
            var t2 = -n + k;
            float tN = Math.Max(Math.Max(t1.X, t1.Y), t1.Z);
            float tF = Math.Min(Math.Min(t2.X, t2.Y), t2.Z);
            if (tN > tF || tF < 0.0) return new Vector2(-1.0f); // no intersection
            return new Vector2(tN, tF);
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

        private Vector3 boxSphereNormal(Vector3 pos)
        {
            return Vector3.Normalize(pos * pos * pos);
        }

        private float BoxSphereIntersect(Vector3 ro, Vector3 rd, float ra)
        {
            var r2 = ra * ra;
            var d2 = rd * rd; var d3 = d2 * rd;
            var o2 = ro * ro; var o3 = o2 * ro;
            float ka = 1.0f / Vector3.Dot(d2, d2);
            float k3 = ka * Vector3.Dot(ro, d3);
            float k2 = ka * Vector3.Dot(o2, d2);
            float k1 = ka * Vector3.Dot(o3, rd);
            float k0 = ka * (Vector3.Dot(o2, o2) - r2 * r2);
            float c2 = k2 - k3 * k3;
            float c1 = k1 + 2.0f * k3 * k3 * k3 - 3.0f * k3 * k2;
            float c0 = k0 - 3.0f * k3 * k3 * k3 * k3 + 6.0f * k3 * k3 * k2 - 4.0f * k3 * k1;
            float p = c2 * c2 + c0 / 3.0f;
            float q = c2 * c2 * c2 - c2 * c0 + c1 * c1;
            float h = q * q - p * p * p;
            if (h < 0.0) return -1.0f; //no intersection
            float sh = (float)Math.Sqrt(h);
            float s = (float)(Math.Sign(q + sh) * Math.Pow(Math.Abs(q + sh), 1.0 / 3.0)); // cuberoot
            float t = (float)(Math.Sign(q - sh) * Math.Pow(Math.Abs(q - sh), 1.0 / 3.0)); // cuberoot
            var w = new Vector2(s + t, s - t);
            var v = new Vector2(w.X + c2 * 4.0f, w.Y * (float)Math.Sqrt(3.0)) * 0.5f;
            float r = (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);
            return -Math.Abs(v.Y) / (float)Math.Sqrt(r + v.X) - c1 / r - k3;
        }

        private Vector3 NorGoursat(Vector3 pos, float ka, float kb)
        {
            return Vector3.Normalize(4.0f * pos * pos * pos - 2.0f * pos * kb);
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
