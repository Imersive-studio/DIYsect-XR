// Shared helpers for the state bug tests: tracked scene objects, a code-made hand, a scalpel session and a few
// reflection helpers. Reflection is used only to set serialized fields on objects built in code and to read the
// private "may hold" flags that the step 2 refactor replaces. A flag that no longer exists counts as cleared.
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DIYsect.Networking;
using DIYsect.Networking.Tests;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction.Tests
{
    public abstract class InteractionTestBase : NetworkedBodyTestBase
    {
        private readonly List<GameObject> _tracked = new List<GameObject>();

        protected T Track<T>(T created) where T : Component
        {
            _tracked.Add(created.gameObject);
            return created;
        }

        protected GameObject Track(GameObject created)
        {
            _tracked.Add(created);
            return created;
        }

        // Runs before the base teardown, so hands and tools are gone before the network shuts down.
        [UnityTearDown]
        public IEnumerator DestroyTracked()
        {
            foreach (GameObject tracked in _tracked)
                if (tracked != null)
                    Object.Destroy(tracked);
            _tracked.Clear();
            yield return null;
        }

        // A hand without hardware: a direct interactor made in code. Wait one frame before using it so it registers.
        protected XRDirectInteractor CreateHand(string handName)
        {
            var hand = Track(new GameObject(handName));
            hand.AddComponent<SphereCollider>().isTrigger = true;
            hand.AddComponent<Rigidbody>().isKinematic = true;
            return hand.AddComponent<XRDirectInteractor>();
        }

        protected static void SetSerialized(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"{target.GetType().Name} has no field {fieldName}.");
            field.SetValue(target, value);
        }

        // Asserts that a private bool flag is false. If the field is gone (replaced by an explicit state), the check passes.
    }

    // Host with the body, one remote client with a copy of the body, and the scalpel spawned under the body.
    public abstract class ScalpelSessionTestBase : InteractionTestBase
    {
        protected NetworkManager _clientManager;
        protected SharedBody _clientBody;
        protected GameObject _serverScalpel;
        protected GameObject _clientScalpel;
        protected ScalpelSteering _serverSteering;
        protected ScalpelSteering _clientSteering;
        protected GuidingHand _serverHand;
        protected GuidingHand _clientHand;

        [UnityTearDown]
        public IEnumerator DestroyScalpel()
        {
            if (_serverScalpel != null)
                Object.Destroy(_serverScalpel);
            yield return null;
        }

        protected IEnumerator StartScalpelSession()
        {
            yield return StartHostAndSpawnBody();
            _clientManager = StartClient("Client");
            yield return WaitForBody(_clientManager, body => _clientBody = body);

            _serverScalpel = Object.Instantiate(LoadScalpelPrefabAsset());
            var spawned = _serverScalpel.GetComponent<NetworkObject>();
            Assert.IsNotNull(spawned, "The scalpel prefab needs a NetworkObject. Run Tools > DIYsect > Add Placeholder Scalpel.");
            spawned.Spawn();
            Assert.IsTrue(spawned.TrySetParent(_serverBody.GetComponent<NetworkObject>(), worldPositionStays: false), "The scalpel could not be parented to the body.");
            _serverScalpel.transform.SetLocalPositionAndRotation(new Vector3(0f, 1.5f, 0f), Quaternion.identity);
            _serverSteering = _serverScalpel.GetComponent<ScalpelSteering>();
            _serverHand = _serverScalpel.GetComponent<GuidingHand>();

            yield return WaitFor(() =>
            {
                foreach (NetworkObject other in _clientManager.SpawnManager.SpawnedObjects.Values)
                {
                    if (other.GetComponent<ScalpelSteering>() != null)
                    {
                        _clientScalpel = other.gameObject;
                        return true;
                    }
                }
                return false;
            }, "the client to receive the scalpel");
            _clientSteering = _clientScalpel.GetComponent<ScalpelSteering>();
            _clientHand = _clientScalpel.GetComponent<GuidingHand>();
            yield return WaitFor(() => _clientSteering.IsSpawned && _clientHand.IsSpawned, "the client scalpel to spawn");
        }
    }
}
