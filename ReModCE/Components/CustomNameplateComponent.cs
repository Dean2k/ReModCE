using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI.QuickMenu;
using ReModCE_ARES.Core;
using ReModCE_ARES.Loader;
using ReModCE_ARES.Managers;
using ReModCE_ARES.SDK;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using VRC;

namespace ReModCE_ARES.Components
{
    public class CustomNameplate : MonoBehaviour
    {
        public VRC.Player player;
        private byte frames;
        private byte ping;
        private int noUpdateCount = 0;
        private TextMeshProUGUI statsText;
        private TextMeshProUGUI customText;
        public bool OverRender;
        public bool VRam;
        public bool Enabled = true;

        public CustomNameplate(IntPtr ptr) : base(ptr)
        {
        }

        private void Start()
        {
            if (Enabled)
            {
                Transform stats = UnityEngine.Object.Instantiate<Transform>(this.gameObject.transform.Find("Contents/Quick Stats"), this.gameObject.transform.Find("Contents"));
                stats.parent = this.gameObject.transform.Find("Contents");
                stats.name = "Nameplate";
                stats.localPosition = new Vector3(0f, 62f, 0f);
                stats.gameObject.SetActive(true);
                statsText = stats.Find("Trust Text").GetComponent<TextMeshProUGUI>();
                statsText.color = Color.white;
                if (OverRender && Enabled)
                {
                    statsText.isOverlay = true;
                }
                else
                {
                    statsText.isOverlay = false;
                }

                stats.Find("Trust Icon").gameObject.SetActive(false);
                stats.Find("Performance Icon").gameObject.SetActive(false);
                stats.Find("Performance Text").gameObject.SetActive(false);
                stats.Find("Friend Anchor Stats").gameObject.SetActive(false);

                NameplateModel custom = IsCustom(player);
                if (custom != null)
                {
                    Transform customStats = UnityEngine.Object.Instantiate<Transform>(this.gameObject.transform.Find("Contents/Quick Stats"), this.gameObject.transform.Find("Contents"));
                    customStats.parent = this.gameObject.transform.Find("Contents");
                    customStats.name = "StaffSetNameplate";
                    customStats.localPosition = new Vector3(0f, 104f, 0f);
                    customStats.gameObject.SetActive(true);
                    customText = customStats.Find("Trust Text").GetComponent<TextMeshProUGUI>();
                    customText.color = Color.white;
                    if (OverRender && Enabled)
                    {
                        customText.isOverlay = true;
                    }
                    else
                    {
                        customText.isOverlay = false;
                    }

                    customStats.Find("Trust Icon").gameObject.SetActive(false);
                    customStats.Find("Performance Icon").gameObject.SetActive(false);
                    customStats.Find("Performance Text").gameObject.SetActive(false);
                    customStats.Find("Friend Anchor Stats").gameObject.SetActive(false);
                }

                frames = player._playerNet.field_Private_Byte_0;
                ping = player._playerNet.field_Private_Byte_1;
            }
        }

        private NameplateModel IsCustom(VRC.Player player)
        {
            try
            {
                return ReModCE_ARES.NameplateModels.First(x => x.UserID == player.prop_APIUser_0.id && x.Active);
            }
            catch
            {
                return null;
            }
        }
        private string avatarStats = "";
        private string avatarId = "";
        private bool avatarLoaded = false;
        private bool wasPrevious = false;
        private bool avatarHidden = false;
        private bool wasPreviousHidden = false;
        private bool avatarShown = false;
        private bool wasPreviousShown = false;
        private int skipX = 0;

        private void Update()
        {
            if (Enabled)
            {
                if (frames == player._playerNet.field_Private_Byte_0 && ping == player._playerNet.field_Private_Byte_1)
                {
                    noUpdateCount++;
                }
                else
                {
                    noUpdateCount = 0;
                }
                frames = player._playerNet.field_Private_Byte_0;
                ping = player._playerNet.field_Private_Byte_1;
                if (skipX >= 50)
                {                  
                    if (VRam)
                    {
                        avatarLoaded = player.GetAvatarObject().active;
                        avatarHidden = ModerationManagerExtension.GetAvatarHidden(player.field_Private_APIUser_0.id);
                        avatarShown = ModerationManagerExtension.GetAvatarShow(player.field_Private_APIUser_0.id);
                        if ((avatarId != player.prop_ApiAvatar_0.id) || (avatarLoaded != wasPrevious) || (avatarHidden != wasPreviousHidden) || (avatarShown != wasPreviousShown) || avatarStats == "1.40 MB")
                        {
                            avatarStats = player.GetVRamActive();
                            avatarId = player.prop_ApiAvatar_0.id;
                        }
                    }
                   
                    string text = "<color=green>Stable</color>";
                    if (noUpdateCount > 30)
                        text = "<color=yellow>Lagging</color>";
                    if (noUpdateCount > 150)
                        text = "<color=red>Crashed</color>";
                    if (VRam)
                    {
                        statsText.text = $"[{player.GetPlatform()}] |" + $" [{player.GetAvatarStatus()}] |" + $"{(player.GetIsMaster() ? " | [<color=#0352ff>HOST</color>] |" : "")}" + $" [{text}] |" + $" [FPS: {player.GetFramesColord()}] |" + $" [Ping: {player.GetPingColord()}] " + $" [VRAM: {avatarStats}] " + $" {(player.ClientDetect() ? " | [<color=red>ClientUser</color>]" : "")}";
                    } else
                    {
                        statsText.text = $"[{player.GetPlatform()}] |" + $" [{player.GetAvatarStatus()}] |" + $"{(player.GetIsMaster() ? " | [<color=#0352ff>HOST</color>] |" : "")}" + $" [{text}] |" + $" [FPS: {player.GetFramesColord()}] |" + $" [Ping: {player.GetPingColord()}] " + $" {(player.ClientDetect() ? " | [<color=red>ClientUser</color>]" : "")}";
                    }
                    if (customText != null)
                    {
                        NameplateModel custom = IsCustom(player);
                        customText.text = custom.Text;
                    }
                    wasPrevious = avatarLoaded;
                    wasPreviousHidden = avatarHidden;
                    wasPreviousShown = avatarShown;
                    skipX = 0;
                } else
                {
                    skipX++;
                }
            }
        }

        public void Dispose()
        {
            Enabled = false;
            statsText.gameObject.SetActive(false);
            customText.gameObject.SetActive(false);
            statsText.text = null;
            customText.text = null;
        }
    }

    internal sealed class CustomNameplateComponent : ModComponent
    {
        private ConfigValue<bool> CustomNameplateEnabled;
        private ReMenuToggle _CustomNameplateEnabled;

        private ConfigValue<bool> NamePlateOverRenderEnabled;
        private ReMenuToggle _namePlateOverRenderEnabled;

        private ConfigValue<bool> VRamShowEnabled;
        private ReMenuToggle _vRamShowEnabled;

        public CustomNameplateComponent()
        {
            CustomNameplateEnabled = new ConfigValue<bool>(nameof(CustomNameplateEnabled), true);
            CustomNameplateEnabled.OnValueChanged += () => _CustomNameplateEnabled.Toggle(CustomNameplateEnabled);

            NamePlateOverRenderEnabled = new ConfigValue<bool>(nameof(NamePlateOverRenderEnabled), true);
            NamePlateOverRenderEnabled.OnValueChanged += () => _namePlateOverRenderEnabled.Toggle(NamePlateOverRenderEnabled);

            VRamShowEnabled = new ConfigValue<bool>(nameof(VRamShowEnabled), false);
            VRamShowEnabled.OnValueChanged += () => _vRamShowEnabled.Toggle(VRamShowEnabled);
        }

        public override void OnPlayerJoined(VRC.Player player)
        {
            if (CustomNameplateEnabled)
            {
                CustomNameplate nameplate = player.transform.Find("Player Nameplate/Canvas/Nameplate").gameObject.AddComponent<CustomNameplate>();
                nameplate.player = player;
                nameplate.OverRender = NamePlateOverRenderEnabled;
                nameplate.VRam = VRamShowEnabled;
            }
        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            base.OnUiManagerInit(uiManager);
            var menu = uiManager.MainMenu.GetCategoryPage(Page.PageNames.Visuals).GetCategory(Page.Categories.Visuals.Nameplate);
            _CustomNameplateEnabled = menu.AddToggle("Custom Nameplates", "Enable/Disable custom nameplates (reload world to fully unload)", ToggleNameplates,
                CustomNameplateEnabled);
            _namePlateOverRenderEnabled = menu.AddToggle("Nameplate Over render", "Enable/Disable the over render of nameplates (reload world to fully unload)", NamePlateOverRenderEnabled.SetValue,
                NamePlateOverRenderEnabled);
            _vRamShowEnabled = menu.AddToggle("Show VRam usage", "This can cause more memory usage and lag spikes", VRamShowEnabled.SetValue,
                VRamShowEnabled);
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (buildIndex == -1)
            {
                ReModCE_ARES.UpdateNamePlates();
            }
        }

        private void ToggleNameplates(bool value)
        {
            CustomNameplateEnabled.SetValue(value);
            try
            {
                if (value)
                {
                    try
                    {
                        foreach (Player player in UnityEngine.Object.FindObjectsOfType<Player>())
                        {
                            CustomNameplate nameplate = player.transform.Find("Player Nameplate/Canvas/Nameplate").gameObject.AddComponent<CustomNameplate>();
                            nameplate.player = player;
                            nameplate.OverRender = NamePlateOverRenderEnabled;
                            nameplate.VRam = VRamShowEnabled;
                        }
                    }
                    catch { }
                }
                else
                {
                    foreach (Player player in UnityEngine.Object.FindObjectsOfType<Player>())
                    {
                        CustomNameplate disabler = player.transform.Find("Player Nameplate/Canvas/Nameplate").gameObject.GetComponent<CustomNameplate>();
                        disabler.Dispose();
                    }
                }
            }
            catch (Exception ex) { ReLogger.Msg(ex.Message); }
        }
    }
}