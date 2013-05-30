namespace MusicDatabase.Engine
{
    public class Assert
    {
        public static void IsTrue(bool condition)
        {
            if (!condition)
            {
                throw new AssertFailedException();
            }
        }
    }
}
