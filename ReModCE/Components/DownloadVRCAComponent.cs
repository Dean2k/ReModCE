using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.VRChat;
using ReModCE_ARES.Loader;
using ReModCE_ARES.Managers;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using VRC;
using VRC.DataModel;
using VRC.UI;

namespace ReModCE_ARES.Components
{
    internal sealed class DownloadVRCAComponent : ModComponent
    {
        private static ReUiButton _vrcaMenuButton;
        private static ReMenuButton _vrcaTargetButton;

        private static PageUserInfo _userInfoPage;

        public override void OnUiManagerInit(UiManager uiManager)
        {
            var targetMenu = uiManager.TargetMenu;

            var userInfoTransform = VRCUiManagerEx.Instance.MenuContent().transform.Find("Screens/UserInfo");
            _userInfoPage = userInfoTransform.GetComponent<PageUserInfo>();

            var buttonContainer = userInfoTransform.Find("Buttons/RightSideButtons/RightUpperButtonColumn/");

            _vrcaMenuButton = new ReUiButton("Download VRCA", Vector2.zero, new Vector2(0.68f, 1.2f), DownloadVRCAMenuButtonOnClick, buttonContainer);
            _vrcaTargetButton = targetMenu.AddButton("Download VRCA", "Downloads the selected users VRCA File.", DownloadVRCATargetButtonOnClick, ResourceManager.GetSprite("remodce.link"));
        }

        private void DownloadVRCATargetButtonOnClick()
        {
            var user = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0;
            if (user == null)
                return;

            DownloadVRCA(user);
        }

        private void DownloadVRCAMenuButtonOnClick()
        {
            var user = _userInfoPage.field_Private_IUser_0;
            if (user == null)
                return;

            DownloadVRCA(user);
        }

        private void DownloadVRCA(IUser user)
        {
            Task.Run(delegate
            {
                var player = PlayerManager.field_Private_Static_PlayerManager_0.GetPlayer(user.prop_String_0)._vrcplayer;
                WebClient webClient = new WebClient
                {
                    Headers =
                    {
                        "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/99.0.4844.84 Safari/537.36"
                    }
                };
                if (!Directory.Exists("ARES"))
                {
                    Directory.CreateDirectory("ARES");
                    Directory.CreateDirectory("ARES/VRCA");
                }
                if (!Directory.Exists("ARES/VRCA"))
                {
                    Directory.CreateDirectory("ARES/VRCA");
                }
                webClient.DownloadFileAsync(new Uri(player.field_Private_ApiAvatar_0.assetUrl), "ARES/VRCA/" + player.field_Private_ApiAvatar_0.name + ".vrca");
                ReLogger.Msg(player.field_Private_ApiAvatar_0.name + " VRCA Downloaded");
                ReModCE_ARES.LogDebug(player.field_Private_ApiAvatar_0.name + " VRCA Downloaded");
            });
        }
    }
}