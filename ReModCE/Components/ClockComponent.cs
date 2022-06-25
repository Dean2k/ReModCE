using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.VRChat;
using ReModCE_ARES.Managers;
using System;
using UnityEngine;
using VRC.UI.Elements;

namespace ReModCE_ARES.Components
{
    internal class ClockComponent : ModComponent
    {
        private ConfigValue<bool> ClockEnabled;
        private ReMenuToggle _clockToggle;
        private bool firstRun = true;
        public ClockComponent()
        {
            ClockEnabled = new ConfigValue<bool>(nameof(ClockEnabled), true);
            ClockEnabled.OnValueChanged += () => _clockToggle.Toggle(ClockEnabled);
        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            base.OnUiManagerInit(uiManager);

            var menu = uiManager.MainMenu.GetCategoryPage(Page.PageNames.Utility).AddCategory("Clock");
            _clockToggle = menu.AddToggle("QM Clock",
                "Enable clock in QM", ToggleClock,
                ClockEnabled);
        }

        public void ToggleClock(bool value)
        {
            ClockEnabled.SetValue(value);
            drawOverlay();
        }

        public override void OnUpdate()
        {
            if (ReModCE_ARES._readyQA)
            {
                if (firstRun)
                {
                    drawOverlay();
                    firstRun = false;
                }
                if (ClockEnabled)
                {                 
                    ReModCE_ARES._hudClock.text = DateTime.Now.ToString("HH:mm:ss");
                }
            }         
        }

        public void drawOverlay()
        {
            if (ClockEnabled)
            {
                ReModCE_ARES._hudObj.SetActive(true);
            }
            else
            {
                ReModCE_ARES._hudObj.SetActive(false);
            }
        }
    }
}
