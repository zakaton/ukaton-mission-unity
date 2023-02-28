using UnityEngine;
using System;
using System.Linq;

/*
    TODO
        discovery ping
*/

public class UkatonMissionUDP : MonoBehaviour
{
  [SerializeField]
  private string address = "0.0.0.0";

  private UdpConnection connection;

  [SerializeField]
  public UkatonMissionBaseClass ukatonMission = new();
  enum MessageType
  {
    PING,

    BATTERY_LEVEL,

    GET_TYPE,
    SET_TYPE,

    GET_NAME,
    SET_NAME,

    MOTION_CALIBRATION,

    GET_SENSOR_DATA_CONFIGURATIONS,
    SET_SENSOR_DATA_CONFIGURATIONS,

    SENSOR_DATA
  }

  private float LastPingTime = 0;
  private float LastTimeReceivedData = 0;

  void Start()
  {
    ukatonMission.Start();
    ukatonMission.connectionEvents.onConnect.AddListener(OnConnect);

    connection = new UdpConnection();
    connection.StartConnection(address, 9999, 11000);
  }


  void OnApplicationQuit()
  {
    connection.Stop();
  }

  void OnDestroy()
  {
    connection.Stop();
  }

  void Update()
  {
    var messages = connection.getMessages();
    if (messages.Length > 0)
    {
      foreach (var message in messages)
      {
        ProcessData(message);
      }
      if (LastTimeReceivedData == 0)
      {
        ukatonMission.connectionEvents.onConnect.Invoke();
      }
      LastTimeReceivedData = Time.time;
    }

    if (Time.time - LastPingTime > 1)
    {
      Ping();
    }
  }

  void ProcessData(byte[] bytes)
  {
    var messageType = (MessageType)bytes[0];
    switch (messageType)
    {
      case MessageType.SENSOR_DATA:
        var segment = new ArraySegment<byte>(bytes, 1, bytes.Length - 1).ToArray();
        ukatonMission.ProcessSensorData(segment);
        break;
      case MessageType.BATTERY_LEVEL:
        //var batteryLevel = bytes[1];
        //Debug.Log($"battery level: {batteryLevel}");
        break;
      default:
        Debug.Log($"uncaught message type {messageType}");
        break;
    }
  }

  void Ping()
  {
    var bytes = new byte[] { (byte)MessageType.PING };
    connection.Send(bytes);
    ukatonMission.logger.Log("PING");
    LastPingTime = Time.time;
  }

  void OnConnect()
  {
    ukatonMission.logger.Log("CONNECTED!");
    var bytesList = ukatonMission.CreateSensorConfiguration();
    bytesList.Insert(0, (byte)bytesList.Count());
    bytesList.Insert(0, (byte)MessageType.SET_SENSOR_DATA_CONFIGURATIONS);
    var bytesArray = bytesList.ToArray();
    //Debug.Log(string.Join(", ", bytesArray));
    connection.Send(bytesArray);
    LastPingTime = Time.time;
  }
}
