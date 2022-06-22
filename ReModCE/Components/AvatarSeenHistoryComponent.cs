using ExitGames.Client.Photon;
using MelonLoader.ICSharpCode.SharpZipLib.GZip;
using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.VRChat;
using ReModCE_ARES.Core;
using ReModCE_ARES.Loader;
using ReModCE_ARES.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using AvatarList = Il2CppSystem.Collections.Generic.List<VRC.Core.ApiAvatar>;

namespace ReModCE_ARES.Components
{
    internal class AvatarSeenHistoryComponent : ModComponent, IAvatarListOwner
    {
        private ReAvatarList _avatarSeenList;
        private readonly List<ReAvatar> _recentSeenAvatars;


        private ConfigValue<bool> AvatarSeenHistoryEnabled;
        private ReMenuToggle _enabledSeenToggle;

        public AvatarSeenHistoryComponent()
        {

            AvatarSeenHistoryEnabled = new ConfigValue<bool>(nameof(AvatarSeenHistoryEnabled), true);
            AvatarSeenHistoryEnabled.OnValueChanged += () =>
            {
                _enabledSeenToggle?.Toggle(AvatarSeenHistoryEnabled);
                _avatarSeenList.GameObject.SetActive(AvatarSeenHistoryEnabled);
            };

            if (File.Exists("UserData/ReModCE_ARES/recent_avatars_seen.bin"))
            {
                try
                {
                    _recentSeenAvatars =
                        BinaryGZipSerializer.Deserialize("UserData/ReModCE_ARES/recent_avatars_seen.bin") as List<ReAvatar>;
                }
                catch (GZipException)
                {
                    ReModCE_ARES.LogDebug($"Your recent seen avatars file seems to be corrupted. I renamed it for you, so this error doesn't happen again.");
                    ReLogger.Error($"Your recent seen avatars file seems to be corrupted. I renamed it for you, so this error doesn't happen again.");
                    File.Delete("UserData/ReModCE_ARES/recent_avatars_seen.bin.corrupted");
                    File.Move("UserData/ReModCE_ARES/recent_avatars_seen.bin", "UserData/ReModCE_ARES/recent_avatars_seen.bin.corrupted");
                    _recentSeenAvatars = new List<ReAvatar>();
                }
            }
            else
            {
                _recentSeenAvatars = new List<ReAvatar>();
            }
        }

        public override void OnUiManagerInitEarly()
        {

            _avatarSeenList = new ReAvatarList("Recently Seen", this, true, false);

        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            var menu = uiManager.MainMenu.GetMenuPage(Page.PageNames.Avatars);

            _enabledSeenToggle = menu.AddToggle("Avatar Seen History", "Enable/Disable avatar Seen history",
               AvatarSeenHistoryEnabled.SetValue, AvatarSeenHistoryEnabled);
        }

        public override void OnPlayerJoined(Player player)
        {
            if (player == null) return;
            if (!AvatarSeenHistoryEnabled) return;

            VRCPlayer vrcPlayer = player._vrcplayer;

            AddAvatarToSeenHistory(vrcPlayer.GetPlayer().GetApiAvatar());
        }

        public override bool OnEventPatch(ref EventData __0)
        {
            try
            {
                if (__0.Code == null)
                {
                    return false;
                }
            }
            catch { return false; }

            if (__0.Code == 42)
            {
                try
                {
                    if (AvatarSeenHistoryEnabled)
                    {
                        PlayerDetails playerDetails = Wrapper.GetPlayerInformationById(__0.sender);
                        AddAvatarToSeenHistory(playerDetails.vrcPlayer.field_Private_ApiAvatar_0);
                    }
                }
                catch { };
            }
            return true;
        }


        private bool IsAvatarInHistorySeen(string id)
        {
            return _recentSeenAvatars.FirstOrDefault(a => a.AvatarID == id) != null;
        }

        private void AddAvatarToSeenHistory(ApiAvatar avatar)
        {
            if (avatar == null)
                return;

            if (IsAvatarInHistorySeen(avatar.id))
            {
                _recentSeenAvatars.RemoveAll(a => a.AvatarID == avatar.id);
            }

            _recentSeenAvatars.Insert(0, new ReAvatar(avatar));

            if (_recentSeenAvatars.Count > 100)
            {
                _recentSeenAvatars.Remove(_recentSeenAvatars.Last());
            }

            SaveSeenAvatarsToDisk();

            _avatarSeenList.RefreshAvatars();
        }

        private void SaveSeenAvatarsToDisk()
        {
            BinaryGZipSerializer.Serialize(_recentSeenAvatars, "UserData/ReModCE_ARES/recent_avatars_seen.bin");
        }

        public AvatarList GetAvatars(ReAvatarList avatarList)
        {
            var list = new AvatarList();
            foreach (var avi in _recentSeenAvatars.Distinct().Select(x => x.AsApiAvatar()).ToList())
            {
                list.Add(avi);
            }
            return list;
        }


        public void Clear(ReAvatarList avatarList)
        {
            _recentSeenAvatars.Clear();
            SaveSeenAvatarsToDisk();
            avatarList.RefreshAvatars();
        }
    }
}
