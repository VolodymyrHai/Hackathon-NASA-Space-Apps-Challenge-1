using UnityEngine;

public class BaseDoor : MonoBehaviour
{
    Transform player, left, right;
    Vector3 leftClosed, rightClosed;
    MoonBaseServices services;
    float openAmount;
    public void Configure(Transform target, MoonBaseServices owner, Transform leftLeaf, Transform rightLeaf)
    {
        player = target; services = owner; left = leftLeaf; right = rightLeaf;
        leftClosed = left.localPosition; rightClosed = right.localPosition;
    }
    void Update() { Simulate(Time.deltaTime); }
    void Simulate(float dt)
    {
        if (left == null || right == null) return;
        if (player == null && services != null) player = services.Player;
        bool near = false;
        if (player != null)
        {
            Vector3 p = transform.InverseTransformPoint(player.position);
            near = new Vector2(p.x,p.z).sqrMagnitude < 3.2f*3.2f && Mathf.Abs(p.y) < 3f;
        }
        // Power loss leaves an escape route, rather than trapping the player.
        bool shouldOpen = near || (services != null && !services.Powered);
        openAmount = Mathf.MoveTowards(openAmount,shouldOpen?1f:0f,dt/0.35f);
        float offset = Mathf.SmoothStep(0,1,openAmount)*1.5f;
        left.localPosition = leftClosed - Vector3.right*offset;
        right.localPosition = rightClosed + Vector3.right*offset;
    }
}
