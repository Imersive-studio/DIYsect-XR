using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.Invite.Tests
{
    public class LobbyRemovalTests
    {
        private sealed class FakeRemoval : ILobbyRemovalService
        {
            public readonly TaskCompletionSource<bool> Completion = new TaskCompletionSource<bool>();
            public string Deleted;
            public string Left;
            public string Player;
            public Task DeleteAsync(string lobbyId) { Deleted = lobbyId; return Completion.Task; }
            public Task RemovePlayerAsync(string lobbyId, string playerId)
            { Left = lobbyId; Player = playerId; return Completion.Task; }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task OldRemovalCannotClearAReplacementRoom(bool host)
        {
            var item = new GameObject("Removal test");
            item.SetActive(false);
            try
            {
                var manager = item.AddComponent<RemovalTestLobbyManager>();
                var service = new FakeRemoval();
                manager.Service = service;
                manager.connectedLobby = new Lobby(id: "old", hostId: host ? "local" : "remote");
                Task<bool> pending = manager.RemovePlayerFromLobby("local");
                var replacement = new Lobby(id: "new", hostId: "local");
                manager.connectedLobby = replacement;
                service.Completion.SetResult(true);
                Assert.That(await pending, Is.True);
                Assert.That(manager.connectedLobby, Is.SameAs(replacement));
                Assert.That(host ? service.Deleted : service.Left, Is.EqualTo("old"));
                if (!host) Assert.That(service.Player, Is.EqualTo("local"));
            }
            finally { UnityEngine.Object.DestroyImmediate(item); }
        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task RemovalReportsItsResultAndRetainsFailedRoomForRetry(bool succeed)
        {
            var item = new GameObject("Removal result");
            item.SetActive(false);
            try
            {
                var manager = item.AddComponent<RemovalTestLobbyManager>();
                var service = new FakeRemoval();
                manager.Service = service;
                var room = new Lobby(id: "room", hostId: "local");
                manager.connectedLobby = room;
                Task<bool> pending = manager.RemovePlayerFromLobby("local");
                if (succeed) service.Completion.SetResult(true);
                else service.Completion.SetException(new Exception("Service unavailable"));
                Assert.That(await pending, Is.EqualTo(succeed));
                if (succeed) Assert.That(manager.connectedLobby, Is.Null);
                else Assert.That(manager.connectedLobby, Is.SameAs(room));
            }
            finally { UnityEngine.Object.DestroyImmediate(item); }
        }

        [Test]
        public async Task RemovingNoRoomIsAlreadyComplete()
        {
            var item = new GameObject("No room");
            item.SetActive(false);
            try
            {
                var manager = item.AddComponent<RemovalTestLobbyManager>();
                Assert.That(await manager.RemovePlayerFromLobby("local"), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(item); }
        }
    }
}
