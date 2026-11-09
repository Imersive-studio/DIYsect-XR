// Authority: none. Local presentation only. It never sends anything over the network; each device makes its own sounds.
using DIYsect.Classroom;
using DIYsect.Dissection;
using DIYsect.Interaction;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// Plays three soft sound cues on this device: a tick while the cut advances, a two-tone sound when you pick up an
    /// organ, and a chime when your quiz answer is correct. The sounds are made from numbers at startup
    /// (<see cref="SoundCueSynth"/>), so there are no audio files. It owns one 2D AudioSource: the cues are feedback
    /// for the person using this device, not sounds in the room, so they are not placed in space and sound the same
    /// on a headset, a phone and a laptop.
    /// Who hears what: the cut tick plays on every device that sees the cut advance (the progress is shared, so a
    /// classmate watching hears it too, at most every 0.15 s). The grab sound plays only on the device that grabbed,
    /// because the server answers only the asker; other people already see the organ's label and ghost. The chime
    /// plays only on the classmate who answered. Event driven, nothing runs in Update.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundCuePlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("The body's incision progress. Drives the cut tick.")]
        private DissectionProgress _progress;
        [SerializeField, Tooltip("The body's organ grab authority. Drives the grab sound.")]
        private OrganGrabAuthority _grabAuthority;
        [SerializeField, Tooltip("The body's quiz. Drives the correct-answer chime. Optional.")]
        private QuizRound _quiz;

        private AudioSource _source;
        private readonly AudioClip[] _clips = new AudioClip[3];
        private readonly CutHapticLimiter _cutLimiter = new CutHapticLimiter(SoundCueRules.CutMinIntervalSeconds);
        private readonly ISoundCueStore _store = new PlayerPrefsSoundCueStore();
        private SoundCueVolumes _volumes;
        private bool _ready;

        /// <summary>The master volume, 0 to 1.</summary>
        public float MasterVolume => _volumes != null ? _volumes.Master : SoundCueVolumes.DefaultMaster;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;

            bool valid = true;
            if (_progress == null)
            {
                Debug.LogError($"{name}: SoundCuePlayer has no DissectionProgress. Run Tools > DIYsect > Add Sound Cues.", this);
                valid = false;
            }
            if (_grabAuthority == null)
            {
                Debug.LogError($"{name}: SoundCuePlayer has no OrganGrabAuthority. Run Tools > DIYsect > Add Sound Cues.", this);
                valid = false;
            }
            if (_quiz == null)
                Debug.LogWarning($"{name}: SoundCuePlayer has no QuizRound, so there is no correct-answer chime. Run Tools > DIYsect > Add Sound Cues after the quiz is added.", this);
            if (!valid)
            {
                enabled = false;
                return;
            }

            _volumes = SoundCueVolumes.Load(_store);
            int rate = AudioSettings.outputSampleRate;
            foreach (SoundCueKind kind in System.Enum.GetValues(typeof(SoundCueKind)))
            {
                float[] samples = SoundCueSynth.Render(kind, rate);
                AudioClip clip = AudioClip.Create($"SoundCue_{kind}", samples.Length, 1, rate, false);
                clip.SetData(samples, 0);
                _clips[(int)kind] = clip;
            }
            _ready = true;
        }

        private void OnEnable()
        {
            if (!_ready)
                return;
            _cutLimiter.Begin(_progress.IncisionProgress);
            _progress.ProgressChanged += OnProgressChanged;
            _grabAuthority.GrabResolved += OnGrabResolved;
            if (_quiz != null)
                _quiz.LocalAnswerResolved += OnAnswerResolved;
        }

        private void OnDisable()
        {
            if (_progress != null)
                _progress.ProgressChanged -= OnProgressChanged;
            if (_grabAuthority != null)
                _grabAuthority.GrabResolved -= OnGrabResolved;
            if (_quiz != null)
                _quiz.LocalAnswerResolved -= OnAnswerResolved;
        }

        private void OnDestroy()
        {
            foreach (AudioClip clip in _clips)
            {
                if (clip != null)
                    Destroy(clip);
            }
        }

        /// <summary>Sets the master volume (0 to 1) and remembers it on this device. For a UI slider.</summary>
        public void SetMasterVolume(float value)
        {
            if (_volumes == null)
                return;
            _volumes.SetMaster(value);
            _volumes.Save(_store);
        }

        /// <summary>Sets the level of one cue (0 to 1) and remembers it on this device.</summary>
        public void SetCueVolume(SoundCueKind kind, float value)
        {
            if (_volumes == null)
                return;
            _volumes.SetCue(kind, value);
            _volumes.Save(_store);
        }

        /// <summary>Plays one cue now at the current volume. Also useful as a "test sound" button next to a slider.</summary>
        public void Play(SoundCueKind kind)
        {
            if (!_ready)
                return;
            float volume = _volumes.Effective(kind);
            if (volume <= 0f)
                return;
            _source.PlayOneShot(_clips[(int)kind], volume);
        }

        private void OnProgressChanged(float progress)
        {
            if (_cutLimiter.ShouldPulse(progress, Time.unscaledTime))
                Play(SoundCueKind.Cut);
        }

        private void OnGrabResolved(int organIndex, bool granted)
        {
            if (granted)
                Play(SoundCueKind.Grab);
        }

        private void OnAnswerResolved(int round, QuizResult result, bool correct)
        {
            if (SoundCueRules.ShouldChime(result, correct))
                Play(SoundCueKind.Correct);
        }
    }
}
