using System;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Buffers.Binary;

/*
  TODO
    fix working on quest
    include models
*/

public class UkatonMission : MonoBehaviour
{
  public TMPro.TMP_Text loggerText;

  public enum DeviceType { motionModule, leftInsole, rightInsole };
  public enum InsoleSide { left, right }
  public enum SensorType { motion, pressure };
  public enum MotionDataType { acceleration, gravity, linearAcceleration, rotation, magnetometer, quaternion };
  public enum PressureDataType { singleByte, doubleByte, centerOfMass, mass, heelToToe };
  public enum SensorDataRate
  {
    _0 = 0,
    _10 = 10,
    _20 = 20,
    _40 = 40,
    _80 = 80,
    _100 = 100
  }

  public DeviceType deviceType
  {
    get
    {
      if (isInsole)
      {
        return insoleSide == InsoleSide.left ? DeviceType.leftInsole : DeviceType.rightInsole;
      }
      else
      {
        return DeviceType.motionModule;
      }
    }
  }

  [SerializeField]
  public string deviceName = "Device Name";
  [SerializeField]
  public bool autoConnect = false;
  [SerializeField]
  public bool isInsole = false;
  [SerializeField]
  public InsoleSide insoleSide;

  [Serializable]
  public class MotionSensorDataRates
  {
    public SensorDataRate acceleration = SensorDataRate._0;
    public SensorDataRate gravity = SensorDataRate._0;
    public SensorDataRate linearAcceleration = SensorDataRate._0;
    public SensorDataRate rotation = SensorDataRate._0;
    public SensorDataRate magnetometer = SensorDataRate._0;
    public SensorDataRate quaternion = SensorDataRate._0;

    public SensorDataRate[] GetRates()
    {
      SensorDataRate[] rates = {
        acceleration,
        gravity,
        linearAcceleration,
        rotation,
        magnetometer,
        quaternion
      };
      return rates;
    }
  }
  [SerializeField]
  private MotionSensorDataRates motionSensorDataRates = new();

  [Serializable]
  public class PressureSensorDataRates
  {
    public SensorDataRate singleByte = SensorDataRate._0;
    public SensorDataRate doubleByte = SensorDataRate._0;
    public SensorDataRate centerOfMass = SensorDataRate._0;
    public SensorDataRate mass = SensorDataRate._0;
    public SensorDataRate heelToToe = SensorDataRate._0;

    public SensorDataRate[] GetRates()
    {
      SensorDataRate[] rates = {
        singleByte,
        doubleByte,
        centerOfMass,
        mass,
        heelToToe
      };
      return rates;
    }
  }
  [SerializeField]
  private PressureSensorDataRates pressureSensorDataRates = new();

  public Logger logger = new Logger(Debug.unityLogger.logHandler);
  [SerializeField]
  public bool enableLogging;
  public void UpdateLogging()
  {
    logger.logEnabled = enableLogging;
  }

  [Serializable]
  public class ConnectionEvents
  {
    public UnityEvent onConnect;
    public UnityEvent onConnecting;
    public UnityEvent onDisconnect;
  }
  public ConnectionEvents connectionEvents;

  [Serializable]
  public class MotionDataEvents
  {
    [field: SerializeField]
    public UnityEvent acceleration { get; set; }

    [field: SerializeField]
    public UnityEvent gravity { get; set; }
    [field: SerializeField]
    public UnityEvent linearAcceleration { get; set; }
    [field: SerializeField]
    public UnityEvent rotation { get; set; }
    [field: SerializeField]
    public UnityEvent magnetometer { get; set; }
    [field: SerializeField]
    public UnityEvent quaternion { get; set; }

    public MotionDataEvents()
    {
      Type type = this.GetType();
      System.Reflection.PropertyInfo[] props = type.GetProperties();
      System.Reflection.PropertyInfo _prop = type.GetProperty(this.ToString());
      foreach (System.Reflection.PropertyInfo prop in props)
      {
        prop.SetValue(this, new UnityEvent());
      }
    }
  }
  [SerializeField]
  public MotionDataEvents motionDataEvents;

  [Serializable]
  public class PressureDataEvents
  {
    public UnityEvent pressure;
    public UnityEvent centerOfMass;
    public UnityEvent mass;
    public UnityEvent heelToToe;
  }
  public PressureDataEvents pressureDataEvents;

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

  private bool _connected = false;
  private float _timeout = 0f;
  private States _state = States.None;
  private string _deviceAddress;
  private bool _foundSensorDataConfigurationCharacteristicUUID = false;
  private bool _foundSensorDataCharacteristicUUID = false;
  private bool _rssiOnly = false;
  private int _rssi = 0;
  void Reset()
  {
    _connected = false;
    _timeout = 0f;
    _state = States.None;
    _deviceAddress = null;
    _foundSensorDataConfigurationCharacteristicUUID = false;
    _foundSensorDataCharacteristicUUID = false;
    _rssi = 0;
  }

  private string StatusMessage
  {
    set
    {
      logger.Log(value);
      if (loggerText)
      {
        loggerText.text = value;
      }
      //BluetoothLEHardwareInterface.Log(value);
    }
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
  }
  void SetState(States newState, float timeout)
  {
    _state = newState;
    _timeout = timeout;
  }

  // Start is called before the first frame update
  void Start()
  {
    NormalizePressurePositions();
    SetupQuaternions();
    UpdateLogging();
    connectionEvents.onConnect.AddListener(updateSensorDataConfiguration);
    if (autoConnect)
    {
      Connect();
    }
  }

  public void Connect()
  {
    if (_connected)
    {
      return;
    }

    connectionEvents.onConnecting.Invoke();

    StatusMessage = "A";
    Reset();
    StatusMessage = "B";

    try
    {

      BluetoothLEHardwareInterface.Initialize(true, false, () =>
      {
        StatusMessage = "C";
        SetState(States.Scan, 0.1f);
        StatusMessage = "C";
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

  private List<byte> CreateSensorConfiguration()
  {
    List<byte> sensorConfiguration = new();

    foreach (SensorType sensorType in Enum.GetValues(typeof(SensorType)))
    {
      Array sensorDataTypes = null;
      SensorDataRate[] sensorDataRates = null;
      switch (sensorType)
      {
        case SensorType.motion:
          sensorDataTypes = Enum.GetValues(typeof(MotionDataType));
          sensorDataRates = motionSensorDataRates.GetRates();
          break;
        case SensorType.pressure:
          sensorDataTypes = Enum.GetValues(typeof(PressureDataType));
          sensorDataRates = pressureSensorDataRates.GetRates();
          break;
      }

      if (sensorDataTypes != null)
      {
        sensorConfiguration.Add((byte)sensorType);
        sensorConfiguration.Add((byte)(sensorDataRates.Length * 3));

        foreach (int sensorDataType in sensorDataTypes)
        {
          sensorConfiguration.Add((byte)sensorDataType);
          byte[] sensorDataRate = BitConverter.GetBytes((int)sensorDataRates[sensorDataType]);
          sensorConfiguration.Add(sensorDataRate[0]);
          sensorConfiguration.Add(sensorDataRate[1]);
        }
      }
    }

    return sensorConfiguration;
  }

  public void updateSensorDataConfiguration()
  {
    if (!_connected)
    {
      return;
    }

    List<byte> sensorDataConfiguration = CreateSensorConfiguration();

    byte[] data = sensorDataConfiguration.ToArray();

    BluetoothLEHardwareInterface.WriteCharacteristic(_deviceAddress, serviceUUID, sensorDataConfigurationCharacteristicUUID, data, data.Length, true, (characteristicUUID) =>
    {
      BluetoothLEHardwareInterface.Log("Write Succeeded");
    });
  }

  Dictionary<InsoleSide, Quaternion> insoleCorrectionQuaternions = new();
  private Quaternion insoleCorrectionQuaternion
  {
    get
    {
      return insoleCorrectionQuaternions[insoleSide];
    }
  }
  Dictionary<DeviceType, Quaternion> correctionQuaternions = new();
  private Quaternion correctionQuaternion
  {
    get
    {
      return correctionQuaternions[deviceType];
    }
  }
  private bool didSetupQuaternions = false;
  private void SetupQuaternions()
  {
    if (didSetupQuaternions)
    {
      return;
    }
    didSetupQuaternions = true;

    insoleCorrectionQuaternions[InsoleSide.right] = Quaternion.Euler(0, (float)-Math.PI / 2, 0);
    insoleCorrectionQuaternions[InsoleSide.left] = Quaternion.Euler((float)-Math.PI / 2, (float)-Math.PI / 2, 0);

    correctionQuaternions[DeviceType.motionModule] = Quaternion.Euler(0, (float)-Math.PI / 2, 0);
    correctionQuaternions[DeviceType.leftInsole] = Quaternion.Euler(0, (float)Math.PI, 0);
    correctionQuaternions[DeviceType.rightInsole] = Quaternion.Euler(0, (float)Math.PI, 0);
  }

  public class MotionData
  {
    public Vector3 acceleration { get; set; }
    public Vector3 gravity { get; set; }
    public Vector3 linearAcceleration { get; set; }
    public Vector3 rotation { get; set; }
    public Vector3 magnetometer { get; set; }
    public Quaternion quaternion { get; set; }
  }
  public MotionData motionData = new();

  public class PressureData
  {
    public double[] pressure = new double[numberOfPressureSensors];
    public Vector2 centerOfMass;
    public double mass;
    public double sum;
    public double heelToToe;

    public static int numberOfPressureSensors = 16;
  }
  public PressureData pressureData = new();

  private UInt16 ToUInt16(byte[] bytes, int byteOffset)
  {
    UInt16 value = BitConverter.ToUInt16(bytes, byteOffset);
    if (!BitConverter.IsLittleEndian)
    {
      value = BinaryPrimitives.ReverseEndianness(value);
    }
    return value;
  }
  private Int16 ToInt16(byte[] bytes, int byteOffset)
  {
    Int16 value = BitConverter.ToInt16(bytes, byteOffset);
    if (!BitConverter.IsLittleEndian)
    {
      value = BinaryPrimitives.ReverseEndianness(value);
    }
    return value;
  }
  private UInt32 ToUInt32(byte[] bytes, int byteOffset)
  {
    UInt32 value = BitConverter.ToUInt32(bytes, byteOffset);
    if (!BitConverter.IsLittleEndian)
    {
      value = BinaryPrimitives.ReverseEndianness(value);
    }
    return value;
  }
  private double ToDouble(byte[] bytes, int byteOffset)
  {
    if (!BitConverter.IsLittleEndian)
    {
      byte[] _bytes = new byte[8];
      for (int i = 0; i < _bytes.Length; i++)
      {
        _bytes[i] = bytes[_bytes.Length - 1 - i];
      }
      return BitConverter.ToDouble(_bytes, byteOffset);
    }
    else
    {
      return BitConverter.ToDouble(bytes, byteOffset);
    }
  }
  private float ToSingle(byte[] bytes, int byteOffset)
  {
    if (!BitConverter.IsLittleEndian)
    {
      byte[] _bytes = new byte[4];
      for (int i = 0; i < _bytes.Length; i++)
      {
        _bytes[i] = bytes[_bytes.Length - 1 - i];
      }
      return BitConverter.ToSingle(_bytes, byteOffset);
    }
    else
    {
      return BitConverter.ToSingle(bytes, byteOffset);
    }
  }

  private void ProcessSensorData(byte[] bytes)
  {
    logger.Log(String.Format("received {0} bytes", bytes.Length));
    int byteOffset = 0;

    UInt16 timestamp = ToUInt16(bytes, byteOffset);
    byteOffset += 2;

    while (byteOffset < bytes.Length)
    {
      SensorType sensorType = (SensorType)bytes[byteOffset++];
      int dataSize = bytes[byteOffset++];
      int finalByteOffset = byteOffset + dataSize;

      switch (sensorType)
      {
        case SensorType.motion:
          while (byteOffset < finalByteOffset)
          {
            MotionDataType motionDataType = (MotionDataType)bytes[byteOffset++];
            double scalar = MotionDataScalars[motionDataType];
            switch (motionDataType)
            {
              case MotionDataType.acceleration:
              case MotionDataType.gravity:
              case MotionDataType.linearAcceleration:
              case MotionDataType.magnetometer:
                Vector3 vector = parseMotionVector(bytes, byteOffset, scalar);
                motionData.GetType().GetProperty(motionDataType.ToString()).SetValue(motionData, vector);
                logger.Log(String.Format("{0}: {1}", motionDataType.ToString(), vector));
                UnityEvent unityEvent = (UnityEvent)motionDataEvents.GetType().GetProperty(motionDataType.ToString())?.GetValue(motionDataEvents, null);
                unityEvent.Invoke();
                byteOffset += 6;
                break;
              case MotionDataType.rotation:
                Vector3 euler = parseMotionEuler(bytes, byteOffset, scalar);
                motionData.GetType().GetProperty(motionDataType.ToString()).SetValue(motionData, euler);
                logger.Log(String.Format("{0}: {1}", motionDataType.ToString(), euler));
                motionDataEvents.rotation.Invoke();
                byteOffset += 6;
                break;
              case MotionDataType.quaternion:
                Quaternion quaternion = parseMotionQuaternion(bytes, byteOffset, scalar);
                motionData.GetType().GetProperty(motionDataType.ToString()).SetValue(motionData, quaternion);
                logger.Log(String.Format("{0}: {1} ({2})", motionDataType.ToString(), quaternion, motionData.quaternion));
                motionDataEvents.quaternion.Invoke();
                byteOffset += 8;
                break;
              default:
                break;
            }
          }
          break;
        case SensorType.pressure:
          while (byteOffset < finalByteOffset)
          {
            PressureDataType pressureDataType = (PressureDataType)bytes[byteOffset++];
            logger.Log(pressureDataType.ToString());
            double scalar = PressureDataScalars.ContainsKey(pressureDataType) ? PressureDataScalars[pressureDataType] : 1;
            switch (pressureDataType)
            {
              case PressureDataType.singleByte:
              case PressureDataType.doubleByte:
                pressureData.sum = 0;
                for (int i = 0; i < PressureData.numberOfPressureSensors; i++)
                {
                  double value = 0;
                  if (pressureDataType == PressureDataType.singleByte)
                  {
                    value = bytes[byteOffset++];
                  }
                  else
                  {
                    value = ToUInt16(bytes, byteOffset);
                    byteOffset += 2;
                  }
                  pressureData.pressure[i] = value;
                  pressureData.sum += value;
                }

                pressureData.centerOfMass.Set(0, 0);
                pressureData.heelToToe = 0;
                if (pressureData.sum > 0)
                {
                  for (int i = 0; i < PressureData.numberOfPressureSensors; i++)
                  {
                    double value = pressureData.pressure[i];
                    double[] pressurePosition = GetPressurePosition(i);
                    double weight = value / pressureData.sum;
                    if (Double.IsInfinity(weight))
                    {
                      weight = 0;
                    }
                    pressureData.centerOfMass.x += (float)(weight * pressurePosition[0]);
                    pressureData.centerOfMass.y += (float)(weight * pressurePosition[1]);
                  }
                  pressureData.heelToToe = 1 - pressureData.centerOfMass.y;
                }

                for (int i = 0; i < PressureData.numberOfPressureSensors; i++)
                {
                  pressureData.pressure[i] *= scalar;
                }
                pressureData.mass = pressureData.sum * (scalar / PressureData.numberOfPressureSensors);

                pressureDataEvents.pressure.Invoke();
                pressureDataEvents.centerOfMass.Invoke();
                pressureDataEvents.mass.Invoke();
                pressureDataEvents.heelToToe.Invoke();
                break;
              case PressureDataType.centerOfMass:
                pressureData.centerOfMass.Set(ToSingle(bytes, byteOffset), ToSingle(bytes, byteOffset + 4));
                byteOffset += 4 * 2;
                logger.Log(pressureData.centerOfMass);
                pressureDataEvents.centerOfMass.Invoke();
                break;
              case PressureDataType.mass:
                pressureData.mass = ToUInt32(bytes, byteOffset) * scalar;
                byteOffset += 4;
                logger.Log(pressureData.mass);
                pressureDataEvents.mass.Invoke();
                break;
              case PressureDataType.heelToToe:
                pressureData.heelToToe = 1 - ToDouble(bytes, byteOffset);
                byteOffset += 8;
                logger.Log(pressureData.heelToToe);
                pressureDataEvents.heelToToe.Invoke();
                break;
              default:
                break;
            }
          }
          break;
        default:
          break;
      }
    }
  }

  Dictionary<MotionDataType, double> MotionDataScalars = new()
  {
    { MotionDataType.acceleration, Math.Pow(2, -8) },
    { MotionDataType.gravity, Math.Pow(2, -8) },
    { MotionDataType.linearAcceleration, Math.Pow(2, -8) },
    { MotionDataType.rotation, Math.Pow(2, -9) },
    { MotionDataType.magnetometer, Math.Pow(2, -4) },
    { MotionDataType.quaternion, Math.Pow(2, -14) }
  };
  Dictionary<PressureDataType, double> PressureDataScalars = new()
  {
    { PressureDataType.singleByte, 1 / Math.Pow(2, 8) },
    { PressureDataType.doubleByte, 1 / Math.Pow(2, 12) },
    { PressureDataType.mass, 1 / Math.Pow(2, 16) }
  };

  private Vector3 parseMotionVector(byte[] bytes, int byteOffset, double scalar)
  {
    Vector3 vector = new();

    Int16 x = ToInt16(bytes, byteOffset);
    Int16 y = ToInt16(bytes, byteOffset + 2);
    Int16 z = ToInt16(bytes, byteOffset + 4);

    if (isInsole)
    {
      if (insoleSide == InsoleSide.right)
      {
        vector.Set(z, x, y);
      }
      else
      {
        vector.Set(-z, x, -y);
      }
    }
    else
    {
      vector.Set(-y, -z, -x);
    }

    vector *= (float)scalar;

    vector.z *= -1;
    return vector;
  }
  private Vector3 parseMotionEuler(byte[] bytes, int byteOffset, double scalar)
  {
    Vector3 euler = new();

    float x = ToInt16(bytes, byteOffset);
    float y = ToInt16(bytes, byteOffset + 2);
    float z = ToInt16(bytes, byteOffset + 4);

    if (isInsole)
    {
      if (insoleSide == InsoleSide.right)
      {
        euler.Set(z, y, x);
      }
      else
      {
        euler.Set(-z, y, -x);
      }
    }
    else
    {
      euler.Set(y, -z, x);
    }

    euler *= (float)scalar;

    euler.z *= -1;
    return euler;
  }
  private Quaternion parseMotionQuaternion(byte[] bytes, int byteOffset, double scalar)
  {
    Quaternion quaternion = new();

    float w = ToInt16(bytes, byteOffset) * (float)scalar;
    float x = ToInt16(bytes, byteOffset + 2) * (float)scalar;
    float y = ToInt16(bytes, byteOffset + 4) * (float)scalar;
    float z = ToInt16(bytes, byteOffset + 6) * (float)scalar;

    //quaternion.Set(-y, -w, -x, z);
    quaternion.Set(-y, w, x, z);

    if (isInsole)
    {
      quaternion *= insoleCorrectionQuaternion;
    }
    quaternion *= correctionQuaternion;

    return quaternion;
  }

  private double[,] PressurePositions = new double[16, 2] {
    {59.55, 32.3},
    {33.1, 42.15},

    {69.5, 55.5},
    {44.11, 64.8},
    {20.3, 71.9},

    {63.8, 81.1},
    {41.44, 90.8},
    {19.2, 102.8},

    {48.3, 119.7},
    {17.8, 130.5},

    {43.3, 177.7},
    {18.0, 177.0},

    {43.3, 200.6},
    {18.0, 200.0},

    {43.5, 242.0},
    {18.55, 242.1},
  };
  private double[] GetPressurePosition(int index)
  {
    double x = PressurePositions[index, 0];
    double y = PressurePositions[index, 1];
    if (insoleSide == InsoleSide.right)
    {
      x = 1 - x;
    }

    double[] pressurePosition = { x, y };
    return pressurePosition;
  }
  private bool didNormalizePressurePositions = false;
  private void NormalizePressurePositions()
  {
    if (didNormalizePressurePositions)
    {
      return;
    }
    didNormalizePressurePositions = true;

    Debug.Log("normalizing pressure positions...");
    for (int i = 0; i < PressureData.numberOfPressureSensors; i++)
    {
      PressurePositions[i, 0] /= 93.257;
      PressurePositions[i, 1] /= 265.069;
    }
  }

  // Update is called once per frame
  void Update()
  {
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
                if (!_connected && _foundSensorDataConfigurationCharacteristicUUID && _foundSensorDataCharacteristicUUID)
                {
                  _connected = true;
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

            if (_connected)
            {
              BluetoothLEHardwareInterface.DisconnectPeripheral(_deviceAddress, (address) =>
              {
                StatusMessage = "Device disconnected";
                BluetoothLEHardwareInterface.DeInitialize(() =>
                              {
                                _connected = false;
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
