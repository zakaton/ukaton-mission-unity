using UnityEngine;

public class UkatonMissionQuaternionListenerUDP : MonoBehaviour
{
  public UkatonMissionUDP ukatonMissionUDP;

  void Start()
  {
    ukatonMissionUDP.ukatonMission.motionDataEvents.quaternion.AddListener(onQuaternionUpdate);
  }

  public bool resetYaw = false;
  private bool didCalibrateYawOnce = true;
  public float yawOffset = 0;
  public void onQuaternionUpdate()
  {
    var e = ukatonMissionUDP.ukatonMission.motionData.quaternion.eulerAngles;
    if (resetYaw)
    {
      yawOffset = e.y;
      resetYaw = false;
      didCalibrateYawOnce = true;
    }
    if (didCalibrateYawOnce)
    {
      var q = Quaternion.Euler(e.x, e.y - yawOffset, e.z);
      transform.localRotation = Quaternion.Lerp(transform.localRotation, q, 0.1f);
    }
  }
}
