using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

[assembly: ModInfo("Graveyard",
    Description = "TODO description",
    Website = "",
    Version = "1.0.0",
    Authors = new[] {"cout970"})
]

namespace Graveyard
{
    public class GraveyardSystem : ModSystem
    {
        public override bool AllowRuntimeReload() => true;

        // Client
        public ICoreClientAPI ClientAPI { get; private set; }

        // Server
        public ICoreServerAPI ServerAPI { get; private set; }
        public DeathHandler DeathHandler { get; private set; }


        public override void Start(ICoreAPI api)
        {
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            ClientAPI = api;
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            ServerAPI = api;
            DeathHandler = new DeathHandler(api);
        }
    }

    public class DeathHandler
    {
        public DeathHandler(ICoreServerAPI api)
            => api.Event.PlayerDeath += OnPlayerDeath;

        private void OnPlayerDeath(IPlayer player, DamageSource source)
        {
            var testPos = player.Entity.Pos.AsBlockPos;
            var gold = player.Entity.World.GetBlock(new AssetLocation("mygoldblock", "mygoldblock"));

            BlockPos placedPos = null;

            for (var i = 0; i < 10; i++)
            {
                var testBlock = player.Entity.World.BlockAccessor.GetBlock(testPos);
                if (testBlock.IsReplacableBy(gold))
                {
                    placedPos = testPos;
                    break;
                }
                else
                {
                    testPos = testPos.Add(BlockFacing.DOWN);
                }
            }

            if (placedPos == null) return;

            player.Entity.World.BlockAccessor.SetBlock(gold.BlockId, placedPos);
        }
    }
}