using BepInEx;
using BepInEx.Configuration;
using RoR2;
using RoR2.Projectile;
using RoR2.Artifacts;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using EntityStates.BrotherMonster;
using EntityStates.BrotherMonster.Weapon;
using Unity;
using UnityEngine;
using UnityEngine.Networking;
using R2API;
using R2API.Utils;
using RoR2.Skills;
using RoR2.CharacterAI;
using System;
using System.Security;
using System.Security.Permissions;
using System.Collections.Generic;


[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]



namespace Blobface
{

    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Blobface.ArtifactKing", "Artifact of the King", "1.0.2")]
    [R2APISubmoduleDependency("LanguageAPI")]
    [R2APISubmoduleDependency("ArtifactAPI")]

    public class ArtifactKing : BaseUnityPlugin
    {
        //public static ConfigEntry<bool> Always { get; set; }
        public static ConfigEntry<int> PrimStocks { get; set; }
        public static ConfigEntry<int> SecStocks { get; set; }
        public static ConfigEntry<int> UtilStocks { get; set; }

        public static ConfigEntry<float> PrimCD { get; set; }
        public static ConfigEntry<float> SecCD { get; set; }
        public static ConfigEntry<float> UtilCD { get; set; }
        public static ConfigEntry<float> SpecialCD { get; set; }

        public static ConfigEntry<float> basehealth { get; set; }
        public static ConfigEntry<float> levelhealth { get; set; }
        public static ConfigEntry<float> basearmor { get; set; }
        public static ConfigEntry<float> baseattackspeed { get; set; }
        public static ConfigEntry<float> basedamage { get; set; }
        public static ConfigEntry<float> leveldamage { get; set; }


        public static ConfigEntry<float> basespeed { get; set; }
        public static ConfigEntry<float> mass { get; set; }
        public static ConfigEntry<float> turningspeed { get; set; }
        public static ConfigEntry<float> jumpingpower { get; set; }
        public static ConfigEntry<float> acceleration { get; set; }
        public static ConfigEntry<int> jumpcount{ get; set; }
        public static ConfigEntry<float> aircontrol { get; set; }

        public static ConfigEntry<float> SlamOrbCount { get; set; }
        public static ConfigEntry<int> SecondaryFan { get; set; }
        public static ConfigEntry<int> UtilityShotgun { get; set; }
        public static ConfigEntry<int> LunarShardAdd { get; set; }
        public static ConfigEntry<int> UltimateWaves { get; set; }
        public static ConfigEntry<int> UltimateCount { get; set; }
        public static ConfigEntry<float> UltimateDuration { get; set; }
        public static ConfigEntry<int> clonecount { get; set; }
        public static ConfigEntry<int> cloneduration { get; set; }
        public static ConfigEntry<float> JumpRecast { get; set; }
        public static ConfigEntry<float> JumpPause { get; set; }
        public static ConfigEntry<float> ShardHoming { get; set; }
        public static ConfigEntry<float> ShardRange { get; set; }
        public static ConfigEntry<float> ShardCone { get; set; }



        private bool hasfired;
        ArtifactDef King = ScriptableObject.CreateInstance<ArtifactDef>();

        static Sprite LoadTexture2D(Byte[] resourceBytes)
        {
            if (resourceBytes == null) throw new ArgumentNullException(nameof(resourceBytes));

            var tempTex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            tempTex.LoadImage(resourceBytes, false);

            return Sprite.Create(tempTex, new Rect(0, 0, tempTex.width, tempTex.height), new Vector2(1f, 1f));
        }

        public void Awake()
        {
            //Always = Config.Bind<bool>("Artifact", "Value", false, "set whether to use the artifact or make the changes constant");
            //Logger.LogMessage("1");

            basehealth = Config.Bind<float>("Stats", "BaseHealth", 1400f, "base health");
            levelhealth = Config.Bind<float>("Stats", "LevelHealth", 420f, "level health");
            basedamage = Config.Bind<float>("Stats", "BaseDamage", 16f, "base damage");
            leveldamage = Config.Bind<float>("Stats", "LevelDamage", 3.2f, "level damage");
            basearmor = Config.Bind<float>("Stats", "BaseArmor", 20f, "base armor");
            baseattackspeed = Config.Bind<float>("Stats", "BaseAttackSpeed", 1f, "base attack speed");

            basespeed = Config.Bind<float>("Movement", "BaseSpeed", 18f, "Mithrix's base movement speed");
            mass = Config.Bind<float>("Movement", "Mass", 1200f, "mass, recommended to increase if you increase his movement speed");
            turningspeed = Config.Bind<float>("Movement", "TurnSpeed", 900f, "how fast mithrix turns");
            jumpingpower = Config.Bind<float>("Movement", "MoonShoes", 75f, "how hard mithrix jumps, vanilla is 25 for context");
            acceleration = Config.Bind<float>("Movement", "Acceleration", 180f, "acceleration");
            jumpcount = Config.Bind<int>("Movement", "JumpCount", 3, "jump count, probably doesn't do anything");
            aircontrol = Config.Bind<float>("Movement", "Aircontrol", 1f, "air control");

            PrimStocks = Config.Bind<int>("Skills", "PrimStocks", 2, "Max Stocks for Mithrix's Weapon Slam");
            SecStocks = Config.Bind<int>("Skills", "SecondaryStocks", 2, "Max Stocks for Mithrix's Dash Attack");
            UtilStocks = Config.Bind<int>("Skills", "UtilStocks", 4, "Max Stocks for Mithrix's Dash");

            PrimCD = Config.Bind<float>("Skills", "PrimCD", 3f, "Cooldown for Mithrix's Weapon Slam");
            SecCD = Config.Bind<float>("Skills", "SecCD", 3f, "Cooldown for Mithrix's Dash Attack");
            UtilCD = Config.Bind<float>("Skills", "UtilCD", 1f, "Cooldown for Mithrix's Dash");
            SpecialCD = Config.Bind<float>("Skills", "SpecialCD", 30f, "Cooldown for Mithrix's Jump Attack");

            SlamOrbCount = Config.Bind<float>("Skillmods", "OrbCount", 16f, "Orbs fired by weapon slam in a circle, note, set this to an integer");
            SecondaryFan = Config.Bind<int>("Skillmods", "FanCount", 5, "half the shards fired in a fan by the secondary skill");
            UtilityShotgun = Config.Bind<int>("Skillmods", "ShotgunCount", 5, "shots fired in a shotgun by utility");
            LunarShardAdd = Config.Bind<int>("Skillmods", "ShardAddCount", 5, "Bonus shards added to each shot of lunar shards");
            UltimateWaves = Config.Bind<int>("Skillmods", "WavePerShot", 16, "waves fired by ultimate per shot");
            UltimateCount = Config.Bind<int>("Skillmods", "WaveShots", 6, "Total shots of ultimate");
            UltimateDuration = Config.Bind<float>("Skillmods", "WaveDuration", 5.5f, "how long ultimate lasts");
            clonecount = Config.Bind<int>("Skillmods", "CloneCount", 2, "clones spawned in phase 3 by jump attack");
            cloneduration = Config.Bind<int>("Skillmods", "CloneDuration", 30, "how long clones take to despawn (like happiest mask)");
            JumpRecast = Config.Bind<float>("Skillmods", "RecastChance", 0f, "chance mithrix has to recast his jump skill. USE WITH CAUTION.");
            JumpPause = Config.Bind<float>("Skillmods", "JumpDelay", 0.2f, "How long Mithrix spends in the air when using his jump special");
            ShardHoming = Config.Bind<float>("Skillmods", "ShardHoming", 30f, "How strongly lunar shards home in to targets. Vanilla is 20f");
            ShardRange = Config.Bind<float>("Skillmods", "ShardRange", 60f, "Range (distance) in which shards look for targets");
            ShardCone = Config.Bind<float>("Skillmods", "ShardCone", 180f, "Cone (Angle) in which shards look for targets");
            LanguageAPI.Add("King", "Artifact of the King");

            GameObject Mithrix = Resources.Load<GameObject>("prefabs/characterbodies/BrotherBody");

            void AdjustStats()
            {
                Logger.LogMessage("Adjusting Stats");
                CharacterBody MithrixBody = Mithrix.GetComponent<CharacterBody>();
                CharacterDirection MithrixDirection = Mithrix.GetComponent<CharacterDirection>();
                CharacterMotor MithrixMotor = Mithrix.GetComponent<CharacterMotor>();
                MithrixMotor.mass = mass.Value;
                MithrixMotor.airControl = aircontrol.Value;
                MithrixMotor.jumpCount = jumpcount.Value;

                MithrixBody.baseMaxHealth = basehealth.Value;
                MithrixBody.levelMaxHealth = levelhealth.Value;

                MithrixBody.baseAttackSpeed = baseattackspeed.Value;

                MithrixBody.baseMoveSpeed = basespeed.Value;
                MithrixBody.baseAcceleration = acceleration.Value;
                MithrixBody.baseJumpPower = jumpingpower.Value;
                MithrixDirection.turnSpeed = turningspeed.Value;

                MithrixBody.baseArmor = basearmor.Value;

                MithrixBody.baseDamage = basedamage.Value;
                MithrixBody.levelDamage = leveldamage.Value;

                ProjectileSteerTowardTarget component = FireLunarShards.projectilePrefab.GetComponent<ProjectileSteerTowardTarget>();
                component.rotationSpeed = ShardHoming.Value;
                ProjectileDirectionalTargetFinder component2 = FireLunarShards.projectilePrefab.GetComponent<ProjectileDirectionalTargetFinder>();
                component2.lookRange = ShardRange.Value;
                component2.lookCone = ShardCone.Value;
                component2.allowTargetLoss = true;

                WeaponSlam.duration = (3.5f / baseattackspeed.Value);
                HoldSkyLeap.duration = JumpPause.Value;
                ExitSkyLeap.cloneCount = clonecount.Value;
                ExitSkyLeap.cloneDuration = cloneduration.Value;
                ExitSkyLeap.recastChance = JumpRecast.Value;
                UltChannelState.waveProjectileCount = UltimateWaves.Value;
                UltChannelState.maxDuration = UltimateDuration.Value;
                UltChannelState.totalWaves = UltimateCount.Value;

                On.RoR2.HealthComponent.ServerFixedUpdate += (orig, self) =>
                {
                    orig(self);
                    if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                    {
                        self.adaptiveArmorValue = Mathf.Max(0f, self.adaptiveArmorValue - 100f * Time.fixedDeltaTime);
                    }


                };

                On.RoR2.HealthComponent.TakeDamage += (orig, self, DamageInfo) =>
                {
                    orig(self, DamageInfo);
                    if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                    {
                        float num10 = DamageInfo.damage / self.fullCombinedHealth * 100f * 50f * (float)self.itemCounts.adaptiveArmor;
                        self.adaptiveArmorValue = Mathf.Min(self.adaptiveArmorValue + num10, 900f);
                    }

                };

            }

            void AdjustSkills()
            {
                SkillLocator SklLocate = Mithrix.GetComponent<SkillLocator>();
                SkillFamily Hammer = SklLocate.primary.skillFamily;
                SkillDef HammerChange = Hammer.variants[0].skillDef;
                HammerChange.baseRechargeInterval = PrimCD.Value;
                HammerChange.baseMaxStock = PrimStocks.Value;

                SkillFamily Bash = SklLocate.secondary.skillFamily;
                SkillDef BashChange = Bash.variants[0].skillDef;
                BashChange.baseRechargeInterval = SecCD.Value;
                BashChange.baseMaxStock = SecStocks.Value;

                SkillFamily Dash = SklLocate.utility.skillFamily;
                SkillDef DashChange = Dash.variants[0].skillDef;
                DashChange.baseRechargeInterval = UtilCD.Value;
                DashChange.baseMaxStock = UtilStocks.Value;

                SkillFamily Ult = SklLocate.special.skillFamily;
                SkillDef UltChange = Ult.variants[0].skillDef;
                UltChange.baseRechargeInterval = SpecialCD.Value;
                UltChange.baseMaxStock = 5;
            }

            void RevertStats()
            {
                CharacterBody MithrixBody = Mithrix.GetComponent<CharacterBody>();
                CharacterDirection MithrixDirection = Mithrix.GetComponent<CharacterDirection>();
                CharacterMotor MithrixMotor = Mithrix.GetComponent<CharacterMotor>();
                MithrixMotor.mass = 900;
                MithrixMotor.airControl = 0.25f;
                MithrixMotor.jumpCount = 0;

                MithrixBody.baseMaxHealth = 1400;
                MithrixBody.levelMaxHealth = 420;

                MithrixBody.baseAttackSpeed = 1f;

                MithrixBody.baseMoveSpeed = 15f;
                MithrixBody.baseAcceleration = 45;
                MithrixBody.baseJumpPower = 25f;
                MithrixDirection.turnSpeed = 270;

                MithrixBody.baseArmor = 20f;

                MithrixBody.baseDamage = 16f;
                MithrixBody.levelDamage = 3.2f;

                ProjectileSteerTowardTarget component = FireLunarShards.projectilePrefab.GetComponent<ProjectileSteerTowardTarget>();
                component.rotationSpeed = 20f;
                ProjectileDirectionalTargetFinder component2 = FireLunarShards.projectilePrefab.GetComponent<ProjectileDirectionalTargetFinder>();
                component2.lookRange = 80f;
                component2.lookCone = 90f;
                component2.allowTargetLoss = true;

                WeaponSlam.duration = 3.5f;
                HoldSkyLeap.duration = 3f;
                ExitSkyLeap.cloneCount = 0;
                ExitSkyLeap.cloneDuration = 0;
                ExitSkyLeap.recastChance = 0f;
                UltChannelState.waveProjectileCount = 8;
                UltChannelState.maxDuration = 9f;
                UltChannelState.totalWaves = 5;
            }

            void RevertSkills()
            {
                SkillLocator SklLocate = Mithrix.GetComponent<SkillLocator>();
                SkillFamily Hammer = SklLocate.primary.skillFamily;
                SkillDef HammerChange = Hammer.variants[0].skillDef;
                HammerChange.baseRechargeInterval = 4f;
                HammerChange.baseMaxStock = 1;

                SkillFamily Bash = SklLocate.secondary.skillFamily;
                SkillDef BashChange = Bash.variants[0].skillDef;
                BashChange.baseRechargeInterval = 5f;
                BashChange.baseMaxStock = 1;

                SkillFamily Dash = SklLocate.utility.skillFamily;
                SkillDef DashChange = Dash.variants[0].skillDef;
                DashChange.baseRechargeInterval = 3f;
                DashChange.baseMaxStock = 2;

                SkillFamily Ult = SklLocate.special.skillFamily;
                SkillDef UltChange = Ult.variants[0].skillDef;
                UltChange.baseRechargeInterval = 30f;
                UltChange.baseMaxStock = 5;
            }


            King.nameToken = "Artifact of the King";
            King.descriptionToken = "Reveal the true strength of the King of Nothing";
            King.smallIconDeselectedSprite = LoadTexture2D(ArtifactofTheKing2.Properties.Resources.headoff);//Resources.Load<Sprite>("@ArtifactofTheKing2:Assets/Import/head-off.png");//LoadoutAPI.CreateSkinIcon(Color.white, Color.white, Color.white, Color.white);
            King.smallIconSelectedSprite = LoadTexture2D(ArtifactofTheKing2.Properties.Resources.headon);//LoadoutAPI.CreateSkinIcon(Color.gray, Color.white, Color.white, Color.white);

            ArtifactAPI.Add(King);

            On.RoR2.Run.Start += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                {
                    Logger.LogMessage("Initializing modded stats");
                    AdjustSkills();
                    AdjustStats();
                }
                else
                {
                    Logger.LogMessage("Reverting to vanilla stats");
                    RevertSkills();
                    RevertStats();
                }

                orig(self);
            };


            On.EntityStates.BrotherMonster.ExitSkyLeap.OnEnter += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                {

                    if (self.isAuthority)
                    {
                        if (self.fixedAge == 0.45f * self.duration)
                        {
                            self.FireRingAuthority();
                        }

                        if (self.fixedAge == 0.9f * self.duration)
                        {
                            self.FireRingAuthority();
                        }
                    }
                }
                
                orig(self);
                
            };

            /*On.EntityStates.BrotherMonster.HoldSkyLeap.OnEnter += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                {
                    
                }
                
                orig(self);
            };*/


            On.EntityStates.BrotherMonster.SlideIntroState.OnEnter += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                {
                    Ray aimRay = self.GetAimRay();
                    if (self.isAuthority)
                    {
                        for (int i = 0; i < UtilityShotgun.Value; i++)
                        {
                            ProjectileManager.instance.FireProjectile(FireLunarShards.projectilePrefab, aimRay.origin, Quaternion.LookRotation(aimRay.direction), self.gameObject, self.characterBody.damage * 0.05f/12f, 0f, Util.CheckRoll(self.characterBody.crit, self.characterBody.master), DamageColorIndex.Default, null, -1f);
                            aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, 4f, 4f, 4f, 0f, 0f);
                        }
                    }
                }
                
               
                orig(self);

            };

            On.EntityStates.BrotherMonster.SprintBash.OnEnter += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                {
                    if (self.isAuthority)
                    {
                        for (int i = 0; i < SecondaryFan.Value; i++)
                        {
                            Ray aimRay = self.GetAimRay();
                            Vector3 forward = Util.ApplySpread(aimRay.direction, 0f, 0f, 1f, 0f, i * 5f, 0f);
                            ProjectileManager.instance.FireProjectile(FireLunarShards.projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(forward), self.gameObject, self.characterBody.damage * 0.1f/12f, 0f, Util.CheckRoll(self.characterBody.crit, self.characterBody.master), DamageColorIndex.Default, null, -1f);
                            Ray aimRay2 = self.GetAimRay();
                            Vector3 forward2 = Util.ApplySpread(aimRay2.direction, 0f, 0f, 1f, 0f, -i * 5f, 0f);
                            ProjectileManager.instance.FireProjectile(FireLunarShards.projectilePrefab, aimRay2.origin, Util.QuaternionSafeLookRotation(forward2), self.gameObject, self.characterBody.damage * 0.1f/12f, 0f, Util.CheckRoll(self.characterBody.crit, self.characterBody.master), DamageColorIndex.Default, null, -1f);
                        }
                    }
                }
                       
                orig(self);

            };

            On.EntityStates.BrotherMonster.WeaponSlam.OnEnter += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                {
                    hasfired = false;
                }
                
                orig(self);
            };

            /*On.EntityStates.BrotherMonster.UltChannelState.OnEnter += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                {
                   
                }
               
                orig(self);
            };*/

            On.EntityStates.BrotherMonster.WeaponSlam.FixedUpdate += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                {
                    if (self.isAuthority)
                    {
                        Logger.LogDebug("added hammer proj");
                        if (self.hasDoneBlastAttack)
                        {
                            Logger.LogDebug("blast attack done");
                            if (self.modelTransform)
                            {
                                if (hasfired == false)
                                {
                                    hasfired = true;
                                    Logger.LogDebug("modeltransformed");
                                    float num = 360f / SlamOrbCount.Value;
                                    Vector3 point = Vector3.ProjectOnPlane(self.inputBank.aimDirection, Vector3.up);
                                    Transform transform2 = self.FindModelChild(WeaponSlam.muzzleString);
                                    Vector3 Position = transform2.position;
                                    for (int i = 0; i < SlamOrbCount.Value; i++)
                                    {
                                        Vector3 forward = Quaternion.AngleAxis(num * (float)i, Vector3.up) * point;
                                        ProjectileManager.instance.FireProjectile(FistSlam.waveProjectilePrefab, Position, Util.QuaternionSafeLookRotation(forward), self.gameObject, self.characterBody.damage * FistSlam.waveProjectileDamageCoefficient, FistSlam.waveProjectileForce, Util.CheckRoll(self.characterBody.crit, self.characterBody.master), DamageColorIndex.Default, null, -1f);
                                    }

                                }

                            }
                        }
                    }
                }
                
                
                orig(self);
            };

            On.EntityStates.BrotherMonster.Weapon.FireLunarShards.OnEnter += (orig, self) =>
            {
                if (RunArtifactManager.instance.IsArtifactEnabled(King.artifactIndex))
                {
                    
                    if (!(self is FireLunarShardsHurt))
                    {
                        if (self.isAuthority)
                        {
                            Ray aimRay = self.GetAimRay();
                            Transform transform = self.FindModelChild(FireLunarShards.muzzleString);
                            if (transform)
                            {
                                aimRay.origin = transform.position;
                            }
                            FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                            fireProjectileInfo.position = aimRay.origin;
                            fireProjectileInfo.rotation = Quaternion.LookRotation(aimRay.direction);
                            fireProjectileInfo.crit = self.characterBody.RollCrit();
                            fireProjectileInfo.damage = self.characterBody.damage * self.damageCoefficient;
                            fireProjectileInfo.damageColorIndex = DamageColorIndex.Default;
                            fireProjectileInfo.owner = self.gameObject;
                            fireProjectileInfo.procChainMask = default(ProcChainMask);
                            fireProjectileInfo.force = 0f;
                            fireProjectileInfo.useFuseOverride = false;
                            fireProjectileInfo.useSpeedOverride = false;
                            fireProjectileInfo.target = null;
                            fireProjectileInfo.projectilePrefab = FireLunarShards.projectilePrefab;

                            for (int i = 0; i < LunarShardAdd.Value; i++)
                            {
                                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                                aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, self.maxSpread * (1f + 0.45f*i), self.spreadYawScale * (1f + 0.45f * i), self.spreadPitchScale * (1f + 0.45f * i), 0f, 0f);//aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, self.maxSpread * (1f + 0.3f*i), self.spreadYawScale * (1f + 0.3f * i), self.spreadPitchScale * (1f + 0.3f * i), 0f, 0f);
                                fireProjectileInfo.rotation = Quaternion.LookRotation(aimRay.direction);
                            }

                        }
                    }
                }
                
                
                orig(self);
            };

        }
    }
}