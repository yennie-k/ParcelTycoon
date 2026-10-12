using System;
using TMPro;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    [Header("Money Settings")]
    [SerializeField] private int startingMoney = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text moneyText;

    private int currentMoney;
    private bool initialized;
    public event Action MoneyChanged;

    public int CurrentMoney => currentMoney;

    private void Awake() { Initialize(); }

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;
        currentMoney = startingMoney;

        UpdateMoneyUI();
    }

    public void RestoreMoney(int amount)
    {
        Initialize();
        currentMoney = Mathf.Max(0, amount);
        UpdateMoneyUI();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentMoney += amount;
        MoneyChanged?.Invoke();

        UpdateMoneyUI();
    }

    public int SpendMoney(int requestedAmount)
    {
        if (requestedAmount <= 0)
        {
            return 0;
        }

        if (currentMoney <= 0)
        {
            return 0;
        }

        int spentAmount = Mathf.Min(
            requestedAmount,
            currentMoney
        );

        currentMoney -= spentAmount;
        MoneyChanged?.Invoke();

        UpdateMoneyUI();

        return spentAmount;
    }

    private void UpdateMoneyUI()
    {
        if (moneyText == null)
        {
            return;
        }

        moneyText.text = $"₩ {currentMoney:N0}";
    }
}