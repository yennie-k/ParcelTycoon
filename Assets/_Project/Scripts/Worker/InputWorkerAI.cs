using System.Collections.Generic;
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

    [Header("Carry Settings")]
    [SerializeField, Min(0.01f)] private float pickupInterval = 0.3f;
    [SerializeField, Range(1, 3)] private int maxCarryCount = 1;
    private const float CollectionDuration = 1.5f;
    private bool collecting;
    private float collectionDeadline;
    private float nextPickupAllowedTime;

    [Header("References")]
    [SerializeField] private Transform carryPoint;

    [SerializeField] private Transform sortingZoneTarget;

    [SerializeField] private Transform waitingPoint;

    [SerializeField] private SortingStation sortingStation;


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
            $"입고 직원 이동속도: {moveSpeed:F2}"
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
        currentState = HasBox ? WorkerState.MovingToSorting : WorkerState.SearchingBox;
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

            case WorkerState.MovingToSorting:
                MoveToSortingZone();
                break;
        }
    }

    private int CollectionLimit => Mathf.Min(MaxCarryCount, Mathf.Max(0, sortingStation == null ? 0 : sortingStation.MaxInputCount - sortingStation.InputCount));

    private bool FinishCollectionIfNeeded()
    {
        if (HasBox && (CarryCount >= CollectionLimit ||
            (collecting && Time.time >= collectionDeadline)))
        {
            collecting = false;
            targetBox = null;
            currentState = WorkerState.MovingToSorting;
            return true;
        }
        return false;
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
        if (FinishCollectionIfNeeded()) return;
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
        if (FinishCollectionIfNeeded()) return;
        if (targetBox == null || !targetBox.IsAvailable ||
            targetBox.CurrentState != ParcelBox.BoxState.Unsorted)
        {
            targetBox = null;
            currentState = WorkerState.SearchingBox;
            return;
        }
        // Capacity may have changed while walking to this box.
        if (CarryCount >= CollectionLimit)
        {
            targetBox = null;
            currentState = HasBox ? WorkerState.MovingToSorting : WorkerState.SearchingBox;
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

    private void MoveToSortingZone()
    {
        if (!HasBox)
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

    // Commit removal only after the station accepted and reparented the box.
    public bool TryTransferCarriedBox(SortingStation station)
    {
        RefreshCarriedBoxes();
        if (!isActiveAndEnabled || currentState != WorkerState.MovingToSorting ||
            station == null || station != sortingStation || !HasBox) return false;
        ParcelBox box = carriedBoxes[carriedBoxes.Count - 1];
        if (!station.TryAddInputBox(box)) return false;
        // Guard the station's early-return placement path if its input reference is missing.
        if (box.IsCarriedBy(carryPoint)) return false;
        carriedBoxes.Remove(box);
        UpdateCarryPositions();
        currentState = HasBox ? WorkerState.MovingToSorting : WorkerState.SearchingBox;
        return true;
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