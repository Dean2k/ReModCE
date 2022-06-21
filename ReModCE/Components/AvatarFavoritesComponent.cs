using MelonLoader;
using Newtonsoft.Json;
using ReModAres.Core;
using ReModAres.Core.Managers;
using ReModAres.Core.UI;
using ReModAres.Core.UI.QuickMenu;
using ReModAres.Core.VRChat;
using ReModCE_ARES.Core;
using ReModCE_ARES.Loader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.XR;
using VRC;
using VRC.Core;
using VRC.DataModel;
using VRC.SDKBase.Validation.Performance.Stats;
using VRC.UI;
using AvatarList = Il2CppSystem.Collections.Generic.List<VRC.Core.ApiAvatar>;
using BuildInfo = ReModCE_ARES.Loader.BuildInfo;

namespace ReModCE_ARES.Components
{
    internal class AvatarFavoritesComponent : ModComponent, IAvatarListOwner
    {
        private ReAvatarList _favoriteAvatarList;
        private ReAvatarList _favoriteAvatarList1;
        private ReAvatarList _favoriteAvatarList2;
        private ReAvatarList _favoriteAvatarList3;
        private ReUiButton _favoriteButton;
        private ReUiButton _favoriteButton1;
        private ReUiButton _favoriteButton2;
        private ReUiButton _favoriteButton3;

        private ReAvatarList _searchedAvatarList;

        private Button.ButtonClickedEvent _changeButtonEvent;

        private const bool EnableApi = true;
        private const string ApiUrl = "https://api.ares-mod.com";
        private const string ApiUnlockedUrl = "https://unlocked.ares-mod.com";
        private string _userAgent = "";
        private HttpClient _httpClient;
        private HttpClientHandler _httpClientHandler;

        private const string PinPath = "UserData/ReModCE_ARES/pin";
        private int _pinCode;
        private ReMenuButton _enterPinButton;
        private ReMenuButton _apiKeyButton;

        private ConfigValue<bool> AvatarFavoritesEnabled;
        private ConfigValue<bool> AvatarFavoritesEnabled1;
        private ConfigValue<bool> AvatarFavoritesEnabled2;
        private ConfigValue<bool> AvatarFavoritesEnabled3;
        private ConfigValue<string> ApiKey;
        private static PageUserInfo _userInfoPage;
        private ReMenuToggle _enabledToggle;
        private ReMenuToggle _enabledToggle1;
        private ReMenuToggle _enabledToggle2;
        private ReMenuToggle _enabledToggle3;
        private ConfigValue<int> MaxAvatarsPerPage;
        private ReMenuButton _maxAvatarsPerPageButton;

        private static ReMenuButton _vrcaTargetButton;
        private static ReMenuButton _vrcaTargetButton1;
        private static ReMenuButton _vrcaTargetButton2;
        private static ReMenuButton _vrcaTargetButton3;

        private List<ReAvatar> _savedAvatars;
        private List<ReAvatar> _savedAvatars1;
        private List<ReAvatar> _savedAvatars2;
        private List<ReAvatar> _savedAvatars3;
        private readonly AvatarList _searchedAvatars;

        private GameObject _avatarScreen;
        private UiInputField _searchBox;
        private UnityAction<string> _searchAvatarsAction;
        private UnityAction<string> _overrideSearchAvatarsAction;
        private UnityAction<string> _emmVRCsearchAvatarsAction;
        private int _updatesWithoutSearch;
        private APIUser user;


        public AvatarFavoritesComponent()
        {
            AvatarFavoritesEnabled = new ConfigValue<bool>(nameof(AvatarFavoritesEnabled), true);
            AvatarFavoritesEnabled.OnValueChanged += () =>
            {
                _enabledToggle.Toggle(AvatarFavoritesEnabled);
                _enabledToggle1.Toggle(AvatarFavoritesEnabled);
                _enabledToggle2.Toggle(AvatarFavoritesEnabled);
                _enabledToggle3.Toggle(AvatarFavoritesEnabled);
                _favoriteAvatarList.GameObject.SetActive(AvatarFavoritesEnabled);
                _favoriteAvatarList1.GameObject.SetActive(AvatarFavoritesEnabled);
                _favoriteAvatarList2.GameObject.SetActive(AvatarFavoritesEnabled);
                _favoriteAvatarList3.GameObject.SetActive(AvatarFavoritesEnabled);
                _favoriteButton.GameObject.SetActive(AvatarFavoritesEnabled);
                _favoriteButton1.GameObject.SetActive(AvatarFavoritesEnabled);
                _favoriteButton2.GameObject.SetActive(AvatarFavoritesEnabled);
                _favoriteButton3.GameObject.SetActive(AvatarFavoritesEnabled);
            };
            AvatarFavoritesEnabled1 = new ConfigValue<bool>(nameof(AvatarFavoritesEnabled1), true);
            AvatarFavoritesEnabled1.OnValueChanged += () =>
            {
                _favoriteAvatarList.GameObject.SetActive(AvatarFavoritesEnabled1);
                _favoriteButton.GameObject.SetActive(AvatarFavoritesEnabled1);
            };
            AvatarFavoritesEnabled2 = new ConfigValue<bool>(nameof(AvatarFavoritesEnabled2), true);
            AvatarFavoritesEnabled2.OnValueChanged += () =>
            {
                _favoriteAvatarList.GameObject.SetActive(AvatarFavoritesEnabled2);
                _favoriteButton.GameObject.SetActive(AvatarFavoritesEnabled2);
            };
            AvatarFavoritesEnabled3 = new ConfigValue<bool>(nameof(AvatarFavoritesEnabled3), true);
            AvatarFavoritesEnabled3.OnValueChanged += () =>
            {
                _favoriteAvatarList.GameObject.SetActive(AvatarFavoritesEnabled3);
                _favoriteButton.GameObject.SetActive(AvatarFavoritesEnabled3);
            };
            MaxAvatarsPerPage = new ConfigValue<int>(nameof(MaxAvatarsPerPage), 100);
            MaxAvatarsPerPage.OnValueChanged += () =>
            {
                _favoriteAvatarList.SetMaxAvatarsPerPage(MaxAvatarsPerPage);
                _favoriteAvatarList1.SetMaxAvatarsPerPage(MaxAvatarsPerPage);
                _favoriteAvatarList2.SetMaxAvatarsPerPage(MaxAvatarsPerPage);
                _favoriteAvatarList3.SetMaxAvatarsPerPage(MaxAvatarsPerPage);
            };

            ApiKey = new ConfigValue<string>(nameof(ApiKey), "");

            _savedAvatars = new List<ReAvatar>();
            _savedAvatars1 = new List<ReAvatar>();
            _savedAvatars2 = new List<ReAvatar>();
            _savedAvatars3 = new List<ReAvatar>();
            _searchedAvatars = new AvatarList();

            if (File.Exists(PinPath))
            {
                if (!int.TryParse(File.ReadAllText(PinPath), out _pinCode))
                {
                    ReModCE_ARES.LogDebug($"Couldn't read pin file from \"{PinPath}\". File might be corrupted.");
                    ReLogger.Warning($"Couldn't read pin file from \"{PinPath}\". File might be corrupted.");
                }
            }
        }

        private void InitializeNetworkClient()
        {
            if (!EnableApi)
#pragma warning disable CS0162 // Unreachable code detected
                return;
#pragma warning restore CS0162 // Unreachable code detected

            _httpClientHandler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };
            _httpClient = new HttpClient(_httpClientHandler);

            var vrHeadset = XRDevice.isPresent ? XRDevice.model : "Desktop";
            vrHeadset = vrHeadset.Replace(' ', '_');

            _userAgent = $"ReModCE/{vrHeadset}.{BuildInfo.Version} (Windows NT 10.0; Win64; x64)";

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
        }

        public override void OnUiManagerInitEarly()
        {
            InitializeNetworkClient();

            _favoriteAvatarList3 = new ReAvatarList("ARES Favorites 3", this, false);
            _favoriteAvatarList3.AvatarPedestal.field_Internal_Action_4_String_GameObject_AvatarPerformanceStats_ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique_0 = new Action<string, GameObject, AvatarPerformanceStats, ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique>(OnAvatarInstantiated3);
            _favoriteAvatarList3.OnEnable += () =>
            {
                // make sure it stays off if it should be off.
                _favoriteAvatarList3.GameObject.SetActive(AvatarFavoritesEnabled3);
            };

            _favoriteAvatarList2 = new ReAvatarList("ARES Favorites 2", this, false);
            _favoriteAvatarList2.AvatarPedestal.field_Internal_Action_4_String_GameObject_AvatarPerformanceStats_ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique_0 = new Action<string, GameObject, AvatarPerformanceStats, ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique>(OnAvatarInstantiated2);
            _favoriteAvatarList2.OnEnable += () =>
            {
                // make sure it stays off if it should be off.
                _favoriteAvatarList2.GameObject.SetActive(AvatarFavoritesEnabled2);
            };

            _favoriteAvatarList1 = new ReAvatarList("ARES Favorites 1", this, false);
            _favoriteAvatarList1.AvatarPedestal.field_Internal_Action_4_String_GameObject_AvatarPerformanceStats_ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique_0 = new Action<string, GameObject, AvatarPerformanceStats, ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique>(OnAvatarInstantiated1);
            _favoriteAvatarList1.OnEnable += () =>
            {
                // make sure it stays off if it should be off.
                _favoriteAvatarList1.GameObject.SetActive(AvatarFavoritesEnabled1);
            };

            _favoriteAvatarList = new ReAvatarList("ARES Favorites", this, false);
            _favoriteAvatarList.AvatarPedestal.field_Internal_Action_4_String_GameObject_AvatarPerformanceStats_ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique_0 = new Action<string, GameObject, AvatarPerformanceStats, ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique>(OnAvatarInstantiated);
            _favoriteAvatarList.OnEnable += () =>
            {
                // make sure it stays off if it should be off.
                _favoriteAvatarList.GameObject.SetActive(AvatarFavoritesEnabled);
            };

            _searchedAvatarList = new ReAvatarList("ARES Search", this);

            var parent = GameObject.Find("UserInterface/MenuContent/Screens/Avatar/Favorite Button").transform.parent;
            _favoriteButton = new ReUiButton("Favorite", new Vector2(-600f, 375f), new Vector2(0.5f, 1f),
                () => FavoriteAvatar(_favoriteAvatarList.AvatarPedestal.field_Internal_ApiAvatar_0, 0),
                parent);
            _favoriteButton.GameObject.SetActive(AvatarFavoritesEnabled);

            _favoriteButton1 = new ReUiButton("Favorite 1", new Vector2(-450, 375f), new Vector2(0.5f, 1f),
                () => FavoriteAvatar(_favoriteAvatarList1.AvatarPedestal.field_Internal_ApiAvatar_0, 1),
                parent);
            _favoriteButton1.GameObject.SetActive(AvatarFavoritesEnabled1);

            _favoriteButton2 = new ReUiButton("Favorite 2", new Vector2(-300f, 375f), new Vector2(0.5f, 1f),
                () => FavoriteAvatar(_favoriteAvatarList2.AvatarPedestal.field_Internal_ApiAvatar_0, 2),
                parent);
            _favoriteButton2.GameObject.SetActive(AvatarFavoritesEnabled2);

            _favoriteButton3 = new ReUiButton("Favorite 3", new Vector2(-150f, 375f), new Vector2(0.5f, 1f),
                () => FavoriteAvatar(_favoriteAvatarList3.AvatarPedestal.field_Internal_ApiAvatar_0, 3),
                parent);
            _favoriteButton3.GameObject.SetActive(AvatarFavoritesEnabled3);
           

            var changeButton = GameObject.Find("UserInterface/MenuContent/Screens/Avatar/Change Button");
            if (changeButton != null)
            {
                var button = changeButton.GetComponent<Button>();
                _changeButtonEvent = button.onClick;

                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(new Action(ChangeAvatarChecked));
            }


            _searchAvatarsAction = DelegateSupport.ConvertDelegate<UnityAction<string>>(
                (Action<string>)SearchAvatars);
            _overrideSearchAvatarsAction = DelegateSupport.ConvertDelegate<UnityAction<string>>(
                (Action<string>)PromptChooseSearch);

            _avatarScreen = GameObject.Find("UserInterface/MenuContent/Screens/Avatar");
            _searchBox = GameObject.Find("UserInterface/MenuContent/Backdrop/Header/Tabs/ViewPort/Content/Search/InputField").GetComponent<UiInputField>();
            MelonCoroutines.Start(LoginToAPICoroutine());
        }

        private void FavoriteAvatar0()
        {
            IUser user = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0;
            if (user == null)
                return;
            var player = PlayerManager.field_Private_Static_PlayerManager_0.GetPlayer(user.prop_String_0)._vrcplayer;
            FavoriteAvatar(player.field_Private_ApiAvatar_0, 0, true);
        }

        private void FavoriteAvatar1()
        {
            IUser user = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0;
            if (user == null)
                return;
            var player = PlayerManager.field_Private_Static_PlayerManager_0.GetPlayer(user.prop_String_0)._vrcplayer;
            FavoriteAvatar(player.field_Private_ApiAvatar_0, 1, true);
        }

        private void FavoriteAvatar2()
        {
            IUser user = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0;
            if (user == null)
                return;
            var player = PlayerManager.field_Private_Static_PlayerManager_0.GetPlayer(user.prop_String_0)._vrcplayer;
            FavoriteAvatar(player.field_Private_ApiAvatar_0, 2, true);
        }

        private void FavoriteAvatar3()
        {
            IUser user = QuickMenuEx.SelectedUserLocal.field_Private_IUser_0;
            if (user == null)
                return;
            var player = PlayerManager.field_Private_Static_PlayerManager_0.GetPlayer(user.prop_String_0)._vrcplayer;
            FavoriteAvatar(player.field_Private_ApiAvatar_0, 3, true);
        }

        public override void OnUiManagerInit(UiManager uiManager)
        {
            base.OnUiManagerInit(uiManager);

            if (ReModCE_ARES.IsRubyLoaded)
            {
                _favoriteButton.Position += new Vector3(420f, 0f);
            }

            var targetMenu = uiManager.TargetMenu;

            var userInfoTransform = VRCUiManagerEx.Instance.MenuContent().transform.Find("Screens/UserInfo");
            _userInfoPage = userInfoTransform.GetComponent<PageUserInfo>();

            _vrcaTargetButton = targetMenu.AddButton("Favorite 0", "Favorite selected users avatar. (not fully working)", FavoriteAvatar0, ResourceManager.GetSprite("remodce.star"));
            _vrcaTargetButton1 = targetMenu.AddButton("Favorite 1", "Favorite selected users avatar. (not fully working)", FavoriteAvatar1, ResourceManager.GetSprite("remodce.star"));
            _vrcaTargetButton2 = targetMenu.AddButton("Favorite 2", "Favorite selected users avatar. (not fully working)", FavoriteAvatar2, ResourceManager.GetSprite("remodce.star"));
            _vrcaTargetButton3 = targetMenu.AddButton("Favorite 3", "Favorite selected users avatar. (not fully working)", FavoriteAvatar3, ResourceManager.GetSprite("remodce.star"));

            var menu = uiManager.MainMenu.GetMenuPage(Page.PageNames.Avatars);
            _enabledToggle = menu.AddToggle("Avatar Favorites", "Enable/Disable avatar favorites", AvatarFavoritesEnabled);
            _enabledToggle1 = menu.AddToggle("Avatar Favorites 1", "Enable/Disable avatar favorites", AvatarFavoritesEnabled1);
            _enabledToggle2 = menu.AddToggle("Avatar Favorites 2", "Enable/Disable avatar favorites", AvatarFavoritesEnabled2);
            _enabledToggle3 = menu.AddToggle("Avatar Favorites 3", "Enable/Disable avatar favorites", AvatarFavoritesEnabled3);
            _maxAvatarsPerPageButton = menu.AddButton($"Avatars Per Page: {MaxAvatarsPerPage}",
                "Set the maximum amount of avatars shown per page",
                () =>
                {
                    VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopupWithCancel("Max Avatars Per Page",
                        MaxAvatarsPerPage.ToString(), InputField.InputType.Standard, true, "Submit",
                        (s, k, t) =>
                        {
                            if (string.IsNullOrEmpty(s))
                                return;

                            if (!int.TryParse(s, out var maxAvatarsPerPage))
                                return;

                            MaxAvatarsPerPage.SetValue(maxAvatarsPerPage);
                            _maxAvatarsPerPageButton.Text = $"Max Avatars Per Page: {MaxAvatarsPerPage}";
                        }, null);
                }, ResourceManager.GetSprite("remodce.max"));

            _enterPinButton = menu.AddButton("Set/Enter Pin", "Set or enter your pin for the ARES API", () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopupWithCancel("Enter pin",
                    "", InputField.InputType.Standard, true, "Submit",
                    (s, k, t) =>
                    {
                        if (string.IsNullOrEmpty(s))
                            return;

                        if (!int.TryParse(s, out var pinCode))
                            return;

                        _pinCode = pinCode;
                        File.WriteAllText(PinPath, _pinCode.ToString());

                        InitializeNetworkClient();

                        LoginToAPI(APIUser.CurrentUser, FetchAvatars);
                        LoginToAPI(APIUser.CurrentUser, FetchAvatars1);
                        LoginToAPI(APIUser.CurrentUser, FetchAvatars2);
                        LoginToAPI(APIUser.CurrentUser, FetchAvatars3);
                    }, null);
            }, ResourceManager.GetSprite("remodce.padlock"));

            _apiKeyButton = menu.AddButton("Enter Api Key", "Set or enter your key for the ARES API", () =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowInputPopupWithCancel("Enter key",
                    "", InputField.InputType.Standard, false, "Submit",
                    (s, k, t) =>
                    {
                        if (string.IsNullOrEmpty(s))
                            return;

                        ApiKey.SetValue(s);
                    }, null);
            }, ResourceManager.GetSprite("remodce.padlock"));

        }

        private void LoginToAPI(APIUser user, Action onLogin)
        {
            if (!EnableApi)
            {
#pragma warning disable CS0162 // Unreachable code detected
                return;
#pragma warning restore CS0162 // Unreachable code detected
            }
            this.user = user;
            onLogin();
        }

        public override void OnUpdate()
        {
            if (_searchBox == null)
                return;

            if (!_avatarScreen.active)
            {
                return;
            }

            if (!_searchBox.field_Public_Button_0.interactable)
            {
                if (!ReModCE_ARES.IsEmmVrcLoaded || _updatesWithoutSearch >= 10)
                {
                    _searchBox.field_Public_Button_0.interactable = true;
                    _searchBox.field_Public_UnityAction_1_String_0 = _searchAvatarsAction;
                }
                else if (ReModCE_ARES.IsEmmVrcLoaded)
                {
                    ++_updatesWithoutSearch;
                }
                // emmVRC will set it to be interactable. We want to grab their search function
            }
            else
            {
                if (ReModCE_ARES.IsEmmVrcLoaded && _updatesWithoutSearch < 10)
                {
                    if (_searchBox.field_Public_UnityAction_1_String_0 == null)
                        return;

                    if (_searchBox.field_Public_UnityAction_1_String_0.method != _overrideSearchAvatarsAction.method)
                    {
                        if (_emmVRCsearchAvatarsAction == null)
                        {
                            _emmVRCsearchAvatarsAction = _searchBox.field_Public_UnityAction_1_String_0;
                        }
                        _searchBox.field_Public_UnityAction_1_String_0 = _overrideSearchAvatarsAction;
                    }
                }
            }
        }

        private void PromptChooseSearch(string searchTerm)
        {
            MelonCoroutines.Start(PrompSearchDelayed(searchTerm));
        }

        private IEnumerator PrompSearchDelayed(string searchTerm)
        {
            yield return new WaitForSeconds(1f);
            VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowStandardPopupV2("Choose Search",
                "Choose whether you want to search with ARES or emmVRC", "ARES",
                () =>
                {
                    SearchAvatars(searchTerm);
                    VRCUiManager.prop_VRCUiManager_0.HideScreen("POPUP");
                }, "emmVRC", () =>
                {
                    _emmVRCsearchAvatarsAction?.Invoke(searchTerm);
                    VRCUiManager.prop_VRCUiManager_0.HideScreen("POPUP");
                }, null);
        }

        private void SearchAvatars(string searchTerm)
        {
            var popupManager = VRCUiPopupManager.field_Private_Static_VRCUiPopupManager_0;
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 3)
            {
                popupManager.ShowStandardPopupV2("ARES Search", "That search term is too short. The search term has to be at least 3 characters.", "I'm sorry!",
                    () =>
                    {
                        popupManager.HideCurrentPopup();
                    });
                return;
            }

            string apiKeyCode = ApiUrl;

            if (ApiKey != "")
            {
                apiKeyCode = ApiUnlockedUrl;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"{apiKeyCode}/records/Avatars?include=AvatarID,AvatarName,AvatarDescription,AuthorID,AuthorName,PCAssetURL,ImageURL,ThumbnailURL,Quest&size=10000&filter=AvatarName,cs,{searchTerm}&filter=Releasestatus,cs,Public");

            if (ApiKey != "")
            {
                request.Headers.Add("X-API-Key", ApiKey);
            }

            _httpClient.SendAsync(request).ContinueWith(rsp =>
            {
                var searchResponse = rsp.Result;
                if (!searchResponse.IsSuccessStatusCode)
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(errorData =>
                    {
                        var errorMessage = JsonConvert.DeserializeObject<ApiError>(errorData.Result).Error;

                        ReLogger.Error($"Could not search for avatars: \"{errorMessage}\"");
                        ReModCE_ARES.LogDebug($"Could not search for avatars: \"{errorMessage}\"");
                        if (searchResponse.StatusCode == HttpStatusCode.Forbidden)
                        {
                            MelonCoroutines.Start(ShowAlertDelayed($"Could not search for avatars\nReason: \"{errorMessage}\""));
                        }
                    });
                }
                else
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(t =>
                    {
                        var avatars = JsonConvert.DeserializeObject<AvatarGet>(t.Result) ?? new AvatarGet { records = new List<ReAvatar>() };
                        if (avatars.records.Count > 0)
                        {
                            MelonCoroutines.Start(RefreshSearchedAvatars(avatars.records));
                        }
                        else
                        {
                            MelonCoroutines.Start(ShowAlertDelayed($"No avatars found with the name: \"{searchTerm}\""));
                        }
                    });
                }
            });
        }



        private IEnumerator RefreshSearchedAvatars(List<ReAvatar> results)
        {
            yield return new WaitForEndOfFrame();

            _searchedAvatars.Clear();
            foreach (var avi in results.Select(x => x.AsApiAvatar()))
            {
                _searchedAvatars.Add(avi);
            }

            ReModCE_ARES.LogDebug($"Found {_searchedAvatars.Count} avatars");
            ReLogger.Msg($"Found {_searchedAvatars.Count} avatars");
            _searchedAvatarList.RefreshAvatars();
        }

        private void ChangeAvatarChecked()
        {
            if (!AvatarFavoritesEnabled)
            {
                _changeButtonEvent.Invoke();
                return;
            }

            var currentAvatar = _favoriteAvatarList.AvatarPedestal.field_Internal_ApiAvatar_0;
            if (!HasAvatarFavorited(currentAvatar.id)) // this isn't in our list. we don't care about it
            {
                _changeButtonEvent.Invoke();
                return;
            }

            new ApiAvatar { id = currentAvatar.id }.Fetch(new Action<ApiContainer>(ac =>
            {
                var updatedAvatar = ac.Model.Cast<ApiAvatar>();
                switch (updatedAvatar.releaseStatus)
                {
                    case "private" when updatedAvatar.authorId != APIUser.CurrentUser.id:
                        VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("ARES", "This avatar is private and you don't own it. You can't switch into it.");
                        break;
                    case "unavailable":
                        VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("ARES", "This avatar has been deleted. You can't switch into it.");
                        break;
                    default:
                        _changeButtonEvent.Invoke();
                        break;
                }
            }), new Action<ApiContainer>(ac =>
            {
                VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("ARES", "This avatar has been deleted. You can't switch into it.");
            }));
        }

        private void FetchAvatars()
        {

            while (APIUser.CurrentUser == null) { System.Threading.Thread.Sleep(10); };
            user = APIUser.CurrentUser;

            string userId = user.id;
            string pin = _pinCode.ToString();
            string url = $"{ApiUrl}/AvatarFavNew.php?UserId=" + HttpUtility.UrlEncode(userId) + "&Pin=" + HttpUtility.UrlEncode(pin) + "&Category=0";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            _httpClient.SendAsync(request).ContinueWith(rsp =>
            {
                var searchResponse = rsp.Result;
                if (!searchResponse.IsSuccessStatusCode)
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(errorData =>
                    {
                        var errorMessage = JsonConvert.DeserializeObject<ApiError>(errorData.Result).Error;
                        ReModCE_ARES.LogDebug($"Could not search for avatars: \"{errorMessage}\"");
                        ReLogger.Error($"Could not search for avatars: \"{errorMessage}\"");
                        if (searchResponse.StatusCode == HttpStatusCode.Forbidden)
                        {
                            MelonCoroutines.Start(ShowAlertDelayed($"Could not search for avatars\nReason: \"{errorMessage}\""));
                        }
                    });
                }
                else
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(t =>
                    {
                        var avatars = JsonConvert.DeserializeObject<List<ReAvatar>>(t.Result) ?? new List<ReAvatar>();
                        _savedAvatars = avatars;
                    });
                }
            });
        }

        private void FetchAvatars1()
        {

            while (APIUser.CurrentUser == null) { System.Threading.Thread.Sleep(10); };
            user = APIUser.CurrentUser;

            string userId = user.id;
            string pin = _pinCode.ToString();
            string url = $"{ApiUrl}/AvatarFavNew.php?UserId=" + HttpUtility.UrlEncode(userId) + "&Pin=" + HttpUtility.UrlEncode(pin) + "&Category=1";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            _httpClient.SendAsync(request).ContinueWith(rsp =>
            {
                var searchResponse = rsp.Result;
                if (!searchResponse.IsSuccessStatusCode)
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(errorData =>
                    {
                        var errorMessage = JsonConvert.DeserializeObject<ApiError>(errorData.Result).Error;
                        ReModCE_ARES.LogDebug($"Could not search for avatars: \"{errorMessage}\"");
                        ReLogger.Error($"Could not search for avatars: \"{errorMessage}\"");
                        if (searchResponse.StatusCode == HttpStatusCode.Forbidden)
                        {
                            MelonCoroutines.Start(ShowAlertDelayed($"Could not search for avatars\nReason: \"{errorMessage}\""));
                        }
                    });
                }
                else
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(t =>
                    {
                        var avatars = JsonConvert.DeserializeObject<List<ReAvatar>>(t.Result) ?? new List<ReAvatar>();
                        _savedAvatars1 = avatars;
                    });
                }
            });
        }

        private void FetchAvatars2()
        {

            while (APIUser.CurrentUser == null) { System.Threading.Thread.Sleep(10); };
            user = APIUser.CurrentUser;

            string userId = user.id;
            string pin = _pinCode.ToString();
            string url = $"{ApiUrl}/AvatarFavNew.php?UserId=" + HttpUtility.UrlEncode(userId) + "&Pin=" + HttpUtility.UrlEncode(pin) + "&Category=2";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            _httpClient.SendAsync(request).ContinueWith(rsp =>
            {
                var searchResponse = rsp.Result;
                if (!searchResponse.IsSuccessStatusCode)
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(errorData =>
                    {
                        var errorMessage = JsonConvert.DeserializeObject<ApiError>(errorData.Result).Error;
                        ReModCE_ARES.LogDebug($"Could not search for avatars: \"{errorMessage}\"");
                        ReLogger.Error($"Could not search for avatars: \"{errorMessage}\"");
                        if (searchResponse.StatusCode == HttpStatusCode.Forbidden)
                        {
                            MelonCoroutines.Start(ShowAlertDelayed($"Could not search for avatars\nReason: \"{errorMessage}\""));
                        }
                    });
                }
                else
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(t =>
                    {
                        var avatars = JsonConvert.DeserializeObject<List<ReAvatar>>(t.Result) ?? new List<ReAvatar>();
                        _savedAvatars2 = avatars;
                    });
                }
            });
        }

        private void FetchAvatars3()
        {

            while (APIUser.CurrentUser == null) { System.Threading.Thread.Sleep(10); };
            user = APIUser.CurrentUser;

            string userId = user.id;
            string pin = _pinCode.ToString();
            string url = $"{ApiUrl}/AvatarFavNew.php?UserId=" + HttpUtility.UrlEncode(userId) + "&Pin=" + HttpUtility.UrlEncode(pin) + "&Category=3";
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            _httpClient.SendAsync(request).ContinueWith(rsp =>
            {
                var searchResponse = rsp.Result;
                if (!searchResponse.IsSuccessStatusCode)
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(errorData =>
                    {
                        var errorMessage = JsonConvert.DeserializeObject<ApiError>(errorData.Result).Error;
                        ReModCE_ARES.LogDebug($"Could not search for avatars: \"{errorMessage}\"");
                        ReLogger.Error($"Could not search for avatars: \"{errorMessage}\"");
                        if (searchResponse.StatusCode == HttpStatusCode.Forbidden)
                        {
                            MelonCoroutines.Start(ShowAlertDelayed($"Could not search for avatars\nReason: \"{errorMessage}\""));
                        }
                    });
                }
                else
                {
                    searchResponse.Content.ReadAsStringAsync().ContinueWith(t =>
                    {
                        var avatars = JsonConvert.DeserializeObject<List<ReAvatar>>(t.Result) ?? new List<ReAvatar>();
                        _savedAvatars3 = avatars;
                    });
                }
            });
        }

        private static IEnumerator ShowAlertDelayed(string message, float seconds = 0.5f)
        {
            if (VRCUiPopupManager.prop_VRCUiPopupManager_0 == null) yield break;

            yield return new WaitForSeconds(seconds);

            VRCUiPopupManager.prop_VRCUiPopupManager_0.ShowAlert("ARES ARES", message);
        }

        private void OnAvatarInstantiated(string url, GameObject avatar, AvatarPerformanceStats avatarPerformanceStats, ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique unknown)
        {
            _favoriteButton.Text = HasAvatarFavorited(_favoriteAvatarList.AvatarPedestal.field_Internal_ApiAvatar_0.id) ? "Unfavorite" : "Favorite";
        }

        private void OnAvatarInstantiated1(string url, GameObject avatar, AvatarPerformanceStats avatarPerformanceStats, ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique unknown)
        {
            _favoriteButton1.Text = HasAvatarFavorited1(_favoriteAvatarList1.AvatarPedestal.field_Internal_ApiAvatar_0.id) ? "Unfavorite 1" : "Favorite 1";
        }

        private void OnAvatarInstantiated2(string url, GameObject avatar, AvatarPerformanceStats avatarPerformanceStats, ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique unknown)
        {
            _favoriteButton2.Text = HasAvatarFavorited2(_favoriteAvatarList2.AvatarPedestal.field_Internal_ApiAvatar_0.id) ? "Unfavorite 2" : "Favorite 2";
        }

        private void OnAvatarInstantiated3(string url, GameObject avatar, AvatarPerformanceStats avatarPerformanceStats, ObjectPublicBoBoBoBoBoBoBoBoBoBoUnique unknown)
        {
            _favoriteButton3.Text = HasAvatarFavorited3(_favoriteAvatarList3.AvatarPedestal.field_Internal_ApiAvatar_0.id) ? "Unfavorite 3" : "Favorite 3";
        }

        private void FavoriteAvatar(ApiAvatar apiAvatar, int category, bool quickFav = false)
        {
            bool hasFavorited = false;
            if (category == 0)
            {
                hasFavorited = HasAvatarFavorited(apiAvatar.id);
            }
            if (category == 1)
            {
                hasFavorited = HasAvatarFavorited1(apiAvatar.id);
            }
            if (category == 2)
            {
                hasFavorited = HasAvatarFavorited2(apiAvatar.id);
            }
            if (category == 3)
            {
                hasFavorited = HasAvatarFavorited3(apiAvatar.id);
            }
            if (hasFavorited)
            {
                string userId = user.id;
                string pin = _pinCode.ToString();
                string url = $"{ApiUrl}/AvatarFavRemoveNew.php?UserId=" + HttpUtility.UrlEncode(userId) + "&Pin=" + HttpUtility.UrlEncode(pin) + "&AvatarID=" + HttpUtility.UrlEncode(apiAvatar.id) + "&Category=" + category;
                _httpClient.GetStringAsync(url);
            }
            if (!hasFavorited)
            {
                ReAvatar reAvatar = new ReAvatar(apiAvatar);
                reAvatar.AuthorName.replaceThis("Æ");
                reAvatar.AvatarDescription.replaceThis("Æ");
                reAvatar.AvatarName.replaceThis("Æ");
                reAvatar.Pin = _pinCode.ToString();
                reAvatar.UserId = APIUser.CurrentUser.id;
                reAvatar.Category = category.ToString();

                var httpWebRequest = (HttpWebRequest)WebRequest.Create($"{ApiUrl}/records/AvatarsFav");
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                string jsonPost = JsonConvert.SerializeObject(reAvatar);

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonPost);
                }
                try
                {
                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                    }
                }
                catch (Exception ex)
                {
                    ReModCE_ARES.LogDebug(ex.Message);
                    ReLogger.Error(ex.Message);
                }
            }

            if (category == 0)
            {
                if (_favoriteAvatarList.AvatarPedestal.field_Internal_ApiAvatar_0.id == apiAvatar.id)
                {
                    if (!HasAvatarFavorited(apiAvatar.id))
                    {
                        _savedAvatars.Insert(0, new ReAvatar(apiAvatar));
                        _favoriteButton.Text = "Unfavorite";
                    }
                    else
                    {
                        _savedAvatars.RemoveAll(a => a.AvatarID == apiAvatar.id);
                        _favoriteButton.Text = "Favorite";
                    }
                }
            }

            if (category == 1)
            {
                if (quickFav)
                {
                    _savedAvatars1.Insert(0, new ReAvatar(apiAvatar));
                }
                else
                {
                    if (_favoriteAvatarList1.AvatarPedestal.field_Internal_ApiAvatar_0.id == apiAvatar.id)
                    {
                        if (!HasAvatarFavorited1(apiAvatar.id))
                        {
                            _savedAvatars1.Insert(0, new ReAvatar(apiAvatar));
                            _favoriteButton1.Text = "Unfavorite 1";
                        }
                        else
                        {
                            _savedAvatars1.RemoveAll(a => a.AvatarID == apiAvatar.id);
                            _favoriteButton1.Text = "Favorite 1";
                        }
                    }
                }
            }

            if (category == 2)
            {
                if (quickFav)
                {
                    _savedAvatars1.Insert(0, new ReAvatar(apiAvatar));
                }
                else
                {
                    if (_favoriteAvatarList2.AvatarPedestal.field_Internal_ApiAvatar_0.id == apiAvatar.id)
                    {
                        if (!HasAvatarFavorited2(apiAvatar.id))
                        {
                            _savedAvatars2.Insert(0, new ReAvatar(apiAvatar));
                            _favoriteButton2.Text = "Unfavorite 2";
                        }
                        else
                        {
                            _savedAvatars2.RemoveAll(a => a.AvatarID == apiAvatar.id);
                            _favoriteButton2.Text = "Favorite 2";
                        }
                    }
                }
            }

            if (category == 3)
            {
                if (quickFav)
                {
                    _savedAvatars1.Insert(0, new ReAvatar(apiAvatar));
                }
                else
                {
                    if (_favoriteAvatarList3.AvatarPedestal.field_Internal_ApiAvatar_0.id == apiAvatar.id)
                    {
                        if (!HasAvatarFavorited3(apiAvatar.id))
                        {
                            _savedAvatars3.Insert(0, new ReAvatar(apiAvatar));
                            _favoriteButton3.Text = "Unfavorite 3";
                        }
                        else
                        {
                            _savedAvatars3.RemoveAll(a => a.AvatarID == apiAvatar.id);
                            _favoriteButton3.Text = "Favorite 3";
                        }
                    }
                }
            }

            _favoriteAvatarList.RefreshAvatars();
            _favoriteAvatarList1.RefreshAvatars();
            _favoriteAvatarList2.RefreshAvatars();
            _favoriteAvatarList3.RefreshAvatars();
        }

        private IEnumerator LoginToAPICoroutine()
        {
            while (APIUser.CurrentUser == null) yield return new WaitForEndOfFrame();

            var user = APIUser.CurrentUser;
            LoginToAPI(user, FetchAvatars);
            LoginToAPI(user, FetchAvatars1);
            LoginToAPI(user, FetchAvatars2);
            LoginToAPI(user, FetchAvatars3);
        }

        private bool HasAvatarFavorited(string id)
        {
            return _savedAvatars.FirstOrDefault(a => a.AvatarID == id) != null;
        }

        private bool HasAvatarFavorited1(string id)
        {
            return _savedAvatars1.FirstOrDefault(a => a.AvatarID == id) != null;
        }

        private bool HasAvatarFavorited2(string id)
        {
            return _savedAvatars2.FirstOrDefault(a => a.AvatarID == id) != null;
        }

        private bool HasAvatarFavorited3(string id)
        {
            return _savedAvatars3.FirstOrDefault(a => a.AvatarID == id) != null;
        }

        public AvatarList GetAvatars(ReAvatarList avatarList)
        {
            if (avatarList == _favoriteAvatarList)
            {
                var list = new AvatarList();
                foreach (var avi in _savedAvatars.Select(x => x.AsApiAvatar()).ToList())
                {
                    list.Add(avi);
                }

                return list;
            }
            if (avatarList == _favoriteAvatarList1)
            {
                var list = new AvatarList();
                foreach (var avi in _savedAvatars1.Select(x => x.AsApiAvatar()).ToList())
                {
                    list.Add(avi);
                }

                return list;
            }
            if (avatarList == _favoriteAvatarList2)
            {
                var list = new AvatarList();
                foreach (var avi in _savedAvatars2.Select(x => x.AsApiAvatar()).ToList())
                {
                    list.Add(avi);
                }

                return list;
            }
            if (avatarList == _favoriteAvatarList3)
            {
                var list = new AvatarList();
                foreach (var avi in _savedAvatars3.Select(x => x.AsApiAvatar()).ToList())
                {
                    list.Add(avi);
                }

                return list;
            }
            else if (avatarList == _searchedAvatarList)
            {
                return _searchedAvatars;
            }

            return null;
        }


        public void Clear(ReAvatarList avatarList)
        {
            if (avatarList == _searchedAvatarList)
            {
                _searchedAvatars.Clear();
                avatarList.RefreshAvatars();
            }
        }
    }
}
