using System.Collections;
using TMPro;
using UnityEngine;

public class PurchaseZone : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private string displayName = "시설 구매";

    [Header("Purchase Settings")]
    [SerializeField] private int purchaseCost = 300;
    [SerializeField] private int paymentPerTick = 100;
    [SerializeField] private float paymentInterval = 0.2f;

    [Header("References")]
    [SerializeField] private MoneyManager moneyManager;
    [SerializeField] private GameObject targetObject;
    [SerializeField] private TMP_Text priceText;

    private bool purchased;
    private bool paymentRunning;
    private int paidAmount;
    private Coroutine paymentCoroutine;
    public int PurchaseCost => purchaseCost;
    public int PaidAmount => paidAmount;
    public bool HasSaveReferences => moneyManager != null && targetObject != null;

    public PurchaseSaveData CaptureState()
    {
        bool complete = purchased || (targetObject != null && targetObject.activeSelf);
        return new PurchaseSaveData { purchased = complete, paidAmount = complete ? 0 : paidAmount };
    }

    public bool IsValidSave(PurchaseSaveData data)
    {
        return data != null && data.paidAmount >= 0 && data.paidAmount <= purchaseCost &&
            (!data.purchased || data.paidAmount == 0);
    }

    public void RestoreState(PurchaseSaveData data)
    {
        OnDisable();
        purchased = data.purchased || data.paidAmount == purchaseCost;
        paidAmount = purchased ? purchaseCost : data.paidAmount;
        targetObject.SetActive(purchased);
        gameObject.SetActive(!purchased);
        UpdatePriceText();
    }

    private void OnDisable()
    {
        if (paymentCoroutine != null) StopCoroutine(paymentCoroutine);
        paymentCoroutine = null;
        paymentRunning = false;
    }

    private void Start()
    {
        UpdatePriceText();
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement player =
            other.GetComponent<PlayerMovement>();

        if (player == null || !SaveManager.GameplayReady)
        {
            return;
        }

        if (purchased || targetObject == null || targetObject.activeSelf || moneyManager == null || paymentRunning)
        {
            return;
        }

        paymentRunning = true;
        Coroutine started = StartCoroutine(PayForPurchase());
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

        if (paymentCoroutine == null)
        {
            return;
        }

        StopCoroutine(paymentCoroutine);
        paymentCoroutine = null;
        paymentRunning = false;
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
            if (spentAmount > 0) SaveManager.Instance?.RequestSave();

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
        if (priceText == null) return;
        priceText.text = PurchasePadText.Purchase(displayName, purchaseCost, paidAmount);
    }

    private void CompletePurchase()
    {
        purchased = true;
        if (targetObject != null)
        {
            targetObject.SetActive(true);
        }

        paymentCoroutine = null;

        gameObject.SetActive(false);
        SaveManager.Instance?.SaveNow();
    }
}
