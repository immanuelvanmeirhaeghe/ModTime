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

        public WeatherManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static WeatherManager Get() => Instance;

        public void Start()
        {                    
        }

        public void Update()
        {
          if (IsModEnabled)
          {
                InitData();
            }
        }

        private void InitData()
        {
            LocalRainManager = RainManager.Get();
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception:\n{exc}";
            ModAPI.Log.Write(info);
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
                HandleException(exc, $"{ModuleName}:{nameof(StartRain)}");
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
                HandleException(exc, $"{ModuleName}:{nameof(StopRain)}");
                return false;
            }
        }

    }
}
