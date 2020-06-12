// **************************************************
//
//  Copyright (c) 2020 Shinichi Hasebe
//  This software is released under the MIT License.
//  http://opensource.org/licenses/mit-license.php
//
// **************************************************

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class OpenURLProvider : MonoBehaviour
{
    public string url;

    public void Open()
    {
        if(url != null && url != "")
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            //Application.OpenURL(url);
            OpenToBlankWindow(url);
            #else
            Application.OpenURL(url);
            #endif
        }
    }

    [DllImport("__Internal")]
    private static extern void OpenToBlankWindow(string _url);
}
