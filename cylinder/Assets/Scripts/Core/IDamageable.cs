namespace Cylinder.Core
{
    /// <summary>
    /// 피해를 받을 수 있는 유닛 인터페이스
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// 피해 받기
        /// </summary>
        void TakeDamage();
    }
}
