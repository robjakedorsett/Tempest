using UnityEngine;

namespace Tempest.Enemies
{
    public class EnemyBootstrapper : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;

        private void Start()
        {
            var health = GetComponent<EnemyHealth>();
            var agent = GetComponent<NaturalAgent>();

            health.Initialize(definition);
            agent.Initialize(definition.moveSpeed);
        }
    }
}
