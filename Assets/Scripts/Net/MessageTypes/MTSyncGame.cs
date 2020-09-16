using UnityEngine;
using SuperNet.Transport;
using SuperNet.Util;
using Ships;
using Ships.ShipSystems;
using Ships.DamageZones;
using Projectiles;

namespace Net.MessageTypes {
    public class MTSyncGame : IMessage {
        public bool Timed => false;
        public bool Reliable => true;
        public bool Ordered => true;
        public bool Unique => true;
        public byte Channel => (byte)MessageType.SyncGame;

        // ### Projectiles data
        public ushort NumProjectiles { get; private set; }
        public ID[] ProjectileIDs { get; private set; }
        public Vector3[] ProjectilePositions { get; private set; }
        public Vector3[] ProjectileRotations { get; private set; }
        public Vector3[] ProjectileVelocities { get; private set; }

        // ### Ships data
        public ushort NumShips { get; private set; }
        public uint SumWaterIngressSections { get; private set; }
        public uint SumGunTurrets { get; private set; }
        public uint SumDamageZones { get; private set; }
        public uint SumDamageZoneDamages { get; private set; }
        public ID[] ShipIDs { get; private set; }
        // - Movement
        public Vector3[] ShipPositions { get; private set; }
        public Vector3[] ShipRotations { get; private set; }
        public Vector3[] ShipVelocities { get; private set; }
        // - Ship state
        public float[] ShipsHitpoints { get; private set; }
        public byte[] ShipsNumWaterIngressSections { get; private set; }
        public byte[] WaterIngressSectionsIDs { get; private set; }
        public int[] WaterIngressNumHoles { get; private set; }
        public float[] WaterIngressWaterLevels { get; private set; }
        // - Autopilot state
        public Autopilot.ChadburnSetting[] ShipChadburnSettings { get; private set; }
        public ushort[] ShipCourses { get; private set; }
        // - Targeting state
        public bool[] ShipsHasTarget { get; private set; }
        public ID[] ShipsTargetShipID { get; private set; }
        // - Armament state
        public byte[] ShipsNumGunTurrets { get; private set; }
        public ID[] GunTurretIDs { get; private set; }
        public bool[] GunTurretsDisabled { get; private set; }
        public float[] GunTurretsReloadProgress { get; private set; }
        public float[] GunTurretsRotation { get; private set; }
        public float[] GunTurretsTargetRotation { get; private set; }
        public float[] GunTurretsElevation { get; private set; }
        public float[] GunTurretsTargetElevation { get; private set; }
        public float[] GunTurretsStabilizationAngle { get; private set; }
        // - Damage zones
        public byte[] ShipNumDamageZones { get; private set; }
        public ID[] ShipDamageZoneIDs { get; private set; }
        public byte[] DamageZoneNumDamages { get; private set; }
        public DamageType[] DamageZoneDamages { get; private set; }

        /// <summary>
        /// Use this when receiving the message, after that call the Read() method to populate the message data
        /// </summary>
        public MTSyncGame() { }

        /// <summary>
        /// Use this when sending the message
        /// </summary>
        /// <param name="projectiles">All projectiles in the game</param>
        /// <param name="ships">All ships in the game</param>
        public MTSyncGame(Projectile[] projectiles, Ship[] ships) {
            // ### Create projectiles data
            NumProjectiles = (ushort)projectiles.Length;
            AllocateProjectileArrays();
            for (int i = 0; i < NumProjectiles; i++) {
                ProjectileIDs[i] = projectiles[i].ID;
                ProjectilePositions[i] = projectiles[i].transform.position;
                ProjectileRotations[i] = projectiles[i].transform.rotation.eulerAngles;
                ProjectileVelocities[i] = projectiles[i].Velocity;
            }

            // ### Create ships data
            NumShips = (ushort)ships.Length;
            SumWaterIngressSections = 0;
            SumGunTurrets = 0;
            SumDamageZones = 0;
            SumDamageZoneDamages = 0;
            foreach (Ship ship in ships) {
                SumWaterIngressSections += (uint)ship.WaterIngressSections.Count;
                SumGunTurrets += (uint)ship.Armament.GunTurrets.Count;
                SumDamageZones += (uint)ship.DamageZones.Count;
                ship.DamageZones.ForEach(dz => SumDamageZoneDamages += (uint)dz.Damages.Count);
            }
            AllocateShipArrays();

            Ship s;
            uint waterIngressSectionsCounter = 0;
            uint gunTurretsCounter = 0;
            uint damageZoneCounter = 0;
            uint damageZoneDamagesCounter = 0;

            for (int i = 0; i < NumShips; i++) {
                s = ships[i];

                ShipIDs[i] = s.ID;
                // - Create ship movement data
                ShipPositions[i] = s.transform.position;
                ShipRotations[i] = s.transform.rotation.eulerAngles;
                ShipVelocities[i] = s.Velocity;
                // - Create ship state data
                ShipsHitpoints[i] = s.HullHitpoints;
                ShipsNumWaterIngressSections[i] = (byte)s.WaterIngressSections.Count;
                for (byte waterIngressSectionIndex = 0; waterIngressSectionIndex < ShipsNumWaterIngressSections[i]; waterIngressSectionIndex++) {
                    WaterIngressSectionsIDs[waterIngressSectionsCounter] = s.WaterIngressSections[waterIngressSectionIndex].sectionID;
                    WaterIngressNumHoles[waterIngressSectionsCounter] = s.WaterIngressSections[waterIngressSectionIndex].numHoles;
                    WaterIngressWaterLevels[waterIngressSectionsCounter++] = s.WaterIngressSections[waterIngressSectionIndex].waterLevel;
                }
                // - Create ship autopilot state
                ShipChadburnSettings[i] = s.Autopilot.Chadburn;
                ShipCourses[i] = s.Autopilot.Course;
                // - Create ship targeting state
                ShipsHasTarget[i] = s.Targeting.Target != null;
                if (ShipsHasTarget[i]) ShipsTargetShipID[i] = (s.Targeting.Target as Ship).ID;
                else ShipsTargetShipID[i] = new ID("");
                // - Create ship armament state
                ShipsNumGunTurrets[i] = (byte)s.Armament.GunTurrets.Count;
                for (byte gunTurretIndex = 0; gunTurretIndex < ShipsNumGunTurrets[i]; gunTurretIndex++) {
                    GunTurretIDs[gunTurretsCounter] = s.Armament.GunTurrets[gunTurretIndex].ID;
                    GunTurretsDisabled[gunTurretsCounter] = s.Armament.GunTurrets[gunTurretIndex].Disabled;
                    GunTurretsReloadProgress[gunTurretsCounter] = s.Armament.GunTurrets[gunTurretIndex].ReloadProgress;
                    GunTurretsRotation[gunTurretsCounter] = s.Armament.GunTurrets[gunTurretIndex].TurretRotation;
                    GunTurretsTargetRotation[gunTurretsCounter] = s.Armament.GunTurrets[gunTurretIndex].TargetTurretRotation;
                    GunTurretsElevation[gunTurretsCounter] = s.Armament.GunTurrets[gunTurretIndex].GunsElevation;
                    GunTurretsTargetElevation[gunTurretsCounter] = s.Armament.GunTurrets[gunTurretIndex].TargetGunsElevation;
                    GunTurretsStabilizationAngle[gunTurretsCounter++] = s.Armament.GunTurrets[gunTurretIndex].StabilizationAngle;
                }
                // - Create ship damage zones state
                ShipNumDamageZones[i] = (byte)s.DamageZones.Count;
                for (byte damageZoneIndex = 0; damageZoneIndex < ShipNumDamageZones[i]; damageZoneIndex++) {
                    ShipDamageZoneIDs[damageZoneCounter] = s.DamageZones[damageZoneIndex].ID;
                    DamageZoneNumDamages[damageZoneCounter++] = (byte)s.DamageZones[damageZoneIndex].Damages.Count;
                    s.DamageZones[damageZoneIndex].Damages.ForEach(d => DamageZoneDamages[damageZoneDamagesCounter++] = d);
                }
            }
        }

        public void Write(Writer writer) {
            // ### Write projectiles data
            writer.Write(NumProjectiles);
            for (ushort i = 0; i < NumProjectiles; i++) {
                writer.Write(ProjectileIDs[i].ToString());
                writer.Write(ProjectilePositions[i]);
                writer.Write(ProjectileRotations[i]);
                writer.Write(ProjectileVelocities[i]);
            }

            // ### Write ships data
            writer.Write(NumShips);
            writer.Write(SumWaterIngressSections);
            writer.Write(SumGunTurrets);
            writer.Write(SumDamageZones);
            writer.Write(SumDamageZoneDamages);

            uint waterIngressSectionsCounter = 0;
            uint gunTurretsCounter = 0;
            uint damageZoneCounter = 0;
            uint damageZoneDamagesCounter = 0;

            for (int shipIndex = 0; shipIndex < NumShips; shipIndex++) {
                writer.Write(ShipIDs[shipIndex].ToString());
                // - Write ship movement data
                writer.Write(ShipPositions[shipIndex]);
                writer.Write(ShipRotations[shipIndex]);
                writer.Write(ShipVelocities[shipIndex]);
                // - Write ship state data
                writer.Write(ShipsHitpoints[shipIndex]);
                writer.Write(ShipsNumWaterIngressSections[shipIndex]);
                for (byte waterIngressSectionIndex = 0; waterIngressSectionIndex < ShipsNumWaterIngressSections[shipIndex]; waterIngressSectionIndex++) {
                    writer.Write(WaterIngressSectionsIDs[waterIngressSectionsCounter]);
                    writer.Write(WaterIngressNumHoles[waterIngressSectionsCounter]);
                    writer.Write(WaterIngressWaterLevels[waterIngressSectionsCounter++]);
                }
                // - Write ship autopilot state
                writer.WriteEnum<Autopilot.ChadburnSetting>(ShipChadburnSettings[shipIndex]);
                writer.Write(ShipCourses[shipIndex]);
                // - Write ship targeting state
                writer.Write(ShipsHasTarget[shipIndex]);
                writer.Write(ShipsTargetShipID[shipIndex].ToString());
                // - Write ship armament state
                writer.Write(ShipsNumGunTurrets[shipIndex]);
                for (byte gunTurretIndex = 0; gunTurretIndex < ShipsNumGunTurrets[shipIndex]; gunTurretIndex++) {
                    writer.Write(GunTurretIDs[gunTurretsCounter].ToString());
                    writer.Write(GunTurretsDisabled[gunTurretsCounter]);
                    writer.Write(GunTurretsReloadProgress[gunTurretsCounter]);
                    writer.Write(GunTurretsRotation[gunTurretsCounter]);
                    writer.Write(GunTurretsTargetRotation[gunTurretsCounter]);
                    writer.Write(GunTurretsElevation[gunTurretsCounter]);
                    writer.Write(GunTurretsTargetElevation[gunTurretsCounter]);
                    writer.Write(GunTurretsStabilizationAngle[gunTurretsCounter++]);
                }
                // - Write ship damage zones state
                writer.Write(ShipNumDamageZones[shipIndex]);
                for (byte damageZoneIndex = 0; damageZoneIndex < ShipNumDamageZones[shipIndex]; damageZoneIndex++) {
                    writer.Write(ShipDamageZoneIDs[damageZoneCounter].ToString());
                    writer.Write(DamageZoneNumDamages[damageZoneCounter]);
                    for (byte damageIndex = 0; damageIndex < DamageZoneNumDamages[damageZoneCounter]; damageIndex++) {
                        writer.WriteEnum<DamageType>(DamageZoneDamages[damageZoneDamagesCounter++]);
                    }
                    damageZoneCounter++;
                }
            }
        }

        public void Read(Reader reader) {
            // ### Read projectiles data
            NumProjectiles = reader.ReadUInt16();
            AllocateProjectileArrays();
            for (ushort i = 0; i < NumProjectiles; i++) {
                ProjectileIDs[i] = new ID(reader.ReadString());
                ProjectilePositions[i] = reader.ReadVector3();
                ProjectileRotations[i] = reader.ReadVector3();
                ProjectileVelocities[i] = reader.ReadVector3();
            }

            // ### Read ships data
            NumShips = reader.ReadUInt16();
            SumWaterIngressSections = reader.ReadUint32();
            SumGunTurrets = reader.ReadUint32();
            SumDamageZones = reader.ReadUint32();
            SumDamageZoneDamages = reader.ReadUint32();
            AllocateShipArrays();

            uint waterIngressSectionsCounter = 0;
            uint gunTurretsCounter = 0;
            uint damageZoneCounter = 0;
            uint damageZoneDamagesCounter = 0;

            for (ushort shipIndex = 0; shipIndex < NumShips; shipIndex++) {
                ShipIDs[shipIndex] = new ID(reader.ReadString());
                // - Read ship movement data
                ShipPositions[shipIndex] = reader.ReadVector3();
                ShipRotations[shipIndex] = reader.ReadVector3();
                ShipVelocities[shipIndex] = reader.ReadVector3();
                // - Read ship state data
                ShipsHitpoints[shipIndex] = reader.ReadSingle();
                ShipsNumWaterIngressSections[shipIndex] = reader.ReadByte();
                for (byte waterIngressSectionIndex = 0; waterIngressSectionIndex < ShipsNumWaterIngressSections[shipIndex]; waterIngressSectionIndex++) {
                    WaterIngressSectionsIDs[waterIngressSectionsCounter] = reader.ReadByte();
                    WaterIngressNumHoles[waterIngressSectionsCounter] = reader.ReadInt32();
                    WaterIngressWaterLevels[waterIngressSectionsCounter++] = reader.ReadSingle();
                }
                // - Read ship autopilot state
                ShipChadburnSettings[shipIndex] = reader.ReadEnum<Autopilot.ChadburnSetting>();
                ShipCourses[shipIndex] = reader.ReadUInt16();
                // - Read ship targeting state
                ShipsHasTarget[shipIndex] = reader.ReadBoolean();
                ShipsTargetShipID[shipIndex] = new ID(reader.ReadString());
                // - Read ship armament state
                ShipsNumGunTurrets[shipIndex] = reader.ReadByte();
                for (byte gunTurretIndex = 0; gunTurretIndex < ShipsNumGunTurrets[shipIndex]; gunTurretIndex++) {
                    GunTurretIDs[gunTurretsCounter] = new ID(reader.ReadString());
                    GunTurretsDisabled[gunTurretsCounter] = reader.ReadBoolean();
                    GunTurretsReloadProgress[gunTurretsCounter] = reader.ReadSingle();
                    GunTurretsRotation[gunTurretsCounter] = reader.ReadSingle();
                    GunTurretsTargetRotation[gunTurretsCounter] = reader.ReadSingle();
                    GunTurretsTargetElevation[gunTurretsCounter] = reader.ReadSingle();
                    GunTurretsTargetElevation[gunTurretsCounter] = reader.ReadSingle();
                    GunTurretsStabilizationAngle[gunTurretsCounter++] = reader.ReadSingle();
                }
                // - Read ship damage zones state
                ShipNumDamageZones[shipIndex] = reader.ReadByte();
                for (byte damageZoneIndex = 0; damageZoneIndex < ShipNumDamageZones[shipIndex]; damageZoneIndex++) {
                    ShipDamageZoneIDs[damageZoneCounter] = new ID(reader.ReadString());
                    DamageZoneNumDamages[damageZoneCounter] = reader.ReadByte();
                    for (byte damageIndex = 0; damageIndex < DamageZoneNumDamages[damageZoneCounter]; damageIndex++) {
                        DamageZoneDamages[damageZoneDamagesCounter++] = reader.ReadEnum<DamageType>();
                    }
                    damageZoneCounter++;
                }
            }
        }

        private void AllocateProjectileArrays() {
            ProjectileIDs = new ID[NumProjectiles];
            ProjectilePositions = new Vector3[NumProjectiles];
            ProjectileRotations = new Vector3[NumProjectiles];
            ProjectileVelocities = new Vector3[NumProjectiles];
        }

        private void AllocateShipArrays() {
            ShipIDs = new ID[NumShips];
            ShipPositions = new Vector3[NumShips];
            ShipRotations = new Vector3[NumShips];
            ShipVelocities = new Vector3[NumShips];
            ShipsHitpoints = new float[NumShips];
            ShipsNumWaterIngressSections = new byte[NumShips];
            WaterIngressSectionsIDs = new byte[SumWaterIngressSections];
            WaterIngressNumHoles = new int[SumWaterIngressSections];
            WaterIngressWaterLevels = new float[SumWaterIngressSections];
            ShipChadburnSettings = new Autopilot.ChadburnSetting[NumShips];
            ShipCourses = new ushort[NumShips];
            ShipsHasTarget = new bool[NumShips];
            ShipsTargetShipID = new ID[NumShips];
            ShipsNumGunTurrets = new byte[NumShips];
            GunTurretIDs = new ID[SumGunTurrets];
            GunTurretsDisabled = new bool[SumGunTurrets];
            GunTurretsReloadProgress = new float[SumGunTurrets];
            GunTurretsRotation = new float[SumGunTurrets];
            GunTurretsTargetRotation = new float[SumGunTurrets];
            GunTurretsElevation = new float[SumGunTurrets];
            GunTurretsTargetElevation = new float[SumGunTurrets];
            GunTurretsStabilizationAngle = new float[SumGunTurrets];
            ShipNumDamageZones = new byte[NumShips];
            ShipDamageZoneIDs = new ID[SumDamageZones];
            DamageZoneNumDamages = new byte[SumDamageZones];
            DamageZoneDamages = new DamageType[SumDamageZoneDamages];
        }
    }
}