using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
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
    // ReSharper disable once UnusedMember.Global
    public class GraveyardSystem : ModSystem
    {
        public override bool AllowRuntimeReload() => true;

        public EventHandler EventHandler { get; set; }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockEntityClass("gravestone_entity", typeof(GravestoneBlockEntity));
            api.RegisterBlockBehaviorClass("GravestoneBlockBehavior", typeof(GravestoneBlockBehavior));
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            EventHandler = new EventHandler(api);
        }
    }

    public class EventHandler
    {
        public EventHandler(ICoreServerAPI api)
        {
            api.Event.PlayerDeath += OnPlayerDeath;
            api.Event.PlayerJoin += OnPlayerJoin;
        }

        public void OnPlayerJoin(IServerPlayer byPlayer)
        {
            var attr = byPlayer.Entity.Properties.Server.Attributes;
            if (attr == null)
            {
                attr = new TreeAttribute();
                byPlayer.Entity.Properties.Server.Attributes = attr;
            }

            attr.SetBool("keepContents", true);
        }

        public void OnPlayerDeath(IPlayer player, DamageSource source)
        {
            var checkPos = player.Entity.Pos.AsBlockPos;
            var gravestone = player.Entity.World.GetBlock(new AssetLocation("graveyard", "gravestone"));

            var placedPos = checkPos;
            for (var i = 0; i < 10; i++)
            {
                var testBlock = player.Entity.World.BlockAccessor.GetBlock(checkPos.Add(BlockFacing.DOWN, i));
                if (!testBlock.IsReplacableBy(gravestone)) continue;

                placedPos = checkPos;
                break;
            }

            player.Entity.World.BlockAccessor.SetBlock(gravestone.BlockId, placedPos);

            var entity = player.Entity.World.BlockAccessor.GetBlockEntity(placedPos);
            if (entity is GravestoneBlockEntity blockEntity)
            {
                blockEntity.FromPlayerInv(player);
            }
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class GravestoneBlockEntity : BlockEntity
    {
        private readonly InventoryGeneric _inv = new InventoryGeneric(100, "gravestone", "0", null);
        public string PlayerUid { get; private set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            _inv.Api = api;
        }

        public void FromPlayerInv(IPlayer player)
        {
            PlayerUid = player.PlayerUID;
            var backpack = player.InventoryManager.GetOwnInventory("backpack");
            var hotbar = player.InventoryManager.GetOwnInventory("hotbar");

            var slotIndex = 0;

            for (var i = 0; i < backpack.QuantitySlots; i++)
            {
                var slot = backpack.GetSlot(i);
                if (slot.Empty) continue;

                _inv.GetSlot(slotIndex).Itemstack = slot.Itemstack;
                slotIndex++;
                slot.Itemstack = null;
                slot.MarkDirty();
            }

            for (var i = 0; i < hotbar.QuantitySlots; i++)
            {
                var slot = hotbar.GetSlot(i);
                if (slot.Empty) continue;

                _inv.GetSlot(slotIndex).Itemstack = slot.Itemstack;
                slotIndex++;
                slot.Itemstack = null;
                slot.MarkDirty();
            }
        }

        public void ToPlayerInv(IPlayer player)
        {
        }

        public override void OnBlockBroken()
        {
            base.OnBlockBroken();
            _inv.DropAll(pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            _inv.ToTreeAttributes(tree);
            if (PlayerUid != null)
            {
                tree.SetString("playerUID", PlayerUid);
            }
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);
            _inv.FromTreeAttributes(tree);
            PlayerUid = tree.GetString("playerUID");
        }
    }

    public class GravestoneBlockBehavior : BlockBehavior
    {
        public GravestoneBlockBehavior(Block block) : base(block)
        {
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel,
            ref EnumHandling handling)
        {
            var entity = world.BlockAccessor.GetBlockEntity(blockSel.Position);

            if (!(entity is GravestoneBlockEntity gravestone))
                return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);

            if (gravestone.PlayerUid != byPlayer.PlayerUID)
                return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);

            if (world.Side == EnumAppSide.Server)
            {
                gravestone.ToPlayerInv(byPlayer);
                world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
            }

            handling = EnumHandling.PreventSubsequent;
            return true;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
            float dropChanceMultiplier, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventSubsequent;
            return null;
        }
    }
}