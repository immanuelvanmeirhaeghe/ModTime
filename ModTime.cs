using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ModTime
{
    class ModTime : MonoBehaviour
    {
        private static ModTime s_Instance;

        private bool showUI = false;

        private static ItemsManager itemsManager;

        private static Player player;

        private static HUDManager hUDManager;

        public bool IsModTimeActive = false;

        private static float m_DayInMinutesDefault = 20f;
        private static float m_NightInMinutesDefault = 10f;

        private static string m_DayInMinutes = "20";
        private static string m_NightInMinutes = "10";

        /// <summary>
        /// ModAPI required security check to enable this mod feature.
        /// </summary>
        /// <returns></returns>
        public bool IsLocalOrHost => ReplTools.AmIMaster() || !ReplTools.IsCoopEnabled();

        public ModTime()
        {
            IsModTimeActive = true;
            s_Instance = this;
        }

        public static ModTime Get()
        {
            return s_Instance;
        }

        private void Update()
        {
            UpdateTimeSettings();

            if (Input.GetKeyDown(KeyCode.Home))
            {
                if (!showUI)
                {
                    hUDManager = HUDManager.Get();

                    itemsManager = ItemsManager.Get();

                    player = Player.Get();

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

        private static void UpdateTimeSettings()
        {
            if (string.IsNullOrEmpty(m_DayInMinutes))
            {
                m_DayInMinutes = m_DayInMinutesDefault.ToString();
            }
            if (string.IsNullOrEmpty(m_NightInMinutes))
            {
                m_NightInMinutes = m_NightInMinutesDefault.ToString();
            }
        }

        private void OnGUI()
        {
            if (showUI)
            {
                InitData();
                InitModUI();
            }
        }

        private static void InitData()
        {
            hUDManager = HUDManager.Get();

            itemsManager = ItemsManager.Get();

            player = Player.Get();

            InitSkinUI();
        }

        private static void InitSkinUI()
        {
            GUI.skin = ModAPI.Interface.Skin;
        }

        private void InitModUI()
        {
            GUI.Box(new Rect(10f, 10f, 450f, 100f), "ModTime UI", GUI.skin.window);

            GUI.Label(new Rect(30f, 30f, 200f, 20f), "Day time (in minutes of real time)", GUI.skin.label);
            m_DayInMinutes = GUI.TextField(new Rect(250f, 30f, 20f, 20f), m_DayInMinutes, GUI.skin.textField);

            GUI.Label(new Rect(30f, 50f, 200f, 20f), "Night time (in minutes of real time)", GUI.skin.label);
            m_NightInMinutes = GUI.TextField(new Rect(250f, 50f, 20f, 20f), m_DayInMinutes, GUI.skin.textField);

            if (GUI.Button(new Rect(250f, 70f, 150f, 20f), "Set time", GUI.skin.button))
            {
                OnClickSetTimeButton();
                showUI = false;
                EnableCursor(false);
            }
        }

        public static void OnClickSetTimeButton()
        {
            try
            {
                SetTime();
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModTime)}.{nameof(ModTime)}:{nameof(OnClickSetTimeButton)}] throws exception: {exc.Message}");
            }
        }

        public static void SetTime()
        {
            try
            {
                TOD_Time m_TOD_Time = MainLevel.Instance.m_TODTime;
                m_TOD_Time.m_DayLengthInMinutes =  Convert.ToSingle(m_DayInMinutes);
                m_TOD_Time.m_NightLengthInMinutes = Convert.ToSingle(m_NightInMinutes);

                MainLevel.Instance.m_TODTime = m_TOD_Time;

                ShowHUDBigInfo($"Time of day set: Day time passes in {m_DayInMinutes} and night time in {m_NightInMinutes}", "Mod Miner Info", HUDInfoLogTextureType.Count.ToString());
            }
            catch (Exception exc)
            {
                ModAPI.Log.Write($"[{nameof(ModTime)}.{nameof(ModTime)}:{nameof(SetTime)}] throws exception: {exc.Message}");
            }
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

    }
}
