namespace RayCasting
{
    class Program
    {
        static void Main()
        {
            var window = new Window(720, 720);
            window.VSync = OpenTK.VSyncMode.Off;
            window.Run(60, 60);
        }
    }
}
