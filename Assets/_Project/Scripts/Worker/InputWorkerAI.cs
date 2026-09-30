using UnityEngine;

public class InputWorkerAI : MonoBehaviour
{
    private enum WorkerState
    {
        SearchingBox,
        MovingToBox,
        MovingToSorting
    }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float pickupDistance = 1.2f;

    [Header("References")]
    [SerializeField] private Transform carryPoint;

    [SerializeField] private Transform sortingZoneTarget;

    [SerializeField] private Transform waitingPoint;

    [SerializeField] private SortingStation sortingStation;


    private WorkerState currentState;

    private ParcelBox targetBox;
    private ParcelBox carriedBox;

    public bool HasBox => carriedBox != null;

    public float MoveSpeed => moveSpeed;

    public void IncreaseMoveSpeed(float amount)
    {
        moveSpeed += amount;

        Debug.Log(
            $"입고 직원 이동속도: {moveSpeed:F2}"
        );
    }

    private void OnEnable()
    {
        currentState = WorkerState.SearchingBox;

        targetBox = null;
        carriedBox = null;
    }

    private void Update()
    {
        switch (currentState)
        {
            case WorkerState.SearchingBox:
                SearchForBox();
                break;

            case WorkerState.MovingToBox:
                MoveToBox();
                break;

            case WorkerState.MovingToSorting:
                MoveToSortingZone();
                break;
        }
    }

    private void SearchForBox()
    {
        if (sortingStation == null)
        {
            return;
        }

        // 분류대 입력 공간이 꽉 찼으면
        // 새로운 상자를 가지러 가지 않는다.
        if (!sortingStation.CanAcceptInput)
        {
            return;
        }
        
        ParcelBox[] boxes =
            FindObjectsByType<ParcelBox>(
                FindObjectsSortMode.None
            );

        ParcelBox nearestBox = null;
        float nearestDistance = Mathf.Infinity;

        foreach (ParcelBox box in boxes)
        {
            if (!box.IsAvailable)
            {
                continue;
            }

            // 미분류 상자만 찾는다.
            if (box.CurrentState !=
                ParcelBox.BoxState.Unsorted)
            {
                continue;
            }

            float distance =
                HorizontalDistance(
                    transform.position,
                    box.transform.position
                );

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestBox = box;
            }
        }

        if (nearestBox == null)
        {
            return;
        }

        targetBox = nearestBox;

        currentState =
            WorkerState.MovingToBox;
    }

    private void MoveToBox()
    {
        if (targetBox == null ||
            !targetBox.IsAvailable ||
            targetBox.CurrentState !=
            ParcelBox.BoxState.Unsorted)
        {
            targetBox = null;

            currentState =
                WorkerState.SearchingBox;

            return;
        }

        MoveTowards(
            targetBox.transform.position
        );

        float distance =
            HorizontalDistance(
                transform.position,
                targetBox.transform.position
            );

        if (distance <= pickupDistance)
        {
            PickUpBox();
        }
    }

    private void PickUpBox()
    {
        if (targetBox == null ||
            !targetBox.IsAvailable)
        {
            targetBox = null;

            currentState =
                WorkerState.SearchingBox;

            return;
        }

        carriedBox = targetBox;

        carriedBox.PickUp(
            carryPoint,
            Vector3.zero
        );

        targetBox = null;

        currentState =
            WorkerState.MovingToSorting;
    }

    private void MoveToSortingZone()
    {
        if (carriedBox == null)
        {
            currentState =
                WorkerState.SearchingBox;

            return;
        }

        if (sortingZoneTarget == null ||
            sortingStation == null)
        {
            return;
        }

        // 분류대에 공간이 없다면
        // 중앙까지 가지 않고 대기 위치로 이동
        if (!sortingStation.CanAcceptInput)
        {
            if (waitingPoint != null)
            {
                MoveTowards(
                    waitingPoint.position
                );
            }

            return;
        }

        // 공간이 생기면 다시 SortingZone으로 이동
        MoveTowards(
            sortingZoneTarget.position
        );

        // 실제 상자 전달은
        // SortingZone Trigger가 담당한다.
    }

    // SortingZone이 직원에게 상자를 요청할 때 사용
    public ParcelBox TakeCarriedBox()
    {
        if (carriedBox == null)
        {
            return null;
        }

        ParcelBox box = carriedBox;

        carriedBox = null;

        currentState =
            WorkerState.SearchingBox;

        return box;
    }

    private void MoveTowards(
        Vector3 targetPosition)
    {
        Vector3 flatTarget =
            new Vector3(
                targetPosition.x,
                transform.position.y,
                targetPosition.z
            );

        Vector3 direction =
            flatTarget - transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up
                );

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed *
                    Time.deltaTime
                );
        }

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                flatTarget,
                moveSpeed *
                Time.deltaTime
            );
    }

    private float HorizontalDistance(
        Vector3 a,
        Vector3 b)
    {
        Vector2 pointA =
            new Vector2(a.x, a.z);

        Vector2 pointB =
            new Vector2(b.x, b.z);

        return Vector2.Distance(
            pointA,
            pointB
        );
    }
}