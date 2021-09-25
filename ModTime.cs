using ModTime.Enums;
using System;
using System.IO;
using System.Xml;
using UnityEngine;
using UnityEngine.UI;

namespace ModTime
{

    /// <summary>
    /// ModTime is a mod for Green Hell, that allows a player to set in-game date and day and night time scales in real time minutes.
    /// Press HOME (default) or the key configurable in ModAPI to open the mod screen.
    /// </summary>
    public class ModTime : MonoBehaviour
    {
        private static readonly string ModName = nameof(ModTime);
        private static readonly string RuntimeConfigurationFile = Path.Combine(Application.dataPath.Replace("GH_Data", "Mods"), "RuntimeConfiguration.xml");
        private static readonly float MinWidth = 450f;
        private static readonly float TotalWidth = 500f;
        private static readonly float MaxWidth = 550f;
        private static readonly float MinHeight = 50f;
        private static readonly float TotalHeight = 150f;
        private static readonly float MaxHeight = 200f;
        private static readonly float StartPositionX = Screen.width / 4f;
        private static readonly float StartPositionY = Screen.height / 4f;
        private static readonly string DefaultDayTimeScale = "20";
        private static readonly string DefaultNightTimeScale = "10";

        private static KeyCode ModKeybindingId { get; set; } = KeyCode.Home;
        private static bool IsMinimized { get; set; } = false;
        private static bool ShowModTimeScreen { get; set; } = false;
        private static float PositionX { get; set; } = StartPositionX;
        private static float PositionY { get; set; } = StartPositionY;

        private Color DefaultGuiColor = GUI.color;
        private static ModTime Instance;
        private static Player LocalPlayer;
        private static HUDManager LocalHUDManager;
        private static CursorManager LocalCursorManager;

        public bool IsModActiveForMultiplayer { get; private set; }
        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public static Rect ModTimeScreen = new Rect(StartPositionX, StartPositionY, TotalWidth, TotalHeight);
        public static string DayTimeScaleInMinutes { get; set; } = DefaultDayTimeScale;
        public static string NightTimeScaleInMinutes { get; set; } = DefaultNightTimeScale;
        public static string InGameDay { get; set; } = MainLevel.Instance.m_TODSky.Cycle.Day.ToString();
        public static string InGameMonth { get; set; } = MainLevel.Instance.m_TODSky.Cycle.Month.ToString();
        public static string InGameYear { get; set; } = MainLevel.Instance.m_TODSky.Cycle.Year.ToString();
        public static string InGameTime { get; set; } = MainLevel.Instance.m_TODSky.Cycle.Hour.ToString();

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
            => $"Only available for single player or when host. Host can activate using ModManager.";
        public static string DayTimeSetMessage(string day, string month, string year, string hour)
            => $"Date time set:\nToday is {day}/{month}/{year}\nat {hour} o'clock.";
        public static string TimeScalesSetMessage(string dayTimeScale, string nightTimeScale)
            => $"Time scales set:\nDay time passes in {dayTimeScale} realtime minutes\nand night time in {nightTimeScale} realtime minutes.";
        public static string PermissionChangedMessage(string permission, string reason)
            => $"Permission to use mods and cheats in multiplayer was {permission} because {reason}.";
        public static string HUDBigInfoMessage(string message, MessageType messageType, Color? headcolor = null)
            => $"<color=#{ (headcolor != null ? ColorUtility.ToHtmlStringRGBA(headcolor.Value) : ColorUtility.ToHtmlStringRGBA(Color.red))  }>{messageType}</color>\n{message}";

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
            ModKeybindingId = GetConfigurableKey();
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

                configuredKeyCode = !string.IsNullOrEmpty(configuredKeybinding)
                                                            ? (KeyCode)Enum.Parse(typeof(KeyCode), configuredKeybinding.Replace("NumPad", "Alpha"))
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
                if (!ShowModTimeScreen)
                {
                    InitData();
                    EnableCursor(true);
                }
                ToggleShowUI();
                if (!ShowModTimeScreen)
                {
                    EnableCursor(false);
                }
            }
        }

        private void ToggleShowUI()
        {
            ShowModTimeScreen = !ShowModTimeScreen;
        }

        private void OnGUI()
        {
            if (ShowModTimeScreen)
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
            LocalCursorManager = CursorManager.Get();
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitWindow()
        {
            int wid = GetHashCode();
            ModTimeScreen = GUILayout.Window(wid, ModTimeScreen, InitModTimeScreen, ModName,
                                                                                    GUI.skin.window,
                                                                                    GUILayout.ExpandWidth(true),
                                                                                    GUILayout.MinWidth(MinWidth),
                                                                                    GUILayout.MaxWidth(TotalWidth),
                                                                                    GUILayout.ExpandHeight(true),
                                                                                    GUILayout.MinHeight(MinHeight),
                                                                                    GUILayout.MaxHeight(TotalHeight)
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
                ModTimeScreen = new Rect(PositionX, PositionY, TotalWidth, MinHeight);
                IsMinimized = true;
            }
            else
            {
                ModTimeScreen = new Rect(PositionX, PositionY, TotalWidth, TotalHeight);
                IsMinimized = false;
            }
            InitWindow();
        }

        private void CloseWindow()
        {
            ShowModTimeScreen = false;
            EnableCursor(false);
        }

        private void InitModTimeScreen(int windowID)
        {
            PositionX = ModTimeScreen.x;
            PositionY = ModTimeScreen.y;

            using (var modContentScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                ScreenMenuBox();
                if (!IsMinimized)
                {
                    ModOptionsBox();
                    TimeScalesBox();
                    DateTimeBox();
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
                    StatusForMultiplayer();
                    GUI.color = DefaultGuiColor;
                    GUILayout.Label($"To show or hide this screen, toggle press [{ModKeybindingId}]", GUI.skin.label);
                }
            }
            else
            {
                OnlyForSingleplayerOrWhenHostBox();
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

        private void StatusForMultiplayer()
        {
            string reason = string.Empty;
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                GUI.color = Color.cyan;
                if (IsModActiveForSingleplayer)
                {
                    reason = "you are the game host";
                }
                if (IsModActiveForMultiplayer)
                {
                    reason = "the game host allowed usage";
                }
                GUILayout.Toggle(true, PermissionChangedMessage($"granted", $"{reason}"), GUI.skin.toggle);
            }
            else
            {
                GUI.color = Color.yellow;
                if (!IsModActiveForSingleplayer)
                {
                    reason = "you are not the game host";
                }
                if (!IsModActiveForMultiplayer)
                {
                    reason = "the game host did not allow usage";
                }
                GUILayout.Toggle(false, PermissionChangedMessage($"revoked", $"{reason}"), GUI.skin.toggle);
            }
        }

        private void DateTimeBox()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                using (var daytimeScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Current date (day / month / year): {InGameDay} / {InGameMonth} / {InGameYear}", GUI.skin.label);
                    GUILayout.Label($"Current time: {InGameTime}", GUI.skin.label);

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
                        if (GUILayout.Button("Change datetime", GUI.skin.button, GUILayout.Width(150f)))
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
                    GUI.color = Color.cyan;
                    GUILayout.Label($"Current daytime scale: {DayTimeScaleInMinutes} minutes", GUI.skin.label);
                    GUILayout.Label($"Current nighttime scale: {NightTimeScaleInMinutes} minutes", GUI.skin.label);

                    GUI.color = DefaultGuiColor;
                    GUILayout.Label("Set in how many real-time minutes a day and night passes in game. Then clicck [Set time scales]", GUI.skin.label);
                    GUILayout.Label($"Default daytime scale: {DefaultDayTimeScale} minutes.", GUI.skin.label);
                    GUILayout.Label($"Default nighttime scale:  {DefaultNightTimeScale} minutes.", GUI.skin.label);
                    using (var changescalesScope = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        GUILayout.Label("Daytime scale: ", GUI.skin.label);
                        DayTimeScaleInMinutes = GUILayout.TextField(DayTimeScaleInMinutes, GUI.skin.textField);
                        GUILayout.Label("Nighttime scale:  ", GUI.skin.label);
                        NightTimeScaleInMinutes = GUILayout.TextField(NightTimeScaleInMinutes, GUI.skin.textField);
                    }
                    if (GUILayout.Button("Set time scales", GUI.skin.button))
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
                    SetDayTime(validGameDate.Day, validGameDate.Month, validGameDate.Year, validGameDate.Hour);
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

        private void SetDayTime(int gameDay,int gameMonth, int gameYear, float gameHour )
        {
            try
            {
                TOD_Sky m_TOD_Sky = MainLevel.Instance.m_TODSky;
                m_TOD_Sky.Cycle.Day = gameDay;
                m_TOD_Sky.Cycle.Hour = gameHour;
                m_TOD_Sky.Cycle.Month = gameMonth;
                m_TOD_Sky.Cycle.Year = gameYear;
                MainLevel.Instance.m_TODSky = m_TOD_Sky;

                MainLevel.Instance.SetTimeConnected(m_TOD_Sky.Cycle);
                MainLevel.Instance.UpdateCurentTimeInMinutes();

                ShowHUDBigInfo(HUDBigInfoMessage(DayTimeSetMessage(InGameDay, InGameMonth, InGameYear, InGameTime), MessageType.Info, Color.green));
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(SetDayTime));
            }
        }
    }
}
