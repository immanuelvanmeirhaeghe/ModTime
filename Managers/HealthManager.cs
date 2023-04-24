using Enums;
using ModTime.Data.Enums;
using ModTime.Data.Player.Condition;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static GameSettings;
using UnityStandardAssets.ImageEffects;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace ModTime.Managers
{
    public class HealthManager : MonoBehaviour
    {
        private static readonly string ModuleName = nameof(HealthManager);
        public static readonly string SavedSettingsFileName = $"{ModuleName}.sav";
        private static Color DefaultColor = GUI.color;
        private static Color DefaultContentColor = GUI.contentColor;
        private static Color DefaultBackGroundColor = GUI.backgroundColor;

        private static HealthManager Instance;
        private static PlayerConditionModule LocalPlayerConditionModule;
        private static FPPController LocalFPPController;
        private static ConsciousnessController LocalConsciousnessController;
        private static InventoryBackpack LocalInventoryBackpack;
        private static PlayerCocaineModule LocalPlayerCocaineModule;
        private static Multipliers LocalMultipliers;

        public bool SettingsLoaded { get; set; } = false;
        public bool IsModEnabled { get; set; } = false;
        public bool HasChanged { get; set; } = false;
        public NutrientsDepletion SelectedActiveNutrientsDepletionPreset { get; set; } = default;
        public int SelectedActiveNutrientsDepletionPresetIndex { get; set; } = 0;

        public bool UseDefault { get; set; } = true;
        public bool IsParameterLossBlocked { get; set; } = false;
        public int ActiveNutrientsDepletionPresetIndex { get; set; }
        public NutrientsDepletion ActiveNutrientsDepletionPreset { get; set; }

        public HealthManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static HealthManager Get() => Instance;

        protected virtual void Start()
        {
            try
            {
                SettingsLoaded = LoadSettings();
            }
            catch (Exception exc)
            {
                HandleException(exc, $"{nameof(Start)}");
               
            }
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
            LocalPlayerConditionModule = PlayerConditionModule.Get();
            LocalFPPController = Player.Get().m_FPPController;
            LocalConsciousnessController = ConsciousnessController.Get();
            LocalInventoryBackpack = InventoryBackpack.Get();
            LocalPlayerCocaineModule = PlayerCocaineModule.Get();
            LocalMultipliers = Multipliers.Get();          
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }

        public NutrientsDepletion GetActiveNutrientsDepletionPreset()
        {
            var _ActiveNutrientsDepletionPreset = DifficultySettings.ActivePreset.m_NutrientsDepletion;
            ActiveNutrientsDepletionPreset = _ActiveNutrientsDepletionPreset;
            return _ActiveNutrientsDepletionPreset;
        }

        public bool SetActiveNutrientsDepletionPreset(int nutrientsDepletionIndex)
        {
            try
            {
                NutrientsDepletion nutrientsDepletionPreset = default;

                switch (nutrientsDepletionIndex)
                {
                    case 0:
                        nutrientsDepletionPreset = NutrientsDepletion.Off;                        
                        break;
                    case 1:
                        nutrientsDepletionPreset = NutrientsDepletion.Low;                        
                        break;
                    case 2:
                        nutrientsDepletionPreset = NutrientsDepletion.Normal;                        
                        break;
                    case 3:
                        nutrientsDepletionPreset = NutrientsDepletion.High;
                        break;
                    default:
                        nutrientsDepletionPreset = GetActiveNutrientsDepletionPreset();                        
                        break;
                }
                ActiveNutrientsDepletionPreset = nutrientsDepletionPreset;
                ActiveNutrientsDepletionPresetIndex = (int)nutrientsDepletionPreset;
                DifficultySettings.ActivePreset.m_NutrientsDepletion = ActiveNutrientsDepletionPreset;
                return true;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(SetActiveNutrientsDepletionPreset));
                return false;
            }
        }

        public string[] GetNutrientsDepletionNames()
        {
            return Enum.GetNames(typeof(NutrientsDepletion));
        }

        public virtual void UpdateNutrition(bool usedefault = true)
        {
            if (!usedefault)
            {
                UpdateCustomNutrition();
            }
            else
            {
                UpdateDefaultNutrition();
            }
        }

        protected virtual void UpdateDefaultNutrition()
        {
            ParasiteSickness m_ParasiteSickness = default;
            if (m_ParasiteSickness == null)
            {
                m_ParasiteSickness = (ParasiteSickness)PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.ParasiteSickness);
            }
            if (ScenarioManager.Get().IsDream() || Cheats.m_GodMode || LocalPlayerConditionModule.GetParameterLossBlocked() || Player.Get().IsDialogMode())
            {
                return;
            }
            if (!LocalFPPController)
            {
                return;
            }
            NutrientsDepletion nutrientsDepletion = DifficultySettings.ActivePreset.m_NutrientsDepletion;
            if (nutrientsDepletion != 0)
            {
                WeaponController weaponController = Player.Get().m_WeaponController;
                bool flag = false;
                if ((bool)weaponController && weaponController.IsAttack())
                {
                    flag = true;
                }
                if (!flag && (bool)Player.Get().GetCurrentItem(Hand.Right) && Player.Get().GetCurrentItem(Hand.Right).m_Info.IsHeavyObject())
                {
                    flag = true;
                }
                float num = Time.deltaTime;
                if (ConsciousnessController.Get().IsUnconscious())
                {
                    num = Player.GetUnconsciousTimeFactor();
                }
                float num2 = 1f;
                float num3 = 1f;
                float num4 = 1f;
                if (LocalFPPController.IsRunning())
                {
                    num2 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionRunMul));
                    num3 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionRunMul));
                    num4 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionRunMul));
                }
                if (flag)
                {
                    num2 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionActionMul));
                    num3 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionActionMul));
                    num4 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionActionMul));
                }
                if (LocalPlayerConditionModule.IsNutritionCarboCriticalLevel())
                {
                    num3 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionMulNoCarbs));
                    num4 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionMulNoCarbs));
                }
                if (InventoryBackpack.Get().IsCriticalOverload())
                {
                    num2 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionWeightCriticalMul));
                    num3 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionWeightCriticalMul));
                    num4 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionWeightCriticalMul));
                }
                else if (InventoryBackpack.Get().IsOverload())
                {
                    num2 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionWeightOverloadMul));
                    num3 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionWeightOverloadMul));
                    num4 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionWeightOverloadMul));
                }
                else
                {
                    num2 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionWeightNormalMul));
                    num3 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionWeightNormalMul));
                    num4 *= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionWeightNormalMul));
                }
                if (m_ParasiteSickness.IsActive())
                {
                    num2 *= m_ParasiteSickness.m_MacroNutricientCarboLossMul * m_ParasiteSickness.m_Level;
                    num3 *= m_ParasiteSickness.m_MacroNutricientFatLossMul * m_ParasiteSickness.m_Level;
                    num4 *= m_ParasiteSickness.m_MacroNutricientProteinsLossMul * m_ParasiteSickness.m_Level;
                }
                switch (nutrientsDepletion)
                {
                    case NutrientsDepletion.Normal:
                        {
                            float s_NormalModeLossMul = GreenHellGame.s_NormalModeLossMul;
                            num2 *= s_NormalModeLossMul;
                            num3 *= s_NormalModeLossMul;
                            num4 *= s_NormalModeLossMul;
                            break;
                        }
                    case NutrientsDepletion.Low:
                        {
                            float s_EasyModeLossMul = GreenHellGame.s_EasyModeLossMul;
                            num2 *= s_EasyModeLossMul;
                            num3 *= s_EasyModeLossMul;
                            num4 *= s_EasyModeLossMul;
                            break;
                        }
                }
                LocalPlayerConditionModule.m_NutritionCarbo -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionPerSecond)) * num * num2 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_CarboConsumptionMul : 1f);
                LocalPlayerConditionModule.m_NutritionCarbo = Mathf.Clamp(LocalPlayerConditionModule.GetNutritionCarbo(), 0f, LocalPlayerConditionModule.GetMaxNutritionCarbo());
                LocalPlayerConditionModule.m_NutritionFat -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionPerSecond)) * num * num3 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_FatConsumptionMul : 1f);
                LocalPlayerConditionModule.m_NutritionFat = Mathf.Clamp(LocalPlayerConditionModule.GetNutritionFat(), 0f, LocalPlayerConditionModule.GetMaxNutritionFat());
                LocalPlayerConditionModule.m_NutritionProteins -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionPerSecond)) * num * num4 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_ProteinsConsumptionMul : 1f);
                LocalPlayerConditionModule.m_NutritionProteins = Mathf.Clamp(LocalPlayerConditionModule.GetNutritionProtein(), 0f, LocalPlayerConditionModule.GetMaxNutritionProtein());
            }
        }

        protected virtual void UpdateCustomNutrition()
        {
            ParasiteSickness m_ParasiteSickness = default;
            if (m_ParasiteSickness == null)
            {
                m_ParasiteSickness = (ParasiteSickness)PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.ParasiteSickness);
            }
            if (ScenarioManager.Get().IsDream() || Cheats.m_GodMode || LocalPlayerConditionModule.GetParameterLossBlocked() || Player.Get().IsDialogMode())
            {
                return;
            }
            if (!LocalFPPController)
            {
                return;
            }
            NutrientsDepletion nutrientsDepletion = DifficultySettings.ActivePreset.m_NutrientsDepletion;
            if (nutrientsDepletion != 0)
            {
                WeaponController weaponController = Player.Get().m_WeaponController;
                bool flag = false;
                if ((bool)weaponController && weaponController.IsAttack())
                {
                    flag = true;
                }
                if (!flag && (bool)Player.Get().GetCurrentItem(Hand.Right) && Player.Get().GetCurrentItem(Hand.Right).m_Info.IsHeavyObject())
                {
                    flag = true;
                }
                float num = Time.deltaTime;
                if (ConsciousnessController.Get().IsUnconscious())
                {
                    num = Player.GetUnconsciousTimeFactor();
                }
                float num2 = 1f;
                float num3 = 1f;
                float num4 = 1f;
                if (LocalFPPController.IsRunning())
                {
                    num2 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionRunMul));
                    num3 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionRunMul));
                    num4 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionRunMul));
                }
                if (flag)
                {
                    num2 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionActionMul));
                    num3 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionActionMul));
                    num4 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionActionMul));
                }
                if (LocalPlayerConditionModule.IsNutritionCarboCriticalLevel())
                {
                    num3 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionMulNoCarbs));
                    num4 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionMulNoCarbs));
                }
                if (InventoryBackpack.Get().IsCriticalOverload())
                {
                    num2 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionWeightCriticalMul));
                    num3 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionWeightCriticalMul));
                    num4 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionWeightCriticalMul));
                }
                else if (InventoryBackpack.Get().IsOverload())
                {
                    num2 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionWeightOverloadMul));
                    num3 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionWeightOverloadMul));
                    num4 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionWeightOverloadMul));
                }
                else
                {
                    num2 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionWeightNormalMul));
                    num3 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionWeightNormalMul));
                    num4 *= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionWeightNormalMul));
                }
                if (m_ParasiteSickness.IsActive())
                {
                    num2 *= m_ParasiteSickness.m_MacroNutricientCarboLossMul * m_ParasiteSickness.m_Level;
                    num3 *= m_ParasiteSickness.m_MacroNutricientFatLossMul * m_ParasiteSickness.m_Level;
                    num4 *= m_ParasiteSickness.m_MacroNutricientProteinsLossMul * m_ParasiteSickness.m_Level;
                }
                switch (nutrientsDepletion)
                {
                    case NutrientsDepletion.Normal:
                        {
                            float s_NormalModeLossMul = GreenHellGame.s_NormalModeLossMul;
                            num2 *= s_NormalModeLossMul;
                            num3 *= s_NormalModeLossMul;
                            num4 *= s_NormalModeLossMul;
                            break;
                        }
                    case NutrientsDepletion.Low:
                        {
                            float s_EasyModeLossMul = GreenHellGame.s_EasyModeLossMul;
                            num2 *= s_EasyModeLossMul;
                            num3 *= s_EasyModeLossMul;
                            num4 *= s_EasyModeLossMul;
                            break;
                        }
                }
                LocalPlayerConditionModule.m_NutritionCarbo -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionPerSecond)) * num * num2 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_CarboConsumptionMul : 1f);
                LocalPlayerConditionModule.m_NutritionCarbo = Mathf.Clamp(LocalPlayerConditionModule.GetNutritionCarbo(), 0f, LocalPlayerConditionModule.GetMaxNutritionCarbo());
                LocalPlayerConditionModule.m_NutritionFat -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionPerSecond)) * num * num3 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_FatConsumptionMul : 1f);
                LocalPlayerConditionModule.m_NutritionFat = Mathf.Clamp(LocalPlayerConditionModule.GetNutritionFat(), 0f, LocalPlayerConditionModule.GetMaxNutritionFat());
                LocalPlayerConditionModule.m_NutritionProteins -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionPerSecond)) * num * num4 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_ProteinsConsumptionMul : 1f);
                LocalPlayerConditionModule.m_NutritionProteins = Mathf.Clamp(LocalPlayerConditionModule.GetNutritionProtein(), 0f, LocalPlayerConditionModule.GetMaxNutritionProtein());
            }
        }

        public void UnblockParametersLoss()
        {
            LocalPlayerConditionModule.UnblockParametersLoss();
        }

        public void BlockParametersLoss()
        {
            LocalPlayerConditionModule.BlockParametersLoss();
        }

        public bool GetParameterLossBlocked()
        {
            return LocalPlayerConditionModule.GetParameterLossBlocked();
        }

        public void GetCustomMultiplierSliders()
        {
            if (LocalMultipliers.CustomNutritionMultipliers != null)
            {
                var customordered = LocalMultipliers.CustomNutritionMultipliers.OrderBy(x => x.Key).ToArray();
                int curIdx = 0;

                using (var custmulV = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    foreach (KeyValuePair<string, float> customconditionMul in customordered)
                    {
                        float mul = LocalMultipliers.GetCustomMultiplierValue(customconditionMul.Key);
                        mul = CustomHorizontalSlider(customconditionMul.Value, 0f, customconditionMul.Value + 1f, customconditionMul.Key);
                        if (GUI.changed)
                        {
                            LocalMultipliers.SetCustomNutritionMultiplierValue(customconditionMul.Key, mul);
                        }
                        curIdx++;
                    }
                }
            }
        }

        public void GetDefaultMultiplierSliders()
        {
            if (LocalMultipliers.DefaultNutritionMultipliers != null)
            {
                var ordered = LocalMultipliers.DefaultNutritionMultipliers.OrderBy(x => x.Key).ToList();

                using (var defmulV = new GUILayout.VerticalScope(GUI.skin.box))
                {
                    foreach (KeyValuePair<string, float> conditionMul in ordered)
                    {
                        CustomHorizontalSlider(conditionMul.Value, 0f, conditionMul.Value + 1f, conditionMul.Key);
                    }
                }
            }
        }

        public bool SaveSettings()
        {
            try
            {
                BinaryFormatter saveSettingsFileBinaryFormatter = new BinaryFormatter();
                using (MemoryStream saveSettingsMemoryStream = new MemoryStream())
                {
                    saveSettingsFileBinaryFormatter.Serialize(saveSettingsMemoryStream, UseDefault);
                    saveSettingsFileBinaryFormatter.Serialize(saveSettingsMemoryStream, IsModEnabled);
                    saveSettingsFileBinaryFormatter.Serialize(saveSettingsMemoryStream, IsParameterLossBlocked);
                    saveSettingsFileBinaryFormatter.Serialize(saveSettingsMemoryStream, ActiveNutrientsDepletionPreset);
                    saveSettingsFileBinaryFormatter.Serialize(saveSettingsMemoryStream, ActiveNutrientsDepletionPresetIndex);
                    saveSettingsFileBinaryFormatter.Serialize(saveSettingsMemoryStream, LocalMultipliers.CustomNutritionMultipliers);
                    saveSettingsMemoryStream.Close();
                }

                return true;
            }
            catch (Exception exc)
            {
                HandleException(exc, $"{nameof(SaveSettings)}");
                return false;
            }
        }

        public bool LoadSettings()
        {
            try
            {
                string savedFileName = SavedSettingsFileName;
                if (GreenHellGame.Instance.FileExistsInRemoteStorage(savedFileName))
                {
                    BinaryFormatter loadSettingsFileBinaryFormatter = new BinaryFormatter();
                    int fileSize = GreenHellGame.Instance.m_RemoteStorage.GetFileSize(savedFileName);
                    byte[] array = new byte[fileSize];
                    int num = GreenHellGame.Instance.m_RemoteStorage.FileRead(savedFileName, array, fileSize);
                    if (num != fileSize)
                    {
                        if (num == 0)
                        {
                            ModAPI.Log.Write($"Local file {savedFileName} is missing!!! Skipping reading data.");
                        }
                        else
                        {
                            ModAPI.Log.Write($"Local file {savedFileName} size mismatch!!! Skipping reading data.");
                        }
                        GreenHellGame.Instance.m_RemoteStorage.FileForget(savedFileName);
                    }
                    else
                    {
                        using (MemoryStream loadSettingsMemoryStream = new MemoryStream(array))
                        {
                            UseDefault = (bool)loadSettingsFileBinaryFormatter.Deserialize(loadSettingsMemoryStream);
                            IsModEnabled = (bool)loadSettingsFileBinaryFormatter.Deserialize(loadSettingsMemoryStream);
                            IsParameterLossBlocked = (bool)loadSettingsFileBinaryFormatter.Deserialize(loadSettingsMemoryStream);
                            ActiveNutrientsDepletionPreset = (NutrientsDepletion)loadSettingsFileBinaryFormatter.Deserialize(loadSettingsMemoryStream);
                            ActiveNutrientsDepletionPresetIndex = (int)loadSettingsFileBinaryFormatter.Deserialize(loadSettingsMemoryStream);
                            LocalMultipliers.CustomNutritionMultipliers = (Dictionary<string, float>)loadSettingsFileBinaryFormatter.Deserialize(loadSettingsMemoryStream);
                            loadSettingsMemoryStream.Close();
                        }                       
                    }                  
                }
                return true;
            }
            catch (Exception exc)
            {
                HandleException(exc, $"{nameof(LoadSettings)}");
                return false;
            }
        }

        public float CustomHorizontalSlider(float sliderValue, float sliderMinValue, float sliderMaxValue, string labelText)
        {
            GUI.contentColor = DefaultContentColor;
            if (labelText.ToLower().Contains("carbo"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Carbo);
            }
            if (labelText.ToLower().Contains("fat"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Fat);
            }
            if (labelText.ToLower().Contains("proteins"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Proteins);
            }
            if (labelText.ToLower().Contains("oxygen") || labelText.ToLower().Contains("hydration"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Hydration);
            }
            if (labelText.ToLower().Contains("energy") || labelText.ToLower().Contains("stamina") || labelText.ToLower().Contains("health"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Energy);
            }

            using (var sliderHScope = new GUILayout.HorizontalScope(GUI.skin.box))
            {
                GUILayout.Label($"{labelText} ({(float)Math.Round(sliderValue, 2, MidpointRounding.ToEven)})");
                sliderValue = GUILayout.HorizontalSlider(sliderValue, sliderMinValue, sliderMaxValue);
                return sliderValue;
            }
        }

    }
}