using MelonLoader;
using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI.QuickMenu;
using ReModCE_ARES.ApplicationBot;
using ReModCE_ARES.Loader;
using ReModCE_ARES.Managers;
using ReModCE_ARES.SDK;
using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.UI;

namespace ReModCE_ARES.Components
{
    internal class ApplicationBotComponent : ModComponent
    {
        private bool ServerStart = false;
        public ApplicationBotComponent()
        {
        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            base.OnUiManagerInit(uiManager);

            var menu = uiManager.MainMenu.GetCategoryPage(Page.PageNames.ApplicationBots);
            var botControls = menu.AddCategory("Bot Controls");
            var botFunny = menu.AddCategory("Bot Funnies");
            var botMovement = menu.AddCategory("Bot Movement");

            botControls.AddButton($"Start Bot 1 with GUI", "Start application bot 1", delegate { if (!ServerStart) { SocketConnection.StartServer(); ServerStart = true; }; Bot.MakeBot(1, true); }, ResourceManager.GetSprite("remodce.exit-door"));
            botControls.AddButton($"Start Bot 1 without GUI", "Start application bot 1", delegate { if (!ServerStart) { SocketConnection.StartServer(); ServerStart = true; }; Bot.MakeBot(1, false); }, ResourceManager.GetSprite("remodce.exit-door"));

            botControls.AddButton($"Start Bot 2 with GUI", "Start application bot 2", delegate { if (!ServerStart) { SocketConnection.StartServer(); ServerStart = true; }; Bot.MakeBot(2, true); }, ResourceManager.GetSprite("remodce.exit-door"));
            botControls.AddButton($"Start Bot 2 without GUI", "Start application bot 2", delegate { if (!ServerStart) { SocketConnection.StartServer(); ServerStart = true; }; Bot.MakeBot(2, false); }, ResourceManager.GetSprite("remodce.exit-door"));

            botControls.AddButton($"Stop Bot 1", "Quits bot 1", delegate { SocketConnection.SendCommandToClients($"Kill Number1"); }, ResourceManager.GetSprite("remodce.exit-door"));
            botControls.AddButton($"Stop Bot 2", "Quits Bot 2", delegate { SocketConnection.SendCommandToClients($"Kill Number2"); }, ResourceManager.GetSprite("remodce.exit-door"));

            botFunny.AddButton($"Join yourself", "Make bots join you.", delegate { SocketConnection.SendCommandToClients($"JoinWorld {RoomExtensions.Current_World_ID}"); }, ResourceManager.GetSprite("remodce.exit-door"));
            botFunny.AddButton($"Orbit yourself", "Make bots orbit you.", delegate { SocketConnection.SendCommandToClients($"OrbitSelected {Wrapper.LocalPlayer().field_Private_APIUser_0.id}"); }, ResourceManager.GetSprite("remodce.exit-door"));
            botFunny.AddButton($"Sit on yourself", "Make bots sit on you.", delegate { SocketConnection.SendCommandToClients($"SitOn {Wrapper.LocalPlayer().field_Private_APIUser_0.id}"); }, ResourceManager.GetSprite("remodce.exit-door"));
            botFunny.AddButton($"Spin bot", "Make bots spin.", delegate { SocketConnection.SendCommandToClients($"SpinbotToggle true"); }, ResourceManager.GetSprite("remodce.exit-door"));
            botFunny.AddButton($"Change avatar", "Change avatar by ID", delegate { if (CloneID()) { SocketConnection.SendCommandToClients($"SetAvatar {Clipboard.GetText()}"); } }, ResourceManager.GetSprite("remodce.exit-door"));
            botFunny.AddButton($"Follow yourself", "Make bots follow you.", delegate { SocketConnection.SendCommandToClients($"Follow {Wrapper.LocalPlayer().field_Private_APIUser_0.id}"); }, ResourceManager.GetSprite("remodce.exit-door"));


            botMovement.AddButton($"Move forward", "move bot.", delegate { SocketConnection.SendCommandToClients($"MoveForwards "); }, ResourceManager.GetSprite("remodce.exit-door"));
            botMovement.AddButton($"Move back", "Rotate bot.", delegate { SocketConnection.SendCommandToClients($"MoveBackwards "); }, ResourceManager.GetSprite("remodce.exit-door"));
            botMovement.AddButton($"Move left", "Rotate bot.", delegate { SocketConnection.SendCommandToClients($"MoveLeft "); }, ResourceManager.GetSprite("remodce.exit-door"));
            botMovement.AddButton($"Move right", "Rotate bot.", delegate { SocketConnection.SendCommandToClients($"MoveRight "); }, ResourceManager.GetSprite("remodce.exit-door"));
            botMovement.AddButton($"Move Up", "Rotate bot.", delegate { SocketConnection.SendCommandToClients($"MoveUp "); }, ResourceManager.GetSprite("remodce.exit-door"));
            botMovement.AddButton($"Move Down", "Rotate bot.", delegate { SocketConnection.SendCommandToClients($"MoveDown "); }, ResourceManager.GetSprite("remodce.exit-door"));
            botMovement.AddButton($"Rotate Left", "Rotate bot.", delegate { SocketConnection.SendCommandToClients("RotateY -20"); }, ResourceManager.GetSprite("remodce.exit-door"));
            botMovement.AddButton($"Rotate right", "Rotate bot.", delegate { SocketConnection.SendCommandToClients("RotateY 20"); }, ResourceManager.GetSprite("remodce.exit-door"));


            botControls.AddButton($"Stop Following things", "Make bots stop orbiting and sitting on.", delegate { SocketConnection.SendCommandToClients($"StopFollow ok"); }, ResourceManager.GetSprite("remodce.exit-door"));
            botControls.AddButton($"Stop spin", "Make bots stop orbiting and sitting on.", delegate { SocketConnection.SendCommandToClients($"SpinbotToggle "); }, ResourceManager.GetSprite("remodce.exit-door"));
        }

        private static GameObject SocialMenuInstance;

        public static GameObject GetSocialMenuInstance()
        {
            if (SocialMenuInstance == null)
            {
                SocialMenuInstance = GameObject.Find("UserInterface/MenuContent/Screens");
            }
            return SocialMenuInstance;
        }

        private static bool CloneID()
        {
            Regex Avatar = new Regex("avtr_[0-9a-fA-F]{8}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{4}\\-[0-9a-fA-F]{12}");
            if (Avatar.IsMatch(Clipboard.GetText()))
            {
                return true;
            }
            else
            {
                MelonLogger.Msg($"Invalid Avatar ID!");
            }
            return false;
        }
    }
}
