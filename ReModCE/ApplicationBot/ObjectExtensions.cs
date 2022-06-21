using UnityEngine;

namespace ReModCE_ARES.ApplicationBot
{
    internal class ObjectExtensions
    {
        private static GameObject CachedPlayerCamera;

        public static GameObject GetPlayerCamera
        {
            get
            {
                if (CachedPlayerCamera == null)
                    CachedPlayerCamera = GameObject.Find("Camera (eye)");
                return CachedPlayerCamera;
            }
        }
    }
}