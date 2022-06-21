using MelonLoader;
using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.VRChat;
using ReModCE_ARES.Loader;
using System.Collections;
using UnityEngine;
using VRC;
using VRC.DataModel;

namespace ReModCE_ARES.Components
{
    public class RemodVRAMAddonComponent : ModComponent
    {
        private static ReMenuButton buttonSize, buttonSizeActive;

        public override void OnUiManagerInit(UiManager uiManager)
        {
            base.OnUiManagerInit(uiManager);

            var subMenu = uiManager.MainMenu.GetMenuPage(Page.PageNames.Optimisation);
            buttonSize = subMenu.AddButton("VRAM\n-", "Click to recalculate VRAM size of avatar", ButtonClick, ResourceManager.GetSprite("remod.server"));
            buttonSizeActive = subMenu.AddButton("VRAM (A)\n-", "Click to recalculate VRAM size of avatar", ButtonClick, ResourceManager.GetSprite("remod.server"));

            ReMenuPage avatarPage = new ReMenuPage(QuickMenuEx.Instance.field_Public_Transform_0.Find("Window/QMParent/Menu_Avatars"));
            avatarPage.AddButton("Log VRAM", $"Logs the VRAM size of all avatars", VRAMCheckerInternal.LogInstance, ResourceManager.GetSprite("remod.server"));
            new ReMenuButton("Log VRAM", $"Logs the VRAM size of this World", VRAMCheckerInternal.LogWorld, QuickMenuEx.Instance.transform.Find("Container/Window/QMParent/Menu_Here/ScrollRect/Viewport/VerticalLayoutGroup/Buttons_WorldActions"), ResourceManager.GetSprite("remod.server"));
        }

        public void ButtonClick()
        {
            long totalSize = 0;
            long totalSizeOnlyActive = 0;
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
            {
                SizeModel sizes = VRAMCheckerInternal.GetSizeForGameObject(player._vrcplayer.field_Internal_GameObject_0);
                totalSize += sizes.size;
                totalSizeOnlyActive += sizes.sizeOnlyActive;
            }
            buttonSize.Text = $"VRAM\n{VRAMCheckerInternal.ToByteString(totalSize)}";
            buttonSizeActive.Text = $"VRAM (A)\n{VRAMCheckerInternal.ToByteString(totalSizeOnlyActive)}";           
        }

        public void ButtonClickSelected()
        {
            string userid = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0.GetUserID();
            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
                if (player.prop_APIUser_0.id == userid)
                {
                    SizeModel sizes = VRAMCheckerInternal.GetSizeForGameObject(player._vrcplayer.field_Internal_GameObject_0);
                    buttonSize.Text = $"VRAM\n{sizes.size}";
                    buttonSizeActive.Text = $"VRAM (A)\n{sizes.sizeOnlyActive}";
                    break;
                }
        }
    }

    public class SizeModel
    {
        public long size {get; set;}
        public long sizeOnlyActive { get; set; }
    }

}