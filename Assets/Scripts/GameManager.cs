using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ships;
using Ships.ShipSystems;
using Ships.ShipSystems.Armaments;
using Ships.DamageZones;
using Projectiles;
using Cam;
using Net;
using Net.MessageTypes;
using SuperNet.Transport;

public class GameManager : MonoBehaviour {
    public static GameManager Inst { get; private set; }

    private const float DISTRIBUTE_RANDOM_SEED_INTERVAL = 1f;

    [Header("Setup")]
    [SerializeField] private List<Player> players = new List<Player>();
    [SerializeField] private List<PlayerFleet> fleets = new List<PlayerFleet>();
    [SerializeField] private PlayerTag thisPlayerTag = PlayerTag.Player0;
    [SerializeField] private CamController camController = null;

    [Header("Current state")]
    [SerializeField] private Player thisPlayer = null;

    [Header("MP settings")]
    [SerializeField] private bool stopGameSync = false;
    [SerializeField] private bool syncGames = true;
    [SerializeField] private float syncGamesInterval = 30f;

    private bool allPlayersReady = false;

    public static PlayerTag ThisPlayerTag => Inst.thisPlayerTag;
    public static Player ThisPlayer => Inst.thisPlayer;
    public static Player GetPlayer(PlayerTag tag) { return Inst.players.Find(p => p.Tag == tag); }

    private void Awake() {
        Inst = this;

        thisPlayerTag = Global.State.playerTag;
        thisPlayer = GetPlayer(thisPlayerTag);

        // Move camera to first ship in fleet
        Vector3 shipPos = fleets.Find(f => f.PlayerTag == thisPlayerTag).Ships[0].transform.position;
        camController.transform.position = new Vector3(shipPos.x, 0f, shipPos.z);
    }

    private void Start() {
        fleets.ForEach(f => f.Ships.ForEach(s => {
            s.OnSinking += ShipSinkingHandler;
        }));

        if (P2PManager.IsMPActive()) {
            // Stop game until all players are loaded
            Time.timeScale = 0f;
            MessageHandler.Inst.OnReceivedGameReady += ReceiveGameReadyHandler;
            StartCoroutine(WaitForPeerGameReady());
        }
    }

    private IEnumerator WaitForPeerGameReady() {
        do {
            P2PManager.Inst.Send(new MTGameReady());
            yield return new WaitForSecondsRealtime(0.05f);
        } while (!allPlayersReady);

        // Start game
        if (Global.State.isRandomSeedHost) {
            StartCoroutine(DistributeRandomSeed());
        }
        StartCoroutine(SyncGamesRoutine());
        Time.timeScale = 1f;
    }

    private void ReceiveGameReadyHandler(Peer peer) {
        MessageHandler.Inst.OnReceivedGameReady -= ReceiveGameReadyHandler;
        allPlayersReady = true;
    }

    private IEnumerator DistributeRandomSeed() {
        while (P2PManager.Inst.Peer.Connected) {
            int seed = (int)System.DateTime.Now.Ticks;
            P2PManager.Inst.Send(new MTRandomSeed { Seed = seed });
            SafeRandom.Seed = seed;
            yield return new WaitForSecondsRealtime(DISTRIBUTE_RANDOM_SEED_INTERVAL);
        }
    }

    private IEnumerator SyncGamesRoutine() {
        while (!stopGameSync) {
            yield return new WaitForSecondsRealtime(syncGamesInterval);
            if (syncGames) {
                List<Projectile> projectiles = new List<Projectile>();
                for (int i = 0; i < ProjectileManager.Inst.transform.childCount; i++) {
                    projectiles.Add(ProjectileManager.Inst.transform.GetChild(i).GetComponent<Projectile>());
                }
                List<Ship> ships = new List<Ship>();
                foreach (PlayerFleet fleet in fleets) {
                    ships.AddRange(fleet.Ships);
                    ships.AddRange(fleet.SunkShips);
                }
                P2PManager.Inst.Send(new MTSyncGame(projectiles.ToArray(), ships.ToArray()));
            }
        }
    }

    private void ShipSinkingHandler(Ship ship) {
        ship.OnSinking -= ShipSinkingHandler;

        foreach (PlayerFleet fleet in fleets) {
            int shipIndex = fleet.Ships.IndexOf(ship);
            if (shipIndex != -1) {
                fleet.Ships.RemoveAt(shipIndex);
                fleet.SunkShips.Add(ship);

                if (fleet.Ships.Count == 0) {
                    Debug.Log("Fleet destroyed: " + fleet.Name + "; PlayerTag: " + fleet.PlayerTag + ", Player name: " + GetPlayer(fleet.PlayerTag).Name);
                }
                return;
            }
        }
    }

    public void SetShipChadburn(PlayerTag playerTag, ID shipID, Autopilot.ChadburnSetting chadburnSetting) {
        PlayerFleet fleet = fleets.Find(f => f.PlayerTag == playerTag);
        if (fleet != null) {
            Ship ship = fleet.Ships.Find(s => s.ID == shipID);
            if (ship != null) {
                ship.Autopilot.Chadburn = chadburnSetting;
            } else Debug.LogWarning("Ship with ID " + shipID + " not found");
        } else Debug.LogWarning("Fleet of player tag " + playerTag + " not found");
    }

    public void SetShipCourse(PlayerTag playerTag, ID shipID, ushort course) {
        PlayerFleet fleet = fleets.Find(f => f.PlayerTag == playerTag);
        if (fleet != null) {
            Ship ship = fleet.Ships.Find(s => s.ID == shipID);
            if (ship != null) {
                ship.Autopilot.Course = course;
            } else Debug.LogWarning("Ship with ID " + shipID + " not found");
        } else Debug.LogWarning("Fleet of player tag " + playerTag + " not found");
    }

    public void SetShipTarget(PlayerTag playerTag, ID shipID, bool hasTarget, ID targetShipID) {
        PlayerFleet fleet = fleets.Find(f => f.PlayerTag == playerTag);
        if (fleet != null) {
            Ship ship = fleet.Ships.Find(s => s.ID == shipID);
            if (ship != null) {
                if (hasTarget) {
                    Ship target = FindShipByID(targetShipID);
                    if (target != null) {
                        ship.Targeting.Target = target;
                    } else Debug.LogWarning(("Target ship ID " + targetShipID + " not found"));
                } else ship.Targeting.Target = null;
            } else Debug.LogWarning("Ship with ID " + shipID + " not found");
        } else Debug.LogWarning("Fleet of player tag " + playerTag + " not found");
    }

    public Projectile FindProjectileByID(ID id) {
        for (int i = 0; i < ProjectileManager.Inst.transform.childCount; i++) {
            Projectile p = ProjectileManager.Inst.transform.GetChild(i).GetComponent<Projectile>();
            if (p.ID == id) return p;
        }
        return null;
    }

    public Ship FindShipByID(ID id) {
        foreach (PlayerFleet fleet in fleets) {
            foreach (Ship ship in fleet.Ships) {
                if (ship.ID == id) return ship;
            }
        }
        return null;
    }

    public void SyncGames(MTSyncGame sync) {
        // Sync projectiles
        for (int i = 0; i < sync.NumProjectiles; i++) {
            Projectile p = FindProjectileByID(sync.ProjectileIDs[i]);
            if (p) {
                p.transform.position += (sync.ProjectilePositions[i] - p.transform.position) * 0.5f;
                p.transform.rotation = Quaternion.Lerp(p.transform.rotation, Quaternion.Euler(sync.ProjectileRotations[i]), 0.5f);
                p.Velocity = Vector3.Lerp(p.Velocity, sync.ProjectileVelocities[i], 0.5f);
            } else Debug.LogWarning("Sync projectile: could not find id " + sync.ProjectileIDs[i]);
        }

        // Sync ships
        uint waterIngressSectionsCounter = 0;
        uint gunTurretsCounter = 0;
        uint damageZoneCounter = 0;
        uint damageZoneDamagesCounter = 0;

        for (int i = 0; i < sync.NumShips; i++) {
            Ship s = FindShipByID(sync.ShipIDs[i]);
            bool counterUpdated = false;
            if (s != null && !s.Equals(null)) {
                if (sync.ShipsHitpoints[i] <= 0 && !s.Destroyed) s.DamageHull(float.MaxValue);
                else if (!s.Destroyed) {
                    s.HullHitpoints = Mathf.Lerp(s.HullHitpoints, sync.ShipsHitpoints[i], 0.5f);
                    s.transform.position += (sync.ShipPositions[i] - s.transform.position) * 0.5f;
                    s.transform.rotation = Quaternion.Lerp(s.transform.rotation, Quaternion.Euler(sync.ShipRotations[i]), 0.5f);
                    s.Velocity = Vector3.Lerp(s.Velocity, sync.ShipVelocities[i], 0.5f);
                    for (byte waterIngressSectionIndex = 0; waterIngressSectionIndex < sync.ShipsNumWaterIngressSections[i]; waterIngressSectionIndex++) {
                        Ship.WaterIngressSection section = s.WaterIngressSections.Find(wis => wis.sectionID == sync.WaterIngressSectionsIDs[waterIngressSectionsCounter]);
                        section.numHoles = Mathf.CeilToInt(Mathf.Lerp(section.numHoles, sync.WaterIngressNumHoles[waterIngressSectionsCounter], 0.5f));
                        section.waterLevel = Mathf.Lerp(section.waterLevel, sync.WaterIngressWaterLevels[waterIngressSectionsCounter++], 0.5f);
                    }
                    if (s.PlayerTag != thisPlayerTag) {
                        s.Autopilot.Chadburn = sync.ShipChadburnSettings[i];
                        s.Autopilot.Course = sync.ShipCourses[i];
                        if (sync.ShipsHasTarget[i]) s.Targeting.Target = FindShipByID(sync.ShipsTargetShipID[i]);
                        else s.Targeting.Target = null;
                    }
                    for (byte gunTurretIndex = 0; gunTurretIndex < sync.ShipsNumGunTurrets[i]; gunTurretIndex++) {
                        GunTurret t = s.Armament.GunTurrets.Find(gt => gt.ID == sync.GunTurretIDs[gunTurretsCounter]);
                        if (sync.GunTurretsDisabled[gunTurretsCounter]) t.Disable();
                        t.ReloadProgress = Mathf.Lerp(t.ReloadProgress, sync.GunTurretsReloadProgress[gunTurretsCounter], 0.5f);
                        t.TurretRotation = Mathf.Lerp(t.TurretRotation, sync.GunTurretsRotation[gunTurretsCounter], 0.5f);
                        t.TargetTurretRotation = Mathf.Lerp(t.TargetTurretRotation, sync.GunTurretsTargetRotation[gunTurretsCounter], 0.5f);
                        t.GunsElevation = Mathf.Lerp(t.GunsElevation, sync.GunTurretsElevation[gunTurretsCounter], 0.5f);
                        t.TargetGunsElevation = Mathf.Lerp(t.TargetGunsElevation, sync.GunTurretsTargetElevation[gunTurretsCounter], 0.5f);
                        t.StabilizationAngle = Mathf.Lerp(t.StabilizationAngle, sync.GunTurretsStabilizationAngle[gunTurretsCounter++], 0.5f);
                    }
                    for (byte damageZoneIndex = 0; damageZoneIndex < sync.ShipNumDamageZones[i]; damageZoneIndex++) {
                        DamageZone dz = s.DamageZones.Find(sdz => sdz.ID == sync.ShipDamageZoneIDs[damageZoneCounter]);
                        for (byte damageIndex = 0; damageIndex < sync.DamageZoneNumDamages[damageZoneCounter]; damageIndex++) {
                            DamageType dt = sync.DamageZoneDamages[damageZoneDamagesCounter++];
                            if (!dz.Damages.Contains(dt)) dz.Damages.Add(dt);
                        }
                        damageZoneCounter++;
                    }
                    counterUpdated = true;
                }
            } else Debug.LogWarning("Sync ship: could not find id " + sync.ShipIDs[i]);

            // Update counter in case they did not get updated inside the ifs
            if (!counterUpdated) {
                waterIngressSectionsCounter += sync.ShipsNumWaterIngressSections[i];
                gunTurretsCounter += sync.ShipsNumGunTurrets[i];
                for (int k = 0; k < sync.ShipNumDamageZones[i]; k++) {
                    damageZoneDamagesCounter += sync.DamageZoneNumDamages[damageZoneCounter];
                    damageZoneCounter++;
                }
            }
        }
    }
}