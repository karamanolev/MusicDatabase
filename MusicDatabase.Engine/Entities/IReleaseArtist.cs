namespace MusicDatabase.Engine.Entities
{
    public interface IReleaseArtist
    {
        Artist Artist { get; set; }
        string JoinString { get; set; }
    }
}
