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

    [Header("Upgrade Zones")]
    [SerializeField] private GameObject sortingSpeedUpgradeZone;
    [SerializeField] private GameObject carryCapacityUpgradeZone;
    [SerializeField] private GameObject workerSpeedUpgradeZone;

    public enum GoalId
    {
        Loading = 0, ConfigurationMissing = 1,
        SecondInbound = 10, DeliveryWorker = 20, InputWorker = 30,
        WarehouseExpansion = 40, SortingLevel2 = 50, CarryLevel2 = 60,
        WorkerSpeedLevel2 = 70, AllUpgradesMax = 80, Complete = 90
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
        new GoalDefinition(GoalId.WarehouseExpansion, "목표: 창고 확장", "Goal: Expand warehouse"),
        new GoalDefinition(GoalId.SortingLevel2, "목표: 분류 속도 Lv.2 달성", "Goal: Reach sorting Lv.2"),
        new GoalDefinition(GoalId.CarryLevel2, "목표: 운반량 Lv.2 달성", "Goal: Reach carry Lv.2"),
        new GoalDefinition(GoalId.WorkerSpeedLevel2, "목표: 직원 속도 Lv.2 달성", "Goal: Reach worker speed Lv.2"),
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
            case GoalId.SortingLevel2: return sortingStation.SortingLevel >= 2;
            case GoalId.CarryLevel2: return playerCarry.CarryLevel >= 2;
            case GoalId.WorkerSpeedLevel2: return workerSpeedUpgrade.WorkerSpeedLevel >= 2;
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

    public void RefreshProgression()
    {
        if (!SaveManager.GameplayReady) return;
        bool secondInboundUnlocked =
            secondInbound != null &&
            secondInbound.activeSelf;

        bool deliveryWorkerUnlocked =
            deliveryWorker != null &&
            deliveryWorker.activeSelf;

        bool inputWorkerUnlocked =
            inputWorker != null &&
            inputWorker.activeSelf;

        // -------------------------
        // 1단계
        // 두 번째 입고 시설
        // -------------------------

        if (!secondInboundUnlocked)
        {
            SetZone(
                secondInboundPurchaseZone,
                true
            );

            SetZone(
                deliveryWorkerHireZone,
                false
            );

            SetZone(
                inputWorkerHireZone,
                false
            );

            SetUpgradeZones(false);

            return;
        }

        // -------------------------
        // 2단계
        // 출고 직원 고용
        // -------------------------

        if (!deliveryWorkerUnlocked)
        {
            SetZone(
                secondInboundPurchaseZone,
                false
            );

            SetZone(
                deliveryWorkerHireZone,
                true
            );

            SetZone(
                inputWorkerHireZone,
                false
            );

            SetUpgradeZones(false);

            return;
        }

        // -------------------------
        // 3단계
        // 입고 직원 고용
        // -------------------------

        if (!inputWorkerUnlocked)
        {
            SetZone(
                secondInboundPurchaseZone,
                false
            );

            SetZone(
                deliveryWorkerHireZone,
                false
            );

            SetZone(
                inputWorkerHireZone,
                true
            );

            SetUpgradeZones(false);

            return;
        }

        // -------------------------
        // 자동화 완료
        // 모든 업그레이드 개방
        // -------------------------

        SetZone(
            secondInboundPurchaseZone,
            false
        );

        SetZone(
            deliveryWorkerHireZone,
            false
        );

        SetZone(
            inputWorkerHireZone,
            false
        );

        SetUpgradeZones(true);
    }

    private void SetUpgradeZones(bool active)
    {
        SetZone(
            sortingSpeedUpgradeZone,
            active
        );

        SetZone(
            carryCapacityUpgradeZone,
            active
        );

        SetZone(
            workerSpeedUpgradeZone,
            active
        );
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