namespace MusicDatabase.Engine.Entities
{
    public class ReleaseArtist : IReleaseArtist
    {
        public Artist Artist { get; set; }
        public string JoinString { get; set; }
    }
}
