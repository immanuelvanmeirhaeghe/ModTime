using Enums;
using ModTime.Data.Enums;
using ModTime.Data.Interfaces;
using ModTime.Data.Modding;
using ModTime.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEditor;

namespace ModTime
{
    /// <summary>
    /// ModTime is a mod for Green Hell that allows a player to set in-game player condition multipliers, date and day and night time scales in real time minutes.
    /// Ingame time can be fast forwarded to the next morning 5AM or night 10PM.
    /// It also allows to manipulate weather to make it rain or stop raining. 
    /// Press Keypad2 (default) or the key configurable in ModAPI to open the mod screen.
    /// </summary>
    public class ModTime : MonoBehaviour
    {
        private static ModTime Instance;
        private static readonly string RuntimeConfiguration = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), $"{nameof(RuntimeConfiguration)}.xml");

        private static readonly string ModName = nameof(ModTime);
        public string ModTimeScreenTitle = $"{ModName} created by [Dragon Legion] Immaanuel#4300";
        private static float ModTimeScreenTotalWidth { get; set; } = 700f;
        private static float ModTimeScreenTotalHeight { get; set; } = 500f;      
        private static float ModTimeScreenMinWidth { get; set; } = 700f;
        private static float ModTimeScreenMaxWidth { get; set; } = Screen.width;
        private static float ModTimeScreenMinHeight { get; set; } = 50f;
        private static  float ModTimeScreenMaxHeight { get; set; } = Screen.height;
        private static float ModTimeScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModTimeScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsModTimeMinimized { get; set; } = false;
        private static int ModTimeScreenId { get; set; } = 0;
        private static bool IsModTimeResizing { get; set; } = false;
        private static Vector2 MouseStartPos;
        private static Vector2 ModTimeScreenStartSize;

        private static float HUDTimeScreenTotalWidth { get; set; } = 100f;
        private static float HUDTimeScreenTotalHeight { get; set; } = 75f;
        private static float HUDTimeScreenMinWidth { get; set; } = 100f;
        private static float HUDTimeScreenMinHeight { get; set; } = 75f;
        private static float HUDTimeScreenMaxWidth { get; set; } = 100f;
        private static float HUDTimeScreenMaxHeight { get; set; } = 75f;
        private static float HUDTimeScreenStartPositionX { get; set; } = Screen.width - HUDTimeScreenTotalWidth;
        private static float HUDTimeScreenStartPositionY { get; set; } = Screen.height - HUDTimeScreenTotalHeight;
        private static bool IsHUDTimeMinimized { get; set; } = false;
        private static int HUDTimeScreenId { get; set; } = 0;

        private bool ShowModTime { get; set; } = false;
        private bool ShowDefaultMuls { get; set; } = false;
        private bool ShowCustomMuls { get; set; } = false;
        private bool ShowModTimeInfo { get; set; } = false;
        private bool ShowHUDTime { get; set; } = false;        

        public static Rect ModTimeScreen = new Rect(ModTimeScreenStartPositionX, ModTimeScreenStartPositionY, ModTimeScreenTotalWidth, ModTimeScreenTotalHeight);
        public static Rect HUDTimeScreen = new Rect(HUDTimeScreenStartPositionX, HUDTimeScreenStartPositionY, HUDTimeScreenTotalWidth, HUDTimeScreenTotalHeight);

        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static WeatherManager LocalWeatherManager;
        private static HealthManager LocalHealthManager;
        private static TimeManager LocalTimeManager;
        private static StylingManager LocalStylingManager;

        public KeyCode ShortcutKey { get; set; } = KeyCode.Keypad2;

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();
      
        public Vector2 DefaultMulsScrollViewPosition { get; set; } = Vector2.zero;
        public Vector2 CustomMulsScrollViewPosition { get; set; } = Vector2.zero;
        public Vector2 ModInfoScrollViewPosition { get; set; } = Vector2.zero;
        public IConfigurableMod SelectedMod { get; set; } = default;
        public Vector2 ConditionMulsScrollViewPosition { get; set; } = Vector2.zero;

        public ModTime()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static ModTime Get()
        {
            return Instance;
        }

        private string OnlyForSinglePlayerOrHostMessage()
     => "Only available for single player or when host. Host can activate using ModManager.";
        private string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        private string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{(headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>{messageType}</color>\n{message}";
        protected virtual void OnlyForSingleplayerOrWhenHostBox()
        {
            using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));
            }
        }

        public virtual KeyCode GetShortcutKey(string buttonID)
        {
            var ConfigurableModList = GetModList();
            if (ConfigurableModList != null && ConfigurableModList.Count > 0)
            {
                SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == ModName);
                return SelectedMod.ConfigurableModButtons.Find(cfgButton => cfgButton.ID == buttonID).ShortcutKey;
            }
            else
            {
                switch (buttonID)
                {
                    case nameof(ShortcutKey):
                        return KeyCode.Keypad2;                  
                    default:
                        return KeyCode.None;
                }
            }
        }

        private List<IConfigurableMod> GetModList()
        {
            List<IConfigurableMod> modList = new List<IConfigurableMod>();
            try
            {
                if (File.Exists(RuntimeConfiguration))
                {
                    using (XmlReader configFileReader = XmlReader.Create(new StreamReader(RuntimeConfiguration)))
                    {
                        while (configFileReader.Read())
                        {
                            configFileReader.ReadToFollowing("Mod");
                            do
                            {
                                string gameID = GameID.GreenHell.ToString();
                                string modID = configFileReader.GetAttribute(nameof(IConfigurableMod.ID));
                                string uniqueID = configFileReader.GetAttribute(nameof(IConfigurableMod.UniqueID));
                                string version = configFileReader.GetAttribute(nameof(IConfigurableMod.Version));

                                var configurableMod = new ConfigurableMod(gameID, modID, uniqueID, version);

                                configFileReader.ReadToDescendant("Button");
                                do
                                {
                                    string buttonID = configFileReader.GetAttribute(nameof(IConfigurableModButton.ID));
                                    string buttonKeyBinding = configFileReader.ReadElementContentAsString();

                                    configurableMod.AddConfigurableModButton(buttonID, buttonKeyBinding);

                                } while (configFileReader.ReadToNextSibling("Button"));

                                if (!modList.Contains(configurableMod))
                                {
                                    modList.Add(configurableMod);
                                }

                            } while (configFileReader.ReadToNextSibling("Mod"));
                        }
                    }
                }
                return modList;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetModList));
                modList = new List<IConfigurableMod>();
                return modList;
            }
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc}";
            ModAPI.Log.Write(info);
            ShowHUDBigInfo(HUDBigInfoMessage(exc.Message, MessageType.Error, Color.red));
        }

        protected virtual void ModManager_onPermissionValueChanged(bool optionValue)
        {
            string reason = optionValue ? "the game host allowed usage" : "the game host did not allow usage";
            IsModActiveForMultiplayer = optionValue;

            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted", $"{reason}"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked", $"{reason}"), MessageType.Info, LocalStylingManager.DefaultAttentionColor))
                            );
        }

        protected virtual void Awake()
        {
            Instance = this;
        }

        protected virtual void OnDestroy()
        {
            Instance = null;
        }

        protected virtual void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;           
            InitData();
            ShortcutKey = GetShortcutKey(nameof(ShortcutKey));
        }

        protected virtual void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalHealthManager = HealthManager.Get();
            LocalTimeManager = TimeManager.Get();
            LocalWeatherManager = WeatherManager.Get();
            LocalStylingManager = StylingManager.Get();
        }

        protected virtual void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer);
            if (blockPlayer)
            {
                LocalPlayer.BlockMoves();
                LocalPlayer.BlockRotation();
                LocalPlayer.BlockInspection();
            }
            else
            {
                LocalPlayer.UnblockMoves();
                LocalPlayer.UnblockRotation();
                LocalPlayer.UnblockInspection();
            }
        }

        public void ShowHUDBigInfo(string text, float duration = 3f)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();

            HUDBigInfo bigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = duration;
            HUDBigInfoData bigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            bigInfo.AddInfo(bigInfoData);
            bigInfo.Show(true);
        }

        public void ShowHUDInfoLog(string ItemInfo, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            var messages = (HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages));
            messages.AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(ItemInfo));
        }

        protected virtual void Update()
        {
            if (Input.GetKeyDown(ShortcutKey))
            {
                if (!ShowModTime)
                {
                    InitData();
                    EnableCursor(blockPlayer: true);
                }
                ToggleShowUI(0);
                if (!ShowModTime)
                {
                    EnableCursor(blockPlayer: false);
                }
            }           
        }

        protected virtual void ToggleShowUI(int controlId)
        {
            switch (controlId)
            {
                case 0:
                    ShowModTime = !ShowModTime;
                  return;
                case 1:
                    ShowDefaultMuls = !ShowDefaultMuls;
                  return;
                case 2:
                    ShowCustomMuls = !ShowCustomMuls;
                  return;
                case 3:
                    ShowModTimeInfo = !ShowModTimeInfo;
                  return;
                case 6:
                    ShowHUDTime = !ShowHUDTime;
                  return;
                default:
                    ShowModTime = !ShowModTime;
                    ShowDefaultMuls = !ShowDefaultMuls;
                    ShowCustomMuls = !ShowCustomMuls;
                    ShowModTimeInfo = !ShowModTimeInfo;
                    ShowHUDTime = !ShowHUDTime;
                  return;
            }          
        }

        protected virtual void OnGUI()
        {
            if (ShowModTime)
            {
                InitData();
                InitSkinUI();
                ShowModTimeWindow();
            }
            if (ShowHUDTime)
            {
                InitData();
                InitSkinUI();
                ShowHUDTimeWindow();
            }
        }

        protected virtual void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        protected virtual void ShowModTimeWindow()
        {
            if (ModTimeScreenId <= 0 || ModTimeScreenId == HUDTimeScreenId)
            {
                ModTimeScreenId = ModTimeScreen.GetHashCode();
            }            
            ModTimeScreen = GUILayout.Window(ModTimeScreenId, ModTimeScreen, InitModTimeScreen, ModTimeScreenTitle, GUI.skin.window, GUILayout.ExpandWidth(true), GUILayout.MinWidth(ModTimeScreenMinWidth), GUILayout.MaxWidth(ModTimeScreenMaxWidth), GUILayout.ExpandHeight(true), GUILayout.MinHeight(ModTimeScreenMinHeight), GUILayout.MaxHeight(ModTimeScreenMaxHeight));
            OnResizingModTimeScreen();
        }

        protected virtual void OnResizingModTimeScreen()
        {
            if (IsModTimeResizing)
            {
                ModTimeScreen.width = ModTimeScreenStartSize.x + (UnityEngine.Event.current.mousePosition.x - MouseStartPos.x);
                ModTimeScreen.height = ModTimeScreenStartSize.y + (UnityEngine.Event.current.mousePosition.y - MouseStartPos.y);
            }
        }

        protected virtual void ModTimeScreenMenuBox()
        {
            string CollapseButtonText = IsModTimeMinimized ?  "O" :  "-";

            if (GUI.Button(new Rect(ModTimeScreen.width - 60f, 0f, 20f, 20f), "=", GUI.skin.button))
            {
                ResizeModTimeWindow();
            }
            if (GUI.Button(new Rect(ModTimeScreen.width - 40f, 0f, 20f, 20f), CollapseButtonText, GUI.skin.button))
            {
                CollapseModTimeWindow();
            }
            if (GUI.Button(new Rect(ModTimeScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow(0);
            }
        }

        protected virtual void ResizeModTimeWindow()
        {
            Rect resizeHandle = new Rect(ModTimeScreen.width - 60f, 0f, ModTimeScreenMinWidth, ModTimeScreenMinHeight);
            GUI.DrawTexture(resizeHandle, Texture2D.whiteTexture);
            if (UnityEngine.Event.current.type == EventType.MouseDown && resizeHandle.Contains(UnityEngine.Event.current.mousePosition))
            {
                IsModTimeResizing = true;
                MouseStartPos = UnityEngine.Event.current.mousePosition;
                ModTimeScreenStartSize = new Vector2(ModTimeScreen.width, ModTimeScreen.height);
            }
            if (UnityEngine.Event.current.type == EventType.MouseUp)
            {
                IsModTimeResizing = false;
            }
        }

        protected virtual void CollapseModTimeWindow()
        {
            if (IsModTimeResizing)
            {
                return;
            }
            if (!IsModTimeMinimized)
            {
                ModTimeScreen = new Rect(ModTimeScreen.x, ModTimeScreen.y, ModTimeScreenTotalWidth, ModTimeScreenMinHeight);
                IsModTimeMinimized = true;
            }
            else
            {
                ModTimeScreen = new Rect(ModTimeScreen.x, ModTimeScreen.y, ModTimeScreenTotalWidth, ModTimeScreenTotalHeight);
                IsModTimeMinimized = false;
            }
            ShowModTimeWindow();
        }

        protected virtual void ShowHUDTimeWindow()
        {
            if (HUDTimeScreenId <= 0 || HUDTimeScreenId == ModTimeScreenId)
            {
                HUDTimeScreenId = GetHashCode() + 1;
                //ModAPI.Log.Write($"{nameof(HUDTimeScreen)} window id set to {HUDTimeScreenId}");
            }
            string hudTimeScreenTitle = $"";
            HUDTimeScreen = GUILayout.Window(HUDTimeScreenId, HUDTimeScreen, InitHUDTimeScreen, hudTimeScreenTitle, GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.MinWidth(HUDTimeScreenMinWidth), GUILayout.MaxWidth(HUDTimeScreenMaxWidth), GUILayout.ExpandHeight(true), GUILayout.MinHeight(HUDTimeScreenMinHeight), GUILayout.MaxHeight(HUDTimeScreenMaxHeight));
        }

        protected virtual void CollapseHUDTimeWindow()
        {
            if (!IsHUDTimeMinimized)
            {
                HUDTimeScreen = new Rect(HUDTimeScreen.x, HUDTimeScreen.y, HUDTimeScreenTotalWidth, HUDTimeScreenMinHeight);
                IsHUDTimeMinimized = true;
            }
            else
            {
                HUDTimeScreen = new Rect(HUDTimeScreen.x, HUDTimeScreen.y, HUDTimeScreenTotalWidth, HUDTimeScreenTotalHeight);
                IsHUDTimeMinimized = false;
            }
            ShowHUDTimeWindow();
        }

        protected virtual void CloseWindow(int controlId)
        {
            switch (controlId)
            {
                case 0:
                    ShowModTime = false;                    
                    EnableCursor(false);
                    return;
                case 1:
                    ShowHUDTime = false;
                    EnableCursor(false);
                    return;
                default:
                    ShowModTime = false;
                    ShowHUDTime = false;
                    EnableCursor(false);
                    return;
            }
        }

        protected virtual void InitHUDTimeScreen(int windowID)
        {
            HUDTimeScreenStartPositionX = HUDTimeScreen.x;
            HUDTimeScreenStartPositionY = HUDTimeScreen.y;
            HUDTimeScreenTotalWidth = HUDTimeScreen.width;

            GUI.backgroundColor = LocalStylingManager.ClearBackgroundColor;
            
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {              
                HUDTimeMenuBox();

                if (!IsHUDTimeMinimized)
                {
                    HUDTimeViewBox();
                }
            }

            GUI.backgroundColor = LocalStylingManager.DefaultBackGroundColor;

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        protected virtual void HUDTimeViewBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.label))
            {
                GUIContent timeContent = new GUIContent($"{LocalTimeManager.HUDTimeString()}");
                GUIContent dateContent = new GUIContent($"{LocalTimeManager.HUDDateString()}");
                GUILayout.Label(timeContent, LocalStylingManager.ColoredTimeLabel(LocalStylingManager.DefaultAttentionColor));
                GUILayout.Label(dateContent, LocalStylingManager.ColoredTimeLabel(LocalStylingManager.DefaultAttentionColor));
            }                
        }

        protected virtual void HUDTimeMenuBox()
        {
            string CollapseButtonText = IsHUDTimeMinimized ? "O" : "-";

            if (GUI.Button(new Rect(HUDTimeScreen.width - 40f, 0f, 20f, 20f), CollapseButtonText, GUI.skin.button))
            {
                CollapseHUDTimeWindow();
            }
            if (GUI.Button(new Rect(HUDTimeScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow(1);
            }
        }

        protected virtual void InitModTimeScreen(int windowID)
        {
            ModTimeScreenStartPositionX = ModTimeScreen.x;
            ModTimeScreenStartPositionY = ModTimeScreen.y;
            ModTimeScreenTotalWidth = ModTimeScreen.width;

            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModTimeScreenMenuBox();
                if (!IsModTimeMinimized && !IsModTimeResizing)
                {
                    ModTimeManagerBox();
                    WeatherManagerBox();
                    TimeManagerBox();
                    HealthManagerBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        protected virtual void HealthManagerBox()
        {
            if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Health Manager", LocalStylingManager.ColoredHeaderLabel(LocalStylingManager.DefaultAttentionColor));
                    GUILayout.Label($"Health Options", LocalStylingManager.ColoredSubHeaderLabel(LocalStylingManager.DefaultAttentionColor));

                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Click in case of emergency, when settings go wrong!", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));

                        if (GUILayout.Button($"Fully heal player!", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickFullyHealButton();
                        }
                    }

                    NutrientsSettingsBox();
                    ConditionMultipliersBox();
                }
            }
            else
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To use, please enable health manager in the options above.", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));
                }
            }
        }

        protected virtual void TimeManagerBox()
        {
            if (LocalTimeManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Time Manager", LocalStylingManager.ColoredHeaderLabel(LocalStylingManager.DefaultAttentionColor));

                    GUILayout.Label($"Time Options", LocalStylingManager.ColoredSubHeaderLabel(LocalStylingManager.DefaultAttentionColor));

                    DayTimeScalesBox();
                    DayCycleBox();
                    TimeScalesBox();
                    ShowHUDTimeOptionBox();
                }
            }
            else
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To use, please enable time manager in the options above.", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));
                }
            }
        }

        protected virtual void WeatherManagerBox()
        {
            if (LocalWeatherManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Weather Manager", LocalStylingManager.ColoredHeaderLabel(LocalStylingManager.DefaultAttentionColor));                    
                    GUILayout.Label($"Weather Options", LocalStylingManager.ColoredSubHeaderLabel(LocalStylingManager.DefaultAttentionColor));

                    RainOption();
                }
            }
            else
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To use, please enable weather manager in the options above.", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));                    
                }
            }
        }

        protected virtual void ModTimeManagerBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{ModName} Manager", LocalStylingManager.ColoredHeaderLabel(LocalStylingManager.DefaultAttentionColor));
                    GUILayout.Label($"{ModName} Options", LocalStylingManager.ColoredSubHeaderLabel(LocalStylingManager.DefaultAttentionColor));

                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        if (GUILayout.Button($"Mod Info", GUI.skin.button))
                        {
                            ToggleShowUI(3);
                        }
                        if (ShowModTimeInfo)
                        {
                            ModTimeInfoBox();
                        }

                        MultiplayerOptionBox();

                        WeatherManagerOption();

                        TimeManagerOption();

                        HealthManagerOption();
                    }
                }        
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        protected virtual void ModTimeInfoBox()
        {
            using (var modinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModInfoScrollViewPosition = GUILayout.BeginScrollView(ModInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));

                GUILayout.Label("Mod Info", LocalStylingManager.ColoredSubHeaderLabel(LocalStylingManager.DefaultHighlightColor));

                using (var gidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.GameID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.GameID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (var midScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.ID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.ID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (var uidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.UniqueID)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.UniqueID}", LocalStylingManager.FormFieldValueLabel);
                }
                using (var versionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.Version)}:", LocalStylingManager.FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.Version}", LocalStylingManager.FormFieldValueLabel);
                }

                GUILayout.Label("Buttons Info", LocalStylingManager.ColoredSubHeaderLabel(LocalStylingManager.DefaultHighlightColor));

                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (var btnidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.ID)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.ID}", LocalStylingManager.FormFieldValueLabel);
                    }
                    using (var btnbindScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.KeyBinding)}:", LocalStylingManager.FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", LocalStylingManager.FormFieldValueLabel);
                    }
                }

                GUILayout.EndScrollView();
            }
        }

        protected virtual void MultiplayerOptionBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    string multiplayerOptionMessage = string.Empty;
                    GUILayout.Label("Multiplayer Info", LocalStylingManager.ColoredSubHeaderLabel(LocalStylingManager.DefaultHighlightColor));
                    if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                    {
                        if (IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are the game host";
                        }
                        if (IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host allowed usage";
                        }
                        GUILayout.Label($"{PermissionChangedMessage($"granted", multiplayerOptionMessage)}", LocalStylingManager.ColoredToggleValueTextLabel(true, Color.green, LocalStylingManager.DefaultAttentionColor));
                    }
                    else
                    {
                        if (!IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are not the game host";
                        }
                        if (!IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host did not allow usage";
                        }
                        GUILayout.Label($"{PermissionChangedMessage($"revoked", multiplayerOptionMessage)}", LocalStylingManager.ColoredToggleValueTextLabel(false, Color.green, LocalStylingManager.DefaultAttentionColor));
                    }                  
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MultiplayerOptionBox));
            }
        }

        protected virtual void RainOption()
        {        
            try
            {
                if (LocalWeatherManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Weather Info", LocalStylingManager.ColoredSubHeaderLabel(LocalStylingManager.DefaultHighlightColor));
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"Current weather: ", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                            GUILayout.Label($"{(LocalWeatherManager.IsRainFallingNow() ? "Raining" : "Dry")} weather", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                        }
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"Change weather to ", LocalStylingManager.TextLabel);
                            bool _isRainEnabled = LocalWeatherManager.IsRainEnabled;
                            LocalWeatherManager.IsRainEnabled = GUILayout.Toggle(LocalWeatherManager.IsRainEnabled, $"{(LocalWeatherManager.IsRainFallingNow() ? "Dry" : "Raining")} weather", LocalStylingManager.ToggleButton);
                            if (_isRainEnabled != LocalWeatherManager.IsRainEnabled)
                            {
                                if (LocalWeatherManager.IsRainEnabled)
                                {
                                   if( LocalWeatherManager.StartRain())
                                    {
                                        ShowHUDBigInfo(HUDBigInfoMessage($"The rain will start falling", MessageType.Info, Color.green), 3f);
                                    }
                                    else
                                    {
                                        ShowHUDBigInfo(HUDBigInfoMessage($"Could not change the weather!", MessageType.Warning, Color.red), 3f);
                                    }
                                }
                                else
                                {
                                    if(LocalWeatherManager.StopRain())
                                    {
                                        ShowHUDBigInfo(HUDBigInfoMessage($"The rain will stop falling", MessageType.Info, Color.green), 3f);
                                    }
                                    else
                                    {
                                        ShowHUDBigInfo(HUDBigInfoMessage($"Could not change the weather!", MessageType.Warning, Color.red), 3f);
                                    }
                                }                               
                            }
                        }
                    }
                }               
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(RainOption));
            }
        }

        protected virtual void ShowHUDTimeOptionBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("HUD Time Info", LocalStylingManager.ColoredSubHeaderLabel(LocalStylingManager.DefaultHighlightColor));
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current setting: ", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                        GUILayout.Label($"HUD Time {(LocalTimeManager.IsHUDTimeEnabled ? "visible" : "hidden")}", LocalStylingManager.ColoredToggleFieldValueLabel(LocalTimeManager.IsHUDTimeEnabled, LocalStylingManager.DefaultHighlightColor, LocalStylingManager.DefaultHighlightColor));
                    }

                    GUILayout.Label($"Show or hide the time HUD using this setting.", LocalStylingManager.TextLabel);

                    bool _isHUDTimeEnabled = LocalTimeManager.IsHUDTimeEnabled;
                    LocalTimeManager.IsHUDTimeEnabled = GUILayout.Toggle(LocalTimeManager.IsHUDTimeEnabled, $"{(LocalTimeManager.IsHUDTimeEnabled ? "Hide" : "Show")} time HUD?", GUI.skin.toggle);
                    if (_isHUDTimeEnabled != LocalTimeManager.IsHUDTimeEnabled)
                    {
                        ToggleShowUI(6);
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ShowHUDTimeOptionBox));
            }
        }

        protected virtual void WeatherManagerOption()
        {
            try
            {
                LocalWeatherManager.IsModEnabled = GUILayout.Toggle(LocalWeatherManager.IsModEnabled, $"Switch weather manager on / off.", GUI.skin.toggle);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(WeatherManagerOption));
            }
        }

        protected virtual void HealthManagerOption()
        {
            try
            {            
                LocalHealthManager.IsModEnabled = GUILayout.Toggle(LocalHealthManager.IsModEnabled, $"Switch health manager on / off.", GUI.skin.toggle);               
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(HealthManagerOption));
            }
        }

        protected virtual void TimeManagerOption()
        {
            try
            {
                LocalTimeManager.IsModEnabled = GUILayout.Toggle(LocalTimeManager.IsModEnabled, $"Switch time manager on / off.", GUI.skin.toggle);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(HealthManagerOption));
            }
        }

        protected virtual void DayCycleBox()
        {
            if (LocalTimeManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current time of day setting: ", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                        GUILayout.Label($"{(LocalTimeManager.IsNight() ? "night time" : "daytime")}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                    }

                    GUILayout.Label("Please note that the time skipped has an impact on player condition! Enable health manager for more info.", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));

                    GUILayout.Label("Go fast forward to the next daytime or night time cycle:",LocalStylingManager.TextLabel);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"To set game time to {( LocalTimeManager.IsNight( ) ? "daytime" : "night time")}, click", LocalStylingManager.TextLabel);
                        if (GUILayout.Button("FFW >>", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickFastForwardDayCycleButton();
                        }
                    }
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        protected virtual void DayTimeScalesBox()
        {
            if (LocalTimeManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (var timescalesScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current daytime length in minutes: ", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                        GUILayout.Label($"{LocalTimeManager.DayLengthInMinutes}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current night time length in minutes: ", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                        GUILayout.Label($"{LocalTimeManager.NightLengthInMinutes}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                    }

                    GUILayout.Label("The scaling is based on 24 hours =  720 minutes daytime +  720 minutes night time = real-time", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));

                    GUILayout.Label("Change scales for in-game day - and night time length in real-life minutes.", LocalStylingManager.TextLabel);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Daytime length: ", LocalStylingManager.TextLabel);
                        LocalTimeManager.DayLengthInMinutes = GUILayout.TextField(LocalTimeManager.DayLengthInMinutes, LocalStylingManager.FormInputTextField);
                        GUILayout.Label("Night time length: ", LocalStylingManager.TextLabel);
                        LocalTimeManager.NightLengthInMinutes = GUILayout.TextField(LocalTimeManager.NightLengthInMinutes, LocalStylingManager.FormInputTextField);
                        if (GUILayout.Button("Apply", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            OnClickSetTimeLengthInMinutesButton();
                        }
                    }                  
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        protected virtual void TimeScalesBox()
        {
            if (LocalTimeManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                string[] timeScaleModes = LocalTimeManager.GetTimeScaleModes();
                int _selectedTimeScaleModeIndex = LocalTimeManager.SelectedTimeScaleModeIndex;
                string _selectedTimeScaleMode = timeScaleModes[LocalTimeManager.SelectedTimeScaleModeIndex];
                LocalTimeManager.SelectedTimeScaleMode = EnumUtils<TimeScaleModes>.GetValue(_selectedTimeScaleMode);

                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current time progress speed:",    LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                        GUILayout.Label($"{LocalTimeManager.GetTimeProgressSpeed()}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                    }

                    GUILayout.Label($"Time progress is calculated using the set time scale mode's factor, multiplied by the set slowmotion factor.", LocalStylingManager.TextLabel);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Set time scale {LocalTimeManager.SelectedTimeScaleMode} factor:", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                        GUILayout.Label($"{LocalTimeManager.GetTimeScaleFactor(LocalTimeManager.SelectedTimeScaleMode)}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Set slowmotion factor:", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                        GUILayout.Label($"{LocalTimeManager.SlowMotionFactor}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                    }
                   
                    GUILayout.Label("Choose a time scale mode: ", LocalStylingManager.TextLabel);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        LocalTimeManager.SelectedTimeScaleModeIndex = GUILayout.SelectionGrid(LocalTimeManager.SelectedTimeScaleModeIndex, timeScaleModes, timeScaleModes.Length,LocalStylingManager.ColoredSelectedGridButton(_selectedTimeScaleModeIndex != LocalTimeManager.SelectedTimeScaleModeIndex));
                        if (_selectedTimeScaleModeIndex != LocalTimeManager.SelectedTimeScaleModeIndex)
                        {
                              _selectedTimeScaleMode = timeScaleModes[LocalTimeManager.SelectedTimeScaleModeIndex];
                            LocalTimeManager.SelectedTimeScaleMode = EnumUtils<TimeScaleModes>.GetValue(_selectedTimeScaleMode);
                        }
                        if (GUILayout.Button("Apply", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            LocalTimeManager.SetSelectedTimeScaleMode(LocalTimeManager.SelectedTimeScaleModeIndex);
                        }                       
                    }                  
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        protected virtual void ConditionMultipliersBox()
        {
            if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                ConditionMulsSettingsBox();              
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        protected virtual void NutrientsSettingsBox()
        {
            if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                string[] NutrientsDepletionPresetNames = LocalHealthManager.GetNutrientsDepletionNames();              
                int _selectedActiveNutrientsDepletionPresetIndex = LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex;
                string _selectedActiveNutrientsDepletionPresetName = NutrientsDepletionPresetNames[LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex];
                string activeDepletionSetToMessage = $"Nutrients depletion preset {_selectedActiveNutrientsDepletionPresetName} has been activated!";
                string _activeNutrientsDepletionPresetName = NutrientsDepletionPresetNames[LocalHealthManager.ActiveNutrientsDepletionPresetIndex];
                LocalHealthManager.ActiveNutrientsDepletionPreset = EnumUtils<NutrientsDepletion>.GetValue(_activeNutrientsDepletionPresetName);

                using (new GUILayout.VerticalScope(GUI.skin.label))
                {  
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Active nutrition depletion preset: ", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                        GUILayout.Label($"{LocalHealthManager.ActiveNutrientsDepletionPreset}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                    }

                    GUILayout.Label("Each preset is an in-game defined preset that by default can be set only once for a game session in your game difficulty settings.", LocalStylingManager.TextLabel);
                    
                    GUILayout.Label("Please note that changing the preset, has to be applied!", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));

                    GUILayout.Label($"Choose a nutrients depletion preset.", LocalStylingManager.TextLabel);
                    LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex = GUILayout.SelectionGrid(LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex, NutrientsDepletionPresetNames, NutrientsDepletionPresetNames.Length, LocalStylingManager.ColoredSelectedGridButton(_selectedActiveNutrientsDepletionPresetIndex!= LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex));
                    if (_selectedActiveNutrientsDepletionPresetIndex != LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex)
                    {
                        _selectedActiveNutrientsDepletionPresetName = NutrientsDepletionPresetNames[LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex];
                    }

                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"To apply the setting, click", LocalStylingManager.TextLabel);
                        if (GUILayout.Button("Apply", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            bool ok = LocalHealthManager.SetActiveNutrientsDepletionPreset(LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex);
                            if (ok)
                            {
                                ShowHUDBigInfo(HUDBigInfoMessage(activeDepletionSetToMessage, MessageType.Info, Color.green));
                            }
                            else
                            {
                                ShowHUDBigInfo(HUDBigInfoMessage($"Could not set {LocalHealthManager.SelectedActiveNutrientsDepletionPreset}", MessageType.Warning, LocalStylingManager.DefaultAttentionColor));
                            }
                        }
                    }
                }
            }            
        }

        protected virtual void ConditionMulsSettingsBox()
        {
            if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    CheatModeOptionBox();

                    if (!Cheats.m_GodMode)
                    {
                        ConditionParameterLossOptionBox();

                        //MultipliersOptionBox();

                        MultipliersBox();
                    }
                    else
                    {
                        GUILayout.Label($"To use parameter loss option and condition settings, first disable God mode.", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));
                    }
                }
            }
        }

        protected virtual void CheatModeOptionBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.label))
                {
                    GUILayout.Label($"Avoid any player damage.", LocalStylingManager.TextLabel);

                    GUILayout.Label($"Please note that this setting is effective immediately!", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));

                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"To enable or disable God mode, click ", LocalStylingManager.TextLabel);
                        Cheats.m_GodMode = GUILayout.Toggle(Cheats.m_GodMode, $"Switch cheat {(Cheats.m_GodMode ? "off" : "on")}", LocalStylingManager.ColoredToggleButton(Cheats.m_GodMode));
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(CheatModeOptionBox));
            }
        }

        protected virtual void ConditionParameterLossOptionBox()
        {
            try
            {
                if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
                {
                    using (new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"Current parameter loss setting:", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                            GUILayout.Label($"{(LocalHealthManager.GetParameterLossBlocked() ? "Blocked." : "Unblocked. Watch out for player condition!")}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                        }
                        GUILayout.Label($"Avoid any player condition depletion with parameter loss setting blocked. When blocked, the player does not need to worry about nutrients.", LocalStylingManager.TextLabel);
                        GUILayout.Label($"Please note that this setting has NOT the same effect as enabling God Mode. The player can still take damage!", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            bool _IsParameterLossBlocked = LocalHealthManager.IsParameterLossBlocked;
                            GUILayout.Label($"Set parameter loss", LocalStylingManager.TextLabel);
                            LocalHealthManager.IsParameterLossBlocked = GUILayout.Toggle(LocalHealthManager.IsParameterLossBlocked, $"{(LocalHealthManager.GetParameterLossBlocked() ? "Unblock" : "Block")} parameter loss", LocalStylingManager.ColoredToggleButton(LocalHealthManager.IsParameterLossBlocked));
                            if (_IsParameterLossBlocked != LocalHealthManager.IsParameterLossBlocked)
                            {
                                if (LocalHealthManager.IsParameterLossBlocked)
                                {
                                    LocalHealthManager.BlockParametersLoss();
                                }
                                else
                                {
                                    LocalHealthManager.UnblockParametersLoss();
                                }
                                ShowHUDBigInfo(HUDBigInfoMessage($"Parameter loss has been {(LocalHealthManager.GetParameterLossBlocked() ? "blocked." : "unblocked.")} ", MessageType.Info, Color.green));
                            }
                        }
                    }
                }
                else
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"To use, please enable health manager in the options above.", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ConditionParameterLossOptionBox));
            }
        }

        protected virtual void MultipliersOptionBox()
        {
            try
            {
                if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
                {
                    using (new GUILayout.VerticalScope(GUI.skin.label))
                    {
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"Current active multipliers:", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                            GUILayout.Label($"{(LocalHealthManager.GetUseDefault() ? "Default multipliers" : "Custom multipliers. Watch your player condition!")}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                        }
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"{nameof(LocalHealthManager.UseDefault)}", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                            GUILayout.Label($"{LocalHealthManager.UseDefault}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                        }
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"{nameof(LocalHealthManager.GetUseDefault)}", LocalStylingManager.ColoredFieldNameLabel(LocalStylingManager.DefaultHighlightColor));
                            GUILayout.Label($"{LocalHealthManager.GetUseDefault()}", LocalStylingManager.ColoredFieldValueLabel(LocalStylingManager.DefaultHighlightColor));
                        }
                        GUILayout.Label($"Choose which condition multipliers to activate. When custom multipliers are active, the player condition will use custom multiplier settings.", LocalStylingManager.TextLabel);
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            bool _UseDefault = LocalHealthManager.GetUseDefault();
                            LocalHealthManager.UseDefault = GUILayout.Toggle(LocalHealthManager.UseDefault, $"{(LocalHealthManager.GetUseDefault() ? "Activate custom multipliers" : "Activate default multipliers")}", LocalStylingManager.ToggleButton);
                            if (_UseDefault != LocalHealthManager.UseDefault)
                            {
                                if (LocalHealthManager.UseDefault)
                                {
                                    LocalHealthManager.SetUseDefault();
                                    ShowHUDBigInfo(HUDBigInfoMessage($"Default multipliers activated!\n" +
                                        $"{nameof(LocalHealthManager.UseDefault)} returns {LocalHealthManager.UseDefault}\n" +
                                        $"{nameof(LocalHealthManager.GetUseDefault)} returns {LocalHealthManager.GetUseDefault()}", MessageType.Info, Color.green));
                                }
                                else
                                {
                                    LocalHealthManager.SetUseCustom();
                                    ShowHUDBigInfo(HUDBigInfoMessage($"Custom multipliers activated!\n" +
                                        $"{nameof(LocalHealthManager.UseDefault)} returns {LocalHealthManager.UseDefault}\n" +
                                        $"{nameof(LocalHealthManager.GetUseDefault)} returns {LocalHealthManager.GetUseDefault()}", MessageType.Info, Color.green));
                                }                              
                            }
                        }
                    }
                }
                else
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"To use, please enable health manager in the options above.", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MultipliersOptionBox));
            }
        }

        protected virtual void MultipliersBox()
        {
            try
            {
                if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        if (GUILayout.Button($"Default settings"))
                        {
                            ToggleShowUI(1);
                        }
                        if (ShowDefaultMuls)
                        {
                            using (new GUILayout.VerticalScope(GUI.skin.box))
                            {
                                GUILayout.Label($"View default condition multipliers.", LocalStylingManager.TextLabel);
                                GUILayout.Label($"Please note that default condition multipliers cannot be changed!", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));

                                DefaultMulsScrollViewPosition = GUILayout.BeginScrollView(DefaultMulsScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(300f));
                                LocalHealthManager.GetDefaultMultiplierSliders();
                                GUILayout.EndScrollView();
                            }
                        }
                        if (GUILayout.Button($"Custom settings"))
                        {
                            ToggleShowUI(2);
                        }
                        if (ShowCustomMuls)
                        {
                            using (new GUILayout.VerticalScope(GUI.skin.box))
                            {
                                GUILayout.Label($"Adjust custom condition multipliers.", LocalStylingManager.TextLabel);
                                GUILayout.Label($"Please note that currently changed settings will not be applied until further notice (to bugged ;P )", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));

                                CustomMulsScrollViewPosition = GUILayout.BeginScrollView(CustomMulsScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(300f));
                                LocalHealthManager.GetCustomMultiplierSliders();
                                GUILayout.EndScrollView();
                            }
                        }
                    }
                }
                else
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"To use, please enable health manager in the options above.", LocalStylingManager.ColoredCommentLabel(LocalStylingManager.DefaultAttentionColor));
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MultipliersBox));
            }
        }

        protected virtual void OnClickSetTimeLengthInMinutesButton()
        {
            try
            {
                bool ok = LocalTimeManager.SetTimeLengthInMinutes(Convert.ToInt32(LocalTimeManager.DayLengthInMinutes), Convert.ToInt32(LocalTimeManager.NightLengthInMinutes));
                if (ok)
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(LocalTimeManager.TimeScalesSetMessage(LocalTimeManager.DayLengthInMinutes, LocalTimeManager.NightLengthInMinutes), MessageType.Info, Color.green));
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input {LocalTimeManager.DayLengthInMinutes} and {LocalTimeManager.NightLengthInMinutes}:\nPlease input numbers only - min. 0.1", MessageType.Warning, LocalStylingManager.DefaultAttentionColor));
                }            
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickSetTimeLengthInMinutesButton));
            }
        }

        protected virtual void OnClickFastForwardDayCycleButton()
        {
            try
            {
                string daytime = LocalTimeManager.SetToNextDayCycle();
                if (!string.IsNullOrEmpty(daytime))
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(LocalTimeManager.DayCycleSetMessage(daytime), MessageType.Info, Color.green));
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Could not set day cycle!", MessageType.Warning, LocalStylingManager.DefaultAttentionColor));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickFastForwardDayCycleButton));
            }
        }

        protected virtual void OnClickFullyHealButton()
        {
            try
            {
                bool ok = LocalHealthManager.ResetParams();
                if (ok)
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Player fully healed!", MessageType.Info, Color.green));
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Could not heal player!", MessageType.Warning, LocalStylingManager.DefaultAttentionColor));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickFullyHealButton));
            }
        }

    }
}
