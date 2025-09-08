using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace angerRoom
{
    /// <summary>
    /// ���� ������ �-HitZone (�����). ���� N ������ ���� ��� ������.
    /// ���� �� ���� ������ �� ��� (CapsuleCollider isTrigger=true + Rigidbody isKinematic=true).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BagVanishOnHits : MonoBehaviour
    {
        [Header("Hits")]
        [Tooltip("��� ������ �� ���� ����")]
        public int hitsToVanish = 3;
        [Tooltip("�� ������ ����� �������� ��� ���� ��� ��� ����� ������")]
        public float minRelativeSpeed = 0.6f;
        [Tooltip("���-���� �� ������� (��� ����� ������ �� ���� �����)")]
        public float perHitterCooldown = 0.20f;

        [Header("Who can hit")]
        public LayerMask hitters = ~0;          // ����� ����� ��� ����� (����: Hitter)
        public List<string> allowedTags = new(); // ��� = �� ���� �����

        [Header("What to hide")]
        [Tooltip("���� ��� ����� �����/�����. �� ��� - ����� �-root �� ��������")]
        public GameObject bagRoot;

        public enum VanishAction { SetInactive, DisableRenderersAndColliders, Destroy }
        [Tooltip("��� ������ �� ��� ���� ����")]
        public VanishAction vanishAction = VanishAction.SetInactive;

        [Header("SFX / Events (�� ����)")]
        public AudioSource hitSfx;
        public UnityEvent onHit;
        public UnityEvent onVanish;

        // ��� ����
        private int _hits = 0;
        private Rigidbody _myRb;
        private readonly Dictionary<int, float> _lastHitTimeById = new();

        void Awake()
        {
            // ����� ��� �����
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            _myRb = GetComponent<Rigidbody>(); // ���� ����� RB ������ �� �-HitZone
            if (!bagRoot) bagRoot = transform.root.gameObject;
        }

        void OnTriggerEnter(Collider other)
        {
            var otherGo = other.attachedRigidbody ? other.attachedRigidbody.gameObject : other.gameObject;
            if (!IsAllowed(otherGo)) return;

            // ����-���� ��-���
            int id = otherGo.GetInstanceID();
            float now = Time.time;
            if (_lastHitTimeById.TryGetValue(id, out float lastT) && now - lastT < perHitterCooldown)
                return;

            // ������ �����
            Vector3 vOther = other.attachedRigidbody ? other.attachedRigidbody.linearVelocity : Vector3.zero;
            Vector3 vMine = _myRb ? _myRb.linearVelocity : Vector3.zero;
            float relSpeed = (vOther - vMine).magnitude;
            if (relSpeed < minRelativeSpeed)
                return;

            _lastHitTimeById[id] = now;

            _hits++;
            if (hitSfx) hitSfx.Play();
            onHit?.Invoke();

            if (_hits >= hitsToVanish)
                Vanish();
        }

        bool IsAllowed(GameObject go)
        {
            // �����
            if ((hitters.value & (1 << go.layer)) == 0) return false;
            // ����� (���������)
            if (allowedTags != null && allowedTags.Count > 0 && !allowedTags.Contains(go.tag)) return false;
            return true;
        }

        void Vanish()
        {
            onVanish?.Invoke();

            if (!bagRoot) bagRoot = transform.root.gameObject;

            switch (vanishAction)
            {
                case VanishAction.SetInactive:
                    bagRoot.SetActive(false);
                    break;

                case VanishAction.DisableRenderersAndColliders:
                    foreach (var r in bagRoot.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
                    foreach (var c in bagRoot.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                    var rb = bagRoot.GetComponentInChildren<Rigidbody>();
                    if (rb) rb.isKinematic = true;
                    break;

                case VanishAction.Destroy:
                    Destroy(bagRoot);
                    break;
            }
        }
    }
}
