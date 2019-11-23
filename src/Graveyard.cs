using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Graveyard
{
    // ReSharper disable once UnusedMember.Global
    public class GraveyardSystem : ModSystem
    {

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

            // This is needed because by default the game drops all the player items before OnPlayerDeath
            attr.SetBool("keepContents", true);
        }

        public void OnPlayerDeath(IPlayer player, DamageSource source)
        {
            var gravestone = player.Entity.World.GetBlock(new AssetLocation("graveyard", "gravestone"));

            var playerPos = player.Entity.Pos.AsBlockPos;
            var checkBlock = player.Entity.World.BlockAccessor.GetBlock(playerPos);
            var placePos = playerPos;

            // If the player is inside a block, check on top for a spot where to place the block
            if (!checkBlock.IsReplacableBy(gravestone))
            {
                for (var i = 0; i < 20; i++)
                {
                    playerPos.Add(BlockFacing.UP);
                    checkBlock = player.Entity.World.BlockAccessor.GetBlock(playerPos);
                    if (!checkBlock.IsReplacableBy(gravestone)) continue;

                    placePos = playerPos;
                    break;
                }
            }

            // If the player is in the air, check bellow until you find ground
            for (var i = 0; i < 20; i++)
            {
                placePos.Add(BlockFacing.DOWN);
                checkBlock = player.Entity.World.BlockAccessor.GetBlock(placePos);
                if (checkBlock.IsReplacableBy(gravestone)) continue;

                placePos.Add(BlockFacing.UP);
                break;
            }

            if (!EmptyInventory(player))
            {
                player.Entity.World.BlockAccessor.SetBlock(gravestone.Id, placePos);

                // Move items to the graveyard
                var entity = player.Entity.World.BlockAccessor.GetBlockEntity(placePos);
                if (entity is GravestoneBlockEntity blockEntity)
                {
                    blockEntity.FromPlayerInv(player);
                }

                if (player is IServerPlayer sp)
                {
                    var middle = player.Entity.World.DefaultSpawnPosition.AsBlockPos;
                    var pos = new BlockPos(placePos.X - middle.X, middle.Y, placePos.Z - middle.Z);

                    sp.SendMessage(GlobalConstants.GeneralChatGroup,
                        Lang.Get("graveyard:msg-gravePos", player.PlayerName, pos),
                        EnumChatType.Notification);
                }
            }
        }

        private static bool EmptyInventory(IPlayer player)
        {
            var hotbar = player.InventoryManager.GetOwnInventory("hotbar");
            var backpack = player.InventoryManager.GetOwnInventory("backpack");

            for (var i = 0; i < hotbar.Count; i++)
            {
                var slot = hotbar[i];
                if (!slot.Empty) return false;
            }

            for (var i = 0; i < backpack.Count; i++)
            {
                var slot = backpack[i];
                if (!slot.Empty) return false;
            }

            return true;
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class GravestoneBlockEntity : BlockEntity
    {
        private readonly InventoryGeneric _inv = new InventoryGeneric(100, null, null);
        public string PlayerUid { get; private set; }
        public string PlayerName { get; private set; }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            _inv.Api = api;
            _inv.LateInitialize("gravestone-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);
            _inv.ResolveBlocksOrItems();
        }

        public void FromPlayerInv(IPlayer player)
        {
            PlayerUid = player.PlayerUID;
            PlayerName = player.PlayerName;

            var backpack = player.InventoryManager.GetOwnInventory("backpack");
            var hotbar = player.InventoryManager.GetOwnInventory("hotbar");

            var slotIndex = 0;

            // Move hotbar items
            for (var i = 0; i < hotbar.Count; i++)
            {
                var slot = hotbar[i];
                if (slot.Empty) continue;

                _inv[slotIndex].Itemstack = slot.Itemstack;
                slotIndex++;
                slot.Itemstack = null;
                slot.MarkDirty();
            }

            // Move backpacks
            for (var i = 0; i < backpack.Count; i++)
            {
                var slot = backpack[i];
                if (slot.Empty) continue;

                _inv[slotIndex].Itemstack = slot.Itemstack;
                slotIndex++;
                slot.Itemstack = null;
                slot.MarkDirty();
            }

            MarkDirty();
        }

        public void ToPlayerInv(IPlayer player)
        {
            for (var i = 0; i < _inv.Count; i++)
            {
                var slot = _inv[i];
                if (slot.Itemstack == null) continue;

                try
                {
                    if (player.InventoryManager.TryGiveItemstack(slot.Itemstack))
                    {
                        slot.Itemstack = null;
                        slot.MarkDirty();
                    }
                }
                catch (Exception e)
                {
                    player.Entity.World.Logger.Error(e.ToString());
                }
            }

            MarkDirty();
        }

        public override void OnBlockBroken()
        {
            base.OnBlockBroken();
            _inv.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            _inv.ToTreeAttributes(tree);
            if (PlayerUid != null)
            {
                tree.SetString("playerUID", PlayerUid);
            }

            if (PlayerName != null)
            {
                tree.SetString("playerName", PlayerName);
            }
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);
            _inv.FromTreeAttributes(tree);
            PlayerUid = tree.GetString("playerUID");
            PlayerName = tree.GetString("playerName");
        }

        public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping,
            Dictionary<int, AssetLocation> itemIdMapping)
        {
            int q = _inv.Count;
            for (int i = 0; i < q; i++)
            {
                ItemSlot slot = _inv[i];
                if (slot.Itemstack == null) continue;

                slot.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
            }
        }

        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve,
            Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping)
        {
            int q = _inv.Count;
            for (int i = 0; i < q; i++)
            {
                ItemSlot slot = _inv[i];
                if (slot.Itemstack == null) continue;

                if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
                {
                    slot.Itemstack = null;
                }
            }
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
            if (byPlayer is IClientPlayer)
            {
                handling = EnumHandling.PreventSubsequent;
                return true;
            }

            var entity = world.BlockAccessor.GetBlockEntity(blockSel.Position);

            if (!(entity is GravestoneBlockEntity gravestone))
                return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);

            if (gravestone.PlayerUid != byPlayer.PlayerUID)
            {
                if (gravestone.PlayerName != null && byPlayer is IServerPlayer serverPlayer)
                {
                    serverPlayer.SendMessage(GlobalConstants.CurrentChatGroup,
                        Lang.Get("graveyard:msg-graveFromPlayer", gravestone.PlayerName),
                        EnumChatType.Notification);
                }

                return base.OnBlockInteractStart(world, byPlayer, blockSel, ref handling);
            }

            if (world.Side == EnumAppSide.Server)
            {
                gravestone.ToPlayerInv(byPlayer);
                world.BlockAccessor.BreakBlock(blockSel.Position, byPlayer);
            }

            handling = EnumHandling.PreventSubsequent;
            return true;
        }

        // ReSharper disable once RedundantAssignment
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer,
            float dropChanceMultiplier, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventSubsequent;
            return null;
        }
    }
}