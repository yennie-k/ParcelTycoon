
using System.Collections;
using UnityEngine;

public class DeliveryZone : MonoBehaviour
{
    [Header("Delivery Settings")]
    [SerializeField] private int rewardPerBox = 100;

    [SerializeField] private float unloadInterval = 0.4f;

    [Header("References")]
    [SerializeField] private MoneyManager moneyManager;

    private Coroutine unloadCoroutine;


    // 플레이어가 출고 구역에 들어왔을 때
    private void OnTriggerEnter(Collider other)
    {
        PlayerCarry playerCarry =
            other.GetComponent<PlayerCarry>();

        if (playerCarry == null)
        {
            return;
        }

        if (unloadCoroutine != null)
        {
            return;
        }

        unloadCoroutine =
            StartCoroutine(UnloadBoxes(playerCarry));
    }


    // 플레이어가 출고 구역을 벗어났을 때
    private void OnTriggerExit(Collider other)
    {
        PlayerCarry playerCarry =
            other.GetComponent<PlayerCarry>();

        if (playerCarry == null)
        {
            return;
        }

        if (unloadCoroutine != null)
        {
            StopCoroutine(unloadCoroutine);

            unloadCoroutine = null;
        }
    }


    // 분류 완료된 상자를 하나씩 출고
    private IEnumerator UnloadBoxes(PlayerCarry playerCarry)
    {
        while (playerCarry != null && playerCarry.HasBox)
        {
            // 분류 완료된 상자만 가져오기
            ParcelBox box =
                playerCarry.TakeTopSortedBox();

            // 분류된 상자가 없다면 출고 중단
            if (box == null)
            {
                break;
            }

            // 상자 배송 완료
            box.Deliver();

            // 배송 수익 지급
            if (moneyManager != null)
            {
                moneyManager.AddMoney(rewardPerBox);
            }

            Debug.Log(
                $"배송 완료! +{rewardPerBox}원"
            );

            // 다음 상자 배송까지 잠시 기다림
            yield return new WaitForSeconds(
                unloadInterval
            );
        }

        unloadCoroutine = null;
    }
}