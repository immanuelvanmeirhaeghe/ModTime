using System;
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

        private bool showUI = false;

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        private static string m_DayInMinutes = "20";
        private static string m_NightInMinutes = "10";

        /// <summary>
        /// ModAPI required security check to enable this mod feature for multiplayer.
        /// See <see cref="ModManager"/> for implementation.
        /// Based on request in chat: use  !requestMods in chat as client to request the host to activate mods for them.
        /// </summary>
        /// <returns>true if enabled, else false</returns>
        public bool IsModActiveForMultiplayer => FindObjectOfType(typeof(ModManager.ModManager)) != null ? ModManager.ModManager.AllowModsForMultiplayer : false;

        public bool IsModActiveForSingleplayer => ReplTools.AmIMaster();

        public ModTime()
        {
            s_Instance = this;
        }

        public static ModTime Get()
        {
            return s_Instance;
        }

        public static void ShowHUDInfoLog(string itemID, string localizedTextKey)
        {
            Localization localization = GreenHellGame.Instance.GetLocalization();
            ((HUDMessages)hUDManager.GetHUD(typeof(HUDMessages))).AddMessage(localization.Get(localizedTextKey) + "  " + localization.Get(itemID));
        }

        public static void ShowHUDBigInfo(string text, string header, string textureName)
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

        private static void EnableCursor(bool enabled = false)
        {
            CursorManager.Get().ShowCursor(enabled, false);
            player = Player.Get();

            if (enabled)
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
                InitModUI();
            }
        }

        private static void InitData()
        {
            hUDManager = HUDManager.Get();
            itemsManager = ItemsManager.Get();
            player = Player.Get();
        }

        private static void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitModUI()
        {
            GUI.Box(new Rect(10f, 340f, 450f, 150f), "ModTime UI - Press HOME to open/close", GUI.skin.window);

            GUI.Label(new Rect(30f, 360f, 200f, 20f), "Day time (in minutes of real time)", GUI.skin.label);
            m_DayInMinutes = GUI.TextField(new Rect(280f, 360f, 20f, 20f), m_DayInMinutes, GUI.skin.textField);

            GUI.Label(new Rect(30f, 380f, 200f, 20f), "Night time (in minutes of real time)", GUI.skin.label);
            m_NightInMinutes = GUI.TextField(new Rect(280f, 380f, 20f, 20f), m_NightInMinutes, GUI.skin.textField);

            CreateSetTimeScalesButton();

            if (GUI.Button(new Rect(280f, 460f, 150f, 20f), "CANCEL", GUI.skin.button))
            {
                showUI = false;
                EnableCursor(false);
            }
        }

        private void CreateSetTimeScalesButton()
        {
            if (IsModActiveForSingleplayer || IsModActiveForMultiplayer)
            {
                if (GUI.Button(new Rect(280f, 400f, 150f, 20f), "Set time scales", GUI.skin.button))
                {
                    OnClickSetTimeScalesButton();
                    showUI = false;
                    EnableCursor(false);
                }
            }
            else
            {
                GUI.Label(new Rect(30f, 400f, 330f, 20f), "Set time scales", GUI.skin.label);
                GUI.Label(new Rect(30f, 420f, 330f, 20f), "is only for single player or when host", GUI.skin.label);
                GUI.Label(new Rect(30f, 440f, 330f, 20f), "Host can activate using ModManager.", GUI.skin.label);
            }
        }

        private static void OnClickSetTimeScalesButton()
        {
            try
            {
                SetTimeScales();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModTime)}.{nameof(ModTime)}:{nameof(OnClickSetTimeScalesButton)}] throws exception: {exc.Message}");
            }
        }

        public static void SetTimeScales()
        {
            try
            {
                TOD_Time m_TOD_Time = MainLevel.Instance.m_TODTime;
                m_TOD_Time.m_DayLengthInMinutes = Convert.ToSingle(m_DayInMinutes);
                m_TOD_Time.m_NightLengthInMinutes = Convert.ToSingle(m_NightInMinutes);

                MainLevel.Instance.m_TODTime = m_TOD_Time;

                ShowHUDBigInfo($"Time scales set: Day time passes in {m_DayInMinutes} minutes and night time in {m_NightInMinutes} minutes", "ModTime Info", HUDInfoLogTextureType.Count.ToString());
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModTime)}.{nameof(ModTime)}:{nameof(SetTimeScales)}] throws exception: {exc.Message}");
            }
        }

    }
}
