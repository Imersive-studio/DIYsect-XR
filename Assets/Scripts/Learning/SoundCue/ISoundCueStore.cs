namespace DIYsect.Learning
{
    /// <summary>Where the volume settings are kept between runs. The real one uses PlayerPrefs; tests use a dictionary.</summary>
    public interface ISoundCueStore
    {
        /// <summary>True and the saved value if there is one. Never throws.</summary>
        bool TryGetFloat(string key, out float value);

        /// <summary>Remembers a value. Never throws.</summary>
        void SetFloat(string key, float value);

        /// <summary>Writes remembered values to disk. Never throws.</summary>
        void Flush();
    }
}
