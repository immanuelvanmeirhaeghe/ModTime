using ModTime.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModTime
{
    public class ModTime : MonoBehaviour
    {
        private static ModTime Instance;
        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        private static KeyCode ModKeybindingId { get; set; } = KeyCode.Pause;
        private static readonly string ModName = nameof(ModTime);
        private static readonly float ModScreenTotalWidth = 500f;
        private static readonly float ModScreenTotalHeight = 150f;
        private static readonly float ModScreenMinWidth = 450f;
        private static readonly float ModScreenMaxWidth = 550f;
        private static readonly float ModScreenMinHeight = 50f;
        private static readonly float ModScreenMaxHeight = 200f;
        private bool ShowUI;
        private Color DefaultGuiColor = GUI.color;
        private static bool IsMinimized { get; set; } = false;
        public static Rect ModTimeScreen = new Rect(ModScreenStartPositionX, ModScreenStartPositionY, ModScreenTotalWidth, ModScreenTotalHeight);

        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static RainManager LocalRainManager;
        private static Watch LocalWatch;
        private Dictionary<int, WatchData> LocalWatchData = new Dictionary<int, WatchData>();

        private static float ModScreenStartPositionX { get; set; } = ((float)Screen.width - ModScreenMaxWidth) % ModScreenTotalWidth;
        private static float ModScreenStartPositionY { get; set; } = ((float)Screen.height - ModScreenMaxHeight) % ModScreenTotalHeight;

        public bool RainEnabled { get; private set; } = false;
        public bool TimeOfDayIsDaytime { get; private set; } = false;
        public bool ProgressTime { get; private set; } = true;

        public static string DayTimeScaleInMinutes { get; set; } = "20";
        public static string NightTimeScaleInMinutes { get; set; } = "10";
        public static string InGameDay { get; set; } = MainLevel.Instance.m_TODSky.Cycle.DateTime.Day.ToString();
        public static string InGameMonth { get; set; } = MainLevel.Instance.m_TODSky.Cycle.DateTime.Month.ToString();
        public static string InGameYear { get; set; } = MainLevel.Instance.m_TODSky.Cycle.DateTime.Year.ToString();
        public static string InGameHour { get; set; } = MainLevel.Instance.m_TODSky.Cycle.DateTime.Hour.ToString();

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public ModTime()
        {
            useGUILayout = true;
            Instance = this;
        }
        public static ModTime Get()
        {
            return Instance;
        }

        public static string OnlyForSinglePlayerOrHostMessage()
            => "Only available for single player or when host. Host can activate using ModManager.";
        public static string DayTimeSetMessage(string daytime)
            => $"Daytime fast forwarded. It is {daytime}";
        public static string TimeScalesSetMessage(string dayTimeScale, string nightTimeScale)
            => $"Time scales set:\nDay time passes in " + dayTimeScale + " realtime minutes\nand night time in " + nightTimeScale + " realtime minutes.";
        public static string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ModKeybindingId = GetConfigurableKey();
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
            if (Input.GetKeyDown(ModKeybindingId))
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

        private void InitData()
        {
            LocalHUDManager = HUDManager.Get();
            LocalPlayer = Player.Get();
            LocalRainManager = RainManager.Get();
            LocalWatch = Watch.Get();
            InitTimeData();
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitWindow()
        {
            ModTimeScreen = GUILayout.Window(GetHashCode(), ModTimeScreen, InitModTimeScreen, ModName, GUI.skin.window, GUILayout.ExpandWidth(expand: true), GUILayout.MinWidth(ModScreenMinWidth), GUILayout.MaxWidth(ModScreenMaxWidth), GUILayout.ExpandHeight(expand: true), GUILayout.MinHeight(ModScreenMinHeight), GUILayout.MaxHeight(ModScreenMaxHeight));
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
            using (new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsMinimized)
                {
                    OptionsBox();
                    TimeScalesBox();
                    TimeOfDayBox();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void OptionsBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var optionsScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label($"To toggle the main mod UI, press [{ModKeybindingId}]", GUI.skin.label);

                    GUILayout.Label("Multiplayer options: ", GUI.skin.label);
                    MultiplayerOptionBox();

                    GUILayout.Label($"Weather options: ", GUI.skin.label);
                    RainOptionBox();
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
            }
        }

        private void MultiplayerOptionBox()
        {
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
                GUILayout.Toggle(true, PermissionChangedMessage($"granted", multiplayerOptionMessage), GUI.skin.toggle);
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
                GUILayout.Toggle(false, PermissionChangedMessage($"revoked", multiplayerOptionMessage), GUI.skin.toggle);
            }
        }

        private void RainOptionBox()
        {
            string rainOptionMessage = string.Empty;
            try
            {
                bool _tRainEnabled = RainEnabled;
                RainEnabled = GUILayout.Toggle(RainEnabled, $"Change the weather?", GUI.skin.toggle);
                if (_tRainEnabled != RainEnabled)
                {
                    if (RainEnabled)
                    {
                        LocalRainManager.ScenarioStartRain();
                    }
                    else
                    {
                        LocalRainManager.ScenarioStopRain();
                    }
                    MainLevel.Instance.EnableAtmosphereAndCloudsUpdate(RainEnabled);
                }
                rainOptionMessage = $"Rain { (RainEnabled ? "is falling" : "has stopped") }";
                ShowHUDBigInfo(HUDBigInfoMessage(rainOptionMessage, MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(RainOptionBox));
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

        private void TimeOfDayBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var timeofdayBoxScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.cyan;
                    GUILayout.Label(GetWatchTimeData(), GUI.skin.label);

                    GUI.color = DefaultGuiColor;
                    GUILayout.Label("To fast forward time in-game to the next daytime cycle, click [FFW >>]", GUI.skin.label);
                    using (var timeofdayScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        if (GUILayout.Button("FFW >>", GUI.skin.button, GUILayout.MaxWidth(200f)))
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

        private void InitTimeData()
        {
            WatchTimeData watchTimeData = new WatchTimeData
            {
                m_Parent = LocalWatch.m_Canvas.transform.Find("Time").gameObject
            };
            watchTimeData.m_TimeHourDec = watchTimeData.m_Parent.transform.Find("HourDec").GetComponent<Text>();
            watchTimeData.m_TimeHourUnit = watchTimeData.m_Parent.transform.Find("HourUnit").GetComponent<Text>();
            watchTimeData.m_TimeMinuteDec = watchTimeData.m_Parent.transform.Find("MinuteDec").GetComponent<Text>();
            watchTimeData.m_TimeMinuteUnit = watchTimeData.m_Parent.transform.Find("MinuteUnit").GetComponent<Text>();
            watchTimeData.m_DayDec = watchTimeData.m_Parent.transform.Find("DayDec").GetComponent<Text>();
            watchTimeData.m_DayUnit = watchTimeData.m_Parent.transform.Find("DayUnit").GetComponent<Text>();
            watchTimeData.m_DayName = watchTimeData.m_Parent.transform.Find("DayName").GetComponent<Text>();
            watchTimeData.m_MonthName = watchTimeData.m_Parent.transform.Find("MonthName").GetComponent<Text>();
            watchTimeData.m_Parent.SetActive(value: false);
            LocalWatchData.Add(0, watchTimeData);
        }


        private string GetWatchTimeData()
        {
            string watchTimeData = string.Empty;
            try
            {
                WatchTimeData data = (WatchTimeData)LocalWatchData[0];
                if (data != null)
                {
                    watchTimeData = $"Time is {data.m_TimeHourDec.text}{data.m_TimeHourUnit.text} : {data.m_TimeMinuteDec.text}{data.m_TimeMinuteUnit.text} "
                                                    + GetWatchTimeDayOfMonth(data);
                }
                return watchTimeData;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetWatchTimeData));
                return string.Empty;
            }
        }

        private string GetWatchTimeDayOfMonth(WatchTimeData data)
        {
            int dayDecimal = Convert.ToInt32(data.m_DayDec.text);
            int dayUnit = Convert.ToInt32(data.m_DayUnit.text);
            string daySuffix;
            switch (dayUnit)
            {
                case 1:
                    daySuffix = "st";
                    break;
                case 2:
                    daySuffix = "nd";
                    break;
                case 3:
                    daySuffix = "rd";
                    break;
                default:
                    daySuffix = "th";
                    break;
            }

            return $"on {data.m_DayName.text},the {dayDecimal}{dayUnit} {daySuffix} of {data.m_MonthName.text}";
        }

        private void TimeScalesBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Change scales for in-game day and night length in real-life minutes. Then click [Set scales]", GUI.skin.label);
                    using (new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Day length: ", GUI.skin.label);
                        DayTimeScaleInMinutes = GUILayout.TextField(DayTimeScaleInMinutes, GUI.skin.textField);
                        GUILayout.Label("Night length: ", GUI.skin.label);
                        NightTimeScaleInMinutes = GUILayout.TextField(NightTimeScaleInMinutes, GUI.skin.textField);
                    }
                    if (GUILayout.Button("Set scales", GUI.skin.button))
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

        private void OnClickSetTimeScalesButton()
        {
            try
            {
                float num = ValidateTimeScale(DayTimeScaleInMinutes);
                float num2 = ValidateTimeScale(NightTimeScaleInMinutes);
                if (num > 0f && num2 > 0f)
                {
                    SetTimeScales(num, num2);
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
                TOD_Sky sky = MainLevel.Instance.m_TODSky;
                DateTime dateTime = sky.Cycle.DateTime;
                if (dateTime != DateTime.MinValue)
                {
                    string daytime = string.Empty ;
                    if ( sky.IsNight)
                    {
                        dateTime = dateTime.AddDays(1);
                        MainLevel.Instance.m_TODSky.Cycle.DateTime = dateTime;
                        MainLevel.Instance.SetDayTime(5, 1);
                        daytime = nameof(sky.Day);
                    }
                    else
                    {
                        MainLevel.Instance.SetDayTime(22, 1);
                        daytime = nameof(sky.Night);
                    }

                    ShowHUDBigInfo(HUDBigInfoMessage(DayTimeSetMessage(daytime), MessageType.Info, Color.green));
                }
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(OnClickFastForwardDayCycleButton));
            }
        }

        private DateTime ValidateDay(string inGameDay, string inGameMonth, string inGameYear)
        {
            if (int.TryParse(inGameYear, out var result) && int.TryParse(inGameMonth, out var result2) && int.TryParse(inGameDay, out var result3))
            {
                if (DateTime.TryParse($"{result}-{result2}-{result3}", out var result5))
                {
                    return new DateTime(result5.Year, result5.Month, result5.Day);
                }
                ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input {result}-{result2}-{result3}: please input  a valid date and time", MessageType.Error, Color.red));
                return DateTime.MinValue;
            }
            ShowHUDBigInfo(HUDBigInfoMessage($"Invalid input " + inGameYear + "-" + inGameMonth + "-" + inGameDay + ": please input valid date and time", MessageType.Error, Color.red));
            return DateTime.MinValue;
        }

        private float ValidateTimeScale(string toValidate)
        {
            if (float.TryParse(toValidate, out var result))
            {
                if (result <= 0f)
                {
                    result = 0.1f;
                }
                if (result > 60f)
                {
                    result = 60f;
                }
                return result;
            }
            ShowHUDBigInfo(HUDBigInfoMessage("Invalid input " + toValidate + ": please input numbers only - min. 0.1 and max. 60", MessageType.Error, Color.red));
            return -1f;
        }

        private void SetTimeScales(float dayTimeScale, float nightTimeScale)
        {
            try
            {
                TOD_Time tODTime = MainLevel.Instance.m_TODTime;
                tODTime.m_DayLengthInMinutes = dayTimeScale;
                tODTime.m_NightLengthInMinutes = nightTimeScale;
                MainLevel.Instance.m_TODTime = tODTime;
                ShowHUDBigInfo(HUDBigInfoMessage(TimeScalesSetMessage(dayTimeScale.ToString(), nightTimeScale.ToString()), MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(SetTimeScales));
            }
        }
    }
}
