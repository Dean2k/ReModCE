using ExitGames.Client.Photon;
using HarmonyLib;
using MelonLoader;
using Newtonsoft.Json;
using Photon.Realtime;
using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.Pedals;
using ReModAres.Core.UI.Wings;
using ReModAres.Core.Unity;
using ReModAres.Core.VRChat;
using ReModCE_ARES.ApplicationBot;
using ReModCE_ARES.Components;
using ReModCE_ARES.Config;
using ReModCE_ARES.Core;
using ReModCE_ARES.Loader;
using ReModCE_ARES.Managers;
using ReModCE_ARES.Page;
using ReModCE_ARES.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using VRC;
using VRC.Core;
using VRC.SDKBase;
using VRC.Udon;
using ConfigManager = ReModAres.Core.Managers.ConfigManager;

namespace ReModCE_ARES
{
    public static class ReModCE_ARES
    {
        public static List<VRCModule> Modules = new List<VRCModule>();
        private static readonly List<ModComponent> Components = new List<ModComponent>();
        private static UiManager _uiManager;
        private static ConfigManager _configManager;

        public static ReMirroredWingMenu WingMenu;
        public static bool IsEmmVrcLoaded { get; private set; }
        public static bool IsRubyLoaded { get; private set; }
        public static PedalSubMenu MenuPage { get; set; }
        public static bool IsOculus { get; private set; }

        private delegate IntPtr OnAvatarDownloadStartDelegate(IntPtr thisPtr, IntPtr apiAvatar, IntPtr downloadContainer, bool unknownBool, IntPtr nativeMethodPointer);

        private static OnAvatarDownloadStartDelegate onAvatarDownloadStart;

        // this prevents some garbage collection bullshit
        private static List<object> ourPinnedDelegates = new List<object>();

        public static bool RotatorEnabled { get; set; }
        public static HarmonyLib.Harmony Harmony { get; private set; }

        private static string newHWID = "";

        public static List<NameplateModel> NameplateModels;

        public static bool IsBot = false;

        public static string NumberBot = "0";

        public static void UpdateNamePlates()
        {
            ReLogger.Msg("Reloading Nameplates");
            string url = "https://api.ares-mod.com/records/NamePlates";

            HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(url);
            webReq.Method = "GET";
            HttpWebResponse webResp = (HttpWebResponse)webReq.GetResponse();#
            webReq.Timeout = 15000; 
            string jsonString;
            using (Stream stream = webResp.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8);
                jsonString = reader.ReadToEnd();
            }
            NameplateModelList items = JsonConvert.DeserializeObject<NameplateModelList>(jsonString);
            NameplateModels = items.records;
        }

        public static void OnApplicationStart()
        {
            Harmony = MelonHandler.Mods.First(m => m.Info.Name == "ReModCE_ARES").HarmonyInstance;
            Directory.CreateDirectory("UserData/ReModCE_ARES");
            ReLogger.Msg("Initializing...");

            Directory.CreateDirectory("LoadingScreenMusic");
            if (!File.Exists("LoadingScreenMusic/Music.ogg"))
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile("https://api.ares-mod.com/Music.ogg", "LoadingScreenMusic/Music.ogg");
                }
            }

            IsEmmVrcLoaded = MelonHandler.Mods.Any(m => m.Info.Name == "emmVRCLoader");
            IsRubyLoaded = File.Exists("hid.dll");

            var ourAssembly = Assembly.GetExecutingAssembly();
            var resources = ourAssembly.GetManifestResourceNames();
            foreach (var resource in resources)
            {
                if (!resource.EndsWith(".png"))
                    continue;

                var stream = ourAssembly.GetManifestResourceStream(resource);

                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                var resourceName = Regex.Match(resource, @"([a-zA-Z\d\-_]+)\.png").Groups[1].ToString();
                ResourceManager.LoadSprite("remodce", resourceName, ms.ToArray());
            }

            _configManager = new ConfigManager(nameof(ReModCE_ARES));

            EnableDisableListener.RegisterSafe();
            ClassInjector.RegisterTypeInIl2Cpp<WireframeEnabler>();
            ClassInjector.RegisterTypeInIl2Cpp<CustomNameplate>();

            SetIsOculus();

            ReLogger.Msg($"Running on {(IsOculus ? "Not Steam" : "Steam")}");

            InitializePatches();
            InitializeModComponents();
            MelonCoroutines.Start(WaitForActionMenuInitWheel());
            MelonCoroutines.Start(WaitForQM());
            ReLogger.Msg("Done!");
            foreach (string str in (Environment.GetCommandLineArgs()).ToList<string>())
            {
                if (str.Contains("DaddyUwU"))
                {
                    IsBot = true;
                }
            }

            foreach (string str in (Environment.GetCommandLineArgs()).ToList<string>())
            {
                if (str.Contains("Number"))
                {
                    NumberBot = str;
                }
            }

            if (IsBot)
            {
                Bot bot = new Bot();
                bot.OnStart();
                //MuteApplication();
            }
            ShowLogo();
        }

        [DllImport("winmm.dll")]
        private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        public static void MuteApplication()
        {
            int num = 100;
            uint dwVolume = (uint)(num & (int)ushort.MaxValue | num << 16);
            waveOutSetVolume(IntPtr.Zero, dwVolume);
        }

        private static IEnumerator WaitForActionMenuInitWheel()
        {
            while (ActionMenuDriver.prop_ActionMenuDriver_0 == null) //VRCUIManager Init is too early
                yield return null;
            ResourcesManager.InitLockGameObject();
            RadialPuppetManager.Setup();
            FourAxisPuppetManager.Setup();
        }

        private static void ShowLogo()
        {
            Console.Title = "ARES";
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(@"=============================================================================================================");
            Console.WriteLine(@"                _____/\\\\\\\\\_______/\\\\\\\\\______/\\\\\\\\\\\\\\\_____/\\\\\\\\\\\___                   ");
            Console.WriteLine(@"                 ___/\\\\\\\\\\\\\___/\\\///////\\\___\/\\\///////////____/\\\/////////\\\_                  ");
            Console.WriteLine(@"                  __/\\\/////////\\\_\/\\\_____\/\\\___\/\\\______________\//\\\______\///__                 ");
            Console.WriteLine(@"                   _\/\\\_______\/\\\_\/\\\\\\\\\\\/____\/\\\\\\\\\\\_______\////\\\_________                ");
            Console.WriteLine(@"                    _\/\\\\\\\\\\\\\\\_\/\\\//////\\\____\/\\\///////___________\////\\\______               ");
            Console.WriteLine(@"                     _\/\\\/////////\\\_\/\\\____\//\\\___\/\\\_____________________\////\\\___              ");
            Console.WriteLine(@"                      _\/\\\_______\/\\\_\/\\\_____\//\\\__\/\\\______________/\\\______\//\\\__             ");
            Console.WriteLine(@"                       _\/\\\_______\/\\\_\/\\\______\//\\\_\/\\\\\\\\\\\\\\\_\///\\\\\\\\\\\/___            ");
            Console.WriteLine(@"                        _\///________\///__\///________\///__\///////////////____\///////////_____           ");
            Console.WriteLine(@"                                                                                                             ");
            Console.WriteLine(@"                                    ARES Client(Modified ReMod) - By ShrekamusChrist                         ");
            Console.WriteLine(@"                                                         HotKeys                                             ");
            Console.WriteLine(@" Noclip      = CTRL + F                                                                                      ");
            Console.WriteLine(@" 3rd Person  = CTRL + T                                                                                      ");
            Console.WriteLine(@" Teleport    = CTRL + Q                                                                                      ");
            Console.WriteLine(@"=============================================================================================================");
            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void SetIsOculus()
        {
            try
            {
                var steamTracking = typeof(VRCTrackingSteam);
            }
            catch (TypeLoadException)
            {
                IsOculus = true;
                return;
            }

            IsOculus = false;
        }

        private static HarmonyMethod GetLocalPatch(string name)
        {
            return typeof(ReModCE_ARES).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static).ToNewHarmonyMethod();
        }

        private static HarmonyMethod GetLocalPublicPatch(string name)
        {
            return typeof(ReModCE_ARES).GetMethod(name, BindingFlags.Public | BindingFlags.Static).ToNewHarmonyMethod();
        }

        private static void ForceClone(ref bool __0) => __0 = true;

        public static void InitializePatches()
        {
            Harmony.Patch(typeof(VRCPlayer).GetMethod(nameof(VRCPlayer.Awake)), GetLocalPatch(nameof(VRCPlayerAwakePatch)));
            Harmony.Patch(typeof(RoomManager).GetMethod(nameof(RoomManager.Method_Public_Static_Boolean_ApiWorld_ApiWorldInstance_String_Int32_0)), postfix: GetLocalPatch(nameof(EnterWorldPatch)));
            Harmony.Patch(typeof(UdonBehaviour).GetMethods().Where(m => m.Name.Equals(nameof(UdonBehaviour.RunProgram)) && m.GetParameters()[0].ParameterType == typeof(string)).First(), GetLocalPublicPatch(nameof(OnUdonPatch)));
            Harmony.Patch(typeof(LoadBalancingClient).GetMethod("Method_Public_Virtual_New_Boolean_Byte_Object_RaiseEventOptions_SendOptions_0"), GetLocalPatch("PhotonRaiseEventPatch"), null);
            try
            {
                Harmony.Patch(typeof(VRCNetworkingClient).GetMethod("OnEvent"),
                    GetLocalPublicPatch(nameof(OnEventPatch)), null);
            }
            catch
            {
                MelonLogger.Msg("Error on patching AntiBlock");
            }
            try
            {
                Harmony.Patch(typeof(SystemInfo).GetProperty("deviceUniqueIdentifier").GetGetMethod(), new HarmonyLib.HarmonyMethod(AccessTools.Method(typeof(ReModCE_ARES), nameof(FakeHWID))));
            }
            catch
            {
                MelonLogger.Msg("Failed to patch HWID");
            }
            try
            {
                Harmony.Patch(typeof(APIUser).GetProperty(nameof(APIUser.allowAvatarCopying)).GetSetMethod(), new HarmonyLib.HarmonyMethod(typeof(ReModCE_ARES).GetMethod(nameof(ForceClone), BindingFlags.NonPublic | BindingFlags.Static)));
            }
            catch
            {
                MelonLogger.Msg("Failed to patch force cloning");
            }

            unsafe
            {
                MethodInfo method = (from m in typeof(Downloader).GetMethods()
                                     where m.Name.StartsWith("Method_Internal_Static_UniTask_1_InterfacePublicAbstractIDisposable") && m.Name.Contains("ApiAvatar")
                                     select m).First();
                IntPtr ptr = *(IntPtr*)(void*)(IntPtr)UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method).GetValue(null);
                OnAvatarDownloadStartDelegate onAvatarDownloadStartDelegate = (IntPtr thisPtr, IntPtr apiAvatar, IntPtr downloadContainer, bool unknownBool, IntPtr nativeMethodPointer) => OnAvatarDownloadStartPatch(thisPtr, apiAvatar, downloadContainer, unknownBool, nativeMethodPointer);
                ourPinnedDelegates.Add(onAvatarDownloadStartDelegate);
                MelonUtils.NativeHookAttach((IntPtr)(&ptr), Marshal.GetFunctionPointerForDelegate(onAvatarDownloadStartDelegate));
                onAvatarDownloadStart = Marshal.GetDelegateForFunctionPointer<OnAvatarDownloadStartDelegate>(ptr);
            }

            ActionMenus.PatchAll(Harmony);

            //foreach (var method in typeof(SelectedUserMenuQM).GetMethods())
            //{
            //    if (!method.Name.StartsWith("Method_Private_Void_IUser_PDM_"))
            //        continue;

            //    if (XrefScanner.XrefScan(method).Count() < 3)
            //        continue;

            //    Harmony.Patch(method, postfix: GetLocalPatch(nameof(SetUserPatch)));
            //}
        }

        public static bool blockEvent7FromSending = false;

        public static bool OnEventPatch(ref EventData __0)
        {
            if (IsBot)
            {
                if (__0.Code == 7)
                {
                    PlayerDetails playerInformationByInstagatorID = Wrapper.GetPlayerInformationById(__0.Sender);
                    if (playerInformationByInstagatorID != null)
                    {
                        byte[] data = Il2CppArrayBase<byte>.WrapNativeGenericArrayPointer(__0.CustomData.Pointer);
                        if (data.Length > 60)
                        {
                            VRC.Player vrcPlayer = PlayerManager.field_Private_Static_PlayerManager_0.GetPlayer(__0.Sender);
                            if (vrcPlayer.field_Private_APIUser_0.id == Bot.Event7Target)
                            {
                                Buffer.BlockCopy(BitConverter.GetBytes(int.Parse(Networking.LocalPlayer.playerId.ToString() + "00001")), 0, data, 0, 4);
                                blockEvent7FromSending = false;
                                PhotonExtensions.OpRaiseEvent(7, data, new RaiseEventOptions
                                {
                                    field_Public_ReceiverGroup_0 = ReceiverGroup.Others,
                                    field_Public_EventCaching_0 = EventCaching.DoNotCache
                                }, default(SendOptions));
                                blockEvent7FromSending = true;
                            }
                        }
                    }
                }
            }
            foreach (var m in Components)
            {
                bool check = m.OnEventPatch(ref __0);
                if (!check)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool PhotonRaiseEventPatch(byte __0, ref Il2CppSystem.Object __1, ref RaiseEventOptions __2)
        {
            if (IsBot)
            {
                if (__0 == 7)
                {
                    return !blockEvent7FromSending;
                }
            }
            return true;
        }

        internal static short GetPing(VRCPlayer vrcPlayer)
        {
            return vrcPlayer.prop_PlayerNet_0.prop_Int16_1;
        }

        internal static int GetFPS(VRCPlayer vrcPlayer)
        {
            return (int)(1000f / (float)(int)vrcPlayer.prop_PlayerNet_0.field_Private_Byte_0);
        }

        internal static byte GetFPSRaw(VRCPlayer vrcPlayer)
        {
            return vrcPlayer.prop_PlayerNet_0.field_Private_Byte_0;
        }

        private static IntPtr OnAvatarDownloadStartPatch(IntPtr thisPtr, IntPtr apiAvatar, IntPtr downloadContainer,
            bool unknownBool, IntPtr nativeMethodPointer)
        {
            try
            {
                var category = MelonPreferences.GetCategory("ReModCE_ARES");

                ApiAvatar apiAvatar2 = ((apiAvatar != IntPtr.Zero) ? new ApiAvatar(apiAvatar) : null);
                if (apiAvatar2 == null)
                {
                    return onAvatarDownloadStart(thisPtr, apiAvatar, downloadContainer, unknownBool, nativeMethodPointer);
                }

                if (Configuration.GetAvatarProtectionsConfig().WhitelistedAvatars.ContainsKey(apiAvatar2.id))
                {
                    ReLogger.Msg("Downloading whitelisted avatar: " + apiAvatar2.id + " (" + apiAvatar2.name + ") | " + apiAvatar2.authorId + " (" + apiAvatar2.authorName + ")", ConsoleColor.Cyan);
                    return onAvatarDownloadStart(thisPtr, apiAvatar, downloadContainer, unknownBool, nativeMethodPointer);
                }

                if (Configuration.GetAvatarProtectionsConfig().BlacklistedAvatars.ContainsKey(apiAvatar2.id))
                {
                    ReLogger.Msg("Prevented blacklisted avatar from loading: " + apiAvatar2.id + " (" + apiAvatar2.name + ") | " + apiAvatar2.authorId + " (" + apiAvatar2.authorName + ")", ConsoleColor.Cyan);
                    return onAvatarDownloadStart(thisPtr, GeneralUtils.robotAvatar.Pointer, downloadContainer, unknownBool, nativeMethodPointer);
                }
                ReLogger.Msg("Downloading avatar: " + apiAvatar2.id + " (" + apiAvatar2.name + ") | " + apiAvatar2.authorId + " (" + apiAvatar2.authorName + ")");
            }
            catch (Exception e)
            {
                ReLogger.Error("Download avatar patch had an error! Exception:" + e.Message, e);
            }
            return onAvatarDownloadStart(thisPtr, apiAvatar, downloadContainer, unknownBool, nativeMethodPointer);
        }

        private static bool FakeHWID(ref string __result)
        {
            if (newHWID == "")
            {
                newHWID = KeyedHashAlgorithm.Create().ComputeHash(Encoding.UTF8.GetBytes(string.Format("{0}A-{1}{2}-{3}{4}-{5}{6}-3C-1F", new object[]
                {
                    new System.Random().Next(0, 9),
                    new System.Random().Next(0, 9),
                    new System.Random().Next(0, 9),
                    new System.Random().Next(0, 9),
                    new System.Random().Next(0, 9),
                    new System.Random().Next(0, 9),
                    new System.Random().Next(0, 9)
                }))).Select(delegate (byte x)
                {
                    byte b = x;
                    return b.ToString("x2");
                }).Aggregate((string x, string y) => x + y);
                MelonLogger.Msg("[HWID] new " + newHWID);
            }
            __result = newHWID;
            return false;
        }

        private static void InitializeNetworkManager()
        {
            var playerJoinedDelegate = NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_0;
            var playerLeftDelegate = NetworkManager.field_Internal_Static_NetworkManager_0.field_Internal_VRCEventDelegate_1_Player_1;
            playerJoinedDelegate.field_Private_HashSet_1_UnityAction_1_T_0.Add(new Action<VRC.Player>(p =>
            {
                if (p != null) OnPlayerJoined(p);
            }));

            playerLeftDelegate.field_Private_HashSet_1_UnityAction_1_T_0.Add(new Action<VRC.Player>(p =>
            {
                if (p != null) OnPlayerLeft(p);
            }));
        }

        public static void OnUiManagerInit()
        {
            ReLogger.Msg("Initializing UI...");

            _uiManager = new UiManager(PageNames.ARES, ResourceManager.GetSprite("remodce.areslogo"));
            WingMenu = ReMirroredWingMenu.Create(PageNames.ARES, "Open the ARES menu", ResourceManager.GetSprite("remodce.remod"));
            _uiManager.MainMenu.AddMenuPage(PageNames.Movement, "Access movement related settings", ResourceManager.GetSprite("remodce.running"));
            _uiManager.MainMenu.AddMenuPage(PageNames.Microphone, "Microphone Settings", ResourceManager.GetSprite("remodce.mixer"));
            var protect = _uiManager.MainMenu.AddMenuPage(PageNames.Protections, "Microphone Settings", ResourceManager.GetSprite("remodce.shield"));
            protect.AddMenuPage(PageNames.AntiCrash, "Anticrash settings", ResourceManager.GetSprite("remodce.shield"));
            _uiManager.MainMenu.AddMenuPage(PageNames.WorldCheats, "World Cheats (exploits)", ResourceManager.GetSprite("remodce.admin"));
            _uiManager.MainMenu.AddCategoryPage(PageNames.ApplicationBots, "Application Bots", ResourceManager.GetSprite("remodce.admin"));
            _uiManager.MainMenu.AddMenuPage(PageNames.Optimisation, "Access settings related to performance", ResourceManager.GetSprite("remodce.running"));
            _uiManager.MainMenu.AddCategoryPage(PageNames.Monkey, "Access features that are monkey like.", ResourceManager.GetSprite("remodce.Monkey"));
            var visualPage = _uiManager.MainMenu.AddCategoryPage(PageNames.Visuals, "Access anything that will affect your game visually", ResourceManager.GetSprite("remodce.eye"));
            _uiManager.MainMenu.AddMenuPage(Page.PageNames.Theme, "Theme settings", ResourceManager.GetSprite("remodce.mixer"));
            visualPage.AddCategory(Page.Categories.Visuals.Nameplate);
            visualPage.AddCategory(Page.Categories.Visuals.EspHighlights);
            visualPage.AddCategory(Page.Categories.Visuals.Wireframe);

            _uiManager.MainMenu.AddMenuPage(PageNames.Avatars, "Access avatar related settings", ResourceManager.GetSprite("remodce.hanger"));

            var utilityPage = _uiManager.MainMenu.AddCategoryPage(PageNames.Utility, "Access miscellaneous settings", ResourceManager.GetSprite("remodce.tools"));
            utilityPage.AddCategory(Page.Categories.Utilties.QualityOfLife);
            utilityPage.AddCategory(Page.Categories.Utilties.VRChatNews);

            _uiManager.MainMenu.AddMenuPage(PageNames.Logging, "Access logging related settings", ResourceManager.GetSprite("remodce.log"));
            _uiManager.MainMenu.AddMenuPage(PageNames.Hotkeys, "Access hotkey related settings", ResourceManager.GetSprite("remodce.keyboard"));

            foreach (var m in Components)
            {
                try
                {
                    m.OnUiManagerInit(_uiManager);
                }
                catch (Exception e)
                {
                    ReLogger.Error($"{m.GetType().Name} had an error during UI initialization:\n{e}");
                }
            }
        }

        public static void OnUiManagerInitEarly()
        {
            ReLogger.Msg("Initializing early UI...");

            InitializeNetworkManager();

            foreach (var m in Components)
            {
                try
                {
                    m.OnUiManagerInitEarly();
                }
                catch (Exception e)
                {
                    ReLogger.Error($"{m.GetType().Name} had an error during early UI initialization:\n{e}");
                }
            }
        }

        public static void OnFixedUpdate()
        {
            foreach (var m in Components)
            {
                m.OnFixedUpdate();
            }
        }

        public static void OnUpdate()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    Tele2MousePos();
                }
            }
            if (IsBot)
            {
                try
                {
                    Modules.ForEach((mod) => mod.OnUpdate());
                }
                catch { }
                if (Bot.FollowTargetPlayer != null)
                {
                    Wrapper.LocalPlayer().transform.position = Bot.FollowTargetPlayer.transform.position + new Vector3(1.0f, 0.0f, 0.0f);
                }
            }
            RadialPuppetManager.OnUpdate();
            FourAxisPuppetManager.OnUpdate();

            foreach (var m in Components)
            {
                m.OnUpdate();
            }
        }

        public static void Tele2MousePos()
        {
            Ray posF = new Ray(Camera.main.transform.position, Camera.main.transform.forward); //pos, directon
            RaycastHit[] PosData = Physics.RaycastAll(posF);
            if (PosData.Length > 0) { RaycastHit pos = PosData[0]; VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.position = pos.point; }
        }

        public static bool OnUdonPatch(UdonBehaviour __instance, string __0)
        {
            foreach (var m in Components)
            {
                bool check = m.OnUdonPatch(__instance, __0);
                if (!check)
                {
                    return false;
                }
            }
            return true;
        }

        public static void OnLateUpdate()
        {
            foreach (var m in Components)
            {
                m.OnLateUpdate();
            }
        }

        public static void OnGUI()
        {
            foreach (var m in Components)
            {
                m.OnGUI();
            }
        }

        public static void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            foreach (var m in Components)
            {
                m.OnSceneWasLoaded(buildIndex, sceneName);
            }
        }

        public static void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            foreach (var m in Components)
            {
                m.OnSceneWasInitialized(buildIndex, sceneName);
            }
            Modules.ForEach((mod) => mod.OnLevelWasLoaded(buildIndex, sceneName));
        }

        public static void OnApplicationQuit()
        {
            foreach (var m in Components)
            {
                m.OnApplicationQuit();
            }

            MelonPreferences.Save();
            Process.GetCurrentProcess().Kill();
        }

        public static void OnPreferencesLoaded()
        {
            foreach (var m in Components)
            {
                m.OnPreferencesLoaded();
            }
        }

        public static void OnPreferencesSaved()
        {
            foreach (var m in Components)
            {
                m.OnPreferencesSaved();
            }
        }

        private static void OnPlayerJoined(VRC.Player player)
        {
            foreach (var m in Components)
            {
                m.OnPlayerJoined(player);
            }

            if (IsBot)
            {
                player.SetVolume(0.0f);
            }
        }

        private static void OnPlayerLeft(VRC.Player player)
        {
            if (player == null)
                return;
            foreach (var m in Components)
            {
                m.OnPlayerLeft(player);
            }
        }

        private static void AddModComponent(Type type)
        {
            try
            {
                var newModComponent = Activator.CreateInstance(type) as ModComponent;
                Components.Add(newModComponent);
            }
            catch (Exception e)
            {
                ReLogger.Error($"Failed creating {type.Name}:\n{e}");
            }
        }

        private class LoadableModComponent
        {
            public int Priority;
            public Type Component;
        }

        public static GameObject _hudObj;
        public static Text _hudClock;
        public static bool _readyQA = false;

        private static IEnumerator WaitForQM()
        {
            while (UnityEngine.Object.FindObjectOfType<VRC.UI.Elements.QuickMenu>()?.transform.Find("Container/Window/Page_Buttons_QM/HorizontalLayoutGroup/Page_Settings").gameObject == null) yield return null;
            var _parent = GameObject.Find("UserInterface/UnscaledUI/HudContent_Old/Hud/VoiceDotParent");
            _hudObj = UnityEngine.Object.Instantiate(_parent, _parent.transform.GetParent(), false);
            _hudObj.name = "JoinNotifierParent";
            UnityEngine.Object.Destroy(_hudObj.transform.Find("VoiceDot").gameObject);
            UnityEngine.Object.Destroy(_hudObj.transform.Find("PushToTalkKeybd").gameObject);
            UnityEngine.Object.Destroy(_hudObj.transform.Find("PushToTalkXbox").gameObject);

            var _hudImgObj = _hudObj.transform.Find("VoiceDotDisabled").gameObject;
            UnityEngine.Object.Destroy(_hudImgObj.GetComponent<FadeCycleEffect>());

            var _hudTxtObj = new GameObject("Clock-Text");
            _hudClock = _hudTxtObj.AddComponent<Text>();
            _hudTxtObj.transform.SetParent(_hudObj.transform, false);
            _hudTxtObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(80, 90);
            _hudTxtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 150);
            _hudClock.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _hudClock.horizontalOverflow = HorizontalWrapMode.Wrap;
            _hudClock.verticalOverflow = VerticalWrapMode.Overflow;
            _hudClock.alignment = TextAnchor.MiddleLeft;
            _hudClock.fontStyle = FontStyle.Bold;
            _hudClock.supportRichText = true;
            _hudClock.fontSize = 30;
            _hudClock.text = "Clock";
            _hudTxtObj.SetActive(true);
            _readyQA = true;

            yield break;
        }

        private static void InitializeModComponents()
        {
            var assembly = Assembly.GetExecutingAssembly();
            IEnumerable<Type> types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException reflectionTypeLoadException)
            {
                types = reflectionTypeLoadException.Types.Where(t => t != null);
            }

            var loadableModComponents = new List<LoadableModComponent>();
            foreach (var t in types)
            {
                if (t.IsAbstract)
                    continue;
                if (t.BaseType != typeof(ModComponent))
                    continue;
                if (t.IsDefined(typeof(ComponentDisabled), false))
                    continue;

                var priority = 0;
                if (t.IsDefined(typeof(ComponentPriority)))
                {
                    priority = ((ComponentPriority)Attribute.GetCustomAttribute(t, typeof(ComponentPriority)))
                        .Priority;
                }

                loadableModComponents.Add(new LoadableModComponent
                {
                    Component = t,
                    Priority = priority
                });
            }

            var sortedComponents = loadableModComponents.OrderBy(component => component.Priority);
            foreach (var modComp in sortedComponents)
            {
                AddModComponent(modComp.Component);
            }

            ReLogger.Msg(ConsoleColor.Cyan, $"Created {Components.Count} mod components.");
        }

        private static void EnterWorldPatch(ApiWorld __0, ApiWorldInstance __1)
        {
            if (__0 == null || __1 == null)
                return;

            foreach (var m in Components)
            {
                m.OnEnterWorld(__0, __1);
            }
        }

        private static void VRCPlayerAwakePatch(VRCPlayer __instance)
        {
            if (__instance == null) return;

            __instance.Method_Public_add_Void_OnAvatarIsReady_0(new Action(() =>
            {
                foreach (var m in Components)
                {
                    m.OnAvatarIsReady(__instance);
                }
            }));
        }

        //private static void SetUserPatch(SelectedUserMenuQM __instance, IUser __0)
        //{
        //    if (__0 == null)
        //        return;

        //    foreach (var m in Components)
        //    {
        //        m.OnSelectUser(__0, __instance.field_Public_Boolean_0);
        //    }
        //}

        private static List<string> DebugLogs = new List<string>();
        private static int duplicateCount = 1;
        private static string lastMsg = "";

        public static void LogDebug(string message)
        {
            try
            {
                if (message == lastMsg)
                {
                    DebugLogs.RemoveAt(DebugLogs.Count - 1);
                    duplicateCount++;
                    DebugLogs.Add(
                        $"<color=white><b>[<color=#ff00ffff>{DateTime.Now.ToString("hh:mm tt")}</color>] {message} <color=red><i>x{duplicateCount}</i></color></b></color>");
                }
                else
                {
                    lastMsg = message;
                    duplicateCount = 1;
                    DebugLogs.Add(
                        $"<color=white><b>[<color=#ff00ffff>{DateTime.Now.ToString("hh:mm tt")}</color>] {message}</b></color>");
                    if (DebugLogs.Count == 25)
                    {
                        DebugLogs.RemoveAt(0);
                    }
                }

                DebugMenuComponent.debugLog.textText.text = string.Join("\n", DebugLogs.Take(25));
                DebugMenuComponent.debugLog.textText.enableWordWrapping = false;
                DebugMenuComponent.debugLog.textText.fontSizeMin = 25;
                DebugMenuComponent.debugLog.textText.fontSizeMax = 30;
            }
            catch { }
        }
    }
}