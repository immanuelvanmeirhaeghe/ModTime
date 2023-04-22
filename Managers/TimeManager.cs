using CJTools;
using ModTime.Data;
using ModTime.Data.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ModTime.Managers
{
    public class TimeManager : MonoBehaviour
    {
        private static TimeManager Instance;
        private static Watch LocalWatch;
        private static readonly string ModuleName = nameof(TimeManager);
        public bool UseDevice { get; set; } = false;
        public bool EnableTimeHUD { get; set; } = false;
        public string DayTimeScaleInMinutes { get; set; } = "20";
        public string NightTimeScaleInMinutes { get; set; } = "10";
   
        public int SelectedTimeScaleModeIndex { get; set; } = 0;
    
        public string SystemInfoServerRestartMessage(Color? color = null)
        {
            return SystemInfoChatMessage("<color=#" + (color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.yellow)) + "><b>Attention all players!</b></color> \nGame host " + GetHostPlayerName() + " is restarting the server. \nYou will be automatically rejoining in a short while. Please hold.", color);
        }
        public string SystemInfoChatMessage(string content, Color? color = null)
        {
            return "<color=#" + (color.HasValue ? ColorUtility.ToHtmlStringRGBA(color.Value) : ColorUtility.ToHtmlStringRGBA(Color.red)) + ">System</color>:\n" + content;
        }
        public string GetHostPlayerName()
        {
            return P2PSession.Instance.GetSessionMaster().GetDisplayName();
        }
        public bool IsHostManager 
            => ReplTools.AmIMaster();
        public bool IsHostInCoop
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
        public bool IsHostWithPlayersInCoop
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
        public TimeScaleModes SelectedTimeScaleMode { get; set; } = TimeScaleModes.Normal;
        public float TimeScaleFactor { get; set; } = 0f;
        public float SlowMotionFactor { get; set; } = 1f;
        private float WantedSlowMotionFactor { get; set; } = 1f;
        private float ChangeSlowMotionTime { get; set; } = 0f;
        public float CurentTimeInMinutes { get; set; } = 0f;
        public bool IsModEnabled { get; set; } = false;

        public TimeManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static TimeManager Get()
        {
            return Instance;
        }

        private void Start()
        {
        }

        private void Update()
        {
           if (IsModEnabled)
           {
                InitData();
                UpdateSlowMotion();
                UpdateTimeScale();             
            }
        }

        private void InitData()
        {
            LocalWatch = Watch.Get();
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

        public bool SetTimeScalesInMinutes(int dayLengthInMinutes, int nightLengthInMinutes)
        {
            try
            {
                if (dayLengthInMinutes >= 12 * 60 || dayLengthInMinutes <= 0)
                {
                    dayLengthInMinutes = 12 * 60;
                }
                if (nightLengthInMinutes >= 12 * 60 || nightLengthInMinutes <= 0)
                {
                    nightLengthInMinutes = 12 * 60;
                }
                if (dayLengthInMinutes > 0f && nightLengthInMinutes > 0f)
                {
                    TOD_Time time = MainLevel.Instance.m_TODTime;
                    time.m_DayLengthInMinutes = dayLengthInMinutes;
                    time.m_NightLengthInMinutes = nightLengthInMinutes;
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
            DateTime dateTime = MainLevel.Instance.m_TODSky.Cycle.DateTime;
            if (IsNight())
            {
                dateTime = dateTime.AddDays(1);
                SetDayTime(dateTime.Day, dateTime.Month, dateTime.Year, 5, 1);
                MainLevel.Instance.m_TODSky.Cycle.DateTime = dateTime;
                return DayCycles.Daytime.ToString();
            }
            else
            {
                SetDayTime(dateTime.Day, dateTime.Month, dateTime.Year, 22, 1);              
                return DayCycles.Night.ToString();
            }          
        }
              
        public void SetDayTime(int day, int month, int year, int hour, int minutes)
        {
            TOD_CycleParameters skyCycle = MainLevel.Instance.m_TODSky.Cycle;
            skyCycle.Day = day;
            skyCycle.Month = month;
            skyCycle.Year = year;
            MainLevel.Instance.m_TODSky.Cycle = skyCycle;
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
                    SelectedTimeScaleMode = TimeScaleModes.Normal;
                    TimeScaleModeIndex = (int)SelectedTimeScaleMode;
                    break;
                case 1:
                    SelectedTimeScaleMode = TimeScaleModes.Medium;
                    TimeScaleModeIndex = (int)SelectedTimeScaleMode;
                    break;
                case 2:
                    SelectedTimeScaleMode = TimeScaleModes.High;
                    TimeScaleModeIndex = (int)SelectedTimeScaleMode;
                    break;
                case 3:
                    SelectedTimeScaleMode = TimeScaleModes.Paused;
                    TimeScaleModeIndex = (int)SelectedTimeScaleMode;
                    break;
                case 4:
                    SelectedTimeScaleMode = TimeScaleModes.Custom;
                    TimeScaleModeIndex = (int)SelectedTimeScaleMode;
                    break;
                default:
                    SelectedTimeScaleMode = TimeScaleModes.Normal;
                    TimeScaleModeIndex = (int)SelectedTimeScaleMode;
                    break;
            }
            MainLevel.Instance.SetTimeScaleMode(mode);
        }

        public float GetSlowMotionFactor()
        {
            if (SlowMotionFactor != WantedSlowMotionFactor)
            {
                UpdateSlowMotion();
            }
            return SlowMotionFactor;
        }

        private void UpdateSlowMotion()
        {
            if (SlowMotionFactor != WantedSlowMotionFactor)
            {
                float b = Time.unscaledTime - ChangeSlowMotionTime;
                SlowMotionFactor = CJTools.Math.GetProportionalClamp(SlowMotionFactor, WantedSlowMotionFactor, b, 0f, 1f);
            }
        }

        private void UpdateTimeScale()
        {
            MainLevel.Instance.UpdateTimeScale();
        }

        public bool IsNight()
        {
            return MainLevel.Instance.IsNight();
        }

        public string GetCurrentTime()
        {
            TOD_CycleParameters skyCycle = MainLevel.Instance.m_TODSky.Cycle;
            float hh = skyCycle.Hour;
            int mm = (int)((skyCycle.Hour - hh) * 60f);
            string timeWatch = $"{(int)hh}:{mm:00}";
            return $"{timeWatch}";
        }

        public string GetCurrentDate()
        {
            TOD_CycleParameters skyCycle = MainLevel.Instance.m_TODSky.Cycle;          
            string dateWatch = $"{skyCycle.Year}-{ skyCycle.Month}-{skyCycle.Day}";
            return $"{dateWatch}";
        }

        public float GetTimeScaleFactor(TimeScaleModes timeScaleMode)
        {
            float factor;
            switch (timeScaleMode)
            {
                case TimeScaleModes.Normal:
                    factor = 1f;
                    break;
                case TimeScaleModes.Medium:
                    factor = 10f;
                    break;
                case TimeScaleModes.High:
                    factor = 0.1f;
                    break;
                case TimeScaleModes.Paused:
                    factor = 0f;
                    break;
                case TimeScaleModes.Custom:
                    factor = SlowMotionFactor;
                    break;
                default:
                    factor = 1f;
                    break;
            }
            return factor;
        }

        public string[] GetTimeScaleModes()
        {
            return Enum.GetNames(typeof(TimeScaleModes));
        }

        public float GetTimeProgressSpeed()
        {
            return Time.timeScale;
        }

    }
}
