using ReModCE_ARES.SDK;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using VRC.UI;

namespace ReModCE_ARES.ApplicationBot
{
    internal static class PlayerExtensions
    {
        public static VRCPlayer LocalVRCPlayer => VRCPlayer.field_Internal_Static_VRCPlayer_0;
        public static Player LocalPlayer => Player.prop_Player_0;
        public static APIUser LocalAPIUser => APIUser.CurrentUser;
        public static USpeaker LocalUSpeaker => LocalVRCPlayer.prop_USpeaker_0;
        public static VRCPlayerApi LocalVRCPlayerAPI => LocalVRCPlayer.field_Private_VRCPlayerApi_0;
        public static PlayerManager PManager => PlayerManager.field_Private_Static_PlayerManager_0;
        public static List<Player> AllPlayers => PManager.field_Private_List_1_Player_0.ToArray().ToList();

        public static ApiAvatar GetAPIAvatar(this Player player)
        {
            return player.prop_ApiAvatar_0;
        }

        public static PlayerNet GetPlayerNet(this Player player)
        {
            return player.prop_PlayerNet_0;
        }

        public static VRCPlayerApi GetVRCPlayerApi(this Player player)
        {
            return player.field_Private_VRCPlayerApi_0;
        }

        public static APIUser GetAPIUser(this Player player)
        {
            return player.field_Private_APIUser_0;
        }

        public static bool isBlocked(this Player player)
        {
            return ModerationManagerExtension.GetIsBlocked(player.GetAPIUser().id);
        }

        public static void Block(this Player player, bool block)
        {   // (if blocked, and trying to block || if unblocked and trying to unblock)
            if (!((player.isBlocked() && block) || (!player.isBlocked() && !block)))
                player.ToggleBlock();
        }

        public static USpeaker GetUSpeaker(this Player player) =>
            player.prop_USpeaker_0;

        public static void SetVolume(this Player player, float vol) =>
            player.GetUSpeaker().field_Private_SimpleAudioGain_0.field_Public_Single_0 = vol;

        public static void Teleport(this Player player)
        {
            LocalVRCPlayer.transform.position = player.GetVRCPlayer().transform.position;
        }

        public static VRCPlayer GetVRCPlayer(this Player player)
        {
            return player._vrcplayer;
        }

        public static string GetName(this Player player)
        {
            return player.GetAPIUser().displayName;
        }

        public static float LocalGain
        {
            get { return USpeaker.field_Internal_Static_Single_1; }
            set { USpeaker.field_Internal_Static_Single_1 = value; }
        }

        public static float AllGain
        {
            get { return USpeaker.field_Internal_Static_Single_0; }
            set { USpeaker.field_Internal_Static_Single_0 = value; }
        }

        public static float DefaultGain => 1f;
        public static float MaxGain => float.MaxValue;

        public static bool IsMaster(this Player player)
        {
            return player.GetVRCPlayerApi().isMaster;
        }

        public static int GetActorNumber(this Player player)
        {
            return player.GetVRCPlayerApi().playerId;
        }

        public static int GetPlayerFrames(this Player player)
        {
            return player.GetPlayerNet().prop_Byte_0 != 0 ? (int)(1000f / player.GetPlayerNet().prop_Byte_0) : 0;
        }

        public static int GetPlayerPing(this Player player)
        {
            return player.GetPlayerNet().prop_Int16_0;
        }

        public static Player GetPlayer(int ActorNumber)
        {
            return AllPlayers.Where(p => p.GetActorNumber() == ActorNumber).FirstOrDefault();
        }

        public static string GetSteamID(this VRCPlayer player)
        {
            return player.field_Private_UInt64_0.ToString();
        }

        private static VRC_EventHandler handler;

        public static void SendVRCEvent(VRC_EventHandler.VrcEvent vrcEvent, VRC_EventHandler.VrcBroadcastType type, GameObject instagator)
        {
            if (handler == null)
                handler = Resources.FindObjectsOfTypeAll<VRC_EventHandler>().FirstOrDefault();
            vrcEvent.ParameterObject = handler.gameObject;
            handler.TriggerEvent(vrcEvent, type, instagator);
        }

        public static GameObject InstantiatePrefab(string PrefabNAME, Vector3 position, Quaternion rotation) // yes it do work
        {
            return Networking.Instantiate(
                VRC_EventHandler.VrcBroadcastType.Always,
                PrefabNAME,
                position,
                rotation
            );
        }

        public static void ChangeAvatar(string AvatarID)
        {
            new PageAvatar { field_Public_SimpleAvatarPedestal_0 = new SimpleAvatarPedestal { field_Internal_ApiAvatar_0 = new ApiAvatar { id = AvatarID } } }.ChangeToSelectedAvatar();
        }

        public static Player GetPlayer(string Displayname)
        {
            return AllPlayers.Where(p => p.GetName() == Displayname).FirstOrDefault();
        }

        public static void ToggleBlock(this Player player)
        {
            var userinfo = GameObject.Find("Screens").transform.Find("UserInfo").GetComponent<PageUserInfo>();
            {
                userinfo.field_Private_APIUser_0 = new APIUser
                {
                    id = player.GetAPIUser().id
                };

                if (player.GetAPIUser().id != APIUser.CurrentUser.id)
                {
                    userinfo.ToggleBlock();
                }
            }
        }

        public static Player GetPlayerByUserID(string UserID)
        {
            return AllPlayers.Where(p => p.GetAPIUser().id == UserID).FirstOrDefault();
        }

        public static void SetGain(float Gain)
        {
            LocalGain = Gain;
        }

        public static void ResetGain()
        {
            USpeaker.field_Internal_Static_Single_1 = DefaultGain;
        }
    }
}