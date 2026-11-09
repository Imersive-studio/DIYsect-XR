using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.MixedReality.Tests
{
    public class TableAlignmentMathTests
    {
        private const float Tolerance = 0.001f;

        // The authored layout: the table top middle is 0.8 m up and 1.2 m in front of the root origin.
        private static readonly Vector3 _tableTopLocal = new Vector3(0.1f, 0.8f, 1.2f);

        private static Vector3 TableTopWorld(Pose root)
        {
            return root.position + root.rotation * _tableTopLocal;
        }

        [Test]
        public void FlatHitPutsTheTableTopMiddleOnTheHit()
        {
            var hit = new Vector3(2f, 0.74f, -1f);
            Pose root = TableAlignmentMath.RootPoseForHit(hit, _tableTopLocal, new Vector3(2f, 1.6f, -3f), Quaternion.identity);
            Assert.AreEqual(0f, Vector3.Distance(hit, TableTopWorld(root)), Tolerance);
        }

        [Test]
        public void HeightOffsetPutsTheRootBelowTheHitByTheTableTopHeight()
        {
            Pose root = TableAlignmentMath.RootPoseForHit(new Vector3(0f, 0.74f, 2f), _tableTopLocal, Vector3.zero, Quaternion.identity);
            Assert.AreEqual(0.74f - 0.8f, root.position.y, Tolerance);
        }

        [Test]
        public void RootFacesAwayFromTheViewerSoTheViewerIsOnTheNegativeZSide()
        {
            var viewer = new Vector3(3f, 1.6f, 1f);
            var hit = new Vector3(3f, 0.74f, 4f);
            Pose root = TableAlignmentMath.RootPoseForHit(hit, _tableTopLocal, viewer, Quaternion.identity);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.forward, root.rotation * Vector3.forward), Tolerance);

            Vector3 viewerLocal = Quaternion.Inverse(root.rotation) * (viewer - TableTopWorld(root));
            Assert.Less(viewerLocal.z, 0f);
            Assert.AreEqual(0f, viewerLocal.x, Tolerance);
        }

        [Test]
        public void ViewerToTheSideTurnsTheRootToFaceThem()
        {
            var viewer = new Vector3(-2f, 1.6f, 0f);
            var hit = new Vector3(0f, 0.74f, 0f);
            Pose root = TableAlignmentMath.RootPoseForHit(hit, _tableTopLocal, viewer, Quaternion.identity);
            // The viewer is on the negative X side, so the root looks along +X.
            Assert.AreEqual(0f, Vector3.Distance(Vector3.right, root.rotation * Vector3.forward), Tolerance);
        }

        [Test]
        public void RootStaysUprightWhateverTheViewerHeight()
        {
            Pose root = TableAlignmentMath.RootPoseForHit(new Vector3(0f, 0.74f, 2f), _tableTopLocal, new Vector3(0.5f, 3f, -1f), Quaternion.identity);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.up, root.rotation * Vector3.up), Tolerance);
        }

        [Test]
        public void TiltedHitUsesOnlyItsPointAndKeepsTheRootUpright()
        {
            // A 5 degree tilt is the surface normal. The pose math only reads the point, so the root stays level.
            var tilted = new Pose(new Vector3(1f, 0.74f, 2f), Quaternion.Euler(5f, 0f, 3f));
            Pose root = TableAlignmentMath.RootPoseForHit(tilted.position, _tableTopLocal, Vector3.zero, Quaternion.identity);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.up, root.rotation * Vector3.up), Tolerance);
            Assert.AreEqual(0f, Vector3.Distance(tilted.position, TableTopWorld(root)), Tolerance);
        }

        [Test]
        public void ViewerDirectlyAboveKeepsTheCurrentHeading()
        {
            Quaternion current = Quaternion.Euler(0f, 70f, 0f);
            Pose root = TableAlignmentMath.RootPoseForHit(new Vector3(1f, 0.74f, 1f), _tableTopLocal, new Vector3(1f, 1.7f, 1f), current);
            Assert.AreEqual(0f, Quaternion.Angle(current, root.rotation), 0.01f);
        }

        [Test]
        public void CurrentHeadingDropsTiltAndRoll()
        {
            Quaternion tiltedHeading = Quaternion.Euler(20f, 70f, 10f);
            Quaternion upright = TableAlignmentMath.UprightYaw(tiltedHeading);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.up, upright * Vector3.up), Tolerance);
        }

        [Test]
        public void WithTableTopAtHeightMovesOnlyTheHeight()
        {
            var root = new Pose(new Vector3(1f, 0.1f, 2f), Quaternion.Euler(0f, 30f, 0f));
            Pose snapped = TableAlignmentMath.WithTableTopAtHeight(root, _tableTopLocal, 0.9f);
            Assert.AreEqual(0.9f, TableTopWorld(snapped).y, Tolerance);
            Assert.AreEqual(root.position.x, snapped.position.x, Tolerance);
            Assert.AreEqual(root.position.z, snapped.position.z, Tolerance);
            Assert.AreEqual(0f, Quaternion.Angle(root.rotation, snapped.rotation), 0.01f);
        }

        [Test]
        public void CarryingTheHandleSlidesTheRootAcrossTheFloorPlaneOnly()
        {
            var handleAtGrab = new Pose(new Vector3(0f, 0.8f, 0f), Quaternion.identity);
            var rootAtGrab = new Pose(new Vector3(0f, 0f, 1f), Quaternion.identity);
            // The hand moves 0.5 m right, 0.3 m up and 0.2 m forward.
            var handleNow = new Pose(new Vector3(0.5f, 1.1f, 0.2f), Quaternion.identity);
            Pose root = TableAlignmentMath.RootPoseFromHandle(handleAtGrab, handleNow, rootAtGrab);
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(0.5f, 0f, 1.2f), root.position), Tolerance);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, root.rotation), 0.01f);
        }

        [Test]
        public void TwistingTheHandleTurnsTheRootAboutTheHandle()
        {
            var handleAtGrab = new Pose(new Vector3(0f, 0.8f, 0f), Quaternion.identity);
            var rootAtGrab = new Pose(new Vector3(0f, 0f, 1f), Quaternion.identity);
            var handleNow = new Pose(handleAtGrab.position, Quaternion.Euler(0f, 90f, 0f));
            Pose root = TableAlignmentMath.RootPoseFromHandle(handleAtGrab, handleNow, rootAtGrab);
            // One metre in front of the handle becomes one metre to its right after a quarter turn.
            Assert.AreEqual(0f, Vector3.Distance(new Vector3(1f, 0f, 0f), root.position), Tolerance);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.right, root.rotation * Vector3.forward), Tolerance);
        }

        [Test]
        public void TippingTheHandleOverDoesNotTiltTheRoot()
        {
            var handleAtGrab = new Pose(Vector3.zero, Quaternion.identity);
            var rootAtGrab = new Pose(new Vector3(0f, 0f, 1f), Quaternion.identity);
            var handleNow = new Pose(Vector3.zero, Quaternion.Euler(60f, 0f, 25f));
            Pose root = TableAlignmentMath.RootPoseFromHandle(handleAtGrab, handleNow, rootAtGrab);
            Assert.AreEqual(0f, Vector3.Distance(Vector3.up, root.rotation * Vector3.up), Tolerance);
        }

        [Test]
        public void HandleThatDidNotMoveLeavesTheRootAlone()
        {
            var handle = new Pose(new Vector3(0.3f, 0.8f, 0.4f), Quaternion.Euler(10f, 40f, 0f));
            var rootAtGrab = new Pose(new Vector3(1f, 0.2f, 2f), Quaternion.Euler(0f, 25f, 0f));
            Pose root = TableAlignmentMath.RootPoseFromHandle(handle, handle, rootAtGrab);
            Assert.AreEqual(0f, Vector3.Distance(rootAtGrab.position, root.position), Tolerance);
            Assert.AreEqual(0f, Quaternion.Angle(rootAtGrab.rotation, root.rotation), 0.01f);
        }

        [Test]
        public void TwistAboutUpReadsTheSignedTurn()
        {
            Assert.AreEqual(40f, TableAlignmentMath.TwistAboutUp(Quaternion.Euler(0f, 40f, 0f)), 0.01f);
            Assert.AreEqual(-40f, TableAlignmentMath.TwistAboutUp(Quaternion.Euler(0f, -40f, 0f)), 0.01f);
            Assert.AreEqual(0f, TableAlignmentMath.TwistAboutUp(Quaternion.identity), 0.01f);
        }

        [Test]
        public void LevelSurfaceIsAcceptedAndTiltedOrVerticalOnesAreNot()
        {
            Assert.IsTrue(TableAlignmentMath.IsLevel(Vector3.up, 10f));
            Assert.IsTrue(TableAlignmentMath.IsLevel(Quaternion.Euler(8f, 0f, 0f) * Vector3.up, 10f));
            Assert.IsFalse(TableAlignmentMath.IsLevel(Quaternion.Euler(20f, 0f, 0f) * Vector3.up, 10f));
            Assert.IsFalse(TableAlignmentMath.IsLevel(Vector3.right, 10f), "A wall is not a table.");
            Assert.IsFalse(TableAlignmentMath.IsLevel(Vector3.down, 10f), "A ceiling is not a table.");
            Assert.IsFalse(TableAlignmentMath.IsLevel(Vector3.zero, 10f));
        }

        [Test]
        public void PickSurfaceSkipsTheFloorAndTakesTheTable()
        {
            var hits = new List<Pose>
            {
                new Pose(new Vector3(0f, 0.74f, 0f), Quaternion.identity),
                new Pose(new Vector3(0f, 0f, 0f), Quaternion.identity),
            };
            Assert.IsTrue(TableAlignmentMath.TryPickSurface(hits, 0.8f, 0.25f, 10f, out Vector3 point));
            Assert.AreEqual(0.74f, point.y, Tolerance);

            hits.Reverse();
            Assert.IsTrue(TableAlignmentMath.TryPickSurface(hits, 0.8f, 0.25f, 10f, out point));
            Assert.AreEqual(0.74f, point.y, Tolerance, "Order must not matter, only height and tilt.");
        }

        [Test]
        public void PickSurfaceAcceptsAGentlyTiltedTable()
        {
            var hits = new List<Pose> { new Pose(new Vector3(0f, 0.74f, 0f), Quaternion.Euler(4f, 0f, 3f)) };
            Assert.IsTrue(TableAlignmentMath.TryPickSurface(hits, 0.8f, 0.25f, 10f, out _));
        }

        [Test]
        public void PickSurfaceRejectsWallsAndSurfacesOutOfRange()
        {
            var wall = new List<Pose> { new Pose(new Vector3(0f, 0.8f, 0f), Quaternion.Euler(90f, 0f, 0f)) };
            Assert.IsFalse(TableAlignmentMath.TryPickSurface(wall, 0.8f, 0.25f, 10f, out _));

            var farBelow = new List<Pose> { new Pose(new Vector3(0f, 0.2f, 0f), Quaternion.identity) };
            Assert.IsFalse(TableAlignmentMath.TryPickSurface(farBelow, 0.8f, 0.25f, 10f, out _));

            Assert.IsFalse(TableAlignmentMath.TryPickSurface(new List<Pose>(), 0.8f, 0.25f, 10f, out _));
        }

        [Test]
        public void NearestIndexIgnoresHeight()
        {
            var tables = new List<Vector3> { new Vector3(5f, 0.7f, 0f), new Vector3(1f, 2f, 1f), new Vector3(-3f, 0.7f, 0f) };
            Assert.AreEqual(1, TableAlignmentMath.NearestIndex(tables, new Vector3(0f, 1.6f, 0f)));
            Assert.AreEqual(-1, TableAlignmentMath.NearestIndex(new List<Vector3>(), Vector3.zero));
        }

        [Test]
        public void SmallTwistIsNotDeliberateButALargeOneIs()
        {
            Assert.IsFalse(TableAlignmentMath.IsDeliberateTurn(Quaternion.identity, Quaternion.Euler(0f, 8f, 0f), 15f));
            Assert.IsTrue(TableAlignmentMath.IsDeliberateTurn(Quaternion.identity, Quaternion.Euler(0f, -40f, 0f), 15f));
            Assert.IsFalse(TableAlignmentMath.IsDeliberateTurn(Quaternion.identity, Quaternion.Euler(50f, 0f, 0f), 15f), "Tipping the handle is not a turn.");
        }

        [Test]
        public void InFrontOfViewer_IsAheadOnTheFloorPlaneAndBelowTheEyes()
        {
            Vector3 point = TableAlignmentMath.InFrontOfViewer(new Vector3(1f, 1.6f, 2f), new Vector3(0f, -0.4f, 1f), 0.9f, 0.7f);

            Assert.AreEqual(1f, point.x, Tolerance);
            Assert.AreEqual(2.9f, point.z, Tolerance);
            Assert.AreEqual(0.9f, point.y, Tolerance);
        }

        [Test]
        public void InFrontOfViewer_FollowsTheHeadingNotTheTilt()
        {
            Vector3 level = TableAlignmentMath.InFrontOfViewer(Vector3.zero, new Vector3(1f, 0f, 0f), 1f, 0.5f);
            Vector3 tilted = TableAlignmentMath.InFrontOfViewer(Vector3.zero, new Vector3(1f, -0.8f, 0f), 1f, 0.5f);

            Assert.AreEqual(0f, Vector3.Distance(level, tilted), Tolerance);
            Assert.AreEqual(1f, level.x, Tolerance);
        }

        [Test]
        public void InFrontOfViewer_LookingStraightDownKeepsTheWorldForward()
        {
            Vector3 point = TableAlignmentMath.InFrontOfViewer(Vector3.zero, Vector3.down, 1f, 0.5f);

            Assert.AreEqual(0f, point.x, Tolerance);
            Assert.AreEqual(1f, point.z, Tolerance);
        }
    }
}
