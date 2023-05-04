using System;
using UnityEngine;

namespace ModTime.Managers
{
    public class WeatherManager : MonoBehaviour
    {
        private static WeatherManager Instance;
        private static RainManager LocalRainManager;
        private static readonly string ModuleName = nameof(WeatherManager);
        public bool IsRainEnabled { get; set; } = false;
        public bool IsModEnabled { get; set; } = false;

        public bool WeatherStateRaining { get; set; } = false;

        public WeatherManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static WeatherManager Get() => Instance;

        protected virtual void Start()
        {
            InitData();
        }

        protected virtual void Update()
        {
          if (IsModEnabled)
          {
                InitData();
            }
        }

        protected virtual void InitData()
        {
            LocalRainManager = RainManager.Get();
        }

       protected virtual void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }

        public virtual bool StartRain()
        {
            try
            {
                LocalRainManager.ScenarioStartRain();             
                MainLevel.Instance.EnableAtmosphereAndCloudsUpdate(true);
                return true;
            }
            catch (Exception exc)
            {
                HandleException(exc,nameof(StartRain));
                return false;
            }
        }

        public virtual bool StopRain()
        {
            try
            {
                LocalRainManager.ScenarioStopRain();
                MainLevel.Instance.EnableAtmosphereAndCloudsUpdate(false);
                return true;
            }
            catch (Exception exc)
            {
                HandleException(exc, $"{nameof(StopRain)}");
                return false;
            }
        }

        public virtual bool IsRainFallingNow()
        {
            if (IsModEnabled)
            {
                return LocalRainManager.IsRain();
            }
            return false;
        }

        public virtual string GetCurrentWeatherInfo()
        {
            try
            {
                return $"{(IsRainFallingNow() ? "Raining" : "Dry")} weather";
            }
            catch (Exception exc)
            {
                HandleException(exc, $"{nameof(GetCurrentWeatherInfo)}");
                return string.Empty;
            }
        }
    }
}
