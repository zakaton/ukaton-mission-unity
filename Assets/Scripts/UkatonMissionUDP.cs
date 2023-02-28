using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;
using System.Linq;

/*
    TODO
        parse data
        discovery ping
*/

public class UkatonMissionUDP : MonoBehaviour
{
  [SerializeField]
  private string address = "0.0.0.0";

  [SerializeField]
  private UkatonMissionBaseClass ukatonMission = new();
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

  static UdpClient udp;
  Thread thread;

  static readonly object lockObject = new object();
  bool processData = false;

  private float LastPingTime = 0;
  private float LastTimeReceivedData = 0;

  void Start()
  {
    ukatonMission.Start();
    ukatonMission.connectionEvents.onConnect.AddListener(OnConnect);
    if (ukatonMission.autoConnect)
    {
      thread = new Thread(new ThreadStart(ThreadMethod));
      thread.Start();
    }
  }

  void OnConnect()
  {
    if (udp != null)
    {
      Debug.Log("CONNECTED!");
      var bytesList = ukatonMission.CreateSensorConfiguration();
      bytesList.Insert(0, (byte)bytesList.Count());
      bytesList.Insert(0, (byte)MessageType.SET_SENSOR_DATA_CONFIGURATIONS);
      var bytesArray = bytesList.ToArray();
      udp.SendAsync(bytesArray, bytesArray.Length);
      LastPingTime = Time.time;
    }
  }

  void OnApplicationQuit()
  {
    if (udp != null)
    {
      udp.Close();
    }
    thread.Abort();
  }

  void Update()
  {
    if (processData)
    {
      /*lock object to make sure there data is 
       *not being accessed from multiple threads at the same time*/
      lock (lockObject)
      {
        processData = false;
        if (LastTimeReceivedData == 0)
        {
          ukatonMission.connectionEvents.onConnect.Invoke();
        }
        LastTimeReceivedData = Time.time;

        //Process received data
        Debug.Log(String.Format("received {0} bytes", bytesToProcess.Length));
        ProcessSensorData(bytesToProcess);
      }
    }

    if (LastTimeReceivedData > 0 && Time.time - LastPingTime > 1)
    {
      Ping();
    }
  }

  void ProcessSensorData(byte[] bytes)
  {
    var messageType = (MessageType)bytes[0];
    switch (messageType)
    {
      case MessageType.SENSOR_DATA:
        var segment = new ArraySegment<byte>(bytes, 1, bytes.Length - 1).ToArray();
        ukatonMission.ProcessSensorData(segment);
        break;
      default:
        Debug.Log($"uncaught message type {messageType}");
        break;
    }
  }

  void Ping()
  {
    var bytes = new byte[] { (byte)MessageType.PING };
    udp.SendAsync(bytes, bytes.Length);
    Debug.Log("PING");
    LastPingTime = Time.time;
  }

  private byte[] bytesToProcess;
  private void ThreadMethod()
  {
    udp = new UdpClient(address, 9999);
    var bytes = new byte[] { (byte)MessageType.PING };
    Debug.Log("initial ping");
    udp.SendAsync(bytes, bytes.Length);
    while (true)
    {
      IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

      byte[] receiveBytes = udp.Receive(ref RemoteIpEndPoint);

      /*lock object to make sure there data is 
      *not being accessed from multiple threads at the same time*/
      lock (lockObject)
      {
        bytesToProcess = receiveBytes;
        processData = true;

        /*
        returnData = Encoding.ASCII.GetString(receiveBytes);

        Debug.Log(returnData);
        Debug.Log(returnData.Length);
        if (returnData.Length > 0)
        {
          //Done, notify the Update function
          processData = true;
        }
        */
      }
    }
  }
}
