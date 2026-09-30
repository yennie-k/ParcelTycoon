using System.Collections;
using TMPro;
using UnityEngine;

public class SortingSpeedUpgradeZone : MonoBehaviour
{
    [Header("Upgrade Cost")]
    [SerializeField] private int baseCost = 500;
    [SerializeField] private int costMultiplier = 2;

    [Header("Payment Settings")]
    [SerializeField] private int paymentPerTick = 50;
    [SerializeField] private float paymentInterval = 0.1f;

    [Header("References")]
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private SortingStation sortingStation;
    [SerializeField] private TMP_Text priceText;

    private int paidAmount;
    private Coroutine paymentCoroutine;

    private int CurrentCost
    {
        get
        {
            int upgradeCount =
                sortingStation.SortingLevel - 1;

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

        if (sortingStation == null ||
            !sortingStation.CanUpgradeSorting)
        {
            return;
        }

        if (paymentCoroutine != null)
        {
            return;
        }

        paymentCoroutine =
            StartCoroutine(PayForUpgrade());
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
            StopCoroutine(paymentCoroutine);
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
        bool upgraded =
            sortingStation.UpgradeSortingSpeed();

        paidAmount = 0;
        paymentCoroutine = null;

        UpdateText();

        if (!upgraded)
        {
            Debug.Log(
                "분류 속도가 이미 최대 레벨입니다."
            );
        }
    }

    private void UpdateText()
    {
        if (priceText == null ||
            sortingStation == null)
        {
            return;
        }

        if (!sortingStation.CanUpgradeSorting)
        {
            priceText.text =
                $"Sort MAX\n{sortingStation.SortingTime:F1}s";

            return;
        }

        int remaining =
            CurrentCost - paidAmount;

        priceText.text =
            $"Sort Lv.{sortingStation.SortingLevel}\n" +
            $"{remaining:N0}";
    }
}