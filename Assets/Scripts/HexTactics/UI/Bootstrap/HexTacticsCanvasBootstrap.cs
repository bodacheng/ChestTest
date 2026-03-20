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

        var prefab = HexTacticsUiFactory.LoadViewPrefab<HexTacticsCanvasView>(HexTacticsUiResourcePaths.CanvasRoot);
        if (prefab != null)
        {
            var instance = Object.Instantiate(prefab);
            instance.name = "HexTacticsCanvasRoot";
            instance.EnsureBuilt();
            return instance;
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
