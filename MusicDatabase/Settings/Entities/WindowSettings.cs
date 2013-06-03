using System.Windows;

namespace MusicDatabase.Settings.Entities
{
    public class WindowSettings
    {
        public string Name { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public WindowState State { get; set; }
    }
}
