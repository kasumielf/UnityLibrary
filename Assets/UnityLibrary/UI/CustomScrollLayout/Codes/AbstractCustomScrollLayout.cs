﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class OnFocusEvent : UnityEvent<int>
{
    public OnFocusEvent() { }
}

public abstract class AbstractCustomScrollLayout : MonoBehaviour, IEventSystemHandler
{
    [Header("ScrollRect Content Container")]
    public GameObject contentView;
    [Header("ScrollRect Magnetic")]
    public bool IsMagnetic = false;
    [Header("ScrollRect Infinite")]
    public bool IsInfinite = true;
    [Header("Initialize on Start")]
    public bool InitOnStart = false;

    [SerializeField]
    public OnFocusEvent onFocusedEvent;

    protected bool IsVertical;
    protected RectTransform contentViewRect;

    private ScrollRect scrollRect;
    private RectTransform selfRect;

    private EventTrigger eventTrigger;
    private bool onDrag = false;
    private bool notUseLayout = true;

    private int currIndex;
    private int curr;
    private int childLen;
    private float contentViewSize;
    private float childSize;
    private float childWorldSize;
    private float spacing = 0.0f;

    // use for magnetic
    private float befAnchor;
    private float aftAnchor;

    public float dest = 0.0f;

    public float[] childDistance;
    public float[] childReposition;
    private RectTransform[] childs;

    // Use this for initialization
    void Start()
    {
        selfRect = gameObject.GetComponent<RectTransform>();
        scrollRect = gameObject.GetComponent<ScrollRect>();
        eventTrigger = gameObject.GetComponent<EventTrigger>();

        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect is empty");
        }
        else
        {
            scrollRect.vertical = IsVertical;
            scrollRect.horizontal = !IsVertical;

            scrollRect.inertia = false;
        }

        if (contentView == null)
        {
            Debug.LogError("Content Container is empty. Please Attach ViewPort or Content View");
            throw new Exception("Content Container is empty. Please Attach ViewPort or Content View");
        }
        else
        {
            if (InitOnStart)
                StartCoroutine(Initialize());
        }

        if (eventTrigger == null)
        {
            Debug.LogError("EventTrigger is empty");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (childs != null)
        {
            int len = childs.Length;
            float scrollSize = 0.0f;

            scrollSize = GetRectSize(selfRect.rect);
            contentViewSize = GetRectSize(contentViewRect.rect);

            for (int i = 0; i < len; ++i)
            {
                childReposition[i] = GetValue(selfRect.position) - GetValue(childs[i].position);
                childDistance[i] = Mathf.Abs(childReposition[i]);

                if (IsInfinite)
                {
                    if (childReposition[i] > dest)
                    {
                        var vector = childs[i].anchoredPosition;
                        IncreaseVector(ref vector, childLen * childSize);
                        childs[i].anchoredPosition = vector;
                    }

                    if (childReposition[i] < dest * -1)
                    {
                        var vector = childs[i].anchoredPosition;
                        DecreaseVector(ref vector, childLen * childSize);
                        childs[i].anchoredPosition = vector;
                    }
                }
            }

            float minDistance = Mathf.Min(childDistance);

            for (int j = 0; j < len; ++j)
            {
                if (minDistance == childDistance[j])
                {
                    currIndex = j;
                    onFocusedEvent.Invoke(currIndex);
                }
            }

            if (IsMagnetic)
            {
                if (onDrag == false)
                {
                   float v = GetValue(childs[currIndex].anchoredPosition) * -1.0f;
                   LerpToPosition(v);
                }
            }
        }
    }

    private void LerpToPosition(float position)
    {
        float newVal = Mathf.Lerp(GetValue(contentViewRect.anchoredPosition), position, Time.deltaTime * 5f);

        contentViewRect.anchoredPosition = ModifyVector(contentViewRect.anchoredPosition, newVal);
    }

    private void SetChildsFromTransform(Transform trans)
    {
        // Initialize Childs if user for dynamic content childs
        var c = trans.childCount;
        childLen = trans.childCount;

        childs = new RectTransform[childLen];
        childDistance = new float[childLen];
        childReposition = new float[childLen];

        int i = 0;
        foreach (RectTransform obj in trans)
        {
            childs[i] = obj;
            i++;
        }
    }

    private void RefreshContentChilds()
    {
        SetChildsFromTransform(contentView.transform);
        childLen = childs.Length;
    }

    #region Public Methods
    public IEnumerator Initialize()
    {
        contentViewSize = 0.0f;
        contentViewRect = contentView.GetComponent<RectTransform>();

        float scrollRectSize = GetRectSize(selfRect.rect);

        // Assign Content Container's childlen
        RefreshContentChilds();

        if (childLen > 1)
        {
            childSize = GetRectSize(childs[0].rect);
            //childSize = Mathf.Abs(GetValue(childs[1].anchoredPosition) - GetValue(childs[0].anchoredPosition));

            if (childSize <= 0.0f)
            {
                childSize = scrollRectSize;
            }

            var layout = contentView.GetComponent<HorizontalOrVerticalLayoutGroup>();

            if (layout != null)
            {
                childSize += layout.spacing;
                spacing = layout.spacing;
                notUseLayout = false;
            }
        }

        // if Child Objects are not enought to width or height, cloning it for size fit
        if (childLen <= 3 && IsInfinite)
        {
            List<GameObject> clones = new List<GameObject>();

            foreach (Transform obj in contentView.transform)
            {
                clones.Add(obj.gameObject);
            }

            int len = clones.Count;

            while (childLen <= 3)
            {
                for (int i = 0; i < len; ++i)
                {
                    Instantiate(clones[i], contentView.transform);
                    childLen++;
                }
            }

            RefreshContentChilds();
        }

        yield return new WaitForEndOfFrame();

        var v = Camera.main.ScreenToWorldPoint(contentViewRect.rect.size);
        dest = GetValue(v) / 2.0f;
        childWorldSize = dest / childLen;
        //ResetPivotAndPosition();
    }
    #endregion

    #region Abstract Methods
    protected abstract float GetValue(Vector2 vector);
    protected abstract Vector2 ModifyVector(Vector2 source, float value);
    protected abstract void IncreaseVector(ref Vector2 vector, float value);
    protected abstract void DecreaseVector(ref Vector2 vector, float value);
    protected abstract float GetRectSize(Rect rect);
    protected abstract void ResetPivotAndPosition();
    #endregion

    #region Event Trigger
    public void BeginDrag()
    {
        onDrag = true;
    }

    public void EndDrag()
    {
        onDrag = false;
    }
    #endregion
}