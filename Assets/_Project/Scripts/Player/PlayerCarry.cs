using System.Collections.Generic;
using UnityEngine;

public class PlayerCarry : MonoBehaviour
{
    [Header("Carry Settings")]
    [SerializeField] private Transform carryPoint;
    [SerializeField] private int maxCarryCount = 3;
    [SerializeField] private float stackSpacing = 1.05f;

    [Header("Carry Upgrade")]
    [SerializeField] private int carryLevel = 1;
    [SerializeField] private int maxCarryLevel = 3;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupInterval = 0.3f;

    private readonly List<ParcelBox> carriedBoxes =
        new List<ParcelBox>();

    private float nextPickupTime;

    public int CarryCount => carriedBoxes.Count;

    public bool HasBox =>
        carriedBoxes.Count > 0;

    public bool IsFull =>
        carriedBoxes.Count >= maxCarryCount;

    public int CarryLevel => carryLevel;

    public int MaxCarryLevel => maxCarryLevel;

    public int MaxCarryCount => maxCarryCount;
    public int NextCarryCount => CanUpgradeCarry ? maxCarryCount + 1 : maxCarryCount;

    public bool CanUpgradeCarry =>
        carryLevel < maxCarryLevel;

    private bool carryBaselineCaptured;
    private int baseCarryCount;

    public void RestoreCarryLevel(int level)
    {
        if (!carryBaselineCaptured)
        {
            baseCarryCount = maxCarryCount - (carryLevel - 1);
            carryBaselineCaptured = true;
        }
        carryLevel = Mathf.Clamp(level, 1, maxCarryLevel);
        maxCarryCount = baseCarryCount + (carryLevel - 1);
    }

    public bool UpgradeCarryCapacity()
    {
        if (!CanUpgradeCarry)
        {
            return false;
        }

        carryLevel++;
        maxCarryCount++;

        Debug.Log(
            $"운반량 업그레이드! " +
            $"Lv.{carryLevel} / 최대 {maxCarryCount}개"
        );

        return true;
    }

    // Trigger 안에 머무는 동안 계속 호출된다.
    private void OnTriggerStay(Collider other)
    {
        // 최대 운반량이면 더 이상 줍지 않는다.
        if (IsFull)
        {
            return;
        }

        if (carryPoint == null)
        {
            return;
        }

        // 이전 상자를 주운 뒤 일정 시간이 지나지 않았다면 기다린다.
        if (Time.time < nextPickupTime)
        {
            return;
        }

        ParcelBox box =
            other.GetComponent<ParcelBox>();

        if (box == null)
        {
            return;
        }

        // 이미 누가 들고 있거나,
        // 분류 중인 상자는 주울 수 없다.
        if (!box.IsAvailable)
        {
            return;
        }

        PickUpBox(box);

        // 다음 상자를 주울 수 있는 시간을 설정한다.
        nextPickupTime =
            Time.time + pickupInterval;
    }

    private void PickUpBox(ParcelBox box)
    {
        int stackIndex =
            carriedBoxes.Count;

        Vector3 stackPosition =
            new Vector3(
                0f,
                stackIndex * stackSpacing,
                0f
            );

        box.PickUp(
            carryPoint,
            stackPosition
        );

        carriedBoxes.Add(box);
    }

    // 맨 위 상자 하나 꺼내기
    public ParcelBox TakeTopBox()
    {
        if (!HasBox)
        {
            return null;
        }

        int lastIndex =
            carriedBoxes.Count - 1;

        ParcelBox box =
            carriedBoxes[lastIndex];

        carriedBoxes.RemoveAt(lastIndex);

        UpdateStackPositions();

        return box;
    }

    // 미분류 상자 하나 꺼내기
    public ParcelBox TakeUnsortedBox()
    {
        return TakeBoxWithState(
            ParcelBox.BoxState.Unsorted
        );
    }

    // 분류 완료 상자 하나 꺼내기
    public ParcelBox TakeTopSortedBox()
    {
        return TakeBoxWithState(
            ParcelBox.BoxState.Sorted
        );
    }

    private ParcelBox TakeBoxWithState(
        ParcelBox.BoxState requiredState)
    {
        // 위쪽 상자부터 확인한다.
        for (int i = carriedBoxes.Count - 1;
             i >= 0;
             i--)
        {
            ParcelBox box =
                carriedBoxes[i];

            if (box == null)
            {
                continue;
            }

            if (box.CurrentState != requiredState)
            {
                continue;
            }

            carriedBoxes.RemoveAt(i);

            UpdateStackPositions();

            return box;
        }

        return null;
    }

    // 중간 상자가 빠졌을 때
    // 남은 상자들을 빈 공간 없이 다시 쌓는다.
    private void UpdateStackPositions()
    {
        for (int i = 0;
             i < carriedBoxes.Count;
             i++)
        {
            if (carriedBoxes[i] == null)
            {
                continue;
            }

            carriedBoxes[i].transform.localPosition =
                new Vector3(
                    0f,
                    i * stackSpacing,
                    0f
                );
        }
    }
}