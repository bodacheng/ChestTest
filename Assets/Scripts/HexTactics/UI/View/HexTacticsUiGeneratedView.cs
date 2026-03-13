using UnityEngine;

public abstract class HexTacticsUiGeneratedView : MonoBehaviour
{
    [SerializeField] private int layoutVersion;

    protected abstract int CurrentLayoutVersion { get; }
    protected abstract bool HasCurrentBindings { get; }

    protected virtual void Awake()
    {
        EnsureBuilt();
    }

    public void EnsureBuilt()
    {
        if (layoutVersion == CurrentLayoutVersion && HasCurrentBindings)
        {
            return;
        }

        BuildDefaultHierarchy();
        layoutVersion = CurrentLayoutVersion;
    }

    public abstract void BuildDefaultHierarchy();
}
