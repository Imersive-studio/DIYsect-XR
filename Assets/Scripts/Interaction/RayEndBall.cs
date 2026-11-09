// Authority: none. Local to this headset; it only draws a small ball and sends nothing over the network.
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Draws a small ball at the far end of a controller or hand ray, so it is clear where the ray points. It follows the end of
    /// the ray's line renderer and shows only while that line is drawn. Sits on the same object as the ray's line renderer and
    /// is added by Tools > DIYsect > Build Shared Scene.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class RayEndBall : MonoBehaviour
    {
        [SerializeField, Tooltip("Unlit material for the ball.")] private Material _material;
        [SerializeField, Min(0.005f), Tooltip("Ball diameter in metres.")] private float _diameter = 0.03f;

        private LineRenderer _line;
        private Transform _ball;

        /// <summary>The ball, or null before it exists. For tests.</summary>
        public Transform Ball => _ball;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "RayEndBall";
            Destroy(ball.GetComponent<Collider>());
            ball.transform.SetParent(null, false);
            ball.transform.localScale = Vector3.one * _diameter;
            if (_material != null)
                ball.GetComponent<Renderer>().sharedMaterial = _material;
            ball.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ball.SetActive(false);
            _ball = ball.transform;
        }

        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRenderBall;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRenderBall;
            if (_ball != null)
                _ball.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_ball != null)
                Destroy(_ball.gameObject);
        }

        // Runs after the toolkit has placed the line's points for this frame, so the ball does not lag the ray.
        [BeforeRenderOrder(XRInteractionUpdateOrder.k_BeforeRenderLineVisual + 1)]
        private void OnBeforeRenderBall()
        {
            if (_ball == null || _line == null)
                return;
            bool drawn = _line.enabled && _line.positionCount >= 2;
            if (_ball.gameObject.activeSelf != drawn)
                _ball.gameObject.SetActive(drawn);
            if (!drawn)
                return;
            Vector3 end = _line.GetPosition(_line.positionCount - 1);
            if (!_line.useWorldSpace)
                end = transform.TransformPoint(end);
            _ball.position = end;
        }
    }
}
