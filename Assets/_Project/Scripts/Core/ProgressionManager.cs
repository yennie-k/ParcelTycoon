using UnityEngine;

public class ProgressionManager : MonoBehaviour
{
    [Header("Progress Targets")]
    [SerializeField] private GameObject secondInbound;
    [SerializeField] private GameObject deliveryWorker;
    [SerializeField] private GameObject inputWorker;

    [Header("Goal State References")]
    [SerializeField] private GameObject warehouseExpansion;
    [SerializeField] private SortingStation sortingStation;
    [SerializeField] private PlayerCarry playerCarry;
    [SerializeField] private WorkerSpeedUpgradeZone workerSpeedUpgrade;

    [Header("Purchase Zones")]
    [SerializeField] private GameObject secondInboundPurchaseZone;
    [SerializeField] private GameObject deliveryWorkerHireZone;
    [SerializeField] private GameObject inputWorkerHireZone;

    [SerializeField] private GameObject warehouseExpansionPurchaseZone;

    [Header("Upgrade Zones")]
    [SerializeField] private GameObject sortingSpeedUpgradeZone;
    [SerializeField] private GameObject carryCapacityUpgradeZone;
    [SerializeField] private GameObject workerSpeedUpgradeZone;

    public enum GoalId
    {
        Loading = 0, ConfigurationMissing = 1,
        SecondInbound = 10, DeliveryWorker = 20, InputWorker = 30,
        BuyTwoUpgrades = 35, WarehouseExpansion = 40, AllUpgradesLevel2 = 50,
        AnyUpgradeMax = 70, AllUpgradesMax = 80, Complete = 90
    }

    private sealed class GoalDefinition
    {
        public readonly GoalId Id;
        public readonly string Korean;
        public readonly string English;
        public GoalDefinition(GoalId id, string korean, string english)
        { Id = id; Korean = korean; English = english; }
    }

    // Display order only. Adding a goal never changes purchase-pad availability.
    private static readonly GoalDefinition[] Goals =
    {
        new GoalDefinition(GoalId.SecondInbound, "목표: 두 번째 입고 시설 해금", "Goal: Unlock second inbound"),
        new GoalDefinition(GoalId.DeliveryWorker, "목표: 출고 직원 고용", "Goal: Hire delivery worker"),
        new GoalDefinition(GoalId.InputWorker, "목표: 입고 직원 고용", "Goal: Hire input worker"),
        new GoalDefinition(GoalId.BuyTwoUpgrades, "목표: 업그레이드 2회 구매", "Buy 2 Upgrades"),
        new GoalDefinition(GoalId.WarehouseExpansion, "목표: 창고 확장", "Goal: Expand warehouse"),
        new GoalDefinition(GoalId.AllUpgradesLevel2, "목표: 모든 업그레이드 Lv.2 이상 달성", "Goal: Reach Lv.2 in all upgrades"),
        new GoalDefinition(GoalId.AnyUpgradeMax, "목표: 업그레이드 1종 MAX 달성", "Goal: Max out one upgrade"),
        new GoalDefinition(GoalId.AllUpgradesMax, "목표: 모든 업그레이드 MAX 달성", "Goal: Max out all upgrades")
    };

    public GoalId CurrentGoal
    {
        get
        {
            if (!SaveManager.GameplayReady) return GoalId.Loading;
            if (secondInbound == null || deliveryWorker == null || inputWorker == null ||
                warehouseExpansion == null || sortingStation == null || playerCarry == null || workerSpeedUpgrade == null)
                return GoalId.ConfigurationMissing;
            foreach (GoalDefinition goal in Goals)
                if (!IsGoalAchieved(goal.Id)) return goal.Id;
            return GoalId.Complete;
        }
    }

    public string GetGoalText(GoalId id, bool korean = true)
    {
        foreach (GoalDefinition goal in Goals)
            if (goal.Id == id) return korean ? goal.Korean : goal.English;
        if (id == GoalId.Complete) return korean ? "모든 성장 목표 완료!" : "All growth goals completed!";
        if (id == GoalId.Loading) return korean ? "진행 상태 불러오는 중…" : "Loading progress...";
        return korean ? "목표 설정을 확인해 주세요" : "Check goal references";
    }

    private bool IsGoalAchieved(GoalId id)
    {
        switch (id)
        {
            case GoalId.SecondInbound: return secondInbound.activeSelf;
            case GoalId.DeliveryWorker: return deliveryWorker.activeSelf;
            case GoalId.InputWorker: return inputWorker.activeSelf;
            case GoalId.WarehouseExpansion: return warehouseExpansion.activeSelf;
            case GoalId.BuyTwoUpgrades: return PurchasedUpgradeCount >= 2;
            case GoalId.AllUpgradesLevel2:
                return sortingStation.SortingLevel >= 2 && playerCarry.CarryLevel >= 2 && workerSpeedUpgrade.WorkerSpeedLevel >= 2;
            case GoalId.AnyUpgradeMax:
                return !sortingStation.CanUpgradeSorting || !playerCarry.CanUpgradeCarry || !workerSpeedUpgrade.CanUpgrade;
            case GoalId.AllUpgradesMax:
                return !sortingStation.CanUpgradeSorting && !playerCarry.CanUpgradeCarry && !workerSpeedUpgrade.CanUpgrade;
            default: return false;
        }
    }

    private void Start()
    {
        RefreshProgression();
    }

    private void Update()
    {
        RefreshProgression();
    }

    // Derived from completed levels, never from partial payments or an extra save field.
    public int PurchasedUpgradeCount =>
        (sortingStation == null ? 0 : Mathf.Max(0, sortingStation.SortingLevel - 1)) +
        (playerCarry == null ? 0 : Mathf.Max(0, playerCarry.CarryLevel - 1)) +
        (workerSpeedUpgrade == null ? 0 : Mathf.Max(0, workerSpeedUpgrade.WorkerSpeedLevel - 1));

    public void RefreshProgression()
    {
        if (!SaveManager.GameplayReady) return;
        bool inbound = secondInbound != null && secondInbound.activeSelf;
        bool delivery = deliveryWorker != null && deliveryWorker.activeSelf;
        bool input = inputWorker != null && inputWorker.activeSelf;

        SetZone(secondInboundPurchaseZone, !inbound);
        SetZone(deliveryWorkerHireZone, inbound && !delivery);
        SetZone(inputWorkerHireZone, inbound && delivery && !input);
        SetZone(carryCapacityUpgradeZone, inbound);
        SetZone(sortingSpeedUpgradeZone, inbound && delivery);
        SetZone(workerSpeedUpgradeZone, inbound && delivery && input);
        SetZone(warehouseExpansionPurchaseZone, inbound && delivery && input &&
            PurchasedUpgradeCount >= 2 && warehouseExpansion != null && !warehouseExpansion.activeSelf);
    }

    private void SetZone(
        GameObject zone,
        bool active)
    {
        if (zone == null)
        {
            return;
        }

        if (zone.activeSelf != active)
        {
            zone.SetActive(active);
        }
    }
}