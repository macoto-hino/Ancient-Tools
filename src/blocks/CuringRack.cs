﻿using AncientTools.BlockEntity;
using AncientTools.Items;
using AncientTools.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace AncientTools.Blocks
{
    class BlockCuringRack: Block
    {
        WorldInteraction[] emptyInteractions = null;
        WorldInteraction[] hookEmptyInteractions = null;
        WorldInteraction[] partialMeatInteractions = null;
        WorldInteraction[] fullInteractions = null;

        int selectionBoxSelected = -1;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            List<ItemStack> curingHook = new List<ItemStack>();
            List<ItemStack> saltedMeat = new List<ItemStack>();

            foreach (Item item in api.World.Items)
            {
                if (item.Code == null) continue;

                if (item.Code.Domain == "ancienttools")
                {
                    if (item.FirstCodePart() == "curinghook")
                        curingHook.Add(new ItemStack(item));
                    else if (item.FirstCodePart() == "saltedmeat" && item.LastCodePart() == "raw")
                        saltedMeat.Add(new ItemStack(item));
                }
            }

            emptyInteractions = ObjectCacheUtil.GetOrCreate(api, "emptyCuringRackInteractions", () =>
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-place-curinghook",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = curingHook.ToArray()
                    }
                };
            });

            hookEmptyInteractions = ObjectCacheUtil.GetOrCreate(api, "emptyHookCuringRackInteractions", () =>
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-take-curinghook",
                        RequireFreeHand = true,
                        MouseButton = EnumMouseButton.Right,
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-place-saltedmeat",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = saltedMeat.ToArray()
                    }
                };
            });

            partialMeatInteractions = ObjectCacheUtil.GetOrCreate(api, "partialCuringRackInteractions", () =>
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-place-saltedmeat",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = saltedMeat.ToArray()
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-take-meat",
                        RequireFreeHand = true,
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });

            fullInteractions = ObjectCacheUtil.GetOrCreate(api, "fullRackInteractions", () =>
            {
                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "ancienttools:blockhelp-take-meat",
                        RequireFreeHand = true,
                        MouseButton = EnumMouseButton.Right
                    }
                };
            });
        }
        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            StringBuilder stringBuilder;
            BECuringRack entityCuringRack = world.BlockAccessor.GetBlockEntity(pos) as BECuringRack;

            if (entityCuringRack == null)
            {
                stringBuilder = new StringBuilder();
                stringBuilder.Append("\n");
                stringBuilder.AppendLine(Lang.Get("ancienttools:blockinfo-curingrack-no-entity"));

                return stringBuilder.ToString();
            }

            string originalBlockInfo = base.GetPlacedBlockInfo(world, pos, forPlayer);
            
            stringBuilder = new StringBuilder(originalBlockInfo);
            stringBuilder.Append("\n");

            //-- Display information based on which selection box is selected. Only shows information for right hook OR left hook --//
            switch(selectionBoxSelected)
            {
                case 1:
                    {
                        if (entityCuringRack.HookItemslot1.Empty)
                        {
                            stringBuilder.AppendLine(Lang.Get("ancienttools:blockinfo-curingrack-hook-missing"));
                            break;
                        }

                        stringBuilder.AppendLine(entityCuringRack.HookItemslot1.Itemstack.GetName());
                        stringBuilder.Append("\n");

                        for (int i = 1; i < 5; i++)
                        {
                            if (entityCuringRack.MeatSlot(i).Empty)
                                break;

                            stringBuilder.AppendLine(entityCuringRack.MeatSlot(i).Itemstack.GetName());

                            if (entityCuringRack.MeatSlot(i).Itemstack.Item is ItemSaltedMeat)
                            {
                                stringBuilder.AppendLine(GetMeatStatus(entityCuringRack.MeatSlot(i)));
                                stringBuilder.Append("\n");
                            }
                        }

                        break;
                    }
                case 0:
                    {
                        if (entityCuringRack.HookItemslot2.Empty)
                        {
                            stringBuilder.AppendLine(Lang.Get("ancienttools:blockinfo-curingrack-hook-missing"));
                            break;
                        }

                        stringBuilder.AppendLine(entityCuringRack.HookItemslot2.Itemstack.GetName());
                        stringBuilder.Append("\n");

                        for (int i = 5; i < 9; i++)
                        {
                            if (entityCuringRack.MeatSlot(i).Empty)
                                break;

                            stringBuilder.AppendLine(entityCuringRack.MeatSlot(i).Itemstack.GetName());

                            if(entityCuringRack.MeatSlot(i).Itemstack.Item is ItemSaltedMeat)
                            {
                                stringBuilder.AppendLine(GetMeatStatus(entityCuringRack.MeatSlot(i)));
                                stringBuilder.Append("\n");
                            }
                        }

                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            return stringBuilder.ToString();
        }
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if(world.BlockAccessor.GetBlockEntity(selection.Position) is BECuringRack entityCuringRack)
            {
                selectionBoxSelected = selection.SelectionBoxIndex;

                switch (selectionBoxSelected)
                {
                    case 1:
                        if (entityCuringRack.HookItemslot1.Empty)
                        {
                            return emptyInteractions;
                        }
                        else if(entityCuringRack.MeatSlot(1).Empty)
                        {
                            return hookEmptyInteractions;
                        }
                        else if(!entityCuringRack.MeatSlot(4).Empty)
                        {
                            return fullInteractions;
                        }
                        else
                        {
                            return partialMeatInteractions;
                        }
                    case 0:
                        if(entityCuringRack.HookItemslot2.Empty)
                        {
                            return emptyInteractions;
                        }
                        else if(entityCuringRack.MeatSlot(5).Empty)
                        {
                            return hookEmptyInteractions;
                        }
                        else if(!entityCuringRack.MeatSlot(8).Empty)
                        {
                            return fullInteractions;
                        }
                        else
                        {
                            return partialMeatInteractions;
                        }
                    default: break;
                }
            }
            return null;
        }
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.BlockAccessor.GetBlockEntity(blockSel.Position) is BECuringRack curingRackEntity)
            {
                curingRackEntity.OnInteract(byPlayer, blockSel.SelectionBoxIndex);
                return true;
            }

            return false;
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            string[] blockCode = this.Code.Path.Split('-');

            blockCode[blockCode.Length - 2] = GetBlockRotation(blockSel.Face, byPlayer.Entity.Pos.XYZInt, blockSel.Position.ToVec3i());
            blockCode[blockCode.Length - 1] = GetBlockType(blockSel.Position, blockCode[blockCode.Length - 2]);

            if (blockCode[blockCode.Length - 1] == null)
                return false;

            int id = world.GetBlock(new AssetLocation("ancienttools", string.Join("-", blockCode))).Id;

            try
            {
                world.BlockAccessor.SetBlock(id, blockSel.Position);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            Block downBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
            string[] blockCode = this.Code.Path.Split('-');

            //-- If the down block does NOT have an up side that is solid... --//
            if (downBlock.SideSolid[4] != true)
            {
                switch (blockCode[blockCode.Length - 2])
                {
                    case "ns":
                        Block eastBlock = world.BlockAccessor.GetBlock(pos.EastCopy());
                        Block westBlock = world.BlockAccessor.GetBlock(pos.WestCopy());

                        //-- Break the block if it does not have a support on either side --//
                        if (eastBlock.BlockMaterial == EnumBlockMaterial.Air || westBlock.BlockMaterial == EnumBlockMaterial.Air)
                        {
                            api.World.BlockAccessor.BreakBlock(pos, null);
                            return;
                        }

                        break;
                    case "ew":
                        Block northBlock = world.BlockAccessor.GetBlock(pos.NorthCopy());
                        Block southBlock = world.BlockAccessor.GetBlock(pos.SouthCopy());

                        //-- Break the block if it does not have a support on either side --//
                        if (northBlock.BlockMaterial == EnumBlockMaterial.Air || southBlock.BlockMaterial == EnumBlockMaterial.Air)
                        {
                            api.World.BlockAccessor.BreakBlock(pos, null);
                            return;
                        }

                        break;
                }

                blockCode[blockCode.Length - 1] = "none";
            }
            else
            {
                switch (blockCode[blockCode.Length - 2])
                {
                    case "ns":
                        Block eastBlock = world.BlockAccessor.GetBlock(pos.EastCopy());
                        Block westBlock = world.BlockAccessor.GetBlock(pos.WestCopy());

                        if (eastBlock.Class == this.Class && westBlock.Class == this.Class && eastBlock.LastCodePart(1) == blockCode[blockCode.Length - 2] && westBlock.LastCodePart(1) == blockCode[blockCode.Length - 2])
                            blockCode[blockCode.Length - 1] = "none";
                        else if (eastBlock.Class == this.Class && eastBlock.LastCodePart(1) == blockCode[blockCode.Length - 2])
                            blockCode[blockCode.Length - 1] = "right";
                        else if (westBlock.Class == this.Class && westBlock.LastCodePart(1) == blockCode[blockCode.Length - 2])
                            blockCode[blockCode.Length - 1] = "left";
                        else
                            blockCode[blockCode.Length - 1] = "full";
                        break;
                    case "ew":
                        Block northBlock = world.BlockAccessor.GetBlock(pos.NorthCopy());
                        Block southBlock = world.BlockAccessor.GetBlock(pos.SouthCopy());

                        if (northBlock.Class == this.Class && southBlock.Class == this.Class && northBlock.LastCodePart(1) == blockCode[blockCode.Length - 2] && southBlock.LastCodePart(1) == blockCode[blockCode.Length - 2])
                            blockCode[blockCode.Length - 1] = "none";
                        else if (northBlock.Class == this.Class && northBlock.LastCodePart(1) == blockCode[blockCode.Length - 2])
                            blockCode[blockCode.Length - 1] = "right";
                        else if (southBlock.Class == this.Class && southBlock.LastCodePart(1) == blockCode[blockCode.Length - 2])
                            blockCode[blockCode.Length - 1] = "left";
                        else
                            blockCode[blockCode.Length - 1] = "full";
                        break;
                }
            }
            int id = world.GetBlock(new AssetLocation("ancienttools", string.Join("-", blockCode))).Id;

            //-- ExchangeBlock is used so that the BECuringRack block entity persists --//
            world.BlockAccessor.ExchangeBlock(id, pos);

            base.OnNeighbourBlockChange(world, pos, neibpos);
        }
        private string GetMeatStatus(ItemSlot meatSlot)
        {
            TransitionState state = meatSlot.Itemstack.Collectible.UpdateAndGetTransitionState(api.World, meatSlot, EnumTransitionType.Perish);
            float multiplier = meatSlot.Itemstack.Collectible.GetTransitionRateMul(api.World, meatSlot, EnumTransitionType.Perish);


            return Lang.Get("ancienttools:blockinfo-curingrack-meat-status", 
                Math.Ceiling(meatSlot.Itemstack.Attributes.GetDouble("curinghoursremaining") / 24), 
                Math.Round((((state.FreshHours + state.TransitionHours) - state.TransitionedHours) / multiplier) / 24));
        }
        private string GetBlockRotation(BlockFacing selectedFace, Vec3i playerPos, Vec3i blockPos)
        {
            //-- Align rack based on the horizontal face clicked --//
            if (selectedFace == BlockFacing.EAST || selectedFace == BlockFacing.WEST)
                return "ns";
            else if (selectedFace == BlockFacing.NORTH || selectedFace == BlockFacing.SOUTH)
                return "ew";
            else
            {
                //-- Otherwise, if the rack is placed on a vertical face, align it based on the angle of the players position against the block position --//
                playerPos.Y = 0;
                blockPos.Y = 0;

                Vec3f rotateBasedOnPlayer = new Vec3f(playerPos.X - blockPos.X, 0, playerPos.Z - blockPos.Z);

                rotateBasedOnPlayer.Normalize();

                if (Math.Abs(rotateBasedOnPlayer.X) >= Math.Abs(rotateBasedOnPlayer.Z))
                    return "ew";
                else
                    return "ns";
            }
        }
        private string GetBlockType(BlockPos blockPos, string rotation)
        {
            Block blockBelow = api.World.BlockAccessor.GetBlock(blockPos.DownCopy());

            if (blockBelow.BlockMaterial == EnumBlockMaterial.Air || blockBelow.BlockMaterial == EnumBlockMaterial.Liquid || blockBelow.BlockMaterial == EnumBlockMaterial.Lava || blockBelow.SideSolid[4] == false)
            {
                //-- If the block is placed over air, liquid, lava, or a block with no solid up side, render without the supports --//
                if (rotation == "ew")
                {
                    if (api.World.BlockAccessor.GetBlock(blockPos.NorthCopy()).BlockMaterial != EnumBlockMaterial.Air && api.World.BlockAccessor.GetBlock(blockPos.SouthCopy()).BlockMaterial != EnumBlockMaterial.Air)
                        return "none";
                    else
                        return null;
                }
                else
                {
                    if (api.World.BlockAccessor.GetBlock(blockPos.EastCopy()).BlockMaterial != EnumBlockMaterial.Air && api.World.BlockAccessor.GetBlock(blockPos.WestCopy()).BlockMaterial != EnumBlockMaterial.Air)
                        return "none";
                    else
                        return null;
                }
            }
            else if (blockBelow.SideSolid[4] == true)
            {
                //-- Otherwise, render with supports. Double supports for single block, right/left supports for 2 lengths together, right/none/left for anything else --//
                if (rotation == "ew")
                {
                    Block northBlock = api.World.BlockAccessor.GetBlock(blockPos.NorthCopy());
                    Block southBlock = api.World.BlockAccessor.GetBlock(blockPos.SouthCopy());

                    if (northBlock.FirstCodePart() == this.FirstCodePart() &&
                        southBlock.FirstCodePart() == this.FirstCodePart() &&
                        northBlock.LastCodePart(1) == rotation &&
                        southBlock.LastCodePart(1) == rotation)
                    {
                        return "none";
                    }
                    else if (northBlock.FirstCodePart() == this.FirstCodePart() &&
                        northBlock.LastCodePart(1) == rotation)
                    {
                        return "right";
                    }
                    else if (southBlock.FirstCodePart() == this.FirstCodePart() &&
                        southBlock.LastCodePart(1) == rotation)
                    {
                        return "left";
                    }
                }
                else
                {
                    Block eastBlock = api.World.BlockAccessor.GetBlock(blockPos.EastCopy());
                    Block westBlock = api.World.BlockAccessor.GetBlock(blockPos.WestCopy());

                    if (eastBlock.FirstCodePart() == this.FirstCodePart() &&
                        westBlock.FirstCodePart() == this.FirstCodePart() &&
                        eastBlock.LastCodePart(1) == rotation &&
                        westBlock.LastCodePart(1) == rotation)
                    {
                        return "none";
                    }
                    else if (eastBlock.FirstCodePart() == this.FirstCodePart() &&
                        eastBlock.LastCodePart(1) == rotation)
                    {
                        return "right";
                    }
                    else if (westBlock.FirstCodePart() == this.FirstCodePart() &&
                        westBlock.LastCodePart(1) == rotation)
                    {
                        return "left";
                    }
                }

                return "full";
            }
            else
                return null;
        }
    }
}
