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
      
        private static readonly string ModuleName = nameof(TimeManager);
        private static Color DefaultColor = GUI.color;
        private static Color DefaultContentColor = GUI.contentColor;
        private static Color DefaultBackGroundColor = GUI.backgroundColor;

        private static Watch LocalWatch;

        public bool UseDevice { get; set; } = false;
        public bool IsHUDTimeEnabled { get; set; } = false;
        public string DayTimeScaleInMinutes { get; set; } = "20";
        public string NightTimeScaleInMinutes { get; set; } = "10";

        public TimeScaleModes SelectedTimeScaleMode { get; set; } = TimeScaleModes.Normal;
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
        public TimeScaleModes TimeScaleMode { get; set; } = TimeScaleModes.Normal;        
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

        protected virtual void Start()
        {
        }

        protected virtual void Update()
        {
           if (IsModEnabled)
           {
                InitData();
                UpdateSlowMotion();
                UpdateTimeScale();
                UpdateCurentTimeInMinutes();
            }
        }

        protected virtual void InitData()
        {
            LocalWatch = Watch.Get();
        }

        protected virtual void UpdateSlowMotion()
        {
            if (SlowMotionFactor != WantedSlowMotionFactor)
            {
                float b = Time.unscaledTime - ChangeSlowMotionTime;
                SlowMotionFactor = CJTools.Math.GetProportionalClamp(SlowMotionFactor, WantedSlowMotionFactor, b, 0f, 1f);
            }
        }

        protected virtual void UpdateTimeScale()
        {
            MainLevel.Instance.UpdateTimeScale();
        }

        protected virtual void UpdateCurentTimeInMinutes()
        {
            var m_TODSky = MainLevel.Instance.m_TODSky;
            CurentTimeInMinutes =
                Mathf.Floor(m_TODSky.Cycle.Year - 2016) * 12f * 30f * 24f * 60f +
                Mathf.Floor(m_TODSky.Cycle.Month) * 30f * 24f * 60f +
                Mathf.Floor(m_TODSky.Cycle.Day) * 24f * 60f +
                Mathf.Floor(m_TODSky.Cycle.Hour) * 60f +
                m_TODSky.Cycle.DateTime.Minute +
                m_TODSky.Cycle.DateTime.Second / 60f;
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
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
            TOD_CycleParameters skyCycle = MainLevel.Instance.m_TODSky.Cycle;
            if (IsNight())
            {
                skyCycle.DateTime.AddDays(1);
                SetDayTime(skyCycle.Day, skyCycle.Month, skyCycle.Year, 5, 1);              
                return DayCycles.Daytime.ToString();
            }
            else
            {
                SetDayTime(skyCycle.Day, skyCycle.Month, skyCycle.Year, 22, 1);            
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

        private void CycleTimeScaleSpeedup()
        {
            if (TimeScaleMode == TimeScaleModes.Normal)
            {
                TimeScaleMode = TimeScaleModes.Fast;
            }
            else
            {
                TimeScaleMode = TimeScaleModes.Normal;
            }
            TimeScaleModeIndex = (int)TimeScaleMode;
            MainLevel.Instance.SetTimeScaleMode(TimeScaleModeIndex);
        }

        private void CycleTimeScaleSlowdown()
        {
            if (TimeScaleMode == TimeScaleModes.VeryFast)
            {
                TimeScaleMode = TimeScaleModes.Normal;
            }
            else
            {
                TimeScaleMode = TimeScaleModes.VeryFast;
            }
            TimeScaleModeIndex = (int)TimeScaleMode;
            MainLevel.Instance.SetTimeScaleMode(TimeScaleModeIndex);
        }

        public void SetSelectedTimeScaleMode(int mode)
        {
            switch (mode)
            {
                case 0:
                    SelectedTimeScaleMode = TimeScaleModes.Normal;                   
                    break;
                case 1:
                    SelectedTimeScaleMode = TimeScaleModes.Fast;
                    break;
                case 2:
                    SelectedTimeScaleMode = TimeScaleModes.VeryFast;
                    break;
                case 3:
                    SelectedTimeScaleMode = TimeScaleModes.Paused;
                    break;
                case 4:
                    SelectedTimeScaleMode = TimeScaleModes.Custom;
                    break;
                default:
                    SelectedTimeScaleMode = TimeScaleModes.Normal;                    
                    break;
            }
            SelectedTimeScaleModeIndex = (int)SelectedTimeScaleMode;
            TimeScaleMode = SelectedTimeScaleMode;
            TimeScaleModeIndex = SelectedTimeScaleModeIndex;
            MainLevel.Instance.SetTimeScaleMode(TimeScaleModeIndex);
        }

        public float GetSlowMotionFactor()
        {
            if (SlowMotionFactor != WantedSlowMotionFactor)
            {
                UpdateSlowMotion();
            }
            return SlowMotionFactor;
        }

        public bool IsNight()
        {
            return MainLevel.Instance.IsNight();
        }

        public float GetCurrentTimeInMinutes()
        {
            CurentTimeInMinutes = MainLevel.Instance.GetCurrentTimeMinutes();
            return CurentTimeInMinutes;
        }

        public string HUDTimeString()
        {
            TOD_Sky m_TODSky = MainLevel.Instance.m_TODSky;
            float hh = m_TODSky.Cycle.Hour;
            int mm = m_TODSky.Cycle.DateTime.Minute;
            int ss = m_TODSky.Cycle.DateTime.Second;
            string hudTimeString = $"{(int)hh}:{mm:00}:{ss:00}";
            return $"{hudTimeString}";
        }

        public string HUDDateString()
        {
            TOD_CycleParameters skyCycle = MainLevel.Instance.m_TODSky.Cycle;          
            string hudDateString = $"{skyCycle.Year}-{ skyCycle.Month}-{skyCycle.Day}";
            return $"{hudDateString}";
        }

        public float GetTimeScaleFactor(TimeScaleModes timeScaleMode)
        {
            float factor;
            switch (timeScaleMode)
            {
                case TimeScaleModes.Normal:
                    factor = 1f;
                    break;
                case TimeScaleModes.Fast:
                    factor = 10f;
                    break;
                case TimeScaleModes.VeryFast:
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

        public float CustomHorizontalSlider(float sliderValue, float sliderMinValue, float sliderMaxValue, string labelText)
        {
            GUI.contentColor = DefaultContentColor;       
            using (var sliderHScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label($"{labelText} ({(float)System.Math.Round(sliderValue, 2, MidpointRounding.ToEven)})");
                sliderValue = GUILayout.HorizontalSlider(sliderValue, sliderMinValue, sliderMaxValue);
                return sliderValue;
            }
        }

    }
}
