using System.Linq;
using VRC.Core;
using VRC.Management;

namespace ReModCE_ARES.SDK
{
    internal static class ModerationManagerExtension
    {
        // pretty sure this shit doesnt work
        public static bool GetIsBlocked(string userID)
            => GetModerationExistsAgainstPlayer(ApiPlayerModeration.ModerationType.Block, userID);

        public static bool GetAvatarHidden(string userID)
            => GetModerationExistsAgainstPlayer(ApiPlayerModeration.ModerationType.HideAvatar, userID);
        public static bool GetAvatarShow(string userID)
            => GetModerationExistsAgainstPlayer(ApiPlayerModeration.ModerationType.ShowAvatar, userID);

        private static bool GetModerationExistsAgainstPlayer(ApiPlayerModeration.ModerationType moderationType, string userID)
        {
            return ModerationFromMe(moderationType) != null &&
                ModerationFromMe(moderationType).ToArray().ToList().Exists((ApiPlayerModeration m)
                => m != null && m.moderationType == moderationType && m.targetUserId == userID);
        }

        private static ModerationManager Instance => ModerationManager.prop_ModerationManager_0;

        private static Il2CppSystem.Collections.Generic.List<ApiPlayerModeration> ModerationFromMe(ApiPlayerModeration.ModerationType type)
        {
            return Instance.Method_Public_List_1_ApiPlayerModeration_ModerationType_0(type);
        }
    }
}