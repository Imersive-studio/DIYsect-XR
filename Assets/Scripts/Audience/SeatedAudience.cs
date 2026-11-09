using System;
using UnityEngine;

namespace DIYsect.Audience
{
    public sealed class SeatedAudience : MonoBehaviour
    {
        [Serializable]
        public struct Figure
        {
            public Transform upperBody;
            public Transform head;
            public Renderer[] shirt;
            public Renderer[] trousers;
            public Renderer[] face;
            public Renderer[] hair;
            public Color shirtColor;
            public Color skinColor;
            public Color hairColor;
        }

        [SerializeField] private Transform _table;
        [SerializeField] private Transform _student;
        [SerializeField] private Figure[] _figures;
        [SerializeField] private string _layoutKey;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private Vector3[] _restPositions;

        public bool Matches(string layoutKey, Transform table, Transform student, int figureCount) =>
            _layoutKey == layoutKey && _table == table && _student == student &&
            _figures != null && _figures.Length == figureCount && transform.childCount == figureCount;

        public void Configure(Transform table, Transform student, Figure[] figures, string layoutKey)
        {
            _table = table;
            _student = student;
            _figures = figures;
            _layoutKey = layoutKey;
            ApplyColors();
        }

        private void Awake()
        {
            if (_table == null || _student == null || _figures == null || _figures.Length == 0)
            {
                Debug.LogError("Seated audience is missing its scene references. Run Tools > DIYsect > Add Seated Audience.", this);
                enabled = false;
                return;
            }

            _restPositions = new Vector3[_figures.Length];
            for (int i = 0; i < _figures.Length; i++)
                _restPositions[i] = _figures[i].upperBody.localPosition;
            ApplyColors();
        }

        // The editor tool also calls this so the audience has color in the scene view.
        public void ApplyColors()
        {
            if (_figures == null)
                return;

            var block = new MaterialPropertyBlock();
            foreach (Figure figure in _figures)
            {
                Tint(figure.shirt, figure.shirtColor, block);
                Tint(figure.trousers, new Color32(48, 52, 62, 255), block);
                Tint(figure.face, figure.skinColor, block);
                Tint(figure.hair, figure.hairColor, block);
            }
        }

        private static void Tint(Renderer[] renderers, Color color, MaterialPropertyBlock block)
        {
            block.SetColor(BaseColor, color);
            foreach (Renderer renderer in renderers)
                renderer.SetPropertyBlock(block);
        }

        // Breathing and gaze are continuous. Feet and seat anchors stay fixed.
        private void LateUpdate()
        {
            Vector3 table = _table.position;
            Vector3 student = _student.position;
            float distanceSquared = Vector3.ProjectOnPlane(student - table, Vector3.up).sqrMagnitude;
            float blend = 1f - Mathf.Exp(-1.4f * Time.deltaTime);
            float time = Time.time;

            for (int i = 0; i < _figures.Length; i++)
            {
                Figure figure = _figures[i];
                float shiftTime = Mathf.Repeat(time + i * 3.71f, AudienceRules.ShiftInterval(i));
                float shift = shiftTime < 1.6f ? Mathf.Sin(shiftTime / 1.6f * Mathf.PI) : 0f;
                figure.upperBody.localPosition = _restPositions[i] +
                    new Vector3(0.024f * shift, 0.009f * Mathf.Sin(time * 1.5f + i * 2.39f), 0f);

                Vector3 target = AudienceRules.FollowsStudent(i, distanceSquared) ? student : table;
                Vector3 look = figure.upperBody.InverseTransformDirection(target - figure.head.position);
                float yaw = Mathf.Clamp(Mathf.Atan2(look.x, look.z) * Mathf.Rad2Deg, -45f, 45f);
                float pitch = Mathf.Clamp(-Mathf.Atan2(look.y, new Vector2(look.x, look.z).magnitude) * Mathf.Rad2Deg, -18f, 18f);
                figure.head.localRotation = Quaternion.Slerp(figure.head.localRotation, Quaternion.Euler(pitch, yaw, 0f), blend);
            }
        }
    }
}
