using UnityEngine;

public abstract class HexTacticsUiGeneratedView : MonoBehaviour
{
    [SerializeField] private int layoutVersion;
    [SerializeField] private int sharedStyleVersion;

    // Shared font/spacing changes must also reach instances of saved prefabs.
    private const int CurrentSharedStyleVersion = 1;

    protected abstract int CurrentLayoutVersion { get; }
    protected abstract bool HasCurrentBindings { get; }

    protected virtual void Awake()
    {
        EnsureBuilt();
    }

    public void EnsureBuilt()
    {
        if (layoutVersion == CurrentLayoutVersion &&
            sharedStyleVersion == CurrentSharedStyleVersion && HasCurrentBindings)
        {
            return;
        }

        BuildDefaultHierarchy();
        layoutVersion = CurrentLayoutVersion;
        sharedStyleVersion = CurrentSharedStyleVersion;
    }

    public abstract void BuildDefaultHierarchy();
}
