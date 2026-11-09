// Loads the real shared scene (Professor_MR) as a classmate or as the professor and uses the new classmate controls through a fake
// mouse: the start screen, the Mute and Raise hand buttons, the tap-to-name card, and the layout. No lobby service, Vivox or microphone
// is involved: the joiner and the voice service are fakes, and the scene hosts a local test room.
// Authority under test: none for the buttons (UI); the hand itself is decided by the server (see HandBoardTests).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DIYsect.Classroom;
using DIYsect.ClassmateConnection;
using DIYsect.ClassmateControls;
using DIYsect.ClassRoster;
using TMPro;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using XRMultiplayer;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace DIYsect.Networking.Tests
{
    public class ClassmateControlsSceneTests
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly InputTestFixture _input = new InputTestFixture();
        private Mouse _mouse;
        private Touchscreen _touch;

        private sealed class FakeJoiner : IClassmateJoiner
        {
            public bool WaitingForPress { get; set; }
            public bool Connected { get; set; }
            public bool CodeRejected { get; set; }
            public int Presses { get; private set; }
            public string LastCode { get; private set; }

            public void PressJoin()
            {
                Presses++;
                WaitingForPress = false;
            }

            public bool JoinWithCode(string typedCode)
            {
                if (!XRMultiplayer.RoomInvite.TryNormalizeCode(typedCode, out string code))
                    return false;
                LastCode = code;
                WaitingForPress = false;
                return true;
            }
        }

        private sealed class FakeVoice : IVoiceService
        {
            public bool IsMuted { get; private set; } = true;
            public bool HasMicPermission { get; set; }
            public bool MayUnmute { get; set; } = true;
            public int SpeakRequests { get; private set; }
            public void RequestToSpeak() => SpeakRequests++;
            public int Requests { get; private set; }
            private Action<bool> _pending;
            public event Action Changed;

            public void RequestMicPermission(Action<bool> done)
            {
                Requests++;
                _pending = done;
            }

            public void SetMuted(bool muted)
            {
                IsMuted = muted;
                Changed?.Invoke();
            }

            public void Answer(bool allowed)
            {
                HasMicPermission = allowed;
                _pending?.Invoke(allowed);
            }
        }

        [SetUp]
        public void SetUpInput()
        {
            _input.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _touch = InputSystem.AddDevice<Touchscreen>();
        }

        [TearDown]
        public void TearDownInput()
        {
            ClassroomRole.OverrideForTests = null;
            _input.TearDown();
        }

        // The scene's module uses the project-wide input actions, which an earlier test may have left off, so it gets a fresh set.
        private static void UseFreshUiActions()
        {
            var module = InScene<EventSystem>().GetComponent<InputSystemUIInputModule>();
            module.enabled = false;
            module.AssignDefaultActions();
            module.enabled = true;
        }

        private static void ClearStrayEventSystems()
        {
            foreach (EventSystem stray in UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                UnityEngine.Object.DestroyImmediate(stray.gameObject);
        }

        private IEnumerator Load(ClassroomRoleKind role)
        {
            ClearStrayEventSystems();
            LogAssert.ignoreFailingMessages = true;
            ClassroomRole.OverrideForTests = role;
#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield break;
#endif
            if (role == ClassroomRoleKind.Classmate)
                UseFreshUiActions();
            for (int i = 0; i < 10; i++)
                yield return null;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            LogAssert.ignoreFailingMessages = true; // the host's voice chat logs while it is torn down
            foreach (NetworkManager manager in UnityEngine.Object.FindObjectsByType<NetworkManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                manager.Shutdown();
                UnityEngine.Object.DestroyImmediate(manager.gameObject);
            }
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            Scene empty = SceneManager.CreateScene("AfterClassmateControls" + Guid.NewGuid());
            SceneManager.SetActiveScene(empty);
            if (scene.IsValid() && scene.isLoaded)
                yield return SceneManager.UnloadSceneAsync(scene);
            yield return null;
        }

        private static T InScene<T>() where T : Component
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(ScenePath).GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null)
                    return found;
            }
            Assert.Fail($"The shared scene has no {typeof(T).Name}.");
            return null;
        }

        private static RectTransform FindUi(string path)
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(ScenePath).GetRootGameObjects())
            {
                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                    if (rect.name == path)
                        return rect;
            }
            Assert.Fail($"The shared scene has no UI object named {path}.");
            return null;
        }

        private static Rect ScreenRect(RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static Vector2 Centre(Button button) => ScreenRect((RectTransform)button.transform).center;

        // The canvas rescales a frame after input starts, so aim until the spot stops moving, then click.
        private IEnumerator Click(Button button)
        {
            Vector2 last = new Vector2(-1f, -1f);
            for (int i = 0; i < 20; i++)
            {
                Vector2 centre = Centre(button);
                _input.Set(_mouse.position, centre);
                yield return null;
                if (centre == last && Centre(button) == centre)
                    break;
                last = centre;
            }
            _input.Press(_mouse.leftButton);
            yield return null;
            _input.Release(_mouse.leftButton);
            yield return null;
            yield return null;
        }

        // This device hosts a local test room (no cloud services), so the scene's own body spawns and the classmate counts as in the room.
        private IEnumerator StartTestRoom()
        {
            NetworkManager manager = UnityEngine.Object.FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
            Assert.IsTrue(manager.StartHost(), "The test room did not start.");
            var hands = InScene<HandBoard>();
            float end = Time.realtimeSinceStartup + 8f;
            while (!hands.IsSpawned && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsTrue(hands.IsSpawned, "The scene's body never spawned.");
            // The bottom bar looks at the connection a few times a second, by the clock, not by frame count: wait for it, however fast the frames run.
            var bar = InScene<ClassmateBottomBar>();
            if (bar.gameObject.activeInHierarchy)
            {
                end = Time.realtimeSinceStartup + 8f;
                while (!bar.IsShowing && Time.realtimeSinceStartup < end)
                    yield return null;
            }
            yield return null;
        }

        private static Button ButtonNamed(string name)
        {
            foreach (Button button in InScene<ClassmateBottomBar>().GetComponentsInChildren<Button>(true))
                if (button.name == name)
                    return button;
            Assert.Fail($"The bottom bar has no {name}.");
            return null;
        }

        // ---- Start screen ----

        [UnityTest]
        public IEnumerator InTheEditorTheStartScreenStaysHiddenBecauseTheAutoJoinIsOff()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            var screen = InScene<ClassmateStartScreen>();
            Assert.IsFalse(screen.IsShowing, "ClassmateAutoJoin switches itself off in the editor, so nothing waits for a press.");
            Assert.IsTrue(InScene<ClassmateAutoJoin>().WaitForJoinPress, "The shared scene's auto join must wait for the start screen.");
        }

        [UnityTest]
        public IEnumerator TheStartScreenShowsAndJoinsOnlyWhenTheBigButtonIsPressed()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            var screen = InScene<ClassmateStartScreen>();
            var joiner = new FakeJoiner { WaitingForPress = true };
            screen.SetJoiner(joiner);
            yield return null;
            yield return null;

            Assert.IsTrue(screen.IsShowing, "The classmate starts on the start screen.");
            Assert.IsTrue(DIYsect.Learning.ChecklistHud.Suppressed, "The checklist line must not draw over the Join page.");
            Assert.IsFalse(UnityEngine.Object.FindFirstObjectByType<XRMultiplayer.ClassmateStatusLabel>(FindObjectsInactive.Include).ShowStatusLine,
                "The connection status line must not draw over the Join page.");
            Assert.AreEqual(0, joiner.Presses, "Nothing joins before the press.");
            Button join = FindUi("JoinTheRoomButton").GetComponent<Button>();
            Assert.AreEqual(ClassmateStartScreen.JoinLabel, join.GetComponentInChildren<TMP_Text>().text);

            yield return Click(join);

            Assert.AreEqual(1, joiner.Presses, "The press asks the joiner to join the room.");
            Assert.AreEqual(StartScreenPhase.Joining, screen.Phase);
            Assert.AreEqual(ClassmateStartScreen.JoiningLabel, join.GetComponentInChildren<TMP_Text>().text);
            Assert.IsFalse(join.interactable, "A second press while joining does nothing.");

            joiner.Connected = true;
            yield return null;
            yield return null;
            Assert.IsFalse(screen.IsShowing, "The start screen closes once the classmate is in the room.");
            Assert.IsFalse(DIYsect.Learning.ChecklistHud.Suppressed, "The checklist line shows again once the classmate is in the room.");
            Assert.IsTrue(UnityEngine.Object.FindFirstObjectByType<XRMultiplayer.ClassmateStatusLabel>(FindObjectsInactive.Include).ShowStatusLine,
                "The connection status line shows again once the classmate is in the room.");
        }

        [UnityTest]
        public IEnumerator AnInviteLinkSkipsTheStartScreen()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            var screen = InScene<ClassmateStartScreen>();
            // The link already started the join, so the joiner no longer waits for a press.
            var joiner = new FakeJoiner { WaitingForPress = false };
            screen.SetJoiner(joiner);
            yield return null;
            yield return null;

            Assert.IsFalse(screen.IsShowing);
            Assert.AreEqual(0, joiner.Presses);
        }

        [UnityTest]
        public IEnumerator ALinkOrATypedCodeCountsAsPressingJoinOnTheRealAutoJoin()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            var join = InScene<ClassmateAutoJoin>();
            Assert.IsFalse(join.JoinPressed);
            Assert.IsTrue(RoomInvite.TryParseLink("diysect://join?code=KHC8QH", out string code));
            join.JoinWithCode(code);
            Assert.IsTrue(join.JoinPressed, "An invite link joins straight away.");
        }

        [UnityTest]
        public IEnumerator TheCodeBoxOnTheStartScreenJoinsWithTheTypedCodeAndRejectsABadOne()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            var screen = InScene<ClassmateStartScreen>();
            var joiner = new FakeJoiner { WaitingForPress = true };
            screen.SetJoiner(joiner);
            yield return null;
            var field = FindUi("CodeField").GetComponent<TMP_InputField>();
            Button codeButton = FindUi("JoinWithCodeButton").GetComponent<Button>();

            field.text = "abc";
            yield return Click(codeButton);
            Assert.IsNull(joiner.LastCode);
            Assert.AreEqual(ClassmateStartScreen.BadCodeText, FindUi("StartMessage").GetComponent<TMP_Text>().text);
            Assert.IsTrue(screen.IsShowing);

            field.text = "khc 8qh";
            yield return Click(codeButton);
            Assert.AreEqual("KHC8QH", joiner.LastCode);
            Assert.AreEqual(StartScreenPhase.Joining, screen.Phase);
        }

        [UnityTest]
        public IEnumerator TheStartScreenIsOnlyOnTheClassmatesHalf()
        {
            yield return Load(ClassroomRoleKind.Professor);
            Assert.IsFalse(InScene<ClassmateStartScreen>().gameObject.activeInHierarchy, "The professor's headset never shows a classmate screen.");
            Assert.IsFalse(InScene<ClassmateBottomBar>().gameObject.activeInHierarchy);
            Assert.IsFalse(InScene<ClassmateMicButton>().gameObject.activeInHierarchy);
            Assert.IsFalse(InScene<ClassmateHandButton>().gameObject.activeInHierarchy);
        }

        // ---- Mute ----

        [UnityTest]
        public IEnumerator TheBottomBarShowsOnlyInTheRoom()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            var bar = InScene<ClassmateBottomBar>();
            Assert.IsFalse(bar.IsShowing, "Mute and Raise hand mean nothing before the classmate is in the room.");
            yield return StartTestRoom();
            Assert.IsTrue(bar.IsShowing);
        }

        [UnityTest]
        public IEnumerator TheMuteButtonStartsMutedAsksForTheMicOnceAndTogglesWithAFakeVoiceService()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            yield return StartTestRoom();
            var mic = InScene<ClassmateMicButton>();
            var voice = new FakeVoice();
            mic.SetService(voice);
            Button button = ButtonNamed("MuteButton");

            Assert.AreEqual(MicButtonModel.UnmuteLabel, mic.Label);
            Assert.IsFalse(mic.IsLive, "A classmate starts muted.");

            yield return Click(button);
            Assert.AreEqual(1, voice.Requests, "The first unmute asks for the microphone.");
            Assert.AreEqual(MicButtonModel.AskingLabel, mic.Label);
            voice.Answer(true);
            Assert.IsTrue(mic.IsLive);
            Assert.AreEqual(MicButtonModel.MuteLabel, mic.Label);

            yield return Click(button);
            Assert.IsFalse(mic.IsLive);
            Assert.AreEqual(MicButtonModel.UnmuteLabel, mic.Label);

            yield return Click(button);
            Assert.AreEqual(1, voice.Requests, "Once allowed, it does not ask again.");
            Assert.IsTrue(mic.IsLive);
        }

        [UnityTest]
        public IEnumerator ARefusedMicStaysMutedAndShowsAShortMessage()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            yield return StartTestRoom();
            var mic = InScene<ClassmateMicButton>();
            var voice = new FakeVoice();
            mic.SetService(voice);

            yield return Click(ButtonNamed("MuteButton"));
            voice.Answer(false);

            Assert.IsFalse(mic.IsLive);
            Assert.AreEqual(MicButtonModel.UnmuteLabel, mic.Label);
            Assert.AreEqual(MicButtonModel.DeniedText, mic.Message);
        }

        // ---- Raise hand ----

        [UnityTest]
        public IEnumerator TheRaiseHandButtonRaisesAndLowersThisClassmatesHand()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            yield return StartTestRoom();
            var board = InScene<HandBoard>();
            var hand = InScene<ClassmateHandButton>();
            Button button = ButtonNamed("RaiseHandButton");
            Assert.AreEqual(HandButtonLabels.Raise, hand.Label);

            yield return Click(button);
            float end = Time.realtimeSinceStartup + 5f;
            while (!board.IsLocalRaised && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsTrue(board.IsLocalRaised, "The server put the hand up.");
            yield return null;
            Assert.AreEqual(HandButtonLabels.Lower, hand.Label);

            yield return Click(button);
            end = Time.realtimeSinceStartup + 5f;
            while (board.IsLocalRaised && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsFalse(board.IsLocalRaised, "Pressing again lowers it.");
            yield return null;
            Assert.AreEqual(HandButtonLabels.Raise, hand.Label);
        }

        [UnityTest]
        public IEnumerator TheProfessorsRosterButtonsLowerHandsFromTheHeadset()
        {
            yield return Load(ClassroomRoleKind.Professor);
            NetworkManager manager = UnityEngine.Object.FindFirstObjectByType<NetworkManager>(FindObjectsInactive.Include);
            Assert.IsTrue(manager.StartHost(), "The test room did not start.");
            var board = InScene<HandBoard>();
            float end = Time.realtimeSinceStartup + 8f;
            while (!board.IsSpawned && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.IsTrue(board.IsSpawned);
            var roster = InScene<ClassRosterPanel>();
            Assert.IsTrue(roster.gameObject.activeInHierarchy, "The professor has the roster.");
            Assert.IsNotNull(roster.transform.Find("LowerNextHand"), "The roster needs a Lower next hand button.");
            Assert.IsNotNull(roster.transform.Find("LowerAllHands"), "The roster needs a Lower all hands button.");

            // The host stands in for a classmate here: any peer's hand is lowered the same way.
            board.RequestSet(true);
            end = Time.realtimeSinceStartup + 5f;
            while (board.Count < 1 && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(1, board.Count);

            roster.LowerNext();
            end = Time.realtimeSinceStartup + 5f;
            while (board.Count > 0 && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(0, board.Count, "Lower next hand takes down the first hand.");

            board.RequestSet(true);
            end = Time.realtimeSinceStartup + 5f;
            while (board.Count < 1 && Time.realtimeSinceStartup < end)
                yield return null;
            roster.LowerAll();
            end = Time.realtimeSinceStartup + 5f;
            while (board.Count > 0 && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(0, board.Count, "Lower all hands takes down every hand.");
        }

        // ---- Layout ----

        [UnityTest]
        public IEnumerator EveryNewControlIsThumbSizedOnScreenInsideTheSafeAreaAndClearOfTheOtherClassmateUi()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            yield return StartTestRoom();
            // Show everything that is normally hidden, so the whole layout is measured at once.
            GameObject bannerPanel = (GameObject)typeof(ReconnectBanner).GetField("_panel", Private).GetValue(InScene<ReconnectBanner>());
            bannerPanel.SetActive(true);
            var toastLabel = (TMP_Text)typeof(PinRejectionToast).GetField("_label", Private).GetValue(InScene<PinRejectionToast>());
            toastLabel.gameObject.SetActive(true);
            var micMessage = (TMP_Text)typeof(ClassmateMicButton).GetField("_message", Private).GetValue(InScene<ClassmateMicButton>());
            micMessage.gameObject.SetActive(true);
            RectTransform card = FindUi("Card");
            card.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            yield return null;

            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            var controls = new Dictionary<string, Rect>
            {
                ["Mute"] = ScreenRect((RectTransform)ButtonNamed("MuteButton").transform),
                ["Raise hand"] = ScreenRect((RectTransform)ButtonNamed("RaiseHandButton").transform),
            };
            var others = new Dictionary<string, Rect>
            {
                ["Reset view"] = ScreenRect((RectTransform)InScene<ClassmateViewReset>().GetComponentInChildren<Button>().transform),
                ["Reconnect banner"] = ScreenRect((RectTransform)bannerPanel.transform),
                ["Pin message"] = ScreenRect(toastLabel.rectTransform),
                ["Mic message"] = ScreenRect(micMessage.rectTransform),
                ["Name card"] = ScreenRect(card),
            };
            var follows = new Dictionary<string, Rect>();
            foreach (string followName in new[] { "FollowProfessorButton", "FollowStudentButton", "BackToMyViewButton" })
                follows[followName] = ScreenRect((RectTransform)FindUi(followName));
            foreach (KeyValuePair<string, Rect> follow in follows)
                others[follow.Key] = follow.Value;

            foreach (KeyValuePair<string, Rect> control in controls)
            {
                Assert.GreaterOrEqual(control.Value.height, Screen.height * 0.08f, $"{control.Key} must be thumb-sized.");
                Assert.GreaterOrEqual(control.Value.width, Screen.height * 0.08f, $"{control.Key} must be thumb-sized.");
                Assert.IsTrue(screen.Contains(control.Value.min) && screen.Contains(control.Value.max), $"{control.Key} must be on screen.");
                foreach (KeyValuePair<string, Rect> other in others)
                    Assert.IsFalse(control.Value.Overlaps(other.Value), $"{control.Key} {control.Value} must not cover {other.Key} {other.Value}.");
            }
            Assert.IsFalse(controls["Mute"].Overlaps(controls["Raise hand"]), "The two bottom buttons must not touch.");

            // The two new lines of text and the card must not sit on the buttons or on each other either.
            foreach (string name in new[] { "Mic message", "Name card" })
            {
                foreach (string other in new[] { "Reset view", "Reconnect banner", "Pin message" })
                    Assert.IsFalse(others[name].Overlaps(others[other]), $"{name} {others[name]} must not cover {other} {others[other]}.");
                foreach (KeyValuePair<string, Rect> follow in follows)
                    Assert.IsFalse(others[name].Overlaps(follow.Value), $"{name} {others[name]} must not cover {follow.Key} {follow.Value} (screen {Screen.width}x{Screen.height}).");
            }
            Assert.IsFalse(others["Pin message"].Overlaps(others["Mic message"]));
            Assert.IsFalse(others["Pin message"].Overlaps(others["Name card"]));
            Assert.IsFalse(others["Mic message"].Overlaps(others["Name card"]));
        }

        [UnityTest]
        public IEnumerator WithoutThePermissionTheMuteButtonAsksToSpeak_AndOpensOnceTheProfessorAllows()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            yield return StartTestRoom();
            var mic = InScene<ClassmateMicButton>();
            var voice = new FakeVoice { MayUnmute = false, HasMicPermission = true };
            mic.SetService(voice);
            Button button = ButtonNamed("MuteButton");

            Assert.AreEqual(MicButtonModel.AskLabel, mic.Label, "Without permission the button offers to ask.");
            yield return Click(button);
            Assert.AreEqual(1, voice.SpeakRequests, "Pressing it asks the professor.");
            Assert.IsFalse(mic.IsLive, "Nobody is heard before the professor allows it.");

            voice.MayUnmute = true;
            for (int i = 0; i < 5; i++)
                yield return null;
            Assert.AreEqual(MicButtonModel.UnmuteLabel, mic.Label, "The label changes by itself when permission arrives.");
            yield return Click(button);
            Assert.IsTrue(mic.IsLive);

            voice.MayUnmute = false;
            for (int i = 0; i < 5; i++)
                yield return null;
            Assert.IsFalse(mic.IsLive, "The microphone closes when the professor takes permission away.");
            Assert.IsTrue(voice.IsMuted);
        }

        [UnityTest]
        public IEnumerator TheNamePageComesFirstAndSendsTheNameBeforeTheJoinPage()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            PlayerPrefs.DeleteKey(ClassmateNameScreen.PrefsKey);
            var names = InScene<ClassmateNameScreen>();
            var joiner = new FakeJoiner { WaitingForPress = true };
            names.SetJoiner(joiner);
            InScene<ClassmateStartScreen>().SetJoiner(joiner);
            yield return null;
            yield return null;

            Assert.IsTrue(names.IsShowing, "The classmate is asked for a name first.");
            var field = FindUi("NameField").GetComponent<TMP_InputField>();
            Button next = ButtonNamed("ContinueButton");

            yield return Click(next);
            Assert.IsTrue(names.IsShowing, "An empty name does not go on.");
            Assert.AreEqual(NameRules.EmptyText, FindUi("NameMessage").GetComponent<TMP_Text>().text);

            field.text = "  Maya   Rao ";
            yield return Click(next);

            Assert.IsTrue(names.Done);
            Assert.IsFalse(names.IsShowing, "The name page goes away after Continue.");
            Assert.AreEqual("Maya Rao", names.SubmittedName);
            Assert.AreEqual("Maya Rao", XRINetworkGameManager.LocalPlayerName.Value, "The name is what the room shows for this player.");
            Assert.IsTrue(InScene<ClassmateStartScreen>().IsShowing, "The Join page is next.");
            PlayerPrefs.DeleteKey(ClassmateNameScreen.PrefsKey);
        }

        [UnityTest]
        public IEnumerator TheProfessorHasAParticipantListWithARowAndAnAllowButtonForEachStudent_AndAClassmateDoesNot()
        {
            yield return Load(ClassroomRoleKind.Professor);
            var panel = InScene<SpeakPanel>();
            Assert.IsTrue(panel.gameObject.activeInHierarchy, "The professor sees who is asking to speak.");
            Assert.GreaterOrEqual(panel.RowCount, 7, "Rows for every student in a room of eight.");
            Assert.IsNotNull(InScene<SpeakBoard>(), "The body carries the speak board.");

            yield return Unload();
            yield return Load(ClassroomRoleKind.Classmate);
            Assert.IsFalse(InScene<SpeakPanel>().gameObject.activeInHierarchy, "A classmate must never see or press the professor's buttons.");
        }

        [UnityTest]
        public IEnumerator TheNewControlsLiveUnderTheSafeAreaPanel()
        {
            yield return Load(ClassroomRoleKind.Classmate);
            foreach (string name in new[] { "MuteButton", "RaiseHandButton", "JoinTheRoomButton", "Card" })
            {
                RectTransform rect = FindUi(name);
                Assert.IsNotNull(rect.GetComponentInParent<SafeAreaFitter>(true), $"{name} must sit under a SafeArea panel.");
            }
        }
    }
}
