using MelonLoader;
using ReModCE_ARES.Loader;
using ReModCE_ARES.Managers;
using ReModCE_ARES.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using VRC.UI;

namespace ReModCE_ARES.ApplicationBot
{
    public class Bot : VRCModule
    {
        private const float MoveSpeed = 0.1f;

        public static void MakeBot(int Profile, bool gpu)
        {
            if (gpu)
            {
                Process.Start(Directory.GetCurrentDirectory() + "\\VRChat.exe", $"--profile={Profile} --fps=25 --no-vr -DaddyUwU -Number{Profile} -nolog %2");
            }
            else
            {
                Process.Start(Directory.GetCurrentDirectory() + "\\VRChat.exe", $"--profile={Profile} --fps=25 --no-vr -batchmode -DaddyUwU -Number{Profile} -nographics -no-stereo-rendering -nolog %2");
            }
        }

        private static GameObject _socialMenuInstance;

        public static GameObject GetSocialMenuInstance()
        {
            if (_socialMenuInstance == null)
            {
                _socialMenuInstance = GameObject.Find("UserInterface/MenuContent/Screens");
            }
            return _socialMenuInstance;
        }

        private static Dictionary<string, Action<string>> Commands = new Dictionary<string, Action<string>>()
        {
            { "JoinWorld", (WorldID) => {
                Console.WriteLine("[Client] Joining World " + WorldID);
                if (RoomExtensions.Current_World != null)
                    Networking.GoToRoom(WorldID);
            } },

            {
            "Follow",(UserID =>
                {
                  if (UserID != string.Empty)
                  {
                    foreach (Player player in Wrapper.GetAllPlayers())
                    {
                      if (player.field_Private_APIUser_0.id == UserID){
                        FollowTargetPlayer = player;
                      }
                    }
                  }
                  ReLogger.Msg("Follow Target Set To " + FollowTargetPlayer.field_Private_APIUser_0.displayName, ConsoleColor.DarkBlue);
                })
            },

             {
            "Mimic",(UserID =>
                {
                  Event7Target = UserID == string.Empty ? string.Empty : UserID;
                  if (UserID != string.Empty)
                  {
                    foreach (Player allPlayer in Wrapper.GetAllPlayers())
                    {
                      if (allPlayer.field_Private_APIUser_0.id == UserID)
                            {
                                Event7TargetPlayer = allPlayer;
                                ReModCE_ARES.blockEvent7FromSending = true;
                            }
                    }
                  }
                  else
                    Event7TargetPlayer = null;
                  ReModCE_ARES.blockEvent7FromSending = false;
                  ReLogger.Msg("Copy Target Set To " + Event7TargetPlayer.field_Private_APIUser_0.displayName, ConsoleColor.DarkBlue);
                })
            },


            { "Kill", (Number) => {
                 if (ReModCE_ARES.NumberBot.Contains(Number))
                 {
                     Application.Quit();
                 }
            } },

            { "StopFollow", (bah) => {
                if (_sitOn)
                {
                    RemoveSetGravity();
                    _sitOn = false;
                }
                OrbitTarget = null;
                FollowTargetPlayer = null;
                Event7TargetPlayer = null;
                  ReModCE_ARES.blockEvent7FromSending = false;
            } },

            { "SitOn", (UserID) => {
                StandardSetup(UserID);
            } },

            { "OrbitSelected", (UserID) => {
                if (_sitOn)
                {
                    RemoveSetGravity();
                    _sitOn = false;
                }
                OrbitTarget = UserID == string.Empty ? null : PlayerExtensions.GetPlayerByUserID(UserID);
            } },

            { "ClickTP", (Position) => {
                if (PlayerExtensions.LocalVRCPlayer != null) {
                    string[] Split = Position.Split(':');
                    float X = float.Parse(Split[0]);
                    float Y = float.Parse(Split[1]);
                    float Z = float.Parse(Split[2]);
                    PlayerExtensions.LocalVRCPlayer.transform.position = new Vector3(X, Y, Z);
                }
            } },

            { "SetAvatar", (AvatarID) => {
                new ApiAvatar { id = AvatarID }.Get(new Action<ApiContainer>(x =>
                {
                    GetSocialMenuInstance().transform.Find("Avatar").GetComponent<PageAvatar>().field_Public_SimpleAvatarPedestal_0.field_Internal_ApiAvatar_0 = x.Model.Cast<ApiAvatar>();
                    GetSocialMenuInstance().transform.Find("Avatar").GetComponent<PageAvatar>().ChangeToSelectedAvatar();
                }), new Action<ApiContainer>(x =>
                {
                    MelonLogger.Msg($"Failed to change to avatar: {AvatarID} | Error Message: {x.Error}");
                }));
            } },

            { "RotateY", (Y) => {
                if (PlayerExtensions.LocalVRCPlayer != null) {
                    PlayerExtensions.LocalVRCPlayer.transform.Rotate(new Vector3(0, float.Parse(Y), 0));
                }
            } },

            { "EventCachingDCToggle", (Enabled) => {
                EventCachingDC = Enabled != string.Empty;
            } },

            { "SpinbotToggle", (Enabled) => {
                Spinbot = Enabled != string.Empty;
            } },

            { "SpinbotSpeed", (Speed) => {
                SpinbotSpeed = int.Parse(Speed);
            } },

            { "MoveUp", (Empty) => {
                MovePlayer(Camera.transform.up * MoveSpeed);
            } },

            { "MoveDown", (Empty) => {
                MovePlayer(Camera.transform.up * -MoveSpeed);
            } },

            { "MoveForwards", (Empty) => {
                MovePlayer(Camera.transform.forward * MoveSpeed);
            } },

            { "MoveBackwards", (Empty) => {
                MovePlayer(Camera.transform.forward * -MoveSpeed);
            } },

            { "MoveRight", (Empty) => {
                MovePlayer(Camera.transform.right * MoveSpeed);
            } },

            { "MoveLeft", (Empty) => {
                MovePlayer(Camera.transform.right * -MoveSpeed);
            } },

            { "Earrape", (Enabled) => {
                PlayerExtensions.SetGain(Enabled == string.Empty ? 1f : float.MaxValue);
            } },

            { "SetTargetFramerate", (Framerate) => {
                if (int.TryParse(Framerate, out int n))
                    Application.targetFrameRate = n;
            } },
        };

        private static GameObject Camera => ObjectExtensions.GetPlayerCamera;

        private static void MovePlayer(Vector3 pos)
        {
            if (PlayerExtensions.LocalVRCPlayer != null)
                PlayerExtensions.LocalVRCPlayer.transform.position += pos;
        }

        public static void ReceiveCommand(string Command)
        {
            var Index = Command.IndexOf(" ");
            var CMD = Command.Substring(0, Index); // Grabbing the command
            var Parameters = Command.Substring(Index + 1); // Grabbing the parameters
            HandleActionOnMainThread(() => { Commands[CMD].Invoke(Parameters); }); // Calling the Action<string> related to the command
        }

        private static void HandleActionOnMainThread(Action action)
        {
            LastActionOnMainThread = action;
        }

        private static Player OrbitTarget;
        private static Action LastActionOnMainThread;
        private static bool EventCachingDC = false;
        private static bool Spinbot = false;
        private static int SpinbotSpeed = 20;

        public override void OnUpdate()
        {
            if (ReModCE_ARES.IsBot)
            {
                if (LastActionOnMainThread != null)
                {
                    LastActionOnMainThread();
                    LastActionOnMainThread = null;
                }
                HandleBotFunctions();
            }
        }

        private static IEnumerator RamClearLoop()
        {
            for (; ; )
            {
                yield return new WaitForSeconds(5f);
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }
        }

        public static IEnumerator EventCachingDCLoop()
        {
            for (; ; )
            {
                try
                {
                    if (EventCachingDC)
                    {
                        PlayerExtensions.SendVRCEvent(new VRC_EventHandler.VrcEvent()
                        {
                            ParameterInt = 1,
                            ParameterFloat = 0,
                            EventType = VRC_EventHandler.VrcEventType.SendRPC,
                            ParameterString = "詬ጋ蛸╫犯Ꙃ츆�繙ໞ㡕蚌낳猻샥⋛濊鈍鶬堾ꅊ속ᩂ⿴刏욫鮞⁢혭볖縱䶽ṳ翜ⶩ綮枞者⿗�≶珓촢켁沠鯙妆】℡�෺ﲴ짓焸뾱ፘᴷ礌廸럻⭍萨ᨧ졀᳿㍦뻈ꉢ웦栏ꘜず羧牞㟈긑쪰⫞ᙓ힁琄ጃॆ拤둮⫇蒭랊ꑃ雞辚筠ㄆᯟ皫፧ᤇᷠ犔ꨦ鿁⯊躇�뷕起该持᧩糓廰䜠�䄹㙔╀ை䶏ٟ㛶⸃斩䃎꓌Ṝ跏Ꮚ펵頰촻⩱⪪ﲉ轊‣缳뺁棓倮燕㉟祐ᨿ狶귭뭛䚐⦩켌揾篧料౉᪕㾪備첤㴹䶘婷딑벶Ḹ䚦꺁徭祃칒᾽◇얰愔蜵⨐�▌뢩鑰Ꙍ쫊△쏘먧겥ⰲ嶥櫜ፑ展찛ෳ媃즗踧ꛂ⭠퀹餽ⶭ堋鿥稊⎸縤㙑ᙅ븬笂꧲覧鍊卙�꓏첾⫅鈆凞虾衜ṏ휨뎛ຌ荕⸬위䗯쾮顜쮘湙셽귣禜끔呷갯ⷊ項酩麏佩삮ꐲ",
                        }, VRC_EventHandler.VrcBroadcastType.Always,
                        PlayerExtensions.LocalVRCPlayer.gameObject);
                    }
                }
                catch { }
                yield return new WaitForSeconds(0.5f);
            }
        }

        private static bool EmojiSpam = false;
        private static string _PrefabName = "";

        private static void HandleBotFunctions()
        {
            if (EmojiSpam)
                SendEmoji(58);

            if (OrbitTarget != null && PlayerExtensions.LocalVRCPlayer != null)
            {
                Physics.gravity = new Vector3(0, 0, 0);
                alpha += Time.deltaTime * OrbitSpeed;
                PlayerExtensions.LocalPlayer.transform.position = new Vector3(OrbitTarget.transform.position.x + a * (float)Math.Cos(alpha), OrbitTarget.transform.position.y, OrbitTarget.transform.position.z + b * (float)Math.Sin(alpha));
            }

            if (Spinbot && PlayerExtensions.LocalVRCPlayer != null)
            {
                PlayerExtensions.LocalVRCPlayer.transform.Rotate(new Vector3(0f, SpinbotSpeed, 0f));
            }

            if (_PrefabName != "")
            {
                SpawnPrefab(_PrefabName);
            }
            if (_sitOn)
            {
                TeleportToIUser(target);
            }
        }

        private static void SpawnPrefab(string Name)
        {
            GameObject obj = PlayerExtensions.InstantiatePrefab(Name,
                PlayerExtensions.LocalVRCPlayer.transform.position,
                new Quaternion(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue));

            obj.SetActive(false);
        }

        private static void SendEmoji(int Number)
        {
            Networking.RPC(RPC.Destination.All,
                PlayerExtensions.LocalVRCPlayer.gameObject,
                "SpawnEmojiRPC",
                new Il2CppSystem.Object[1] {
                    new Il2CppSystem.Int32 {
                        m_value = Number
                    }.BoxIl2CppObject()
                });
        }

        public static float OrbitSpeed = 5f;
        public static float alpha = 0f;
        public static float a = 1f;
        public static float b = 1f;
        public static float Range = 1f;
        public static float Height = 0f;
        public static VRCPlayer currentPlayer;
        public static Player selectedPlayer;

        public override void OnStart()
        {
            MelonLogger.Msg("OnStart Override Called");
            EventCachingDCLoop().Start();
            RamClearLoop().Start();
            SocketConnection.Client();
            return;
        }

        public override void OnLevelWasLoaded(int buildIndex, string sceneName)
        {
            // will put a message to respond to server here
        }

        public static void StandardSetup(string userId)
        {
            var user = PlayerExtensions.GetPlayerByUserID(userId)._vrcplayer;
            if (user == null)
                return;

            target = user;
            SetGravity();
            _sitOn = true;
            TeleportToIUser(user);
        }

        private static void SetGravity()
        {
            if (Physics.gravity == Vector3.zero) return;

            _originalGravity = Physics.gravity;
            Physics.gravity = Vector3.zero;
        }

        private static void RemoveSetGravity()
        {
            if (_originalGravity == Vector3.zero) return;
            Physics.gravity = _originalGravity;
        }

        private static bool _sitOn = false;
        private static VRCPlayer target;
        private static Vector3 _originalGravity;
        private static Vector3 _playerLastPos;

        public static byte[] E7Data;
        public static string Event7Target = "";
        public static VRC.Player Event7TargetPlayer = null;

        private static void TeleportToIUser(VRCPlayer user)
        {
            try
            {
                Vector3 playerPosition = new Vector3();
                playerPosition = user.field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Head).position + new Vector3(0, 0.1f, 0);

                var localTransform = VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;
                if (_playerLastPos != null)
                {
                    if (_playerLastPos != localTransform.position)
                    {
                        localTransform.position = playerPosition;
                    }
                }

                _playerLastPos = playerPosition;
            }
            catch { target = null; RemoveSetGravity(); }
        }
        public static VRC.Player FollowTargetPlayer = null;
    }
}