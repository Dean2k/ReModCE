using VRC;
using VRC.Core;

namespace ReModCE_ARES.ApplicationBot
{
    internal static class MenuExtensions
    {
        public static QuickMenu GetQuickMenu() =>
            QuickMenu.prop_QuickMenu_0;

        public static APIUser GetSelectedAPIUser() =>
            GetQuickMenu().prop_APIUser_0;

        public static Player GetSelectedPlayer() =>
            PlayerExtensions.GetPlayerByUserID(GetSelectedAPIUser().id);
    }
}