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

        private static float ModTimeScreenTotalWidth { get; set; } = 800f;
        private static float ModTimeScreenTotalHeight { get; set; } = 500f;      
        private static float ModTimeScreenMinWidth { get; set; } = 800f;
        private static float ModTimeScreenMaxWidth { get; set; } = Screen.width;
        private static float ModTimeScreenMinHeight { get; set; } = 50f;
        private static  float ModTimeScreenMaxHeight { get; set; } = Screen.height;
        private static float ModTimeScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModTimeScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsModTimeMinimized { get; set; } = false;
        private static int ModTimeScreenId { get; set; }

        private static float HUDTimeScreenTotalWidth { get; set; } = 150f;
        private static float HUDTimeScreenTotalHeight { get; set; } = 150f;
        private static float HUDTimeScreenMinWidth { get; set; } = 150f;
        private static float HUDTimeScreenMinHeight { get; set; } = 50f;
        private static float HUDTimeScreenMaxWidth { get; set; } = 150f;
        private static float HUDTimeScreenMaxHeight { get; set; } = 150f;
        private static float HUDTimeScreenStartPositionX { get; set; } = 0f;
        private static float HUDTimeScreenStartPositionY { get; set; } = Screen.height - HUDTimeScreenTotalHeight - 75f;
        private static bool IsHUDTimeMinimized { get; set; }
        private static int HUDTimeScreenId { get; set; }

        private Color DefaultColor = GUI.color;
        private Color DefaultContentColor = GUI.contentColor;
        private Color DefaultBackGroundColor = GUI.backgroundColor;
        private GUIStyle HeaderLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            fontSize = 16
        };
        private GUIStyle SubHeaderLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = HeaderLabel.alignment,
            fontStyle = HeaderLabel.fontStyle,
            fontSize = HeaderLabel.fontSize - 2,
        };
        private GUIStyle FormFieldNameLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12,
            stretchWidth = true            
        };
        private GUIStyle FormFieldValueLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12,
            stretchWidth = true
        };
        private GUIStyle FormInputTextField => new GUIStyle(GUI.skin.textField)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 12,
            stretchWidth = true,
            stretchHeight = true,
            wordWrap = true
        };
        private GUIStyle CommentLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Italic,
            fontSize = 12,
            stretchWidth = true,         
            wordWrap = true
        };
        private GUIStyle TextLabel => new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize =12,
            stretchWidth = true,
            wordWrap = true
        };
        private GUIStyle ToggleButton => new GUIStyle(GUI.skin.toggle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,            
            stretchWidth = true
        };

        public GUIStyle ColoredToggleValueTextLabel(bool enabled, Color enabledColor, Color disabledColor)
        {
            GUIStyle style = TextLabel;
            style.normal.textColor = enabled ? enabledColor : disabledColor;
            return style;
        }

        public GUIStyle ColoredToggleButton(bool activated, Color enabledColor, Color disabledColor)
        {
            GUIStyle style = ToggleButton;
            style.active.textColor = activated ? enabledColor : disabledColor;
            style.onActive.textColor = activated ? enabledColor : disabledColor;
            style = GUI.skin.button;
            return style;
        }

        public GUIStyle ColoredCommentLabel(Color color)
        {
            GUIStyle style = CommentLabel;
            style.normal.textColor = color;
            return style;
        }

        public GUIStyle ColoredFieldNameLabel(Color color)
        {
            GUIStyle style = FormFieldNameLabel;
            style.normal.textColor = color;
            return style;
        }

        public GUIStyle ColoredFieldValueLabel(Color color)
        {
            GUIStyle style = FormFieldValueLabel;
            style.normal.textColor = color;
            return style;
        }

        public GUIStyle ColoredToggleFieldValueLabel(bool enabled, Color enabledColor, Color disabledColor)
        {
            GUIStyle style = FormFieldValueLabel;
            style.normal.textColor = enabled ? enabledColor : disabledColor;
            return style;
        }       

        public GUIStyle ColoredHeaderLabel(Color color)
        {
            GUIStyle style = HeaderLabel;
            style.normal.textColor = color;
            return style;
        }

        public GUIStyle ColoredSubHeaderLabel(Color color)
        {
            GUIStyle style = SubHeaderLabel;
            style.normal.textColor = color;
            return style;
        }

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

        public KeyCode GetShortcutKey(string buttonID)
        {
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

        protected virtual void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ShortcutKey = GetShortcutKey(nameof(ShortcutKey));
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
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

        public void ShowHUDBigInfo(string text, float duration = 3f)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();
            HUDBigInfo obj = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = duration;
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
            var messages = ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages)));
            messages.AddMessage($"{localization.Get(localizedTextKey)}  {localization.Get(itemID)}");
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

        private void ToggleShowUI(int controlId)
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
                    ShowHUDTime = !ShowHUDTime;
                  return;
            }          
        }

        private void OnGUI()
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

        private void ShowModTimeWindow()
        {
            if (ModTimeScreenId < 0 || ModTimeScreenId == HUDTimeScreenId)
            {
                ModTimeScreenId = GetHashCode();
            }
            string modTimeScreenTitle = $"{ModName} created by [Dragon Legion] Immaanuel#4300";
            ModTimeScreen = GUILayout.Window(ModTimeScreenId, ModTimeScreen, InitModTimeScreen, modTimeScreenTitle, GUI.skin.window, GUILayout.ExpandWidth(true), GUILayout.MinWidth(ModTimeScreenMinWidth), GUILayout.MaxWidth(ModTimeScreenMaxWidth), GUILayout.ExpandHeight(true), GUILayout.MinHeight(ModTimeScreenMinHeight), GUILayout.MaxHeight(ModTimeScreenMaxHeight));
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
            ShowModTimeWindow();
        }

        private void ShowHUDTimeWindow()
        {
            if (HUDTimeScreenId < 0 || HUDTimeScreenId == ModTimeScreenId)
            {
                HUDTimeScreenId = GetHashCode() + 1;
            }
            string hudTimeScreenTitle = $"HUD Time";
            HUDTimeScreen = GUILayout.Window(HUDTimeScreenId, HUDTimeScreen, InitHUDTimeScreen, hudTimeScreenTitle, GUI.skin.window, GUILayout.ExpandWidth(true), GUILayout.MinWidth(HUDTimeScreenMinWidth), GUILayout.MaxWidth(HUDTimeScreenMaxWidth), GUILayout.ExpandHeight(true), GUILayout.MinHeight(HUDTimeScreenMinHeight), GUILayout.MaxHeight(HUDTimeScreenMaxHeight));
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
            ShowHUDTimeWindow();
        }

        private void CloseWindow()
        {
            ShowModTime = false;
            ShowHUDTime = false;
            EnableCursor(false);
        }

        private void InitHUDTimeScreen(int windowID)
        {
            HUDTimeScreenStartPositionX = HUDTimeScreen.x;
            HUDTimeScreenStartPositionY = HUDTimeScreen.y;
            HUDTimeScreenTotalWidth = HUDTimeScreen.width;

            GUI.backgroundColor = Color.clear;
            
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {              
                HUDTimeMenuBox();

                if (!IsHUDTimeMinimized)
                {
                    HUDTimeViewBox();
                }
            }

            GUI.backgroundColor = DefaultBackGroundColor;

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void HUDTimeViewBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.label))
            {
                GUIContent timeContent = new GUIContent($"{LocalTimeManager.HUDTimeString()}.");
                GUIContent dateContent = new GUIContent($"{LocalTimeManager.HUDDateString()}.");
                GUILayout.Label(timeContent, ColoredHeaderLabel(Color.yellow));
                GUILayout.Label(dateContent, ColoredSubHeaderLabel(Color.white));
            }                
        }

        private void HUDTimeMenuBox()
        {
            string CollapseButtonText = IsHUDTimeMinimized ? "O" : "-";

            if (GUI.Button(new Rect(HUDTimeScreen.width - 40f, 0f, 20f, 20f), CollapseButtonText, GUI.skin.button))
            {
                CollapseHUDTimeWindow();
            }
            if (GUI.Button(new Rect(HUDTimeScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void InitModTimeScreen(int windowID)
        {
            ModTimeScreenStartPositionX = ModTimeScreen.x;
            ModTimeScreenStartPositionY = ModTimeScreen.y;
            ModTimeScreenTotalWidth = ModTimeScreen.width;

            GUI.backgroundColor = DefaultBackGroundColor;
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
               ScreenMenuBox();

                if (!IsModTimeMinimized)
                {
                    ModTimeManagerBox();
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
                GUILayout.Label($"Health Manager", ColoredHeaderLabel(Color.yellow));

                GUILayout.Label($"Health Options", ColoredSubHeaderLabel(Color.yellow));

                NutrientsSettingsBox();
                ConditionMultipliersBox();                
            }
            else
            {
                using (var enablehmmulboxscope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To use, please enable health manager in the options above.", ColoredCommentLabel(Color.yellow));
                }
            }
        }

        private void TimeManagerBox()
        {
            if (LocalTimeManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Time Manager", ColoredHeaderLabel(Color.yellow));

                    GUILayout.Label($"Time Options", ColoredSubHeaderLabel(Color.yellow));

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
                    GUILayout.Label($"To use, please enable time manager in the options above.", ColoredCommentLabel(Color.yellow));
                }
            }
        }

        private void WeatherManagerBox()
        {
            if (LocalWeatherManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Weather Manager", ColoredHeaderLabel(Color.yellow));
                    
                    GUILayout.Label($"Weather Options", ColoredSubHeaderLabel(Color.yellow));

                    RainOption();
                }
            }
            else
            {
                using (var enablelweatherboxscope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To use, please enable weather manager in the options above.", ColoredCommentLabel(Color.yellow));                    
                }
            }
        }

        private void ModTimeManagerBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var modOptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{ModName} Manager", ColoredHeaderLabel(Color.yellow));

                    GUILayout.Label($"{ModName} Options", ColoredSubHeaderLabel(Color.yellow));

                    using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        if (GUILayout.Button($"Mod Info", GUI.skin.button))
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
                ModInfoScrollViewPosition = GUILayout.BeginScrollView(ModInfoScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));

                GUILayout.Label("Mod Info", ColoredSubHeaderLabel(Color.cyan));

                using (var gidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.GameID)}:", FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.GameID}", FormFieldValueLabel);
                }
                using (var midScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.ID)}:", FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.ID}", FormFieldValueLabel);
                }
                using (var uidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.UniqueID)}:", FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.UniqueID}", FormFieldValueLabel);
                }
                using (var versionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(IConfigurableMod.Version)}:", FormFieldNameLabel);
                    GUILayout.Label($"{SelectedMod.Version}", FormFieldValueLabel);
                }

                GUILayout.Label("Buttons Info", ColoredSubHeaderLabel(Color.cyan));

                foreach (var configurableModButton in SelectedMod.ConfigurableModButtons)
                {
                    using (var btnidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.ID)}:", FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.ID}", FormFieldValueLabel);
                    }
                    using (var btnbindScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"{nameof(IConfigurableModButton.KeyBinding)}:", FormFieldNameLabel);
                        GUILayout.Label($"{configurableModButton.KeyBinding}", FormFieldValueLabel);
                    }
                }

                GUILayout.EndScrollView();
            }
        }

        private void MultiplayerOptionBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    string multiplayerOptionMessage = string.Empty;
                    GUILayout.Label("Multiplayer Info", ColoredSubHeaderLabel(Color.cyan));
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
                        GUILayout.Label($"{PermissionChangedMessage($"granted", multiplayerOptionMessage)}", ColoredToggleValueTextLabel(true, Color.green, Color.yellow));
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
                        GUILayout.Label($"{PermissionChangedMessage($"revoked", multiplayerOptionMessage)}", ColoredToggleValueTextLabel(false, Color.green, Color.yellow));
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
                if (LocalWeatherManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
                {
                    using (new GUILayout.VerticalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Weather Info", ColoredSubHeaderLabel(Color.cyan));
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"Current weather: ", ColoredFieldNameLabel(Color.cyan));
                            GUILayout.Label(LocalWeatherManager.GetCurrentWeatherInfo(), ColoredFieldValueLabel(Color.cyan));
                        }

                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"To change the weather, click ", TextLabel);
                            bool _isRainEnabled = LocalWeatherManager.IsRainEnabled;
                            LocalWeatherManager.IsRainEnabled = GUILayout.Toggle(LocalWeatherManager.IsRainEnabled, $"Switch weather", ColoredToggleButton(LocalWeatherManager.IsRainEnabled,Color.green,DefaultColor), GUILayout.ExpandWidth(true));
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

        private void ShowHUDTimeOptionBox()
        {
            try
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("HUD Time Info", ColoredSubHeaderLabel(Color.cyan));
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current setting: ", ColoredFieldNameLabel(Color.cyan));
                        GUILayout.Label($"HUD Time {(LocalTimeManager.IsHUDTimeEnabled ? "visible" : "hidden")}", ColoredToggleFieldValueLabel(LocalTimeManager.IsHUDTimeEnabled, Color.cyan, Color.cyan));
                    }

                    GUILayout.Label($"Show or hide the time HUD using this setting.", TextLabel);

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

        private void WeatherManagerOption()
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
            if (LocalTimeManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (var timeofdayBoxScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current time of day setting: ", ColoredFieldNameLabel(Color.cyan));
                        GUILayout.Label($"{(LocalTimeManager.IsNight() ? "night time" : "daytime")}", ColoredFieldValueLabel(Color.cyan));
                    }

                    GUILayout.Label("Please note that the time skipped has an impact on player condition! Enable health manager for more info.", ColoredCommentLabel(Color.yellow));

                    GUILayout.Label("Go fast forward to the next daytime or night time cycle:",TextLabel);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"To set game time to {( LocalTimeManager.IsNight( ) ? "daytime" : "night time")}, click", TextLabel);
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

        private void DayTimeScalesBox()
        {
            if (LocalTimeManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (var timescalesScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current daytime length in minutes: ", ColoredFieldNameLabel(Color.cyan));
                        GUILayout.Label($"{LocalTimeManager.DayTimeScaleInMinutes}", ColoredFieldValueLabel(Color.cyan));
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current night time length in minutes: ", ColoredFieldNameLabel(Color.cyan));
                        GUILayout.Label($"{LocalTimeManager.NightTimeScaleInMinutes}", ColoredFieldValueLabel(Color.cyan));
                    }

                    GUILayout.Label("The scaling is based on 24 hours =  720 minutes daytime +  720 minutes night time = real-time", ColoredCommentLabel(Color.yellow));

                    GUILayout.Label("Change scales for in-game day - and night time length in real-life minutes.", TextLabel);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Daytime length: ", TextLabel);
                        LocalTimeManager.DayTimeScaleInMinutes = GUILayout.TextField(LocalTimeManager.DayTimeScaleInMinutes, FormInputTextField);
                        GUILayout.Label("Night time length: ", TextLabel);
                        LocalTimeManager.NightTimeScaleInMinutes = GUILayout.TextField(LocalTimeManager.NightTimeScaleInMinutes, FormInputTextField);
                        if (GUILayout.Button("Apply", GUI.skin.button, GUILayout.Width(150f)))
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
                        GUILayout.Label($"Current time progress speed:", ColoredFieldNameLabel(Color.cyan));
                        GUILayout.Label($"{LocalTimeManager.GetTimeProgressSpeed()}", ColoredFieldValueLabel(Color.cyan));
                    }

                    GUILayout.Label($"Time progress is calculated using the set time scale mode's factor, multiplied by the set slowmotion factor.", TextLabel);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Set time scale {LocalTimeManager.SelectedTimeScaleMode} factor:", ColoredFieldNameLabel(Color.cyan));
                        GUILayout.Label($"{LocalTimeManager.GetTimeScaleFactor(LocalTimeManager.SelectedTimeScaleMode)}", ColoredFieldValueLabel(Color.cyan));
                    }
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Set slowmotion factor:", ColoredFieldNameLabel(Color.cyan));
                        GUILayout.Label($"{LocalTimeManager.SlowMotionFactor}", ColoredFieldValueLabel(Color.cyan));
                    }
                   
                    GUILayout.Label("Choose a time scale mode: ", TextLabel);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        LocalTimeManager.SelectedTimeScaleModeIndex = GUILayout.SelectionGrid(LocalTimeManager.SelectedTimeScaleModeIndex, timeScaleModes, timeScaleModes.Length, GUI.skin.button);
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
                    if (LocalTimeManager.SelectedTimeScaleMode == TimeScaleModes.Custom)
                    {
                        GUILayout.Label($"Set a  custom slowmotion factor. Click [Apply]", TextLabel);
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            string _slowmotionValue = $"Slowmotion factor";
                            GUILayout.Label($"{_slowmotionValue} ({(float)Math.Round(LocalTimeManager.SlowMotionFactor, 2, MidpointRounding.ToEven)})");
                            LocalTimeManager.SlowMotionFactor = GUILayout.HorizontalSlider(LocalTimeManager.SlowMotionFactor, 0f, 1f);
                        }
                        if (GUILayout.Button("Apply", GUI.skin.button, GUILayout.Width(150f)))
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
            if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
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
            if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                string[] depletionPresets = LocalHealthManager.GetNutrientsDepletionNames();
                string _activeNutrientsDepletionPreset = depletionPresets[LocalHealthManager.ActiveNutrientsDepletionPresetIndex];
                LocalHealthManager.ActiveNutrientsDepletionPreset = EnumUtils<NutrientsDepletion>.GetValue(_activeNutrientsDepletionPreset);               

                using (new GUILayout.VerticalScope(GUI.skin.label))
                {
                   
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Current setting: ", ColoredFieldNameLabel(Color.cyan));
                        GUILayout.Label($"{LocalHealthManager.ActiveNutrientsDepletionPreset}", ColoredFieldValueLabel(Color.cyan));
                    }

                    GUILayout.Label("Each preset is an in-game defined preset that by default can be set only once for a game session in your game difficulty settings.", TextLabel);

                    GUILayout.Label($"Choose a nutrients depletion preset.", TextLabel);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        int _selectedActiveNutrientsDepletionPresetIndex = LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex;
                        string _selectedActiveNutrientsDepletionPreset = depletionPresets[LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex];
                        LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex = GUILayout.SelectionGrid(LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex, depletionPresets, depletionPresets.Length, GUI.skin.button);
                        if (_selectedActiveNutrientsDepletionPresetIndex != LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex)
                        {
                            _selectedActiveNutrientsDepletionPreset = depletionPresets[LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex];                         
                        }
                        if (GUILayout.Button("Apply", GUI.skin.button, GUILayout.Width(150f)))
                        {
                            bool ok = LocalHealthManager.SetActiveNutrientsDepletionPreset(LocalHealthManager.SelectedActiveNutrientsDepletionPresetIndex);
                            if (ok)
                            {
                                string activeDepletionSetToMessage = $"Current active nutrients depletion preset: {_selectedActiveNutrientsDepletionPreset}";
                                ShowHUDBigInfo(HUDBigInfoMessage(activeDepletionSetToMessage, MessageType.Info, Color.green));
                            }
                            else
                            {
                                ShowHUDBigInfo(HUDBigInfoMessage($"Could not set {LocalHealthManager.SelectedActiveNutrientsDepletionPreset}", MessageType.Warning, Color.yellow));
                            }
                        }
                    }
                }
            }            
        }

        private void ConditionMulsSettingsBox()
        {
            if (LocalHealthManager.IsModEnabled && (IsModActiveForSingleplayer || IsModActiveForMultiplayer))
            {
                using (new GUILayout.VerticalScope(GUI.skin.label))
                {
                    CheatModeOptionBox();
                    if (!Cheats.m_GodMode)
                    {
                        ConditionParameterLossOptionBox();

                        GUILayout.Label($"Please note that only custom multipliers can be adjusted, not any default multiplier!", ColoredCommentLabel(Color.yellow));

                        ConditionOptionBox();

                        using (new GUILayout.VerticalScope(GUI.skin.box))
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

                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
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
                    else
                    {
                        GUILayout.Label($"Not available when in God mode.", ColoredCommentLabel(Color.yellow));
                    }
                }
            }
        }

        private void CheatModeOptionBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.label))
            {
                GUILayout.Label($"Avoid any player condition depletion!", TextLabel);

                CheatConditionOption();
            }
        }

        private void ConditionOptionBox()
        {
            try
            {
                using (new GUILayout.HorizontalScope(GUI.skin.box)) 
                {
                    GUILayout.Label($"To change which nutrition multipliers to use, click ", TextLabel);
                    LocalHealthManager.UseDefault = GUILayout.Toggle(LocalHealthManager.UseDefault, $"Switch to {(LocalHealthManager.UseDefault ? "custom" : "default" )} multipliers", ColoredToggleButton(LocalHealthManager.UseDefault, Color.green, DefaultColor), GUILayout.ExpandWidth(true));
                } 
                
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ConditionOptionBox));
            }
        }

        private void ConditionParameterLossOptionBox()
        {
            try
            {
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To change parameter loss, click ", TextLabel);
                    LocalHealthManager.IsParameterLossBlocked = GUILayout.Toggle(LocalHealthManager.IsParameterLossBlocked, $"Switch parameter loss {(LocalHealthManager.IsParameterLossBlocked ? "off" : "on")}", ColoredToggleButton(LocalHealthManager.IsParameterLossBlocked, Color.green, DefaultColor), GUILayout.ExpandWidth(true));
                }                    
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ConditionParameterLossOptionBox));
            }
        }

        private void CheatConditionOption()
        {
            try
            {
                using (new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To change cheat God mode, click ", TextLabel);
                    Cheats.m_GodMode = GUILayout.Toggle(Cheats.m_GodMode, $"Switch cheat {(Cheats.m_GodMode ? "off" : "on")}", ColoredToggleButton(Cheats.m_GodMode, Color.green, DefaultColor), GUILayout.ExpandWidth(true));
                }                   
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(CheatConditionOption));
            }
        }

        private void CustomMulsScrollViewBox()
        {
            using (new GUILayout.VerticalScope(GUI.skin.box))
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
