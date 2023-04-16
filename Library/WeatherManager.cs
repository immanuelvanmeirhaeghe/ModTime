using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ModTime.Library
{
    public class WeatherManager : MonoBehaviour
    {
        private static WeatherManager Instance;
        private static RainManager LocalRainManager;
        private static readonly string ModuleName = nameof(WeatherManager);
        public bool IsRainEnabled { get; set; } = false;

        public WeatherManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static WeatherManager Get()
        {
            return Instance;
        }

        public void Start()
        {
            SetModuleReferences();
            InitData();
        }

        private void SetModuleReferences()
        {
          
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
            LocalRainManager.ScenarioStartRain();
            IsRainEnabled = true;
            MainLevel.Instance.EnableAtmosphereAndCloudsUpdate(true);
            return true;
        }

        public bool StopRain()
        {
            LocalRainManager.ScenarioStopRain();
            IsRainEnabled = false;
            MainLevel.Instance.EnableAtmosphereAndCloudsUpdate(false);
            return true;
        }

    }
}
