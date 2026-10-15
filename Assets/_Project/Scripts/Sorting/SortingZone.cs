using System.Collections;
using UnityEngine;

public class SortingZone : MonoBehaviour
{
    [Header("Transfer Settings")]
    [SerializeField] private float transferInterval = 0.3f;

    [Header("References")]
    [SerializeField] private SortingStation sortingStation;

    private Coroutine playerTransferCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        // -------------------------
        // 플레이어 처리
        // -------------------------

        PlayerCarry playerCarry =
            other.GetComponent<PlayerCarry>();

        if (playerCarry != null)
        {
            if (playerTransferCoroutine == null)
            {
                playerTransferCoroutine =
                    StartCoroutine(
                        TransferPlayerBoxes(
                            playerCarry
                        )
                    );
            }

            return;
        }

        // -------------------------
        // 입고 직원 처리
        // -------------------------

        InputWorkerAI inputWorker =
            other.GetComponent<InputWorkerAI>();

        if (inputWorker != null)
        {
            TryReceiveWorkerBox(
                inputWorker
            );
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // 직원이 상자를 들고 있는데
        // InputPoint가 꽉 차 있으면 여기에서 기다리게 된다.
        InputWorkerAI inputWorker =
            other.GetComponent<InputWorkerAI>();

        if (inputWorker != null)
        {
            TryReceiveWorkerBox(
                inputWorker
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerCarry playerCarry =
            other.GetComponent<PlayerCarry>();

        if (playerCarry == null)
        {
            return;
        }

        if (playerTransferCoroutine != null)
        {
            StopCoroutine(
                playerTransferCoroutine
            );

            playerTransferCoroutine = null;
        }
    }

    // 플레이어는 여러 개를 가지고 있으므로
    // 일정 간격으로 하나씩 전달
    private IEnumerator TransferPlayerBoxes(
        PlayerCarry playerCarry)
    {
        while (playerCarry != null)
        {
            if (sortingStation == null)
            {
                break;
            }

            if (!sortingStation.CanAcceptInput)
            {
                yield return null;
                continue;
            }

            ParcelBox box =
                playerCarry.TakeUnsortedBox();

            if (box == null)
            {
                break;
            }

            bool added =
                sortingStation.TryAddInputBox(
                    box
                );

            if (!added)
            {
                break;
            }

            yield return new WaitForSeconds(
                transferInterval
            );
        }

        playerTransferCoroutine = null;
    }

    // 직원은 현재 상자 하나만 들고 있으므로
    // 판에 닿으면 바로 전달
    private void TryReceiveWorkerBox(
        InputWorkerAI worker)
    {
        if (sortingStation == null)
        {
            return;
        }

        if (!sortingStation.CanAcceptInput)
        {
            return;
        }

        if (!worker.HasBox)
        {
            return;
        }

        worker.TryTransferCarriedBox(sortingStation);
    }
}