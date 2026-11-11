using System.Collections;
using TMPro;
using UnityEngine;

public sealed class WorkerCarryCapacityUpgradeZone : MonoBehaviour
{
    [SerializeField] private int currentLevel = 1;
    [SerializeField] private int baseCost = 2000;
    [SerializeField] private int costMultiplier = 2;
    [SerializeField] private int paymentPerTick = 100;
    [SerializeField] private float paymentInterval = 0.2f;
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private WorkerAI deliveryWorker;
    [SerializeField] private InputWorkerAI inputWorker;
    [SerializeField] private TMP_Text priceText;

    private int paidAmount;
    private Coroutine payment;
    private bool paying;

    public int CurrentLevel => currentLevel;
    public int MaxLevel => 3;
    public bool CanUpgrade => currentLevel < MaxLevel;
    public bool HasSaveReferences => moneyManager != null && deliveryWorker != null && inputWorker != null;
    public int CurrentCost => CanUpgrade ? CostAt(currentLevel) : 0;

    private int CostAt(int level) => baseCost * (int)Mathf.Pow(costMultiplier, level - 1);

    public UpgradeSaveData CaptureState() =>
        new UpgradeSaveData { level = currentLevel, paidAmount = CanUpgrade ? paidAmount : 0 };

    public bool IsValidSave(UpgradeSaveData data) =>
        data != null && data.level >= 1 && data.level <= MaxLevel && data.paidAmount >= 0 &&
        data.paidAmount <= (data.level < MaxLevel ? CostAt(data.level) : 0);

    public void RestoreState(UpgradeSaveData data)
    {
        StopPayment();
        currentLevel = Mathf.Clamp(data.level, 1, MaxLevel);
        paidAmount = CanUpgrade ? data.paidAmount : 0;
        if (CanUpgrade && paidAmount == CurrentCost)
        {
            currentLevel++;
            paidAmount = 0;
        }
        ApplyCapacity();
        UpdateText();
    }

    private void ApplyCapacity()
    {
        if (inputWorker != null) inputWorker.SetMaxCarryCount(currentLevel);
        if (deliveryWorker != null) deliveryWorker.SetMaxCarryCount(currentLevel);
    }

    private void Start()
    {
        ApplyCapacity();
        UpdateText();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!SaveManager.GameplayReady || !CanUpgrade || paying || moneyManager == null ||
            other.GetComponent<PlayerMovement>() == null) return;
        paying = true;
        Coroutine started = StartCoroutine(PayForUpgrade());
        payment = paying ? started : null;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerMovement>() != null) StopPayment();
    }

    private void OnDisable() => StopPayment();

    private void StopPayment()
    {
        if (payment != null) StopCoroutine(payment);
        payment = null;
        paying = false;
    }

    private IEnumerator PayForUpgrade()
    {
        int cost = CurrentCost;
        while (paidAmount < cost)
        {
            int spent = moneyManager.SpendMoney(Mathf.Min(paymentPerTick, cost - paidAmount));
            paidAmount += spent;
            if (spent > 0) SaveManager.Instance?.RequestSave();
            if (paidAmount >= cost)
            {
                currentLevel++;
                paidAmount = 0;
                payment = null;
                paying = false;
                ApplyCapacity();
                UpdateText();
                SaveManager.Instance?.SaveNow();
                yield break;
            }
            UpdateText();
            yield return new WaitForSeconds(paymentInterval);
        }
        payment = null;
        paying = false;
    }

    private void UpdateText()
    {
        if (priceText == null) return;
        priceText.text = PurchasePadText.Upgrade("직원 운반량", currentLevel, CanUpgrade, CurrentCost,
            paidAmount, CanUpgrade ? $"{currentLevel}개" : $"최대 {currentLevel}개",
            $"{Mathf.Min(currentLevel + 1, MaxLevel)}개");
    }
}
