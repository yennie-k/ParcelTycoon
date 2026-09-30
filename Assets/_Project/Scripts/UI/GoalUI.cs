using TMPro;
using UnityEngine;

public class GoalUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text goalText;

    [SerializeField]
    private GameObject secondInbound;

    [SerializeField]
    private GameObject deliveryWorker;

    [SerializeField]
    private GameObject inputWorker;

    private void Update()
    {
        UpdateGoal();
    }

    private void UpdateGoal()
    {
        if (goalText == null)
        {
            return;
        }

        // 1단계: 두 번째 입고 시설 구매
        if (secondInbound != null &&
            !secondInbound.activeSelf)
        {
            goalText.text =
                "Goal: Unlock 2nd Inbound (300)";

            return;
        }

        // 2단계: 출고 직원 고용
        if (deliveryWorker != null &&
            !deliveryWorker.activeSelf)
        {
            goalText.text =
                "Goal: Hire Delivery Worker (500)";

            return;
        }

        // 3단계: 입고 직원 고용
        if (inputWorker != null &&
            !inputWorker.activeSelf)
        {
            goalText.text =
                "Goal: Hire Input Worker (1,000)";

            return;
        }

        // 자동화 완료
        goalText.text =
            "Goal: Upgrade your warehouse!";
    }
}