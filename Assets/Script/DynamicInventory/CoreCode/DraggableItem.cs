using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler, IBeginDragHandler
{
    [Header("Min Distance Player can drag")]
    [SerializeField] float DragThreshold = 10f;

    [Header("References")]
    [SerializeField] RectTransform rectTransform;
    [SerializeField] RectTransform content;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] LayoutElement placeHolderLayout;

    public RectTransform RectTransform => rectTransform;

    AnimationCurve swapEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    float swapDuration = 0.25f;
    RecordableList recordableList;
    bool canBeDraggableToTheContainer;

    Canvas RootCanvas => recordableList.RootCanvas;

    Coroutine swapCoroutine;
    Vector2 _pointDownPosition;
    Vector2 dragOffset;
    bool isDragging;
    Canvas dragSortingCanvas;

    //PlaceHolder Elements
    Coroutine placeHolderCoroutine;
    Vector2 naturalSize;
    Vector2 contentsOffsetMin;
    Vector2 contentsOffsetMax;
    bool disCreatePlaceHolder;

    //Events
    public static System.Action<DraggableItem> OnBeingRemoved;
    public static System.Action<DraggableItem> OnBulletRemoved;

    private void Awake()
    {
        contentsOffsetMax = content.offsetMax;
        contentsOffsetMin = content.offsetMin;
    }

    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        placeHolderLayout = GetComponent<LayoutElement>();

        if (transform.childCount > 0)
            content = transform.GetChild(0).GetComponent<RectTransform>();
    }

    // Initialize Object
    public void Initialize(AnimationCurve easingCurve, float animationDuration, RecordableList recordableList,
        bool disCreatePlaceHolder, bool canBeDraggableToTheContainer)
    {
        swapEase = easingCurve;
        swapDuration = animationDuration;

        this.recordableList = recordableList;
        this.disCreatePlaceHolder = disCreatePlaceHolder;
        this.canBeDraggableToTheContainer = canBeDraggableToTheContainer;
    }

    //Drag Logic
    public void OnPointerDown(PointerEventData eventData)
    {
        _pointDownPosition = eventData.position;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        recordableList.OnItemDragStart(this);

        naturalSize = rectTransform.rect.size;

        dragOffset = (Vector2)content.position - eventData.position;
        content.SetParent(RootCanvas.transform, true);

        canvasGroup.blocksRaycasts = false;

        dragSortingCanvas = content.gameObject.AddComponent<Canvas>();
        dragSortingCanvas.overrideSorting = true;
        dragSortingCanvas.sortingOrder = 1000;

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            if (Vector2.Distance(eventData.position, _pointDownPosition) < DragThreshold)
                return;
            OnBeginDrag(eventData);
        }

        content.position = eventData.position + dragOffset;
        recordableList.OnItemBeingDragged(eventData.position);

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;

        RecordableList sourseRecordableList = recordableList;
        RecordableList destinationRecordableList = sourseRecordableList;

        // Check if the item can be dropped into another container
        if (canBeDraggableToTheContainer)
        {
            var HoveringOverGameObject = eventData.pointerCurrentRaycast.gameObject;
            destinationRecordableList = HoveringOverGameObject != null
                ? HoveringOverGameObject.GetComponentInParent<RecordableList>()
                : null;
        }

        canvasGroup.blocksRaycasts = true;
        if (dragSortingCanvas != null)
        {
            Destroy(dragSortingCanvas);
            dragSortingCanvas = null;
        }

        sourseRecordableList.OnItemDropped(this, destinationRecordableList, eventData.position);

    }

    //Put back to the original position
    public void ReturnVisualToContainer()
    {
        if (swapCoroutine != null)
        {
            StopCoroutine(swapCoroutine);
            swapCoroutine = null;
        }

        content.SetParent(rectTransform, false);
        content.offsetMax = contentsOffsetMax;
        content.offsetMin = contentsOffsetMin;
    }

    //Animation Logic if needed
    public void AnimateVisualToContainer(Canvas rootCanvas)
    {
        if (swapCoroutine != null)
        {
            StopCoroutine(swapCoroutine);
        }

        content.SetParent(rootCanvas.transform, true);
        swapCoroutine = StartCoroutine(LerpVisualToContainer());
    }

    private IEnumerator LerpVisualToContainer()
    {
        Vector3 StartWorld = content.position;
        float elapsed = 0f;

        while (elapsed < swapDuration)
        {
            elapsed += Time.deltaTime;
            float t = swapEase.Evaluate(Mathf.Clamp01(elapsed / swapDuration));
            content.position = Vector3.LerpUnclamped(StartWorld, rectTransform.position, t);
            yield return null;
        }

        ReturnVisualToContainer();
    }

    //Collapsing Container if the object is dragout out
    public void CollapsePlaceHolder()
    {
        if (disCreatePlaceHolder)
        {
            placeHolderLayout.ignoreLayout = true;
            return;
        }
        StartPlaceHolderAnimation(0f);
    }
    public void ExpandPlaceHolder()
    {
        if (disCreatePlaceHolder)
        {
            placeHolderLayout.ignoreLayout = false;
            return;
        }
        StartPlaceHolderAnimation(1f);
    }

    public void StartPlaceHolderAnimation(float targetFactor)
    {
        if (placeHolderCoroutine != null)
        {
            StopCoroutine(placeHolderCoroutine);
        }
        placeHolderCoroutine = StartCoroutine(AnimatePlaceHolder(targetFactor));
    }

    private IEnumerator AnimatePlaceHolder(float targetFactor)
    {
        float startWidth = placeHolderLayout.preferredWidth <  0f ? 
            naturalSize.x : placeHolderLayout.preferredWidth;
        float startHeight = placeHolderLayout.preferredHeight < 0f ? 
            naturalSize.y : placeHolderLayout.preferredHeight;
        float targetWidth = naturalSize.x * targetFactor;
        float targetHeight = naturalSize.y * targetFactor;

        float elapsed = 0f;
        while (elapsed < swapDuration)
        {
            elapsed += Time.deltaTime;
            float positionInTime = swapEase.Evaluate(Mathf.Clamp01(elapsed / swapDuration));

            float width = Mathf.LerpUnclamped(startWidth, targetWidth, positionInTime);
            float height = Mathf.LerpUnclamped(startHeight, targetHeight, positionInTime);

            placeHolderLayout.preferredWidth = width;
            placeHolderLayout.preferredHeight = height;
            placeHolderLayout.minWidth = width;
            placeHolderLayout.minHeight = height;

            yield return null;
        }

        placeHolderLayout.preferredWidth = targetWidth;
        placeHolderLayout.preferredHeight = targetHeight;
        placeHolderLayout.minWidth = targetWidth;
        placeHolderLayout.minHeight = targetHeight;

        placeHolderCoroutine = null;
    }

    //remove
    public void AnimateRemoval(AnimationCurve curve, float duration)
    {
        if (swapCoroutine != null)
            StopCoroutine(swapCoroutine);
        if (placeHolderCoroutine != null)
            StopCoroutine(placeHolderCoroutine);

        canvasGroup.blocksRaycasts = false;
        Vector3 visualWorldPosition = content.position;

        rectTransform.SetParent(RootCanvas.transform.transform, true);
        rectTransform.position = visualWorldPosition;

        content.SetParent(rectTransform, true);
        content.anchoredPosition = Vector3.zero;

        var deathCanvas = gameObject.GetComponent<Canvas>();
        deathCanvas.overrideSorting = true;
        deathCanvas.sortingOrder = 1000;

        StartCoroutine(ScaleDownThenDestory(curve, duration));
    }

    private IEnumerator ScaleDownThenDestory(AnimationCurve curve, float duration)
    {
        Vector3 startScale = rectTransform.localScale;
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float positionInTime = swapEase.Evaluate(Mathf.Clamp01(elapsed / duration));
            rectTransform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, positionInTime);
            yield return null;
        }

        rectTransform.localScale = Vector3.zero;
        //remove event call
        OnBeingRemoved?.Invoke(this);
        Destroy(gameObject);

    }

}