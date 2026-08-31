namespace Cylinder.Player
{
    /// <summary>
    /// 플레이어 상태 (기획문서 부록 A)
    /// </summary>
    public enum PlayerState
    {
        Idle,               // 정지
        Move,               // 보행 (관성 적용)
        Run,                // 달리기 (공압 충전, Jump와 겹칠 수 있음)
        Jump,               // 점프 / 체공
        Attack,             // 근접 공격 (지상·공중, 악셀·부착 중 불가)
        Accel,              // 악셀 대쉬 중 (중력 없음, 공격 입력 무시)
        AccelReverse,       // 역분사로 대쉬 중단
        WallStick,          // 벽 부착 (1s, 이탈은 악셀/점프, 공격 불가)
        CeilingStick,       // 천장 부착 (1s, 이탈은 악셀/점프, 공격 불가)
        Dead                // 사망
    }
}
