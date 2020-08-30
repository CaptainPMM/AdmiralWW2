using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ships.ShipSystems.Armaments;
using Projectiles;

namespace Ships.ShipSystems {
    public class TargetingSystem : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private AnimationCurve distanceToElevationHeuristic = null;
        [SerializeField] private AnimationCurve elevationToFlightTimeHeuristic = null;
        [SerializeField] private AnimationCurve bestDistanceToRotOffsetDispersionHeuristic = null;
        [SerializeField] private AnimationCurve bestDistanceToElevOffsetDispersionHeuristic = null;
        [SerializeField] private float waitForTurretsCheckInterval = 1f;
        [SerializeField] private float waitForProjectileHitsCheckInterval = 3f;
        [SerializeField] private float volleyTurretFireDelay = 0.5f;
        [SerializeField] private Ship ship = null;

        [Header("Current state")]
        [SerializeField] private GameObject targetGO = null;

        private ITarget target = null;

        public GameObject TargetGO => targetGO;
        public ITarget Target {
            get => target;
            set {
                target = value;
                targetGO = value?.GameObject;
                if (Application.isPlaying) ResetTargeting();
            }
        }

        private Coroutine targetingRoutine = null;

        [SerializeField] private float directDist = 0f;
        [SerializeField] private float directEstimElev = 0f;
        [SerializeField] private float estimFlightTime = 0f;
        [SerializeField] private Vector3 estimTargetPos = Vector3.zero;
        [SerializeField] private float estimDist = 0f;
        [SerializeField] private float estimRot = 0f;
        [SerializeField] private float estimElev = 0f;
        [SerializeField] private float bestDist = float.MaxValue;
        [SerializeField] private float bestRotOffset = 0f;
        [SerializeField] private float bestElevOffset = 0f;
        [SerializeField] private float currRotOffsetDispersion = 0f;
        [SerializeField] private float currElevOffsetDispersion = 0f;
        [SerializeField] private List<TurretOffsetSetting> currTurretOffsets = new List<TurretOffsetSetting>();
        [SerializeField] private int lastVolleyProjectiles = 0;
        [SerializeField] private List<ProjectileHitResult> lastVolleyResults = new List<ProjectileHitResult>();

        private void Start() {
            if (targetGO != null) Target = targetGO.GetComponent<ITarget>();
        }

        private void ResetTargeting() {
            if (targetingRoutine != null) {
                StopCoroutine(targetingRoutine);
                targetingRoutine = null;
            }

            ship.Armament.SetEngageGunTurrets(false);
            ship.Armament.SetGunTurretsStabilization(false);

            // Reset all values
            directDist = 0f;
            directEstimElev = 0f;
            estimFlightTime = 0f;
            estimTargetPos = Vector3.zero;
            estimDist = 0f;
            estimRot = 0f;
            estimElev = 0f;
            bestDist = float.MaxValue;
            bestRotOffset = 0f;
            bestElevOffset = 0f;
            currRotOffsetDispersion = 0f;
            currElevOffsetDispersion = 0f;
            currTurretOffsets.Clear();
            lastVolleyProjectiles = 0;
            lastVolleyResults.Clear();

            if (target != null) targetingRoutine = StartCoroutine(TargetingRoutine());
        }

        private IEnumerator TargetingRoutine() {
            ship.Armament.SetEngageGunTurrets(true);
            ship.Armament.SetGunTurretsStabilization(true);
            bestDist = Vector3.Distance(ship.WorldPos, target.WorldPos);

            while (target != null) {
                // Wait until all turrets reloaded and aimed...
                while (!ship.Armament.GunTurretsReadyAndAimed()) yield return new WaitForSecondsRealtime(waitForTurretsCheckInterval);

                // Calculate estimated values
                directDist = Vector3.Distance(ship.WorldPos, target.WorldPos);
                directEstimElev = distanceToElevationHeuristic.Evaluate(directDist);
                estimFlightTime = elevationToFlightTimeHeuristic.Evaluate(directEstimElev);
                estimTargetPos = target.WorldPos + target.Velocity * (estimFlightTime / Time.fixedDeltaTime);
                estimDist = Vector3.Distance(ship.WorldPos, estimTargetPos);
                estimElev = distanceToElevationHeuristic.Evaluate(estimDist);
                estimRot = Vector3.SignedAngle(ship.transform.forward, (target.WorldPos - ship.WorldPos).normalized, Vector3.up);

                // Calculate offset values for each turret
                currTurretOffsets.Clear();
                currRotOffsetDispersion = bestDistanceToRotOffsetDispersionHeuristic.Evaluate(bestDist);
                currElevOffsetDispersion = bestDistanceToElevOffsetDispersionHeuristic.Evaluate(bestDist);
                for (int i = 0; i < ship.Armament.GunTurrets.Count; i++) {
                    float rotOffset = bestRotOffset + Mathf.Lerp(-currRotOffsetDispersion, currRotOffsetDispersion, (float)i / (float)(ship.Armament.GunTurrets.Count - 1)); ;
                    float elevOffset = bestElevOffset + Mathf.Lerp(-currElevOffsetDispersion, currElevOffsetDispersion, (float)i / (float)(ship.Armament.GunTurrets.Count - 1)); ;

                    ship.Armament.GunTurrets[i].TargetTurretRotation = estimRot + rotOffset;
                    ship.Armament.GunTurrets[i].TargetGunsElevation = estimElev + elevOffset;
                    if (ship.Armament.GunTurrets[i].SternTurret) {
                        ship.Armament.GunTurrets[i].TargetTurretRotation = 90f + (90f - ship.Armament.GunTurrets[i].TargetTurretRotation);
                    }

                    currTurretOffsets.Add(new TurretOffsetSetting { turret = ship.Armament.GunTurrets[i], rotOffset = rotOffset, elevOffset = elevOffset });
                }

                // Wait until all turrets reloaded and aimed...
                while (!ship.Armament.GunTurretsReadyAndAimed()) yield return new WaitForSecondsRealtime(waitForTurretsCheckInterval);

                // Fire volley!
                lastVolleyProjectiles = 0;
                lastVolleyResults.Clear();
                foreach (GunTurret gt in ship.Armament.GunTurrets) {
                    gt.OnFire += TurretFiredHandler;
                    gt.Fire();
                    lastVolleyProjectiles += gt.NumGuns;
                    yield return new WaitForSecondsRealtime(volleyTurretFireDelay);
                }

                // Wait until all shots hit...
                while (lastVolleyResults.Count < lastVolleyProjectiles) yield return new WaitForSecondsRealtime(waitForProjectileHitsCheckInterval);

                // Find best shot and apply offset values
                bestDist = float.MaxValue;
                GunTurret bestTurret = null;
                foreach (ProjectileHitResult phr in lastVolleyResults) {
                    float dist = Vector3.Distance(phr.hitWorldPos, target.WorldPos);
                    if (dist < bestDist) {
                        bestDist = dist;
                        bestTurret = phr.fromTurret;
                    }
                }

                if (bestTurret) {
                    TurretOffsetSetting tos = currTurretOffsets.Find(to => to.turret == bestTurret);
                    bestRotOffset = tos.rotOffset;
                    bestElevOffset = tos.elevOffset;
                }
            }
        }

        private void TurretFiredHandler(GunTurret turret, Projectile[] projectiles) {
            turret.OnFire -= TurretFiredHandler;
            foreach (Projectile p in projectiles) {
                p.OnHit += ProjectileHitHandler;
            }
        }

        private void ProjectileHitHandler(Projectile projectile, Vector3 worldPos) {
            projectile.OnHit -= ProjectileHitHandler;
            lastVolleyResults.Add(new ProjectileHitResult { fromTurret = projectile.FromTurret, hitWorldPos = worldPos });
        }

        [System.Serializable]
        public struct TurretOffsetSetting {
            public GunTurret turret;
            public float rotOffset;
            public float elevOffset;
        }

        [System.Serializable]
        public struct ProjectileHitResult {
            public GunTurret fromTurret;
            public Vector3 hitWorldPos;
        }
    }
}