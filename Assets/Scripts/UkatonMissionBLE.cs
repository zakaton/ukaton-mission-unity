using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class UkatonMissionBLE : UkatonMission
{
  static private string GENERATE_UUID(string value)
  {
    return String.Format("5691eddf-{0}-4420-b7a5-bb8751ab5181", value);
  }
  static bool IsEqual(string uuid1, string uuid2)
  {
    if (uuid1.Length == 4)
      uuid1 = GENERATE_UUID(uuid1);
    if (uuid2.Length == 4)
      uuid2 = GENERATE_UUID(uuid2);

    return (uuid1.ToUpper().Equals(uuid2.ToUpper()));
  }

  private string serviceUUID = GENERATE_UUID("0000");
  private string sensorDataConfigurationCharacteristicUUID = GENERATE_UUID("6001");
  private string sensorDataCharacteristicUUID = GENERATE_UUID("6002");

  private float _timeout = 0f;
  private States _state = States.None;
  private string _deviceAddress;
  private bool _foundSensorDataConfigurationCharacteristicUUID = false;
  private bool _foundSensorDataCharacteristicUUID = false;
  private bool _rssiOnly = false;
  private int _rssi = 0;
  void Reset()
  {
    IsConnected = false;
    _timeout = 0f;
    _state = States.None;
    _deviceAddress = null;
    _foundSensorDataConfigurationCharacteristicUUID = false;
    _foundSensorDataCharacteristicUUID = false;
    _rssi = 0;
  }

  enum States
  {
    None,
    Scan,
    ScanRSSI,
    ReadRSSI,
    Connect,
    RequestMTU,
    Subscribe,
    Unsubscribe,
    Disconnect,
    StopScan
  }
  void SetState(States newState, float timeout)
  {
    _state = newState;
    _timeout = timeout;
  }

  // Start is called before the first frame update
  public override void Start()
  {
    base.Start();
    if (autoConnect)
    {
      Connect();
    }
  }

  public override void _UpdateSensorDataConfiguration()
  {
    if (!IsConnected)
    {
      return;
    }

    List<byte> sensorDataConfiguration = CreateSensorConfiguration();

    byte[] data = sensorDataConfiguration.ToArray();
    logger.Log(string.Join(", ", data));

    BluetoothLEHardwareInterface.WriteCharacteristic(_deviceAddress, serviceUUID, sensorDataConfigurationCharacteristicUUID, data, data.Length, true, (characteristicUUID) =>
    {
      BluetoothLEHardwareInterface.Log("Write Succeeded");
    });
  }

  public override void Connect()
  {
    if (IsConnected)
    {
      return;
    }

    IsConnecting = true;
    connectionEvents.onConnecting.Invoke();

    Reset();
    try
    {

      BluetoothLEHardwareInterface.Initialize(true, false, () =>
      {
        SetState(States.Scan, 0.1f);
      }, (error) =>
      {
        StatusMessage = "Error during initialize: " + error;
      });
    }
    catch (Exception e)
    {
      StatusMessage = e.Message;
    }
  }
  public override void Disconnect()
  {
    if (IsConnected)
    {
      SetState(States.Disconnect, 0.1f);
    }
    else
    {
      Debug.Log(String.Format("state: {0}", _state));
      if (_state == States.Scan)
      {
        SetState(States.StopScan, 0.1f);
      }
    }
  }

  // Update is called once per frame
  public override void Update()
  {
    CheckUpdateSensorDataConfiguration();

    if (_timeout > 0f)
    {
      _timeout -= Time.deltaTime;
      if (_timeout <= 0f)
      {
        _timeout = 0f;

        switch (_state)
        {
          case States.None:
            break;

          case States.Scan:
            StatusMessage = "Scanning for " + deviceName;

            BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, (address, name) =>
            {
              // if your device does not advertise the rssi and manufacturer specific data
              // then you must use this callback because the next callback only gets called
              // if you have manufacturer specific data

              if (!_rssiOnly)
              {
                if (name.Contains(deviceName))
                {
                  StatusMessage = "Found " + name;

                  // found a device with the name we want
                  // this example does not deal with finding more than one
                  _deviceAddress = address;
                  SetState(States.Connect, 0.5f);
                }
              }

            }, (address, name, rssi, bytes) =>
            {

              // use this one if the device responses with manufacturer specific data and the rssi

              if (name.Contains(deviceName))
              {
                StatusMessage = "Found " + name;

                if (_rssiOnly)
                {
                  _rssi = rssi;
                }
                else
                {
                  // found a device with the name we want
                  // this example does not deal with finding more than one
                  _deviceAddress = address;
                  SetState(States.Connect, 0.5f);
                }
              }

            }, _rssiOnly); // this last setting allows RFduino to send RSSI without having manufacturer data

            if (_rssiOnly)
              SetState(States.ScanRSSI, 0.5f);
            break;

          case States.ScanRSSI:
            break;

          case States.ReadRSSI:
            StatusMessage = $"Call Read RSSI";
            BluetoothLEHardwareInterface.ReadRSSI(_deviceAddress, (address, rssi) =>
            {
              StatusMessage = $"Read RSSI: {rssi}";
            });

            SetState(States.ReadRSSI, 2f);
            break;

          case States.Connect:
            StatusMessage = "Connecting...";

            // set these flags
            _foundSensorDataConfigurationCharacteristicUUID = false;
            _foundSensorDataCharacteristicUUID = false;

            // note that the first parameter is the address, not the name. I have not fixed this because
            // of backwards compatiblity.
            // also note that I am note using the first 2 callbacks. If you are not looking for specific characteristics you can use one of
            // the first 2, but keep in mind that the device will enumerate everything and so you will want to have a timeout
            // large enough that it will be finished enumerating before you try to subscribe or do any other operations.
            BluetoothLEHardwareInterface.ConnectToPeripheral(_deviceAddress, null, null, (address, _serviceUUID, characteristicUUID) =>
            {
              BluetoothLEHardwareInterface.StopScan();

              if (IsEqual(_serviceUUID, serviceUUID))
              {
                StatusMessage = "Found Service UUID";

                _foundSensorDataConfigurationCharacteristicUUID = _foundSensorDataConfigurationCharacteristicUUID || IsEqual(characteristicUUID, sensorDataConfigurationCharacteristicUUID);
                _foundSensorDataCharacteristicUUID = _foundSensorDataCharacteristicUUID || IsEqual(characteristicUUID, sensorDataCharacteristicUUID);

                // if we have found both characteristics that we are waiting for
                // set the state. make sure there is enough timeout that if the
                // device is still enumerating other characteristics it finishes
                // before we try to subscribe
                if (!IsConnected && _foundSensorDataConfigurationCharacteristicUUID && _foundSensorDataCharacteristicUUID)
                {
                  IsConnected = true;
                  IsConnecting = false;
                  connectionEvents.onConnect.Invoke();
                  SetState(States.RequestMTU, 2f);
                }
              }
            });
            break;

          case States.RequestMTU:
            StatusMessage = "Requesting MTU";

            BluetoothLEHardwareInterface.RequestMtu(_deviceAddress, 185, (address, newMTU) =>
            {
              StatusMessage = "MTU set to " + newMTU.ToString();

              SetState(States.Subscribe, 0.1f);
            });
            break;

          case States.Subscribe:
            StatusMessage = "Subscribing to characteristics...";

            BluetoothLEHardwareInterface.SubscribeCharacteristicWithDeviceAddress(_deviceAddress, serviceUUID, sensorDataCharacteristicUUID, (notifyAddress, notifyCharacteristic) =>
            {
              _state = States.None;

              // read the initial state of the button
              BluetoothLEHardwareInterface.ReadCharacteristic(_deviceAddress, serviceUUID, sensorDataCharacteristicUUID, (characteristic, bytes) =>
              {
                ProcessSensorData(bytes);
              });

              SetState(States.ReadRSSI, 1f);

            }, (address, characteristicUUID, bytes) =>
            {
              if (_state != States.None)
              {
                // some devices do not properly send the notification state change which calls
                // the lambda just above this one so in those cases we don't have a great way to
                // set the state other than waiting until we actually got some data back.
                // The esp32 sends the notification above, but if yuor device doesn't you would have
                // to send data like pressing the button on the esp32 as the sketch for this demo
                // would then send data to trigger this.
                SetState(States.ReadRSSI, 1f);
              }

              // we received some data from the device
              ProcessSensorData(bytes);
            });
            break;

          case States.Unsubscribe:
            BluetoothLEHardwareInterface.UnSubscribeCharacteristic(_deviceAddress, serviceUUID, sensorDataCharacteristicUUID, null);
            SetState(States.Disconnect, 4f);
            break;

          case States.Disconnect:
            StatusMessage = "Commanded disconnect.";

            if (IsConnected)
            {
              BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, (address) =>
              {
                StatusMessage = "Device disconnected";
                BluetoothLEHardwareInterface.DeInitialize(() =>
                              {
                                IsConnected = false;
                                _state = States.None;
                                connectionEvents.onDisconnect.Invoke();
                              });
              });
            }
            else
            {
              BluetoothLEHardwareInterface.DeInitialize(() =>
              {
                _state = States.None;
              });
            }
            break;
        }
      }
    }
  }
}
