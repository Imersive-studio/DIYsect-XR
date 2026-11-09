using System;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// Keeps the sound settings in PlayerPrefs, so each device remembers its own volume. PlayerPrefs can fail
    /// (blocked storage, full disk), so every call is wrapped: a failure is logged once and the cues use the defaults.
    /// </summary>
    public class PlayerPrefsSoundCueStore : ISoundCueStore
    {
        private bool _warned;

        public bool TryGetFloat(string key, out float value)
        {
            value = 0f;
            try
            {
                if (!PlayerPrefs.HasKey(key))
                    return false;
                value = PlayerPrefs.GetFloat(key);
                return true;
            }
            catch (Exception exception)
            {
                Warn(exception);
                return false;
            }
        }

        public void SetFloat(string key, float value)
        {
            try
            {
                PlayerPrefs.SetFloat(key, value);
            }
            catch (Exception exception)
            {
                Warn(exception);
            }
        }

        public void Flush()
        {
            try
            {
                PlayerPrefs.Save();
            }
            catch (Exception exception)
            {
                Warn(exception);
            }
        }

        private void Warn(Exception exception)
        {
            if (_warned)
                return;
            _warned = true;
            Debug.LogWarning($"Sound cue settings could not be read or saved: {exception.Message}");
        }
    }
}
