namespace Tempest.Weapons
{
    public class PlayerLoadout
    {
        public WeaponDefinition PrimaryWeapon { get; }
        public WeaponDefinition SecondaryWeapon { get; }
        public DeployableDefinition Consumable { get; }

        public PlayerLoadout(WeaponDefinition primary, WeaponDefinition secondary, DeployableDefinition consumable = null)
        {
            PrimaryWeapon = primary;
            SecondaryWeapon = secondary;
            Consumable = consumable;
        }

        public bool IsValid()
        {
            return PrimaryWeapon != null
                && SecondaryWeapon != null
                && PrimaryWeapon.slot == WeaponSlot.Primary
                && SecondaryWeapon.slot == WeaponSlot.Secondary;
        }
    }
}
