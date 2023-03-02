using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;
using UnityEngine;

[CustomEditor(typeof(UkatonMissionMonoBehavior))]
public class UkatonMissionMonoBehavior_Inspector : Editor
{
  public VisualTreeAsset m_InspectorXML;

  Button connectButton;
  VisualElement insoleSideElement;
  VisualElement pressureDataRatesElement;
  VisualElement pressureDataEventsElement;

  UkatonMissionMonoBehavior ukatonMissionMonoBehavior;
  UkatonMission ukatonMission { get { return ukatonMissionMonoBehavior.ukatonMission; } }

  UkatonMissionBLE ble { get { return ukatonMissionMonoBehavior.ukatonMissionBLE; } }
  UkatonMissionUDP udp { get { return ukatonMissionMonoBehavior.ukatonMissionUDP; } }
  UkatonMission other
  {
    get
    {
      return ukatonMission == ble ? udp : ble;
    }
  }

  public override void OnInspectorGUI()
  {
    ukatonMissionMonoBehavior = serializedObject.targetObject as UkatonMissionMonoBehavior;

    GUILayout.Label("Device Information", EditorStyles.boldLabel);

    GUILayout.BeginHorizontal();
    EditorGUILayout.PrefixLabel("connection type");
    ukatonMissionMonoBehavior.connectionType = (UkatonMissionMonoBehavior.ConnectionType)EditorGUILayout.EnumPopup(ukatonMissionMonoBehavior.connectionType);
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    EditorGUILayout.PrefixLabel("enable logging?");
    ble.enableLogging = udp.enableLogging = EditorGUILayout.Toggle(ukatonMission.enableLogging);
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    EditorGUILayout.PrefixLabel("device name");
    ble.deviceName = udp.deviceName = GUILayout.TextField(ukatonMission.deviceName);
    GUILayout.EndHorizontal();

    if (ukatonMission == udp)
    {
      GUILayout.BeginHorizontal();
      EditorGUILayout.PrefixLabel("ip address");
      udp.address = GUILayout.TextField(udp.address);
      GUILayout.EndHorizontal();
    }

    GUILayout.BeginHorizontal();
    EditorGUILayout.PrefixLabel("auto connect?");
    ble.autoConnect = udp.autoConnect = EditorGUILayout.Toggle(ukatonMission.autoConnect);
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    EditorGUILayout.PrefixLabel("is insole?");
    ble.isInsole = udp.isInsole = EditorGUILayout.Toggle(ukatonMission.isInsole);
    GUILayout.EndHorizontal();

    if (ukatonMission.isInsole)
    {
      GUILayout.BeginHorizontal();
      GUILayout.Label("connection type");
      ble.insoleSide = udp.insoleSide = (UkatonMission.InsoleSide)EditorGUILayout.EnumPopup(ukatonMission.insoleSide);
      GUILayout.EndHorizontal();
    }

    GUILayout.BeginHorizontal();
    GUI.enabled = EditorApplication.isPlaying;
    string buttonText = "connect";
    if (ukatonMission.IsConnected)
    {
      buttonText = "disconnect";
    }
    else if (ukatonMission.IsConnecting)
    {
      buttonText = "connecting";
    }
    bool toggleConnection = GUILayout.Button(buttonText);
    if (toggleConnection)
    {
      if (!ukatonMission.IsConnected)
      {
        ukatonMission.Connect();
      }
      else
      {
        ukatonMission.Disconnect();
      }
    }
    GUI.enabled = true;
    GUILayout.EndHorizontal();

    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    GUILayout.Label("Sensor Data Configuration", EditorStyles.boldLabel);

    EditorGUI.BeginChangeCheck();
    EditorGUILayout.PropertyField(serializedObject.FindProperty("motionSensorDataRates"));
    if (ukatonMission.isInsole)
    {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("pressureSensorDataRates"));
    }
    if (EditorApplication.isPlaying && EditorGUI.EndChangeCheck())
    {
      ukatonMission.UpdateSensorDataConfiguration();
    }

    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
    GUILayout.Label("Events", EditorStyles.boldLabel);

    EditorGUILayout.PropertyField(serializedObject.FindProperty("motionDataEvents"));
    if (ukatonMission.isInsole)
    {
      EditorGUILayout.PropertyField(serializedObject.FindProperty("pressureDataEvents"));
    }

    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

    serializedObject.ApplyModifiedProperties();
  }
}