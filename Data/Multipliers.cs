using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static P2PStats.ReplicationStat;

namespace ModTime.Data.Player.Condition
{
    public class Multipliers : MonoBehaviour
    {
        private static Multipliers Instance;

        public Multipliers()
        {
            Instance = this;
        }

        public static Multipliers Get() => Instance;

        #region  NutritionFat

        public float m_NutritionFatConsumptionMulNoCarbs { get; set; } = 1f;
        public float m_NutritionFatConsumptionPerSecond { get; set; } = 1f;
        public float m_NutritionFatConsumptionRunMul { get; set; } = 1f;
        public float m_NutritionFatConsumptionActionMul { get; set; } = 2f;
        public float m_NutritionFatConsumptionWeightNormalMul { get; set; } = 1f;
        public float m_NutritionFatConsumptionWeightOverloadMul { get; set; } = 1.5f;
        public float m_NutritionFatConsumptionWeightCriticalMul { get; set; } = 1.8f;

        #endregion

        #region NutritionProteins

        public float m_NutritionProteinsConsumptionMulNoCarbs { get; set; } = 1f;
        public float m_NutritionProteinsConsumptionRunMul { get; set; } = 1f;
        public float m_NutritionProteinsConsumptionActionMul { get; set; } = 3f;
        public float m_NutritionProteinsConsumptionWeightNormalMul { get; set; } = 1f;
        public float m_NutritionProteinsConsumptionWeightOverloadMul { get; set; } = 1.5f;
        public float m_NutritionProteinsConsumptionWeightCriticalMul { get; set; } = 1.8f;
        public float m_NutritionProteinsConsumptionPerSecond { get; set; } = 1f;

        #endregion

        #region NutritionCarbohydrates

        public float m_NutritionCarbohydratesConsumptionRunMul { get; set; } = 1f;
        public float m_NutritionCarbohydratesConsumptionActionMul { get; set; } = 1f;
        public float m_NutritionCarbohydratesConsumptionWeightNormalMul { get; set; } = 1f;
        public float m_NutritionCarbohydratesConsumptionWeightOverloadMul { get; set; } = 1.5f;
        public float m_NutritionCarbohydratesConsumptionWeightCriticalMul { get; set; } = 1.8f;
        public float m_NutritionCarbohydratesConsumptionPerSecond { get; set; } = 1f;

        #endregion

        #region Hydration

        private float m_HydrationConsumptionRunMul { get; set; } = 0.5f;
        private float m_HydrationConsumptionPerSecond { get; set; } = 0.5f;
        private float m_HydrationConsumptionDuringFeverPerSecond { get; set; } = 0.5f;

        #endregion

        #region Health

        private float m_HealthLossPerSecondNoNutrition { get; set; } = 0.05f;
        private float m_HealthLossPerSecondNoHydration { get; set; } = 0.05f;
        private float m_HealthLossPerSecondNoOxygen { get; set; } = 10f;

        #endregion

        #region Stamina

        private float m_StaminaConsumptionWalkPerSecond { get; set; } = 1f;
        private float m_StaminaConsumptionRunPerSecond { get; set; } = 1f;
        private float m_StaminaConsumptionDepletedPerSecond { get; set; } = 1f;
        private float m_StaminaRegenerationPerSecond { get; set; } = 1f;

        #endregion

        #region Oxygen

        private float m_OxygenConsumptionPerSecond { get; set; } = 1f;

        #endregion

        #region Energy

        private float m_EnergyConsumptionPerSecond { get; set; } = 0.1f;
        private float m_EnergyConsumptionPerSecondNoNutrition { get; set; } = 0.1f;
        private float m_EnergyConsumptionPerSecondFever { get; set; } = 0.1f;
        private float m_EnergyConsumptionPerSecondFoodPoison { get; set; } = 0.1f;
        private float m_EnergyLossDueLackOfNutritionPerSecond { get; set; } = 1f;
        private float m_EnergyRecoveryDueNutritionPerSecond { get; set; } = 1f;
        private float m_EnergyRecoveryDueHydrationPerSecond { get; set; } = 1f;

        #endregion

        #region Dirtiness

        private float m_DirtinessIncreasePerSecond { get; set; } = 0.1f;

        #endregion

        #region Multipliers

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

        public float GetDefaultMultiplierValue(string name)
        {
            if (DefaultNutritionMultipliers.ContainsKey(name))
            {
                return DefaultNutritionMultipliers[name];
            }
            else
            {
                return -1f;
            }
        }

        private Dictionary<string, float> _customMuls;
        public Dictionary<string, float> CustomNutritionMultipliers
        {
            get
            {
                if(_customMuls == null)
                {
                    _customMuls = new Dictionary<string, float>();
                    foreach (var item in DefaultNutritionMultipliers)
                    {
                        _customMuls.Add(item.Key, item.Value);
                    }                    
                }
                return _customMuls;
            }
            set { _customMuls = value; }
        }

        public float GetCustomMultiplierValue(string name)
        {
            if (CustomNutritionMultipliers.ContainsKey(name))
            {
                return CustomNutritionMultipliers[name];
            }
            else
            {
                return -1f;
            }
        }

        public bool SetCustomNutritionMultiplierValue(string name, float value)
        {
            if (CustomNutritionMultipliers.ContainsKey(name))
            {
                CustomNutritionMultipliers[name] = value;
                return true;
            }
            else
            {
                return false;  
            }
        }

        #endregion

    }
}
