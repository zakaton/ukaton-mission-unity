using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class UkatonMissions : MonoBehaviour
{
  [Header("Insoles")]
  [SerializeField]
  private UkatonMission leftMission;

  [SerializeField]
  private UkatonMission rightMission;

  public class PressureData
  {
    public double sum;
    public Dictionary<UkatonMission.InsoleSide, double> mass = new();
    public Vector2 centerOfMass;
  }
  public PressureData pressureData = new();

  void Start()
  {
    leftMission.pressureDataEvents.pressure.AddListener(updatePressureData);
    rightMission.pressureDataEvents.pressure.AddListener(updatePressureData);
  }

  [Serializable]
  public class PressureDataEvents
  {
    public UnityEvent centerOfMass;
    public UnityEvent mass;
  }

  [Header("Events")]
  public PressureDataEvents pressureDataEvents;

  public void updatePressureData()
  {
    pressureData.sum = leftMission.pressureData.sum + rightMission.pressureData.sum;

    if (pressureData.sum > 0)
    {
      pressureData.mass[UkatonMission.InsoleSide.left] = leftMission.pressureData.sum / pressureData.sum;
      pressureData.mass[UkatonMission.InsoleSide.right] = rightMission.pressureData.sum / pressureData.sum;

      pressureData.centerOfMass.x = (float)pressureData.mass[UkatonMission.InsoleSide.right];
      pressureData.centerOfMass.y = leftMission.pressureData.centerOfMass.y * (float)pressureData.mass[UkatonMission.InsoleSide.left] + rightMission.pressureData.centerOfMass.y * (float)pressureData.mass[UkatonMission.InsoleSide.right];

      Debug.Log(pressureData.centerOfMass);

      pressureDataEvents.mass.Invoke();
      pressureDataEvents.centerOfMass.Invoke();
    }
  }
}
