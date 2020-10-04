using ModManager;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModTime
{
    /// <summary>
    /// ModTime is a mod for Green Hell
    /// that allows a player to custom set time scales.
    /// (only in single player mode - Use ModManager for multiplayer).
    /// Enable the mod UI by pressing Home.
    /// </summary>
    public class ModTime : MonoBehaviour
    {
        private static ModTime s_Instance;

        private static readonly string ModName = nameof(ModTime);

        private bool ShowUI = false;

        public static Rect ModTimeScreen = new Rect(Screen.width / 2f, Screen.height / 2f, 450f, 150f);

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        private static ModManager.ModManager modManager;

        private static string m_DayInMinutes = "20";

        private static string m_NightInMinutes = "10";

        private static string m_Day = MainLevel.Instance.m_TODSky.Cycle.Day.ToString();

        private static string m_Month = MainLevel.Instance.m_TODSky.Cycle.Month.ToString();

        private static string m_Year = MainLevel.Instance.m_TODSky.Cycle.Year.ToString();

        private static string m_Hour = MainLevel.Instance.m_TODSky.Cycle.Hour.ToString();

        public ModTime()
        {
            useGUILayout = true;
            s_Instance = this;
        }

        public static ModTime Get()
        {
            return s_Instance;
        }

        private static string HUDBigInfoMessage(string message) => $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.red)}>System</color>\n{message}";

        public void Start()
        {
            ModManager.ModManager.onPermissionValueChanged += ModManager_onPermissionValueChanged;
        }

        private void ModManager_onPermissionValueChanged(bool optionValue)
        {
            IsModActiveForMultiplayer = optionValue;
            ShowHUDBigInfo(
                          (optionValue ?
                            HUDBigInfoMessage($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>Permission to use mods for multiplayer was granted!</color>")
                            : HUDBigInfoMessage($"<color=#{ColorUtility.ToHtmlStringRGBA(Color.yellow)}>Permission to use mods for multiplayer was revoked!</color>")),
                           $"{ModName} Info",
                           HUDInfoLogTextureType.Count.ToString());
        }

        public bool IsModActiveForMultiplayer { get; private set; }

        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public void ShowHUDBigInfo(string text, string header, string textureName)
        {
            HUDBigInfo hudBigInfo = (HUDBigInfo)hUDManager.GetHUD(typeof(HUDBigInfo));
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
            ((HUDMessages)hUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        private void EnableCursor(bool blockPlayer = false)
        {
            CursorManager.Get().ShowCursor(blockPlayer, false);

            if (blockPlayer)
            {
                player.BlockMoves();
                player.BlockRotation();
                player.BlockInspection();
            }
            else
            {
                player.UnblockMoves();
                player.UnblockRotation();
                player.UnblockInspection();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                if (!ShowUI)
                {
                    InitData();
                    EnableCursor(true);
                }
                ToggleShowUI();
                if (!ShowUI)
                {
                    EnableCursor(false);
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

        private void InitData()
        {
            hUDManager = HUDManager.Get();
            itemsManager = ItemsManager.Get();
            player = Player.Get();
        }

        private void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitWindow()
        {
            int wid = GetHashCode();
            ModTimeScreen = GUILayout.Window(wid, ModTimeScreen, InitModTimeScreen, $"{ModName}", GUI.skin.window);
        }

        private void InitModTimeScreen(int windowID)
        {
            using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
            {
                if (GUI.Button(new Rect(430f, 0f, 20f, 20f), "X", GUI.skin.button))
                {
                    CloseWindow();
                }
                using (var verticalScope2 = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Set how many real minutes a day or night takes in game.", GUI.skin.label);
                    GUILayout.Label("Default scales: Day time: 20 minutes. Night time: 10 minutes.", GUI.skin.label);
                }
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("Day time: ", GUI.skin.label);
                    m_DayInMinutes = GUILayout.TextField(m_DayInMinutes, GUI.skin.textField);
                    GUILayout.Label("Night time: ", GUI.skin.label);
                    m_NightInMinutes = GUILayout.TextField(m_NightInMinutes, GUI.skin.textField);
                    SetTimeScalesButton();
                }
                using (var verticalScope3 = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Set the current date and time in game.", GUI.skin.label);
                    GUILayout.Label("Day starts at 5AM. Night starts at 10PM", GUI.skin.label);
                }
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("Day: ", GUI.skin.label);
                    m_Day = GUILayout.TextField(m_Day, GUI.skin.textField);
                    GUILayout.Label("Month: ", GUI.skin.label);
                    m_Month = GUILayout.TextField(m_Month, GUI.skin.textField);
                    GUILayout.Label("Year: ", GUI.skin.label);
                    m_Year = GUILayout.TextField(m_Year, GUI.skin.textField);
                    GUILayout.Label("Hour: ", GUI.skin.label);
                    m_Hour = GUILayout.TextField(m_Hour, GUI.skin.textField);
                    SetDateTimeButton();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void CloseWindow()
        {
            ShowUI = false;
            EnableCursor(false);
        }

        private void SetTimeScalesButton()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                if (GUILayout.Button("Set time scales", GUI.skin.button))
                {
                    OnClickSetTimeScalesButton();
                    CloseWindow();
                }
            }
            else
            {
                using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Set time scales", GUI.skin.label);
                    GUILayout.Label("is only for single player or when host.", GUI.skin.label);
                    GUILayout.Label("Host can activate using ModManager.", GUI.skin.label);
                }
            }
        }

        private void OnClickSetTimeScalesButton()
        {
            try
            {
                SetTimeScales();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickSetTimeScalesButton)}] throws exception: {exc.Message}");
            }
        }

        private void SetDateTimeButton()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                if (GUILayout.Button("Set date time", GUI.skin.button))
                {
                    OnClickSetDayTimeButton();
                    CloseWindow();
                }
            }
            else
            {
                using (var verticalScope = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    GUILayout.Label("Set date time", GUI.skin.label);
                    GUILayout.Label("is only for single player or when host.", GUI.skin.label);
                    GUILayout.Label("Host can activate using ModManager.", GUI.skin.label);
                }
            }
        }

        private void OnClickSetDayTimeButton()
        {
            try
            {
                SetDayTime();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(OnClickSetDayTimeButton)}] throws exception: {exc.Message}");
            }
        }

        public void SetTimeScales()
        {
            try
            {
                TOD_Time m_TOD_Time = MainLevel.Instance.m_TODTime;
                m_TOD_Time.m_DayLengthInMinutes = Convert.ToSingle(m_DayInMinutes);
                m_TOD_Time.m_NightLengthInMinutes = Convert.ToSingle(m_NightInMinutes);
                MainLevel.Instance.m_TODTime = m_TOD_Time;

                ShowHUDBigInfo(
                   HUDBigInfoMessage(
                       $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>Time scales set</color>:\nDay time passes in {m_DayInMinutes} realtime minutes\nand night time in {m_NightInMinutes} realtime minutes."),
                   $"{ModName} Info",
                   HUDInfoLogTextureType.Count.ToString());
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(SetTimeScales)}] throws exception: {exc.Message}");
            }
        }

        public void SetDayTime()
        {
            try
            {
                TOD_Sky m_TOD_Sky = MainLevel.Instance.m_TODSky;
                m_TOD_Sky.Cycle.Day = Convert.ToInt32(m_Day);
                m_TOD_Sky.Cycle.Hour = Convert.ToSingle(m_Hour);
                m_TOD_Sky.Cycle.Month = Convert.ToInt32(m_Month);
                m_TOD_Sky.Cycle.Year = Convert.ToInt32(m_Year);
                MainLevel.Instance.m_TODSky = m_TOD_Sky;

                MainLevel.Instance.SetTimeConnected(m_TOD_Sky.Cycle);
                MainLevel.Instance.UpdateCurentTimeInMinutes();

                ShowHUDBigInfo(
                    HUDBigInfoMessage(
                        $"<color=#{ColorUtility.ToHtmlStringRGBA(Color.green)}>Date time set</color>:\nToday is {m_Day}/{m_Month}/{m_Year}\nat {m_Hour} o'clock."),
                    $"{ModName} Info",
                    HUDInfoLogTextureType.Count.ToString());
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(SetDayTime)}] throws exception: {exc.Message}");
            }
        }
    }
}
