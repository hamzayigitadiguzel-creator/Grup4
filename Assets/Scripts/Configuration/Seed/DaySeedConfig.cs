using UnityEngine;

[CreateAssetMenu(menuName = "Storia/Prototype/Day Seed Config")]
public sealed class DaySeedConfig : ScriptableObject
{
    [Header("Developer Settings")]
    [SerializeField] private bool _useFixedSeedInEditor = true;

    [SerializeField] private int _fixedSeed = 12345;

    public int GetRunSeed()
    {
#if UNITY_EDITOR
        if (_useFixedSeedInEditor)
            return _fixedSeed;
#endif
        // Build'de veya editor'da fixed kapalıysa:
        return System.Environment.TickCount;
    }

#if UNITY_EDITOR
    public bool UseFixedSeedInEditor => _useFixedSeedInEditor;
    public int FixedSeed => _fixedSeed;
#endif
}
