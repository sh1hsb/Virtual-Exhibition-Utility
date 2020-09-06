// **************************************************
//
//  Copyright (c) 2020 Shinichi Hasebe
//  This software is released under the MIT License.
//  http://opensource.org/licenses/mit-license.php
//
// **************************************************

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UserControlManager : MonoBehaviour
{
    #region Classes, Structures, Enumerated types
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
        ToClosestAutoMovePoint
    }

    public enum ColliderType
    {
        None,
        Sphere,
        Capsule,
        Others
    }
    #endregion

    #region Public and Serialized Fields
    public ControlState controlState = ControlState.None;

    [Header("Camera Control Setting")]
    public Camera playerCamera;
    
    [Space(10)]
    public Vector2 rotationSpeed = new Vector2(0.05f, 0.05f);
    public bool reverseRotationHorizontal;
    public bool reverseRotationVertical;
    
    [Space(10)]
    public ClickMoveType clickMoveType = ClickMoveType.None;
    public float moveRate = 1f;
    [Range(0f, 1f)] public float moveSmoothing = 0.1f;
    public bool lookAtMovePointOnMove;
    public float closestAutoMovePointDetectOriginDistance = 3.0f;
    [Range(0f, 1f)] public float forwardClosestAutoMovePointDetectRange = 0.5f;
    public List<GameObject> autoMovePointObjects;
    
    [Space(10)]
    public float fovRate = 10f;
    public float fovPinchRate = 0.05f;
    public float fovMin = 30f;
    public float fovMax = 80f;

    [Header("Camera Collider Setting")]
    public bool enableCollider;
    public float wallBoundValue = 0.5f;

    [Header("Ground Standing Setting")]
    public bool enableGroundStanding;
    public bool stayHeightOnNoGround = true;
    public float heightFromGround = 1.7f;
    public float groundHeightTolerance = 0.05f; // 地面からの高さに対する許容誤差
    public float upLiftScale = 0.1f;            // 地面からの高さまで座標を高くする際に急に飛び出したようにしないための移動倍率

    [Header("Tags")]
    public string controllableObjectTag = "ControllableObject";
    public string movePointObjectTag = "MovePointObject";
    public string popupEventObjectTag = "PopupEventObject";
    #endregion

    #region Private Fields
    private Vector3 cameraPosition;
    private Vector2 cameraAngle;
    private Vector2 objectAngle;
    private Vector2 initialMousePosition;
    private Vector2 lastMousePosition;
    private Quaternion cameraPose;
    private bool isBounded;

    private RaycastHit hit;
    private Transform hitTransform;

    private GameObject lastClosestPointObject;

    private PopupEventInvoker currentPopupEvent;

    private float touchDistanceDelta;
    private float lastTouchDistance;
    private bool multiTouchEngaged;

    private Collider playerCollider;
    private ColliderType playerColliderType = ColliderType.None;
    private float colliderRadius;
    private float colliderHeight;
    private Vector3 colliderCenter;
    private int colliderDirection = -1;
    private Vector3 capsuleSpherePos;

    private Vector3 nextMoveDirection;
    #endregion

    #region Properties
    public bool IsEventInvoked
    {
        get
        {
            return controlState == ControlState.MoveToPoint || controlState == ControlState.PopupEvent;
        }
    }
    #endregion

    #region Unity API
    void Start()
    {
        // 子オブジェクトからカメラを取得
        playerCamera = GetComponentInChildren<Camera>();

        if(playerCamera != null)
        {
            // 移動目標の位置を現在の位置に設定しておく
            cameraPosition = transform.position;
        }

        // Colliderを取得
        playerCollider = GetComponent<Collider>();

        if(playerCollider != null)
        {
            if(playerCollider is SphereCollider)
            {
                playerColliderType = ColliderType.Sphere;
                SphereCollider c = GetComponent<SphereCollider>();
                colliderRadius = c.radius;
                colliderCenter = c.center;
            }
            else if(playerCollider is CapsuleCollider)
            {
                playerColliderType = ColliderType.Capsule;
                CapsuleCollider c = GetComponent<CapsuleCollider>();
                colliderRadius = c.radius;
                colliderHeight = c.height;
                colliderCenter = c.center;
                colliderDirection = c.direction;

                switch (colliderDirection)
                {
                    case 0: // x
                        capsuleSpherePos = new Vector3(colliderHeight / 2 - colliderRadius, 0f, 0f);
                        break;

                    case 1: // y
                        capsuleSpherePos = new Vector3(0f, colliderHeight / 2 - colliderRadius, 0f);
                        break;

                    case 2: // z
                        capsuleSpherePos = new Vector3(0f, 0f, colliderHeight / 2 - colliderRadius);
                        break;

                    default:
                        capsuleSpherePos = Vector3.zero;
                        break;
                }
            }
            else
            {
                playerColliderType = ColliderType.Others;
                enableCollider = false;
            }
        }
        else
        {
            enableCollider = false;
        }
        
        // 自動移動ポイントのオブジェクトのタグを変更
        foreach(GameObject obj in autoMovePointObjects)
        {
            obj.transform.tag = movePointObjectTag;
        }
    }

    void Update()
    {
        if(playerCamera != null)
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
                }
            }
            
            // そのほかの場合はタッチ未検出とし、カーソル位置はマウス位置とする
            else
            {
                cursorPosition = Input.mousePosition;
            }

            // カーソル位置からRayを飛ばす
            Ray ray = playerCamera.ScreenPointToRay(cursorPosition);
            //rayDir = ray.direction;

            #if UNITY_EDITOR
            Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red);
            nextMoveDirection = new Vector3(ray.direction.x, 0f, ray.direction.z);

            Ray rayToForward = new Ray(transform.position, new Vector3(transform.forward.x, 0f, transform.forward.z));
            Debug.DrawRay(rayToForward.origin, rayToForward.direction.normalized, Color.blue);
            #endif

            if (!isBounded)
            {
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
                            }
                            // "MovePointObject"
                            else if (hitTransform.CompareTag(movePointObjectTag))
                            {
                                controlState = ControlState.MoveToPoint;
                                cameraAngle.x = playerCamera.transform.localEulerAngles.x;
                                cameraAngle.y = transform.localEulerAngles.y;
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
                                }
                                // 取得できなかった場合はCameraControlと同様の処理を行う
                                else
                                {
                                    controlState = ControlState.CameraControl;
                                    cameraAngle.x = playerCamera.transform.localEulerAngles.x;
                                    cameraAngle.y = transform.localEulerAngles.y;
                                }
                            }
                            // タグなし
                            else
                            {
                                controlState = ControlState.CameraControl;
                                cameraAngle.x = playerCamera.transform.localEulerAngles.x;
                                cameraAngle.y = transform.localEulerAngles.y;
                            }
                        }
                        else
                        {
                            controlState = ControlState.CameraControl;
                            cameraAngle.x = playerCamera.transform.localEulerAngles.x;
                            cameraAngle.y = transform.localEulerAngles.y;
                        }

                        // カーソル位置を取得し初期値とする
                        initialMousePosition = cursorPosition;
                        lastMousePosition = cursorPosition;
                    }

                    // ポイントへの移動時は移動時または移動直後に回転ができるように各値を取得する
                    // 移動しきっていない状態でクリックしてしまったときに対する処理
                    else if (controlState == ControlState.MoveToPoint)
                    {
                        cameraAngle.x = playerCamera.transform.localEulerAngles.x;
                        cameraAngle.y = transform.localEulerAngles.y;
                        initialMousePosition = cursorPosition;
                        lastMousePosition = cursorPosition;
                    }

                    // ポップアップ時はUI上でない場所でクリックするとポップアップ解除
                    // タッチ時UI外を2回タップしないと解除できないのでそこは要検討
                    else if (controlState == ControlState.PopupEvent && isNotPointerOverUI)
                    {
                        if (currentPopupEvent != null)
                        {
                            currentPopupEvent.Disappear();
                            ResetState();
                        }
                    }
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
                            playerCamera.transform.localEulerAngles = new Vector3(cameraAngle.x, playerCamera.transform.localEulerAngles.y, playerCamera.transform.localEulerAngles.z);
                            transform.localEulerAngles = new Vector3(transform.localEulerAngles.x, cameraAngle.y, transform.localEulerAngles.z);
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
                            switch (clickMoveType)
                            {
                                case ClickMoveType.Freewalk:
                                    if (Vector2.Distance(initialMousePosition, lastMousePosition) < 0.1f)
                                    {
                                        // カメラ位置を更新
                                        cameraPosition += new Vector3(ray.direction.x, 0f, ray.direction.z) * moveRate;
                                    }
                                    break;

                                case ClickMoveType.ToClosestAutoMovePoint:

                                    if (Vector2.Distance(initialMousePosition, lastMousePosition) < 0.1f && autoMovePointObjects.Count > 0)
                                    {
                                        Vector3 detectOrigin = transform.position + ray.direction * closestAutoMovePointDetectOriginDistance;

                                        GameObject target = null;
                                        float minDistance = 100f;

                                        // 登録されたオブジェクトとの距離を計測
                                        foreach (GameObject obj in autoMovePointObjects)
                                        {
                                            // 最後に移動した地点のオブジェクトは飛ばす
                                            if (lastClosestPointObject != null && obj == lastClosestPointObject)
                                            {
                                                continue;
                                            }

                                            // オブジェクトまでの距離を計算
                                            float dist = Vector3.Distance(detectOrigin, obj.transform.position);

                                            // 距離が近く、カメラの前方にあればターゲット判定する
                                            if (dist < minDistance && Vector3.Dot((obj.transform.position - transform.position).normalized, transform.forward) > forwardClosestAutoMovePointDetectRange)
                                            {
                                                minDistance = dist;
                                                target = obj;
                                            }
                                        }

                                        if (target != null)
                                        {
                                            // 移動ターゲットを更新
                                            lastClosestPointObject = target;

                                            // 状態をMoveToPointに更新する
                                            controlState = ControlState.MoveToPoint;

                                            // カメラ位置を更新
                                            cameraPosition = new Vector3(target.transform.position.x, transform.position.y, target.transform.position.z);

                                            // 移動方向を向く
                                            if (lookAtMovePointOnMove)
                                            {
                                                // 目的の方向へ向くための回転を計算
                                                Vector3 targetRot = Quaternion.LookRotation(target.transform.position - transform.position, Vector3.up).eulerAngles;

                                                // 現在の姿勢を取得
                                                Vector3 currentRot = transform.rotation.eulerAngles;

                                                // Y軸のみを回転するようにする
                                                cameraPose = Quaternion.Euler(currentRot.x, targetRot.y, currentRot.z);
                                            }
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
                                if (hitTransform != null)
                                {
                                    // カメラ位置を更新
                                    cameraPosition = new Vector3(hitTransform.position.x, transform.position.y, hitTransform.position.z);

                                    // 移動方向を向く
                                    if (lookAtMovePointOnMove)
                                    {
                                        // 目的の方向へ向くための回転を計算
                                        Vector3 targetRot = Quaternion.LookRotation(hitTransform.position - transform.position, Vector3.up).eulerAngles;

                                        // 現在の姿勢を取得
                                        Vector3 currentRot = transform.rotation.eulerAngles;

                                        // Y軸のみを回転するようにする
                                        cameraPose = Quaternion.Euler(currentRot.x, targetRot.y, currentRot.z);
                                    }

                                    MovePointEventInvoker eventInvoker = hitTransform.GetComponent<MovePointEventInvoker>();

                                    if(eventInvoker != null)
                                    {
                                        eventInvoker.Invoke();
                                    }
                                }
                            }
                            else
                            {
                                controlState = ControlState.None;
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
            }

            // 地面(下側の当たり判定付きオブジェクト)に立てるようにする
            if (enableGroundStanding)
            {
                // カメラの下方向に向けてRayを照射
                Ray rayToGround = new Ray(transform.position, -Vector3.up);

                // イベントオブジェクトが足元に来ることを考慮してすべてのオブジェクトに対してRayを飛ばす
                RaycastHit[] groundHits = Physics.RaycastAll(rayToGround);

                if(groundHits.Length > 0)
                {
                    float firstGroundDistance = 100f;
                    bool groundDetect = false;

                    // イベントオブジェクトでないオブジェクトまでの距離を測定し一番近くにあるものを地面とみなす
                    foreach (RaycastHit hit in groundHits)
                    {
                        if (CheckObjectIsNotEventObject(hit.transform) && hit.distance < firstGroundDistance)
                        {
                            firstGroundDistance = hit.distance;
                            groundDetect = true;
                        }
                    }

                    if (groundDetect)
                    {
                        // 激しくバウンドするのを防ぐため誤差値を考慮して挙動を制御
                        if (firstGroundDistance > heightFromGround + groundHeightTolerance)
                        {
                            // 地面から目標高さより2倍以上浮いている場合は重力による自由落下
                            if (firstGroundDistance > heightFromGround * 2f)
                            {
                                cameraPosition.y += -9.8f * Time.deltaTime;
                            }
                            // 地面から目標高さより2倍以下の場合は距離と調整倍率により高さを制御
                            else
                            {
                                cameraPosition.y += (heightFromGround - firstGroundDistance) * upLiftScale;
                            }                            
                        }
                        else if (firstGroundDistance < heightFromGround - groundHeightTolerance)
                        {
                            // 地面にめり込んでいる場合はめり込まないように位置を調整
                            // いきなり飛び出した感じにならないように倍率調整する
                            cameraPosition.y += (heightFromGround - firstGroundDistance) * upLiftScale;
                        }
                    }
                }
                else
                {
                    // stayHeightOnNoGround = trueのときは地面がない場所で無限に落下しないように高さ座標を固定
                    if (!stayHeightOnNoGround)
                    {
                        cameraPosition.y += -9.8f * Time.deltaTime;
                    }
                }

            }


            // カメラの位置・姿勢を更新
            if (Vector3.Distance(transform.position, cameraPosition) > 0.01f)
            {
                // 位置を更新
                // 移動ポイントへ移動するとき以外は移動後に他のオブジェクトに衝突するかどうかをチェックし、衝突する場合は現在の場所を目的地にする
                if (controlState != ControlState.MoveToPoint && enableCollider && CheckPlayerIsGoingToCollide(cameraPosition - transform.position, true))
                {
                    cameraPosition = transform.position;
                }

                transform.position = Vector3.Lerp(transform.position, cameraPosition, moveSmoothing);

                // 姿勢を更新
                if (controlState == ControlState.MoveToPoint && lookAtMovePointOnMove)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, cameraPose, 0.1f);
                }

                // 目的地に到着したら
                if (Vector3.Distance(transform.position, cameraPosition) <= 0.01f)
                {
                    transform.position = cameraPosition;

                    // 移動完了前にクリックしていた場合に状態が切り替わらないため、ここでCameraControlに状態変更する
                    if (controlState == ControlState.MoveToPoint)
                    {
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
                    camFOV = playerCamera.fieldOfView - touchDistanceDelta * fovPinchRate;
                }
                else
                {
                    camFOV = playerCamera.fieldOfView + Input.GetAxis("Mouse ScrollWheel") * fovRate;
                }

                if (camFOV >= fovMin && camFOV <= fovMax)
                {
                    playerCamera.fieldOfView = camFOV;
                }
            }
        }
    }

    /*
    private void OnTriggerEnter(Collider other)
    {

    }*/

    private void OnTriggerStay(Collider other)
    {
        // MovePoint等のイベント発生用オブジェクト以外のときは当たり判定をとり、物体をすり抜けないようにする
        if (enableCollider && controlState != ControlState.MoveToPoint && CheckObjectIsNotEventObject(other.gameObject.transform))
        {
            isBounded = true;

            // 跳ね返り方向を計算
            Vector3 boundDirection = (transform.position - new Vector3(other.gameObject.transform.position.x, transform.position.y, other.gameObject.transform.position.z)).normalized;

            // 目的地を衝突したものから離れるように設定
            cameraPosition += boundDirection * wallBoundValue;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (enableCollider && CheckObjectIsNotEventObject(other.gameObject.transform))
        {
            isBounded = false;
        }
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 次の移動時にオブジェクトにぶつかる場合は色を変更する
        if (CheckPlayerIsGoingToCollide(nextMoveDirection, false))
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.blue;
        }

        switch (playerColliderType)
        {
            case ColliderType.Sphere:

                // 次の移動位置を表示
                Gizmos.DrawWireSphere(transform.position + transform.rotation * colliderCenter + nextMoveDirection * moveRate, colliderRadius);
                break;

            case ColliderType.Capsule:

                // 次の移動位置を表示
                Gizmos.DrawWireSphere(transform.position + transform.rotation * (colliderCenter + capsuleSpherePos) + nextMoveDirection * moveRate, colliderRadius);
                Gizmos.DrawWireSphere(transform.position + transform.rotation * (colliderCenter - capsuleSpherePos) + nextMoveDirection * moveRate, colliderRadius);
                break;

            default:
                break;
        }
    }
    #endif
    #endregion

    #region Public Methods
    public void ResetState()
    {
        controlState = ControlState.None;
        hitTransform = null;
        currentPopupEvent = null;
    }

    public void WarpTo(Vector3 position)
    {
        if (playerCamera != null)
        {
            transform.position = position;
            cameraPosition = position;
        }
    }

    public void MoveTo(Vector3 position)
    {
        cameraPosition = position;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// オブジェクトのタグをチェックしイベント発生用オブジェクトかどうか調べる
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    private bool CheckObjectIsNotEventObject(Transform target)
    {
        if(target.CompareTag(movePointObjectTag) || target.CompareTag(popupEventObjectTag))
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// プレイヤーオブジェクトが他のオブジェクトに衝突するかどうかを調べる
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    private bool CheckPlayerIsGoingToCollide(Vector3 direction, bool considerSmoothing)
    {
        RaycastHit hit;
        bool hitResult;

        switch (playerColliderType)
        {
            case ColliderType.Sphere:

                hitResult = Physics.SphereCast(transform.position + transform.rotation * colliderCenter,
                                               colliderRadius,
                                               direction,
                                               out hit,
                                               considerSmoothing ? moveRate * moveSmoothing : moveRate);
                break;

            case ColliderType.Capsule:

                hitResult = Physics.CapsuleCast(transform.position + transform.rotation * colliderCenter + transform.rotation * capsuleSpherePos,
                                                transform.position + transform.rotation * colliderCenter - transform.rotation * capsuleSpherePos,
                                                colliderRadius,
                                                direction,
                                                out hit,
                                                considerSmoothing ? moveRate * moveSmoothing : moveRate);
                break;

            default:
                return false;
        }

        // 衝突したものがイベントオブジェクトの場合は無視
        if (hit.transform != null && !CheckObjectIsNotEventObject(hit.transform))
        {
            return false;
        }
        else
        {
            return hitResult;
        }
    }
    #endregion
}
