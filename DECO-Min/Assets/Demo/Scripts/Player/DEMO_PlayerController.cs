using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class DEMO_PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed    = 5f;
    [SerializeField] private float runSpeed     = 9f;
    [SerializeField] private float rotateSpeed  = 12f;
    [SerializeField] private float gravity      = -20f;
    [SerializeField] private float jumpHeight   = 1.2f;
    [SerializeField] private float groundRadius = 0.28f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float peelRadius   = 3.5f;  // enlarged

    private CharacterController cc;
    private Transform           camTf;
    private Vector3             vVel;
    private bool                grounded;
    private Transform           groundCheck;

    // Expose velocity so StickerThrower can offset spawn point
    public Vector3 Velocity => cc != null ? cc.velocity : Vector3.zero;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        var gc = new GameObject("_GroundCheck");
        gc.transform.SetParent(transform);
        gc.transform.localPosition = new Vector3(0, -0.92f, 0);
        groundCheck = gc.transform;
    }

    private void Update()
    {
        if (camTf == null)
            camTf = Camera.main != null ? Camera.main.transform : null;

        UpdateGround();
        Move();
        Jump();
        Gravity();
        Interact();
    }

    private void UpdateGround()
    {
        grounded = Physics.CheckSphere(groundCheck.position, groundRadius,
                                       groundMask, QueryTriggerInteraction.Ignore);
        if (grounded && vVel.y < 0) vVel.y = -2f;
    }

    private void Move()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        float h = 0, v = 0;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1;
        if (h == 0 && v == 0) return;

        Vector3 dir;
        if (camTf != null)
        {
            var f = camTf.forward; f.y = 0; f.Normalize();
            var r = camTf.right;   r.y = 0; r.Normalize();
            dir = (f * v + r * h).normalized;
        }
        else dir = new Vector3(h, 0, v).normalized;

        float spd = kb.leftShiftKey.isPressed ? runSpeed : walkSpeed;
        transform.rotation = Quaternion.Slerp(transform.rotation,
                             Quaternion.LookRotation(dir), rotateSpeed * Time.deltaTime);
        cc.Move(dir * spd * Time.deltaTime);
    }

    private void Jump()
    {
        if (Keyboard.current?.spaceKey.wasPressedThisFrame == true && grounded)
            vVel.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
    }

    private void Gravity()
    {
        vVel.y += gravity * Time.deltaTime;
        cc.Move(vVel * Time.deltaTime);
    }

    // ── Interact (E key) ──────────────────────────────────────────────────────
    private void Interact()
    {
        if (Keyboard.current?.eKey.wasPressedThisFrame != true) return;

        Debug.Log("[PlayerController] E pressed — searching for interactables...");

        // ── Priority 1: retrieve dropped stickers (missed shots on ground/wall) ─
        var dropped = FindObjectsByType<DEMO_DroppedSticker>(FindObjectsSortMode.None);
        DEMO_DroppedSticker nearestDrop = null;
        float minDropDist = float.MaxValue;

        foreach (var ds in dropped)
        {
            float d = Vector3.Distance(transform.position, ds.transform.position);
            if (d < ds.PickupRadius && d < minDropDist)
            { minDropDist = d; nearestDrop = ds; }
        }

        if (nearestDrop != null)
        {
            Debug.Log($"[PlayerController] Retrieving dropped sticker at dist={minDropDist:F2}");
            nearestDrop.Retrieve();
            return; // one action per key press
        }

        // ── Priority 2: peel fully-stickered FieldObjects ─────────────────────
        var all = FindObjectsByType<DEMO_StickerizedObject>(FindObjectsSortMode.None);
        DEMO_StickerizedObject best = null;
        float minDist = float.MaxValue;

        foreach (var so in all)
        {
            if (so.IsBeingPeeled) continue;
            float d = Vector3.Distance(transform.position, so.transform.position);
            Debug.Log($"  ↳ StickerizedObject '{so.name}' dist={d:F2}  radius={peelRadius}");
            if (d < peelRadius && d < minDist) { minDist = d; best = so; }
        }

        if (best != null)
        {
            Debug.Log($"[PlayerController] Peeling '{best.name}'");
            best.StartPeeling(this);
        }
        else
        {
            Debug.Log("[PlayerController] Nothing to interact with.");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, peelRadius);
    }
}
