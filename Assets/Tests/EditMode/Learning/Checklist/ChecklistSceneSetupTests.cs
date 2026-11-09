using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Learning.Tests
{
    public class ChecklistSceneSetupTests
    {
        [TestCase("Assets/Scenes/LectureHall_VR.unity")]
        [TestCase("Assets/Scenes/Professor_MR.unity")]
        public void RebuildingTwiceKeepsOnePresentationAndPreservesNetworkObjects(string path)
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(path);
            try
            {
                Transform root = scene.GetRootGameObjects().Single(item => item.name == "DIYsect").transform;
                NetworkObject[] network = root.GetComponentsInChildren<NetworkObject>(true);
                string[] ids = network.Select(item => EditorJsonUtility.ToJson(item)).ToArray();
                Type builder = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(assembly => assembly.GetType("DIYsect.Editor.AddDissectionChecklist"))
                    .First(type => type != null);
                MethodInfo rebuild = builder.GetMethod("Rebuild", BindingFlags.Static | BindingFlags.Public);
                Assert.That(rebuild, Is.Not.Null);
                int objectCount = 0;
                for (int pass = 0; pass < 2; pass++)
                {
                    rebuild.Invoke(null, new object[] { scene });
                    Transform checklist = root.Find("DissectionChecklist");
                    Assert.That(checklist, Is.Not.Null);
                    Assert.That(root.GetComponentsInChildren<ChecklistPanel>(true).Length, Is.EqualTo(1));
                    Assert.That(checklist.GetComponentsInChildren<Collider>(true), Is.Empty);
                    Assert.That(checklist.GetComponentsInChildren<NetworkObject>(true), Is.Empty);
                    Assert.That(checklist.GetComponentsInChildren<TextMesh>(true), Is.Empty);
                    Assert.That(checklist.GetComponentsInChildren<TMP_Text>(true).Length, Is.EqualTo(18));
                    foreach (Transform child in checklist.GetComponentsInChildren<Transform>(true))
                        Assert.That(child.gameObject.layer, Is.EqualTo(0));
                    if (pass == 0) objectCount = checklist.GetComponentsInChildren<Transform>(true).Length;
                    else Assert.That(checklist.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(objectCount));
                }
                CollectionAssert.AreEquivalent(network, root.GetComponentsInChildren<NetworkObject>(true));
                for (int i = 0; i < network.Length; i++) Assert.That(EditorJsonUtility.ToJson(network[i]), Is.EqualTo(ids[i]));
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
