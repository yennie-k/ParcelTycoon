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
    [SerializeField] private int paymentPerTick = 50;
    [SerializeField] private float paymentInterval = 0.1f;

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

    public bool CanUpgrade =>
        workerSpeedLevel < maxWorkerSpeedLevel;

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

        if (player == null)
        {
            return;
        }

        if (!CanUpgrade)
        {
            return;
        }

        if (paymentCoroutine != null)
        {
            return;
        }

        paymentCoroutine =
            StartCoroutine(
                PayForUpgrade()
            );
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement player =
            other.GetComponent<PlayerMovement>();

        if (player == null)
        {
            return;
        }

        if (paymentCoroutine != null)
        {
            StopCoroutine(
                paymentCoroutine
            );

            paymentCoroutine = null;
        }
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
    }

    private void CompleteUpgrade()
    {
        if (!CanUpgrade)
        {
            paymentCoroutine = null;
            UpdateText();
            return;
        }

        workerSpeedLevel++;

        if (deliveryWorker != null)
        {
            deliveryWorker.IncreaseMoveSpeed(
                speedIncreasePerLevel
            );
        }

        if (inputWorker != null)
        {
            inputWorker.IncreaseMoveSpeed(
                speedIncreasePerLevel
            );
        }

        Debug.Log(
            $"직원 속도 업그레이드! Lv.{workerSpeedLevel}"
        );

        paidAmount = 0;
        paymentCoroutine = null;

        UpdateText();
    }

    private void UpdateText()
    {
        if (priceText == null)
        {
            return;
        }

        if (!CanUpgrade)
        {
            priceText.text =
                $"Worker MAX\nLv.{workerSpeedLevel}";

            return;
        }

        int remaining =
            CurrentCost - paidAmount;

        priceText.text =
            $"Worker Lv.{workerSpeedLevel}\n" +
            $"{remaining:N0}";
    }
}