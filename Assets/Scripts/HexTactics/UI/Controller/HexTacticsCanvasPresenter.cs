using UnityEngine;

[DefaultExecutionOrder(200)]
[DisallowMultipleComponent]
[RequireComponent(typeof(HexTacticsPrototype))]
public sealed class HexTacticsCanvasPresenter : MonoBehaviour
{
    private HexTacticsPrototype prototype;
    private HexTacticsCanvasView view;

    private void Awake()
    {
        prototype = GetComponent<HexTacticsPrototype>();
        view = HexTacticsCanvasBootstrap.EnsureView();
    }

    private void LateUpdate()
    {
        if (prototype == null)
        {
            return;
        }

        if (view == null)
        {
            view = HexTacticsCanvasBootstrap.EnsureView();
        }

        view.Render(prototype.BuildUiSnapshot(), new HexTacticsCanvasView.Actions(
            prototype.UiStartCpuMode,
            prototype.UiReturnToModeSelect,
            prototype.UiAddCharacterToPlayerTeam,
            prototype.UiPlaceRosterCharacterAt,
            prototype.UiRemovePlayerCharacterAt,
            prototype.UiMovePlacedCharacterTo,
            prototype.UiTryStartCpuBattle,
            prototype.UiClearSelection,
            prototype.UiSetSelectedUnitWait,
            prototype.UiSelectSelectedUnitSkill,
            prototype.UiSelectUnit,
            prototype.UiSetUnitWait,
            prototype.UiCycleUnitSkill,
            prototype.UiReturnToTeamBuilder,
            prototype.UiRetryBattle));
    }
}
