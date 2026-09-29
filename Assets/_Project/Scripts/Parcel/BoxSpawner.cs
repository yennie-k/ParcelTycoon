using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    [Header("Box Settings")]
    [SerializeField] private GameObject boxPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private int maxBoxCount = 5;
    [SerializeField] private float spawnDelay = 2f;
    [SerializeField] private float stackSpacing = 1.05f;

    private readonly List<ParcelBox> spawnedBoxes =
        new List<ParcelBox>();

    private Coroutine spawnCoroutine;

    private void Start()
    {
        StartFilling();
    }

    private void StartFilling()
    {
        if (spawnCoroutine != null)
        {
            return;
        }

        spawnCoroutine =
            StartCoroutine(FillSpawnStack());
    }

    private IEnumerator FillSpawnStack()
    {
        while (true)
        {
            RefreshSpawnStack();

            if (spawnedBoxes.Count >= maxBoxCount)
            {
                break;
            }

            SpawnBox();

            yield return new WaitForSeconds(
                spawnDelay
            );
        }

        spawnCoroutine = null;
    }

    private void SpawnBox()
    {
        if (boxPrefab == null)
        {
            return;
        }

        int stackIndex = spawnedBoxes.Count;

        Vector3 spawnPosition =
            transform.position +
            Vector3.up *
            (stackIndex * stackSpacing);

        GameObject boxObject =
            Instantiate(
                boxPrefab,
                spawnPosition,
                Quaternion.identity,
                transform
            );

        ParcelBox box =
            boxObject.GetComponent<ParcelBox>();

        if (box == null)
        {
            Destroy(boxObject);
            return;
        }

        box.Initialize(this);

        spawnedBoxes.Add(box);
    }

    public void RequestNewBox()
    {
        RefreshSpawnStack();
        StartFilling();
    }

    private void RefreshSpawnStack()
    {
        // 플레이어나 다른 시스템이 가져간 상자는
        // 더 이상 이 SpawnPoint의 자식이 아니므로 목록에서 제거
        spawnedBoxes.RemoveAll(
            box =>
                box == null ||
                box.transform.parent != transform
        );

        // 남아 있는 상자를 아래부터 다시 정렬
        for (int i = 0;
             i < spawnedBoxes.Count;
             i++)
        {
            Vector3 position =
                transform.position +
                Vector3.up *
                (i * stackSpacing);

            spawnedBoxes[i].transform.position =
                position;
        }
    }
}