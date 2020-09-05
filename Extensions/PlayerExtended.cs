using UnityEngine;

namespace ModTime
{
    class PlayerExtended : Player
    {
        protected override void Start()
        {
            base.Start();
            new GameObject($"__{nameof(ModTime)}__").AddComponent<ModTime>();
        }
    }
}
