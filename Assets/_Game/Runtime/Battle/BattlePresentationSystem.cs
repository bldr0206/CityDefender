using System.Collections.Generic;
using UnityEngine;

namespace ColorChargeTD.Battle
{
    public sealed class BattlePresentationSystem
    {
        private readonly Dictionary<TowerRuntimeModel, GameObject> towerViews = new Dictionary<TowerRuntimeModel, GameObject>();
        private readonly Dictionary<EnemyRuntimeModel, GameObject> enemyViews = new Dictionary<EnemyRuntimeModel, GameObject>();

        private Transform root;
        private Transform towerRoot;
        private Transform enemyRoot;
        private Transform projectilesRoot;

        #region Lifecycle
        public void Initialize(Transform parent)
        {
            Dispose();

            root = CreateChild(parent, "BattlePresentation");
            towerRoot = CreateChild(root, "Towers");
            enemyRoot = CreateChild(root, "Enemies");
            projectilesRoot = CreateChild(root, "Projectiles");
        }

        public void Dispose()
        {
            ClearViews(towerViews);
            ClearViews(enemyViews);

            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }

            root = null;
            towerRoot = null;
            enemyRoot = null;
            projectilesRoot = null;
        }
        #endregion

        #region Events
        public void NotifyTowerFired(TowerRuntimeModel tower, EnemyRuntimeModel enemy)
        {
            if (tower == null || enemy == null || tower.Definition == null)
            {
                return;
            }

            if (!towerViews.TryGetValue(tower, out GameObject towerView) || towerView == null)
            {
                return;
            }

            if (!enemyViews.TryGetValue(enemy, out GameObject enemyView) || enemyView == null)
            {
                return;
            }

            TowerWorldPresenter presenter = towerView.GetComponent<TowerWorldPresenter>();
            Transform muzzle = presenter != null ? presenter.MuzzleTransform : null;
            Vector3 start = muzzle != null ? muzzle.position : towerView.transform.position + Vector3.up * 0.35f;

            GameObject projectilePrefab = tower.Definition.ProjectilePrefab;
            if (projectilePrefab != null && projectilesRoot != null)
            {
                GameObject projectileInstance = Object.Instantiate(projectilePrefab, start, Quaternion.identity, projectilesRoot);
                TowerProjectileView projectileView = projectileInstance.GetComponent<TowerProjectileView>();
                if (projectileView != null)
                {
                    projectileView.BeginFlight(start, enemyView.transform, tower.Definition.ProjectileTravelTime);
                }
                else
                {
                    Object.Destroy(projectileInstance);
                }
            }

            if (tower.Definition.FireSfx != null)
            {
                AudioSource.PlayClipAtPoint(tower.Definition.FireSfx, start);
            }

            if (tower.Definition.MuzzleVfxPrefab != null)
            {
                Object.Instantiate(tower.Definition.MuzzleVfxPrefab, start, Quaternion.identity, projectilesRoot);
            }
        }
        #endregion

        #region Sync
        public void Sync(List<TowerRuntimeModel> towers, List<EnemyRuntimeModel> enemies)
        {
            SyncTowers(towers);
            SyncEnemies(enemies);
        }

        private void SyncTowers(List<TowerRuntimeModel> towers)
        {
            HashSet<TowerRuntimeModel> activeTowers = new HashSet<TowerRuntimeModel>();

            for (int i = 0; i < towers.Count; i++)
            {
                TowerRuntimeModel tower = towers[i];
                if (tower == null || tower.Definition == null)
                {
                    continue;
                }

                activeTowers.Add(tower);

                if (!towerViews.TryGetValue(tower, out GameObject towerView) || towerView == null)
                {
                    towerView = SpawnView(
                        tower.Definition.Prefab,
                        towerRoot,
                        "TowerFallback",
                        PrimitiveType.Cylinder,
                        new Vector3(0.7f, 0.5f, 0.7f),
                        new Color(0.8f, 0.2f, 0.2f, 1f));

                    towerViews[tower] = towerView;
                }

                towerView.transform.SetPositionAndRotation(tower.Slot.Position, Quaternion.identity);

                TowerWorldPresenter worldPresenter = towerView.GetComponent<TowerWorldPresenter>();
                if (worldPresenter == null)
                {
                    worldPresenter = towerView.AddComponent<TowerWorldPresenter>();
                }

                worldPresenter.ApplyPresentation(tower);
            }

            RemoveMissingViews(towerViews, activeTowers);
        }

        private void SyncEnemies(List<EnemyRuntimeModel> enemies)
        {
            HashSet<EnemyRuntimeModel> activeEnemies = new HashSet<EnemyRuntimeModel>();

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyRuntimeModel enemy = enemies[i];
                if (enemy == null || enemy.Definition == null || !enemy.IsAlive)
                {
                    continue;
                }

                activeEnemies.Add(enemy);

                if (!enemyViews.TryGetValue(enemy, out GameObject enemyView) || enemyView == null)
                {
                    enemyView = SpawnView(
                        enemy.Definition.Prefab,
                        enemyRoot,
                        "EnemyFallback",
                        PrimitiveType.Sphere,
                        new Vector3(0.5f, 0.5f, 0.5f),
                        new Color(0.85f, 0.3f, 0.3f, 1f));

                    enemyViews[enemy] = enemyView;
                }

                enemyView.transform.position = enemy.Position;
            }

            RemoveMissingViews(enemyViews, activeEnemies);
        }
        #endregion

        #region Factory
        private static GameObject SpawnView(
            GameObject prefab,
            Transform parent,
            string fallbackName,
            PrimitiveType fallbackPrimitiveType,
            Vector3 fallbackScale,
            Color fallbackColor)
        {
            GameObject instance;
            if (prefab != null)
            {
                instance = Object.Instantiate(prefab, parent);
            }
            else
            {
                instance = GameObject.CreatePrimitive(fallbackPrimitiveType);
                instance.name = fallbackName;
                instance.transform.SetParent(parent, false);
                instance.transform.localScale = fallbackScale;
                ApplyFallbackColor(instance, fallbackColor);

                Collider collider = instance.GetComponent<Collider>();
                if (collider != null)
                {
                    Object.Destroy(collider);
                }
            }

            instance.transform.localRotation = Quaternion.identity;
            return instance;
        }

        private static void ApplyFallbackColor(GameObject target, Color color)
        {
            MeshRenderer renderer = target.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.material.color = color;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }
        #endregion

        #region Cleanup
        private static void ClearViews<TModel>(Dictionary<TModel, GameObject> views)
        {
            foreach (KeyValuePair<TModel, GameObject> pair in views)
            {
                if (pair.Value != null)
                {
                    Object.Destroy(pair.Value);
                }
            }

            views.Clear();
        }

        private static void RemoveMissingViews<TModel>(Dictionary<TModel, GameObject> views, HashSet<TModel> activeModels)
        {
            List<TModel> staleKeys = new List<TModel>();

            foreach (KeyValuePair<TModel, GameObject> pair in views)
            {
                if (!activeModels.Contains(pair.Key))
                {
                    if (pair.Value != null)
                    {
                        Object.Destroy(pair.Value);
                    }

                    staleKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleKeys.Count; i++)
            {
                views.Remove(staleKeys[i]);
            }
        }
        #endregion
    }
}
