using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortingStation : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField] private float sortingTime = 2f;
    [SerializeField] private int sortingLevel = 1;
    [SerializeField] private int maxSortingLevel = 4;
    [SerializeField] private float sortingTimeDecrease = 0.4f;
    [SerializeField] private float minimumSortingTime = 0.8f;

    [Header("Input Settings")]
    [SerializeField] private int maxInputCount = 5;
    [SerializeField] private float inputStackSpacing = 1.05f;

    [Header("Output Settings")]
    [SerializeField] private int maxOutputCount = 5;
    [SerializeField] private float outputStackSpacing = 1.05f;

    [Header("References")]
    [SerializeField] private Transform inputPoint;
    [SerializeField] private Transform sortingPoint;
    [SerializeField] private Transform outputPoint;

    // 분류를 기다리고 있는 미분류 상자
    private readonly List<ParcelBox> inputBoxes =
        new List<ParcelBox>();

    // 분류가 끝난 상자
    private readonly List<ParcelBox> outputBoxes =
        new List<ParcelBox>();

    // 현재 실제로 분류 중인 상자
    private ParcelBox currentBox;

    private Coroutine sortingCoroutine;

    public bool IsBusy => currentBox != null;

    public bool CanAcceptInput =>
        inputBoxes.Count < maxInputCount;

    public int SortingLevel => sortingLevel;

    public int MaxSortingLevel => maxSortingLevel;

    public float SortingTime => sortingTime;

    public bool CanUpgradeSorting =>
        sortingLevel < maxSortingLevel;

    private bool sortingBaselineCaptured;
    private float baseSortingTime;

    public void RestoreSortingLevel(int level)
    {
        if (!sortingBaselineCaptured)
        {
            baseSortingTime = sortingTime + (sortingLevel - 1) * sortingTimeDecrease;
            sortingBaselineCaptured = true;
        }
        sortingLevel = Mathf.Clamp(level, 1, maxSortingLevel);
        sortingTime = Mathf.Max(minimumSortingTime,
            baseSortingTime - (sortingLevel - 1) * sortingTimeDecrease);
    }

    public bool UpgradeSortingSpeed()
    {
        if (!CanUpgradeSorting)
        {
            return false;
        }

        sortingLevel++;

        sortingTime -= sortingTimeDecrease;

        sortingTime =
            Mathf.Max(
                sortingTime,
                minimumSortingTime
            );

        Debug.Log(
            $"분류 속도 업그레이드! " +
            $"Lv.{sortingLevel} / {sortingTime:F1}초"
        );

        return true;
    }

    private void Update()
    {
        RefreshInputStack();
        RefreshOutputStack();

        // 현재 분류 중인 상자가 있다면 기다린다.
        if (IsBusy)
        {
            return;
        }

        // 입력 상자가 없으면 할 일이 없다.
        if (inputBoxes.Count == 0)
        {
            return;
        }

        // 출력 적재대가 가득 찼다면
        // 공간이 생길 때까지 분류를 멈춘다.
        if (outputBoxes.Count >= maxOutputCount)
        {
            return;
        }

        StartNextSorting();
    }

    // 플레이어나 직원이 미분류 상자를 맡길 때 호출
    public bool TryAddInputBox(ParcelBox box)
    {
        if (box == null)
        {
            return false;
        }

        if (box.CurrentState !=
            ParcelBox.BoxState.Unsorted)
        {
            return false;
        }

        RefreshInputStack();

        if (inputBoxes.Count >= maxInputCount)
        {
            return false;
        }

        int stackIndex = inputBoxes.Count;

        Vector3 position =
            inputPoint.position +
            Vector3.up *
            (stackIndex * inputStackSpacing);

        box.PlaceInInput(
            inputPoint,
            position
        );

        inputBoxes.Add(box);

        return true;
    }

    // InputPoint의 맨 아래 상자를 꺼내 분류 시작
    private void StartNextSorting()
    {
        if (inputBoxes.Count == 0)
        {
            return;
        }

        currentBox = inputBoxes[0];

        inputBoxes.RemoveAt(0);

        RefreshInputStack();

        currentBox.PlaceForSorting(
            sortingPoint
        );

        sortingCoroutine =
            StartCoroutine(SortCurrentBox());
    }

    private IEnumerator SortCurrentBox()
    {
        yield return new WaitForSeconds(
            sortingTime
        );

        if (currentBox == null)
        {
            sortingCoroutine = null;
            yield break;
        }

        RefreshOutputStack();

        int stackIndex =
            outputBoxes.Count;

        Vector3 position =
            outputPoint.position +
            Vector3.up *
            (stackIndex * outputStackSpacing);

        currentBox.CompleteSorting(
            outputPoint,
            position
        );

        outputBoxes.Add(currentBox);

        Debug.Log(
            $"분류 완료! 입력 {inputBoxes.Count}개 / 출력 {outputBoxes.Count}개"
        );

        currentBox = null;
        sortingCoroutine = null;
    }

    private void RefreshInputStack()
    {
        inputBoxes.RemoveAll(
            box =>
                box == null ||
                box.transform.parent != inputPoint
        );

        for (int i = 0;
             i < inputBoxes.Count;
             i++)
        {
            Vector3 position =
                inputPoint.position +
                Vector3.up *
                (i * inputStackSpacing);

            inputBoxes[i].transform.position =
                position;
        }
    }

    private void RefreshOutputStack()
    {
        outputBoxes.RemoveAll(
            box =>
                box == null ||
                !box.IsAvailable ||
                box.transform.parent != outputPoint
        );

        for (int i = 0;
             i < outputBoxes.Count;
             i++)
        {
            Vector3 position =
                outputPoint.position +
                Vector3.up *
                (i * outputStackSpacing);

            outputBoxes[i].transform.position =
                position;
        }
    }
}