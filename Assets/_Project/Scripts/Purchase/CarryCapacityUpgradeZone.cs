using System.Collections;
using TMPro;
using UnityEngine;

public class CarryCapacityUpgradeZone : MonoBehaviour
{
    [Header("Upgrade Cost")]
    [SerializeField] private int baseCost = 800;
    [SerializeField] private int costMultiplier = 2;

    [Header("Payment Settings")]
    [SerializeField] private int paymentPerTick = 100;
    [SerializeField] private float paymentInterval = 0.2f;

    [Header("References")]
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private PlayerCarry playerCarry;
    [SerializeField] private TMP_Text priceText;

    private int paidAmount;
    private Coroutine paymentCoroutine;

    private bool paymentRunning;
    public bool HasSaveReferences => moneyManager != null && playerCarry != null;

    public UpgradeSaveData CaptureState()
    {
        return new UpgradeSaveData { level = playerCarry.CarryLevel, paidAmount = playerCarry.CanUpgradeCarry ? paidAmount : 0 };
    }

    public bool IsValidSave(UpgradeSaveData data)
    {
        if (data == null || data.level < 1 || data.level > playerCarry.MaxCarryLevel || data.paidAmount < 0) return false;
        if (data.level == playerCarry.MaxCarryLevel) return data.paidAmount == 0;
        int cost = baseCost * (int)Mathf.Pow(costMultiplier, data.level - 1);
        return data.paidAmount <= cost;
    }

    public void RestoreState(UpgradeSaveData data)
    {
        StopPayment();
        playerCarry.RestoreCarryLevel(data.level);
        paidAmount = data.paidAmount;
        // Normalize a fully paid tier without spending or firing gameplay completion events.
        if (playerCarry.CanUpgradeCarry && paidAmount == CurrentCost)
        {
            playerCarry.RestoreCarryLevel(data.level + 1);
            paidAmount = 0;
        }
        if (!playerCarry.CanUpgradeCarry) paidAmount = 0;
        UpdateText();
    }

    private void StopPayment()
    {
        if (paymentCoroutine != null) StopCoroutine(paymentCoroutine);
        paymentCoroutine = null;
        paymentRunning = false;
    }

    private void OnDisable() { StopPayment(); }

    private int CurrentCost
    {
        get
        {
            int upgradeCount =
                playerCarry.CarryLevel - 1;

            return baseCost *
                (int)Mathf.Pow(
                    costMultiplier,
                    upgradeCount
                );
        }
    }

    private void Start()
    {
        UpdateText();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player =
            other.GetComponent<PlayerMovement>();

        if (player == null || !SaveManager.GameplayReady)
        {
            return;
        }

        if (playerCarry == null ||
            !playerCarry.CanUpgradeCarry)
        {
            return;
        }

        if (paymentRunning || moneyManager == null)
        {
            return;
        }

        paymentRunning = true;
        Coroutine started = StartCoroutine(PayForUpgrade());
        paymentCoroutine = paymentRunning ? started : null;
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement player =
            other.GetComponent<PlayerMovement>();

        if (player == null)
        {
            return;
        }

        StopPayment();
    }

    private IEnumerator PayForUpgrade()
    {
        while (paidAmount < CurrentCost)
        {
            int remainingCost =
                CurrentCost - paidAmount;

            int requestedPayment =
                Mathf.Min(
                    paymentPerTick,
                    remainingCost
                );

            int spent =
                moneyManager.SpendMoney(
                    requestedPayment
                );

            paidAmount += spent;
            if (spent > 0) SaveManager.Instance?.RequestSave();

            UpdateText();

            if (paidAmount >= CurrentCost)
            {
                CompleteUpgrade();
                yield break;
            }

            yield return new WaitForSeconds(
                paymentInterval
            );
        }

        paymentCoroutine = null;
        paymentRunning = false;
    }

    private void CompleteUpgrade()
    {
        bool upgraded =
            playerCarry.UpgradeCarryCapacity();

        paidAmount = 0;
        paymentCoroutine = null;
        paymentRunning = false;

        UpdateText();
        SaveManager.Instance?.SaveNow();

        if (!upgraded)
        {
            Debug.Log(
                "운반량이 이미 최대 레벨입니다."
            );
        }
    }

    private void UpdateText()
    {
        if (priceText == null || playerCarry == null) return;
        priceText.text = PurchasePadText.Upgrade("플레이어 운반량", playerCarry.CarryLevel,
            playerCarry.CanUpgradeCarry, CurrentCost, paidAmount,
            playerCarry.CanUpgradeCarry ? $"{playerCarry.MaxCarryCount}개" : $"최대 {playerCarry.MaxCarryCount}개",
            $"{playerCarry.NextCarryCount}개");
    }
}