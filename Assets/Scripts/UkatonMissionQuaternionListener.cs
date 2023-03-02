using UnityEngine;

public class UkatonMissionQuaternionListener : MonoBehaviour
{
  public UkatonMissionMonoBehavior ukatonMissionMonoBehavior;

  void Start()
  {
    ukatonMissionMonoBehavior.motionDataEvents.quaternion.AddListener(onQuaternionUpdate);
  }

  public bool resetYaw = false;
  private bool didCalibrateYawOnce = true;
  public float yawOffset = 0;
  public void onQuaternionUpdate()
  {
    var e = ukatonMissionMonoBehavior.ukatonMission.motionData.quaternion.eulerAngles;
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
