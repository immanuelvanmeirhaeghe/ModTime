using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace ModTime.Library
{
    public class HealthManager : MonoBehaviour
    {
        private static readonly string ModuleName = nameof(HealthManager);

        private Color DefaultContentColor = GUI.contentColor;

        public Dictionary<string, float> DefaultNutritionMultipliers => new Dictionary<string, float>
            {
                { "m_NutritionFatConsumptionMulNoCarbs", 1f },
                { "m_NutritionProteinsConsumptionMulNoCarbs", 1f },
                { "m_NutritionCarbohydratesConsumptionRunMul", 1f },
                { "m_NutritionFatConsumptionRunMul", 1f },
                { "m_NutritionProteinsConsumptionRunMul", 1f },
                { "m_NutritionCarbohydratesConsumptionActionMul", 1f },
                { "m_NutritionFatConsumptionActionMul", 2f },
                { "m_NutritionProteinsConsumptionActionMul", 3f },
                { "m_NutritionCarbohydratesConsumptionWeightNormalMul", 1f },
                { "m_NutritionFatConsumptionWeightNormalMul", 1f },
                { "m_NutritionProteinsConsumptionWeightNormalMul", 1f },
                { "m_NutritionCarbohydratesConsumptionWeightOverloadMul", 1.5f },
                { "m_NutritionFatConsumptionWeightOverloadMul", 1.5f },
                { "m_NutritionProteinsConsumptionWeightOverloadMul", 1.5f },
                { "m_NutritionCarbohydratesConsumptionWeightCriticalMul", 1.8f },
                { "m_NutritionFatConsumptionWeightCriticalMul", 1.8f },
                { "m_NutritionProteinsConsumptionWeightCriticalMul", 1.8f },
                { "m_HydrationConsumptionRunMul", 0.5f },
                { "m_StaminaConsumptionWalkPerSecond", 1f },
                { "m_StaminaConsumptionRunPerSecond", 1f },
                { "m_StaminaConsumptionDepletedPerSecond", 1f },
                { "m_StaminaRegenerationPerSecond",1f},
                { "m_NutritionCarbohydratesConsumptionPerSecond", 1f },
                { "m_NutritionFatConsumptionPerSecond", 1f },
                { "m_NutritionProteinsConsumptionPerSecond", 1f },
                { "m_HydrationConsumptionPerSecond", 0.5f },
                { "m_HydrationConsumptionDuringFeverPerSecond", 0.5f },
                { "m_OxygenConsumptionPerSecond", 1f },
                { "m_EnergyConsumptionPerSecond", 0.1f },
                { "m_EnergyConsumptionPerSecondNoNutrition", 0.1f },
                { "m_EnergyConsumptionPerSecondFever", 0.1f },
                { "m_EnergyConsumptionPerSecondFoodPoison", 0.1f },
                { "m_HealthLossPerSecondNoNutrition", 0.05f },
                { "m_HealthLossPerSecondNoHydration", 0.05f },
                { "m_HealthLossPerSecondNoOxygen", 10f },
                { "m_EnergyLossDueLackOfNutritionPerSecond", 1f },
                { "m_EnergyRecoveryDueNutritionPerSecond", 1f },
                { "m_EnergyRecoveryDueHydrationPerSecond", 1f },
                { "m_DirtinessIncreasePerSecond", 0.1f }
            };
        
        public Dictionary<string, float> CustomNutritionMultipliers = new Dictionary<string, float>
                {
                    { nameof(NutritionFatConsumptionMulNoCarbs), NutritionFatConsumptionMulNoCarbs },
                    { nameof(NutritionProteinsConsumptionMulNoCarbs), NutritionProteinsConsumptionMulNoCarbs },
                    { nameof(NutritionCarbohydratesConsumptionRunMul), NutritionCarbohydratesConsumptionRunMul },
                    { nameof(NutritionFatConsumptionRunMul), NutritionFatConsumptionRunMul },
                    { nameof(NutritionProteinsConsumptionRunMul), NutritionProteinsConsumptionRunMul },
                    { nameof(NutritionCarbohydratesConsumptionActionMul), NutritionCarbohydratesConsumptionActionMul },
                    { nameof(NutritionFatConsumptionActionMul), NutritionFatConsumptionActionMul },
                    { nameof(NutritionProteinsConsumptionActionMul), NutritionProteinsConsumptionActionMul },
                    { nameof(NutritionCarbohydratesConsumptionWeightNormalMul), NutritionCarbohydratesConsumptionWeightNormalMul },
                    { nameof(NutritionFatConsumptionWeightNormalMul), NutritionFatConsumptionWeightNormalMul },
                    { nameof(NutritionProteinsConsumptionWeightNormalMul), NutritionProteinsConsumptionWeightNormalMul },
                    { nameof(NutritionCarbohydratesConsumptionWeightOverloadMul), NutritionCarbohydratesConsumptionWeightOverloadMul },
                    { nameof(NutritionFatConsumptionWeightOverloadMul), NutritionFatConsumptionWeightOverloadMul },
                    { nameof(NutritionProteinsConsumptionWeightOverloadMul), NutritionProteinsConsumptionWeightOverloadMul },
                    { nameof(NutritionCarbohydratesConsumptionWeightCriticalMul), NutritionCarbohydratesConsumptionWeightCriticalMul },
                    { nameof(NutritionFatConsumptionWeightCriticalMul), NutritionFatConsumptionWeightCriticalMul },
                    { nameof(NutritionProteinsConsumptionWeightCriticalMul), NutritionProteinsConsumptionWeightCriticalMul },
                    { nameof(HydrationConsumptionRunMul), HydrationConsumptionRunMul },
                    { nameof(StaminaConsumptionWalkPerSecond), StaminaConsumptionWalkPerSecond },
                    { nameof(StaminaConsumptionRunPerSecond), StaminaConsumptionRunPerSecond },
                    { nameof(StaminaConsumptionDepletedPerSecond), StaminaConsumptionDepletedPerSecond },
                    { nameof(StaminaRegenerationPerSecond), StaminaRegenerationPerSecond },
                    { nameof(NutritionCarbohydratesConsumptionPerSecond), NutritionCarbohydratesConsumptionPerSecond },
                    { nameof(NutritionFatConsumptionPerSecond), NutritionFatConsumptionPerSecond },
                    { nameof(NutritionProteinsConsumptionPerSecond), NutritionProteinsConsumptionPerSecond },
                    { nameof(HydrationConsumptionPerSecond), HydrationConsumptionPerSecond },
                    { nameof(HydrationConsumptionDuringFeverPerSecond), HydrationConsumptionDuringFeverPerSecond },
                    { nameof(OxygenConsumptionPerSecond), OxygenConsumptionPerSecond },
                    { nameof(EnergyConsumptionPerSecond), EnergyConsumptionPerSecond },
                    { nameof(EnergyConsumptionPerSecondNoNutrition), EnergyConsumptionPerSecondNoNutrition },
                    { nameof(EnergyConsumptionPerSecondFever), EnergyConsumptionPerSecondFever },
                    { nameof(EnergyConsumptionPerSecondFoodPoison), EnergyConsumptionPerSecondFoodPoison },
                    { nameof(HealthLossPerSecondNoNutrition), HealthLossPerSecondNoNutrition },
                    { nameof(HealthLossPerSecondNoHydration), HealthLossPerSecondNoHydration },
                    { nameof(HealthLossPerSecondNoOxygen), HealthLossPerSecondNoOxygen },
                    { nameof(EnergyLossDueLackOfNutritionPerSecond), EnergyLossDueLackOfNutritionPerSecond },
                    { nameof(EnergyRecoveryDueNutritionPerSecond), EnergyRecoveryDueNutritionPerSecond },
                    { nameof(EnergyRecoveryDueHydrationPerSecond), EnergyRecoveryDueHydrationPerSecond },
                    { nameof(DirtinessIncreasePerSecond), DirtinessIncreasePerSecond }
                };

        public bool IsParameterLossBlocked { get; set; } = false;
        public bool IsModEnabled { get; set; } = false;
        public bool HasChanged { get; set; } = false;

        private static HealthManager Instance;        
        private static PlayerConditionModule LocalPlayerConditionModule;
        private static FPPController LocalFPPController;
        private static ConsciousnessController LocalConsciousnessController;
        private static InventoryBackpack LocalInventoryBackpack;
        private static PlayerCocaineModule LocalPlayerCocaineModule;

        #region Multipliers
        public static float NutritionFatConsumptionMulNoCarbs { get; set; } = 1f;
        public static float NutritionProteinsConsumptionMulNoCarbs { get; set; } = 1f;
        public static float NutritionCarbohydratesConsumptionRunMul { get; set; } = 1f;
        public static float NutritionFatConsumptionRunMul { get; set; } = 1f;
        public static float NutritionProteinsConsumptionRunMul { get; set; } = 1f;
        public static float NutritionCarbohydratesConsumptionActionMul { get; set; } = 1f;
        public static float NutritionFatConsumptionActionMul { get; set; } = 2f;
        public static float NutritionProteinsConsumptionActionMul { get; set; } = 3f;
        public static float NutritionCarbohydratesConsumptionWeightNormalMul { get; set; } = 1f;
        public static float NutritionFatConsumptionWeightNormalMul { get; set; } = 1f;
        public static float NutritionProteinsConsumptionWeightNormalMul { get; set; } = 1f;
        public static float NutritionCarbohydratesConsumptionWeightOverloadMul { get; set; } = 1.5f;
        public static float NutritionFatConsumptionWeightOverloadMul { get; set; } = 1.5f;
        public static float NutritionProteinsConsumptionWeightOverloadMul { get; set; } = 1.5f;
        public static float NutritionCarbohydratesConsumptionWeightCriticalMul { get; set; } = 1.8f;
        public static float NutritionFatConsumptionWeightCriticalMul { get; set; } = 1.8f;
        public static float NutritionProteinsConsumptionWeightCriticalMul { get; set; } = 1.8f;
        public static float HydrationConsumptionRunMul { get; set; } = 0.5f;
        public static float StaminaConsumptionWalkPerSecond { get; set; } = 1f;
        public static float StaminaConsumptionRunPerSecond { get; set; } = 1f;
        public static float StaminaConsumptionDepletedPerSecond { get; set; } = 1f;
        public static float StaminaRegenerationPerSecond { get; set; } = 1f;
        public static float NutritionCarbohydratesConsumptionPerSecond { get; set; } = 1f;
        public static float NutritionFatConsumptionPerSecond { get; set; } = 1f;
        public static float NutritionProteinsConsumptionPerSecond { get; set; } = 1f;
        public static float HydrationConsumptionPerSecond { get; set; } = 0.5f;
        public static float HydrationConsumptionDuringFeverPerSecond { get; set; } = 0.5f;
        public static float OxygenConsumptionPerSecond { get; set; } = 1f;
        public static float EnergyConsumptionPerSecond { get; set; } = 0.1f;
        public static float EnergyConsumptionPerSecondNoNutrition { get; set; } = 0.1f;
        public static float EnergyConsumptionPerSecondFever { get; set; } = 0.1f;
        public static float EnergyConsumptionPerSecondFoodPoison { get; set; } = 0.1f;
        public static float HealthLossPerSecondNoNutrition { get; set; } = 0.05f;
        public static float HealthLossPerSecondNoHydration { get; set; } = 0.05f;
        public static float HealthLossPerSecondNoOxygen { get; set; } = 10f;
        public static float EnergyLossDueLackOfNutritionPerSecond { get; set; } = 1f;
        public static float EnergyRecoveryDueNutritionPerSecond { get; set; } = 1f;
        public static float EnergyRecoveryDueHydrationPerSecond { get; set; } = 1f;
        public static float DirtinessIncreasePerSecond { get; set; } = 0.1f;
        #endregion

        public HealthManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static HealthManager Get()
        {
            return Instance;
        }

        public void Start()
        {         
            SetModuleReferences();          
        }

        public void Update()
        {
            if (IsModEnabled)
            {
                InitData();
                UpdateNutritionMulMap();
                UpdateNutrition();
                UpdateParameterLoss();
            }         
        }

        private void InitData()
        {
            LocalPlayerConditionModule = PlayerConditionModule.Get();
            LocalFPPController = Player.Get().m_FPPController;
            LocalConsciousnessController = ConsciousnessController.Get();
            LocalInventoryBackpack = InventoryBackpack.Get();
            LocalPlayerCocaineModule = PlayerCocaineModule.Get();
        }

        private void UpdateNutritionMulMap()
        {
           if (HasChanged) 
            {
                CustomNutritionMultipliers = new Dictionary<string, float>
                {
                    { nameof(NutritionFatConsumptionMulNoCarbs), NutritionFatConsumptionMulNoCarbs },
                    { nameof(NutritionProteinsConsumptionMulNoCarbs), NutritionProteinsConsumptionMulNoCarbs },
                    { nameof(NutritionCarbohydratesConsumptionRunMul), NutritionCarbohydratesConsumptionRunMul },
                    { nameof(NutritionFatConsumptionRunMul), NutritionFatConsumptionRunMul },
                    { nameof(NutritionProteinsConsumptionRunMul), NutritionProteinsConsumptionRunMul },
                    { nameof(NutritionCarbohydratesConsumptionActionMul), NutritionCarbohydratesConsumptionActionMul },
                    { nameof(NutritionFatConsumptionActionMul), NutritionFatConsumptionActionMul },
                    { nameof(NutritionProteinsConsumptionActionMul), NutritionProteinsConsumptionActionMul },
                    { nameof(NutritionCarbohydratesConsumptionWeightNormalMul), NutritionCarbohydratesConsumptionWeightNormalMul },
                    { nameof(NutritionFatConsumptionWeightNormalMul), NutritionFatConsumptionWeightNormalMul },
                    { nameof(NutritionProteinsConsumptionWeightNormalMul), NutritionProteinsConsumptionWeightNormalMul },
                    { nameof(NutritionCarbohydratesConsumptionWeightOverloadMul), NutritionCarbohydratesConsumptionWeightOverloadMul },
                    { nameof(NutritionFatConsumptionWeightOverloadMul), NutritionFatConsumptionWeightOverloadMul },
                    { nameof(NutritionProteinsConsumptionWeightOverloadMul), NutritionProteinsConsumptionWeightOverloadMul },
                    { nameof(NutritionCarbohydratesConsumptionWeightCriticalMul), NutritionCarbohydratesConsumptionWeightCriticalMul },
                    { nameof(NutritionFatConsumptionWeightCriticalMul), NutritionFatConsumptionWeightCriticalMul },
                    { nameof(NutritionProteinsConsumptionWeightCriticalMul), NutritionProteinsConsumptionWeightCriticalMul },
                    { nameof(HydrationConsumptionRunMul), HydrationConsumptionRunMul },
                    { nameof(StaminaConsumptionWalkPerSecond), StaminaConsumptionWalkPerSecond },
                    { nameof(StaminaConsumptionRunPerSecond), StaminaConsumptionRunPerSecond },
                    { nameof(StaminaConsumptionDepletedPerSecond), StaminaConsumptionDepletedPerSecond },
                    { nameof(StaminaRegenerationPerSecond), StaminaRegenerationPerSecond },
                    { nameof(NutritionCarbohydratesConsumptionPerSecond), NutritionCarbohydratesConsumptionPerSecond },
                    { nameof(NutritionFatConsumptionPerSecond), NutritionFatConsumptionPerSecond },
                    { nameof(NutritionProteinsConsumptionPerSecond), NutritionProteinsConsumptionPerSecond },
                    { nameof(HydrationConsumptionPerSecond), HydrationConsumptionPerSecond },
                    { nameof(HydrationConsumptionDuringFeverPerSecond), HydrationConsumptionDuringFeverPerSecond },
                    { nameof(OxygenConsumptionPerSecond), OxygenConsumptionPerSecond },
                    { nameof(EnergyConsumptionPerSecond), EnergyConsumptionPerSecond },
                    { nameof(EnergyConsumptionPerSecondNoNutrition), EnergyConsumptionPerSecondNoNutrition },
                    { nameof(EnergyConsumptionPerSecondFever), EnergyConsumptionPerSecondFever },
                    { nameof(EnergyConsumptionPerSecondFoodPoison), EnergyConsumptionPerSecondFoodPoison },
                    { nameof(HealthLossPerSecondNoNutrition), HealthLossPerSecondNoNutrition },
                    { nameof(HealthLossPerSecondNoHydration), HealthLossPerSecondNoHydration },
                    { nameof(HealthLossPerSecondNoOxygen), HealthLossPerSecondNoOxygen },
                    { nameof(EnergyLossDueLackOfNutritionPerSecond), EnergyLossDueLackOfNutritionPerSecond },
                    { nameof(EnergyRecoveryDueNutritionPerSecond), EnergyRecoveryDueNutritionPerSecond },
                    { nameof(EnergyRecoveryDueHydrationPerSecond), EnergyRecoveryDueHydrationPerSecond },
                    { nameof(DirtinessIncreasePerSecond), DirtinessIncreasePerSecond }
                };
            }
        }

        private void UpdateParameterLoss()
        {
            if (IsParameterLossBlocked)
            {
                BlockParametersLoss();
            }
            else
            {
                UnblockParametersLoss();
            }
        }

        private void UnblockParametersLoss()
        {
            LocalPlayerConditionModule.UnblockParametersLoss();
        }

        private void BlockParametersLoss()
        {
            LocalPlayerConditionModule.BlockParametersLoss();
        }

        private void SetModuleReferences()
        {
          
        }

        private void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }

        private void UpdateNutrition()
        {
            NutrientsDepletion nutrientsDepletion = DifficultySettings.ActivePreset.m_NutrientsDepletion;
            if (nutrientsDepletion != 0)
            {
                bool flag = false;
                float num = Time.deltaTime;
                if (LocalConsciousnessController.IsUnconscious())
                {
                    num = Player.GetUnconsciousTimeFactor();
                }
                float num2 = 1f;
                float num3 = 1f;
                float num4 = 1f;
                if (LocalFPPController.IsRunning())
                {
                    num2 *= NutritionCarbohydratesConsumptionRunMul;
                    num3 *= NutritionFatConsumptionRunMul;
                    num4 *= NutritionProteinsConsumptionRunMul;
                }
                if (flag)
                {
                    num2 *= NutritionCarbohydratesConsumptionActionMul;
                    num3 *= NutritionFatConsumptionActionMul;
                    num4 *= NutritionProteinsConsumptionActionMul;
                }
                if (LocalPlayerConditionModule.IsNutritionCarboCriticalLevel())
                {
                    num3 *= NutritionFatConsumptionMulNoCarbs;
                    num4 *= NutritionProteinsConsumptionMulNoCarbs;
                }

                if (LocalInventoryBackpack.IsCriticalOverload())
                {
                    num2 *= NutritionCarbohydratesConsumptionWeightCriticalMul;
                    num3 *= NutritionFatConsumptionWeightCriticalMul;
                    num4 *= NutritionProteinsConsumptionWeightCriticalMul;
                }
                else if (LocalInventoryBackpack.IsOverload())
                {
                    num2 *= NutritionCarbohydratesConsumptionWeightOverloadMul;
                    num3 *= NutritionFatConsumptionWeightOverloadMul;
                    num4 *= NutritionProteinsConsumptionWeightOverloadMul;
                }
                else
                {
                    num2 *= NutritionCarbohydratesConsumptionWeightNormalMul;
                    num3 *= NutritionFatConsumptionWeightNormalMul;
                    num4 *= NutritionProteinsConsumptionWeightNormalMul;
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

                LocalPlayerConditionModule.m_NutritionCarbo -= NutritionCarbohydratesConsumptionPerSecond * num * num2 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_CarboConsumptionMul : 1f);
                LocalPlayerConditionModule.m_NutritionCarbo = Mathf.Clamp(LocalPlayerConditionModule.m_NutritionCarbo, 0f, LocalPlayerConditionModule.GetMaxNutritionCarbo());
                LocalPlayerConditionModule.m_NutritionFat -= NutritionFatConsumptionPerSecond * num * num3 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_FatConsumptionMul : 1f);
                LocalPlayerConditionModule.m_NutritionFat = Mathf.Clamp(LocalPlayerConditionModule.m_NutritionFat, 0f, LocalPlayerConditionModule.GetMaxNutritionFat());
                LocalPlayerConditionModule.m_NutritionProteins -= NutritionProteinsConsumptionPerSecond * num * num4 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_ProteinsConsumptionMul : 1f);
                LocalPlayerConditionModule.m_NutritionProteins = Mathf.Clamp(LocalPlayerConditionModule.m_NutritionProteins, 0f, LocalPlayerConditionModule.GetMaxNutritionProtein());
            }

        }

        public void GetCustomMultiplierSliders()
        {
            if (CustomNutritionMultipliers != null)
            {
                 var customordered = CustomNutritionMultipliers.OrderBy(x => x.Key).ToList();

                foreach (KeyValuePair<string, float> customconditionMul in customordered)
                {
                    using (var custmulH = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        float _val = MathF.Round(customconditionMul.Value, 1);
                      
                        if (customconditionMul.Key.ToLower().Contains("carbo"))
                        {
                           GUI.contentColor = IconColors.GetColor(IconColors.Icon.Carbo);
                        }
                        if (customconditionMul.Key.ToLower().Contains("fat"))
                        {
                            GUI.contentColor = IconColors.GetColor(IconColors.Icon.Fat);
                        }
                        if (customconditionMul.Key.ToLower().Contains("proteins"))
                        {
                            GUI.contentColor = IconColors.GetColor(IconColors.Icon.Proteins);
                        }
                        if (customconditionMul.Key.ToLower().Contains("oxygen") || customconditionMul.Key.ToLower().Contains("hydration"))
                        {
                            GUI.contentColor = IconColors.GetColor(IconColors.Icon.Hydration);
                        }
                        if (customconditionMul.Key.ToLower().Contains("energy") || customconditionMul.Key.ToLower().Contains("stamina") || customconditionMul.Key.ToLower().Contains("health"))
                        {
                            GUI.contentColor = IconColors.GetColor(IconColors.Icon.Energy);
                        }                      

                        switch (customconditionMul.Key)
                        {
                            case nameof(NutritionFatConsumptionMulNoCarbs):                              
                                NutritionFatConsumptionMulNoCarbs = GUILayout.HorizontalSlider(NutritionFatConsumptionMulNoCarbs, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionFatConsumptionMulNoCarbs, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionProteinsConsumptionMulNoCarbs):
                                NutritionProteinsConsumptionMulNoCarbs = GUILayout.HorizontalSlider(NutritionProteinsConsumptionMulNoCarbs, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionProteinsConsumptionMulNoCarbs, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionCarbohydratesConsumptionRunMul):
                                NutritionCarbohydratesConsumptionRunMul = GUILayout.HorizontalSlider(NutritionCarbohydratesConsumptionRunMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionCarbohydratesConsumptionRunMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionFatConsumptionRunMul):
                                NutritionFatConsumptionRunMul = GUILayout.HorizontalSlider(NutritionFatConsumptionRunMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionFatConsumptionRunMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionProteinsConsumptionRunMul):
                                NutritionProteinsConsumptionRunMul = GUILayout.HorizontalSlider(NutritionProteinsConsumptionRunMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionProteinsConsumptionRunMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionCarbohydratesConsumptionActionMul):
                                NutritionCarbohydratesConsumptionActionMul = GUILayout.HorizontalSlider(NutritionCarbohydratesConsumptionActionMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionCarbohydratesConsumptionActionMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionFatConsumptionActionMul):
                                NutritionFatConsumptionActionMul = GUILayout.HorizontalSlider(NutritionFatConsumptionActionMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionFatConsumptionActionMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionProteinsConsumptionActionMul):
                                NutritionProteinsConsumptionActionMul = GUILayout.HorizontalSlider(NutritionProteinsConsumptionActionMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionProteinsConsumptionActionMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionCarbohydratesConsumptionWeightNormalMul):
                                NutritionCarbohydratesConsumptionWeightNormalMul = GUILayout.HorizontalSlider(NutritionCarbohydratesConsumptionWeightNormalMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionCarbohydratesConsumptionWeightNormalMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionFatConsumptionWeightNormalMul):
                                NutritionFatConsumptionWeightNormalMul = GUILayout.HorizontalSlider(NutritionFatConsumptionWeightNormalMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionFatConsumptionWeightNormalMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionProteinsConsumptionWeightNormalMul):
                                NutritionProteinsConsumptionWeightNormalMul = GUILayout.HorizontalSlider(NutritionProteinsConsumptionWeightNormalMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionProteinsConsumptionWeightNormalMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionCarbohydratesConsumptionWeightOverloadMul):
                                NutritionCarbohydratesConsumptionWeightOverloadMul = GUILayout.HorizontalSlider(NutritionCarbohydratesConsumptionWeightOverloadMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionCarbohydratesConsumptionWeightOverloadMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionFatConsumptionWeightOverloadMul):
                                NutritionFatConsumptionWeightOverloadMul = GUILayout.HorizontalSlider(NutritionFatConsumptionWeightOverloadMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionFatConsumptionWeightOverloadMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionProteinsConsumptionWeightOverloadMul):
                                NutritionProteinsConsumptionWeightOverloadMul = GUILayout.HorizontalSlider(NutritionProteinsConsumptionWeightOverloadMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionProteinsConsumptionWeightOverloadMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionCarbohydratesConsumptionWeightCriticalMul):
                                NutritionCarbohydratesConsumptionWeightCriticalMul = GUILayout.HorizontalSlider(NutritionCarbohydratesConsumptionWeightCriticalMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionCarbohydratesConsumptionWeightCriticalMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionFatConsumptionWeightCriticalMul):
                                NutritionFatConsumptionWeightCriticalMul = GUILayout.HorizontalSlider(NutritionFatConsumptionWeightCriticalMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionFatConsumptionWeightCriticalMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionProteinsConsumptionWeightCriticalMul):
                                NutritionProteinsConsumptionWeightCriticalMul = GUILayout.HorizontalSlider(NutritionProteinsConsumptionWeightCriticalMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionProteinsConsumptionWeightCriticalMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(HydrationConsumptionRunMul):
                                HydrationConsumptionRunMul = GUILayout.HorizontalSlider(HydrationConsumptionRunMul, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(HydrationConsumptionRunMul, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(StaminaConsumptionWalkPerSecond):
                                StaminaConsumptionWalkPerSecond = GUILayout.HorizontalSlider(StaminaConsumptionWalkPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(StaminaConsumptionWalkPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(StaminaConsumptionRunPerSecond):
                                StaminaConsumptionRunPerSecond = GUILayout.HorizontalSlider(StaminaConsumptionRunPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(StaminaConsumptionRunPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(StaminaConsumptionDepletedPerSecond):
                                StaminaConsumptionDepletedPerSecond = GUILayout.HorizontalSlider(StaminaConsumptionDepletedPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(StaminaConsumptionDepletedPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(StaminaRegenerationPerSecond):
                                StaminaRegenerationPerSecond = GUILayout.HorizontalSlider(StaminaRegenerationPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(StaminaRegenerationPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionCarbohydratesConsumptionPerSecond):
                                NutritionCarbohydratesConsumptionPerSecond = GUILayout.HorizontalSlider(NutritionCarbohydratesConsumptionPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionCarbohydratesConsumptionPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionFatConsumptionPerSecond):
                                NutritionFatConsumptionPerSecond = GUILayout.HorizontalSlider(NutritionFatConsumptionPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionFatConsumptionPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(NutritionProteinsConsumptionPerSecond):
                                NutritionProteinsConsumptionPerSecond = GUILayout.HorizontalSlider(NutritionProteinsConsumptionPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(NutritionProteinsConsumptionPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(HydrationConsumptionPerSecond):
                                HydrationConsumptionPerSecond = GUILayout.HorizontalSlider(HydrationConsumptionPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(HydrationConsumptionPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(HydrationConsumptionDuringFeverPerSecond):
                                HydrationConsumptionDuringFeverPerSecond = GUILayout.HorizontalSlider(HydrationConsumptionDuringFeverPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(HydrationConsumptionDuringFeverPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(OxygenConsumptionPerSecond):
                                OxygenConsumptionPerSecond = GUILayout.HorizontalSlider(OxygenConsumptionPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(OxygenConsumptionPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(EnergyConsumptionPerSecond):
                                EnergyConsumptionPerSecond = GUILayout.HorizontalSlider(EnergyConsumptionPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(EnergyConsumptionPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(EnergyConsumptionPerSecondNoNutrition):
                                EnergyConsumptionPerSecondNoNutrition = GUILayout.HorizontalSlider(EnergyConsumptionPerSecondNoNutrition, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(EnergyConsumptionPerSecondNoNutrition, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(EnergyConsumptionPerSecondFever):
                                EnergyConsumptionPerSecondFever = GUILayout.HorizontalSlider(EnergyConsumptionPerSecondFever, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(EnergyConsumptionPerSecondFever, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(EnergyConsumptionPerSecondFoodPoison):
                                EnergyConsumptionPerSecondFoodPoison = GUILayout.HorizontalSlider(EnergyConsumptionPerSecondFoodPoison, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(EnergyConsumptionPerSecondFoodPoison, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(HealthLossPerSecondNoNutrition):
                                HealthLossPerSecondNoNutrition = GUILayout.HorizontalSlider(HealthLossPerSecondNoNutrition, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(HealthLossPerSecondNoNutrition, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(HealthLossPerSecondNoHydration):
                                HealthLossPerSecondNoHydration = GUILayout.HorizontalSlider(HealthLossPerSecondNoHydration, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(HealthLossPerSecondNoHydration, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(HealthLossPerSecondNoOxygen):
                                HealthLossPerSecondNoOxygen = GUILayout.HorizontalSlider(HealthLossPerSecondNoOxygen, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(HealthLossPerSecondNoOxygen, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(EnergyLossDueLackOfNutritionPerSecond):
                                EnergyLossDueLackOfNutritionPerSecond = GUILayout.HorizontalSlider(EnergyLossDueLackOfNutritionPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(EnergyLossDueLackOfNutritionPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(EnergyRecoveryDueNutritionPerSecond):
                                EnergyRecoveryDueNutritionPerSecond = GUILayout.HorizontalSlider(EnergyRecoveryDueNutritionPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(EnergyRecoveryDueNutritionPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(EnergyRecoveryDueHydrationPerSecond):
                                EnergyRecoveryDueHydrationPerSecond = GUILayout.HorizontalSlider(EnergyRecoveryDueHydrationPerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(EnergyRecoveryDueHydrationPerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            case nameof(DirtinessIncreasePerSecond):
                                DirtinessIncreasePerSecond = GUILayout.HorizontalSlider(DirtinessIncreasePerSecond, 0f, 3f, GUILayout.MinWidth(200f));
                                if (_val != MathF.Round(DirtinessIncreasePerSecond, 1))
                                {
                                    HasChanged = true;
                                }
                                break;
                            default:
                                break;
                        }

                        GUI.contentColor = DefaultContentColor;
                    }
                }
            }
        }

        public void GetDefaultMultiplierSliders()
        {
            if (DefaultNutritionMultipliers != null)
            {
               var ordered = DefaultNutritionMultipliers.OrderBy(x => x.Key).ToList();

                foreach (KeyValuePair<string, float> conditionMul in ordered)
                {
                    using (var dmulH = new GUILayout.HorizontalScope(GUI.skin.box))
                    {
                        float _val = MathF.Round(conditionMul.Value, 1);
                      
                        if (conditionMul.Key.ToLower().Contains("carbo"))
                        {
                            GUI.contentColor = IconColors.GetColor(IconColors.Icon.Carbo);
                        }
                        if (conditionMul.Key.ToLower().Contains("fat"))
                        {
                            GUI.contentColor = IconColors.GetColor(IconColors.Icon.Fat);
                        }
                        if (conditionMul.Key.ToLower().Contains("proteins"))
                        {
                            GUI.contentColor = IconColors.GetColor(IconColors.Icon.Proteins);
                        }
                        if (conditionMul.Key.ToLower().Contains("oxygen") || conditionMul.Key.ToLower().Contains("hydration"))
                        {
                            GUI.contentColor = IconColors.GetColor(IconColors.Icon.Hydration);
                        }
                        if (conditionMul.Key.ToLower().Contains("energy") || conditionMul.Key.ToLower().Contains("stamina") || conditionMul.Key.ToLower().Contains("health"))
                        {
                            GUI.contentColor = IconColors.GetColor(IconColors.Icon.Energy);
                        }
                        
                        GUILayout.Label($"{conditionMul.Key}: {_val} ", GUI.skin.label);
                        _ = GUILayout.HorizontalSlider(_val, 0f, 3f, GUILayout.MinWidth(200f));
                        GUI.contentColor = DefaultContentColor;
                    }
                }
            }
        }
    }

}
