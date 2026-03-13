using UnityEngine;

public static class HexTacticsCompositionRoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        var prototypes = Object.FindObjectsByType<HexTacticsPrototype>(FindObjectsSortMode.None);
        for (var i = 0; i < prototypes.Length; i++)
        {
            if (prototypes[i].GetComponent<HexTacticsCanvasPresenter>() == null)
            {
                prototypes[i].gameObject.AddComponent<HexTacticsCanvasPresenter>();
            }
        }
    }
}
