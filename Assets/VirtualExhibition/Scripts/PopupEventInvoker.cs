// **************************************************
//
//  Copyright (c) 2020 Shinichi Hasebe
//  This software is released under the MIT License.
//  http://opensource.org/licenses/mit-license.php
//
// **************************************************

using UnityEngine;
using UnityEngine.Events;

public class PopupEventInvoker : MonoBehaviour
{
    public enum MainUIPopupPosition
    {
        ObjectPoint,
        ScreenCenter
    }

    public UserControlManager cameraController;

    public GameObject mainPopupUI;
    public MainUIPopupPosition popupPosition;
    public int edgeOffset = 10;

    public GameObject[] AppearObjects;

    public UnityEvent onPopup;
    public UnityEvent onDisappear;

    private Vector2 screenCenter;
    private Canvas mainUICanvas;
    private RectTransform mainUIRectTransform;

    void Start()
    {
        // オブジェクトを非表示
        Disappear();

        // メインUIのRectTransformを取得
        mainUIRectTransform = mainPopupUI.GetComponent<RectTransform>();

        // メインUIのCanvasを取得
        mainUICanvas = mainPopupUI.transform.parent.GetComponent<Canvas>();
    }

    public void Popup()
    {
        if(mainPopupUI != null)
        {
            mainPopupUI.SetActive(true);

            //var screenPos = RectTransformUtility.WorldToScreenPoint(cameraController.targetCamera, this.transform.position);//スクリーン座標
            //var viewportPos = cameraController.targetCamera.WorldToViewportPoint(this.transform.position);//ビューポート座標

            // スクリーンセンターを取得
            screenCenter = new Vector2(Screen.width / 2, Screen.height / 2);

            switch (popupPosition)
            {
                case MainUIPopupPosition.ScreenCenter:
                    
                    // スクリーンセンターに移動
                    if (mainUIRectTransform != null) mainUIRectTransform.position = screenCenter;
                    break;

                case MainUIPopupPosition.ObjectPoint:
                    
                    if (mainUIRectTransform != null) 
                    {
                        // スクリーン座標を取得
                        var screenPos = RectTransformUtility.WorldToScreenPoint(cameraController.playerCamera, this.transform.position);

                        // UIがスクリーンからはみ出ないように位置調整
                        if (screenPos.x + mainUIRectTransform.sizeDelta.x * mainUICanvas.scaleFactor / 2f > Screen.width)
                        {
                            screenPos.x = Screen.width - mainUIRectTransform.sizeDelta.x * mainUICanvas.scaleFactor / 2f - edgeOffset;
                        }
                        else if(screenPos.x - mainUIRectTransform.sizeDelta.x * mainUICanvas.scaleFactor / 2f < 0)
                        {
                            screenPos.x = mainUIRectTransform.sizeDelta.x * mainUICanvas.scaleFactor / 2f + edgeOffset;
                        }

                        if (screenPos.y + mainUIRectTransform.sizeDelta.y * mainUICanvas.scaleFactor / 2f > Screen.height)
                        {
                            screenPos.y = Screen.height - mainUIRectTransform.sizeDelta.y * mainUICanvas.scaleFactor / 2f - edgeOffset;
                        }
                        else if (screenPos.y - mainUIRectTransform.sizeDelta.y * mainUICanvas.scaleFactor / 2f < 0)
                        {
                            screenPos.y = mainUIRectTransform.sizeDelta.y * mainUICanvas.scaleFactor / 2f + edgeOffset;
                        }

                        // 位置を更新
                        mainUIRectTransform.position = screenPos;
                    }
                    break;

                default:
                    break;
            }
        }

        foreach (GameObject obj in AppearObjects)
        {
            obj.SetActive(true);
        }

        onPopup?.Invoke();
    }

    public void Disappear()
    {
        if (mainPopupUI != null)
        {
            mainPopupUI.SetActive(false);
        }

        foreach (GameObject obj in AppearObjects)
        {
            obj.SetActive(false);
        }

        onDisappear?.Invoke();
    }
}
