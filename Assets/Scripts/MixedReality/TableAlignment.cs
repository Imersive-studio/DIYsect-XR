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

        private readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();
        private readonly List<Pose> _hitPoses = new List<Pose>();
        private readonly List<Vector3> _tableCentres = new List<Vector3>();
        private Vector3 _tableTopLocal;
        private Pose _handleRestLocal;
        private Pose _handleAtGrab;
        private Pose _rootAtGrab;
        private Coroutine _follow;
        private bool _placed;

        /// <summary>How the root was last placed.</summary>
        public enum Source { None, DetectedTable, Handle }

        /// <summary>Raised after every placement, with what decided it.</summary>
        public event Action<Source> Placed;

        public Source LastSource { get; private set; }

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
            if (_follow != null)
            {
                StopCoroutine(_follow);
                _follow = null;
            }
        }

        /// <summary>
        /// Starts looking for planes. Start calls it once the scene permission is granted, because the headset reports no planes before that.
        /// </summary>
        public void BeginPlaneDetection()
        {
            if (_planeManager != null)
                _planeManager.enabled = true;
        }

        /// <summary>Places the root on the detected table nearest to the viewer. Returns false when no table plane is known.</summary>
        public bool PlaceOnDetectedTable()
        {
            if (_planeManager == null)
                return false;

            _tableCentres.Clear();
            foreach (ARPlane plane in _planeManager.trackables)
            {
                if ((plane.classifications & PlaneClassifications.Table) != 0 && TableAlignmentMath.IsLevel(plane.normal, _maxTiltDegrees))
                    _tableCentres.Add(plane.center);
            }
            int nearest = TableAlignmentMath.NearestIndex(_tableCentres, _viewer.position);
            if (nearest < 0)
                return false;

            ApplyRootPose(TableAlignmentMath.RootPoseForHit(_tableCentres[nearest], _tableTopLocal, _viewer.position, _root.rotation));
            Finish(Source.DetectedTable);
            return true;
        }

        /// <summary>Moves the root. Only the root moves, so the body, tray and screen keep their layout relative to it.</summary>
        public void ApplyRootPose(Pose pose)
        {
            _root.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
        {
            // A detected table places the root once, so it never pulls the table away from where the professor put it.
            if (!_placed)
                PlaceOnDetectedTable();
            SetPlaneVisuals(_follow != null);
        }

        private void OnHandleGrabbed(SelectEnterEventArgs args)
        {
            _handleAtGrab = new Pose(_handle.transform.position, _handle.transform.rotation);
            _rootAtGrab = RootPose;
            SetVirtualTableVisible(true);
            SetPlaneVisuals(true);
            if (_follow == null)
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
            if (_follow != null)
            {
                StopCoroutine(_follow);
                _follow = null;
            }

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
            _placed = true;
            LastSource = source;
            SetVirtualTableVisible(!_hideVirtualTableWhenPlaced);
            Placed?.Invoke(source);
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
