// **************************************************
//
//  Copyright (c) 2020 Shinichi Hasebe
//  This software is released under the MIT License.
//  http://opensource.org/licenses/mit-license.php
//
// **************************************************

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class LongPressable : MonoBehaviour
{
    public UnityEvent onPressBegin;
    public UnityEvent onPressHold;
    public UnityEvent onRelease;

    private bool isPressed;
    private EventTrigger eventTrigger;

    void Start()
    {
        eventTrigger = GetComponent<EventTrigger>();

        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) => 
        {
            isPressed = true;
            onPressBegin.Invoke();
        });

        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) => 
        { 
            isPressed = false;
            onRelease.Invoke();
        });

        eventTrigger.triggers.Add(pointerDownEntry);
        eventTrigger.triggers.Add(pointerUpEntry);
    }

    void Update()
    {
        if (isPressed)
        {
            onPressHold.Invoke();
        }
    }
}
