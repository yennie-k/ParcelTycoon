using System.Collections;
using TMPro;
using UnityEngine;

public class PurchaseZone : MonoBehaviour
{
    [Header("Purchase Settings")]
    [SerializeField] private int purchaseCost = 300;
    [SerializeField] private int paymentPerTick = 50;
    [SerializeField] private float paymentInterval = 0.1f;

    [Header("References")]
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private GameObject targetObject;
    [SerializeField] private TMP_Text priceText;

    private int paidAmount;
    private Coroutine paymentCoroutine;

    private void Start()
    {
        UpdatePriceText();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player =
            other.GetComponent<PlayerMovement>();

        if (player == null)
        {
            return;
        }

        if (paymentCoroutine != null)
        {
            return;
        }

        paymentCoroutine =
            StartCoroutine(PayForPurchase());
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement player =
            other.GetComponent<PlayerMovement>();

        if (player == null)
        {
            return;
        }

        if (paymentCoroutine == null)
        {
            return;
        }

        StopCoroutine(paymentCoroutine);
        paymentCoroutine = null;
    }

    private IEnumerator PayForPurchase()
    {
        while (paidAmount < purchaseCost)
        {
            int remainingCost =
                purchaseCost - paidAmount;

            int requestedPayment =
                Mathf.Min(
                    paymentPerTick,
                    remainingCost
                );

            int spentAmount =
                moneyManager.SpendMoney(
                    requestedPayment
                );

            paidAmount += spentAmount;

            UpdatePriceText();

            if (paidAmount >= purchaseCost)
            {
                CompletePurchase();
                yield break;
            }

            yield return new WaitForSeconds(
                paymentInterval
            );
        }
    }

    private void UpdatePriceText()
    {
        if (priceText == null)
        {
            return;
        }

        int remainingCost =
            purchaseCost - paidAmount;

        priceText.text =
            $"{remainingCost:N0}";
    }

    private void CompletePurchase()
    {
        if (targetObject != null)
        {
            targetObject.SetActive(true);
        }

        paymentCoroutine = null;

        gameObject.SetActive(false);
    }
}