using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System;

[CustomEditor(typeof(UkatonMission))]
public class UkatonMission_Inspector : Editor
{
  public VisualTreeAsset m_InspectorXML;

  Button connectButton;
  VisualElement insoleSideElement;
  VisualElement pressureDataRatesElement;
  VisualElement pressureDataEventsElement;

  UkatonMission ukatonMission;
  public override VisualElement CreateInspectorGUI()
  {

    ukatonMission = serializedObject.targetObject as UkatonMission;

    // Create a new VisualElement to be the root of our inspector UI
    VisualElement myInspector = new VisualElement();

    // Load from default reference
    m_InspectorXML.CloneTree(myInspector);

    // Whenever any serialized property on this serialized object changes its value, call CheckForWarnings.
    myInspector.TrackPropertyValue(serializedObject.FindProperty("deviceName"), onDeviceNameUpdate);
    myInspector.TrackPropertyValue(serializedObject.FindProperty("autoConnect"), onAutoConnectUpdate);
    myInspector.TrackPropertyValue(serializedObject.FindProperty("isInsole"), onIsInsoleUpdate);
    myInspector.TrackPropertyValue(serializedObject.FindProperty("insoleSide"), onInsoleSideUpdate);

    connectButton = myInspector.Q("connectButton") as Button;
    connectButton.clicked += onConnectButtonUpdate;
    connectButton.SetEnabled(EditorApplication.isPlaying);

    foreach (UkatonMission.MotionDataType motionDataType in Enum.GetValues(typeof(UkatonMission.MotionDataType)))
    {
      myInspector.TrackPropertyValue(serializedObject.FindProperty(String.Format("motionSensorDataRates.{0}", motionDataType.ToString())), onMotionDataRateUpdate);
    }

    foreach (UkatonMission.PressureDataType pressureDataType in Enum.GetValues(typeof(UkatonMission.PressureDataType)))
    {
      myInspector.TrackPropertyValue(serializedObject.FindProperty(String.Format("pressureSensorDataRates.{0}", pressureDataType.ToString())), onPressureDataRateUpdate);
    }

    myInspector.TrackPropertyValue(serializedObject.FindProperty("enableLogging"), onEnableLoggingUpdate);


    ukatonMission.connectionEvents.onConnect.AddListener(onConnect);
    ukatonMission.connectionEvents.onConnecting.AddListener(onConnecting);

    insoleSideElement = myInspector.Q("insoleSideElement");
    pressureDataRatesElement = myInspector.Q("pressureDataRatesElement");
    pressureDataEventsElement = myInspector.Q("pressureDataEventsElement");

    updateInsoleUI();

    // Return the finished inspector UI
    return myInspector;
  }

  void updateInsoleUI()
  {
    insoleSideElement.style.display = ukatonMission.isInsole ? DisplayStyle.Flex : DisplayStyle.None;
    pressureDataRatesElement.style.display = ukatonMission.isInsole ? DisplayStyle.Flex : DisplayStyle.None;
    pressureDataEventsElement.style.display = ukatonMission.isInsole ? DisplayStyle.Flex : DisplayStyle.None;
  }

  void onDeviceNameUpdate(SerializedProperty serializedProperty)
  {
    //Debug.Log(ukatonMission.deviceName);
  }
  void onAutoConnectUpdate(SerializedProperty serializedProperty)
  {
    //Debug.Log(ukatonMission.deviceName);
  }
  void onIsInsoleUpdate(SerializedProperty serializedProperty)
  {
    updateInsoleUI();
    //Debug.Log(ukatonMission.isInsole);
  }
  void onInsoleSideUpdate(SerializedProperty serializedProperty)
  {
    //Debug.Log(ukatonMission.insoleSide);
  }
  void onConnectButtonUpdate()
  {
    if (EditorApplication.isPlaying)
    {

      ukatonMission.Connect();
    }
  }
  void onConnect()
  {
    connectButton.text = "connected";
    connectButton.SetEnabled(false);
  }
  void onConnecting()
  {
    connectButton.SetEnabled(false);
    connectButton.text = "connecting...";
  }


  void onMotionDataRateUpdate(SerializedProperty serializedProperty)
  {
    //Debug.Log(serializedProperty.name);
    ukatonMission.updateSensorDataConfiguration();
  }

  void onPressureDataRateUpdate(SerializedProperty serializedProperty)
  {
    //Debug.Log(serializedProperty.name);
    ukatonMission.updateSensorDataConfiguration();
  }

  void onEnableLoggingUpdate(SerializedProperty serializedProperty)
  {
    //Debug.Log(serializedProperty.name);
    ukatonMission.UpdateLogging();
  }
}