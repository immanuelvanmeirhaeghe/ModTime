using Enums;
using ModManager;
using ModManager.Data.Interfaces;
using ModManager.Data.Modding;
using ModTime.Data;
using ModTime.Data.Enums;
using ModTime.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModTime
{
    /// <summary>
    /// ModTime is a mod for Green Hell that allows a player to set in-game player condition multipliers,
    /// date and day and night time scales in real time minutes.
    /// Ingame time can be fast forwarded to the next morning 5AM or night 10PM.
    /// It also allows to manipulate weather to make it rain or stop raining.
    /// Press Keypad2 (default) or the key configurable in ModAPI to open the mod screen.
    /// </summary>
    public class ModTime : MonoBehaviour
    {
        private static ModTime Instance;
        private static readonly string RuntimeConfiguration = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), $"{nameof(RuntimeConfiguration)}.xml");

        private static readonly string ModName = nameof(ModTime);

        private static float ModTimeScreenTotalWidth { get; set; } = 500f;
        private static float ModTimeScreenTotalHeight { get; set; } = 450f;      
        private static float ModTimeScreenMinWidth { get; set; } = 500f;
        private static float ModTimeScreenMaxWidth { get; set; } = Screen.width;
        private static float ModTimeScreenMinHeight { get; set; } = 50f;
        private static  float ModTimeScreenMaxHeight { get; set; } = Screen.height;
        private static float ModTimeScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModTimeScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsModTimeMinimized { get; set; } = false;
        private static int ModTimeScreenId { get; set; } = -1;

        private static float HUDTimeScreenTotalWidth { get; set; } = 150f;
        private static float HUDTimeScreenTotalHeight { get; set; } = 150f;
        private static float HUDTimeScreenMinWidth { get; set; } = 150f;
        private static float HUDTimeScreenMinHeight { get; set; } = 150f;
        private static float HUDTimeScreenMaxWidth { get; set; } = 150f;
        private static float HUDTimeScreenMaxHeight { get; set; } = 150f;
        private static float HUDTimeScreenStartPositionX { get; set; } = 0f;
        private static float HUDTimeScreenStartPositionY { get; set; } = Screen.height - HUDTimeScreenTotalHeight;
        private static bool IsHUDTimeMinimized { get; set; }
        private static int HUDTimeScreenId { get; set; } = -1;

        private Color DefaultColor = GUI.color;
        private Color DefaultContentColor = GUI.contentColor;
        private Color DefaultBackGroundColor = GUI.backgroundColor;
        public GUIStyle InfoHeaderLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 12
        };
        public GUIStyle InfoFieldNameLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true,
            fontStyle = FontStyle.Italic
        };
        public GUIStyle InfoFieldValueLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12,
            stretchWidth = true,
            wordWrap = true
        };
        public GUIStyle HUDTimeWatchLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            stretchWidth = true,
            stretchHeight = true            
        };

        public GUIStyle HUDTimeDateLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            stretchWidth = true,
            stretchHeight = true
        };

        private bool ShowModTime { get; set; } = false;
        private bool ShowDefaultMuls { get; set; } = false;
        private bool ShowCustomMuls { get; set; } = false;
        private bool ShowModInfo { get; set; } = false;
        private bool ShowHUDTime { get; set; } = false;        

        private static Rect ModTimeScreen = new Rect(ModTimeScreenStartPositionX, ModTimeScreenStartPositionY, ModTimeScreenTotalWidth, ModTimeScreenTotalHeight);
        private static Rect HUDTimeScreen = new Rect(HUDTimeScreenStartPositionX, HUDTimeScreenStartPositionY, HUDTimeScreenTotalWidth, HUDTimeScreenTotalHeight);

        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static WeatherManager LocalWeatherManager;
        private static HealthManager LocalHealthManager;
        private static TimeManager LocalTimeManager;
              
        public KeyCode ShortcutKey { get; set; } = KeyCode.Keypad2;

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();
      
        public Vector2 DefaultMulsScrollViewPosition { get; private set; }
        public Vector2 CustomMulsScrollViewPosition { get; private set; }
        public Vector2 ModInfoScrollViewPosition { get; private set; }
        public IConfigurableMod SelectedMod { get; set; }

        public ModTime()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static ModTime Get()
        {
            return Instance;
        }

        private string DayCycleSetMessage(string daytime)
            => $"{daytime}";
        private string TimeScalesSetMessage(string dayTimeScale, string nightTimeScale)
            => $"Time scales set:\nDay time passes in " + dayTimeScale + " realtime minutes\nand night time in " + nightTimeScale + " realtime minutes.";
        private string OnlyForSinglePlayerOrHostMessage()
            => "Only available for single player or when host. Host can activate using ModManager.";
        private string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        private string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";
        private void OnlyForSingleplayerOrWhenHostBox()
        {
            using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUI.color = Color.yellow;
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
                GUI.color = DefaultColor;
            }
        }
        
        private KeyCode GetConfigurableModShortcutKey(string buttonId)
        {
            KeyCode result = KeyCode.None;
            string value = string.Empty;
            try
            {
                if (File.Exists(RuntimeConfiguration))
                {
                    using (XmlReader xmlReader = XmlReader.Create(new StreamReader(RuntimeConfiguration)))
                    {
                        while (xmlReader.Read())
                        {
                            if (xmlReader["ID"] == ModName && xmlReader.ReadToFollowing("Button") && xmlReader["ID"] == buttonId)
                            {
                                value = xmlReader.ReadElementContentAsString();
                            }
                        }
                    }
                }
                if (!string.IsNullOrEmpty(value))
                {
                    result = EnumUtils<KeyCode>.GetValue(value);
                }
                else if (buttonId == nameof(ShortcutKey))
                {
                    result = ShortcutKey;
                }
                return result;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetConfigurableModShortcutKey));
                if (buttonId == nameof(ShortcutKey))
                {
                    result = ShortcutKey;
                }
                return result;
            }
        }

        public KeyCode GetShortcutKey()
        {
            string buttonID = nameof(ShortcutKey);
            var ConfigurableModList = GetModList();
            if (ConfigurableModList != null && ConfigurableModList.Count > 0)
            {
                SelectedMod = ConfigurableModList.Find(cfgMod => cfgMod.ID == ModName);
                return SelectedMod.ConfigurableModButtons.Find(cfgButton => cfgButton.ID == buttonID).ShortcutKey;
            }
            else
            {
                return KeyCode.Keypad2; 
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

                                var configurableMod = new ModManager.Data.Modding.ConfigurableMod(gameID, modID, uniqueID, version);

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

        private void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ShortcutKey = GetShortcutKey();
        }

        public GUIStyle ColoredToggleValueLabel(GUIStyle style, bool enabled)
        {
            style.normal.textColor = enabled ? Color.green : DefaultColor;
            return style;
        }

        public GUIStyle ReColoredControl(Rect screenRect, GUIStyle style, Color textNormal, Color background)
        {
            style.normal.textColor = textNormal;
            style.normal.background = MakeBackgroundTexture((int)screenRect.width, (int)screenRect.height, background);
            return style;
        }

        public Texture2D MakeBackgroundTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D backgroundTexture = new Texture2D(width, height);

            backgroundTexture.SetPixels(pixels);
            backgroundTexture.Apply();

            return backgroundTexture;
        }

        public GUIStyle ColoredInfoHeaderLabel(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            return style;
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
            ShowHUDBigInfo(HUDBigInfoMessage(info, MessageType.Error, Color.red));
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            string reason = optionValue ? "the game host allowed usage" : "the game host did not allow usage";
            IsModActiveForMultiplayer = optionValue;

            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage(PermissionChangedMessage($"granted", $"{reason}"), MessageType.Info, Color.green)
                            : HUDBigInfoMessage(PermissionChangedMessage($"revoked", $"{reason}"), MessageType.Info, Color.yellow))
                            );
        }

        public void ShowHUDBigInfo(string text)
        {
            string header = ModName + " Info";
            string textureName = HUDInfoLogTextureType.Reputation.ToString();
            HUDBigInfo obj = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = 2f;
            HUDBigInfoData data = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            obj.AddInfo(data);
            obj.Show(show: true);
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        private void EnableCursor(bool blockPlayer = false)
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

        private void Update()
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
                    EnableCursor();
                }
            }           
        }

        private void ToggleShowUI(int level)
        {
            switch (level)
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
                    ShowModInfo = !ShowModInfo;
                    return;
                case 6:
                    ShowHUDTime = !ShowHUDTime;
                    return;
                default:
                    ShowModTime = !ShowModTime;
                    ShowDefaultMuls = !ShowDefaultMuls;
                    ShowCustomMuls = !ShowCustomMuls;
                    ShowModInfo = !ShowModInfo;
                    return;
            }
        }

        private void OnGUI()
        {
            if (ShowModTime || ShowHUDTime)
            {
                InitData();
                InitSkinUI();

                if (ShowModTime)
                {
                    InitModTimeWindow();
                }
                if (ShowHUDTime)
                {
                    InitHUDTimeWindow();
                }
            }
        }

        private void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalHealthManager = HealthManager.Get();
            LocalTimeManager = TimeManager.Get();
            LocalWeatherManager = WeatherManager.Get();
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitModTimeWindow()
        {
            if (ModTimeScreenId < 0 || ModTimeScreenId == HUDTimeScreenId)
            {
                ModTimeScreenId = GetHashCode();
            }
            string modWindowTitle = $"{ModName} created by [Dragon Legion] Immaanuel#4300";
            ModTimeScreen = GUILayout.Window(ModTimeScreenId, ModTimeScreen, InitModTimeScreen, modWindowTitle, GUI.skin.window, GUILayout.ExpandWidth(true), GUILayout.MinWidth(ModTimeScreenMinWidth), GUILayout.MaxWidth(ModTimeScreenMaxWidth), GUILayout.ExpandHeight(true), GUILayout.MinHeight(ModTimeScreenMinHeight), GUILayout.MaxHeight(ModTimeScreenMaxHeight));
        }

        private void ScreenMenuBox()
        {
            string CollapseButtonText = IsModTimeMinimized ?  "O" :  "-";

            if (GUI.Button(new Rect(ModTimeScreen.width - 40f, 0f, 20f, 20f), CollapseButtonText, GUI.skin.button))
            {
                CollapseWindow();
            }
            if (GUI.Button(new Rect(ModTimeScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                ToggleShowUI(0);
            }
        }

        private void CollapseWindow()
        {
            ModTimeScreenStartPositionX = ModTimeScreen.x;
            ModTimeScreenStartPositionY = ModTimeScreen.y;
            ModTimeScreenTotalWidth = ModTimeScreen.width;          

            if (!IsModTimeMinimized)
            {
                ModTimeScreen = new Rect(ModTimeScreenStartPositionX, ModTimeScreenStartPositionY, ModTimeScreenTotalWidth, ModTimeScreenMinHeight);
                IsModTimeMinimized = true;
            }
            else
            {
                ModTimeScreen = new Rect(ModTimeScreenStartPositionX, ModTimeScreenStartPositionY, ModTimeScreenTotalWidth, ModTimeScreenTotalHeight);
                IsModTimeMinimized = false;
            }
            InitModTimeWindow();
        }

        private void InitHUDTimeWindow()
        {
            if (HUDTimeScreenId < 0 || HUDTimeScreenId == ModTimeScreenId)
            {
                HUDTimeScreenId = GetHashCode() + 1;
            }
            string hudWindowTitle = $"HUD Time";
            HUDTimeScreen = GUILayout.Window(HUDTimeScreenId, HUDTimeScreen, InitHUDTime, hudWindowTitle, GUI.skin.label, GUILayout.ExpandWidth(true), GUILayout.MinWidth(HUDTimeScreenMinWidth), GUILayout.MaxWidth(HUDTimeScreenMaxWidth), GUILayout.ExpandHeight(true), GUILayout.MinHeight(HUDTimeScreenMinHeight), GUILayout.MaxHeight(HUDTimeScreenMaxHeight));
        }

        private void CollapseHUDTimeWindow()
        {
            HUDTimeScreenStartPositionX = HUDTimeScreen.x;
            HUDTimeScreenStartPositionY = HUDTimeScreen.y;
            HUDTimeScreenTotalWidth = HUDTimeScreen.width;

            if (!IsHUDTimeMinimized)
            {
                HUDTimeScreen = new Rect(HUDTimeScreenStartPositionX, HUDTimeScreenStartPositionY, HUDTimeScreenTotalWidth, HUDTimeScreenMinHeight);
                IsHUDTimeMinimized = true;
            }
            else
            {
                HUDTimeScreen = new Rect(HUDTimeScreenStartPositionX, HUDTimeScreenStartPositionY, HUDTimeScreenTotalWidth, HUDTimeScreenTotalHeight);
                IsHUDTimeMinimized = false;
            }
            InitHUDTimeWindow();
        }

        private void CloseWindow()
        {
            ShowModTime = false;
            ShowHUDTime = false;
            EnableCursor();
        }

        private void InitHUDTime(int windowID)
        {
            HUDTimeScreenStartPositionX = HUDTimeScreen.x;
            HUDTimeScreenStartPositionY = HUDTimeScreen.y;
            HUDTimeScreenTotalWidth = HUDTimeScreen.width;

            using (var timehudContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                HUDTimeMenuBox();

                if (!IsHUDTimeMinimized)
                {
                    HUDTimeViewBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void HUDTimeViewBox()
        {
            using (var positionScope = new GUILayout.VerticalScope(ReColoredControl(HUDTimeScreen, GUI.skin.box, DefaultColor, Color.clear)))
            {
                GUIContent timeWatch = new GUIContent($"{LocalTimeManager.GetCurrentTime()}.");
                GUIContent dateWatch = new GUIContent($"{LocalTimeManager.GetCurrentDate()}.");
                GUILayout.Label(timeWatch, HUDTimeWatchLabel);
                GUILayout.Label(dateWatch, HUDTimeDateLabel);
            }                
        }

        private void HUDTimeMenuBox()
        {
            string CollapseButtonText = IsModTimeMinimized ? "O" : "-";

            if (GUI.Button(new Rect(HUDTimeScreen.width - 40f, 0f, 20f, 20f), CollapseButtonText, GUI.skin.button))
            {
                CollapseHUDTimeWindow();
            }
            if (GUI.Button(new Rect(HUDTimeScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                ToggleShowUI(6);
            }
        }

        private void InitModTimeScreen(int windowID)
        {
            ModTimeScreenStartPositionX = ModTimeScreen.x;
            ModTimeScreenStartPositionY = ModTimeScreen.y;
            ModTimeScreenTotalWidth = ModTimeScreen.width;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
               ScreenMenuBox();

                if (!IsModTimeMinimized)
                {
                    ModOptionsBox();
                    WeatherManagerBox();
                    TimeManagerBox();
                    HealthManagerBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void HealthManagerBox()
        {
            if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                GUILayout.Label($"Health Manager", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.yellow));
                
                NutrientsSettingsBox();
                ConditionMultipliersBox();                
            }
            else
            {
                using (var enablehmmulboxscope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To use, please enable health manager in the options above.", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.yellow));
                }
            }
        }

        private void TimeManagerBox()
        {
            if (LocalTimeManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (var timemngboxscope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"Time manager", GUI.skin.label);
                    GUI.color = DefaultColor;

                    DayTimeScalesBox();
                    DayCycleBox();
                    TimeScalesBox();
                    WatchOptionBox();
                }
            }
            else
            {
                using (var enabletimemngboxscope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"To use, please enable time manager in the options above.", GUI.skin.label);
                    GUI.color = DefaultColor;
                }
            }
        }

        private void WeatherManagerBox()
        {
            if (LocalWeatherManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (var weathermngrScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"Weather manager", GUI.skin.label);
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Weather options: ", GUI.skin.label);
                    GUI.color = DefaultColor;

                    RainOption();
                }
            }
            else
            {
                using (var enablelweatherboxscope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"Enable weather manager in the {ModName} options.", GUI.skin.label);
                    GUI.color = DefaultColor;
                }
            }
        }

        private void ModOptionsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var modOptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{ModName} Options", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                    using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        if (GUILayout.Button($"Mod info"))
                        {
                            ToggleShowUI(3);
                        }
                        if (ShowModInfo)
                        {
                            ModInfoBox();
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

        private void ModInfoBox()
        {
            using (var modinfoScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ModInfoScrollViewPosition = GUILayout.BeginScrollView(ModInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(100f));

                GUILayout.Label("Mod info", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                using (var gidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.GameID)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.GameID}", InfoFieldValueLabel);
                }
                using (var midScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.ID)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.ID}", InfoFieldValueLabel);
                }
                using (var uidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.UniqueID)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.UniqueID}", InfoFieldValueLabel);
                }
                using (var versionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.Version)}:", InfoFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.Version}", InfoFieldValueLabel);
                }

                GUILayout.Label("Mod buttons info", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (var btnidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.ID)}:", InfoFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.ID}", InfoFieldValueLabel);
                    }
                    using (var btnbindScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.KeyBinding)}:", InfoFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", InfoFieldValueLabel);
                    }
                }

                GUILayout.EndScrollView();
            }
        }

        private void MultiplayerOptionBox()
        {
            try
            {
                using (var multiplayeroptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label("Multiplayer options:", GUI.skin.label);                    
                    string multiplayerOptionMessage = string.Empty;
                    GUI.color = DefaultColor;
                    if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
                    {
                        GUI.color = Color.green;
                        if (IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are the game host";
                        }
                        if (IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host allowed usage";
                        }
                        _ = GUILayout.Toggle(true, PermissionChangedMessage($"granted", multiplayerOptionMessage), GUI.skin.toggle);
                        GUI.color = DefaultColor;
                    }
                    else
                    {
                        GUI.color = Color.yellow;
                        if (!IsModActiveForSingleplayer)
                        {
                            multiplayerOptionMessage = $"you are not the game host";
                        }
                        if (!IsModActiveForMultiplayer)
                        {
                            multiplayerOptionMessage = $"the game host did not allow usage";
                        }
                        _ = GUILayout.Toggle(false, PermissionChangedMessage($"revoked", $"{multiplayerOptionMessage}"), GUI.skin.toggle);
                        GUI.color = DefaultColor;
                    }                  
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MultiplayerOptionBox));
            }
        }

        private void RainOption()
        {
            try
            {
                bool _tRainEnabled = LocalWeatherManager.IsRainEnabled;
                LocalWeatherManager.IsRainEnabled = GUILayout.Toggle(LocalWeatherManager.IsRainEnabled, "Switch between raining or dry weather?", GUI.skin.toggle);
                if (_tRainEnabled != LocalWeatherManager.IsRainEnabled)
                {
                    if (LocalWeatherManager.IsRainEnabled)
                    {
                        LocalWeatherManager.StartRain();
                    }
                    else
                    {
                        LocalWeatherManager.StopRain();
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(RainOption));
            }
        }

        private void WatchOptionBox()
        {
            try
            {
                using (var gtimeoptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Time Options", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                    bool _EnableTimeHUD = LocalTimeManager.EnableTimeHUD;
                    LocalTimeManager.EnableTimeHUD = GUILayout.Toggle(LocalTimeManager.EnableTimeHUD, $"Enable the time HUD?", GUI.skin.toggle);
                    if (_EnableTimeHUD != LocalTimeManager.EnableTimeHUD)
                    {
                        ToggleShowUI(6);
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(WatchOptionBox));
            }
        }

        private void WeatherManagerOption()
        {
            try
            {
                LocalWeatherManager.IsModEnabled = GUILayout.Toggle(LocalWeatherManager.IsModEnabled, $"Switch weather manager on / off", GUI.skin.toggle);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(WeatherManagerOption));
            }
        }

        private void HealthManagerOption()
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

        private void TimeManagerOption()
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

        private void DayCycleBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var timeofdayBoxScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Current time of day: {(LocalTimeManager.IsNight() ? "night time" : "daytime")}", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                    GUILayout.Label("Please note that the time skipped has an impact on player condition! Enable health manager for more info.", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.yellow));

                    GUILayout.Label("Go fast forward to the next daytime or night time cycle:", GUI.skin.label);
                    using (var actbutScopeView = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"To set game time to {( LocalTimeManager.IsNight( ) ? "daytime" : "night time")}, click", GUI.skin.label);
                        if (GUILayout.Button("FFW >>", GUI.skin.button, GUILayout.MaxWidth(50f)))
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

        private void DayTimeScalesBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var timescalesScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Current daytime length: {LocalTimeManager.DayTimeScaleInMinutes} and night time length: {LocalTimeManager.NightTimeScaleInMinutes} ", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                    GUILayout.Label("Change scales for in-game day - and night time length in real-life minutes. To set, click [Apply]", GUI.skin.label);
                    using (var timescalesInputScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Daytime length: ", GUI.skin.label);
                        LocalTimeManager.DayTimeScaleInMinutes = GUILayout.TextField(LocalTimeManager.DayTimeScaleInMinutes, GUI.skin.textField);
                        GUILayout.Label("Night time length: ", GUI.skin.label);
                        LocalTimeManager.NightTimeScaleInMinutes = GUILayout.TextField(LocalTimeManager.NightTimeScaleInMinutes, GUI.skin.textField);
                        if (GUILayout.Button("Apply", GUI.skin.button, GUILayout.MaxWidth(50f)))
                        {
                            OnClickSetTimeScalesButton();
                        }
                    }                  
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void TimeScalesBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var ctimescalesScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Current time progress speed = {LocalTimeManager.GetTimeProgressSpeed()} = Time scale factor * slowmotion factor = {LocalTimeManager.GetTimeScaleFactor(LocalTimeManager.SelectedTimeScaleMode)} * {LocalTimeManager.GetSlowMotionFactor()}", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                    string[] timeScaleModes = LocalTimeManager.GetTimeScaleModes();
                    int _selectedTimeScaleModeIndex = LocalTimeManager.SelectedTimeScaleModeIndex;

                    GUILayout.Label("Choose a time scale mode. To set, click [Apply]", GUI.skin.label);
                    using (var timemodeInputScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        LocalTimeManager.SelectedTimeScaleModeIndex = GUILayout.SelectionGrid(LocalTimeManager.SelectedTimeScaleModeIndex, timeScaleModes, timeScaleModes.Length, GUI.skin.button);
                        if (_selectedTimeScaleModeIndex != LocalTimeManager.SelectedTimeScaleModeIndex)
                        {
                            string _selectedTimeScaleMode = timeScaleModes[LocalTimeManager.SelectedTimeScaleModeIndex];
                            LocalTimeManager.SelectedTimeScaleMode = EnumUtils<TimeScaleModes>.GetValue(_selectedTimeScaleMode);
                        }
                        if (GUILayout.Button("Apply", GUI.skin.button))
                        {
                            LocalTimeManager.SetTimeScaleMode(LocalTimeManager.SelectedTimeScaleModeIndex);
                        }                       
                    }
                    if (LocalTimeManager.SelectedTimeScaleMode == TimeScaleModes.Custom)
                    {
                        GUILayout.Label($"Choose slowmotion factor. To set, click [Apply]", GUI.skin.label);
                        LocalTimeManager.SlowMotionFactor = UIControlManager.CustomHorizontalSlider(LocalTimeManager.SlowMotionFactor, 0f, 1f, "factor");
                        if (GUILayout.Button("Apply", GUI.skin.button, GUILayout.MaxWidth(50f)))
                        {
                            LocalTimeManager.SetSlowMotionFactor(LocalTimeManager.SlowMotionFactor);
                        }
                    }
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void ConditionMultipliersBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                ConditionMulsSettingsBox();              
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void NutrientsSettingsBox()
        {
            using (var positionScope = new GUILayout.VerticalScope(GUI.skin.label))
            {
                string activeDepletionSetToMessage = $"Current active nutrients depletion preset: {LocalHealthManager.SelectedActiveNutrientsDepletionPreset}";
                GUILayout.Label(activeDepletionSetToMessage, ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));

                string[] depletionPresets = LocalHealthManager.GetNutrientsDepletionNames();
                int _selectedActiveNutrientsDepletionPresetIndex = LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex;          

                GUILayout.Label("Choose a nutrients depletion preset. To set, click [Apply]", GUI.skin.label);
             
                using (var apsInputScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex = GUILayout.SelectionGrid(LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex, depletionPresets, depletionPresets.Length, GUI.skin.button);
                    if (_selectedActiveNutrientsDepletionPresetIndex != LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex)
                    {
                        string selectedPreset = depletionPresets[LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex];
                        LocalHealthManager.SelectedActiveNutrientsDepletionPreset = EnumUtils<NutrientsDepletion>.GetValue(selectedPreset);
                    }
                    if (GUILayout.Button("Apply", GUI.skin.button, GUILayout.Width(150f)))
                    {
                        bool ok = LocalHealthManager.SetActiveNutrientsDepletionPreset(LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex);
                        if (ok)
                        {                            
                            ShowHUDBigInfo(HUDBigInfoMessage(activeDepletionSetToMessage, MessageType.Info, Color.green));
                        }
                        else
                        {
                            ShowHUDBigInfo(HUDBigInfoMessage($"Could not set {LocalHealthManager.SelectedActiveNutrientsDepletionPreset}", MessageType.Warning, Color.yellow));
                        }
                    }
                    if (GUILayout.Button("Save settings"))
                    {
                        bool saved =LocalHealthManager.SaveSettings();
                        if (saved)
                        {
                            ShowHUDBigInfo(HUDBigInfoMessage("Saved", MessageType.Info, Color.green));
                        }
                        else
                        {
                            ShowHUDBigInfo(HUDBigInfoMessage($"Could not save settings", MessageType.Warning, Color.yellow));
                        }
                    }
                }
            }
        }

        private void ConditionMulsSettingsBox()
        {
            using (var positionScope = new GUILayout.VerticalScope(GUI.skin.label))
            {
                GUILayout.Label($"Avoid any player condition depletion!", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));
                ConditionParameterLossOption();

                GUILayout.Label($"Choose which condition multipliers to use:", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.cyan));
                GUILayout.Label($"Please note that only custom multipliers can be adjusted, not any default multiplier!", ColoredInfoHeaderLabel(InfoHeaderLabel, Color.yellow));
                ConditionOption();

                using (var ftftftScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    if (GUILayout.Button($"Default multipliers"))
                    {
                        ToggleShowUI(1);
                    }
                    if (ShowDefaultMuls)
                    {
                        DefaultMulsScrollViewBox();
                    }

                    if (GUILayout.Button($"Custom multipliers"))
                    {
                        ToggleShowUI(2);
                    }
                    if (ShowCustomMuls)
                    {
                        CustomMulsScrollViewBox();
                    }
                }

                if (GUILayout.Button("Apply", GUI.skin.button))
                {
                    if (LocalHealthManager.IsParameterLossBlocked)
                    {
                        LocalHealthManager.BlockParametersLoss();
                    }
                    else
                    {
                        LocalHealthManager.UnblockParametersLoss();
                    }
                    ShowHUDBigInfo(HUDBigInfoMessage($"Parameter loss has been {(LocalHealthManager.GetParameterLossBlocked() ? "enabled" : "disabled")} ", MessageType.Info, Color.green));

                    LocalHealthManager.UpdateNutrition(LocalHealthManager.UseDefault);
                    ShowHUDBigInfo(HUDBigInfoMessage($"Using {(LocalHealthManager.UseDefault ? "default multipliers" : "custom multipliers")} ", MessageType.Info, Color.green));
                }
            }
        }

        private void ConditionOption()
        {
            try
            {
                LocalHealthManager.UseDefault = GUILayout.Toggle(LocalHealthManager.UseDefault, $"Switch between default (= on) or custom (= off) nutrition multipliers.", GUI.skin.toggle);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ConditionOption));
            }
        }

        private void ConditionParameterLossOption()
        {
            try
            {
                LocalHealthManager.IsParameterLossBlocked = GUILayout.Toggle(LocalHealthManager.IsParameterLossBlocked, $"Switch parameter loss on / off.", GUI.skin.toggle);
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ConditionParameterLossOption));
            }
        }

        private void CustomMulsScrollViewBox()
        {
            using (var custommulsslidersscope = new GUILayout.VerticalScope(GUI.skin.box))
            {             
                CustomMulsScrollView();
            }
        }

        private void CustomMulsScrollView()
        {
            CustomMulsScrollViewPosition = GUILayout.BeginScrollView(CustomMulsScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(250f));
            LocalHealthManager.GetCustomMultiplierSliders();
            GUILayout.EndScrollView();
        }

        private void DefaultMulsScrollViewBox()
        {
            using (var defslidersscope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                DefaultMulsScrollView();
            }
        }

        private void DefaultMulsScrollView()
        {
            DefaultMulsScrollViewPosition = GUILayout.BeginScrollView(DefaultMulsScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(250f));
            LocalHealthManager.GetDefaultMultiplierSliders();
            GUILayout.EndScrollView();
        }

        private void OnClickSetTimeScalesButton()
        {
            try
            {
                bool ok = LocalTimeManager.SetTimeScalesInMinutes(Convert.ToInt32(LocalTimeManager.DayTimeScaleInMinutes), Convert.ToInt32(LocalTimeManager.NightTimeScaleInMinutes));
                if (ok)
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(TimeScalesSetMessage(LocalTimeManager.DayTimeScaleInMinutes, LocalTimeManager.NightTimeScaleInMinutes), MessageType.Info, Color.green));
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input {LocalTimeManager.DayTimeScaleInMinutes} and {LocalTimeManager.NightTimeScaleInMinutes}:\nPlease input numbers only - min. 0.1", MessageType.Warning, Color.yellow));
                }            
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickSetTimeScalesButton));
            }
        }

        private void OnClickFastForwardDayCycleButton()
        {
            try
            {
                string daytime = LocalTimeManager.SetToNextDayCycle();
                if (!string.IsNullOrEmpty(daytime))
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(DayCycleSetMessage(daytime), MessageType.Info, Color.green));
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Could not set day cycle!", MessageType.Warning, Color.yellow));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickFastForwardDayCycleButton));
            }
        }

    }
}
