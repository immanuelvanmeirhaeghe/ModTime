using Enums;
using ModTime.Data.Enums;
using ModTime.Data.Player.Condition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace ModTime.Managers
{
    public class HealthManager : MonoBehaviour
    {
        private static readonly string ModuleName = nameof(HealthManager);

        public bool UseDefault { get; set; } = true;
        public bool IsParameterLossBlocked { get; set; } = false;
        public bool IsModEnabled { get; set; } = false;
        public bool HasChanged { get; set; } = false;

        private static HealthManager Instance;
        private static PlayerConditionModule LocalPlayerConditionModule;
        private static FPPController LocalFPPController;
        private static ConsciousnessController LocalConsciousnessController;
        private static InventoryBackpack LocalInventoryBackpack;
        private static PlayerCocaineModule LocalPlayerCocaineModule;
        private static Multipliers LocalMultipliers;

        public NutrientsDepletion ActiveNutrientsDepletionPreset { get; set; } 
        public int ActiveNutrientsDepletionPresetIndex { get; set; }

        public HealthManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static HealthManager Get() => Instance;

        public void Start()
        { 
        }

        private void Update()
        {
            if (IsModEnabled)
            {
                InitData();                                       
            }         
        }

        private void InitData()
        {
            LocalPlayerConditionModule = PlayerConditionModule.Get();
            LocalFPPController = Player.Get().m_FPPController;
            LocalConsciousnessController = ConsciousnessController.Get();
            LocalInventoryBackpack = InventoryBackpack.Get();
            LocalPlayerCocaineModule = PlayerCocaineModule.Get();
            LocalMultipliers = Multipliers.Get();
            ActiveNutrientsDepletionPreset = GetActiveNutrientsDepletionPreset();
            ActiveNutrientsDepletionPresetIndex = (int) ActiveNutrientsDepletionPreset;
        }

        public NutrientsDepletion GetActiveNutrientsDepletionPreset()
        {
            var _ActiveNutrientsDepletionPreset = DifficultySettings.ActivePreset.m_NutrientsDepletion;
            return _ActiveNutrientsDepletionPreset;
        }

        public bool SetActiveNutrientsDepletionPreset(NutrientsDepletion nutrientsDepletion)
        {
            try
            {
                ActiveNutrientsDepletionPreset = nutrientsDepletion;
                DifficultySettings.ActivePreset.m_NutrientsDepletion = ActiveNutrientsDepletionPreset;
                return true;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetActiveNutrientsDepletionPreset));
                return false;
            }
        }

        public string[] GetNutrientsDepletionNames()
        {
            return Enum.GetNames(typeof(NutrientsDepletion));
        }

        public void UpdateNutrition(bool usedefault = true)
        {
            if(!usedefault)
            {
                UpdateCustomNutrition();
            }
            else
            {
                UpdateDefaultNutrition();
            }         
        }

        private static void UpdateDefaultNutrition()
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
                    num2 *= m_ParasiteSickness.m_MacroNutricientCarboLossMul * (float)m_ParasiteSickness.m_Level;
                    num3 *= m_ParasiteSickness.m_MacroNutricientFatLossMul * (float)m_ParasiteSickness.m_Level;
                    num4 *= m_ParasiteSickness.m_MacroNutricientProteinsLossMul * (float)m_ParasiteSickness.m_Level;
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

        private void UpdateCustomNutrition()
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
                    num2 *= m_ParasiteSickness.m_MacroNutricientCarboLossMul * (float)m_ParasiteSickness.m_Level;
                    num3 *= m_ParasiteSickness.m_MacroNutricientFatLossMul * (float)m_ParasiteSickness.m_Level;
                    num4 *= m_ParasiteSickness.m_MacroNutricientProteinsLossMul * (float)m_ParasiteSickness.m_Level;
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

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
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
                        mul = UIControlManager.CustomHorizontalSlider(customconditionMul.Value, 0f, customconditionMul.Value + 1f, customconditionMul.Key);
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
                        UIControlManager.CustomHorizontalSlider(conditionMul.Value, 0f, conditionMul.Value + 1f, conditionMul.Key);                       
                    }
                }               
            }
        }

    }

}
