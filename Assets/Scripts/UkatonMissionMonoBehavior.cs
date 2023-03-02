using UnityEngine;

public class UkatonMissionMonoBehavior : MonoBehaviour
{
  public enum ConnectionType
  {
    Bluetooth,
    Wifi
  }
  public ConnectionType connectionType = ConnectionType.Bluetooth;

  [SerializeField]
  public UkatonMissionBLE ukatonMissionBLE;
  [SerializeField]
  public UkatonMissionUDP ukatonMissionUDP;
  [SerializeField]
  public UkatonMission ukatonMission
  {
    get
    {
      switch (connectionType)
      {
        case ConnectionType.Bluetooth:
          return ukatonMissionBLE;
        case ConnectionType.Wifi:
          return ukatonMissionUDP;
        default:
          return ukatonMissionBLE;
      }
    }
  }

  public UkatonMission.MotionDataEvents motionDataEvents = new();
  public UkatonMission.PressureDataEvents pressureDataEvents = new();
  public UkatonMission.MotionSensorDataRates motionSensorDataRates = new();
  public UkatonMission.PressureSensorDataRates pressureSensorDataRates = new();

  // Start is called before the first frame update
  void Start()
  {
    ukatonMissionBLE.motionDataEvents = ukatonMissionUDP.motionDataEvents = motionDataEvents;
    ukatonMissionBLE.pressureDataEvents = ukatonMissionUDP.pressureDataEvents = pressureDataEvents;

    ukatonMissionBLE.motionSensorDataRates = ukatonMissionUDP.motionSensorDataRates = motionSensorDataRates;
    ukatonMissionBLE.pressureSensorDataRates = ukatonMissionUDP.pressureSensorDataRates = pressureSensorDataRates;

    ukatonMission.Start();
  }

  // Update is called once per frame
  void Update()
  {
    ukatonMission.Update();
  }

  private void OnDestroy()
  {
    ukatonMissionUDP.connection.Stop();
  }
  private void OnApplicationQuit()
  {
    ukatonMissionUDP.connection.Stop();
  }

  public void Connect()
  {
    ukatonMission.Connect();
  }
}
