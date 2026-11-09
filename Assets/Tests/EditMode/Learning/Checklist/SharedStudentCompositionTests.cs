using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DIYsect.Learning.Tests
{
    public class SharedStudentCompositionTests
    {
        [Test]
        public void RepeatedCompositionPreservesBodyAndChecklistBindings()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Professor_MR.unity");
            try
            {
                Transform root = scene.GetRootGameObjects().Single(item => item.name == "DIYsect").transform;
                var network = root.GetComponentsInChildren<NetworkObject>(true);
                var identities = network.Select(item => EditorJsonUtility.ToJson(item)).ToArray();
                var tracker = root.GetComponentInChildren<ChecklistTracker>(true);
                Assert.That(tracker, Is.Not.Null);
                string checklist = EditorJsonUtility.ToJson(tracker);
                Type builder = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("DIYsect.Editor.SharedStudentPresentation"))
                    .First(type => type != null);
                MethodInfo apply = builder.GetMethod("ApplyToScene", BindingFlags.Public | BindingFlags.Static);
                GameObject[] previous = null;
                for (int pass = 0; pass < 2; pass++)
                {
                    var scenery = (GameObject[])apply.Invoke(null, new object[] { scene });
                    CollectionAssert.AreEquivalent(new[] { "LectureHall", "OperatingLight", "SeatedAudience" }, scenery.Select(item => item.name));
                    foreach (GameObject item in scenery)
                    {
                        Assert.That(item.scene, Is.EqualTo(scene));
                        Assert.That(item.GetComponentsInChildren<NetworkObject>(true), Is.Empty);
                        Assert.That(root.Cast<Transform>().Count(child => child.name == item.name), Is.EqualTo(1));
                    }
                    Assert.That(root.Find("SeatedAudience").childCount, Is.EqualTo(40));
                    if (previous != null) CollectionAssert.AreEquivalent(previous, scenery);
                    previous = scenery;
                }
                CollectionAssert.AreEquivalent(network, root.GetComponentsInChildren<NetworkObject>(true));
                for (int i = 0; i < network.Length; i++)
                    Assert.That(EditorJsonUtility.ToJson(network[i]), Is.EqualTo(identities[i]));
                Assert.That(root.GetComponentInChildren<ChecklistTracker>(true), Is.SameAs(tracker));
                Assert.That(EditorJsonUtility.ToJson(tracker), Is.EqualTo(checklist));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
