using UnityEngine;

public class ParcelBox : MonoBehaviour
{
    public enum BoxState
    {
        Unsorted,
        Sorting,
        Sorted
    }

    [Header("Box State")]
    [SerializeField]
    private BoxState currentState = BoxState.Unsorted;

    [Header("Box Materials")]
    [SerializeField] private Material unsortedMaterial;
    [SerializeField] private Material sortedMaterial;

    private BoxSpawner sourceSpawner;

    private Collider boxCollider;
    private Renderer boxRenderer;

    private bool isCarried;
    private bool isInStation;

    public BoxState CurrentState => currentState;

    public bool IsAvailable =>
        !isCarried &&
        !isInStation &&
        currentState != BoxState.Sorting;

    private void Awake()
    {
        boxCollider = GetComponent<Collider>();
        boxRenderer = GetComponent<Renderer>();

        currentState = BoxState.Unsorted;

        ChangeMaterial(unsortedMaterial);
    }

    public void Initialize(BoxSpawner spawner)
    {
        sourceSpawner = spawner;
    }

    public void PickUp(
        Transform carryPoint,
        Vector3 stackPosition)
    {
        if (!IsAvailable || carryPoint == null)
        {
            return;
        }

        isCarried = true;
        isInStation = false;

        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        transform.SetParent(carryPoint);

        transform.localPosition = stackPosition;
        transform.localRotation = Quaternion.identity;

        if (sourceSpawner != null)
        {
            sourceSpawner.RequestNewBox();
            sourceSpawner = null;
        }
    }

    // 미분류 상자를 입력 적재대에 놓는다.
    public void PlaceInInput(
        Transform inputPoint,
        Vector3 worldPosition)
    {
        if (inputPoint == null)
        {
            return;
        }

        isCarried = false;
        isInStation = true;

        currentState = BoxState.Unsorted;

        transform.SetParent(inputPoint);

        transform.position = worldPosition;
        transform.rotation = Quaternion.identity;

        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
    }

    // 입력 적재대에서 실제 분류 위치로 이동
    public void PlaceForSorting(
        Transform sortingPoint)
    {
        if (sortingPoint == null)
        {
            return;
        }

        isCarried = false;
        isInStation = true;

        currentState = BoxState.Sorting;

        transform.SetParent(sortingPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }
    }

    // 분류 완료 후 출력 적재대로 이동
    public void CompleteSorting(
        Transform outputPoint,
        Vector3 worldPosition)
    {
        if (outputPoint == null)
        {
            return;
        }

        currentState = BoxState.Sorted;

        isCarried = false;
        isInStation = false;

        transform.SetParent(outputPoint);

        transform.position = worldPosition;
        transform.rotation = Quaternion.identity;

        ChangeMaterial(sortedMaterial);

        if (boxCollider != null)
        {
            boxCollider.enabled = true;
        }
    }

    private void ChangeMaterial(Material newMaterial)
    {
        if (boxRenderer == null ||
            newMaterial == null)
        {
            return;
        }

        boxRenderer.sharedMaterial = newMaterial;
    }

    public void Deliver()
    {
        Destroy(gameObject);
    }
}