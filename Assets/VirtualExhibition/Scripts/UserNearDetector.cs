﻿// **************************************************
//
//  Copyright (c) 2020 Shinichi Hasebe
//  This software is released under the MIT License.
//  http://opensource.org/licenses/mit-license.php
//
// **************************************************

using UnityEngine;
using UnityEngine.Events;

public class UserNearDetector : MonoBehaviour
{
    public Camera targetCamera;
    public float detectDistance = 4.0f;
    public bool manageRenderer = true;
    public bool manageCollider = true;

    public UnityEvent onUserDetect;
    public UnityEvent onUserLost;

    private Renderer[] renderers;
    private Collider[] colliders;
    private bool isDetected;

    // Start is called before the first frame update
    void Start()
    {
        // RendererとColliderを取得
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();

        // RendererとColliderを無効化
        UpdateState(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(targetCamera != null)
        {
            float distance = Vector3.Distance(targetCamera.transform.position, this.gameObject.transform.position);

            if (distance <= detectDistance && !isDetected)
            {
                UpdateState(true);

                onUserDetect?.Invoke();
            }
            else if(distance > detectDistance && isDetected)
            {
                UpdateState(false);

                onUserLost?.Invoke();
            }
        }
    }

    private void UpdateState(bool state)
    {
        isDetected = state;

        if (manageRenderer)
        {
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = state;
            }
        }

        if (manageCollider)
        {
            foreach (Collider collider in colliders)
            {
                collider.enabled = state;
            }
        }
    }
}
