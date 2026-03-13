using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class HexTacticsCanvasBootstrap
{
    public static HexTacticsCanvasView EnsureView()
    {
        EnsureEventSystem();

        var existing = Object.FindFirstObjectByType<HexTacticsCanvasView>();
        if (existing != null)
        {
            existing.EnsureBuilt();
            return existing;
        }

        var prefab = Resources.Load<GameObject>(HexTacticsUiResourcePaths.CanvasRoot);
        if (prefab != null)
        {
            var instance = Object.Instantiate(prefab);
            instance.name = "HexTacticsCanvasRoot";
            var viewFromPrefab = instance.GetComponent<HexTacticsCanvasView>();
            if (viewFromPrefab == null)
            {
                viewFromPrefab = instance.AddComponent<HexTacticsCanvasView>();
            }

            viewFromPrefab.EnsureBuilt();
            return viewFromPrefab;
        }

        var root = new GameObject("HexTacticsCanvasRoot", typeof(RectTransform));
        var view = root.AddComponent<HexTacticsCanvasView>();
        view.EnsureBuilt();
        return view;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
