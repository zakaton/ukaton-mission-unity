using UnityEngine;

public class UkatonMissionQuaternionListenerUDP : MonoBehaviour
{
  public UkatonMissionUDP ukatonMissionUDP;

  void Start()
  {
    ukatonMissionUDP.ukatonMission.motionDataEvents.quaternion.AddListener(onQuaternionUpdate);
  }

  public void onQuaternionUpdate()
  {
    transform.localRotation = ukatonMissionUDP.ukatonMission.motionData.quaternion;
  }
}
