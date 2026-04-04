using System.Collections.Generic;
using ColorChargeTD.Domain;

namespace ColorChargeTD.Battle
{
    public sealed class ProjectileHitScheduler
    {
        private struct PendingHit
        {
            public EnemyRuntimeModel Target;
            public float Remaining;
            public int Damage;
            public ColorCharge TowerColor;
        }

        private readonly List<PendingHit> pending = new List<PendingHit>(16);
        private readonly DamageResolver damageResolver;

        public ProjectileHitScheduler(DamageResolver resolver)
        {
            damageResolver = resolver;
        }

        public void Clear()
        {
            pending.Clear();
        }

        public void Enqueue(EnemyRuntimeModel target, float delaySeconds, int damage, ColorCharge towerColor)
        {
            if (target == null || delaySeconds <= 0f)
            {
                damageResolver.ApplyDelayedDamage(target, damage, towerColor);
                return;
            }

            pending.Add(new PendingHit
            {
                Target = target,
                Remaining = delaySeconds,
                Damage = damage,
                TowerColor = towerColor,
            });
        }

        public void Tick(float deltaTime)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                PendingHit hit = pending[i];
                hit.Remaining -= deltaTime;
                if (hit.Remaining > 0f)
                {
                    pending[i] = hit;
                    continue;
                }

                pending.RemoveAt(i);
                damageResolver.ApplyDelayedDamage(hit.Target, hit.Damage, hit.TowerColor);
            }
        }
    }
}
