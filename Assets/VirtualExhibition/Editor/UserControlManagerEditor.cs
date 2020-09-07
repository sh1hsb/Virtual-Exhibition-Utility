// **************************************************
//
//  Copyright (c) 2020 Shinichi Hasebe
//  This software is released under the MIT License.
//  http://opensource.org/licenses/mit-license.php
//
// **************************************************

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UserControlManager))]
public class UserControlManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var userControlManager = target as UserControlManager;

        if (GUILayout.Button("Register Tags"))
        {
            userControlManager.RegisterTags(false);
        }
    }
}
