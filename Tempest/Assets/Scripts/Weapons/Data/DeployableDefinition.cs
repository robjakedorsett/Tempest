using UnityEngine;

namespace Tempest.Weapons
{
    [CreateAssetMenu(fileName = "NewDeployable", menuName = "Tempest/Weapons/Deployable Definition")]
    public class DeployableDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string deployableName;
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Config")]
        public GameObject deployablePrefab;
        public float cooldown;
        public int maxCharges;

        [Header("Meta")]
        public bool unlockedByDefault = true;
    }
}
