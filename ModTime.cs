﻿using ModManager;
using ModTime.Data;
using ModTime.Data.Enums;
using ModTime.Managers;
using System;
using System.Collections.Generic;
using System.IO;
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

        private static readonly string ModName = nameof(ModTime);
        private static float ModScreenTotalWidth { get; set; } = 500f;
        private static float ModScreenTotalHeight { get; set; } = 450f;
        private static float ModScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static readonly float ModScreenMinWidth = 500f;
        private static readonly float ModScreenMaxWidth = Screen.width;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = Screen.height;       

        private Color DefaultGuiColor = GUI.color;
        private Color DefaultContentColor = GUI.contentColor;
        private Color DefaultBackGroundColor = GUI.backgroundColor;

        private bool ShowUI { get; set; } = false;
        private bool ShowDefaultMuls { get; set; } = false;
        private bool ShowCustomMuls { get; set; } = false;
        private bool ShowModInfo { get; set; } = false;
        private bool IsMinimized { get; set; } = false;

        private static Rect ModTimeScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);        
        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");

        private static Rect TimeHUDScreen = new Rect(0f, 0f, 100f, 100f);

        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static WeatherManager LocalWeatherManager;
        private static HealthManager LocalHealthManager;
        private static TimeManager LocalTimeManager;
              
        public KeyCode ShortcutKey { get; set; } = KeyCode.Keypad2;
        public bool IsModActiveForMultiplayer { get; private set; } = P2PSession.Instance.AmIMaster();
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();
      
        public Vector2 DefaultMulsScrollViewPosition { get; private set; }
        public Vector2 CustomMulsScrollViewPosition { get; private set; }
        public bool ShowTimeHUD { get; private set; }
        

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
                GUI.color = DefaultGuiColor;
            }
        }

        private KeyCode GetConfigurableModShortcutKey(string buttonId)
        {
            KeyCode result = KeyCode.None;
            string value = string.Empty;
            try
            {
                if (File.Exists(RuntimeConfigurationFile))
                {
                    using (XmlReader xmlReader = XmlReader.Create(new StreamReader(RuntimeConfigurationFile)))
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

        private void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ShortcutKey = GetConfigurableModShortcutKey(nameof(ShortcutKey));
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
                if (!ShowUI)
                {
                    InitData();
                    EnableCursor(blockPlayer: true);
                }
                ToggleShowUI(0);
                if (!ShowUI)
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
                    ShowUI = !ShowUI;
                    break;
                case 1:
                    ShowDefaultMuls = !ShowDefaultMuls;
                    break;
                case 2:
                    ShowCustomMuls = !ShowCustomMuls;
                    break;
                case 3:
                    ShowModInfo = !ShowModInfo;
                    break;
                default:
                    ShowUI = !ShowUI;
                    ShowDefaultMuls = !ShowDefaultMuls;
                    ShowCustomMuls = !ShowCustomMuls;
                    ShowModInfo = !ShowModInfo;
                    break;
            }
        }

        private void OnGUI()
        {
            if (ShowUI)
            {
                InitData();
                InitSkinUI();
                InitWindow();
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

        private void InitWindow()
        {
            if (ShowUI)
            {
                int WindowId = base.GetHashCode();
                string WindowTitle = $"{ModName} created by [Dragon Legion] Immaanuel#4300";
                ModTimeScreen = GUILayout.Window(WindowId, ModTimeScreen, InitModTimeScreen, WindowTitle, GUI.skin.window, GUILayout.ExpandWidth(true), GUILayout.MinWidth(ModScreenMinWidth), GUILayout.MaxWidth(ModScreenMaxWidth), GUILayout.ExpandHeight(true), GUILayout.MinHeight(ModScreenMinHeight), GUILayout.MaxHeight(ModScreenMaxHeight));
            }
        }

        private void ScreenMenuBox()
        {
            string CollapseButtonText;
            if (IsMinimized)
            {
                CollapseButtonText = "O";
            }
            else
            {
                CollapseButtonText = "-";
            }

            if (GUI.Button(new Rect(ModTimeScreen.width - 40f, 0f, 20f, 20f), CollapseButtonText, GUI.skin.button))
            {
                CollapseWindow();
            }
            if (GUI.Button(new Rect(ModTimeScreen.width - 20f, 0f, 20f, 20f), "X", GUI.skin.button))
            {
                CloseWindow();
            }
        }

        private void CollapseWindow()
        {
            ModScreenStartPositionX = ModTimeScreen.x;
            ModScreenStartPositionY = ModTimeScreen.y;
            ModScreenTotalWidth = ModTimeScreen.width;          

            if (!IsMinimized)
            {
                ModTimeScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenMinHeight);
                IsMinimized = true;
            }
            else
            {
                ModTimeScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
                IsMinimized = false;
            }
            InitWindow();
        }

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor();
        }

        private void InitModTimeScreen(int windowID)
        {
            ModScreenStartPositionX = ModTimeScreen.x;
            ModScreenStartPositionY = ModTimeScreen.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
               ScreenMenuBox();

                if (!IsMinimized)
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
                GUI.color = Color.yellow;
                GUILayout.Label($"Health manager", GUI.skin.label);
                GUI.color = DefaultGuiColor;

                ConditionMultipliersBox();
            }
            else
            {
                using (var enablehmmulboxscope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"To use, please enable health manager in the options above.", GUI.skin.label);
                    GUI.color = DefaultGuiColor;
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
                    GUI.color = DefaultGuiColor;

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
                    GUI.color = DefaultGuiColor;
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
                    GUI.color = DefaultGuiColor;

                    RainOption();
                }
            }
            else
            {
                using (var enablelweatherboxscope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"Enable weather manager in the {ModName} options.", GUI.skin.label);
                    GUI.color = DefaultGuiColor;
                }
            }
        }

        private void ModOptionsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var modOptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.cyan;
                    GUILayout.Label($"{ModName} options:", GUI.skin.label);
                    GUI.color = DefaultGuiColor;

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
                using (var gidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.GameID)}:", GUI.skin.label);
                    GUILayout.Label($"{GameID.GreenHell}", GUI.skin.label);
                }
                using (var midScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.ID)}:", GUI.skin.label);
                    GUILayout.Label($"{ModName}", GUI.skin.label);
                }
                using (var uidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.UniqueID)}:", GUI.skin.label);
                    GUILayout.Label($"", GUI.skin.label);
                }
                using (var versionScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableMod.Version)}:", GUI.skin.label);
                    GUILayout.Label($"", GUI.skin.label);
                }
                GUI.color = Color.cyan;
                GUILayout.Label("Buttons: ", GUI.skin.label);
                GUI.color = DefaultGuiColor;
                using (var btnidScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableModButton.ID)}:", GUI.skin.label);
                    GUILayout.Label($"{nameof(ShortcutKey)}", GUI.skin.label);
                }
                using (var btnbindScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label($"{nameof(ConfigurableModButton.KeyBinding)}:", GUI.skin.label);
                    GUILayout.Label($"{ShortcutKey}", GUI.skin.label);
                }
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
                    GUI.color = DefaultGuiColor;
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
                        GUI.color = DefaultGuiColor;
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
                        GUI.color = DefaultGuiColor;
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
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Time options:", GUI.skin.label);
                    GUI.color = DefaultGuiColor;

                    bool _showTimeHUD = ShowTimeHUD;
                    ShowTimeHUD = GUILayout.Toggle(ShowTimeHUD, $"Show time HUD?", GUI.skin.toggle);
                    if (_showTimeHUD != ShowTimeHUD)
                    {
                        TimeHUD();
                    }
                }

            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(WatchOptionBox));
            }
        }

        private void TimeHUD()
        {
            GUIContent timeWatch = new GUIContent($"{LocalTimeManager.GetCurrentTime()}.");
            GUIContent dateWatch = new GUIContent($"{LocalTimeManager.GetCurrentDate()}.");
            GUI.backgroundColor = Color.grey;
            GUI.contentColor = Color.green;
            GUI.Label(new Rect(TimeHUDScreen.x,TimeHUDScreen.y, TimeHUDScreen.width, TimeHUDScreen.height/2f), timeWatch.text, GUI.skin.label);
            GUI.Label(new Rect(TimeHUDScreen.x, TimeHUDScreen.y+50f, TimeHUDScreen.width, TimeHUDScreen.height/2f), dateWatch.text, GUI.skin.label);

            GUI.backgroundColor = DefaultBackGroundColor;
            GUI.contentColor = DefaultContentColor;
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
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var timeofdayBoxScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Current time of day: {(LocalTimeManager.IsNight() ? "night time" : "daytime")}", GUI.skin.label);
                    GUI.color = Color.yellow;
                    GUILayout.Label("Please note that the time skipped has an impact on player condition! Enable health manager for more info.", GUI.skin.label);
                    GUI.color = DefaultGuiColor;
                    GUILayout.Label("Go fast forward to the next daytime or night time cycle:", GUI.skin.label);
                    using (var actbutScopeView = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"To set game time to {( LocalTimeManager.IsNight( ) ? "daytime" : "night time")}, click", GUI.skin.label);
                        if (GUILayout.Button("FFW >>", GUI.skin.button))
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
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Current daytime length: {LocalTimeManager.DayTimeScaleInMinutes} and night time length: {LocalTimeManager.NightTimeScaleInMinutes} ", GUI.skin.label);
                    GUI.color = DefaultGuiColor;

                    GUILayout.Label("Change scales for in-game day - and night time length in real-life minutes. To set, click [Apply]", GUI.skin.label);
                    using (var timescalesInputScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Daytime length: ", GUI.skin.label);
                        LocalTimeManager.DayTimeScaleInMinutes = GUILayout.TextField(LocalTimeManager.DayTimeScaleInMinutes, GUI.skin.textField);
                        GUILayout.Label("Night time length: ", GUI.skin.label);
                        LocalTimeManager.NightTimeScaleInMinutes = GUILayout.TextField(LocalTimeManager.NightTimeScaleInMinutes, GUI.skin.textField);
                        if (GUILayout.Button("Apply", GUI.skin.button))
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
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Current time progress speed = {LocalTimeManager.GetTimeProgressSpeed()} =\n " +
                        $"Time scale factor * slowmotion factor =\n " +
                        $"{LocalTimeManager.GetTimeScaleFactor(LocalTimeManager.TimeScaleMode)} * {LocalTimeManager.GetSlowMotionFactor()}  ", GUI.skin.label);
                    GUI.color = DefaultGuiColor;

                    string[] timeScaleModes = LocalTimeManager.GetTimeScaleModes();
                    int _SelectedTimeScaleModeIndex = LocalTimeManager.SelectedTimeScaleModeIndex;

                    GUILayout.Label("Choose a time scale mode. To set, click [Apply]", GUI.skin.label);
                    using (var timemodeInputScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        LocalTimeManager.SelectedTimeScaleModeIndex = GUILayout.SelectionGrid(LocalTimeManager.SelectedTimeScaleModeIndex, timeScaleModes, timeScaleModes.Length, GUI.skin.button);
                        if (_SelectedTimeScaleModeIndex != LocalTimeManager.SelectedTimeScaleModeIndex)
                        {
                            LocalTimeManager.TimeScaleMode = EnumUtils<TimeScaleModes>.GetValue(timeScaleModes[LocalTimeManager.SelectedTimeScaleModeIndex]);                           
                        }
                        if (GUILayout.Button("Apply", GUI.skin.button))
                        {
                            LocalTimeManager.SetTimeScaleMode(LocalTimeManager.SelectedTimeScaleModeIndex);
                        }                       
                    }
                    if (LocalTimeManager.TimeScaleMode == TimeScaleModes.Custom)
                    {
                        GUILayout.Label($"Choose slowmotion factor. To set, click [Apply]", GUI.skin.label);
                        LocalTimeManager.SlowMotionFactor = UIControlManager.CustomHorizontalSlider(LocalTimeManager.SlowMotionFactor, 0f, 1f, "factor");
                        if (GUILayout.Button("Apply", GUI.skin.button))
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
                GUI.color = Color.cyan;
                GUILayout.Label($"Avoid any player condition depletion!", GUI.skin.label);
                GUI.color = DefaultGuiColor;
                ConditionParameterLossOption();

                GUI.color = Color.cyan;
                GUILayout.Label($"Choose which depletion rates to use:", GUI.skin.label);
                GUI.color = DefaultGuiColor;                
                ConditionOption();

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

                    LocalHealthManager.UpdateNutrition(LocalHealthManager.UseDefault);
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
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
            CustomMulsScrollViewPosition = GUILayout.BeginScrollView(CustomMulsScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));
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
            DefaultMulsScrollViewPosition = GUILayout.BeginScrollView(DefaultMulsScrollViewPosition, GUI.skin.scrollView, GUILayout.MinHeight(150f));
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
