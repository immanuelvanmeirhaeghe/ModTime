using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }

        public bool StartRain()
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

        public bool StopRain()
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

        public bool IsRainFallingNow()
        {
            if (IsModEnabled)
            {
                return LocalRainManager.IsRain();
            }
            return false;
        }

        public string GetCurrentWeatherInfo()
        {
            try
            {
                return $"{(IsRainFallingNow() ? "Raining" : "Dry")} weather";
            }
            catch (Exception exc)
            {
                HandleException(exc, $"{nameof(StopRain)}");
                return string.Empty;
            }
        }
    }
}
