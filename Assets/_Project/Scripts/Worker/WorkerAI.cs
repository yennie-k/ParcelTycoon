using UnityEngine;

public class WorkerAI : MonoBehaviour
{
    private enum WorkerState
    {
        SearchingBox,
        MovingToBox,
        MovingToDelivery
    }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float pickupDistance = 1.2f;
    [SerializeField] private float deliveryDistance = 1.5f;

    [Header("References")]
    [SerializeField] private Transform carryPoint;
    [SerializeField] private Transform deliveryTarget;
    [SerializeField] private MoneyManager moneyManager;

    [Header("Reward")]
    [SerializeField] private int rewardPerBox = 100;

    private WorkerState currentState;

    private ParcelBox targetBox;
    private ParcelBox carriedBox;

    public float MoveSpeed => moveSpeed;

    private bool speedBaselineCaptured;
    private float baseMoveSpeed;

    // Independent of Awake/OnEnable so inactive workers can be restored too.
    public void RestoreMoveSpeedBonus(float bonus)
    {
        if (!speedBaselineCaptured)
        {
            baseMoveSpeed = moveSpeed;
            speedBaselineCaptured = true;
        }
        moveSpeed = baseMoveSpeed + bonus;
    }

    public void IncreaseMoveSpeed(float amount)
    {
        moveSpeed += amount;

        Debug.Log(
            $"출고 직원 이동속도: {moveSpeed:F2}"
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

            case WorkerState.MovingToDelivery:
                MoveToDelivery();
                break;
        }
    }

    private void SearchForBox()
    {
        ParcelBox[] boxes =
            FindObjectsByType<ParcelBox>(
                FindObjectsSortMode.None
            );

        ParcelBox nearestBox = null;
        float nearestDistance = Mathf.Infinity;

        foreach (ParcelBox box in boxes)
        {
            // 이미 누군가 들고 있는 상자이거나
            // 아직 분류가 완료되지 않은 상자는 무시한다.
            if (!box.IsAvailable ||
                box.CurrentState != ParcelBox.BoxState.Sorted)
            {
                continue;
            }

            float distance =
                Vector3.Distance(
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
        currentState = WorkerState.MovingToBox;
    }

    
    private void MoveToBox()
    {
        // 목표 상자가 사라졌거나,
        // 다른 사람이 가져갔거나,
        // 분류 완료 상태가 아니면 다시 탐색한다.
        if (targetBox == null ||
            !targetBox.IsAvailable ||
            targetBox.CurrentState != ParcelBox.BoxState.Sorted)
        {
            targetBox = null;
            currentState = WorkerState.SearchingBox;
            return;
        }

        // 목표 상자까지 이동
        MoveTowards(targetBox.transform.position);

        float distance = HorizontalDistance(
            transform.position,
            targetBox.transform.position
        );

        // 상자에 충분히 가까워지면 획득
        if (distance <= pickupDistance)
        {
            PickUpBox();
        }
    }

    
    private void PickUpBox()
    {
        // 분류 완료된 상자만 획득할 수 있다.
        if (targetBox == null ||
            !targetBox.IsAvailable ||
            targetBox.CurrentState != ParcelBox.BoxState.Sorted)
        {
            targetBox = null;
            currentState = WorkerState.SearchingBox;
            return;
        }

        // 직원이 운반할 상자 지정
        carriedBox = targetBox;

        // 직원의 CarryPoint에 상자를 부착
        carriedBox.PickUp(
            carryPoint,
            Vector3.zero
        );

        // 목표 상자 초기화
        targetBox = null;

        // 출고 구역으로 이동하는 상태로 변경
        currentState = WorkerState.MovingToDelivery;
    }

    private void MoveToDelivery()
    {
        if (carriedBox == null)
        {
            currentState = WorkerState.SearchingBox;
            return;
        }

        if (deliveryTarget == null)
        {
            return;
        }

        MoveTowards(deliveryTarget.position);

        float distance =
            HorizontalDistance(
                transform.position,
                deliveryTarget.position
            );

        if (distance <= deliveryDistance)
        {
            DeliverBox();
        }
    }

    private void DeliverBox()
    {
        if (carriedBox == null)
        {
            return;
        }

        carriedBox.Deliver();
        carriedBox = null;

        if (moneyManager != null)
        {
            moneyManager.AddMoney(rewardPerBox);
        }

        currentState = WorkerState.SearchingBox;
    }

    private void MoveTowards(Vector3 targetPosition)
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
                    rotationSpeed * Time.deltaTime
                );
        }

        transform.position =
            Vector3.MoveTowards(
                transform.position,
                flatTarget,
                moveSpeed * Time.deltaTime
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