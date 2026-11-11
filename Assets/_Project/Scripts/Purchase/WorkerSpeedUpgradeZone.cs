using System.Collections;
using TMPro;
using UnityEngine;

public class WorkerSpeedUpgradeZone : MonoBehaviour
{
    [Header("Upgrade Settings")]
    [SerializeField] private int workerSpeedLevel = 1;
    [SerializeField] private int maxWorkerSpeedLevel = 4;

    [SerializeField]
    private float speedIncreasePerLevel = 0.75f;

    [Header("Upgrade Cost")]
    [SerializeField] private int baseCost = 1000;
    [SerializeField] private int costMultiplier = 2;

    [Header("Payment Settings")]
    [SerializeField] private int paymentPerTick = 100;
    [SerializeField] private float paymentInterval = 0.2f;

    [Header("References")]
    [SerializeField] private MoneyManager moneyManager;

    [SerializeField]
    private WorkerAI deliveryWorker;

    [SerializeField]
    private InputWorkerAI inputWorker;

    [SerializeField]
    private TMP_Text priceText;

    private int paidAmount;
    private Coroutine paymentCoroutine;

    public int WorkerSpeedLevel => workerSpeedLevel;
    public int MaxWorkerSpeedLevel => maxWorkerSpeedLevel;

    public bool CanUpgrade =>
        workerSpeedLevel < maxWorkerSpeedLevel;

    private bool paymentRunning;
    public bool HasSaveReferences => moneyManager != null && inputWorker != null && deliveryWorker != null;

    public UpgradeSaveData CaptureState()
    {
        return new UpgradeSaveData { level = workerSpeedLevel, paidAmount = CanUpgrade ? paidAmount : 0 };
    }

    public bool IsValidSave(UpgradeSaveData data)
    {
        if (data == null || data.level < 1 || data.level > maxWorkerSpeedLevel || data.paidAmount < 0) return false;
        if (data.level == maxWorkerSpeedLevel) return data.paidAmount == 0;
        int cost = baseCost * (int)Mathf.Pow(costMultiplier, data.level - 1);
        return data.paidAmount <= cost;
    }

    public void RestoreState(UpgradeSaveData data)
    {
        StopPayment();
        RestoreWorkerLevel(data.level);
        paidAmount = data.paidAmount;
        // Normalize a fully paid tier without spending or firing gameplay completion events.
        if (CanUpgrade && paidAmount == CurrentCost)
        {
            RestoreWorkerLevel(data.level + 1);
            paidAmount = 0;
        }
        if (!CanUpgrade) paidAmount = 0;
        UpdateText();
    }

    private void StopPayment()
    {
        if (paymentCoroutine != null) StopCoroutine(paymentCoroutine);
        paymentCoroutine = null;
        paymentRunning = false;
    }

    private void OnDisable() { StopPayment(); }

    public void RestoreWorkerLevel(int level)
    {
        workerSpeedLevel = Mathf.Clamp(level, 1, maxWorkerSpeedLevel);
        float bonus = (workerSpeedLevel - 1) * speedIncreasePerLevel;
        if (deliveryWorker != null) deliveryWorker.RestoreMoveSpeedBonus(bonus);
        if (inputWorker != null) inputWorker.RestoreMoveSpeedBonus(bonus);
    }

    private int CurrentCost
    {
        get
        {
            int upgradeCount =
                workerSpeedLevel - 1;

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

        if (!CanUpgrade)
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
        if (!CanUpgrade)
        {
            paymentCoroutine = null;
        paymentRunning = false;
            UpdateText();
            return;
        }

        RestoreWorkerLevel(workerSpeedLevel + 1);

        Debug.Log(
            $"직원 속도 업그레이드! Lv.{workerSpeedLevel}"
        );

        paidAmount = 0;
        paymentCoroutine = null;
        paymentRunning = false;

        UpdateText();
        SaveManager.Instance?.SaveNow();
    }

    private void UpdateText()
    {
        if (priceText == null) return;
        priceText.text = PurchasePadText.Upgrade("직원 이동 속도", workerSpeedLevel,
            CanUpgrade, CurrentCost, paidAmount, SpeedText(0f), SpeedText(CanUpgrade ? speedIncreasePerLevel : 0f));
    }

    private string SpeedText(float increase)
    {
        if (inputWorker != null && deliveryWorker != null &&
            !Mathf.Approximately(inputWorker.MoveSpeed, deliveryWorker.MoveSpeed))
            return $"입고 {inputWorker.MoveSpeed + increase:F2} / 출고 {deliveryWorker.MoveSpeed + increase:F2}";
        if (inputWorker != null) return $"{inputWorker.MoveSpeed + increase:F2}";
        if (deliveryWorker != null) return $"{deliveryWorker.MoveSpeed + increase:F2}";
        return "미연결";
    }
}
