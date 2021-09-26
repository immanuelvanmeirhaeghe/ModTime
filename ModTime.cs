﻿using ModTime.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModTime
{

    /// <summary>
    /// ModTime is a mod for Green Hell, that allows a player to set in-game date and day and night time scales in real time minutes.
    /// Press OemMinus (default) or the key configurable in ModAPI to open the mod screen.
    /// </summary>
    public class ModTime : MonoBehaviour
    {
        private static readonly string ModName = nameof(ModTime);
        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");

        private static readonly float ModScreenTotalWidth = 500f;

        private static readonly float ModScreenTotalHeight = 150f;

        private static readonly float ModScreenMinWidth = 450f;

        private static readonly float ModScreenMaxWidth = 550f;

        private static readonly float ModScreenMinHeight = 50f;

        private static readonly float ModScreenMaxHeight = 200f;

        private static readonly string DefaultDayTimeScale = "20";

        private static readonly string DefaultNightTimeScale = "10";

        private static KeyCode ModKeybindingId { get; set; } = KeyCode.Minus;

        private static float ModScreenStartPositionX { get; set; } = (Screen.width - ModScreenMaxWidth) % ModScreenTotalWidth;


        private static float ModScreenStartPositionY { get; set; } = (Screen.height - ModScreenMaxHeight) % ModScreenTotalHeight;


        private static bool IsMinimized { get; set; } = false;

        private bool ShowModUI;

        public bool IsModActiveForMultiplayer { get; private set; }

        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        private Dictionary<int, WatchData> WatchDataDictionary = new Dictionary<int, WatchData>();

        private static Color DefaultGuiColor;
        private static ModTime Instance;
        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static CursorManager LocalCursorManager;
        private static Watch LocalWatch;

        public static Rect ModTimeScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        public static string DayTimeScaleInMinutes { get; set; } = DefaultDayTimeScale;
        public static string NightTimeScaleInMinutes { get; set; } = DefaultNightTimeScale;
        public static string InGameDay { get; set; } = MainLevel.Instance.m_TODSky.Cycle.DateTime.Day.ToString();
        public static string InGameMonth { get; set; } = MainLevel.Instance.m_TODSky.Cycle.DateTime.Month.ToString();
        public static string InGameYear { get; set; } = MainLevel.Instance.m_TODSky.Cycle.DateTime.Year.ToString();
        public static string InGameTime { get; set; } = MainLevel.Instance.m_TODSky.Cycle.DateTime.Hour.ToString();

        public static bool IsTimeProgressing { get; set; } = true;
        public static bool IsRaining { get; set; } = false;
        public static bool IsDaytime { get; set; } = true;


        public ModTime()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static ModTime Get()
        {
            return Instance;
        }

        public static string SystemInfoChatMessage(string content, Color? color = null)
            => $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))}>System</color>:\n{content}";
        public static string OnlyForSinglePlayerOrHostMessage()
            => $"Only available for single player or when host. Host can activate using ModManager.";
        public static string FlagStateChangedMessage(bool flagState, string content, Color? color = null)
   => SystemInfoChatMessage(
       $"<color=#{(color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow))}>{content} { (flagState ? "enabled" : "disabled")  }!</color>",
       color);
        public static string DayTimeSetMessage(string day, string month, string year, string hour)
            => $"Date time set:\nToday is {day}/{month}/{year}\nat {hour} o'clock.";
        public static string TimeScalesSetMessage(string dayTimeScale, string nightTimeScale)
            => $"Time scales set:\nDay time passes in {dayTimeScale} realtime minutes\nand night time in {nightTimeScale} realtime minutes.";
        public static string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

        public delegate void OnOptionToggled(bool optionValue, string optionText);
        public static event OnOptionToggled onOptionToggled;
        private void ModTime_onOptionToggled(bool optionValue, string optionText)
        {
            ShowHUDBigInfo(
                FlagStateChangedMessage(optionValue, optionText));
        }

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ModKeybindingId = GetConfigurableKey();
            onOptionToggled += ModTime_onOptionToggled;
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

        private KeyCode GetConfigurableKey()
        {
            KeyCode configuredKeyCode = default;
            string configuredKeybinding = string.Empty;

            try
            {
                //ModAPI.Log.Write($"Searching XML runtime configuration file {RuntimeConfigurationFile}...");
                if (File.Exists(RuntimeConfigurationFile))
                {
                    using (var xmlReader = XmlReader.Create(new StreamReader(RuntimeConfigurationFile)))
                    {
                        //ModAPI.Log.Write($"Reading XML runtime configuration file...");
                        while (xmlReader.Read())
                        {
                            //ModAPI.Log.Write($"Searching configuration for Button for Mod with ID = {ModName}...");
                            if (xmlReader["ID"] == ModName)
                            {
                                if (xmlReader.ReadToFollowing(nameof(Button)))
                                {
                                    //ModAPI.Log.Write($"Found configuration for Button for Mod with ID = {ModName}!");
                                    configuredKeybinding = xmlReader.ReadElementContentAsString();
                                    //ModAPI.Log.Write($"Configured keybinding = {configuredKeybinding}.");
                                }
                            }
                        }
                    }
                    //ModAPI.Log.Write($"XML runtime configuration\n{File.ReadAllText(RuntimeConfigurationFile)}\n");
                }

                configuredKeybinding = configuredKeybinding?.Replace("NumPad", "Alpha").Replace("Oem", "");

                configuredKeyCode = !string.IsNullOrEmpty(configuredKeybinding)
                                                            ? (KeyCode)Enum.Parse(typeof(KeyCode), configuredKeybinding)
                                                            : ModKeybindingId;
                //ModAPI.Log.Write($"Configured key code: { configuredKeyCode }");
                return configuredKeyCode;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetConfigurableKey));
                return configuredKeyCode;
            }
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModName}:{methodName}] throws exception:\n{exc.Message}";
            ModAPI.Log.Write(info);
            ShowHUDBigInfo(HUDBigInfoMessage(info, MessageType.Error, Color.red));
        }

        public void ShowHUDBigInfo(string text)
        {
            string header = $"{ModName} Info";
            string textureName = HUDInfoLogTextureType.Count.ToString();

            HUDBigInfo hudBigInfo = (HUDBigInfo)LocalHUDManager.GetHUD(typeof(HUDBigInfo));
            HUDBigInfoData.s_Duration = 2f;
            HUDBigInfoData hudBigInfoData = new HUDBigInfoData
            {
                m_Header = header,
                m_Text = text,
                m_TextureName = textureName,
                m_ShowTime = Time.time
            };
            hudBigInfo.AddInfo(hudBigInfoData);
            hudBigInfo.Show(true);
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)LocalHUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            LocalCursorManager.ShowCursor(blockPlayer, false);

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
            if (Input.GetKeyDown(ModKeybindingId))
            {
                if (!ShowModUI)
                {
                    InitData();
                    EnableCursor(true);
                }
                ToggleShowUI();
                if (!ShowModUI)
                {
                    EnableCursor(false);
                }
            }
        }

        private void ToggleShowUI()
        {
            ShowModUI = !ShowModUI;
        }

        private void OnGUI()
        {
            if (ShowModUI)
            {
                InitData();
                InitTimeData();
                InitSkinUI();
                InitWindow();
            }
        }

        private void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalCursorManager = CursorManager.Get();
            LocalWatch = Watch.Get();
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
            DefaultGuiColor = GUI.color;
        }

        private void InitWindow()
        {
            int wid = GetHashCode();
            ModTimeScreen = GUILayout.Window(wid, ModTimeScreen, InitModTimeScreen, ModName,
                                                                                    GUI.skin.window,
                                                                                    GUILayout.ExpandWidth(true),
                                                                                    GUILayout.MinWidth(ModScreenMinWidth),
                                                                                    GUILayout.MaxWidth(ModScreenMaxWidth),
                                                                                    GUILayout.ExpandHeight(true),
                                                                                    GUILayout.MinHeight(ModScreenMinHeight),
                                                                                    GUILayout.MaxHeight(ModScreenMaxHeight)
                                                                                    );
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
            ShowModUI = false;
            EnableCursor(false);
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
                    //ModOptionsBox();
                    TimeScalesBox();
                    DateTimeCycleBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void ModOptionsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                GUI.color = DefaultGuiColor;
                using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To toggle the mod main UI, press [{ModKeybindingId}]", GUI.skin.label);
                    //GUILayout.Label($"Options for multiplayer", GUI.skin.label);
                    //ModStatusOptionBox();
                    GUILayout.Label($"Options for weather and time", GUI.skin.label);
                    TimeOptionBox();
                    WeatherOptionBox();
                    DaytimeOptionBox();
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        //private void ModStatusOptionBox()
        //{
        //    string reason = string.Empty;
        //    if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
        //    {
        //        GUI.color = Color.green;
        //        if (IsModActiveForSingleplayer)
        //        {
        //            reason = "you are the game host";
        //        }
        //        if (IsModActiveForMultiplayer)
        //        {
        //            reason = "the game host allowed usage";
        //        }
        //        GUILayout.Toggle(true, PermissionChangedMessage($"granted", reason), GUI.skin.toggle);
        //    }
        //    else
        //    {
        //        GUI.color = Color.yellow;
        //        if (!IsModActiveForSingleplayer)
        //        {
        //            reason = "you are not the game host";
        //        }
        //        if (!IsModActiveForMultiplayer)
        //        {
        //            reason = "the game host did not allow usage";
        //        }
        //        GUILayout.Toggle(false, PermissionChangedMessage($"revoked", reason), GUI.skin.toggle);
        //    }
        //}

        private void TimeOptionBox()
        {
            bool _timeOption = IsTimeProgressing;
            string _timeOptionText = $"Progress or stop time? ";
            IsTimeProgressing = GUILayout.Toggle(IsTimeProgressing, _timeOptionText, GUI.skin.toggle);
            //ToggleProgressTime(_timeOption);
        }

        private void WeatherOptionBox()
        {
            bool _rainOptionState = IsRaining;
            string _rainOptionText = $"Is it raining?";
            IsRaining = GUILayout.Toggle(IsRaining, _rainOptionText, GUI.skin.toggle);
            // ToggleWeather(_rainOptionState);
        }

        private void DaytimeOptionBox()
        {
            bool _daytimeOption = IsDaytime;
            string _daytimeOptionText = $"Is it daytime or night?";
            IsDaytime = GUILayout.Toggle(IsDaytime, _daytimeOptionText, GUI.skin.toggle);
            //ToggleDaytime(_daytimeOption);
        }

        private void OnlyForSingleplayerOrWhenHostBox()
        {
            using (var infoScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUI.color = Color.yellow;
                GUILayout.Label(OnlyForSinglePlayerOrHostMessage(), GUI.skin.label);
            }
        }

        private void DateTimeCycleBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var daytimeScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    //GUI.color = Color.cyan;
                    //GUILayout.Label(GetWatchData(), GUI.skin.label);

                    GUI.color = DefaultGuiColor;
                    GUILayout.Label("Change the date (day / month / year) and time in game. Then click [Change datetime]", GUI.skin.label);
                    using (var changedaytimeScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("New date (day / month / year): ", GUI.skin.label);
                        InGameDay = GUILayout.TextField(InGameDay, GUI.skin.textField);
                        GUILayout.Label(" / ", GUI.skin.label);
                        InGameMonth = GUILayout.TextField(InGameMonth, GUI.skin.textField);
                        GUILayout.Label(" / ", GUI.skin.label);
                        InGameYear = GUILayout.TextField(InGameYear, GUI.skin.textField);
                        GUILayout.Label("Time: ", GUI.skin.label);
                        InGameTime = GUILayout.TextField(InGameTime, GUI.skin.textField);
                        if (GUILayout.Button("Change datetime", GUI.skin.button, GUILayout.MaxWidth(200f)))
                        {
                            OnClickSetDayTimeButton();
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
                using (var timescalesScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Set scales in real-time minutes for daytime and nighttime. Then click [Set time scales]", GUI.skin.label);
                    using (var changescalesScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Daytime: ", GUI.skin.label);
                        DayTimeScaleInMinutes = GUILayout.TextField(DayTimeScaleInMinutes, GUI.skin.textField);
                        GUILayout.Label("Nighttime:  ", GUI.skin.label);
                        NightTimeScaleInMinutes = GUILayout.TextField(NightTimeScaleInMinutes, GUI.skin.textField);
                    }
                    if (GUILayout.Button("Set time scales", GUI.skin.button, GUILayout.MaxWidth(200f)))
                    {
                        OnClickSetTimeScalesButton();
                    }
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        public static void ToggleWeather(bool optionState)
        {
            if (optionState != IsRaining)
            {
                onOptionToggled?.Invoke(optionState, $"Rain option has been");
                if (IsRaining)
                {
                    RainManager.Get().ScenarioStartRain();
                }
                else
                {
                    RainManager.Get().ScenarioStopRain();
                }
            }
        }

        public static void ToggleProgressTime(bool optionState)
        {
            if (optionState != IsTimeProgressing)
            {
                onOptionToggled?.Invoke(optionState, $"Time progress has been");
                if (IsTimeProgressing)
                {
                    MainLevel.Instance.StartDayTimeProgress();
                }
                else
                {
                    MainLevel.Instance.StopDayTimeProgress(); ;
                }
            }
        }

        public static void ToggleDaytime(bool optionState)
        {
            if (optionState != IsDaytime)
            {
                onOptionToggled?.Invoke(optionState, $"Daytime has been");
                if (IsDaytime && !IsNight())
                {
                    MainLevel.Instance.SetDayTime(5, 0);
                }
                else
                {
                    MainLevel.Instance.SetDayTime(22, 0);
                }
            }
        }

        private void OnClickSetTimeScalesButton()
        {
            try
            {
                float validDayTimeScale = ValidateTimeScale(DayTimeScaleInMinutes);
                float validNightTimeScale = ValidateTimeScale(NightTimeScaleInMinutes);
                if (validDayTimeScale > 0 && validNightTimeScale > 0)
                {
                    SetTimeScales(validDayTimeScale, validNightTimeScale);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickSetTimeScalesButton));
            }
        }

        private void OnClickSetDayTimeButton()
        {
            try
            {
                DateTime validGameDate = ValidateDay(InGameDay, InGameMonth, InGameYear, InGameTime);
                if (validGameDate != DateTime.MinValue)
                {
                    SetDateTimeCycle(validGameDate.Day, validGameDate.Month, validGameDate.Year, validGameDate.Hour);
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickSetDayTimeButton));
            }
        }

        private DateTime ValidateDay(string inGameDay, string inGameMonth, string inGameYear, string inGameHour)
        {
            if (int.TryParse(inGameYear, out int year) && int.TryParse(inGameMonth, out int month) && int.TryParse(inGameDay, out int day) && float.TryParse(inGameHour, out float hour))
            {
                if (DateTime.TryParse($"{year}-{month}-{day}", out DateTime parsed))
                {
                    DateTime dayTime = new DateTime(parsed.Year, parsed.Month, parsed.Day, Convert.ToInt32(hour), 0, 0);
                    return dayTime;
                }
                else
                {
                    ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input {year}-{month}-{day}T{hour}:00:00:000Z: please input  a valid date and time", MessageType.Error, Color.red));
                    return DateTime.MinValue;
                }
            }
            else
            {
                ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input {inGameYear}-{inGameMonth}-{inGameDay}T{inGameHour}:00:00:000Z: please input valid date and time", MessageType.Error, Color.red));
                return DateTime.MinValue;
            }
        }

        private float ValidateTimeScale(string toValidate)
        {
            if (float.TryParse(toValidate, out float count))
            {
                if (count <= 1)
                {
                    count = 1;
                }
                if (count > 60)
                {
                    count = 60;
                }
                return count;
            }
            else
            {
                ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input {toValidate}: please input numbers only - min. 1 and max. 60", MessageType.Error, Color.red));
                return -1;
            }
        }

        private void SetTimeScales(float dayTimeScale, float nightTimeScale)
        {
            try
            {
                TOD_Time m_TOD_Time = MainLevel.Instance.m_TODTime;
                m_TOD_Time.m_DayLengthInMinutes = dayTimeScale;
                m_TOD_Time.m_NightLengthInMinutes = nightTimeScale;
                MainLevel.Instance.m_TODTime = m_TOD_Time;
                ShowHUDBigInfo(HUDBigInfoMessage(TimeScalesSetMessage(dayTimeScale.ToString(), nightTimeScale.ToString()), MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(SetTimeScales));
            }
        }

        private void SetDateTimeCycle(int gameDay, int gameMonth, int gameYear, int gameHour)
        {
            try
            {
                TOD_Sky m_TOD_Sky = MainLevel.Instance.m_TODSky;
                m_TOD_Sky.Cycle.DateTime = new DateTime(gameYear, gameMonth, gameDay);
                m_TOD_Sky.Cycle.Hour = gameHour;
                MainLevel.Instance.m_TODSky = m_TOD_Sky;
                ShowHUDBigInfo(HUDBigInfoMessage(DayTimeSetMessage(InGameDay, InGameMonth, InGameYear, InGameTime), MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(SetDateTimeCycle));
            }
        }

        public static bool IsNight()
        {
            if (!(MainLevel.Instance.m_TODSky.Cycle.Hour < 5f))
            {
                return MainLevel.Instance.m_TODSky.Cycle.Hour > 22f;
            }
            return true;
        }

        private void InitTimeData()
        {
            WatchTimeData watchTimeData = new WatchTimeData();
            watchTimeData.m_Parent = LocalWatch.m_Canvas.transform.Find("Time").gameObject;
            watchTimeData.m_TimeHourDec = watchTimeData.m_Parent.transform.Find("HourDec").GetComponent<Text>();
            watchTimeData.m_TimeHourUnit = watchTimeData.m_Parent.transform.Find("HourUnit").GetComponent<Text>();
            watchTimeData.m_TimeMinuteDec = watchTimeData.m_Parent.transform.Find("MinuteDec").GetComponent<Text>();
            watchTimeData.m_TimeMinuteUnit = watchTimeData.m_Parent.transform.Find("MinuteUnit").GetComponent<Text>();
            watchTimeData.m_DayDec = watchTimeData.m_Parent.transform.Find("DayDec").GetComponent<Text>();
            watchTimeData.m_DayUnit = watchTimeData.m_Parent.transform.Find("DayUnit").GetComponent<Text>();
            watchTimeData.m_DayName = watchTimeData.m_Parent.transform.Find("DayName").GetComponent<Text>();
            watchTimeData.m_MonthName = watchTimeData.m_Parent.transform.Find("MonthName").GetComponent<Text>();
            watchTimeData.m_Parent.SetActive(value: false);
            WatchDataDictionary.Add(0, watchTimeData);
        }

        private string GetWatchData()
        {
            InitTimeData();
            WatchTimeData watchTimeData = (WatchTimeData)WatchDataDictionary[0];
            var num3 = watchTimeData.m_TimeHourDec.text;
            var num2 = watchTimeData.m_TimeHourUnit.text;
            var num5 = watchTimeData.m_TimeMinuteDec.text;
            var num4 = watchTimeData.m_TimeMinuteUnit.text;
            var num9 = watchTimeData.m_DayDec.text;
            var num8 = watchTimeData.m_DayUnit.text;
            var num7 = watchTimeData.m_DayName.text;
            var num6 = watchTimeData.m_MonthName.text;
            var num1 = MainLevel.Instance.m_TODSky.Cycle.Year;
            string watchData = $"It is currently {num3}{num2}:{num5}{num4} o'clock on {num7}, {num9}{num8}  {num6} in the year {num1}";

            return watchData;
        }

    }
}
