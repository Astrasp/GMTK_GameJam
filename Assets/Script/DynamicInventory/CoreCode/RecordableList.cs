using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class RecordableList : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] Canvas rootCanvas;
    [SerializeField] RectTransform thisContainer;
    [SerializeField] List<DraggableItem> items = new List<DraggableItem>();

    [Header("Other List")]
    [SerializeField] bool isElementsCanBeDraggableToTheContainer;

    [Header("Swapping Animation Value")]
    [SerializeField] float swapDuration = 0.15f;
    [SerializeField] AnimationCurve swapEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Removal Logic")]
    [SerializeField] bool draggingOutRemovalEntry;
    [SerializeField] float distanceUnitlRemoval = 100f;

    [Header("Removal Animation")]
    [SerializeField] float removeDuration = 0.2f;
    [SerializeField] AnimationCurve removeEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public Canvas RootCanvas => rootCanvas;
    public IReadOnlyList<DraggableItem> Items => items;

    RectTransform draggedContainer;
    int currentIndex;
    bool removalPending;
    PlaceHolderCollapse collapseMode;

    //events
    public static event Action<IReadOnlyList<DraggableItem>> OnOrderChanged;

    [ContextMenu("Grab List Entries")]
    void GrabListEntries()
    {
        items = GetComponentsInChildren<DraggableItem>().ToList();
    }

    private void Reset()
    {
        thisContainer = GetComponent<RectTransform>();
        //rootCanvas = GetComponent<Canvas>();
        items = GetComponentsInChildren<DraggableItem>().ToList();

    }

    private void Awake()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (thisContainer == null)
            thisContainer = GetComponent<RectTransform>();

        if (items.Count == 0)
            items = GetComponentsInChildren<DraggableItem>().ToList();

        collapseMode = thisContainer.GetComponent<GridLayoutGroup>() != null
            ? PlaceHolderCollapse.notSmooth
            : PlaceHolderCollapse.Smooth;

        foreach (var item in items) 
        {
            item.Initialize(swapEase, swapDuration, this, 
                collapseMode == PlaceHolderCollapse.notSmooth, 
                isElementsCanBeDraggableToTheContainer);
        }

    }

    //Add and Removing
    public void AddItem(DraggableItem item, int index = -1)
    {
        if (item == null) return;

        item.transform.SetParent(thisContainer);
        item.Initialize(swapEase, swapDuration, this,
            collapseMode == PlaceHolderCollapse.notSmooth,
            isElementsCanBeDraggableToTheContainer);


        if (index < 0)
        {
            items.Add(item);
            item.RectTransform.SetSiblingIndex(items.Count - 1);
        }
        else
        {
            index = Mathf.Clamp(index, 0, items.Count);
            items.Insert(index, item);
            item.RectTransform.SetSiblingIndex(index);
        }

        OnOrderChanged?.Invoke(items);
    }
    public void RemoveItem(DraggableItem item)
    {
        if (!items.Contains(item)) return;

        if (draggedContainer == item.RectTransform)
            draggedContainer = null;

        int removeIndex = items.IndexOf(item);
        items.Remove(item);
        OnOrderChanged?.Invoke(items);

        item.AnimateRemoval(removeEase, removeDuration);

        if (collapseMode == PlaceHolderCollapse.notSmooth)
        {
            AnimateNeighborsFrom(removeIndex);
        }
    }

    //Dragging Logic
    public void OnItemDragStart(DraggableItem item)
    {
        draggedContainer = item.RectTransform;
        currentIndex = items.IndexOf(item);
    }

    public void OnItemBeingDragged(Vector2 screenPosition)
    {
        if (draggedContainer == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                thisContainer, screenPosition, rootCanvas.worldCamera,
                out Vector2 localPoint
        );

        bool wantsRemoval = draggingOutRemovalEntry && IsOutsideRemovalThreshold(localPoint);

        if (wantsRemoval != removalPending)
        {
            removalPending = wantsRemoval;
            if (wantsRemoval)
            {
                items[currentIndex].CollapsePlaceHolder();
            }
            else
            {
                items[currentIndex].ExpandPlaceHolder();
            }

            if (collapseMode == PlaceHolderCollapse.notSmooth)
                AnimateNeighborsFrom(currentIndex + 1);
        }

        if (removalPending) return;

        int targetIndex = GetTargetIndex(localPoint, currentIndex);
        if (targetIndex != currentIndex)
        {
            MoveItem(currentIndex, targetIndex);
        }
    }

    private bool IsOutsideRemovalThreshold(Vector2 localPoint)
    {
        if(thisContainer.rect.Contains(localPoint)) return false;

        float clampX = Mathf.Clamp(localPoint.x, thisContainer.rect.xMin, thisContainer.rect.xMax);
        float clampY = Mathf.Clamp(localPoint.y, thisContainer.rect.yMin, thisContainer.rect.yMax);
        Vector2 closePoint = new Vector2(clampX, clampY);

        return Vector2.Distance(localPoint, closePoint) >= distanceUnitlRemoval;
    }

    public void OnItemDropped(DraggableItem item, RecordableList destinationList, Vector2 screenposition)
    {
        if (draggedContainer == null) return;
        draggedContainer = null;

        if (destinationList != null && destinationList != this)
        {
            removalPending = false;
            TranferItemTo(item, destinationList, screenposition);
            return;
        }

        if (removalPending)
        {
            removalPending = false;
            RequestRemoval(item);
            return;
        }

        item.ReturnVisualToContainer();
        OnOrderChanged?.Invoke(items);
    }

    public void TranferItemTo(DraggableItem item, RecordableList destinationList, Vector2 screenPosition)
    {
        if(!items.Contains(item)) return;

        item.ExpandPlaceHolder();

        items.Remove(item);
        OnOrderChanged?.Invoke(items);

        int destinationIndex = destinationList.GetTargetIndexFromScreenPosition(screenPosition);
        destinationList.AddItem(item, destinationIndex);

        item.ReturnVisualToContainer();

        if (collapseMode == PlaceHolderCollapse.notSmooth)
            AnimateNeighborsFrom(currentIndex);
    }

    public void MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;

        DraggableItem moved = items[fromIndex];

        items.RemoveAt(fromIndex);
        items.Insert(toIndex, moved);

        int low = Mathf.Min(fromIndex, toIndex);
        int high = Mathf.Max(fromIndex, toIndex);

        for (int i = low; i <= high; i++)
        {
            items[i].RectTransform.SetSiblingIndex(i);

            if (items[i] != moved)
                items[i].AnimateVisualToContainer(rootCanvas);
        }

        currentIndex = toIndex;
    }

    //Removing
    public void RequestRemoval(DraggableItem item)
    {
        if (draggedContainer == item.RectTransform)
            draggedContainer = null;

        //Comfirm Remove
        ConfirmRemoval(item);
    }

    public void ConfirmRemoval(DraggableItem item)
    {
        removalPending = false;
        RemoveItem(item);
    }

    //Suppert Methods
    private void AnimateNeighborsFrom(int startIndex)
    {
        for (int i = Mathf.Max(startIndex, 0); i < items.Count; i++)
            items[i].AnimateVisualToContainer(rootCanvas);
    }

    private int GetTargetIndex(Vector2 localPoint, int fallback)
    {
        if (items.Count == 0) return 0;

        for (int i = 0; i < items.Count; i++)
        {
            if (RectContainsLocalPoint(items[i].RectTransform, localPoint))
                return i;
        }

        int nearest = fallback;
        float bestSqrtDistance = float.MaxValue;

        for (int i = 0; i < items.Count; i++)
        {
            float sqrDistance = ((Vector2)items[i].RectTransform.localPosition - localPoint).sqrMagnitude;
            if (sqrDistance < bestSqrtDistance)
            {
                bestSqrtDistance = sqrDistance;
                nearest = i;
            }
        }

        return nearest;
    }

    private int GetTargetIndexFromScreenPosition(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
                thisContainer, screenPosition, rootCanvas.worldCamera,
                out Vector2 localPoint
            );
        return GetTargetIndex(localPoint, 0);
    }

    private static bool RectContainsLocalPoint(RectTransform rt, Vector2 localPoint)
    {
        Vector2 offset = localPoint - (Vector2)rt.localPosition;
        return Mathf.Abs(offset.x) <= rt.rect.width * 0.5 &&
            Mathf.Abs(offset.y) <= rt.rect.height * 0.5;
    }
}

public enum PlaceHolderCollapse 
{ 
    Smooth, 
    notSmooth 
}