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

        private bool showUI = false;

        public Rect ModTimeScreen = new Rect(10f, 340f, 450f, 150f);

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        [Tooltip("Time scale in real minutes")]
        private static string m_DayInMinutes = "20";
        [Tooltip("Time scale in real minutes")]
        private static string m_NightInMinutes = "10";
        [Tooltip("Day")]
        private static string m_Day = MainLevel.Instance.m_TODSky.Cycle.Day.ToString();
        [Tooltip("Month")]
        private static string m_Month = MainLevel.Instance.m_TODSky.Cycle.Month.ToString();
        [Tooltip("Year")]
        private static string m_Year = MainLevel.Instance.m_TODSky.Cycle.Year.ToString();
        [Tooltip("Hour")]
        private static string m_Hour = MainLevel.Instance.m_TODSky.Cycle.Hour.ToString();

        public static bool TestRainFXInfoShown { get; private set; }

        public static bool TestRainFxEnabled { get; private set; }

        private void UpdateRainTest()
        {
            if (RainManager.Get().IsRain())
            {
                ShowHUDBigInfo("Testing rain FX - check beneath roofs!", $"{ModName} Info", HUDInfoLogTextureType.Count.ToString());
                TestRainFXInfoShown = true;
                RainProofing();
            }
            else
            {
                TestRainFXInfoShown = false;
            }
        }

        public void RainProofing()
        {
            try
            {
                List<Construction> roofs = Construction.s_AllRoofs;
                foreach (Construction roof in roofs)
                {
                    Vector3Int roofPosition = Vector3Int.FloorToInt(roof.transform.position);
                    ModAPI.Log.Write($"Roof location x: {roofPosition.x} y: {roofPosition.y} z: {roofPosition.z}");
                    //RainCutter roofCutter = new RainCutter
                    //{
                    //    m_Type = RainCutterType.Tent
                    //};
                    //((RainCutterExtended)roofCutter).SetBoxCollider(roof.m_BoxCollider);
                    //RainManager.Get().RegisterRainCutter(((RainCutterExtended)roofCutter));
                }
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{ModName}.{ModName}:{nameof(RainProofing)}] throws exception: {exc.Message}");
            }
        }

        public bool IsModActiveForMultiplayer => FindObjectOfType(typeof(ModManager.ModManager)) != null && ModManager.ModManager.AllowModsForMultiplayer;

        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public ModTime()
        {
            useGUILayout = true;
            s_Instance = this;
        }

        public static ModTime Get()
        {
            return s_Instance;
        }

        public void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)hUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        public void ShowHUDBigInfo(string text, string header, string textureName)
        {
            HUDManager hUDManager = HUDManager.Get();

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
                if (!showUI)
                {
                    InitData();
                    EnableCursor(true);
                }
                // toggle menu
                showUI = !showUI;
                if (!showUI)
                {
                    EnableCursor(false);
                }
            }
        }

        private void OnGUI()
        {
            if (showUI)
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
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
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
                    CreateSetTimeScalesButton();
                }
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    GUILayout.Label("Set the current date and time in game.", GUI.skin.label);
                    GUILayout.Label("Day starts at 5AM. Night starts at 10PM", GUI.skin.label);
                }
                using (var horizontalScope = new GUILayout.HorizontalScope(GUI.skin.box))
                {
                    //GUILayout.Label("Day: ", GUI.skin.label);
                    m_Day = GUILayout.TextField(m_Day, GUI.skin.textField);
                    //GUILayout.Label("Month: ", GUI.skin.label);
                    m_Month = GUILayout.TextField(m_Month, GUI.skin.textField);
                    //GUILayout.Label("Year: ", GUI.skin.label);
                    m_Year = GUILayout.TextField(m_Year, GUI.skin.textField);
                    //GUILayout.Label("Hour: ", GUI.skin.label);
                    m_Hour = GUILayout.TextField(m_Hour, GUI.skin.textField);
                    CreateSetDateTimeButton();
                }
            }
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 10000f));
        }

        private void CloseWindow()
        {
            showUI = false;
            EnableCursor(false);
        }

        private void CreateSetTimeScalesButton()
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

        private void CreateSetDateTimeButton()
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
                    $"<color #3a7a24>Time scales set</color>: Day time passes in {m_DayInMinutes} minutes and night time in {m_NightInMinutes} minutes",
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
                    $"<color #3a7a24>Current in game date set</color>: {m_Day}/{m_Month}/{m_Year}",
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
