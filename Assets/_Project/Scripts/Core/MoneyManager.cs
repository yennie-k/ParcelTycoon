using TMPro;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    [Header("Money Settings")]
    [SerializeField] private int startingMoney = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text moneyText;

    private int currentMoney;

    public int CurrentMoney => currentMoney;

    private void Awake()
    {
        currentMoney = startingMoney;

        UpdateMoneyUI();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentMoney += amount;

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

        UpdateMoneyUI();

        return spentAmount;
    }

    private void UpdateMoneyUI()
    {
        if (moneyText == null)
        {
            return;
        }

        moneyText.text =
            $"Money: {currentMoney:N0}";
    }
}