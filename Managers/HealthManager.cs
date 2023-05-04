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
using static Mono.Security.X509.X520;

namespace ModTime.Managers
{
    public class HealthManager : MonoBehaviour
    {
        private static readonly string ModuleName = nameof(HealthManager);
        public static readonly string SavedSettingsFileName = $"{ModuleName}.sav";

        private static HealthManager Instance;
        private static PlayerConditionModule LocalPlayerConditionModule;
        private static FPPController LocalFPPController;
        private static ConsciousnessController LocalConsciousnessController;
        private static DeathController LocalDeathController;
        private static InventoryBackpack LocalInventoryBackpack;
        private static PlayerCocaineModule LocalPlayerCocaineModule;
        private static Multipliers LocalMultipliers;

        public bool SettingsLoaded { get; set; } = false;
        public bool IsModEnabled { get; set; } = false;
        public bool HasChanged { get; set; } = false;

        public NutrientsDepletion SelectedActiveNutrientsDepletionPreset { get; set; } = DifficultySettings.ActivePreset.m_NutrientsDepletion;
        public int SelectedActiveNutrientsDepletionPresetIndex { get; set; } = (int)DifficultySettings.ActivePreset.m_NutrientsDepletion;

        private bool IsUseDefault = true;
        public bool UseDefault { get; set; } = true;
        public bool IsParameterLossBlocked { get; set; } = false;
      
        public NutrientsDepletion ActiveNutrientsDepletionPreset { get; set; } = DifficultySettings.ActivePreset.m_NutrientsDepletion;
        public int ActiveNutrientsDepletionPresetIndex { get; set; } = (int)DifficultySettings.ActivePreset.m_NutrientsDepletion;

        public Dictionary<string, float> CustomMultipliers { get; set; }
        public Dictionary<string, float> DefaultMultipliers { get; set; }

        public HealthManager()
        {
            useGUILayout = true;
            Instance = this;
        }

        public static HealthManager Get() => Instance;

        protected virtual void Start()
        {
            InitData();
        }

        protected virtual void InitData()
        {
            LocalPlayerConditionModule = PlayerConditionModule.Get();
            LocalFPPController = FPPController.Get();
            LocalConsciousnessController = ConsciousnessController.Get();
            LocalDeathController = DeathController.Get();
            LocalInventoryBackpack = InventoryBackpack.Get();
            LocalPlayerCocaineModule = PlayerCocaineModule.Get();
            LocalMultipliers = Multipliers.Get();
        }

        public virtual bool GetUseDefault()
        {
            return IsUseDefault;
        }

        public virtual void SetUseDefault()
        {
            IsUseDefault = true;
        }

        public virtual void SetUseCustom()
        {
            IsUseDefault = false;
        }

        public virtual void UnblockParametersLoss()
        {
            LocalPlayerConditionModule.UnblockParametersLoss();
        }

        public virtual void BlockParametersLoss()
        {
            LocalPlayerConditionModule.BlockParametersLoss();
        }

        public virtual bool GetParameterLossBlocked()
        {
            return LocalPlayerConditionModule.GetParameterLossBlocked();
        }

        protected virtual void Update()
        {
            if (IsModEnabled)
            {
                InitData();
                //UpdateStamina(GetUseDefault());
                //UpdateEnergy(GetUseDefault());
                //UpdateMaxHP(GetUseDefault());
                //UpdateHP(GetUseDefault());
                //UpdateMaxStamina(GetUseDefault());
                UpdateParameterLoss(GetParameterLossBlocked());
                //UpdateNutrition(GetUseDefault());
            }
        }

        protected virtual void UpdateStamina(bool usedefault)
        {
            if (usedefault)
            {
                UpdateDefaultStamina();
            }
            else
            {
                UpdateCustomStamina();
            }
        }

        protected virtual void UpdateCustomStamina()
        {
            if (IsModEnabled && !UseDefault)
            {
                if (Player.Get().IsDialogMode())
                {
                    return;
                }

                float deltaTime = Time.deltaTime;
                FPPController fPPController = LocalFPPController;
                if (fPPController == null)
                {
                    return;
                }

                if (Cheats.m_GodMode || Player.Get().IsInArenaMode())
                {
                    LocalPlayerConditionModule.m_Stamina = LocalPlayerConditionModule.GetMaxStamina();
                    return;
                }

                float num = LocalPlayerConditionModule.GetStamina();
                bool flag = (bool)Player.Get().GetCurrentItem() && Player.Get().GetCurrentItem().m_Info.IsHeavyObject();
                if (fPPController.IsActive() && fPPController.IsWalking())
                {
                    num -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_StaminaConsumptionWalkPerSecond)) * deltaTime * (PlayerCocaineModule.Get().m_Active ? PlayerCocaineModule.Get().m_StaminaConsumptionMul : 1f) * (flag ? 1.6f : 1f);
                }
                else if (fPPController.IsActive() && fPPController.IsRunning())
                {
                    num -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_StaminaConsumptionRunPerSecond)) * deltaTime * (PlayerCocaineModule.Get().m_Active ? PlayerCocaineModule.Get().m_StaminaConsumptionMul : 1f) * (flag ? 1.6f : 1f);
                }
                else if (fPPController.IsActive() && fPPController.IsDepleted())
                {
                    num -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_StaminaConsumptionDepletedPerSecond)) * deltaTime * (PlayerCocaineModule.Get().m_Active ? PlayerCocaineModule.Get().m_StaminaConsumptionMul : 1f);
                }
                else if (!MakeFireController.Get().IsActive() || MakeFireController.Get().m_State != MakeFireController.State.Game)
                {
                    num += LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_StaminaRegenerationPerSecond)) * deltaTime;
                }

                if (num < LocalPlayerConditionModule.m_Stamina || Time.time - LocalMultipliers.m_LastDecreaseStaminaTime >= LocalMultipliers.m_StaminaRenerationDelay)
                {
                    LocalPlayerConditionModule.m_Stamina = num;
                }

                if (LocalPlayerConditionModule.m_Stamina - LocalMultipliers.m_PrevStamina < 0f)
                {
                    LocalMultipliers.m_LastDecreaseStaminaTime = Time.time;
                }

                LocalPlayerConditionModule.m_Stamina = Mathf.Clamp(LocalPlayerConditionModule.GetStamina(), 0f, LocalPlayerConditionModule.m_Stamina);
                if (LocalMultipliers.m_IsLowStamina)
                {
                    if (LocalPlayerConditionModule.m_Stamina < LocalMultipliers.m_LowStaminaRecoveryLevel)
                    {
                        LocalMultipliers.m_IsLowStamina = LocalPlayerConditionModule.m_Stamina < LocalMultipliers.m_LowStaminaLevel && LocalPlayerConditionModule.m_Stamina < LocalPlayerConditionModule.GetStamina();
                    }
                    else if (LocalPlayerConditionModule.m_Stamina >= LocalMultipliers.m_LowStaminaRecoveryLevel)
                    {
                        LocalMultipliers.m_IsLowStamina = false;
                    }
                }
                else if (LocalPlayerConditionModule.m_Stamina < LocalMultipliers.m_LowStaminaLevel && LocalPlayerConditionModule.m_Stamina < LocalPlayerConditionModule.GetStamina())
                {
                    LocalMultipliers.m_IsLowStamina = true;
                }

                LocalMultipliers.m_PrevStamina = LocalPlayerConditionModule.m_Stamina;
            }
        }

        protected virtual void UpdateDefaultStamina()
        {
            if (IsModEnabled && UseDefault)
            {
                if (Player.Get().IsDialogMode())
                {
                    return;
                }

                float deltaTime = Time.deltaTime;
                if (LocalFPPController == null)
                {
                    return;
                }

                if (Cheats.m_GodMode || Player.Get().IsInArenaMode())
                {
                    LocalPlayerConditionModule.m_Stamina = LocalPlayerConditionModule.GetMaxStamina();
                    return;
                }

                float num = LocalPlayerConditionModule.GetStamina();
                bool flag = (bool)Player.Get().GetCurrentItem() && Player.Get().GetCurrentItem().m_Info.IsHeavyObject();
                if (LocalFPPController.IsActive() && LocalFPPController.IsWalking())
                {
                    num -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_StaminaConsumptionWalkPerSecond)) * deltaTime * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_StaminaConsumptionMul : 1f) * (flag ? 1.6f : 1f);
                }
                else if (LocalFPPController.IsActive() && LocalFPPController.IsRunning())
                {
                    num -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_StaminaConsumptionRunPerSecond)) * deltaTime * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_StaminaConsumptionMul : 1f) * (flag ? 1.6f : 1f);
                }
                else if (LocalFPPController.IsActive() && LocalFPPController.IsDepleted())
                {
                    num -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_StaminaConsumptionDepletedPerSecond)) * deltaTime * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_StaminaConsumptionMul : 1f);
                }
                else if (!MakeFireController.Get().IsActive() || MakeFireController.Get().m_State != MakeFireController.State.Game)
                {
                    num += LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_StaminaRegenerationPerSecond)) * deltaTime;
                }

                if (num < LocalPlayerConditionModule.m_Stamina || Time.time - LocalMultipliers.m_LastDecreaseStaminaTime >= LocalMultipliers.m_StaminaRenerationDelay)
                {
                    LocalPlayerConditionModule.m_Stamina = num;
                }

                if (LocalPlayerConditionModule.m_Stamina - LocalMultipliers.m_PrevStamina < 0f)
                {
                    LocalMultipliers.m_LastDecreaseStaminaTime = Time.time;
                }

                LocalPlayerConditionModule.m_Stamina = Mathf.Clamp(LocalPlayerConditionModule.m_Stamina, 0f, LocalPlayerConditionModule.GetStamina());
                if (LocalMultipliers.m_IsLowStamina)
                {
                    if (LocalPlayerConditionModule.m_Stamina < LocalMultipliers.m_LowStaminaRecoveryLevel)
                    {
                        LocalMultipliers.m_IsLowStamina = LocalPlayerConditionModule.m_Stamina < LocalMultipliers.m_LowStaminaLevel && LocalPlayerConditionModule.m_Stamina < LocalPlayerConditionModule.GetStamina();
                    }
                    else if (LocalPlayerConditionModule.m_Stamina >= LocalMultipliers.m_LowStaminaRecoveryLevel)
                    {
                        LocalMultipliers.m_IsLowStamina = false;
                    }
                }
                else if (LocalPlayerConditionModule.m_Stamina < LocalMultipliers.m_LowStaminaLevel && LocalPlayerConditionModule.m_Stamina < LocalPlayerConditionModule.GetStamina())
                {
                    LocalMultipliers.m_IsLowStamina = true;
                }

                LocalMultipliers.m_PrevStamina = LocalPlayerConditionModule.m_Stamina;
            }
        }

        protected virtual void UpdateEnergy(bool usedefault)
        {
            if (usedefault)
            {
                UpdateDefaultEnergy();
            }
            else
            {
                UpdateCustomEnergy();
            }
        }

        protected virtual void UpdateCustomEnergy()
        {
            if (IsModEnabled && !UseDefault)
            {
                if (ScenarioManager.Get().IsDream())
                {
                    return;
                }

                float deltaTime = Time.deltaTime;
                if (!LocalFPPController || LocalDeathController.IsActive() || LocalConsciousnessController.IsActive())
                {
                    return;
                }

                float num = 1f;
                Insomnia insomnia = (Insomnia)PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.Insomnia);
                if (insomnia != null && insomnia.IsActive())
                {
                    num = insomnia.m_EnergyLossMulFinal;
                }

                if (DifficultySettings.ActivePreset.m_Energy && !Cheats.m_GodMode && !GetParameterLossBlocked() && !Player.Get().IsDialogMode())
                {
                    LocalPlayerConditionModule.m_Energy -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_EnergyConsumptionPerSecond)) * deltaTime * num;
                    if (LocalPlayerConditionModule.IsNutritionCarboCriticalLevel() || LocalPlayerConditionModule.IsNutritionFatCriticalLevel() || LocalPlayerConditionModule.IsNutritionProteinsCriticalLevel())
                    {
                        LocalPlayerConditionModule.m_Energy -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_EnergyConsumptionPerSecondNoNutrition)) * deltaTime * num;
                    }

                    if (PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.Fever).IsActive())
                    {
                        LocalPlayerConditionModule.m_Energy -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_EnergyConsumptionPerSecondFever)) * deltaTime * num;
                    }

                    if (PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.FoodPoisoning).IsActive())
                    {
                        LocalPlayerConditionModule.m_Energy -= LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_EnergyConsumptionPerSecondFoodPoison)) * deltaTime * num;
                    }
                }

                LocalPlayerConditionModule.m_Energy = Mathf.Clamp(LocalPlayerConditionModule.m_Energy, 0f, LocalPlayerConditionModule.GetMaxEnergy());
                if (LocalPlayerConditionModule.m_Energy <= PlayerSanityModule.Get().m_LowEnegryWhispersLevel)
                {
                    PlayerSanityModule.Get().OnWhispersEvent(PlayerSanityModule.WhisperType.LowEnergy);
                }
            }
        }

        protected virtual void UpdateDefaultEnergy()
        {
            if (IsModEnabled && UseDefault)
            {
                if (ScenarioManager.Get().IsDream())
                {
                    return;
                }

                float deltaTime = Time.deltaTime;
                if (!LocalFPPController || LocalDeathController.IsActive() || LocalConsciousnessController.IsActive())
                {
                    return;
                }

                float num = 1f;
                Insomnia insomnia = (Insomnia)PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.Insomnia);
                if (insomnia != null && insomnia.IsActive())
                {
                    num = insomnia.m_EnergyLossMulFinal;
                }

                if (DifficultySettings.ActivePreset.m_Energy && !Cheats.m_GodMode && !GetParameterLossBlocked() && !Player.Get().IsDialogMode())
                {
                    LocalPlayerConditionModule.m_Energy -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_EnergyConsumptionPerSecond)) * deltaTime * num;
                    if (LocalPlayerConditionModule.IsNutritionCarboCriticalLevel() || LocalPlayerConditionModule.IsNutritionFatCriticalLevel() || LocalPlayerConditionModule.IsNutritionProteinsCriticalLevel())
                    {
                        LocalPlayerConditionModule.m_Energy -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_EnergyConsumptionPerSecondNoNutrition)) * deltaTime * num;
                    }

                    if (PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.Fever).IsActive())
                    {
                        LocalPlayerConditionModule.m_Energy -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_EnergyConsumptionPerSecondFever)) * deltaTime * num;
                    }

                    if (PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.FoodPoisoning).IsActive())
                    {
                        LocalPlayerConditionModule.m_Energy -= LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_EnergyConsumptionPerSecondFoodPoison)) * deltaTime * num;
                    }
                }

                LocalPlayerConditionModule.m_Energy = Mathf.Clamp(LocalPlayerConditionModule.m_Energy, 0f, LocalPlayerConditionModule.GetMaxEnergy());
                if (LocalPlayerConditionModule.m_Energy <= PlayerSanityModule.Get().m_LowEnegryWhispersLevel)
                {
                    PlayerSanityModule.Get().OnWhispersEvent(PlayerSanityModule.WhisperType.LowEnergy);
                }
            }
        }

        protected virtual void UpdateMaxHP(bool usedefault)
        {
            if (usedefault)
            {
                UpdateDefaultMaxHP();
            }
            else
            {
                UpdateCustomMaxHP();
            }
        }

        protected virtual void UpdateCustomMaxHP()
        {
            if (IsModEnabled && !UseDefault)
            {
                LocalPlayerConditionModule.m_MaxHP = LocalPlayerConditionModule.m_Hydration * 0.25f + LocalPlayerConditionModule.m_NutritionFat * 0.25f + LocalPlayerConditionModule.m_NutritionCarbo * 0.25f + LocalPlayerConditionModule.m_NutritionProteins * 0.25f;
                LocalPlayerConditionModule.m_MaxHP = Mathf.Clamp(LocalPlayerConditionModule.GetMaxHP(), 0f, LocalPlayerConditionModule.m_MaxHP);
            }
        }

        protected virtual void UpdateDefaultMaxHP()
        {
            if (IsModEnabled && UseDefault)
            {
                LocalPlayerConditionModule.m_MaxHP = LocalPlayerConditionModule.m_Hydration * 0.25f + LocalPlayerConditionModule.m_NutritionFat * 0.25f + LocalPlayerConditionModule.m_NutritionCarbo * 0.25f + LocalPlayerConditionModule.m_NutritionProteins * 0.25f;
                LocalPlayerConditionModule.m_MaxHP = Mathf.Clamp(LocalPlayerConditionModule.m_MaxHP, 0f, 100f);
            }
        }

        protected virtual void UpdateHP(bool usedefault)
        {
            if (usedefault)
            {
                UpdateDefaultHP();
            }
            else
            {
                UpdateCustomHP();
            }
        }

        protected virtual void UpdateCustomHP()
        {
            if (IsModEnabled && !UseDefault)
            {
                if (ScenarioManager.Get().IsDream() || Player.Get().IsInArenaMode())
                {
                    return;
                }

                float deltaTime = Time.deltaTime;
                if (LocalFPPController == null || LocalDeathController.IsActive() || LocalConsciousnessController.IsActive())
                {
                    return;
                }

                if (!Cheats.m_GodMode && !Player.Get().IsDialogMode())
                {
                    if (LocalPlayerConditionModule.IsNutritionCarboCriticalLevel() || LocalPlayerConditionModule.IsNutritionFatCriticalLevel() || LocalPlayerConditionModule.IsNutritionProteinsCriticalLevel())
                    {
                        LocalPlayerConditionModule.IncreaseHP((0f - LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_HealthLossPerSecondNoNutrition))) * deltaTime);
                    }

                    if (LocalPlayerConditionModule.IsHydrationCriticalLevel())
                    {
                        LocalPlayerConditionModule.IncreaseHP((0f - LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_HealthLossPerSecondNoHydration))) * deltaTime);
                    }

                    bool flag = true;
                    List<Injury> injuriesList = PlayerInjuryModule.Get().GetInjuriesList();
                    if (injuriesList != null)
                    {
                        for (int i = 0; i < injuriesList.Count; i++)
                        {
                            if (injuriesList[i].m_ParentInjury == null && injuriesList[i].m_Type != InjuryType.Worm && injuriesList[i].m_Type != InjuryType.Leech)
                            {
                                flag = false;
                                break;
                            }
                        }
                    }

                    if (flag)
                    {
                        float num = (MainLevel.Instance.m_TODTime.m_DayLengthInMinutes + MainLevel.Instance.m_TODTime.m_NightLengthInMinutes) * 60f;
                        float num2 = LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_HealthRecoveryPerDayNormalMode));
                        switch (DifficultySettings.ActivePreset.m_BaseDifficulty)
                        {
                            case GameDifficulty.Easy:
                                num2 = LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_HealthRecoveryPerDayEasyMode));
                                break;
                            case GameDifficulty.Hard:
                                num2 = LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_HealthRecoveryPerDayHardMode));
                                break;
                        }

                        if (SleepController.Get().IsActive() && !SleepController.Get().IsWakingUp())
                        {
                            LocalPlayerConditionModule.IncreaseHP(num2 / num * Player.GetSleepTimeFactor());
                        }
                        else
                        {
                            LocalPlayerConditionModule.IncreaseHP(num2 / num * deltaTime);
                        }
                    }

                    if (LocalPlayerConditionModule.m_Oxygen <= 0f)
                    {
                        LocalPlayerConditionModule.IncreaseHP(0f - LocalMultipliers.GetCustomMultiplierValue(nameof(Multipliers.m_HealthLossPerSecondNoOxygen)) * deltaTime);
                    }
                }

                LocalPlayerConditionModule.m_HP = Mathf.Clamp(LocalPlayerConditionModule.GetHP(), 0f, LocalPlayerConditionModule.GetMaxHP());
                if (LocalPlayerConditionModule.m_HP < 10f)
                {
                    if (PlayerAudioModule.Get().IsHeartBeatSoundPlaying())
                    {
                        PlayerAudioModule.Get().PlayHeartBeatSound(1f, loop: true);
                    }
                }
                else if (PlayerAudioModule.Get().IsHeartBeatSoundPlaying())
                {
                    PlayerAudioModule.Get().StopHeartBeatSound();
                }
            }
        }

        protected virtual void UpdateDefaultHP()
        {
            if (IsModEnabled && UseDefault)
            {
                if (ScenarioManager.Get().IsDream() || Player.Get().IsInArenaMode())
                {
                    return;
                }

                float deltaTime = Time.deltaTime;
                if (!LocalFPPController || LocalDeathController.IsActive() || LocalConsciousnessController.IsActive())
                {
                    return;
                }

                if (!Cheats.m_GodMode && !Player.Get().IsDialogMode())
                {
                    if (LocalPlayerConditionModule.IsNutritionCarboCriticalLevel() || LocalPlayerConditionModule.IsNutritionFatCriticalLevel() || LocalPlayerConditionModule.IsNutritionProteinsCriticalLevel())
                    {
                        LocalPlayerConditionModule.IncreaseHP((0f - LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_HealthLossPerSecondNoNutrition))) * deltaTime);
                    }

                    if (LocalPlayerConditionModule.IsHydrationCriticalLevel())
                    {
                        LocalPlayerConditionModule.IncreaseHP((0f - LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_HealthLossPerSecondNoHydration))) * deltaTime);
                    }

                    bool flag = true;
                    List<Injury> injuriesList = PlayerInjuryModule.Get().GetInjuriesList();
                    for (int i = 0; i < injuriesList.Count; i++)
                    {
                        if (injuriesList[i].m_ParentInjury == null && injuriesList[i].m_Type != InjuryType.Worm && injuriesList[i].m_Type != InjuryType.Leech)
                        {
                            flag = false;
                            break;
                        }
                    }

                    if (flag)
                    {
                        float num = (MainLevel.Instance.m_TODTime.m_DayLengthInMinutes + MainLevel.Instance.m_TODTime.m_NightLengthInMinutes) * 60f;
                        float num2 = LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_HealthRecoveryPerDayNormalMode));
                        switch (DifficultySettings.ActivePreset.m_BaseDifficulty)
                        {
                            case GameDifficulty.Easy:
                                num2 = LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_HealthRecoveryPerDayEasyMode));
                                break;
                            case GameDifficulty.Hard:
                                num2 = LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_HealthRecoveryPerDayHardMode));
                                break;
                        }

                        if (SleepController.Get().IsActive() && !SleepController.Get().IsWakingUp())
                        {
                            LocalPlayerConditionModule.IncreaseHP(num2 / num * Player.GetSleepTimeFactor());
                        }
                        else
                        {
                            LocalPlayerConditionModule.IncreaseHP(num2 / num * deltaTime);
                        }
                    }

                    if (LocalPlayerConditionModule.m_Oxygen <= 0f)
                    {
                        LocalPlayerConditionModule.IncreaseHP((0f - LocalMultipliers.GetDefaultMultiplierValue(nameof(Multipliers.m_HealthLossPerSecondNoOxygen))) * deltaTime);
                    }
                }

                LocalPlayerConditionModule.m_HP = Mathf.Clamp(LocalPlayerConditionModule.m_HP, 0f, LocalPlayerConditionModule.GetMaxHP());
                if (LocalPlayerConditionModule.m_HP < 10f)
                {
                    if (PlayerAudioModule.Get().IsHeartBeatSoundPlaying())
                    {
                        PlayerAudioModule.Get().PlayHeartBeatSound(1f, loop: true);
                    }
                }
                else if (PlayerAudioModule.Get().IsHeartBeatSoundPlaying())
                {
                    PlayerAudioModule.Get().StopHeartBeatSound();
                }
            }
        }

        protected virtual void UpdateMaxStamina(bool usedefault)
        {
            if (usedefault)
            {
                UpdateDefaultMaxStamina();
            }
            else
            {
                UpdateCustomMaxStamina();
            }
        }

        protected virtual void UpdateCustomMaxStamina()
        {
            if (IsModEnabled && !UseDefault)
            {
                LocalMultipliers.m_MaxStamina = LocalPlayerConditionModule.GetEnergy();
                LocalMultipliers.m_MaxStamina = Mathf.Clamp(LocalMultipliers.m_MaxStamina, 0f, LocalPlayerConditionModule.GetMaxStamina());
            }
        }

        protected virtual void UpdateDefaultMaxStamina()
        {
            if (IsModEnabled && UseDefault)
            {
                LocalMultipliers.m_MaxStamina = LocalPlayerConditionModule.GetEnergy();
                LocalMultipliers.m_MaxStamina = Mathf.Clamp(LocalPlayerConditionModule.GetMaxStamina(), 0f, 100f);
            }
        }

        protected virtual void UpdateParameterLoss(bool blocked)
        {
            if (blocked)
            {
                BlockParametersLoss();
            }
            else
            {
                UnblockParametersLoss();
            }
        }

        protected virtual void UpdateNutrition(bool usedefault)
        {
            if (usedefault)
            {
                UpdateDefaultNutrition();
            }
            else
            {
                UpdateCustomNutrition();
            }
        }

        protected virtual void UpdateDefaultNutrition()
        {
            if (IsModEnabled && UseDefault)
            {
                ParasiteSickness m_ParasiteSickness = (ParasiteSickness)PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.ParasiteSickness);
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
                    if (m_ParasiteSickness != null && m_ParasiteSickness.IsActive())
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
        }

        protected virtual void UpdateCustomNutrition()
        {
            if (IsModEnabled && !UseDefault)
            {
                ParasiteSickness m_ParasiteSickness = (ParasiteSickness)PlayerDiseasesModule.Get().GetDisease(ConsumeEffect.ParasiteSickness) ?? default;
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
                        num2 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionRunMul));
                        num3 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionRunMul));
                        num4 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionRunMul));
                    }
                    if (flag)
                    {
                        num2 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionActionMul));
                        num3 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionActionMul));
                        num4 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionActionMul));
                    }
                    if (LocalPlayerConditionModule.IsNutritionCarboCriticalLevel())
                    {
                        num3 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionMulNoCarbs));
                        num4 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionMulNoCarbs));
                    }
                    if (InventoryBackpack.Get().IsCriticalOverload())
                    {
                        num2 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionWeightCriticalMul));
                        num3 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionWeightCriticalMul));
                        num4 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionWeightCriticalMul));
                    }
                    else if (InventoryBackpack.Get().IsOverload())
                    {
                        num2 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionWeightOverloadMul));
                        num3 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionWeightOverloadMul));
                        num4 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionWeightOverloadMul));
                    }
                    else
                    {
                        num2 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionWeightNormalMul));
                        num3 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionWeightNormalMul));
                        num4 *= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionWeightNormalMul));
                    }
                    if (m_ParasiteSickness != null && m_ParasiteSickness.IsActive())
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
                    LocalPlayerConditionModule.m_NutritionCarbo -= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionCarbohydratesConsumptionPerSecond)) * num * num2 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_CarboConsumptionMul : 1f);
                    LocalPlayerConditionModule.m_NutritionCarbo = Mathf.Clamp(LocalPlayerConditionModule.GetNutritionCarbo(), 0f, LocalPlayerConditionModule.GetMaxNutritionCarbo());
                    LocalPlayerConditionModule.m_NutritionFat -= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionFatConsumptionPerSecond)) * num * num3 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_FatConsumptionMul : 1f);
                    LocalPlayerConditionModule.m_NutritionFat = Mathf.Clamp(LocalPlayerConditionModule.GetNutritionFat(), 0f, LocalPlayerConditionModule.GetMaxNutritionFat());
                    LocalPlayerConditionModule.m_NutritionProteins -= GetCustomNutritionMultiplierValue(nameof(Multipliers.m_NutritionProteinsConsumptionPerSecond)) * num * num4 * (LocalPlayerCocaineModule.m_Active ? LocalPlayerCocaineModule.m_ProteinsConsumptionMul : 1f);
                    LocalPlayerConditionModule.m_NutritionProteins = Mathf.Clamp(LocalPlayerConditionModule.GetNutritionProtein(), 0f, LocalPlayerConditionModule.GetMaxNutritionProtein());
                }
            }
        }

        public virtual bool ResetParams()
        {
            try
            {
                LocalPlayerConditionModule.ResetParams();
                return true;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(ResetParams));
                return false;
            }

        }

        public virtual PlayerConditionData GetParams()
        {
            try
            {
                var conditionData = new PlayerConditionData
                {
                    m_Stamina = LocalPlayerConditionModule.GetStamina(),
                    m_MaxStamina = LocalPlayerConditionModule.GetMaxStamina(),
                    m_HP = LocalPlayerConditionModule.GetHP(),
                    m_MaxHP = LocalPlayerConditionModule.GetMaxHP(),
                    m_NutritionFat = LocalPlayerConditionModule.GetNutritionFat(),
                    m_MaxNutritionFat = LocalPlayerConditionModule.GetMaxNutritionFat(),
                    m_NutritionCarbo = LocalPlayerConditionModule.GetNutritionCarbo(),
                    m_MaxNutritionCarbo = LocalPlayerConditionModule.GetMaxNutritionCarbo(),
                    m_NutritionProteins = LocalPlayerConditionModule.GetNutritionProtein(),
                    m_MaxNutritionProteins = LocalPlayerConditionModule.GetMaxNutritionProtein(),
                    m_Hydration = LocalPlayerConditionModule.GetHydration(),
                    m_MaxHydration = LocalPlayerConditionModule.GetMaxHydration(),
                    m_Energy = LocalPlayerConditionModule.GetEnergy(),
                    m_MaxEnergy = LocalPlayerConditionModule.GetMaxEnergy(),
                    m_Dirtiness = LocalPlayerConditionModule.m_Dirtiness,
                    m_MaxDirtiness = LocalPlayerConditionModule.m_MaxDirtiness
                };
                return conditionData;
            }
            catch (Exception exc)
            {
                HandleException(exc, nameof(GetParams));
                return new PlayerConditionData();
            }

        }

        protected virtual void HandleException(Exception exc, string methodName)
        {
            string info = $"[{ModuleName}:{methodName}] throws exception -  {exc.TargetSite?.Name}:\n{exc.Message}\n{exc.InnerException}\n{exc.Source}\n{exc.StackTrace}";
            ModAPI.Log.Write(info);
            Debug.Log(info);
        }

        public virtual NutrientsDepletion GetActiveNutrientsDepletionPreset()
        {
            var _activeNutrientsDepletionPreset = DifficultySettings.ActivePreset.m_NutrientsDepletion;
            ActiveNutrientsDepletionPreset = _activeNutrientsDepletionPreset;
            return _activeNutrientsDepletionPreset;
        }

        public virtual bool SetActiveNutrientsDepletionPreset(int nutrientsDepletionIndex)
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

        public virtual string[] GetNutrientsDepletionNames()
        {
            return Enum.GetNames(typeof(NutrientsDepletion));
        }

        public virtual void GetCustomMultiplierSliders()
        {
            if (LocalMultipliers.CustomMultipliers != null)
            {
                CustomMultipliers = LocalMultipliers.CustomMultipliers;
               var customordered = CustomMultipliers.OrderBy(x => x.Key).ToArray();
            
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    foreach (KeyValuePair<string, float> customconditionMul in customordered)
                    {
                        float midpointRoundedToEvenValue = (float)Math.Round(CustomMultipliers[customconditionMul.Key], 2, MidpointRounding.ToEven);

                        SetConditionMultiplierContentColor(customconditionMul);
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"{customconditionMul.Key} ({midpointRoundedToEvenValue})");
                            CustomMultipliers[customconditionMul.Key] = GUILayout.HorizontalSlider(CustomMultipliers[customconditionMul.Key], 0f, customconditionMul.Value + 1f);
                            if (midpointRoundedToEvenValue != (float)Math.Round(CustomMultipliers[customconditionMul.Key], 2, MidpointRounding.ToEven))
                            {                             
                                HasChanged = SetCustomNutritionMultiplierValue(customconditionMul.Key, (float)Math.Round(CustomMultipliers[customconditionMul.Key], 2, MidpointRounding.ToEven));
                            }
                        }
                    }
                }
            }
        }

        public virtual void SetConditionMultiplierContentColor(KeyValuePair<string, float> multiplier)
        {
            if (multiplier.Key.ToLower().Contains("carbo"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Carbo);
            }
            if (multiplier.Key.ToLower().Contains("fat"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Fat);
            }
            if (multiplier.Key.ToLower().Contains("proteins"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Proteins);
            }
            if (multiplier.Key.ToLower().Contains("oxygen") || multiplier.Key.ToLower().Contains("hydration"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Hydration);
            }
            if (multiplier.Key.ToLower().Contains("energy") || multiplier.Key.ToLower().Contains("stamina") || multiplier.Key.ToLower().Contains("health"))
            {
                GUI.contentColor = IconColors.GetColor(IconColors.Icon.Energy);
            }
        }

        public virtual void GetDefaultMultiplierSliders()
        {
            if (LocalMultipliers.DefaultMultipliers != null)
            {
                DefaultMultipliers = LocalMultipliers.DefaultMultipliers;
                var orderedDefaultMultipliers = DefaultMultipliers.OrderBy(x => x.Key).ToList();

                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    foreach (KeyValuePair<string, float> defaultMul in orderedDefaultMultipliers)
                    {
                        SetConditionMultiplierContentColor(defaultMul);
                        using (new GUILayout.HorizontalScope(GUI.skin.box))
                        {
                            GUILayout.Label($"{defaultMul.Key} ({(float)Math.Round(DefaultMultipliers[defaultMul.Key], 2, MidpointRounding.ToEven)})");
                            GUILayout.HorizontalSlider(DefaultMultipliers[defaultMul.Key], 0f, defaultMul.Value + 1f);                         
                        }
                    }
                }
            }
        }

        public virtual float GetCustomNutritionMultiplierValue(string name)
        {
            if (CustomMultipliers.ContainsKey(name))
            {
                return CustomMultipliers[name];
            }
            else
            {
                return -1f;
            }
        }

        public virtual bool SetCustomNutritionMultiplierValue(string name, float value)
        {
            if (CustomMultipliers.ContainsKey(name))
            {
                CustomMultipliers[name] = value;
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}