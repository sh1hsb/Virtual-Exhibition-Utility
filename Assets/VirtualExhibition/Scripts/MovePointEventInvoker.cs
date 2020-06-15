// **************************************************
//
//  Copyright (c) 2020 Shinichi Hasebe
//  This software is released under the MIT License.
//  http://opensource.org/licenses/mit-license.php
//
// **************************************************

using UnityEngine;
using UnityEngine.Events;

public class MovePointEventInvoker : MonoBehaviour
{
    public UnityEvent movePointEvents;

    public void Invoke()
    {
        movePointEvents?.Invoke();
    }
}
