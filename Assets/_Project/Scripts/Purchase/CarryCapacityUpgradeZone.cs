using System.Collections;
using TMPro;
using UnityEngine;

public class CarryCapacityUpgradeZone : MonoBehaviour
{
    [Header("Upgrade Cost")]
    [SerializeField] private int baseCost = 750;
    [SerializeField] private int costMultiplier = 2;

    [Header("Payment Settings")]
    [SerializeField] private int paymentPerTick = 50;
    [SerializeField] private float paymentInterval = 0.1f;

    [Header("References")]
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private PlayerCarry playerCarry;
    [SerializeField] private TMP_Text priceText;

    private int paidAmount;
    private Coroutine paymentCoroutine;

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

        if (player == null)
        {
            return;
        }

        if (playerCarry == null ||
            !playerCarry.CanUpgradeCarry)
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
            playerCarry.UpgradeCarryCapacity();

        paidAmount = 0;
        paymentCoroutine = null;

        UpdateText();

        if (!upgraded)
        {
            Debug.Log(
                "운반량이 이미 최대 레벨입니다."
            );
        }
    }

    private void UpdateText()
    {
        if (priceText == null ||
            playerCarry == null)
        {
            return;
        }

        if (!playerCarry.CanUpgradeCarry)
        {
            priceText.text =
                $"Carry\nMAX\n{playerCarry.MaxCarryCount}";

            return;
        }

        int remaining =
            CurrentCost - paidAmount;

        priceText.text =
            $"Carry Lv.{playerCarry.CarryLevel}\n" +
            $"{remaining:N0}";
    }
}