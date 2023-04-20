using ModTime.Data.Player.Condition;
using ModTime.Managers;
using UnityEngine;

namespace ModTime.Extensions
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModTime)}__").AddComponent<ModTime>();
            new GameObject($"__{nameof(WeatherManager)}__").AddComponent<WeatherManager>();
            new GameObject($"__{nameof(HealthManager)}__").AddComponent<HealthManager>();
            new GameObject($"__{nameof(TimeManager)}__").AddComponent<TimeManager>();
            new GameObject($"__{nameof(Multipliers)}__").AddComponent<Multipliers>();
        }
    }
}
