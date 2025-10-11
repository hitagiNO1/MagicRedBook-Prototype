using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CamZoneLyla : MonoBehaviour
{
  #region Inspector

  [SerializeField]
  private CinemachineVirtualCamera virtualCamera = null;
    public SmoothFollow RedBookFollow;
    public FollowAI LylaFollow;

    #endregion


    #region MonoBehaviour

    private void Start ()
  {
    virtualCamera.enabled = false;
  }

  private void OnTriggerEnter (Collider other)
  {
    if ( other.CompareTag("Player") )
      virtualCamera.enabled = true;
  }

  private void OnTriggerExit (Collider other)
  {
    if ( other.CompareTag("Player"))
        {
            virtualCamera.enabled = false;
            RedBookFollow.SetFollow(true);
            LylaFollow.enabled = true; // 啟用 FollowAI
            Destroy(virtualCamera.gameObject);
        }
  }

  private void OnValidate ()
  {
    GetComponent<Collider>().isTrigger = true;
  }

  #endregion
}