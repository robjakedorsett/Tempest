public class PlayerContext
{
    public PlayerContext(PlayerMotor motor, PlayerInput input, PlayerStance stance, PlayerHealth health)
    {
        Motor = motor;
        Input = input;
        Stance = stance;
        Health = health;
    }

    public PlayerMotor Motor { get; set; }
    public PlayerInput Input { get; set; }
    public PlayerStance Stance { get; set; }
    public PlayerHealth Health { get; set; }
}
