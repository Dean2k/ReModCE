using ExitGames.Client.Photon;
using MelonLoader;
using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.VRChat;
using System.Linq;
using System.Reflection;
using UnhollowerRuntimeLib.XrefScans;
using VRC;
using VRC.Core;
using VRC.DataModel;

namespace ReModCE_ARES.Components
{
    internal class SoftCloneComponent : ModComponent
    {

        private static ReMenuButton _vrcaTargetButton;


        private static MethodInfo _loadAvatarMethod;
        private static bool _state = false;
        private static bool _sentTwice = false;
        private static Il2CppSystem.Object AvatarDictCache { get; set; }

        public SoftCloneComponent()
        {
            _loadAvatarMethod =
                typeof(VRCPlayer).GetMethods()
                    .First(mi =>
                        mi.Name.StartsWith("Method_Private_Void_Boolean_")
                        && mi.Name.Length < 31
                        && mi.GetParameters().Any(pi => pi.IsOptional)
                        && XrefScanner.UsedBy(mi) // Scan each method
                            .Any(instance => instance.Type == XrefType.Method
                                             && instance.TryResolve() != null
                                             && instance.TryResolve().Name == "ReloadAvatarNetworkedRPC"));


        }

        public override bool OnEventPatch(ref EventData __0)
        {
            if (_state
                && __0.Code == 42
                && AvatarDictCache != null
                && __0.Sender == Player.prop_Player_0.field_Private_VRCPlayerApi_0.playerId
               )
            {
                try
                {
                    __0.Parameters[245].Cast<Il2CppSystem.Collections.Hashtable>()["avatarDict"] = AvatarDictCache;
                }
                catch (System.Exception ex) { ReModCE_ARES.LogDebug(ex.Message); }

                if (_sentTwice)
                {
                    _state = false;
                    _sentTwice = false;
                }
                else
                {
                    _sentTwice = true;
                }
            }
            return true;
        } 

        public override void OnUiManagerInit(UiManager uiManager)
        {
            base.OnUiManagerInit(uiManager);

            var targetMenu = uiManager.TargetMenu;

            _vrcaTargetButton = targetMenu.AddButton("SoftClone", "Softclone the selected user (Works on even Private avatars only you will see)", SoftClone, ResourceManager.GetSprite("remodce.copy"));
        }

        private void SoftClone()
        {
            var user = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0;
            if (user == null)
                return;

            AvatarDictCache = null;
            string target = UserSelectionManager.field_Private_Static_UserSelectionManager_0.field_Private_APIUser_1.id;

            ReModCE_ARES.LogDebug(target);

            _state = true;

            AvatarDictCache = PlayerManager.prop_PlayerManager_0
                .field_Private_List_1_Player_0
                .ToArray()
                .FirstOrDefault(a => a.field_Private_APIUser_0.id == target)
                ?.prop_Player_1.field_Private_Hashtable_0["avatarDict"];
            _loadAvatarMethod.Invoke(VRCPlayer.field_Internal_Static_VRCPlayer_0, new object[] { true });
        }


    }
}
