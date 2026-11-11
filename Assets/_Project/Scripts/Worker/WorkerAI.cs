using System.Collections.Generic;
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

    [Header("Carry Settings")]
    [SerializeField, Min(0.01f)] private float pickupInterval = 0.3f;
    [SerializeField, Range(1, 3)] private int maxCarryCount = 1;
    private const float CollectionDuration = 1.5f;
    private bool collecting;
    private float collectionDeadline;
    private float nextPickupAllowedTime;

    [Header("References")]
    [SerializeField] private Transform carryPoint;
    [SerializeField] private Transform deliveryTarget;
    [SerializeField] private MoneyManager moneyManager;

    [Header("Reward")]
    [SerializeField] private int rewardPerBox = 100;

    private WorkerState currentState;

    private ParcelBox targetBox;
    [SerializeField] private List<ParcelBox> carriedBoxes = new List<ParcelBox>();

    public int CarryCount => carriedBoxes.Count;
    public int MaxCarryCount => Mathf.Clamp(maxCarryCount, 1, 3);
    public bool HasBox => CarryCount > 0;
    public bool IsFull => CarryCount >= MaxCarryCount;

    // Absolute capacity for a future shared upgrade; existing cargo is never discarded.
    public void SetMaxCarryCount(int capacity)
    {
        maxCarryCount = Mathf.Clamp(capacity, 1, 3);
    }

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
        RefreshCarriedBoxes();
        // Recover owned children if a runtime list was lost during a script reload.
        if (carryPoint != null)
            foreach (ParcelBox box in carryPoint.GetComponentsInChildren<ParcelBox>(true))
                if (box.IsCarriedBy(carryPoint) && !carriedBoxes.Contains(box)) carriedBoxes.Add(box);
        collecting = false;
        targetBox = null;
        currentState = HasBox ? WorkerState.MovingToDelivery : WorkerState.SearchingBox;
        UpdateCarryPositions();
    }

    private void OnDisable()
    {
        // Resume delivery on enable instead of starting another collection window.
        collecting = false;
        targetBox = null;
    }

    private void RefreshCarriedBoxes()
    {
        if (carriedBoxes == null) carriedBoxes = new List<ParcelBox>();
        if (carriedBoxes.RemoveAll(box => box == null || !box.IsCarriedBy(carryPoint)) > 0)
            UpdateCarryPositions();
        if (!HasBox) collecting = false;
    }

    private void UpdateCarryPositions()
    {
        for (int i = 0; i < carriedBoxes.Count; i++)
            carriedBoxes[i].transform.localPosition = Vector3.up * (i * 1.05f);
    }

    private void Update()
    {
        RefreshCarriedBoxes();
        FinishCollectionIfNeeded();
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

    private int CollectionLimit => MaxCarryCount;

    private bool FinishCollectionIfNeeded()
    {
        if (HasBox && (CarryCount >= CollectionLimit ||
            (collecting && Time.time >= collectionDeadline)))
        {
            collecting = false;
            targetBox = null;
            currentState = WorkerState.MovingToDelivery;
            return true;
        }
        return false;
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
        if (FinishCollectionIfNeeded()) return;
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
        if (FinishCollectionIfNeeded()) return;
        if (targetBox == null || !targetBox.IsAvailable ||
            targetBox.CurrentState != ParcelBox.BoxState.Sorted)
        {
            targetBox = null;
            currentState = WorkerState.SearchingBox;
            return;
        }
        // Capacity may have changed while walking to this box.
        if (CarryCount >= CollectionLimit)
        {
            targetBox = null;
            currentState = HasBox ? WorkerState.MovingToDelivery : WorkerState.SearchingBox;
            return;
        }
        // Gate only acquisition; movement and collection exit checks still run during cooldown.
        float now = Time.time;
        if (HasBox && now < nextPickupAllowedTime) return;
        if (!targetBox.TryPickUp(carryPoint, Vector3.up * (CarryCount * 1.05f)))
        {
            targetBox = null;
            currentState = WorkerState.SearchingBox;
            return;
        }
        nextPickupAllowedTime = now + Mathf.Max(0.01f, pickupInterval);
        bool firstBox = !HasBox;
        carriedBoxes.Add(targetBox);
        UpdateCarryPositions();
        targetBox = null;
        if (firstBox)
        {
            collecting = true;
            collectionDeadline = now + CollectionDuration;
        }
        currentState = WorkerState.SearchingBox;
        FinishCollectionIfNeeded();
    }

    private void MoveToDelivery()
    {
        if (!HasBox)
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
        if (!HasBox)
        {
            return;
        }

        RefreshCarriedBoxes();
        for (int i = carriedBoxes.Count - 1; i >= 0; i--)
        {
            ParcelBox box = carriedBoxes[i];
            if (box.CurrentState != ParcelBox.BoxState.Sorted) continue;
            // Remove before deferred destruction and payment: no repeated reward.
            carriedBoxes.RemoveAt(i);
            box.Deliver();
            if (moneyManager != null) moneyManager.AddMoney(rewardPerBox);
        }
        UpdateCarryPositions();
        collecting = false;
        currentState = HasBox ? WorkerState.MovingToDelivery : WorkerState.SearchingBox;
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