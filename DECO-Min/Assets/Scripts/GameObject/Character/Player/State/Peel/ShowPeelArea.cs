using UnityEngine;

public class ShowPeelArea : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private PlayerPeel _playerPeel;

    void OnDrawGizmos()
    {
        if (_playerPeel != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(_player.transform.position, _playerPeel.GetPeelableRange());

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_player.transform.position, _playerPeel.GetPeelableChainRange());
        }
    }
}
