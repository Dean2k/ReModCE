using MelonLoader;
using ReModCE_ARES.Loader;
using ReModCE_ARES.SDK.Utils;
using System.Collections;
using UnityEngine;

namespace ReModCE_ARES.SDK
{
    internal class GeneralWrapper
    {
        internal static FastMethodInfo closeMenuFunction;
        internal static FastMethodInfo closePopupFunction;
        internal static FastMethodInfo alertActionFunction;
        internal static FastMethodInfo alertPopupFunction;
        internal static FastMethodInfo reloadPlayerAvatarFunction;
        internal static FastMethodInfo loadAvatarFunction;

        internal static VRCUiPopupManager GetVRCUiPopupManager()
        {
            return VRCUiPopupManager.prop_VRCUiPopupManager_0;
        }

        internal static void AlertAction(string title, string content, string button1Text, System.Action button1Action, string button2Text, System.Action button2Action)
        {
            try
            {
                alertActionFunction.Invoke(GetVRCUiPopupManager(), title, content, button1Text, (Il2CppSystem.Action)button1Action, button2Text, (Il2CppSystem.Action)button2Action, null);
            }
            catch (System.Exception e)
            {
                ReLogger.Error("AlertAction", e);
            }
        }

        internal static void ClosePopup()
        {
            try
            {
                closePopupFunction.Invoke(GetVRCUiPopupManager(), null);
            }
            catch (System.Exception e)
            {
                ReLogger.Error("ClosePopup", e);
            }
        }
    }
}