using ModTime.Enums;
using ModTime.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModTime
{
    /// <summary>
    /// ModTime is a mod for Green Hell that allows a player to set in-game date and day and night time scales in real time minutes.
    /// Ingame time can be fast forwarded to the next morning 5AM or night 10PM.
    /// It also allows to manipulate weather to make it rain or stop raining.
    /// Press LeftShift+Keypad2 (default) or the key configurable in ModAPI to open the mod screen.
    /// </summary>
    public class ModTime : MonoBehaviour
    {
        private static ModTime Instance;

        private static readonly string ModName = nameof(ModTime);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinWidth = 450f;
        private static readonly float ModScreenMaxWidth = 550f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 200f;
        private bool ShowUI;
        private Color DefaultGuiColor = GUI.color;
        private static Rect ModTimeScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);
        private static float ModScreenStartPositionX { get; set; } = Screen.width / 2f;
        private static float ModScreenStartPositionY { get; set; } = Screen.height / 2f;
        private static bool IsMinimized { get; set; } = false;
        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        private static KeyCode ShortCutKey { get; set; } = KeyCode.Keypad2;

        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static WeatherManager LocalWeatherManager;
        private static HealthManager LocalHealthManager;
        private static TimeManager LocalTimeManager;
       
        public static string DayTimeScaleInMinutes { get; set; } = "20";
        public static string NightTimeScaleInMinutes { get; set; } = "10";
        public static float SlowMotionFactor { get; set; } = 1f;
        public static int SelectedTimeScaleModeIndex { get; set; } = 0;
        public static TimeScaleModes TimeScaleMode { get; set; } = TimeScaleModes.Normal;
        public static bool UseDeviceDateAndTime { get; set; } = false;
        public static bool HasChanged { get; set; } = false;

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();
        public bool IsRainEnabled { get; private set; } = false;
        
        public ModTime()
        {
            useGUILayout = true;
            Instance = this;
        }
        public static ModTime Get()
        {
            return Instance;
        }

        public static string DayCycleSetMessage(string daytime)
            => $"{daytime}";
        public static string TimeScalesSetMessage(string dayTimeScale, string nightTimeScale)
            => $"Time scales set:\nDay time passes in " + dayTimeScale + " realtime minutes\nand night time in " + nightTimeScale + " realtime minutes.";
        public static string OnlyForSinglePlayerOrHostMessage()
            => "Only available for single player or when host. Host can activate using ModManager.";
        public static string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

        private KeyCode GetShortCutKey(string buttonId)
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
                else if (buttonId == nameof(ShortCutKey))
                {
                    result = ShortCutKey;
                }
                return result;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetShortCutKey));
                if (buttonId == nameof(ShortCutKey))
                {
                    result = ShortCutKey;
                }
                return result;
            }
        }

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ShortCutKey = GetShortCutKey(nameof(ShortCutKey));
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
            ModAPI.Log.Write(info);
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
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(ShortCutKey))
            {
                if (!ShowUI)
                {
                    InitData();
                    EnableCursor(blockPlayer: true);
                }
                ToggleShowUI();
                if (!ShowUI)
                {
                    EnableCursor();
                }
            }
            if (TimeScaleMode == TimeScaleModes.Paused)
            {
                LocalTimeManager.Pause(true);
            }
            else
            {
                LocalTimeManager.Pause(false);
            }
        }

        private void ToggleShowUI()
        {
            ShowUI = !ShowUI;
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
            ModTimeScreen = GUILayout.Window(GetHashCode(), ModTimeScreen, InitModTimeScreen, $"{ModName} created by [Dragon Legion] Immaanuel#4300",
                                                GUI.skin.window,
                                                GUILayout.ExpandWidth(true),
                                                GUILayout.MinWidth(ModScreenMinWidth),
                                                GUILayout.MaxWidth(ModScreenMaxWidth),
                                                GUILayout.ExpandHeight(true),
                                                GUILayout.MinHeight(ModScreenMinHeight),
                                                GUILayout.MaxHeight(ModScreenMaxHeight));
        }

        private void ScreenMenuBox()
        {
            if (GUI.Button(new Rect(ModTimeScreen.width - 40f, 0f, 20f, 20f), "-", GUI.skin.button))
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
                    DayTimeScalesBox();
                    DayCycleBox();
                    CustomTimeScaleBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ModOptionsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To toggle {ModName} main UI, press [{ShortCutKey}]", GUI.skin.label);
                    MultiplayerOptionBox();
                    WeatherOptionBox();
                    GameTimeOptionBox();
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void MultiplayerOptionBox()
        {
            try
            {
                using (var multiplayeroptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Multiplayer options: ", GUI.skin.label);
                    string multiplayerOptionMessage = string.Empty;
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
                    }
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(MultiplayerOptionBox));
            }
        }

        private void WeatherOptionBox()
        {
            try
            {
                using (var weatheroptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"Weather options: ", GUI.skin.label);
                    RainOption();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(WeatherOptionBox));
            }
        }

        private void RainOption()
        {
            try
            {
                bool _tRainEnabled = IsRainEnabled;
                IsRainEnabled = GUILayout.Toggle(IsRainEnabled, $"Switch between raining or dry weather", GUI.skin.toggle);
                if (_tRainEnabled != IsRainEnabled)
                {
                    if (IsRainEnabled)
                    {
                        LocalWeatherManager.StartRain();
                    }
                    else
                    {
                        LocalWeatherManager.StopRain();
                    }                   
                    //string rainOptionMessage = $"Rain { (IsRainEnabled ? "is falling" : "has stopped") }";
                    //ShowHUDBigInfo(HUDBigInfoMessage(rainOptionMessage, MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(RainOption));
            }
        }

        private void GameTimeOptionBox()
        {
            try
            {
                using (var weatheroptionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Current time is set {(UseDeviceDateAndTime ? "using device date and time." : "using game date and time." )}.", GUI.skin.label);
                    GUILayout.Label($"It is now {LocalTimeManager.GetCurrentDateAndTime()}.", GUI.skin.label);
                    GUI.color = DefaultGuiColor;
                    GUILayout.Label($"Game time options: ", GUI.skin.label);
                    UseDeviceOption();
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(WeatherOptionBox));
            }
        }

        private void UseDeviceOption()
        {
            try
            {
                bool _UseDeviceDateAndTime =  UseDeviceDateAndTime;
                UseDeviceDateAndTime = GUILayout.Toggle(UseDeviceDateAndTime, $"Switch between device - or game date and time.", GUI.skin.toggle);
                if (_UseDeviceDateAndTime != UseDeviceDateAndTime)
                {
                    if (UseDeviceDateAndTime)
                    {
                        LocalTimeManager.UseDeviceDateAndTime(true);
                    }
                    else
                    {
                        LocalTimeManager.UseDeviceDateAndTime(false);
                    }               
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(UseDeviceOption));
            }
        }

        private void OnlyForSingleplayerOrWhenHostBox()
        {
            using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUI.color = Color.yellow;
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
            }
        }

        private void DayCycleBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var timeofdayBoxScope = new GUILayout.VerticalScope(GUI.skin.box))
                {               
                    GUI.color = DefaultGuiColor;
                    GUILayout.Label("Go fast forward to the next day - or night time cycle: ", GUI.skin.label);
                    using (var actbutScopeView = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label($"Click [FFW >>] to set game time to { ( LocalTimeManager.IsNight( ) ? "the start of night time." : "daytime early morning." )  }", GUI.skin.label);
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
                    GUILayout.Label($"Current daytime length in minutes: {DayTimeScaleInMinutes} ", GUI.skin.label);
                    GUILayout.Label($"Current night time length in minutes: {NightTimeScaleInMinutes} ", GUI.skin.label);

                    GUI.color = DefaultGuiColor;
                    GUILayout.Label("Change scales for in-game day and night length in real-life minutes. To set, click [Apply]", GUI.skin.label);
                    using (var timescalesInputScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Daytime length: ", GUI.skin.label);
                        DayTimeScaleInMinutes = GUILayout.TextField(DayTimeScaleInMinutes, GUI.skin.textField);
                        GUILayout.Label("Night time length: ", GUI.skin.label);
                        NightTimeScaleInMinutes = GUILayout.TextField(NightTimeScaleInMinutes, GUI.skin.textField);
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

        private void CustomTimeScaleBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var ctimescalesScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Current time scale mode: {TimeScaleMode}.", GUI.skin.label);
                    GUIStyle selectedButtonStyle = new GUIStyle(GUI.skin.button);
                    selectedButtonStyle.onActive.textColor = Color.cyan;

                    string[] timeScaleModes = GetTimeScaleModes();
                    int _SelectedTimeScaleModeIndex = SelectedTimeScaleModeIndex;
                    float _SlowMotionFactor = SlowMotionFactor;

                    GUI.color = DefaultGuiColor;                    
                    GUILayout.Label("Choose a time scale mode:", GUI.skin.label);
                    using (var timemodeInputScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        SelectedTimeScaleModeIndex = GUILayout.SelectionGrid(SelectedTimeScaleModeIndex, timeScaleModes, timeScaleModes.Length, selectedButtonStyle);
                        if (_SelectedTimeScaleModeIndex != SelectedTimeScaleModeIndex)
                        {
                            TimeScaleMode = EnumUtils<TimeScaleModes>.GetValue(timeScaleModes[SelectedTimeScaleModeIndex]);
                            LocalTimeManager.SetTimeScaleMode(SelectedTimeScaleModeIndex);
                        }
                    }
                    if (TimeScaleMode==TimeScaleModes.Custom)
                    {
                        GUILayout.Label("Set custom time scale factor to apply:", GUI.skin.label);
                        SlowMotionFactor = GUILayout.HorizontalSlider(SlowMotionFactor, 0f, 1f);
                        if (_SlowMotionFactor != SlowMotionFactor)
                        {
                            LocalTimeManager.SetSlowMotionFactor(SlowMotionFactor);
                        }
                    }
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        public string[] GetTimeScaleModes()
        {
            return Enum.GetNames(typeof(TimeScaleModes));
        }

        public void ConditionMultipliersBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var mulBoxScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = DefaultGuiColor;
                    GUILayout.Label("Change multipliers for player condition: ", GUI.skin.label);

                    bool _HasChanged = HasChanged;                  

                    LocalHealthManager.GetMultipliers();
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void OnClickSetTimeScalesButton()
        {
            try
            {
                bool ok = LocalTimeManager.SetTimeScalesInMinutes(DayTimeScaleInMinutes, NightTimeScaleInMinutes);
                if (ok)
                {
                    ShowHUDBigInfo(HUDBigInfoMessage(TimeScalesSetMessage(DayTimeScaleInMinutes, NightTimeScaleInMinutes), MessageType.Info, Color.green));
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input {DayTimeScaleInMinutes} and {NightTimeScaleInMinutes}:\nPlease input numbers only - min. 0.1", MessageType.Warning, Color.yellow));
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
