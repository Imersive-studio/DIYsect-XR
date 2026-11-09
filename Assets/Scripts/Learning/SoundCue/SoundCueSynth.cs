using System;

namespace DIYsect.Learning
{
    /// <summary>
    /// Builds the three sound cues as raw samples, so the project needs no audio files. Plain C# with no Unity types:
    /// the same input always gives the same numbers, which makes it easy to test. Every note fades in and out with a
    /// smooth curve, so a cue starts and ends at silence and never clicks. Samples are mono, between -1 and 1.
    /// </summary>
    public static class SoundCueSynth
    {
        /// <summary>The loudest sample of every cue. Kept well under 1 so the cues stay soft.</summary>
        public const float PeakLevel = 0.6f;

        private readonly struct Note
        {
            public readonly double Frequency;
            public readonly double Start;
            public readonly double Duration;
            public readonly double Gain;
            public readonly double Attack;
            public readonly double Release;
            public readonly double Decay;

            public Note(double frequency, double start, double duration, double gain, double attack, double release, double decay)
            {
                Frequency = frequency;
                Start = start;
                Duration = duration;
                Gain = gain;
                Attack = attack;
                Release = release;
                Decay = decay;
            }
        }

        // A short low tick: a low tone with a faint upper partner, gone in 70 ms.
        private static readonly Note[] CutNotes =
        {
            new Note(200.0, 0.0, 0.07, 1.0, 0.006, 0.040, 30.0),
            new Note(400.0, 0.0, 0.07, 0.3, 0.006, 0.040, 30.0),
        };

        // Two tones, the second higher: G4 then C5.
        private static readonly Note[] GrabNotes =
        {
            new Note(392.0, 0.00, 0.09, 1.0, 0.008, 0.060, 12.0),
            new Note(523.25, 0.07, 0.12, 1.0, 0.008, 0.080, 12.0),
        };

        // A rising chime: C5, E5, G5, each one ringing on a little longer.
        private static readonly Note[] CorrectNotes =
        {
            new Note(523.25, 0.00, 0.35, 1.0, 0.010, 0.250, 5.0),
            new Note(659.25, 0.09, 0.35, 0.9, 0.010, 0.250, 5.0),
            new Note(783.99, 0.18, 0.50, 0.8, 0.010, 0.350, 5.0),
        };

        /// <summary>Returns the mono samples of one cue at the given sample rate.</summary>
        public static float[] Render(SoundCueKind kind, int sampleRate)
        {
            if (sampleRate < 8000)
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "The sample rate must be at least 8000.");
            Note[] notes = NotesFor(kind);

            double total = 0.0;
            foreach (Note note in notes)
                total = Math.Max(total, note.Start + note.Duration);
            int count = (int)Math.Round(total * sampleRate);

            var mix = new double[count];
            foreach (Note note in notes)
            {
                int first = (int)Math.Round(note.Start * sampleRate);
                for (int i = first; i < count; i++)
                {
                    double t = (double)i / sampleRate - note.Start;
                    if (t < 0.0)
                        continue;
                    if (t >= note.Duration)
                        break;
                    mix[i] += Math.Sin(2.0 * Math.PI * note.Frequency * t) * Envelope(note, t) * note.Gain;
                }
            }

            double peak = 0.0;
            for (int i = 0; i < count; i++)
                peak = Math.Max(peak, Math.Abs(mix[i]));
            double scale = peak > 0.0 ? PeakLevel / peak : 0.0;

            var samples = new float[count];
            for (int i = 0; i < count; i++)
                samples[i] = (float)(mix[i] * scale);
            return samples;
        }

        /// <summary>How long a cue lasts in seconds.</summary>
        public static float DurationSeconds(SoundCueKind kind)
        {
            double total = 0.0;
            foreach (Note note in NotesFor(kind))
                total = Math.Max(total, note.Start + note.Duration);
            return (float)total;
        }

        // Fade in (smooth), fade out (smooth), and a gentle exponential fall in between. Zero at both ends.
        private static double Envelope(Note note, double t)
        {
            double fadeIn = note.Attack > 0.0 ? Math.Min(1.0, t / note.Attack) : 1.0;
            double fadeOut = 1.0;
            if (note.Release > 0.0 && t > note.Duration - note.Release)
                fadeOut = Math.Max(0.0, Math.Min(1.0, (note.Duration - t) / note.Release));
            double smoothIn = Math.Sin(fadeIn * Math.PI / 2.0);
            double smoothOut = Math.Sin(fadeOut * Math.PI / 2.0);
            return smoothIn * smoothIn * smoothOut * smoothOut * Math.Exp(-note.Decay * t);
        }

        private static Note[] NotesFor(SoundCueKind kind)
        {
            switch (kind)
            {
                case SoundCueKind.Cut: return CutNotes;
                case SoundCueKind.Grab: return GrabNotes;
                case SoundCueKind.Correct: return CorrectNotes;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sound cue.");
            }
        }
    }
}
