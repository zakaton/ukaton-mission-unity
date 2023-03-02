using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class UkatonMissions
{
  [Header("Insoles")]
  [SerializeField]
  private UkatonMissionMonoBehavior leftInsole;

  [SerializeField]
  private UkatonMissionMonoBehavior rightInsole;

  public class PressureData
  {
    public double sum;
    public Dictionary<UkatonMission.InsoleSide, double> mass = new();
    public Vector2 centerOfMass;
  }
  public PressureData pressureData = new();

  void Start()
  {
    leftInsole.pressureDataEvents.pressure.AddListener(updatePressureData);
    rightInsole.pressureDataEvents.pressure.AddListener(updatePressureData);
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
    pressureData.sum = leftInsole.ukatonMission.pressureData.sum + rightInsole.ukatonMission.pressureData.sum;

    if (pressureData.sum > 0)
    {
      pressureData.mass[UkatonMission.InsoleSide.left] = leftInsole.ukatonMission.pressureData.sum / pressureData.sum;
      pressureData.mass[UkatonMission.InsoleSide.right] = rightInsole.ukatonMission.pressureData.sum / pressureData.sum;

      pressureData.centerOfMass.x = (float)pressureData.mass[UkatonMission.InsoleSide.right];
      pressureData.centerOfMass.y = leftInsole.ukatonMission.pressureData.centerOfMass.y * (float)pressureData.mass[UkatonMission.InsoleSide.left] + rightInsole.ukatonMission.pressureData.centerOfMass.y * (float)pressureData.mass[UkatonMission.InsoleSide.right];

      Debug.Log(pressureData.centerOfMass);

      pressureDataEvents.mass.Invoke();
      pressureDataEvents.centerOfMass.Invoke();
    }
  }
}
