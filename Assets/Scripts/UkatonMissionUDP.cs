using UnityEngine;
using System;
using System.Linq;
using UnityEditor;

[Serializable]
public class UkatonMissionUDP : UkatonMission
{
  [SerializeField]
  public string address = "0.0.0.0";

  public UdpConnection connection = null;

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

  private bool shouldConnect = false;

  public override void Start()
  {
    base.Start();
    if (autoConnect)
    {
      Connect();
    }
  }

  public override void Connect()
  {
    if (connection == null)
    {
      Debug.Log(address);
      connection = new UdpConnection();
      connection.StartConnection(address, 9999, 11000); // TODO - different ports for diff
    }
    IsConnecting = true;
    shouldConnect = true;
    connectionEvents.onConnecting.Invoke();
  }

  public override void Disconnect()
  {
    shouldConnect = false;
    IsConnected = false;
    LastTimeReceivedData = 0;
  }

  public override void Update()
  {
    if (connection == null)
    {
      return;
    }

    CheckUpdateSensorDataConfiguration();

    var messages = connection.getMessages();
    if (messages.Length > 0)
    {
      foreach (var message in messages)
      {
        ProcessData(message);
      }
      if (LastTimeReceivedData == 0)
      {
        OnConnect();
      }
      LastTimeReceivedData = time;
    }

    if (shouldConnect && time - LastPingTime > 1)
    {
      if (LastTimeReceivedData > 0 && time - LastTimeReceivedData > 5)
      {
        _UpdateSensorDataConfiguration();
      }
      else
      {
        Ping();
      }
    }
  }

  public void ProcessData(byte[] bytes)
  {
    var messageType = (MessageType)bytes[0];
    switch (messageType)
    {
      case MessageType.SENSOR_DATA:
        var segment = new ArraySegment<byte>(bytes, 1, bytes.Length - 1).ToArray();
        ProcessSensorData(segment);
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

  public void Ping()
  {
    var bytes = new byte[] { (byte)(IsConnected ? MessageType.PING : MessageType.GET_NAME) };
    connection.Send(bytes);
    logger.Log("PING");
    LastPingTime = time;
  }

  private float time
  {
    get
    {
      return EditorApplication.isPlaying ? Time.time : (float)EditorApplication.timeSinceStartup;
    }
  }

  void OnConnect()
  {
    IsConnected = true;
    IsConnecting = false;
    connectionEvents.onConnect.Invoke();
  }

  public override void _UpdateSensorDataConfiguration()
  {
    var bytesList = CreateSensorConfiguration();
    bytesList.Insert(0, (byte)bytesList.Count());
    bytesList.Insert(0, (byte)MessageType.SET_SENSOR_DATA_CONFIGURATIONS);
    var bytesArray = bytesList.ToArray();
    logger.Log(string.Join(", ", bytesArray));
    connection.Send(bytesArray);
    LastPingTime = time;
  }
}
