using ModTime.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ModTime.Library
{
    public class TimeManager : MonoBehaviour
    {
        private static TimeManager Instance;
        private static Watch LocalWatch;
        private static readonly string ModuleName = nameof(TimeManager);

        public static string SystemInfoServerRestartMessage(Color? color = null)
        {
            return SystemInfoChatMessage("<color=#" + (color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow)) + "><b>Attention all players!</b></color> \nGame host " + GetHostPlayerName() + " is restarting the server. \nYou will be automatically rejoining in a short while. Please hold.", color);
        }
        public static string SystemInfoChatMessage(string content, Color? color = null)
        {
            return "<color=#" + (color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.red)) + ">System</color>:\n" + content;
        }
        public static string GetHostPlayerName()
        {
            return P2PSession.Instance.GetSessionMaster().GetDisplayName();
        }
        public static bool IsHostManager 
            => ReplTools.AmIMaster();
        public static bool IsHostInCoop
        {
            get
            {
                if (IsHostManager)
                {
                    return ReplTools.IsCoopEnabled();
                }
                return false;
            }
        }
        public static bool IsHostWithPlayersInCoop
        {
            get
            {
                if (IsHostInCoop)
                {
                    return !ReplTools.IsPlayingAlone();
                }
                return false;
            }
        }
        public bool IsWatchInitialized { get; set; } = false;
        public bool WasPausedLastFrame { get; set; } = false;
        public int TimeScaleModeIndex { get; set; } = 0;
        public TimeScaleModes TimeScaleMode { get; set; } = TimeScaleModes.Normal;
        public float TimeScaleFactor { get; set; } = 0f;
        public float SlowMotionFactor { get; set; } = 1f;
        private float WantedSlowMotionFactor { get; set; } = 1f;
        private float ChangeSlowMotionTime { get; set; } = 0f;
        public float CurentTimeInMinutes { get; set; } = 0f;

        public TimeManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static TimeManager Get()
        {
            return Instance;
        }

        public void Start()
        {
            SetModuleReferences();          
        }

        public void Update()
        {
            InitData();
            UpdateSlowMotion();
            UpdateTimeScale();
        }

        private void InitData()
        {
            LocalWatch = Watch.Get();
        }

        private void SetModuleReferences()
        {
          
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception:\n{exc}";
            ModAPI.Log.Write(info);
        }

        public void Pause(bool pause)
        {
            MainLevel.Instance.Pause(pause);
        }

        public float ValidateTimeScaleMinutes(string toValidate)
        {
            if (float.TryParse(toValidate, out var result))
            {
                if (result <= 0f)
                {
                    result = 20f;
                }
                if (result > 60f)
                {
                    result = 60f;
                }
                return result;
            }           
            return -1f;
        }

        public bool SetTimeScalesInMinutes(string dayLengthInMinutes, string nightLengthInMinutes)
        {
            try
            {
                float num = ValidateTimeScaleMinutes(dayLengthInMinutes);
                float num2 = ValidateTimeScaleMinutes(nightLengthInMinutes);
                if (num > 0f && num2 > 0f)
                {
                    TOD_Time time = MainLevel.Instance.m_TODTime;
                    time.m_DayLengthInMinutes = num;
                    time.m_NightLengthInMinutes = num2;
                    MainLevel.Instance.m_TODTime = time;
                    return true;
                }
                else
                {
                   return false;
                }                
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(SetTimeScalesInMinutes));
                return false;
            }
        }

        public string SetToNextDayCycle()
        {
            string daytime = string.Empty;
            DateTime dateTime = MainLevel.Instance.m_TODSky.Cycle.DateTime;
            if (dateTime != DateTime.MinValue)
            {                
                if (IsNight())
                {
                    dateTime = dateTime.AddDays(1);
                    MainLevel.Instance.m_TODSky.Cycle.DateTime = dateTime;
                    SetDayTime(5, 1);
                    daytime = DayCycles.Daytime.ToString();
                }
                else
                {
                    SetDayTime(22, 1);
                    daytime = DayCycles.Night.ToString();
                }
            }
            return daytime;
        }

        public void UseDeviceDateAndTime(bool set)
        {
            try
            {
                TOD_Time time = MainLevel.Instance.m_TODTime;
                time.UseDeviceDate = set;
                time.UseDeviceTime = set;
                MainLevel.Instance.m_TODTime = time;
                SaveAndReload();
            }
            catch (Exception exc)
            {
                HandleException(exc, $"{ModuleName}:{nameof(UseDeviceDateAndTime)}");
            }
        }

        private void SaveAndReload()
        {
            try
            {
                if (IsHostWithPlayersInCoop && ReplTools.CanSaveInCoop())
                {
                    P2PSession.Instance.SendTextChatMessage(SystemInfoServerRestartMessage());                   
                    SaveGame.SaveCoop();
                    if (ReplTools.AmIMaster())
                    {
                        P2PSession.Instance.Restart();
                    }
                }
                if (!IsHostWithPlayersInCoop)
                {
                    SaveGame.Save();
                }
                if (!ReplTools.IsPlayingAlone())
                {
                    SaveGame.Save();
                    MainLevel.Instance.Initialize();
                }         
            }
            catch (Exception exc)
            {
                HandleException(exc, $"{ModuleName}:{nameof(SaveAndReload)}");
            }
        }

        private void CycleTimeScaleSpeedup()
        {
            if (TimeScaleMode == TimeScaleModes.Normal)
            {
                TimeScaleMode = TimeScaleModes.Medium;
            }
            else
            {
                TimeScaleMode = TimeScaleModes.Normal;
            }
        }

        private void CycleTimeScaleSlowdown()
        {
            if (TimeScaleMode == TimeScaleModes.High)
            {
                TimeScaleMode = TimeScaleModes.Normal;
            }
            else
            {
                TimeScaleMode = TimeScaleModes.High;
            }
        }

        public void SetGameDate(int day, int month, int year)
        {
            TOD_CycleParameters skyCycle = MainLevel.Instance.m_TODSky.Cycle;
            skyCycle.Day = day;
            skyCycle.Month = month;
            skyCycle.Year = year;
            MainLevel.Instance.SetTimeConnected(skyCycle);
        }

        public void SetDayTime(int hour, int minutes)
        {
            MainLevel.Instance.SetDayTime(hour, minutes);
        }

        public void SetSlowMotionFactor(float factor)
        {
            WantedSlowMotionFactor = factor;
            ChangeSlowMotionTime = Time.unscaledTime;
            MainLevel.Instance.SetSlowMotionFactor(factor);
        }

        public void SetTimeScaleMode(int mode)
        {
            switch (mode)
            {
                case 0:
                    TimeScaleMode = TimeScaleModes.Normal;
                    TimeScaleModeIndex = (int)TimeScaleMode;
                    break;
                case 1:
                    TimeScaleMode = TimeScaleModes.High;
                    TimeScaleModeIndex = (int)TimeScaleMode;
                    break;
                case 2:
                    TimeScaleMode = TimeScaleModes.Medium;
                    TimeScaleModeIndex = (int)TimeScaleMode;
                    break;
                case 3:
                    TimeScaleMode = TimeScaleModes.Paused;
                    TimeScaleModeIndex = (int)TimeScaleMode;
                    break;
                case 4:
                    TimeScaleMode = TimeScaleModes.Custom;
                    TimeScaleModeIndex = (int)TimeScaleMode;
                    break;
                default:
                    TimeScaleMode = TimeScaleModes.Normal;
                    TimeScaleModeIndex = (int)TimeScaleMode;
                    break;
            }
        }

        private void UpdateSlowMotion()
        {
            if (SlowMotionFactor != WantedSlowMotionFactor)
            {
                float b = Time.unscaledTime - ChangeSlowMotionTime;
                SlowMotionFactor = CJTools.Math.GetProportionalClamp(SlowMotionFactor, WantedSlowMotionFactor, b, 0f, 1f);
            }
        }

        public void UpdateTimeScale()
        {
            float _timeScale = 1f;
            bool can_pause = true;

            if (!ReplTools.IsPlayingAlone())
            {
                ReplTools.ForEachLogicalPlayer(delegate (ReplicatedLogicalPlayer player)
                {
                    if (!player.m_PauseGame)
                    {
                        can_pause = false;
                    }
                });
            }
            
            if (MainLevel.Instance.IsPause() && can_pause)
            {
                if (Time.time != 0f)
                {
                    _timeScale = 0f;
                    MainLevel.Instance.PauseAllAudio(pause: true);
                    WasPausedLastFrame = true;
                }
            }
            else
            {
                switch (TimeScaleMode)
                {
                    case TimeScaleModes.Normal:
                        _timeScale = 1f;
                        break;
                    case TimeScaleModes.Medium:
                        _timeScale *= 10f;
                        break;
                    case TimeScaleModes.High:
                        _timeScale *= 0.1f;
                        break;
                    case TimeScaleModes.Paused:
                        _timeScale = 0f;
                        break;
                    case TimeScaleModes.Custom:
                        _timeScale *= TimeScaleFactor;
                        break;
                    default:
                        _timeScale = 1f;
                        break;
                }                              
                if (Time.timeScale == 0f)
                {
                    MainLevel.Instance.PauseAllAudio(pause: false);
                }
                if (WasPausedLastFrame)
                {
                    MainLevel.Instance.m_LastUnpauseTime = Time.time;
                }
                WasPausedLastFrame = false;
            }
            Time.timeScale = _timeScale * SlowMotionFactor;
        }

        public bool IsNight()
        {
            return MainLevel.Instance.IsNight();
        }

        public string GetCurrentDateAndTime()
        {
            TOD_CycleParameters skyCycle = MainLevel.Instance.m_TODSky.Cycle;
            float hh = skyCycle.Hour;
            int mm = (int)((skyCycle.Hour - hh) * 60f);
            string text = $"{(int)hh}:{mm:00}";
            return $"{skyCycle.Day}/{skyCycle.Month}/{skyCycle.Year} at {text}";
        }
    }
}
