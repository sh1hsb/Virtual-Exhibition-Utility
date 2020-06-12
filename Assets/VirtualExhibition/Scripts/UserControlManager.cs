// **************************************************
//
//  Copyright (c) 2020 Shinichi Hasebe
//  This software is released under the MIT License.
//  http://opensource.org/licenses/mit-license.php
//
// **************************************************

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UserControlManager : MonoBehaviour
{
    public enum ControlState
    {
        None,
        CameraControl,
        ObjectControl,
        MoveToPoint,
        PopupEvent
    }

    public enum TouchState
    {
        None,
        Single,
        Multi
    }

    public enum ClickMoveType
    {
        None,
        Freewalk,
        ToClosestPoint
    }

    public ControlState controlState = ControlState.None;

    [Header("Camera Control Setting")]
    public Camera targetCamera;
    [Space(10)]
    public Vector2 rotationSpeed = new Vector2(0.05f, 0.05f);
    public bool reverseRotationHorizontal;
    public bool reverseRotationVertical;
    [Space(10)]
    
    public ClickMoveType clickMoveType = ClickMoveType.None;
    public float moveRate = 1f;
    [Range(0f, 1f)] public float moveSmoothing = 0.1f;
    public bool lookAtMovePointOnMove;
    [Range(0f, 1f)] public float forwardObjectDetectThreshold = 0.5f;
    public List<GameObject> movePointObjects;
    [Space(10)]
    public float fovRate = 10f;
    public float fovPinchRate = 0.05f;
    public float fovMin = 30f;
    public float fovMax = 80f;

    [Header("Tags")]
    public string controllableObjectTag = "ControllableObject";
    public string movePointObjectTag = "MovePointObject";
    public string popupEventObjectTag = "PopupEventObject";


    private Vector3 cameraPosition;
    private Vector2 cameraAngle;
    private Vector2 objectAngle;
    private Vector2 initialMousePosition;
    private Vector2 lastMousePosition;
    private Quaternion cameraPose;

    RaycastHit hit;
    Transform hitTransform;

    private GameObject lastClosestPointObject;

    private PopupEventInvoker currentPopupEvent;

    private float touchDistanceDelta;
    private float lastTouchDistance;
    private bool multiTouchEngaged;

    public bool IsEventInvoked
    {
        get
        {
            return controlState == ControlState.MoveToPoint || controlState == ControlState.PopupEvent;
        }
    }

    void Start()
    {
        if(targetCamera != null)
        {
            // 移動目標の位置を現在の位置に設定しておく
            cameraPosition = targetCamera.transform.position;
        }
        
        foreach(GameObject obj in movePointObjects)
        {
            obj.transform.tag = movePointObjectTag;
        }
    }

    void Update()
    {
        if(targetCamera != null)
        {
            // 現在のカーソル位置
            Vector3 cursorPosition;

            // タッチを検出
            Touch touchData;
            TouchPhase touchPhase = TouchPhase.Canceled;
            TouchState touchState = TouchState.None;

            // タッチ数が1つの時にタッチ検出としデータを取得
            if(Input.touchCount > 0)
            {
                touchData = Input.GetTouch(0);
                touchPhase = touchData.phase;
                cursorPosition = touchData.position;

                if(Input.touchCount == 1)
                {
                    // 
                    touchState = TouchState.Single;
                    if (multiTouchEngaged) multiTouchEngaged = false;
                    //Debug.Log("Single Touch Detected");
                }
                else
                {
                    touchState = TouchState.Multi;

                    // 2つめのタッチポイントを取得
                    Touch secondTouchData = Input.GetTouch(1);

                    // 2つのタッチポイント間の距離を測定
                    float touchDist = Vector2.Distance(touchData.position, secondTouchData.position);

                    if (!multiTouchEngaged)
                    {
                        multiTouchEngaged = true;
                        lastTouchDistance = touchDist;
                    }

                    // 前フレームとの差を計算
                    touchDistanceDelta = touchDist - lastTouchDistance;

                    // 今回の計算値を保存
                    lastTouchDistance = touchDist;

                    //Debug.Log("Multi Touch Detected");
                }
            }
            
            // そのほかの場合はタッチ未検出とし、カーソル位置はマウス位置とする
            else
            {
                cursorPosition = Input.mousePosition;
            }

            // カーソル位置からRayを飛ばす
            Ray ray = targetCamera.ScreenPointToRay(cursorPosition);
            Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red, 3.0f);


            // --------------------------------------------------
            // 左クリック or シングルタップ開始
            // --------------------------------------------------
            if ((touchState == TouchState.Single && touchPhase == TouchPhase.Began) || Input.GetMouseButtonDown(0))
            {
                // UI上にポインタがあるかどうかチェック
                bool isNotPointerOverUI = EventSystem.current != null ? !EventSystem.current.IsPointerOverGameObject() : true;

                // イベント発生中、またはUI上にカーソルがある場合は操作しない
                if (!IsEventInvoked && isNotPointerOverUI)
                {
                    // Rayがヒットしたオブジェクトについて判定を行う
                    if (Physics.Raycast(ray, out hit))
                    {
                        // Transformを取得
                        hitTransform = hit.transform;

                        // タグを取得
                        // "ControllableObject"
                        if (hitTransform.CompareTag(controllableObjectTag))
                        {
                            controlState = ControlState.ObjectControl;
                            objectAngle = hitTransform.localEulerAngles;
                            //Debug.Log("Object Control Start");
                        }
                        // "MovePointObject"
                        else if (hitTransform.CompareTag(movePointObjectTag))
                        {
                            controlState = ControlState.MoveToPoint;
                            cameraAngle = targetCamera.transform.localEulerAngles;
                            //Debug.Log("Move to Point Start");
                        }
                        // "PopupEventObject"
                        else if (hitTransform.CompareTag(popupEventObjectTag))
                        {
                            // PopupEventInvokerを取得
                            currentPopupEvent = hitTransform.GetComponent<PopupEventInvoker>();

                            // PopupEventInvokerが取得出来たらポップアップを実行
                            if (currentPopupEvent != null)
                            {
                                controlState = ControlState.PopupEvent;
                                currentPopupEvent.Popup();
                                //Debug.Log("Popup Start");
                            }
                            // 取得できなかった場合はCameraControlと同様の処理を行う
                            else
                            {
                                controlState = ControlState.CameraControl;
                                cameraAngle = targetCamera.transform.localEulerAngles;
                                //Debug.Log("Camera Control Start (No Popup)");
                            }
                        }
                        // タグなし
                        else
                        {
                            controlState = ControlState.CameraControl;
                            cameraAngle = targetCamera.transform.localEulerAngles;
                            //Debug.Log("Camera Control Start (No tag)");
                        }
                    }
                    else
                    {
                        controlState = ControlState.CameraControl;
                        cameraAngle = targetCamera.transform.localEulerAngles;
                        //Debug.Log("Camera Control Start (No Hit)");
                    }

                    // カーソル位置を取得し初期値とする
                    initialMousePosition = cursorPosition;
                    lastMousePosition = cursorPosition;
                }

                // ポイントへの移動時は移動時または移動直後に回転ができるように各値を取得する
                // 移動しきっていない状態でクリックしてしまったときに対する処理
                else if(controlState == ControlState.MoveToPoint)
                {
                    cameraAngle = targetCamera.transform.localEulerAngles;
                    initialMousePosition = cursorPosition;
                    lastMousePosition = cursorPosition;
                    //Debug.Log("Camera Control Start (Moving)");
                }

                // ポップアップ時はUI上でない場所でクリックするとポップアップ解除
                // タッチ時UI外を2回タップしないと解除できないのでそこは要検討
                else if (controlState == ControlState.PopupEvent && isNotPointerOverUI)
                {
                    //Debug.Log("Popup End?");

                    if (currentPopupEvent != null)
                    {
                        currentPopupEvent.Disappear();
                        ResetState();
                        //Debug.Log("Popup Dissapear");
                    }                }
            }

            // --------------------------------------------------
            // 左クリック or シングルタップ継続
            // --------------------------------------------------
            else if ((touchState == TouchState.Single && (touchPhase == TouchPhase.Moved || touchPhase == TouchPhase.Stationary)) || (touchState != TouchState.Multi && Input.GetMouseButton(0)))
            {
                switch (controlState)
                {
                    case ControlState.CameraControl:
                    case ControlState.MoveToPoint:

                        // カーソルの位置から回転量を計算
                        cameraAngle.y += (reverseRotationHorizontal ? -1 : 1) * (lastMousePosition.x - cursorPosition.x) * rotationSpeed.y;
                        cameraAngle.x += (reverseRotationVertical ? -1 : 1) * (cursorPosition.y - lastMousePosition.y) * rotationSpeed.x;

                        // オブジェクトに回転を適用
                        targetCamera.transform.localEulerAngles = cameraAngle;
                        break;

                    case ControlState.ObjectControl:

                        // カーソルの位置から回転量を計算
                        objectAngle.y += (lastMousePosition.x - cursorPosition.x) * rotationSpeed.y;
                        objectAngle.x += (cursorPosition.y - lastMousePosition.y) * rotationSpeed.x;

                        // オブジェクトに回転を適用
                        hitTransform.localEulerAngles = objectAngle;
                        break;

                    default:
                        break;
                }

                if (controlState != ControlState.PopupEvent)
                {
                    lastMousePosition = cursorPosition;
                }  
            }

            // --------------------------------------------------
            // 左クリック or シングルタップ終了
            // --------------------------------------------------
            else if ((touchState == TouchState.Single && (touchPhase == TouchPhase.Ended || touchPhase == TouchPhase.Canceled)) || Input.GetMouseButtonUp(0))
            {
                switch (controlState)
                {
                    case ControlState.CameraControl:

                        // 移動位置を設定
                        //Debug.Log("camera pos update? (camera control)");

                        switch (clickMoveType)
                        {
                            case ClickMoveType.Freewalk:
                                if (Vector2.Distance(initialMousePosition, lastMousePosition) < 0.1f)
                                {
                                    // カメラ位置を更新
                                    cameraPosition += new Vector3(ray.direction.x, 0f, ray.direction.z) * moveRate;

                                    //Debug.Log("camera pos update (camera control)");
                                }
                                break;

                            case ClickMoveType.ToClosestPoint:

                                if(Vector2.Distance(initialMousePosition, lastMousePosition) < 0.1f && movePointObjects.Count > 0)
                                {
                                    GameObject target = null;
                                    float minDistance = 100f;

                                    // 登録されたオブジェクトとの距離を計測
                                    foreach (GameObject obj in movePointObjects)
                                    {
                                        // 最後に移動した地点のオブジェクトは飛ばす
                                        if(lastClosestPointObject != null && obj == lastClosestPointObject)
                                        {
                                            continue;
                                        }

                                        // オブジェクトまでの距離を計算
                                        float dist = Vector3.Distance(targetCamera.transform.position, obj.transform.position);

                                        // 距離が近く、カメラの前方にあればターゲット判定する
                                        if (dist < minDistance && Vector3.Dot((obj.transform.position - targetCamera.transform.position).normalized, targetCamera.transform.forward) > forwardObjectDetectThreshold)
                                        {
                                            minDistance = dist;
                                            target = obj;
                                        }
                                    }

                                    if(target != null)
                                    {
                                        // 移動ターゲットを更新
                                        lastClosestPointObject = target;

                                        // 状態をMoveToPointに更新する
                                        controlState = ControlState.MoveToPoint;

                                        // カメラ位置を更新
                                        cameraPosition = new Vector3(target.transform.position.x, targetCamera.transform.position.y, target.transform.position.z);

                                        // 移動方向を向く
                                        if (lookAtMovePointOnMove)
                                        {
                                            // 目的の方向へ向くための回転を計算
                                            Vector3 targetRot = Quaternion.LookRotation(target.transform.position - targetCamera.transform.position, Vector3.up).eulerAngles;

                                            // 現在の姿勢を取得
                                            Vector3 currentRot = targetCamera.transform.rotation.eulerAngles;

                                            // Y軸のみを回転するようにする
                                            cameraPose = Quaternion.Euler(currentRot.x, targetRot.y, currentRot.z);
                                        }

                                        //Debug.Log("camera pos update (to closest point)");
                                    }
                                }
                                break;

                            default:
                                break;
                        }

                        break;

                    case ControlState.MoveToPoint:

                        // 移動位置を設定
                        if (Vector2.Distance(initialMousePosition, lastMousePosition) < 0.1f)
                        {
                            //Debug.Log("camera pos update? (move to point)");

                            if (hitTransform != null)
                            {
                                // カメラ位置を更新
                                cameraPosition = new Vector3(hitTransform.position.x, targetCamera.transform.position.y, hitTransform.position.z);

                                // 移動方向を向く
                                if (lookAtMovePointOnMove)
                                {
                                    // 目的の方向へ向くための回転を計算
                                    Vector3 targetRot = Quaternion.LookRotation(hitTransform.position - targetCamera.transform.position, Vector3.up).eulerAngles;

                                    // 現在の姿勢を取得
                                    Vector3 currentRot = targetCamera.transform.rotation.eulerAngles;

                                    // Y軸のみを回転するようにする
                                    cameraPose = Quaternion.Euler(currentRot.x, targetRot.y, currentRot.z);
                                }

                                //Debug.Log("camera pos update (move to point)");
                            }
                        }
                        else
                        {
                            controlState = ControlState.None;
                            //Debug.Log("camera pos no update (move to point)");
                        }
                        break;

                    default:
                        break;
                }
                
                hitTransform = null;
            }
            else
            {
                if (!IsEventInvoked)
                {
                    controlState = ControlState.None;
                }
            }

            // カメラの位置・姿勢を更新
            if(Vector3.Distance(targetCamera.transform.position, cameraPosition) > 0.01f)
            {
                // 位置を更新
                targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, cameraPosition, moveSmoothing);

                // 姿勢を更新
                if(controlState == ControlState.MoveToPoint && lookAtMovePointOnMove)
                {
                    targetCamera.transform.rotation = Quaternion.Lerp(targetCamera.transform.rotation, cameraPose, 0.1f);
                }

                // 目的地に到着したら
                if(Vector3.Distance(targetCamera.transform.position, cameraPosition) <= 0.01f)
                {
                    targetCamera.transform.position = cameraPosition;

                    // 移動完了前にクリックしていた場合に状態が切り替わらないため、ここでCameraControlに状態変更する
                    if (controlState == ControlState.MoveToPoint)
                    {
                        //controlState = ControlState.None;
                        controlState = ControlState.CameraControl;
                    }
                }
            }

            // カメラのFOVをコントロール
            if (!IsEventInvoked)
            {
                float camFOV;

                if(touchState == TouchState.Multi)
                {
                    camFOV = targetCamera.fieldOfView - touchDistanceDelta * fovPinchRate;
                }
                else
                {
                    camFOV = targetCamera.fieldOfView + Input.GetAxis("Mouse ScrollWheel") * fovRate;
                }

                if (camFOV >= fovMin && camFOV <= fovMax)
                {
                    targetCamera.fieldOfView = camFOV;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Collision");
    }

    private void OnTriggerEnter(Collider other)
    {
        if(CheckCollideObjectIsNotEventObject(other))
        {
            Debug.Log("Trigger object...");
        }
        else
        {
            Debug.Log("Trigger event object.");
        }
    }

    public void ResetState()
    {
        controlState = ControlState.None;
        hitTransform = null;
        currentPopupEvent = null;
    }

    public void WarpTo(Vector3 position)
    {
        if (targetCamera != null)
        {
            targetCamera.transform.position = position;
            cameraPosition = position;
        }
    }

    public void MoveTo(Vector3 position)
    {
        cameraPosition = position;
    }

    private bool CheckCollideObjectIsNotEventObject(Collider other)
    {
        if(other.gameObject.transform.CompareTag(movePointObjectTag) || other.gameObject.transform.CompareTag(popupEventObjectTag))
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}
