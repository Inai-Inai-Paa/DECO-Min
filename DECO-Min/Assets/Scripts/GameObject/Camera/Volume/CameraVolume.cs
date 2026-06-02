using UnityEngine;

public class CameraVolume : MonoBehaviour
{
    [System.Serializable]
    public struct CameraParams
    {
        public Vector3 offset;     // プレイヤーからの相対位置
        public float fieldOfView;  // 画角
        public float smoothness;   // カメラの追従速度
    }

    [Header("Volume Settings")]
    public float radius = 20f; // 影響半径

    public bool followPlayer = true; // プレイヤーを追従するかどうかのフラグ

    [Header("Camera Settings")]
    public CameraParams config = new CameraVolume.CameraParams
    {
        offset = new Vector3(0, 5, -10),
        fieldOfView = 60f,
        smoothness = 5f
    };

    // 地面（このオブジェクトの直下）を基準としたカメラの位置と回転を計算する関数
    public bool GetPreviewTransform(out Vector3 position, out Quaternion rotation, out Vector3 targetpos)
    {
        position = transform.position;
        rotation = transform.rotation;
        targetpos = transform.position;

        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;
        float maxDistance = 500f; // 余裕を持たせた距離

        // 通常の物理レイキャスト（ゲーム再生中や、上記で見つからなかった場合のフォールバック）
        // あらゆるレイヤー（~0）を対象に探す
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 groundPoint = hit.point;
            targetpos = groundPoint;
            position = groundPoint + (transform.rotation * config.offset);
            rotation = transform.rotation;
            return true;
        }

        return false; 
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);

        // デバッグ用にレイとカメラ想定位置をSceneビューにも線で描画
        if (GetPreviewTransform(out Vector3 camPos, out _, out Vector3 targetpos))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, camPos);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(targetpos, camPos);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(targetpos, transform.position);
        }
    }
}
