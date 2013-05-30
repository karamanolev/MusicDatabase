using FluentNHibernate.Mapping;
using System.Windows;

namespace MusicDatabase.Settings.Entities
{
    public class WindowSettings
    {
        public class WindowSettingsMap : ClassMap<WindowSettings>
        {
            public WindowSettingsMap()
            {
                Id(x => x.Id);
                Map(x => x.Name);
                Map(x => x.X);
                Map(x => x.Y);
                Map(x => x.Width);
                Map(x => x.Height);
                Map(x => x.State);
            }
        }

        public virtual int Id { get; protected set; }
        public virtual string Name { get; set; }
        public virtual int X { get; set; }
        public virtual int Y { get; set; }
        public virtual int Width { get; set; }
        public virtual int Height { get; set; }
        public virtual WindowState State { get; set; }
    }
}
