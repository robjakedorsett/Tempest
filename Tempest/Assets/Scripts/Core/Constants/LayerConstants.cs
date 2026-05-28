using UnityEngine;

public static class LayerConstants
{
    public static readonly LayerMask Ground = LayerMask.GetMask("Ground");
    public static readonly LayerMask Player = LayerMask.GetMask("Player");
    public static readonly LayerMask Enemy = LayerMask.GetMask("Enemy");
}
