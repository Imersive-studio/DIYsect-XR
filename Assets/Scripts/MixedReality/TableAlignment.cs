// Authority: none. This runs on one client only and holds no network state. Each client places its own copy of the
// scene content. The shared body syncs its organ and scalpel poses in body-local space, so moving the root locally
// never changes what the other clients see.
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.MixedReality
{
    /// <summary>
    /// Lines the virtual table, and the body on it, up with a real table in passthrough by moving the scene content root.
    /// Detected table: when the headset reports a horizontal plane labelled as a table, the root is placed on the nearest one.
    /// Alignment handle: grab it and carry the virtual table over the real one. While held the virtual table is shown with
    /// the detected planes. On release the table top snaps to a detected level surface below it, and the table turns to face
    /// the viewer unless the handle was twisted on purpose. Without any detected plane the table top stays at the handle's
    /// own height, so putting the handle down on the real surface sets the height by hand.
    /// Once placed, the virtual table can be hidden so only the body shows on the real table.
    /// </summary>
    public class TableAlignment : MonoBehaviour
    {
        // The template's permissions component already asks for this. Here it is only read, to know when planes can be requested.
        private const string ScenePermission = "com.oculus.permission.USE_SCENE";

        [Header("Scene content")]
        [SerializeField, Tooltip("Root of the table, tray, screen and body. This is the only thing that moves.")] private Transform _root;
        [SerializeField, Tooltip("Renderer of the virtual table top. Its middle and upper face define the table top.")] private Renderer _tableTop;
        [SerializeField, Tooltip("Everything that draws the virtual table. Hidden after placement when the option below is on.")] private Renderer[] _virtualTable;
        [SerializeField, Tooltip("The headset camera, used to face the table toward the viewer.")] private Transform _viewer;

        [Header("Alignment")]
        [SerializeField, Tooltip("Handle the professor carries to the real table.")] private XRGrabInteractable _handle;
        [SerializeField, Tooltip("Optional. Casts down onto detected planes when the handle is released.")] private ARRaycastManager _raycastManager;
        [SerializeField, Tooltip("Optional. Source of table planes. Left off until the scene permission is granted.")] private ARPlaneManager _planeManager;
        [SerializeField, Tooltip("Hide the virtual table once it is placed, so only the body shows on the real table.")] private bool _hideVirtualTableWhenPlaced = true;
        [SerializeField, Min(0.05f), Tooltip("How far above or below the virtual table top a detected surface may be and still be used, in metres.")] private float _snapRange = 0.25f;
        [SerializeField, Range(0f, 45f), Tooltip("Largest tilt of a surface that still counts as a table top, in degrees.")] private float _maxTiltDegrees = 10f;
        [SerializeField, Range(1f, 90f), Tooltip("A handle twist larger than this keeps the heading the professor chose, in degrees.")] private float _deliberateTurnDegrees = 15f;

        [Header("When no table is near")]
        [SerializeField, Tooltip("Put the table in front of the professor when no detected table is within reach, so nobody has to walk to a far table.")] private bool _placeInFrontWhenNoNearbyTable = true;
        [SerializeField, Min(0.5f), Tooltip("A detected table farther than this from the viewer, in metres, is ignored.")] private float _maxDetectedTableDistance = 2.5f;
        [SerializeField, Min(1f), Tooltip("Seconds to wait for a nearby table before placing the table in front of the viewer.")] private float _waitForTableSeconds = 6f;
        [SerializeField, Min(0.3f), Tooltip("How far in front of the viewer the table goes, in metres.")] private float _inFrontDistance = 0.9f;
        [SerializeField, Min(0.2f), Tooltip("How far below the viewer's eyes the table top goes, in metres.")] private float _tableBelowEyes = 0.7f;

        private readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();
        private readonly List<Pose> _hitPoses = new List<Pose>();
        private readonly List<Vector3> _tableCentres = new List<Vector3>();
        private Vector3 _tableTopLocal;
        private Pose _handleRestLocal;
        private Pose _handleAtGrab;
        private Pose _rootAtGrab;
        private Coroutine _follow;
        private AlignmentPhase _phaseBeforeCarry;

        /// <summary>How the root was last placed.</summary>
        public enum Source { None, DetectedTable, Handle, InFrontOfViewer }

        /// <summary>Raised after every placement, with what decided it.</summary>
        public event Action<Source> Placed;

        public Source LastSource { get; private set; }

        /// <summary>Where the alignment is now. The follow coroutine and the plane visuals are derived from it.</summary>
        public AlignmentPhase Phase { get; private set; }

        /// <summary>Raised after the phase changes, with the new phase.</summary>
        public event Action<AlignmentPhase> PhaseChanged;

        /// <summary>The root's current pose in world space.</summary>
        public Pose RootPose => new Pose(_root.position, _root.rotation);

        /// <summary>The middle of the virtual table top, in the root's own space.</summary>
        public Vector3 TableTopLocal => _tableTopLocal;

        private void Awake()
        {
            if (_root == null || _tableTop == null || _viewer == null || _handle == null)
            {
                Debug.LogError($"{name}: TableAlignment needs a root, a table top renderer, a viewer camera and a handle. Run Tools > DIYsect > Add Table Alignment.", this);
                enabled = false;
                return;
            }
            if (!Mathf.Approximately(_root.lossyScale.x, 1f) || !Mathf.Approximately(_root.lossyScale.y, 1f) || !Mathf.Approximately(_root.lossyScale.z, 1f))
            {
                Debug.LogError($"{name}: the root must have scale 1 so the table keeps its real size.", this);
                enabled = false;
                return;
            }

            Bounds top = _tableTop.bounds;
            _tableTopLocal = _root.InverseTransformPoint(new Vector3(top.center.x, top.max.y, top.center.z));
            // The handle waits just beyond the viewer's edge of the table, at table top height.
            Vector3 restPosition = _tableTopLocal + new Vector3(0f, 0.03f, -(top.extents.z + 0.12f));
            _handleRestLocal = new Pose(restPosition, Quaternion.identity);
            ParkHandle();
            SetPlaneVisuals(false);
        }

        private void Start()
        {
            if (_placeInFrontWhenNoNearbyTable)
                StartCoroutine(PlaceInFrontIfNoTable());
            if (_planeManager == null)
                return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(ScenePermission))
                BeginPlaneDetection();
            else
                Application.focusChanged += OnFocusChanged;
#else
            BeginPlaneDetection();
#endif
        }

        private void OnEnable()
        {
            _handle.firstSelectEntered.AddListener(OnHandleGrabbed);
            _handle.lastSelectExited.AddListener(OnHandleReleased);
            if (_planeManager != null)
                _planeManager.trackablesChanged.AddListener(OnPlanesChanged);
        }

        private void OnDestroy()
        {
            Application.focusChanged -= OnFocusChanged;
        }

        // The permission dialog takes focus away from the app. When focus returns, ask again whether planes may be used.
        private void OnFocusChanged(bool hasFocus)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (hasFocus && UnityEngine.Android.Permission.HasUserAuthorizedPermission(ScenePermission))
            {
                Application.focusChanged -= OnFocusChanged;
                BeginPlaneDetection();
            }
#endif
        }

        private void OnDisable()
        {
            _handle.firstSelectEntered.RemoveListener(OnHandleGrabbed);
            _handle.lastSelectExited.RemoveListener(OnHandleReleased);
            if (_planeManager != null)
                _planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
            StopFollowing();
            // A carry cut short by disabling is not a placement. The root stays where it is, the virtual table stays
            // visible, and the phase goes back to what it was before the grab.
            if (Phase == AlignmentPhase.Carrying)
            {
                SetVirtualTableVisible(true);
                SetPlaneVisuals(false);
                SetPhase(_phaseBeforeCarry);
            }
        }

        /// <summary>
        /// Starts looking for planes. Start calls it once the scene permission is granted, because the headset reports no planes before that.
        /// </summary>
        public void BeginPlaneDetection()
        {
            if (_planeManager != null)
                _planeManager.enabled = true;
            // A carry in progress keeps going; it goes back to detecting only if nothing was placed before it.
            if (Phase == AlignmentPhase.AwaitingPermission && _planeManager != null)
                SetPhase(AlignmentPhase.Detecting);
            else if (Phase == AlignmentPhase.Carrying && _phaseBeforeCarry == AlignmentPhase.AwaitingPermission && _planeManager != null)
                _phaseBeforeCarry = AlignmentPhase.Detecting;
        }

        /// <summary>Places the root on the detected table nearest to the viewer. Returns false when no table plane is known, or while the handle is carried.</summary>
        public bool PlaceOnDetectedTable()
        {
            if (_planeManager == null)
                return false;

            _tableCentres.Clear();
            float reach = _placeInFrontWhenNoNearbyTable ? _maxDetectedTableDistance : float.MaxValue;
            foreach (ARPlane plane in _planeManager.trackables)
            {
                Vector3 flat = plane.center - _viewer.position;
                flat.y = 0f;
                if ((plane.classifications & PlaneClassifications.Table) != 0
                    && TableAlignmentMath.IsLevel(plane.normal, _maxTiltDegrees)
                    && flat.magnitude <= reach)
                    _tableCentres.Add(plane.center);
            }
            int nearest = TableAlignmentMath.NearestIndex(_tableCentres, _viewer.position);
            if (nearest < 0)
                return false;

            return PlaceOnTableAt(_tableCentres[nearest]);
        }

        /// <summary>
        /// Places the root on a table whose top is centred at this world position. Returns false while the handle is carried,
        /// because the carry wins over a detected table.
        /// </summary>
        public bool PlaceOnTableAt(Vector3 tableCentre)
        {
            if (Phase == AlignmentPhase.Carrying)
                return false;

            ApplyRootPose(TableAlignmentMath.RootPoseForHit(tableCentre, _tableTopLocal, _viewer.position, _root.rotation));
            Finish(Source.DetectedTable);
            return true;
        }

        /// <summary>
        /// Puts the table straight ahead of the viewer, at a comfortable height, with the virtual table showing. For when
        /// there is no real table within reach. The handle still moves it afterwards.
        /// </summary>
        public void PlaceInFrontOfViewer()
        {
            Vector3 hit = TableAlignmentMath.InFrontOfViewer(_viewer.position, _viewer.forward, _inFrontDistance, _tableBelowEyes);
            ApplyRootPose(TableAlignmentMath.RootPoseForHit(hit, _tableTopLocal, _viewer.position, _root.rotation));
            Finish(Source.InFrontOfViewer);
        }

        private IEnumerator PlaceInFrontIfNoTable()
        {
            yield return new WaitForSeconds(_waitForTableSeconds);
            // A placed table, or one being carried to a real table, is already where the professor wants it.
            if (Phase != AlignmentPhase.Placed && Phase != AlignmentPhase.Carrying)
                PlaceInFrontOfViewer();
        }

        /// <summary>Moves the root. Only the root moves, so the body, tray and screen keep their layout relative to it.</summary>
        public void ApplyRootPose(Pose pose)
        {
            _root.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            // A detected table places the root once, so it never pulls the table away from where the professor put it.
            // Planes that arrive during a carry only get drawn; the carry decides where the table goes.
            if (Phase == AlignmentPhase.Detecting)
                PlaceOnDetectedTable();
            SetPlaneVisuals(Phase == AlignmentPhase.Carrying);
        }

        private void OnHandleGrabbed(SelectEnterEventArgs args)
        {
            if (Phase == AlignmentPhase.Carrying)
                return;
            _handleAtGrab = new Pose(_handle.transform.position, _handle.transform.rotation);
            _rootAtGrab = RootPose;
            _phaseBeforeCarry = Phase;
            SetVirtualTableVisible(true);
            SetPlaneVisuals(true);
            SetPhase(AlignmentPhase.Carrying);
            _follow = StartCoroutine(FollowHandle());
        }

        // Runs only while the handle is held. XRI moves the handle every frame, so the root has to follow every frame.
        private IEnumerator FollowHandle()
        {
            while (true)
            {
                yield return null;
                Pose handleNow = new Pose(_handle.transform.position, _handle.transform.rotation);
                ApplyRootPose(TableAlignmentMath.RootPoseFromHandle(_handleAtGrab, handleNow, _rootAtGrab));
            }
        }

        private void OnHandleReleased(SelectExitEventArgs args)
        {
            // A release that arrives after the carry was cut short (for example by disabling) places nothing.
            if (Phase != AlignmentPhase.Carrying)
                return;
            StopFollowing();

            Pose handleNow = new Pose(_handle.transform.position, _handle.transform.rotation);
            Pose pose = TableAlignmentMath.RootPoseFromHandle(_handleAtGrab, handleNow, _rootAtGrab);

            if (TrySnapToSurface(pose, out Vector3 surface))
            {
                bool keepHeading = TableAlignmentMath.IsDeliberateTurn(_handleAtGrab.rotation, handleNow.rotation, _deliberateTurnDegrees);
                if (keepHeading)
                    pose = TableAlignmentMath.WithTableTopAtHeight(pose, _tableTopLocal, surface.y);
                else
                    pose = TableAlignmentMath.RootPoseForHit(new Vector3(TopCentre(pose).x, surface.y, TopCentre(pose).z), _tableTopLocal, _viewer.position, pose.rotation);
            }

            ApplyRootPose(pose);
            SetPlaneVisuals(false);
            ParkHandle();
            Finish(Source.Handle);
        }

        // Looks straight down through the middle of the virtual table top for a level detected surface near its height.
        private bool TrySnapToSurface(Pose rootPose, out Vector3 surface)
        {
            surface = default;
            if (_raycastManager == null || !_raycastManager.enabled)
                return false;

            Vector3 top = TopCentre(rootPose);
            var ray = new Ray(top + Vector3.up * _snapRange, Vector3.down);
            if (!_raycastManager.Raycast(ray, _hits, TrackableType.PlaneWithinPolygon))
                return false;

            _hitPoses.Clear();
            foreach (ARRaycastHit hit in _hits)
                _hitPoses.Add(hit.pose);
            return TableAlignmentMath.TryPickSurface(_hitPoses, top.y, _snapRange, _maxTiltDegrees, out surface);
        }

        private Vector3 TopCentre(Pose rootPose)
        {
            return rootPose.position + rootPose.rotation * _tableTopLocal;
        }

        private void Finish(Source source)
        {
            LastSource = source;
            // With no real table under it, the virtual table stays so the body is not left floating.
            SetVirtualTableVisible(!_hideVirtualTableWhenPlaced || source == Source.InFrontOfViewer);
            SetPhase(AlignmentPhase.Placed);
            Placed?.Invoke(source);
        }

        // The only place the phase changes.
        private void SetPhase(AlignmentPhase phase)
        {
            if (Phase == phase)
                return;
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        private void StopFollowing()
        {
            if (_follow == null)
                return;
            StopCoroutine(_follow);
            _follow = null;
        }

        // The handle always waits at the same spot beside the table, wherever the table has been moved to.
        private void ParkHandle()
        {
            _handle.transform.SetPositionAndRotation(
                _root.TransformPoint(_handleRestLocal.position),
                _root.rotation * _handleRestLocal.rotation);
        }

        private void SetVirtualTableVisible(bool visible)
        {
            foreach (Renderer part in _virtualTable)
                if (part != null)
                    part.enabled = visible;
        }

        private void SetPlaneVisuals(bool visible)
        {
            if (_planeManager == null)
                return;
            // Only the drawing is switched, because the planes themselves must stay alive to be ray cast against.
            foreach (ARPlane plane in _planeManager.trackables)
                foreach (Renderer drawing in plane.GetComponentsInChildren<Renderer>(true))
                    drawing.enabled = visible;
        }
    }
}
