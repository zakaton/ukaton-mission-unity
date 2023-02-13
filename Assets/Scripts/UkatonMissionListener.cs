using System.Collections;
using UnityEngine;

public class UkatonMissionListener : MonoBehaviour
{
  public UkatonMission ukatonMission;

  public void onQuaternionUpdate()
  {
    Quaternion q = ukatonMission.motionData.quaternion;
    // Debug.Log(q);
    Debug.Log(q.eulerAngles);
    transform.rotation = ukatonMission.motionData.quaternion;
  }
}
