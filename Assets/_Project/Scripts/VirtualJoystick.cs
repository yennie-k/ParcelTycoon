using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class VirtualJoystick : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [Header("Joystick UI")]
    [SerializeField] private RectTransform handle;

    [Header("Movement Range")]
    [Range(0.1f, 1f)]
    [SerializeField] private float handleRange = 1f;

    private RectTransform background;
    private Vector2 direction;

    // 다른 스크립트가 현재 조이스틱 방향을 읽을 수 있게 해준다.
    public Vector2 Direction => direction;

    private void Awake()
    {
        background = GetComponent<RectTransform>();
        ResetJoystick();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // 누른 순간부터 손가락 위치를 반영한다.
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (handle == null)
        {
            return;
        }

        bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        if (!converted)
        {
            return;
        }

        Vector2 halfSize = background.rect.size * 0.5f;

        if (halfSize.x <= 0f || halfSize.y <= 0f)
        {
            return;
        }

        // 배경의 중심을 기준으로 입력 방향을 -1~1 범위로 변환한다.
        Vector2 normalizedInput = new Vector2(
            localPoint.x / halfSize.x,
            localPoint.y / halfSize.y
        );

        direction = Vector2.ClampMagnitude(normalizedInput, 1f);

        // 손잡이가 배경 이미지 밖으로 완전히 빠져나가지 않도록 한다.
        float backgroundRadius = Mathf.Min(halfSize.x, halfSize.y);
        float handleRadius =
            Mathf.Min(handle.rect.width, handle.rect.height) * 0.5f;

        float maxDistance =
            Mathf.Max(0f, backgroundRadius - handleRadius) * handleRange;

        handle.anchoredPosition = direction * maxDistance;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    private void OnDisable()
    {
        ResetJoystick();
    }

    private void ResetJoystick()
    {
        direction = Vector2.zero;

        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }
}