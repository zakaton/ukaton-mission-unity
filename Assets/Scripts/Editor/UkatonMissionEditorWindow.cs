using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class UkatonMissionEditorWindow : EditorWindow
{
  private UkatonMissionUDP ukatonMission = new();

  private int[] selectionIDs;
  private List<int> idsToOrbit = new();
  private int numberOfIdsToOrbit = 0;
  private bool isConnected = false;
  private bool isConnecting = false;

  private bool shouldOrbitCamera = false;
  private bool shouldEnableQuaternion = false;

  // Add menu item named "Ukaton Mission" to the Window menu
  [MenuItem("Window/Ukaton Mission")]
  public static void ShowWindow()
  {
    //Show existing window instance. If one doesn't exist, make one.
    EditorWindow editorWindow = EditorWindow.GetWindow(typeof(UkatonMissionEditorWindow), true, "Ukaton Mission");
  }

  void Awake()
  {
    ukatonMission.Start();
    ukatonMission.connectionEvents.onConnect.AddListener(onConnect);
    ukatonMission.connectionEvents.onDisconnect.AddListener(onDisconnect);
    ukatonMission.connectionEvents.onConnecting.AddListener(onConnecting);
    ukatonMission.connectionEvents.onStopConnecting.AddListener(onStopConnecting);
    ukatonMission.motionDataEvents.quaternion.AddListener(onQuaternion);
  }

  // reference
  void resetCameraView()
  {
    var sceneView = UnityEditor.SceneView.lastActiveSceneView;
    sceneView.rotation = Quaternion.Euler(0, 0, 0);
    sceneView.Repaint();
  }

  void setCameraPivot()
  {
    var selectedGameObject = Selection.activeGameObject;
    if (selectedGameObject != null)
    {
      var sceneView = UnityEditor.SceneView.lastActiveSceneView;
      sceneView.pivot = selectedGameObject.transform.position;
      sceneView.Repaint();
    }

  }

  void onConnect()
  {
    isConnecting = false;
    isConnected = true;
    if (shouldEnableQuaternion)
    {
      enableQuaternion();
    }
  }
  void onDisconnect()
  {
    isConnected = false;
    isConnecting = false;
  }
  void onConnecting()
  {
    isConnecting = true;
  }
  void onStopConnecting()
  {
    isConnecting = false;
  }

  void enableQuaternion()
  {
    ukatonMission.motionSensorDataRates.quaternion = UkatonMission.SensorDataRate._10;
    ukatonMission.UpdateSensorDataConfiguration();
  }
  void disableQuaternion()
  {
    ukatonMission.motionSensorDataRates.quaternion = UkatonMission.SensorDataRate._0;
    ukatonMission.UpdateSensorDataConfiguration();
  }

  void onQuaternion()
  {
    if (shouldOrbitCamera)
    {
      var sceneView = UnityEditor.SceneView.lastActiveSceneView;
      sceneView.rotation = ukatonMission.motionData.quaternion;
      sceneView.Repaint();
    }
    else
    {
      var selectedGameObject = Selection.activeGameObject;
      if (idsToOrbit.Contains(selectedGameObject.GetInstanceID()))
      {
        selectedGameObject.transform.rotation = ukatonMission.motionData.quaternion;
      }
    }
  }

  private void OnDestroy()
  {
    ukatonMission.Disconnect();
    ukatonMission.Update();
  }

  void OnGUI()
  {
    GUILayout.Label("Device Information", EditorStyles.boldLabel);
    ukatonMission.deviceName = EditorGUILayout.TextField("Device Name", ukatonMission.deviceName);
    ukatonMission.enableLogging = EditorGUILayout.Toggle("enable debugging", ukatonMission.enableLogging);
    if (isConnected)
    {
      bool _shouldEnableQuaternion = EditorGUILayout.Toggle("enable quaternion", shouldEnableQuaternion);
      if (shouldEnableQuaternion != _shouldEnableQuaternion)
      {
        shouldEnableQuaternion = _shouldEnableQuaternion;
        if (shouldEnableQuaternion)
        {
          enableQuaternion();
        }
        else
        {
          disableQuaternion();
        }
      }
    }

    if (GUILayout.Button("resetCameraView"))
    {
      resetCameraView();
    }


    if (isConnected)
    {
      if (GUILayout.Button("disconnect"))
      {
        ukatonMission.Disconnect();
      }
    }
    else
    {
      if (isConnecting)
      {
        if (GUILayout.Button("connecting..."))
        {
          ukatonMission.Disconnect();
        }
      }
      else
      {
        if (GUILayout.Button("connect"))
        {
          ukatonMission.Connect();
        }
      }
    }

    GUILayout.Label("Object Selection", EditorStyles.boldLabel);
    if (selectionIDs != null)
    {
      if (GUILayout.Button("rotate object"))
      {
        foreach (var id in selectionIDs)
        {
          if (!idsToOrbit.Contains(id))
          {
            numberOfIdsToOrbit++;
            idsToOrbit.Add(id);
            Debug.Log(String.Format("adding {0} [{1}]", id, numberOfIdsToOrbit));
          }
        }
      }
      if (GUILayout.Button("stop rotating object"))
      {
        foreach (var id in selectionIDs)
        {
          if (idsToOrbit.Contains(id))
          {
            numberOfIdsToOrbit--;
            idsToOrbit.Remove(id);
            Debug.Log(String.Format("removing {0} [{1}]", id, numberOfIdsToOrbit));
          }
        }
      }
    }
    if (GUILayout.Button("clear"))
    {
      Debug.Log("clearing all");
      idsToOrbit.Clear();
      numberOfIdsToOrbit = 0;
    }

    GUILayout.Label("Camera", EditorStyles.boldLabel);
    shouldOrbitCamera = EditorGUILayout.Toggle("orbit camera", shouldOrbitCamera);

    if (GUILayout.Button("set camera pivot"))
    {
      setCameraPivot();
    }
  }

  void OnSelectionChange()
  {
    selectionIDs = Selection.instanceIDs;
  }

  void Update()
  {
    ukatonMission.Update();
  }
}