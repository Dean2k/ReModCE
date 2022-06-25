using ExitGames.Client.Photon;
using Il2CppSystem.Collections.Generic;
using Photon.Realtime;
using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.VRChat;
using ReModCE_ARES.Core;
using ReModCE_ARES.Loader;
using ReModCE_ARES.Managers;
using UnityEngine;
using VRC.Core;
using Player = VRC.Player;

namespace ReModCE_ARES.Components
{
    internal class AntiBlockComponent : ModComponent
    {
        private static ConfigValue<bool> AntiBlockEnabled;
        private ReMenuToggle _antiblockToggle;

        public AntiBlockComponent()
        {
            AntiBlockEnabled = new ConfigValue<bool>(nameof(AntiBlockEnabled), true);
            AntiBlockEnabled.OnValueChanged += () => _antiblockToggle.Toggle(AntiBlockEnabled);

        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            base.OnUiManagerInit(uiManager);

            var menu = uiManager.MainMenu.GetMenuPage(Page.PageNames.Protections);

            _antiblockToggle = menu.AddToggle("Anti-Block",
                "Enable / Disable the anti-block", AntiBlockEnabled.SetValue,
                AntiBlockEnabled);
        }

        public override bool OnEventPatch(ref EventData __0)
        {
            if (__0.Code == 33)
            {
                try
                {
                    Dictionary<byte, Il2CppSystem.Object> moderationData = __0.Parameters[__0.CustomDataKey].Cast<Dictionary<byte, Il2CppSystem.Object>>();

                    byte moderationType = moderationData[0].Unbox<byte>();

                    switch (moderationType)
                    {

                        case 21:
                            {
                                if (moderationData.ContainsKey(1) == true)
                                {
                                    bool isBlocked = moderationData[10].Unbox<bool>();
                                    PlayerDetails playerDetails = Wrapper.GetPlayerInformationById(moderationData[1].Unbox<int>());

                                    if (playerDetails != null)
                                    {
                                        if (isBlocked)
                                        {
                                            VRCUiManagerEx.Instance.QueueHudMessage(
                                                playerDetails.displayName + " has blocked you.", Color.red);
                                            ReLogger.Msg(playerDetails.displayName + " has blocked you.", Color.red);
                                            ReModCE_ARES.LogDebug(playerDetails.displayName + " has blocked you.");

                                            if (AntiBlockEnabled)
                                            {
                                                return false;
                                            }
                                        }
                                        else
                                        {
                                            if (playerDetails.displayName !=
                                                Wrapper.GetLocalVRCPlayer().prop_VRCPlayerApi_0.displayName)
                                            {
                                                // bugged atm so get rid of overlay message
                                                //VRCUiManagerEx.Instance.QueueHudMessage(
                                                //    playerDetails.displayName + " has unblocked or unmuted you.",
                                                //    Color.green);
                                                ReLogger.Msg(playerDetails.displayName + " has unblocked or unmuted you.",
                                                    Color.green);
                                                ReModCE_ARES.LogDebug(playerDetails.displayName +
                                                                  " has unblocked or unmuted you.");
                                            }
                                        }
                                    }
                                }

                                break;
                            }
                    }
                }
                catch { }
            }

            return true;
        }

        public override void OnPlayerLeft(Player player)
        {
            if (player == null) return;
            if (player.field_Private_APIUser_0 == null) return;
            if (Wrapper.playerList == null) return;
            try
            {
                if (Wrapper.playerList.ContainsKey(player.prop_VRCPlayerApi_0.displayName))
                {
                    Wrapper.playerList.Remove(player.prop_VRCPlayerApi_0.displayName);
                }
            }
            catch { }
        }

        public override void OnPlayerJoined(Player player)
        {
            bool isLocalPlayer = player.prop_APIUser_0.id == APIUser.CurrentUser.id;
            PlayerDetails info = new PlayerDetails
            {
                id = player.prop_APIUser_0.id,
                displayName = player.prop_APIUser_0.displayName,
                isLocalPlayer = isLocalPlayer,
                isInstanceMaster = player.prop_VRCPlayerApi_0.isMaster,
                isVRUser = player.prop_VRCPlayerApi_0.IsUserInVR(),
                isQuestUser = player.prop_APIUser_0.last_platform != "standalonewindows",
                blockedLocalPlayer = false,

                player = player,
                playerApi = player.prop_VRCPlayerApi_0,
                vrcPlayer = player.prop_VRCPlayer_0,
                apiUser = player.prop_APIUser_0,
                networkBehaviour = player.prop_VRCPlayer_0,
            };
            if (!Wrapper.playerList.ContainsKey(info.displayName))
            {
                Wrapper.playerList.Add(info.displayName, info);
            }
        }

        public override void OnEnterWorld(ApiWorld world, ApiWorldInstance instance)
        {
            Wrapper.playerList.Clear();
        }
    }
}
