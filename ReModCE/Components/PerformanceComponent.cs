using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI.QuickMenu;
using ReModCE_ARES.Loader;
using System.Collections;
using System.Diagnostics;
using UnityEngine;

namespace ReModCE_ARES.Components
{
    internal class PerformanceComponent : ModComponent
    {
        private ConfigValue<bool> HighPriorityEnabled;
        private ReMenuToggle _highPriorityToggle;

        private ConfigValue<bool> FPS144Enabled;
        private ReMenuToggle _fps144Toggle;

        private ConfigValue<bool> FPS240Enabled;
        private ReMenuToggle _fps240Toggle;

        private ConfigValue<bool> FPS999Enabled;
        private ReMenuToggle _fps999Toggle;

        private ConfigValue<bool> RamClearEnable;
        private ReMenuToggle _ramClearEnable;


        private ConfigValue<bool> AdaptiveGraphicsEnabled;
        private ReMenuToggle _adaptiveGraphicsToggle;

        private ConfigValue<bool> GfxUltraLowEnabled;
        private ReMenuToggle _gfxUltraLowToggle;

        private ConfigValue<bool> GfxLowEnabled;
        private ReMenuToggle _gfxLowToggle;

        private ConfigValue<bool> GfxMedEnabled;
        private ReMenuToggle _gfxMedToggle;

        private ConfigValue<bool> GfxHighEnabled;
        private ReMenuToggle _gfxHighToggle;

        private ConfigValue<bool> GfxUltraEnabled;
        private ReMenuToggle _gfxUltraToggle;

        public PerformanceComponent()
        {
            HighPriorityEnabled = new ConfigValue<bool>(nameof(HighPriorityEnabled), true);
            HighPriorityEnabled.OnValueChanged += () => _highPriorityToggle.Toggle(HighPriorityEnabled);

            FPS144Enabled = new ConfigValue<bool>(nameof(FPS144Enabled), false);
            FPS144Enabled.OnValueChanged += () => _fps144Toggle.Toggle(FPS144Enabled);

            FPS240Enabled = new ConfigValue<bool>(nameof(FPS240Enabled), false);
            FPS240Enabled.OnValueChanged += () => _fps240Toggle.Toggle(FPS240Enabled);

            FPS999Enabled = new ConfigValue<bool>(nameof(FPS999Enabled), false);
            FPS999Enabled.OnValueChanged += () => _fps999Toggle.Toggle(FPS999Enabled);

            RamClearEnable = new ConfigValue<bool>(nameof(RamClearEnable), false);
            RamClearEnable.OnValueChanged += () => _ramClearEnable.Toggle(RamClearEnable);

            AdaptiveGraphicsEnabled = new ConfigValue<bool>(nameof(AdaptiveGraphicsEnabled), false);
            AdaptiveGraphicsEnabled.OnValueChanged += () => _adaptiveGraphicsToggle.Toggle(AdaptiveGraphicsEnabled);

            GfxUltraLowEnabled = new ConfigValue<bool>(nameof(GfxUltraLowEnabled), false);
            GfxUltraLowEnabled.OnValueChanged += () => _gfxUltraLowToggle.Toggle(GfxUltraLowEnabled);

            GfxLowEnabled = new ConfigValue<bool>(nameof(GfxLowEnabled), false);
            GfxLowEnabled.OnValueChanged += () => _gfxLowToggle.Toggle(GfxLowEnabled);

            GfxMedEnabled = new ConfigValue<bool>(nameof(GfxMedEnabled), false);
            GfxMedEnabled.OnValueChanged += () => _gfxMedToggle.Toggle(GfxMedEnabled);

            GfxHighEnabled = new ConfigValue<bool>(nameof(GfxHighEnabled), false);
            GfxHighEnabled.OnValueChanged += () => _gfxHighToggle.Toggle(GfxHighEnabled);

            GfxUltraEnabled = new ConfigValue<bool>(nameof(GfxUltraEnabled), false);
            GfxUltraEnabled.OnValueChanged += () => _gfxUltraToggle.Toggle(GfxUltraEnabled);
        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            base.OnUiManagerInit(uiManager);

            var menu = uiManager.MainMenu.GetMenuPage(Page.PageNames.Optimisation);
            var subMenu = menu.AddMenuPage("Performance", "Performance related settings", ResourceManager.GetSprite("remodce.speedometer"));
            _highPriorityToggle = subMenu.AddToggle("High Priority",
                "Enable whether player joins/leaves should be logged in console.", SetHighPriority,
                HighPriorityEnabled);

            _fps144Toggle = subMenu.AddToggle("FPS 144 Max",
                "Sets FPS limit to 144", SetFPS144,
                FPS144Enabled);

            _fps240Toggle = subMenu.AddToggle("FPS 240 Max",
                "Sets FPS limit to 240", SetFPS240,
                FPS240Enabled);

            _fps999Toggle = subMenu.AddToggle("FPS 999 Max",
                "Sets FPS limit to 999", SetFPS999,
                FPS999Enabled);

            _ramClearEnable = subMenu.AddToggle("Ram Clear Loop",
                "Keep cleaning Ram (This may cause stutters)", ToggleRamClear,
                RamClearEnable);

            _adaptiveGraphicsToggle = subMenu.AddToggle("Adaptive Graphics",
                "Auto Adjust Graphics depending on FPS", AdjustGraphics,
                AdaptiveGraphicsEnabled);

            _gfxUltraLowToggle = subMenu.AddToggle("Ultra Low Graphics",
                "Ultra Low Graphics setting", AdjustGraphicsUltraLow,
                GfxUltraLowEnabled);

            _gfxLowToggle = subMenu.AddToggle("Low Graphics",
                "Low Graphics setting", AdjustGraphicsLow,
                GfxLowEnabled);

            _gfxMedToggle = subMenu.AddToggle("Med Graphics",
                "Med Graphics setting", AdjustGraphicsMed,
                GfxMedEnabled);

            _gfxHighToggle = subMenu.AddToggle("High Graphics",
                "High Graphics setting", AdjustGraphicsHigh,
                GfxHighEnabled);

            _gfxUltraToggle = subMenu.AddToggle("Ultra Graphics",
                "Ultra Graphics setting", AdjustGraphicsUltra,
                GfxUltraEnabled);

            if (!ReModCE_ARES.IsBot)
            {
                SetHighPriority(HighPriorityEnabled);
                AdjustGraphicsUltraLow(GfxUltraLowEnabled);
                AdjustGraphicsLow(GfxLowEnabled);
                AdjustGraphicsMed(GfxMedEnabled);
                AdjustGraphicsHigh(GfxHighEnabled);
                AdjustGraphicsUltra(GfxUltraEnabled);
                ToggleRamClear(RamClearEnable);
                SetFPS144(FPS144Enabled);
                SetFPS240(FPS240Enabled);
                SetFPS999(FPS999Enabled);
            }
        }

        private static bool EscapeLoop = false;

        private void ToggleRamClear(bool value)
        {
            RamClearEnable.SetValue(value);
            if (value)
            {
                EscapeLoop = false;
                RamClearLoop().Start();
            } else
            {
                EscapeLoop = true;
            }
        }

        private void AdjustGraphicsUltraLow(bool value)
        {
            GfxUltraLowEnabled.SetValue(value);
            if (!value) { return; }
            _gfxLowToggle.Toggle(false);
            _gfxMedToggle.Toggle(false);
            _gfxHighToggle.Toggle(false);
            _gfxUltraToggle.Toggle(false);
            ultraLow();
        }

        private void AdjustGraphicsLow(bool value)
        {
            GfxLowEnabled.SetValue(value);
            if (!value) { return; }
            low();
            _gfxUltraLowToggle.Toggle(false);
            _gfxMedToggle.Toggle(false);
            _gfxHighToggle.Toggle(false);
            _gfxUltraToggle.Toggle(false);
        }

        private void AdjustGraphicsMed(bool value)
        {
            GfxMedEnabled.SetValue(value);
            if (!value) { return; }
            medium();
            _gfxLowToggle.Toggle(false);
            _gfxUltraLowToggle.Toggle(false);
            _gfxHighToggle.Toggle(false);
            _gfxUltraToggle.Toggle(false);
        }

        private void AdjustGraphicsHigh(bool value)
        {
            GfxHighEnabled.SetValue(value);
            if (!value) { return; }
            high();
            _gfxLowToggle.Toggle(false);
            _gfxMedToggle.Toggle(false);
            _gfxUltraLowToggle.Toggle(false);
            _gfxUltraToggle.Toggle(false);
        }

        private void AdjustGraphicsUltra(bool value)
        {
            GfxUltraEnabled.SetValue(value);
            if (!value) { return; }
            ultra();
            _gfxLowToggle.Toggle(false);
            _gfxMedToggle.Toggle(false);
            _gfxHighToggle.Toggle(false);
            _gfxUltraLowToggle.Toggle(false);
        }

        private void SetFPS240(bool value)
        {
            FPS240Enabled.SetValue(value);

            if (value)
            {
                Application.targetFrameRate = 240;
                _fps144Toggle.Toggle(false);
                _fps999Toggle.Toggle(false);
            }
            if (!FPS144Enabled && !value && !FPS999Enabled)
            {
                Application.targetFrameRate = 90;
            }
        }

        private void SetFPS999(bool value)
        {
            FPS999Enabled.SetValue(value);

            if (value)
            {
                Application.targetFrameRate = 999;
                _fps144Toggle.Toggle(false);
                _fps240Toggle.Toggle(false);
            }
            if (!FPS144Enabled && !value && !FPS240Enabled)
            {
                Application.targetFrameRate = 90;
            }
        }

        private void SetFPS144(bool value)
        {
            FPS144Enabled.SetValue(value);

            if (value)
            {
                Application.targetFrameRate = 144;
                _fps240Toggle.Toggle(false);
                _fps999Toggle.Toggle(false);
            }
            if (!FPS240Enabled && !value && !FPS999Enabled)
            {
                Application.targetFrameRate = 90;
            }
        }

        private void SetHighPriority(bool value)
        {
            HighPriorityEnabled.SetValue(value);
            if (value)
            {
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;

                Process[] processes = Process.GetProcessesByName("vrserver");
                foreach (Process proc in processes)
                {
                    proc.PriorityClass = ProcessPriorityClass.High;
                }

                Process[] processes2 = Process.GetProcessesByName("OVRServer_x64");
                foreach (Process proc in processes2)
                {
                    proc.PriorityClass = ProcessPriorityClass.High;
                }
            }
        }

        private void AdjustGraphics(bool value)
        {
            AdaptiveGraphicsEnabled.SetValue(value);
        }

        public static float Average = 0f;
        public static float FPS = 0f;
        private static float RefreshRate = 0f;
        private static string lastMessage = "";
        public override void OnUpdate()
        {
            if (AdaptiveGraphicsEnabled)
            {
                Average += ((Time.deltaTime / Time.timeScale) - Average) * 0.03f;
                FPS = 1 / Average;

                RefreshRate += Time.deltaTime;
                if (RefreshRate < 8f) return;
                RefreshRate = 0f;

                if (FPS > 60f)
                {
                    ultra();
                }
                else if (FPS > 50f)
                {
                    high();
                }
                else if (FPS > 40f)
                {
                    medium();
                }
                else if (FPS > 30f)
                {
                    low();
                }
                else if (FPS < 20f)
                {
                    ultraLow();
                }
            }
            if (AdaptiveGraphicsEnabled && (GfxLowEnabled))
            {
                AdaptiveGraphicsEnabled.SetValue(false);
            }
        }

        private static IEnumerator RamClearLoop()
        {
            for (; ; )
            {
                if (EscapeLoop)
                {
                    break;
                }
                yield return new WaitForSeconds(5f);
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }
        }

        public static void ultra()
        {
            QualitySettings.antiAliasing = 8;
            QualitySettings.pixelLightCount = 8;
            QualitySettings.lodBias = 2f;
            QualitySettings.shadowDistance = 200f;
            if (lastMessage != "Ultra")
            {
                ReLogger.Msg("Setting graphics to Ultra");
                ReModCE_ARES.LogDebug("Setting graphics to Ultra");
                lastMessage = "Ultra";
            }
        }
        public static void high()
        {
            QualitySettings.antiAliasing = 6;
            QualitySettings.pixelLightCount = 6;
            QualitySettings.lodBias = 1.5f;
            QualitySettings.shadowDistance = 180f;
            if (lastMessage != "High")
            {
                ReLogger.Msg("Setting graphics to High");
                ReModCE_ARES.LogDebug("Setting graphics to High");
                lastMessage = "High";
            }
        }

        public static void medium()
        {
            QualitySettings.antiAliasing = 4;
            QualitySettings.pixelLightCount = 4;
            QualitySettings.lodBias = 1.2f;
            QualitySettings.shadowDistance = 120f;
            if (lastMessage != "Med")
            {
                ReLogger.Msg("Setting graphics to Med");
                ReModCE_ARES.LogDebug("Setting graphics to Med");
                lastMessage = "Med";
            }
        }

        public static void low()
        {
            QualitySettings.antiAliasing = 2;
            QualitySettings.pixelLightCount = 2;
            QualitySettings.lodBias = 0.8f;
            QualitySettings.shadowDistance = 80f;
            if (lastMessage != "Low")
            {
                ReLogger.Msg("Setting graphics to Low");
                ReModCE_ARES.LogDebug("Setting graphics to Low");
                lastMessage = "Low";
            }
        }

        public static void ultraLow()
        {
            QualitySettings.antiAliasing = 0;
            QualitySettings.pixelLightCount = 0;
            QualitySettings.lodBias = 0.4f;
            QualitySettings.shadowDistance = 20f;
            if (lastMessage != "Ultra Low")
            {
                ReLogger.Msg("Setting graphics to Ultra Low");
                ReModCE_ARES.LogDebug("Setting graphics to Ultra Low");
                lastMessage = "Ultra Low";
            }
        }
    }
}
