using UnityEngine;

public class ProgressionManager : MonoBehaviour
{
    [Header("Progress Targets")]
    [SerializeField] private GameObject secondInbound;
    [SerializeField] private GameObject deliveryWorker;
    [SerializeField] private GameObject inputWorker;

    [Header("Purchase Zones")]
    [SerializeField] private GameObject secondInboundPurchaseZone;
    [SerializeField] private GameObject deliveryWorkerHireZone;
    [SerializeField] private GameObject inputWorkerHireZone;

    [Header("Upgrade Zones")]
    [SerializeField] private GameObject sortingSpeedUpgradeZone;
    [SerializeField] private GameObject carryCapacityUpgradeZone;
    [SerializeField] private GameObject workerSpeedUpgradeZone;

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