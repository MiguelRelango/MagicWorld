using UnityEngine;
namespace Hearthbound
{
    public sealed class ForestMeshOwner : MonoBehaviour
    {
        public Mesh OwnedMesh;
        void OnDestroy() { if (OwnedMesh != null) Destroy(OwnedMesh); }
    }
}
