using System.Collections.Generic;
using ColorChargeTD.Data;
using ColorChargeTD.Domain;
using UnityEngine;

namespace ColorChargeTD.Battle
{
    public sealed class BattlePresentationSystem
    {
        private readonly Dictionary<TowerRuntimeModel, GameObject> towerViews = new Dictionary<TowerRuntimeModel, GameObject>();
        private readonly Dictionary<EnemyRuntimeModel, GameObject> enemyViews = new Dictionary<EnemyRuntimeModel, GameObject>();
        private readonly Dictionary<PlacedStructureRuntimeModel, GameObject> structureViews =
            new Dictionary<PlacedStructureRuntimeModel, GameObject>();

        private Transform root;
        private Transform towerRoot;
        private Transform structureRoot;
        private Transform enemyRoot;
        private Transform projectilesRoot;
        private Transform pathOverlaysRoot;
        private readonly MaterialPropertyBlock pathMarkerPropertyBlock = new MaterialPropertyBlock();
        private readonly MaterialPropertyBlock enemyVisualPropertyBlock = new MaterialPropertyBlock();

        private static readonly int EnemyBaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EnemyColorId = Shader.PropertyToID("_Color");

        #region Lifecycle
        public void Initialize(Transform parent)
        {
            Dispose();

            root = CreateChild(parent, "BattlePresentation");
            towerRoot = CreateChild(root, "Towers");
            structureRoot = CreateChild(root, "PlacedStructures");
            enemyRoot = CreateChild(root, "Enemies");
            projectilesRoot = CreateChild(root, "Projectiles");
        }

        public void BuildPathRouteMarkers(
            LevelLayoutRuntimeDefinition layout,
            WaveDefinition wave,
            GameObject pathMarkerPrefab,
            float markerSpacing,
            float markerYOffset)
        {
            if (root == null)
            {
                return;
            }

            if (pathOverlaysRoot != null)
            {
                Object.Destroy(pathOverlaysRoot.gameObject);
                pathOverlaysRoot = null;
            }

            if (pathMarkerPrefab == null || wave == null)
            {
                return;
            }

            pathOverlaysRoot = CreateChild(root, "PathOverlays");
            PathRouteMarkerOverlayBuilder.Build(
                pathOverlaysRoot,
                pathMarkerPrefab,
                layout,
                wave,
                markerSpacing,
                markerYOffset,
                pathMarkerPropertyBlock);
        }

        public void Dispose()
        {
            ClearViews(towerViews);
            ClearViews(structureViews);
            ClearViews(enemyViews);

            if (root != null)
            {
                Object.Destroy(root.gameObject);
            }

            root = null;
            towerRoot = null;
            structureRoot = null;
            enemyRoot = null;
            projectilesRoot = null;
            pathOverlaysRoot = null;
        }
        #endregion

        #region Events
        public void NotifyEnemyDamaged(EnemyRuntimeModel enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (!enemyViews.TryGetValue(enemy, out GameObject enemyView) || enemyView == null)
            {
                return;
            }

            EnemyWorldPresenter presenter = enemyView.GetComponent<EnemyWorldPresenter>();
            if (presenter != null)
            {
                presenter.NotifyHit();
            }
        }

        public void NotifyTowerFired(TowerRuntimeModel tower, EnemyRuntimeModel enemy, float travelTime)
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
                    projectileView.BeginFlight(start, enemyView.transform, travelTime, tower.Definition.ProjectileArcPeakHeight);
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
        public void Sync(
            List<TowerRuntimeModel> towers,
            List<EnemyRuntimeModel> enemies,
            List<PlacedStructureRuntimeModel> placedStructures)
        {
            SyncTowers(towers);
            SyncPlacedStructures(placedStructures);
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

        private void SyncPlacedStructures(List<PlacedStructureRuntimeModel> placedStructures)
        {
            if (structureRoot == null)
            {
                return;
            }

            HashSet<PlacedStructureRuntimeModel> active = new HashSet<PlacedStructureRuntimeModel>();

            if (placedStructures == null)
            {
                RemoveMissingViews(structureViews, active);
                return;
            }

            for (int i = 0; i < placedStructures.Count; i++)
            {
                PlacedStructureRuntimeModel model = placedStructures[i];
                if (model == null || model.Definition == null)
                {
                    continue;
                }

                active.Add(model);

                if (!structureViews.TryGetValue(model, out GameObject view) || view == null)
                {
                    Color fallback = model.Kind == BuildSlotKind.RoadTrap
                        ? new Color(1f, 0.5f, 0.2f, 1f)
                        : new Color(0.35f, 0.65f, 1f, 1f);

                    view = SpawnView(
                        model.Definition.Prefab,
                        structureRoot,
                        "StructureFallback",
                        PrimitiveType.Cube,
                        new Vector3(0.6f, 0.35f, 0.6f),
                        fallback);

                    structureViews[model] = view;
                }

                view.transform.SetPositionAndRotation(model.Slot.Position, Quaternion.identity);
                SyncAuxiliaryIncomeRing(view, model);
            }

            RemoveMissingViews(structureViews, active);
        }

        private static void SyncAuxiliaryIncomeRing(GameObject view, PlacedStructureRuntimeModel model)
        {
            if (view == null || model == null)
            {
                return;
            }

            StructureAuxiliaryIncomeRingPresenter ring = view.GetComponent<StructureAuxiliaryIncomeRingPresenter>();
            AuxiliaryBuildingDefinition aux = model.Definition as AuxiliaryBuildingDefinition;
            bool wantRing = model.Kind == BuildSlotKind.Auxiliary && aux != null && aux.HasPeriodicIncome;

            if (!wantRing)
            {
                if (ring != null)
                {
                    ring.Hide();
                }

                return;
            }

            if (ring == null)
            {
                ring = view.AddComponent<StructureAuxiliaryIncomeRingPresenter>();
            }

            ring.Setup(model, aux);
            ring.RefreshFill();
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

                    float visualScale = enemy.Definition.VisualScale;
                    enemyView.transform.localScale = enemyView.transform.localScale * visualScale;

                    ApplyEnemyChargeTint(enemyView, enemy.Definition.Color);

                    enemyViews[enemy] = enemyView;
                }

                enemyView.transform.position = enemy.Position;

                EnemyWorldPresenter enemyPresenter = enemyView.GetComponent<EnemyWorldPresenter>();
                if (enemyPresenter == null)
                {
                    enemyPresenter = enemyView.AddComponent<EnemyWorldPresenter>();
                }

                EnemyPresentationProfile profile = enemy.Definition != null ? enemy.Definition.PresentationProfile : null;
                if (profile != null)
                {
                    enemyPresenter.SetPresentationProfile(profile);
                }

                enemyPresenter.Sync(enemy, Time.deltaTime);

                EnemyHealthWorldBar healthBar = enemyView.GetComponent<EnemyHealthWorldBar>();
                if (healthBar == null)
                {
                    healthBar = enemyView.AddComponent<EnemyHealthWorldBar>();
                }

                healthBar.Apply(enemy);
            }

            RemoveMissingViews(enemyViews, activeEnemies);
        }
        #endregion

        #region EnemyVisualTint
        private void ApplyEnemyChargeTint(GameObject root, ColorCharge charge)
        {
            if (root == null)
            {
                return;
            }

            Color color = charge.ToUnityColor();
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                enemyVisualPropertyBlock.Clear();
                renderer.GetPropertyBlock(enemyVisualPropertyBlock);
                enemyVisualPropertyBlock.SetColor(EnemyBaseColorId, color);
                enemyVisualPropertyBlock.SetColor(EnemyColorId, color);
                renderer.SetPropertyBlock(enemyVisualPropertyBlock);
            }
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
