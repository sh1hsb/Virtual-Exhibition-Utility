# Virtual Exhibition Utility for Unity

## Overview
Utility for creating virtual exhibition applications (like first person shooter games).  
A character controller that we can control by mouse/tap only is implemented.

## How to use
1. Download UnityPackage from [Release Page](https://github.com/sh1hsb/Virtual-Exhibition-Utility/releases).
2. Import UnityPackage to your project.
3. Add "Player" prefab (`VirtualExhibition/Prefabs/Player.prefab`) to scene.
4. If other cameras are already present, remove or disable GameObject or Camera component.
5. Edit ”UserControlManager” component in "Player" GameObject to match your content specifications. 
6. If you want the UI to popup by clicking in a specific position, place Popup event object (sample prefab → `VirtualExhibition/Prefabs/Exclamation_Popup.prefab`) to scene and setup .
7. If you want the player to move to the position you clicked on, place Move point object (sample prefab → `VirtualExhibition/Prefabs/MovePoint_xxx.prefab`) to scene and setup.
8. For more information on configuration, check example scene (If you want to check example scene, you need to install TextMesh Pro package and TMP Essencial Resources.).

## How to play
* Click/Tap ... Move(in Freewalk mode), Event invoke (move to point, popup etc...)
* Drag&Drop/Swipe ... Camera rotation, Object control
* Mouse wheel/Pinch ... Camera zoom
