using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.VRChat;
using ReModCE_ARES.Managers;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.DataModel;
using VRC.UI;

namespace ReModCE_ARES.Components
{
    internal sealed class TeleportComponent : ModComponent
    {
        private static ReUiButton _teleportMenuButton;
        private static ReMenuButton _teleportTargetButton;

        private static PageUserInfo _userInfoPage;

        public override void OnUiManagerInit(UiManager uiManager)
        {
            var targetMenu = uiManager.TargetMenu;

            var userInfoTransform = VRCUiManagerEx.Instance.MenuContent().transform.Find("Screens/UserInfo");
            _userInfoPage = userInfoTransform.GetComponent<PageUserInfo>();

            var buttonContainer = userInfoTransform.Find("Buttons/RightSideButtons/RightUpperButtonColumn/");

            _teleportMenuButton = new ReUiButton("Teleport", Vector2.zero, new Vector2(0.68f, 1.2f), TeleportMenuButtonOnClick, buttonContainer);
            _teleportTargetButton = targetMenu.AddButton("Teleport", "Teleports to target.", TeleportTargetButtonOnClick, ResourceManager.GetSprite("remodce.teleport"));
        }

        private void TeleportMenuButtonOnClick()
        {
            var user = _userInfoPage.field_Private_IUser_0;
            if (user == null)
                return;

            TeleportToIUser(user);
        }

        private void TeleportTargetButtonOnClick()
        {
            var user = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0;
            if (user == null)
                return;

            TeleportToIUser(user);
        }

        private void TeleportToIUser(IUser user)
        {
            var player = PlayerManager.field_Private_Static_PlayerManager_0.GetPlayer(user.prop_String_0)._vrcplayer;
            if (player == null)
                return;

            var transform = player.transform;
            var playerPosition = transform.position;

            var localTransform = VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;
            localTransform.position = playerPosition;

            VRCUiManagerEx.Instance.CloseUi();
        }

        public override void OnSetupUserInfo(APIUser apiUser)
        {
            bool check = APIUser.CurrentUser.id != apiUser.id && PlayerManager.field_Private_Static_PlayerManager_0.GetPlayer(apiUser.id) != null;
            _teleportMenuButton.Active = check;
            _teleportTargetButton.Active = check;
        }
    }
}