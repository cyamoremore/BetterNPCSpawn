using Oxide.Core;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("BetterNPCSpawn", "Cya", "2.2.0")]
    [Description("Advanced NPC spawning with patrol routes for Frontier map")]
    public class BetterNPCSpawn : RustPlugin
    {
        private Configuration config;
        private readonly List<BaseEntity> spawnedNPCs = new List<BaseEntity>();
        private readonly Dictionary<BaseEntity, PatrolRoute> npcPatrols = new Dictionary<BaseEntity, PatrolRoute>();
        private const string PermAdmin = "betternpcspawn.admin";

        private class PatrolRoute
        {
            public Vector3[] WayPoints { get; set; }
            public int CurrentWayPoint { get; set; }
            public float WaitTime { get; set; }
            public float WaitTimer { get; set; }
            public bool IsWaiting { get; set; }

            public PatrolRoute(Vector3[] wayPoints, float waitTime = 2f)
            {
                WayPoints = wayPoints;
                CurrentWayPoint = 0;
                WaitTime = waitTime;
                WaitTimer = 0f;
                IsWaiting = false;
            }
        }

        public class Configuration
        {
            public string NpcPrefab = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_roam.prefab";
            public bool AutoSpawnOnInit = true;
            public int MaxNPCCount = 100;
            public bool EnableLogging = true;
            public bool EnablePatrols = true;
            public float PatrolUpdateInterval = 0.5f;
            public NpcBehavior DefaultBehavior = new NpcBehavior();
            public Dictionary<string, Vector3[]> PatrolRoutes = new Dictionary<string, Vector3[]>();

            public Vector3[] SpawnLocations = new Vector3[]
            {
                // Coal
                new Vector3(-1478.49f, 7.76f, -376.48f),
                new Vector3(-1500.77f, 7.00f, -394.36f),
                new Vector3(-1510.67f, 13.54f, -383.50f),
                new Vector3(-1507.37f, 16.32f, -360.75f),
                new Vector3(-1509.55f, 19.60f, -370.12f),
                new Vector3(-1512.00f, 19.69f, -373.02f),
                new Vector3(-1465.94f, 7.00f, -387.10f),

                // Georgetown
                new Vector3(-120.84f, 1.66f, 264.22f),
                new Vector3(-135.05f, 3.10f, 285.04f),
                new Vector3(-145.53f, 28.77f, 229.40f),
                new Vector3(-157.23f, 3.10f, 250.61f),
                new Vector3(-166.25f, 3.10f, 242.82f),
                new Vector3(-128.30f, 3.10f, 275.84f),
                new Vector3(-110.30f, 3.10f, 253.49f),
                new Vector3(-149.56f, 2.55f, 218.57f),

                // Junkyard
                new Vector3(-914.99f, 20.12f, -263.69f),
                new Vector3(-209.08f, 18.90f, -253.93f),
                new Vector3(-390.59f, 14.58f, -293.24f),
                new Vector3(-280.94f, 12.42f, -246.49f),
                new Vector3(-105.00f, 4.79f, -236.42f),

                // Kan
                new Vector3(-506.82f, 6.57f, -664.75f),
                new Vector3(-583.80f, 5.57f, -679.49f),
                new Vector3(-608.91f, 5.60f, -622.22f),
                new Vector3(-644.51f, 5.66f, -549.79f),
                new Vector3(-674.80f, 5.00f, -501.21f),
                new Vector3(-663.06f, 5.60f, -464.77f),

                // Rebel
                new Vector3(-496.45f, 5.57f, -674.20f),
                new Vector3(-513.22f, 5.57f, -689.95f),
                new Vector3(-528.18f, 5.60f, -632.80f),
                new Vector3(-554.75f, 5.60f, -559.45f),
                new Vector3(-584.33f, 5.00f, -511.75f),
                new Vector3(-573.60f, 5.60f, -474.30f),

                // Reservation
                new Vector3(279.35f, 1.07f, -615.61f),
                new Vector3(242.73f, 0.99f, -565.03f),
                new Vector3(218.99f, 0.99f, -513.96f),
                new Vector3(242.17f, 0.99f, -462.73f),
                new Vector3(283.50f, 1.06f, -411.86f),

                // Special
                new Vector3(915.64f, 1.20f, 465.19f),
                new Vector3(926.72f, 1.20f, 492.18f),
                new Vector3(918.74f, 1.20f, 484.23f),
                new Vector3(941.67f, 1.17f, 516.14f),
                new Vector3(962.07f, 1.20f, 562.67f),
                new Vector3(943.60f, 1.19f, 550.50f),
                new Vector3(960.48f, 1.17f, 551.17f),
                new Vector3(981.66f, 1.21f, 561.72f),
                new Vector3(980.51f, 1.20f, 562.42f),
                new Vector3(962.14f, 1.17f, 525.11f),
                new Vector3(938.29f, 1.18f, 531.54f),
                new Vector3(948.93f, 1.19f, 626.91f),
                new Vector3(962.21f, 1.17f, 634.29f),
                new Vector3(981.20f, 1.18f, 531.85f),
                new Vector3(971.16f, 1.18f, 596.32f),

                // Thornhill
                new Vector3(-178.49f, 7.70f, -376.48f),
                new Vector3(-150.77f, 7.00f, -394.36f),
                new Vector3(-165.30f, 13.54f, -383.50f),
                new Vector3(-157.60f, 16.32f, -360.75f),
                new Vector3(-169.25f, 19.60f, -370.12f),
                new Vector3(-162.80f, 19.69f, -373.92f),
                new Vector3(-146.94f, 7.89f, -387.18f),

                // Train
                new Vector3(783.86f, 6.45f, -1127.94f),
                new Vector3(770.36f, 6.56f, -1119.66f),
                new Vector3(752.77f, 6.62f, -1166.56f),
                new Vector3(795.42f, 6.40f, -1189.69f),
                new Vector3(769.56f, 6.67f, -1221.72f),
                new Vector3(722.36f, 9.68f, -1202.75f)
            };
        }

        public class NpcBehavior
        {
            public float Health = 150f;
            public float HealthRegenerationRate = 0f;
            public float DetectionRange = 50f;
            public float MemoryDuration = 10f;
            public float SenseRange = 50f;
            public float VisionRange = 100f;
            public float AttackRange = 35f;
            public float AttackRate = 2f;
            public float DamageScale = 1.2f;
            public bool IsAggressive = true;
            public bool WillChase = true;
            public float AimConeScale = 0.5f;
            public float AimSway = 0.5f;
            public float AimSwaySpeed = 0.5f;
            public float RecoilScale = 1f;
            public float Speed = 5f;
            public float TurnSpeed = 120f;
            public float RoamRange = 30f;
            public float ChaseRange = 50f;
            public float WalkSpeed = 2f;
            public float SprintSpeed = 7f;
            public float StoppingDistance = 2f;
            public float CoverSearchRange = 30f;
            public float AggressionScale = 1f;
            public float GroupAggressionModifier = 1.2f;
            public string PreferredWeapon = "pistol.revolver";
            public float WeaponSwitchCooldown = 2f;
            public int AmmoPerMagazine = 30;
            public bool InfiniteAmmo = false;
            public bool CanUseCover = true;
            public bool CanHearExplosions = true;
            public bool CanSeeInDarkness = true;
            public float NightVisionRange = 30f;
            public bool IsActive = true;
            public string DisplayName = "Scientist";
            public float DespawnDistance = 200f;
            public bool EnablePatrol = true;
            public float PatrolWaitTime = 2f;
            public string PatrolRouteName = "default";
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();

            config.PatrolRoutes["coal_route"] = new Vector3[]
            {
                new Vector3(-1478.49f, 7.76f, -376.48f),
                new Vector3(-1500.77f, 7.00f, -394.36f),
                new Vector3(-1510.67f, 13.54f, -383.50f),
                new Vector3(-1507.37f, 16.32f, -360.75f),
            };

            config.PatrolRoutes["georgetown_route"] = new Vector3[]
            {
                new Vector3(-120.84f, 1.66f, 264.22f),
                new Vector3(-135.05f, 3.10f, 285.04f),
                new Vector3(-145.53f, 28.77f, 229.40f),
                new Vector3(-157.23f, 3.10f, 250.61f),
                new Vector3(-128.30f, 3.10f, 275.84f),
            };

            config.PatrolRoutes["prison_route"] = new Vector3[]
            {
                new Vector3(915.64f, 1.20f, 465.19f),
                new Vector3(926.72f, 1.20f, 492.18f),
                new Vector3(941.67f, 1.17f, 516.14f),
                new Vector3(962.07f, 1.20f, 562.67f),
                new Vector3(943.60f, 1.19f, 550.50f),
            };

            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            if (config == null)
                LoadDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void Init()
        {
            permission.RegisterPermission(PermAdmin, this);
        }

        private void OnServerInitialized()
        {
            if (config.AutoSpawnOnInit)
                SpawnCustomNPCs();

            if (config.EnablePatrols)
            {
                timer.Every(config.PatrolUpdateInterval, () =>
                {
                    if (config.EnablePatrols)
                        UpdatePatrols();
                });
            }
        }

        private void Unload()
        {
            foreach (var npc in spawnedNPCs)
            {
                if (npc != null && !npc.IsDestroyed)
                    npc.Kill();
            }
            spawnedNPCs.Clear();
            npcPatrols.Clear();
        }

        private void UpdatePatrols()
        {
            List<BaseEntity> toRemove = new List<BaseEntity>();

            foreach (var kvp in npcPatrols)
            {
                BaseEntity entity = kvp.Key;
                PatrolRoute route = kvp.Value;

                if (entity == null || entity.IsDestroyed)
                {
                    toRemove.Add(entity);
                    continue;
                }

                object scientist = TryGetNpcComponent(entity);
                if (scientist == null)
                {
                    toRemove.Add(entity);
                    continue;
                }

                if (route.IsWaiting)
                {
                    route.WaitTimer -= Time.deltaTime;
                    if (route.WaitTimer <= 0f)
                    {
                        route.IsWaiting = false;
                        MoveToNextWayPoint(scientist, route);
                    }
                }
                else
                {
                    Vector3 currentTarget = route.WayPoints[route.CurrentWayPoint];
                    float distance = Vector3.Distance(entity.transform.position, currentTarget);

                    if (distance < 2f)
                    {
                        route.CurrentWayPoint = (route.CurrentWayPoint + 1) % route.WayPoints.Length;
                        route.IsWaiting = true;
                        route.WaitTimer = route.WaitTime;
                    }
                    else
                    {
                        MoveToWayPoint(scientist, currentTarget);
                    }
                }
            }

            foreach (var entity in toRemove)
            {
                npcPatrols.Remove(entity);
            }
        }

        private void MoveToNextWayPoint(object scientist, PatrolRoute route)
        {
            if (route.WayPoints.Length > 0)
            {
                Vector3 nextTarget = route.WayPoints[route.CurrentWayPoint];
                MoveToWayPoint(scientist, nextTarget);
            }
        }

        private void MoveToWayPoint(object scientist, Vector3 targetPosition)
        {
            if (scientist == null)
                return;

            TrySetField(scientist, "navigationPoint", targetPosition);
            TrySetField(scientist, "targetPoint", targetPosition);
            Vector3 direction = (targetPosition - GetScientistPosition(scientist)).normalized;
            TrySetField(scientist, "moveDirection", direction);
        }

        private Vector3 GetScientistPosition(object scientist)
        {
            if (scientist == null)
                return Vector3.zero;

            var transform = scientist.GetType().GetField("transform",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (transform != null)
            {
                var value = transform.GetValue(scientist) as Component;
                if (value != null)
                    return value.transform.position;
            }

            var posProp = scientist.GetType().GetProperty("position",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (posProp != null)
            {
                var value = posProp.GetValue(scientist, null);
                if (value is Vector3 vec)
                    return vec;
            }

            return Vector3.zero;
        }

        private object TryGetNpcComponent(BaseEntity entity)
        {
            if (entity == null)
                return null;

            var candidateTypes = new[]
            {
                "ScientistNPC",
                "HumanNPC",
                "NPCPlayer",
                "BaseNpc",
                "BasePlayer"
            };

            foreach (var typeName in candidateTypes)
            {
                var type = entity.GetType().GetProperty(typeName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase);

                if (type != null)
                {
                    var value = type.GetValue(entity, null);
                    if (value != null)
                        return value;
                }

                var field = entity.GetType().GetField(typeName,
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.IgnoreCase);

                if (field != null)
                {
                    var value = field.GetValue(entity);
                    if (value != null)
                        return value;
                }

                var component = entity.GetComponent(System.Type.GetType("ScientistNPC"));
                if (component != null)
                    return component;
            }

            return entity.GetComponent("ScientistNPC") as object;
        }

        private void SetupPatrolRoute(BaseEntity npcEntity, NpcBehavior behavior)
        {
            if (!behavior.EnablePatrol || string.IsNullOrEmpty(behavior.PatrolRouteName))
                return;

            if (!config.PatrolRoutes.ContainsKey(behavior.PatrolRouteName))
            {
                Puts("Patrol route not found: " + behavior.PatrolRouteName);
                return;
            }

            Vector3[] wayPoints = config.PatrolRoutes[behavior.PatrolRouteName];
            PatrolRoute route = new PatrolRoute(wayPoints, behavior.PatrolWaitTime);
            npcPatrols[npcEntity] = route;
        }

        private void SpawnCustomNPCs()
        {
            if (config.SpawnLocations == null || config.SpawnLocations.Length == 0)
            {
                Puts("No spawn locations configured!");
                return;
            }

            int spawned = 0;

            foreach (Vector3 coord in config.SpawnLocations)
            {
                if (spawnedNPCs.Count >= config.MaxNPCCount)
                {
                    Puts("Max NPC limit reached: " + config.MaxNPCCount);
                    break;
                }

                if (SpawnNPCAtLocation(coord))
                    spawned++;
            }

            if (config.EnableLogging)
                Puts("Spawned " + spawned + "/" + config.SpawnLocations.Length + " NPCs");
        }

        private bool SpawnNPCAtLocation(Vector3 coord)
        {
            try
            {
                Vector3 pos = coord;
                float groundY = TerrainMeta.HeightMap.GetHeight(pos);
                if (groundY > 0f)
                    pos.y = groundY;

                BaseEntity entity = GameManager.server.CreateEntity(config.NpcPrefab, pos, Quaternion.identity);
                if (entity == null)
                    return false;

                object scientist = TryGetNpcComponent(entity);
                if (scientist == null)
                {
                    entity.Kill();
                    return false;
                }

                ApplyNpcBehavior(scientist, config.DefaultBehavior);
                entity.Spawn();
                GiveNpcClothing(scientist);
                GiveNpcWeapon(scientist, config.DefaultBehavior);
                spawnedNPCs.Add(entity);
                SetupPatrolRoute(entity, config.DefaultBehavior);

                return true;
            }
            catch (System.Exception ex)
            {
                if (config.EnableLogging)
                    Puts("SpawnNPCAtLocation failed: " + ex.Message);
                return false;
            }
        }

        private void GiveNpcClothing(object scientist)
        {
            if (scientist == null)
                return;

            try
            {
                var inventory = GetInventory(scientist);
                if (inventory == null)
                    return;

                string[] clothing = new string[]
                {
                    "hat.boonie",
                    "jacket.wood",
                    "pants.wood",
                    "shoes.boots",
                    "gloves.leather"
                };

                foreach (string clothingItem in clothing)
                {
                    ItemDefinition itemDef = ItemManager.FindItemDefinition(clothingItem);
                    if (itemDef != null)
                    {
                        try
                        {
                            var method = inventory.GetType().GetMethod("GiveItem",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);

                            if (method != null)
                                method.Invoke(inventory, new object[] { itemDef, 1 });
                        }
                        catch { }
                    }
                }
            }
            catch { }
        }

        private void GiveNpcWeapon(object scientist, NpcBehavior behavior)
        {
            if (string.IsNullOrEmpty(behavior.PreferredWeapon))
                return;

            ItemDefinition weaponDef = ItemManager.FindItemDefinition(behavior.PreferredWeapon);
            if (weaponDef != null)
            {
                var inventory = GetInventory(scientist);
                if (inventory != null)
                {
                    try
                    {
                        var method = inventory.GetType().GetMethod("GiveItem",
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance);

                        if (method != null)
                            method.Invoke(inventory, new object[] { weaponDef, 1 });
                    }
                    catch { }
                }
            }

            if (!behavior.InfiniteAmmo && behavior.AmmoPerMagazine > 0)
            {
                ItemDefinition ammoDef = ItemManager.FindItemDefinition("ammo.pistol");
                if (ammoDef != null)
                {
                    var inventory = GetInventory(scientist);
                    if (inventory != null)
                    {
                        try
                        {
                            var method = inventory.GetType().GetMethod("GiveItem",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);

                            if (method != null)
                                method.Invoke(inventory, new object[] { ammoDef, behavior.AmmoPerMagazine });
                        }
                        catch { }
                    }
                }
            }
        }

        private object GetInventory(object scientist)
        {
            if (scientist == null)
                return null;

            var inventoryProp = scientist.GetType().GetProperty("inventory",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (inventoryProp != null)
            {
                var value = inventoryProp.GetValue(scientist, null);
                if (value != null)
                    return value;
            }

            var inventoryField = scientist.GetType().GetField("inventory",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (inventoryField != null)
            {
                var value = inventoryField.GetValue(scientist);
                if (value != null)
                    return value;
            }

            return null;
        }

        private void ApplyNpcBehavior(object scientist, NpcBehavior behavior)
        {
            if (scientist == null)
                return;

            TrySetField(scientist, "startHealth", behavior.Health);
            TrySetField(scientist, "health", behavior.Health);
            TrySetField(scientist, "_health", behavior.Health);
            TrySetField(scientist, "healthRegenerationRate", behavior.HealthRegenerationRate);
            TrySetField(scientist, "detectionRange", behavior.DetectionRange);
            TrySetField(scientist, "DetectionRange", behavior.DetectionRange);
            TrySetField(scientist, "senseRange", behavior.SenseRange);
            TrySetField(scientist, "visionRange", behavior.VisionRange);
            TrySetField(scientist, "memoryDuration", behavior.MemoryDuration);
            TrySetField(scientist, "attackRange", behavior.AttackRange);
            TrySetField(scientist, "attackRate", behavior.AttackRate);
            TrySetField(scientist, "damageScale", behavior.DamageScale);
            TrySetField(scientist, "aggressionScale", behavior.AggressionScale);
            TrySetField(scientist, "groupAggressionModifier", behavior.GroupAggressionModifier);
            TrySetField(scientist, "aimConeScale", behavior.AimConeScale);
            TrySetField(scientist, "aimSway", behavior.AimSway);
            TrySetField(scientist, "aimSwaySpeed", behavior.AimSwaySpeed);
            TrySetField(scientist, "recoilScale", behavior.RecoilScale);
            TrySetField(scientist, "speed", behavior.Speed);
            TrySetField(scientist, "turnSpeed", behavior.TurnSpeed);
            TrySetField(scientist, "roamRange", behavior.RoamRange);
            TrySetField(scientist, "chaseRange", behavior.ChaseRange);
            TrySetField(scientist, "walkSpeed", behavior.WalkSpeed);
            TrySetField(scientist, "sprintSpeed", behavior.SprintSpeed);
            TrySetField(scientist, "stoppingDistance", behavior.StoppingDistance);
            TrySetField(scientist, "isAggressive", behavior.IsAggressive);
            TrySetField(scientist, "willChase", behavior.WillChase);
            TrySetField(scientist, "canUseCover", behavior.CanUseCover);
            TrySetField(scientist, "canHearExplosions", behavior.CanHearExplosions);
            TrySetField(scientist, "canSeeInDarkness", behavior.CanSeeInDarkness);
            TrySetField(scientist, "nightVisionRange", behavior.NightVisionRange);
            TrySetField(scientist, "preferredWeapon", behavior.PreferredWeapon);
            TrySetField(scientist, "weaponSwitchCooldown", behavior.WeaponSwitchCooldown);
            TrySetField(scientist, "ammoPerMagazine", behavior.AmmoPerMagazine);
            TrySetField(scientist, "infiniteAmmo", behavior.InfiniteAmmo);
        }

        private void TrySetField(object obj, string fieldName, object value)
        {
            if (obj == null || value == null)
                return;

            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (field != null)
            {
                try
                {
                    field.SetValue(obj, value);
                    return;
                }
                catch
                {
                }
            }

            var prop = obj.GetType().GetProperty(fieldName,
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.IgnoreCase);

            if (prop != null && prop.CanWrite)
            {
                try
                {
                    prop.SetValue(obj, value);
                }
                catch
                {
                }
            }
        }

        [ChatCommand("npcreset")]
        private void CmdResetNPCs(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                SendReply(player, "No permission!");
                return;
            }

            foreach (var npc in spawnedNPCs)
            {
                if (npc != null && !npc.IsDestroyed)
                    npc.Kill();
            }
            spawnedNPCs.Clear();
            npcPatrols.Clear();

            SpawnCustomNPCs();
            SendReply(player, "NPCs reset and respawned!");
        }

        [ChatCommand("npcstatus")]
        private void CmdNPCStatus(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                SendReply(player, "No permission!");
                return;
            }

            int alive = 0;
            foreach (var npc in spawnedNPCs)
            {
                if (npc != null && !npc.IsDestroyed)
                    alive++;
            }

            SendReply(player, "Active NPCs: " + alive + "/" + spawnedNPCs.Count);
            SendReply(player, "NPCs on patrol: " + npcPatrols.Count);
        }

        [ChatCommand("npcconfig")]
        private void CmdNPCConfig(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermAdmin))
            {
                SendReply(player, "No permission!");
                return;
            }

            if (args.Length < 1)
            {
                SendReply(player, "Usage: /npcconfig [aggressivity|damage|health|accuracy|detection|speed|attackrate|patrol] [value]");
                return;
            }

            string setting = args[0].ToLower();
            if (args.Length < 2)
            {
                SendReply(player, "You must provide a value for: " + setting);
                return;
            }

            if (!float.TryParse(args[1], out float value))
            {
                SendReply(player, "Value must be a number!");
                return;
            }

            switch (setting)
            {
                case "aggressivity":
                case "agression":
                    config.DefaultBehavior.AggressionScale = value;
                    SendReply(player, "Aggressivity set to: " + value);
                    break;

                case "damage":
                    config.DefaultBehavior.DamageScale = value;
                    SendReply(player, "Damage set to: " + value);
                    break;

                case "health":
                    config.DefaultBehavior.Health = value;
                    SendReply(player, "Health set to: " + value);
                    break;

                case "accuracy":
                case "aim":
                    config.DefaultBehavior.AimConeScale = value;
                    SendReply(player, "Accuracy set to: " + value);
                    break;

                case "detection":
                    config.DefaultBehavior.DetectionRange = value;
                    SendReply(player, "Detection range set to: " + value);
                    break;

                case "speed":
                    config.DefaultBehavior.Speed = value;
                    SendReply(player, "Speed set to: " + value);
                    break;

                case "attackrate":
                    config.DefaultBehavior.AttackRate = value;
                    SendReply(player, "Attack rate set to: " + value);
                    break;

                case "patrol":
                    config.DefaultBehavior.EnablePatrol = value > 0;
                    SendReply(player, "Patrol: " + (value > 0 ? "ON" : "OFF"));
                    break;

                default:
                    SendReply(player, "Unknown setting!");
                    break;
            }

            SaveConfig();
        }
    }
}
