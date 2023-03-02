using UnityEngine;

public class UkatonMissionPressureListener : MonoBehaviour
{
  public UkatonMissionMonoBehavior ukatonMissionMonoBehavior;
  private Transform sensors;

  void Start()
  {
    ukatonMissionMonoBehavior.pressureDataEvents.pressure.AddListener(onPressureUpdate);
    sensors = transform.Find("sensors");
  }

  public void onPressureUpdate()
  {
    for (int i = 0; i < UkatonMission.PressureData.numberOfPressureSensors; i++)
    {
      double value = ukatonMissionMonoBehavior.ukatonMission.pressureData.pressure[i];
      Transform sensor = sensors.GetChild(i);
      MeshRenderer renderer = sensor.GetComponent<MeshRenderer>();
      Color tempColor = renderer.material.color;
      tempColor.a = (float)value;
      renderer.material.color = tempColor;
    }
  }
}
