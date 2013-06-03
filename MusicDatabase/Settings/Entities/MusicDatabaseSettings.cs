using System;
using System.Collections.Generic;

namespace MusicDatabase.Settings.Entities
{
    public class MusicDatabaseSettings
    {
        public int Id { get; set; }
        public string CollectionDatabasePath { get; set; }
        public int LocalConcurrencyLevel { get; set; }
        public List<WindowSettings> WindowsSettings { get; set; }

        public int ActualLocalConcurrencyLevel
        {
            get
            {
                if (this.LocalConcurrencyLevel != 0)
                {
                    return this.LocalConcurrencyLevel;
                }
                return Environment.ProcessorCount;
            }
        }

        public MusicDatabaseSettings()
        {
            this.WindowsSettings = new List<WindowSettings>();
        }

        public static MusicDatabaseSettings CreateDefault()
        {
            return new MusicDatabaseSettings()
            {
            };
        }
    }
}
