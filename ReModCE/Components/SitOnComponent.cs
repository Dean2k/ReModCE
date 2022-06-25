using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.VRChat;
using UnityEngine;
using VRC;
using VRC.DataModel;

namespace ReModCE_ARES.Components
{
    internal sealed class SitOnComponent : ModComponent
    {
        private static ReMenuButton _teleportTargetHeadButton;
        private static ReMenuButton _teleportTargetLeftHandButton;
        private static ReMenuButton _teleportTargetRightHandButton;
        private static ReMenuButton _teleportTargetRightLegButton;
        private static ReMenuButton _teleportTargetLeftLegButton;
        private static ReMenuButton _teleportTargetHipsButton;
        private static IUser target;
        private Vector3 _originalGravity;
        private Vector3 _playerLastPos;
        public string bodyPart;

        public override void OnUiManagerInit(UiManager uiManager)
        {
            var targetMenu = uiManager.TargetMenu;

            var userInfoTransform = VRCUiManagerEx.Instance.MenuContent().transform.Find("Screens/UserInfo");

            var buttonContainer = userInfoTransform.Find("Buttons/RightSideButtons/RightUpperButtonColumn/");
            var submenu = targetMenu.AddMenuPage("Sit On", "Sit on x body part",
                ResourceManager.GetSprite("remodce.legs"));

            _teleportTargetHeadButton = submenu.AddButton("Sit On Head", "Sit on target (press jump to stop).", TeleportTargetButtonOnClick, ResourceManager.GetSprite("remodce.legs"));
            _teleportTargetLeftHandButton = submenu.AddButton("Sit On Left Hand", "Sit on target (press jump to stop).", TeleportTargetLeftHandButtonOnClick, ResourceManager.GetSprite("remodce.legs"));
            _teleportTargetRightHandButton = submenu.AddButton("Sit On Right Hand", "Sit on target (press jump to stop).", TeleportTargetRightHandButtonOnClick, ResourceManager.GetSprite("remodce.legs"));
            _teleportTargetRightLegButton = submenu.AddButton("Sit On Right Leg", "Sit on target (press jump to stop).", TeleportTargetRightLegButtonOnClick, ResourceManager.GetSprite("remodce.legs"));
            _teleportTargetLeftLegButton = submenu.AddButton("Sit On Left Leg", "Sit on target (press jump to stop).", TeleportTargetLeftLegButtonOnClick, ResourceManager.GetSprite("remodce.legs"));
            _teleportTargetHipsButton = submenu.AddButton("Sit On Hips", "Sit on target (press jump to stop).", TeleportTargetHipsButtonOnClick, ResourceManager.GetSprite("remodce.legs"));
            uiManager.MainMenu.AddButton("Stop Siton",
                "Stop sitting on incase Jump doesn't work.", StopSit, ResourceManager.GetSprite("remodce.legs"));

        }

        private void StandardSetup()
        {
            var user = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0;
            if (user == null)
                return;

            target = user;
            SetGravity();

            TeleportToIUser(user);
        }

        private void TeleportTargetButtonOnClick()
        {
            bodyPart = "Head";
            StandardSetup();
        }

        private void TeleportTargetLeftHandButtonOnClick()
        {
            bodyPart = "LeftHand";
            StandardSetup();
        }

        private void TeleportTargetRightHandButtonOnClick()
        {
            bodyPart = "RightHand";
            StandardSetup();
        }

        private void TeleportTargetRightLegButtonOnClick()
        {
            bodyPart = "RightLeg";
            StandardSetup();
        }

        private void TeleportTargetLeftLegButtonOnClick()
        {
            bodyPart = "LeftLeg";
            StandardSetup();
        }

        private void TeleportTargetHipsButtonOnClick()
        {
            bodyPart = "Hips";
            StandardSetup();
        }

        private void StopSit()
        {
            target = null;
            RemoveSetGravity();
        }

        private void SetGravity()
        {
            if (Physics.gravity == Vector3.zero) return;

            _originalGravity = Physics.gravity;
            Physics.gravity = Vector3.zero;
        }

        private void RemoveSetGravity()
        {
            if (_originalGravity == Vector3.zero) return;
            Physics.gravity = _originalGravity;
        }

        private void TeleportToIUser(IUser user)
        {
            try
            {
                var player = PlayerManager.field_Private_Static_PlayerManager_0.GetPlayer(user.prop_String_0)._vrcplayer;
                if (player == null)
                    return;
                Vector3 playerPosition = new Vector3();
                if (bodyPart == "Head")
                {
                    playerPosition = player.field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Head).position + new Vector3(0, 0.1f, 0);
                }
                if (bodyPart == "LeftHand")
                {
                    playerPosition = player.field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftIndexProximal).position + new Vector3(0, 0.1f, 0);
                }
                if (bodyPart == "RightHand")
                {
                    playerPosition = player.field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightIndexProximal).position + new Vector3(0, 0.1f, 0);
                }
                if (bodyPart == "RightLeg")
                {
                    playerPosition = player.field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.RightFoot).position + new Vector3(0, 0.1f, 0);
                }
                if (bodyPart == "LeftLeg")
                {
                    playerPosition = player.field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.LeftFoot).position + new Vector3(0, 0.1f, 0);
                }
                if (bodyPart == "Hips")
                {
                    playerPosition = player.field_Internal_Animator_0.GetBoneTransform(HumanBodyBones.Hips).position + new Vector3(0, 0.1f, 0);
                }
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

        public override void OnUpdate()
        {
            if (target != null)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    target = null;
                    RemoveSetGravity();
                    return;
                }
                if (Input.GetButton("Oculus_CrossPlatform_Button1"))
                {
                    target = null;
                    RemoveSetGravity();
                    return;
                }
                TeleportToIUser(target);
            }
        }
    }
}