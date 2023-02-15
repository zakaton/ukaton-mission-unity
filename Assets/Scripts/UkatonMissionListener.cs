using System.Collections;
using UnityEngine;

public class UkatonMissionListener : MonoBehaviour
{
  public UkatonMission ukatonMission;

  public void onQuaternionUpdate()
  {
    transform.rotation = ukatonMission.motionData.quaternion;
  }
}
