using UnityEngine;

public class UkatonMissionQuaternionListener : MonoBehaviour
{
  public UkatonMission ukatonMission;

  void Start()
  {
    ukatonMission.motionDataEvents.quaternion.AddListener(onQuaternionUpdate);
  }

  public void onQuaternionUpdate()
  {
    transform.localRotation = ukatonMission.motionData.quaternion;
  }
}
