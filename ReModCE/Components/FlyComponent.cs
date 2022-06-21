using ReModAres.Core;
using ReModAres.Core.Api;
using ReModAres.Core.Managers;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.UI.Wings;
using ReModAres.Core.VRChat;
using ReModCE_ARES.Managers;
using System.Collections.Generic;
using System.Linq;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using VRC.Animation;
using VRC.SDK3.Components;
using VRCSDK2;
using Object = UnityEngine.Object;
using VRC_AvatarPedestal = VRC.SDKBase.VRC_AvatarPedestal;
using VRC_Pickup = VRC.SDKBase.VRC_Pickup;
using VRC_UiShape = VRC.SDKBase.VRC_UiShape;

// ReSharper disable InconsistentNaming
namespace ReModCE_ARES.Components
{
    public class FlyComponent : ModComponent
    {
        private bool _noclipEnabled;
        private readonly List<int> _disabledColliders = new List<int>();
        private bool _flyEnabled;
        private ConfigValue<float> FlySpeed;
        private Vector3 _originalGravity;
        private ConfigValue<bool> EnableFlyHotkey;
        private ConfigValue<bool> FlyViewpointBased;

        private ReMenuButton _flySpeedButton;

        private ReMenuToggle _flyToggle;
        private ReMirroredWingToggle _noclipWingToggle;
        private ReMenuToggle _noclipToggle;
        private ReMenuToggle _hotkeyToggle;
        private ReMenuToggle _viewpointFlyingToggle;

        private ConfigValue<bool> ActionMenuFlyEnabled;
        private ReMenuToggle _actionMenuFlyEnabled;

        private Transform _cameraTransform;

        public FlyComponent()
        {
            FlySpeed = new ConfigValue<float>(nameof(FlySpeed), 4);
            FlySpeed.OnValueChanged += () => _flySpeedButton.Text = $"Fly Speed: {FlySpeed}";
            EnableFlyHotkey = new ConfigValue<bool>(nameof(EnableFlyHotkey), true);
            EnableFlyHotkey.OnValueChanged += () => _hotkeyToggle.Toggle(EnableFlyHotkey);



            ActionMenuFlyEnabled = new ConfigValue<bool>(nameof(ActionMenuFlyEnabled), true);
            ActionMenuFlyEnabled.OnValueChanged += () => _actionMenuFlyEnabled.Toggle(ActionMenuFlyEnabled);

            FlyViewpointBased = new ConfigValue<bool>(
                nameof(FlyViewpointBased),
                true,
                "Fly Viewpoint Based",
                "Whether to use Player/Viewpoint Transform as forward/right vectors.");
            FlyViewpointBased.OnValueChanged += () => _viewpointFlyingToggle?.Toggle(FlyViewpointBased);

        }

        private void ToggleMenu(bool value)
        {
            ActionMenuFlyEnabled.SetValue(value);
        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            var movementMenu = uiManager.MainMenu.GetMenuPage(Page.PageNames.Movement);
            var hotkeyMenu = uiManager.MainMenu.GetMenuPage(Page.PageNames.Hotkeys);

            _actionMenuFlyEnabled = movementMenu.AddToggle("Action Menu Toggle",
                "Enable the action menu quick toggle (restart required).", ToggleMenu,
                ActionMenuFlyEnabled);

            try
            {
                if (ActionMenuFlyEnabled)
                {
                    VRCActionMenuPage.AddToggle(ActionMenuPage.Main, "Fly", false, ToggleFlyQuick,
                        ResourceManager.GetTexture("remodce.arms-up"));
                }
            }
            catch { }
            _flyToggle = movementMenu.AddToggle("Fly", "Enable/Disable Fly", ToggleFly, _flyEnabled);
            _noclipToggle = movementMenu.AddToggle("Noclip", "Enable/Disable Noclip", ToggleNoclip, _noclipEnabled);
            _noclipWingToggle = ReModCE_ARES.WingMenu.AddToggle("Noclip", "Enable/Disable Noclip", b =>
            {
                if (b)
                {
                    ToggleNoclip(true);
                }
                else
                {
                    ToggleFly(false);
                }
            }, _noclipEnabled);

            _hotkeyToggle = hotkeyMenu.AddToggle("Fly Hotkey", "Enable/Disable fly hotkey",
                EnableFlyHotkey.SetValue, EnableFlyHotkey);

            _flySpeedButton = movementMenu.AddButton($"Fly Speed: {FlySpeed}", "Adjust your speed when flying", () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopupWithCancel("Set fly speed", FlySpeed.ToString(), InputField.InputType.Standard, false, "Submit",
                    (s, k, t) =>
                    {
                        if (string.IsNullOrEmpty(s))
                            return;

                        if (!float.TryParse(s, out var flySpeed))
                            return;

                        FlySpeed.SetValue(flySpeed);
                    }, null);
            }, ResourceManager.GetSprite("remodce.speed"));

            _viewpointFlyingToggle = movementMenu.AddToggle(
                "Fly Viewpoint Based",
                "Flying based of Player or Viewpoint transform",
                FlyViewpointBased);
        }

        private readonly List<Il2CppSystem.Type> _blacklistedComponents = new List<Il2CppSystem.Type>
        {
            Il2CppType.Of<PlayerSelector>(),
            Il2CppType.Of<VRC_Pickup>(),
            Il2CppType.Of<QuickMenu>(),
            Il2CppType.Of<VRC_Station>(),
            Il2CppType.Of<VRC_AvatarPedestal>(),
            Il2CppType.Of<VRC_UiShape>(),
            Il2CppType.Of<VRCUiShape>()
        };

        private void ToggleFlyQuick(bool value)
        {
            if (ReModCE_ARES.RotatorEnabled)
            {
                ToggleFly(false);
                return;
            }
            if (_flyEnabled)
            {
                ToggleFly(false);
            }
            else
            {
                ToggleFly(true);
            }

        }

        private void ToggleNoclipObjects()
        {
            var player = VRCPlayer.field_Internal_Static_VRCPlayer_0;
            if (player == null)
                return;

            var colliders = Object.FindObjectsOfType<Collider>();
            var ownCollider = player.GetComponents<Collider>().FirstOrDefault();

            foreach (var collider in colliders)
            {
                if (_blacklistedComponents.Any(comp => collider.GetComponent(comp) != null))
                {
                    continue;
                }

                if (collider == ownCollider)
                    continue;

                if (!(_noclipEnabled && collider.enabled || !_noclipEnabled && _disabledColliders.Contains(collider.GetInstanceID())))
                    continue;

                collider.enabled = !_noclipEnabled;
                if (_noclipEnabled)
                {
                    _disabledColliders.Add(collider.GetInstanceID());
                }
            }
        }

        private void ToggleFly(bool value)
        {
            if (ReModCE_ARES.RotatorEnabled)
            {
                _flyEnabled = false;
                _flyToggle?.Toggle(false);
                return;
            }
            _flyEnabled = value;
            _flyToggle?.Toggle(value);

            if (_cameraTransform == null
                || !_cameraTransform) _cameraTransform = VRCVrCamera.field_Private_Static_VRCVrCamera_0.field_Public_Camera_0.transform;

            if (_flyEnabled)
            {
                if (Physics.gravity == Vector3.zero) return;

                _originalGravity = Physics.gravity;
                Physics.gravity = Vector3.zero;
            }
            else
            {
                if (_noclipEnabled) ToggleNoclip(false);

                if (_originalGravity == Vector3.zero) return;

                Physics.gravity = _originalGravity;
                _originalGravity = Vector3.zero;
            }
        }

        private void ToggleNoclip(bool value)
        {
            if (ReModCE_ARES.RotatorEnabled)
            {
                _noclipEnabled = false;
                _noclipToggle?.Toggle(false);
                return;
            }
            _noclipEnabled = value;
            _noclipToggle?.Toggle(value);
            if (_noclipEnabled && !_flyEnabled)
            {
                ToggleFly(true);
            }

            ToggleNoclipObjects();
        }

        private void HandleHotkeys()
        {
            if (!EnableFlyHotkey) return;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.F))
            {
                if (!_flyEnabled)
                {
                    ToggleNoclip(true);
                }
                else
                {
                    ToggleFly(false);
                }
            }
        }

        public override void OnUpdate()
        {
            if (ReModCE_ARES.RotatorEnabled)
            {
                ToggleFly(false);
            }
            HandleHotkeys();
            HandleFly();
        }

        private VRCMotionState _motionState;
        private void HandleFly()
        {
            if (!_flyEnabled)
                return;

            if (VRCUiManagerEx.IsOpen)
                return;

            var player = VRCPlayer.field_Internal_Static_VRCPlayer_0;
            if (player == null)
                return;

            if (_motionState == null)
            {
                _motionState = player.GetComponent<VRCMotionState>();
            }

            if (XRDevice.isPresent && ActionMenuDriver.field_Public_Static_ActionMenuDriver_0.IsOpen())
                return;


            var playerTransform = player.transform;
            var flyingTransform = FlyViewpointBased ? _cameraTransform : playerTransform;
            if (XRDevice.isPresent)
            {
                // better to combine scalars before vector or it'll keep creating new vectors several times
                playerTransform.position += flyingTransform.forward * (Time.deltaTime * Input.GetAxis("Vertical") * FlySpeed);
                playerTransform.position += flyingTransform.right * (Time.deltaTime * Input.GetAxis("Horizontal") * FlySpeed);
                playerTransform.position += new Vector3(
                    0f,
                    Time.deltaTime * Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickVertical") * FlySpeed,
                    0f);
            }
            else
            {
                var speed = Input.GetKey(KeyCode.LeftShift) ? FlySpeed * 2 : FlySpeed;
                playerTransform.position += flyingTransform.forward * (Time.deltaTime * Input.GetAxis("Vertical") * speed);
                playerTransform.position += flyingTransform.right * (Time.deltaTime * Input.GetAxis("Horizontal") * speed);

                if (!Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.Q))
                {
                    playerTransform.position -= new Vector3(0f, Time.deltaTime * speed, 0f);
                }

                if (!Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.E))
                {
                    playerTransform.position += new Vector3(0f, Time.deltaTime * speed, 0f);
                }
            }

            _motionState?.Reset();
        }
    }
}
