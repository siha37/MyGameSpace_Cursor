namespace Cylinder.Player
{
    /// <summary>
    /// 플레이어 상태 (기획문서 부록 A)
    /// </summary>
    public enum PlayerState
    {
        Idle,
        Move,
        Run,
        Jump,
        Accel,
        AccelReverse,
        WallStick,
        CeilingStick,
        Stunned,
        Dead
    }
}
