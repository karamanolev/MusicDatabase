namespace MusicDatabase.Audio.Mp3
{
    public enum LameVbrMode
    {
        Off = 0,
        Mt,               /* obsolete, same as vbr_mtrh */
        Rh,
        Abr,
        Mtrh,
        MaxIndicator,    /* Don't use this! It's used for sanity checks.       */
        Default = Mtrh    /* change this to change the default VBR mode of LAME */
    }
}
