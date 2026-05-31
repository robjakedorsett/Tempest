namespace Tempest.Enemies
{
    public class EnemyContext
    {
        public EnemyContext(EnemyHealth health, NaturalAgent agent, EnemyDefinition definition)
        {
            Health = health;
            Agent = agent;
            Definition = definition;
        }

        public EnemyHealth Health { get; set; }
        public NaturalAgent Agent { get; set; }
        public EnemyDefinition Definition { get; set; }
    }
}
