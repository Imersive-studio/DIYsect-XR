// Authority: none. Presentation only; it reads the server-owned quiz counts and never writes them.
using System.Collections.Generic;
using DIYsect.Interaction;
using DIYsect.Presentation;
using TMPro;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// The class quiz on the body. While a round is asking or revealed, every organ glows warmer the more classmates tapped
    /// it (coral blended into its own colour, see <see cref="QuizHeatmapRule"/>); an organ nobody tapped keeps its own colour.
    /// When the professor reveals, the correct organ turns mint and rises out of the body (the lift of
    /// <see cref="ClassmatePopOutPresenter"/>). A small chip above the body counts the answers.
    ///
    /// It reads <see cref="QuizRound"/> only when its StateChanged event fires. The per-frame step eases the colours and runs
    /// only while something is glowing or fading. Colours go through a MaterialPropertyBlock on _BaseColor and _Color, never a
    /// new material, and are restored when the round is cleared or this component is switched off.
    /// </summary>
    [RequireComponent(typeof(QuizRound))]
    public class QuizHeatmapPresenter : MonoBehaviour
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int _colorId = Shader.PropertyToID("_Color");
        private static readonly Vector2 ChipBox = new Vector2(0.4f, 0.026f);
        private const float ChipCap = 0.0135f;

        [SerializeField, Tooltip("The quiz on this body. Found on the same object when empty.")]
        private QuizRound _quiz;
        [SerializeField, Tooltip("Lifts the correct organ on reveal. Found on the same object when empty; the lift is skipped if there is none.")]
        private ClassmatePopOutPresenter _popOut;
        [SerializeField, Tooltip("Font for the answer chip (Inter). Run Tools > DIYsect > Add Quiz Heatmap to fill this in.")]
        private TMP_FontAsset _fontAsset;
        [SerializeField, Range(0f, 1f), Tooltip("How far an organ moves toward coral at the highest heat.")]
        private float _hotStrength = 0.8f;
        [SerializeField, Range(0f, 1f), Tooltip("How far the correct organ moves toward mint on reveal.")]
        private float _mintStrength = 0.75f;
        [SerializeField, Min(0f), Tooltip("Metres between the top of the body and the answer chip.")]
        private float _chipGap = LabelAnchor.Gap;

        // Colours: the gentlest blend is CoralSoft's own opacity (0.16), the strongest is _hotStrength of full Coral.
        // One hue in two strengths reads as "warmer" without a second colour to learn, and it stays clear of the violet classmate pointer.
        private readonly List<OrganGlow> _glows = new List<OrganGlow>();
        private MaterialPropertyBlock _block;
        private TextMeshPro _chip;
        private Vector3 _chipLocal;
        private bool _chipPlaced;
        private bool _stepping;

        private void Awake()
        {
            if (_quiz == null)
                _quiz = GetComponent<QuizRound>();
            if (_popOut == null)
                _popOut = GetComponent<ClassmatePopOutPresenter>();
            _block = new MaterialPropertyBlock();
            if (_fontAsset == null)
            {
                Debug.LogError($"{name}: QuizHeatmapPresenter has no font. Run Tools > DIYsect > Add Quiz Heatmap.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _quiz.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (_quiz != null)
                _quiz.StateChanged -= Refresh;
            if (_popOut != null)
                _popOut.SetQuizLift(-1);
            RestoreAll();
            if (_chip != null)
                _chip.gameObject.SetActive(false);
            _stepping = false;
        }

        private void OnDestroy()
        {
            if (_chip != null)
                Destroy(_chip.gameObject);
        }

        // Reads the round again and sets what every organ is heading toward. No easing happens here.
        private void Refresh()
        {
            int organs = _quiz.IsSpawned ? _quiz.OrganCount : 0;
            bool showing = organs > 0 && _quiz.Phase != QuizPhase.Idle;
            bool revealed = showing && _quiz.Phase == QuizPhase.Revealed;
            EnsureGlows(organs);

            int total = 0;
            int max = 0;
            if (showing)
                for (int i = 0; i < organs; i++)
                {
                    int taps = _quiz.TapsFor(i);
                    total += taps;
                    if (taps > max)
                        max = taps;
                }
            for (int i = 0; i < _glows.Count; i++)
            {
                _glows[i].TargetHeat = showing ? QuizHeatmapRule.Heat(_quiz.TapsFor(i), total, max) : 0f;
                _glows[i].TargetMint = revealed && i == _quiz.TargetOrgan ? 1f : 0f;
            }

            if (_popOut != null)
                _popOut.SetQuizLift(revealed ? _quiz.TargetOrgan : -1);
            ShowChip(showing ? QuizHeatmapRule.ChipText(total) : string.Empty);
            _stepping = true;
        }

        // Polling exception: the colours need a clock to ease. Runs only while something is glowing or fading.
        private void LateUpdate()
        {
            if (!_stepping)
                return;
            float step = Time.deltaTime / MotionSpec.PopOutIn;
            bool moving = false;
            foreach (OrganGlow glow in _glows)
            {
                glow.Step(step);
                Paint(glow);
                moving |= glow.IsActive;
            }
            // Painting every frame while active also re-applies the tint if another presenter cleared the block.
            _stepping = moving;
        }

        private void Paint(OrganGlow glow)
        {
            if (!glow.IsActive)
            {
                // Restore once, on the way out, so an organ this presenter never touched keeps whatever another presenter set.
                if (glow.Painted)
                {
                    glow.Restore();
                    glow.Painted = false;
                }
                return;
            }
            glow.Painted = true;
            float heat = Ease.OutCubic(glow.Heat);
            float strength = QuizHeatmapRule.Strength(heat, GuidePalette.CoralSoft.a, _hotStrength * GuidePalette.Coral.a);
            float mint = Ease.OutCubic(glow.Mint) * _mintStrength;
            for (int i = 0; i < glow.Renderers.Length; i++)
            {
                Renderer renderer = glow.Renderers[i];
                if (renderer == null)
                    continue;
                Color tinted = Color.Lerp(glow.BaseColors[i], GuidePalette.Coral, strength);
                tinted = Color.Lerp(tinted, GuidePalette.Mint, mint);
                renderer.GetPropertyBlock(_block);
                _block.SetColor(_baseColorId, tinted);
                _block.SetColor(_colorId, tinted);
                renderer.SetPropertyBlock(_block);
            }
        }

        private void RestoreAll()
        {
            foreach (OrganGlow glow in _glows)
            {
                glow.SnapOff();
                if (glow.Painted)
                    glow.Restore();
                glow.Painted = false;
            }
        }

        // Organs and their original colours are read once; the body's organ list does not change after spawn.
        private void EnsureGlows(int organs)
        {
            if (_glows.Count == organs || organs == 0)
                return;
            RestoreAll();
            _glows.Clear();
            for (int i = 0; i < organs; i++)
            {
                Renderer[] renderers = _quiz.Body.Organs[i].GetComponentsInChildren<Renderer>();
                var colors = new Color[renderers.Length];
                for (int r = 0; r < renderers.Length; r++)
                    colors[r] = BaseColorOf(renderers[r]);
                _glows.Add(new OrganGlow(renderers, colors));
            }
        }

        private void ShowChip(string text)
        {
            if (text.Length == 0)
            {
                if (_chip != null)
                    _chip.gameObject.SetActive(false);
                return;
            }
            if (_chip == null)
            {
                _chip = LabelText.Create(transform, "Quiz answer count", _fontAsset, ChipCap, GuidePalette.Coral, FontStyles.Bold,
                    TextAlignmentOptions.Center, ChipBox, new Vector2(0.5f, 0.5f), false, onTop: true);
                _chip.gameObject.AddComponent<Billboard>();
            }
            if (!_chipPlaced)
            {
                Bounds bounds = LabelAnchor.RendererBounds(transform, _chip.transform);
                _chipLocal = transform.InverseTransformPoint(LabelAnchor.Above(bounds, _chipGap));
                _chipPlaced = true;
            }
            _chip.transform.position = transform.TransformPoint(_chipLocal);
            _chip.text = text;
            _chip.gameObject.SetActive(true);
        }

        private static Color BaseColorOf(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            if (material == null)
                return Color.white;
            if (material.HasProperty(_baseColorId))
                return material.GetColor(_baseColorId);
            return material.HasProperty(_colorId) ? material.GetColor(_colorId) : Color.white;
        }

        // One organ: its renderers, their own colours, and the eased heat and mint amounts.
        private sealed class OrganGlow
        {
            public readonly Renderer[] Renderers;
            public readonly Color[] BaseColors;
            public bool Painted;
            public float Heat;
            public float Mint;
            public float TargetHeat;
            public float TargetMint;

            public OrganGlow(Renderer[] renderers, Color[] baseColors)
            {
                Renderers = renderers;
                BaseColors = baseColors;
            }

            public bool IsActive => Heat > 0f || Mint > 0f || TargetHeat > 0f || TargetMint > 0f;

            public void Step(float amount)
            {
                Heat = Mathf.MoveTowards(Heat, TargetHeat, amount);
                Mint = Mathf.MoveTowards(Mint, TargetMint, amount);
            }

            public void SnapOff()
            {
                Heat = Mint = TargetHeat = TargetMint = 0f;
            }

            public void Restore()
            {
                foreach (Renderer renderer in Renderers)
                    if (renderer != null)
                        renderer.SetPropertyBlock(null);
            }
        }
    }
}
