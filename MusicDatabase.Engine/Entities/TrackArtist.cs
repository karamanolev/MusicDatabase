namespace MusicDatabase.Engine.Entities
{
    public class TrackArtist : IReleaseArtist
    {
        public Artist Artist { get; set; }
        public string JoinString { get; set; }
    }
}
