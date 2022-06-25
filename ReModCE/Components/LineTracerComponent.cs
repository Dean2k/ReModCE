namespace ReModCE_ARES.Components
{

    using global::ReModCE_ARES.Managers;
    using ReModAres.Core;
    using ReModAres.Core.Managers;
    using ReModAres.Core.UI.QuickMenu;
    using ReModAres.Core.Unity;
    using ReModAres.Core.VRChat;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.XR;
    using VRC;
    using VRC.Core;

    public sealed class LineTracerComponent : ModComponent
    {

        private const string RightTrigger = "Oculus_CrossPlatform_SecondaryIndexTrigger";

        private static readonly int Cull = Shader.PropertyToID("_Cull");

        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");

        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");

        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");

        // Requi told me no, do this
        private ConfigValue<Color> FriendsColor;
        private ConfigValue<Color> OthersColor;

        private static Material lineMaterial;

        private static RenderObjectListener renderObjectListener;
        private static RenderObjectListener renderObjectDesktopListener;

        private readonly List<Player> cachedPlayers = new();

        private ConfigValue<bool> lineTracerEnabled;
        private ConfigValue<bool> lineTracerDesktopEnabled;

        private bool materialSetup;

        private Transform originTransform;
        private Transform originDesktopTransform;


        public LineTracerComponent()
        {
            RenderObjectListener.RegisterSafe();

            lineTracerEnabled = new ConfigValue<bool>(
                nameof(lineTracerEnabled),
                false,
                "Enable Line Tracer (Right Trigger)");

            lineTracerDesktopEnabled = new ConfigValue<bool>(
                nameof(lineTracerDesktopEnabled),
                false,
                "Enable Line Tracer");

            FriendsColor = new ConfigValue<Color>(nameof(FriendsColor), Color.yellow);
            OthersColor = new ConfigValue<Color>(nameof(OthersColor), Color.magenta);

        }

        public override void OnEnterWorld(ApiWorld world, ApiWorldInstance instance)
        {
            cachedPlayers.Clear();
        }

        public override void OnPlayerJoined(Player player)
        {
            if (player.prop_APIUser_0.IsSelf) return;
            cachedPlayers.Add(player);
        }

        public override void OnPlayerLeft(Player player)
        {
            cachedPlayers.Remove(player);
        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            ReMenuCategory espMenu = uiManager.MainMenu.GetCategoryPage(Page.PageNames.Visuals).GetCategory(Page.Categories.Visuals.EspHighlights);

            espMenu.AddToggle(
                "[VR] Line Tracer",
                "Hold Right Trigger to draw lines to each players from your hand",
                lineTracerEnabled);

            espMenu.AddToggle(
                "Desktop Line Tracer (Not finished)",
                "Draw lines to each players from your hand",
                lineTracerDesktopEnabled);

            // Late enough that the camera is on now
            renderObjectListener = VRCVrCamera.field_Private_Static_VRCVrCamera_0.field_Public_Camera_0.gameObject
                                              .AddComponent<RenderObjectListener>();
            renderObjectListener.hideFlags = HideFlags.HideAndDontSave;
            renderObjectListener.RenderObject += OnRenderObject;

            renderObjectDesktopListener = VRCVrCamera.field_Private_Static_VRCVrCamera_0.field_Public_Camera_0.gameObject
                .AddComponent<RenderObjectListener>();
            renderObjectDesktopListener.hideFlags = HideFlags.HideAndDontSave;
            renderObjectDesktopListener.RenderObject += OnRenderObjectDesktop;
        }

        private static Transform GetOriginTransform()
        {
            VRCPlayer localPlayer = VRCPlayer.field_Internal_Static_VRCPlayer_0;
            if (!localPlayer) return null;

            Animator localAnimator = localPlayer.GetAvatarObject()?.GetComponent<Animator>();
            if (localAnimator == null
                || !localAnimator.isHuman) return null;

            // try to grab from the tip of the finger all the way to the hand. otherwise fail
            return localAnimator.GetBoneTransform(HumanBodyBones.RightIndexDistal)
                   ?? localAnimator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate)
                   ?? localAnimator.GetBoneTransform(HumanBodyBones.RightIndexProximal)
                   ?? localAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        }

        private static Transform GetOriginTransformDesktop()
        {
            VRCPlayer localPlayer = VRCPlayer.field_Internal_Static_VRCPlayer_0;
            if (!localPlayer) return null;

            Animator localAnimator = localPlayer.GetAvatarObject()?.GetComponent<Animator>();
            if (localAnimator == null
                || !localAnimator.isHuman) return null;

            return localAnimator.GetBoneTransform(HumanBodyBones.Hips);
        }

        private new void OnRenderObject()
        {
            // In World/Room
            if (!RoomManager.field_Private_Static_Boolean_0) return;

            if (!lineTracerEnabled
                || !XRDevice.isPresent) return;

            if (Input.GetAxis(RightTrigger) < 0.4f) return;

            if (!materialSetup) SetupMaterial();

            // local player
            if (!originTransform) originTransform = GetOriginTransform();
            if (originTransform == null) return;

            // Initialize GL
            GL.Begin(1); // Lines
            lineMaterial.SetPass(0);

            // goes way faster to re-use the cached players
            foreach (Player player in cachedPlayers)
            {
                if (!player) continue;
                GL.Color(
                    player.GetAPIUser().isFriend ? FriendsColor.Value : OthersColor.Value);
                GL.Vertex(originTransform.position);
                GL.Vertex(player.transform.position);
            }

            // End GL
            GL.End();
        }

        private void OnRenderObjectDesktop()
        {
            // In World/Room
            if (!RoomManager.field_Private_Static_Boolean_0) return;

            if (!lineTracerDesktopEnabled) return;

            if (!materialSetup) SetupMaterial();

            // local player
            if (!originDesktopTransform) originDesktopTransform = GetOriginTransformDesktop();
            if (originDesktopTransform == null) return;

            // Initialize GL
            GL.Begin(1); // Lines
            lineMaterial.SetPass(0);

            // goes way faster to re-use the cached players
            foreach (Player player in cachedPlayers)
            {
                if (!player) continue;
                GL.Color(
                    player.GetAPIUser().isFriend ? FriendsColor.Value : OthersColor.Value);
                GL.Vertex(originDesktopTransform.position);
                GL.Vertex(player.transform.position);
            }

            // End GL
            GL.End();
        }

        private void SetupMaterial()
        {
            lineMaterial = Material.GetDefaultLineMaterial();
            lineMaterial.SetInt(SrcBlend, 5);
            lineMaterial.SetInt(DstBlend, 10);
            lineMaterial.SetInt(Cull, 0);
            lineMaterial.SetInt(ZWrite, 0);
            lineMaterial.hideFlags = HideFlags.HideAndDontSave;

            materialSetup = true;
        }

    }

}