using AncientTools.Blocks;
using AncientTools.Utility;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AncientTools.BlockEntity
{
    class BEMortar : BlockEntityDisplay
    {

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "mortarcontainer";

        private const float GRIND_TIME_IN_SECONDS = 4;

        private long grindStartTime = -1;

        internal InventoryGeneric inventory;

        private ILoadedSound ambientSound, stoneSound;

        private SimpleParticleProperties grindingParticles;
        private int particleColor;

        private Vec3f lookAtPlayerVector = new Vec3f(0.0f, 0.0f, 0.0f);
        private float[] animationPositions = { 0.050f, 0.050f, 0.075f, 0.1f, 0.125f, 0.150f, 0.125f, 0.1f, 0.070f, 0.050f, 0.050f };
        private Vec3f[] animationRotations = {
            new Vec3f(0.0f, 0.15f, 0.0f),
            new Vec3f(0.0f, 0.10f, 0.0f),
            new Vec3f(0.0f, 0.05f, 0.0f),
            new Vec3f(0f, 0.025f, 0f),
            new Vec3f(0f, 0.0f, 0f),
            new Vec3f(0f, 0.0f, 0f),
            new Vec3f(0f, 0.0f, 0f),
            new Vec3f(0f, 0.025f, 0f),
            new Vec3f(0.0f, 0.05f, 0.0f),
            new Vec3f(0.0f, 0.10f, 0.0f),
            new Vec3f(0.0f, 0.15f, 0.0f)
        };

        private int animPosition = 0;

        private GridRecipe alchemyRecipe;

        public ItemSlot ResourceSlot
        {
            get { return inventory[0]; }
        }
        public ItemSlot PestleSlot
        {
            get { return inventory[1]; }
        }
        public ItemSlot AlchemySlot1
        {
            get { return inventory[2];  }
        }
        public ItemSlot AlchemySlot2
        {
            get { return inventory[3]; }
        }
        public ItemSlot AlchemyOutput
        {
            get { return inventory[4]; }
        }

        public BEMortar()
        {
            inventory = new InventoryGeneric(5, null, null);
        }
        ~BEMortar()
        {
            if (ambientSound != null) ambientSound.Dispose();
            if (stoneSound != null) stoneSound.Dispose();
        }
        public override void Initialize(ICoreAPI api)
        {
            //-- Holds mesh data of items inserted into the mortar. The resource mesh is NOT used as the resource appearance is not actually represented by the inserted resrouce. --//
            meshes = new MeshData[5];

            if(api.Side == EnumAppSide.Client)
            {
                InitializeGindingParticles();

                ambientSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("ancienttools", "sounds/block/mortargrind.ogg"),
                    ShouldLoop = true,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1.0f
                });
                stoneSound = ((IClientWorldAccessor)api.World).LoadSound(new SoundParams()
                {
                    Location = new AssetLocation("game", "sounds/block/loosestone2.ogg"),
                    ShouldLoop = false,
                    Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
                    DisposeOnFinish = false,
                    Volume = 1.0f,
                    Range = 32
                });
            }

            base.Initialize(api);
        }
        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if(ambientSound != null)
            {
                ambientSound.Stop();
                ambientSound.Dispose();
            }
            if(stoneSound != null)
            {
                stoneSound.Stop();
                stoneSound.Dispose();
            }
        }
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            BlockMortar block = Api.World.BlockAccessor.GetBlock(Pos) as BlockMortar;

            if (!PestleSlot.Empty)
            {
                //-- Each time the block is set to dirty, animPostion is changed. This modifies which translate/rotate keyframe is used to transform the pestle mesh --// 
                if (PestleSlot.Itemstack.Item.FirstCodePart() == "pestle")
                {
                    mesher.AddMeshData(meshes[1].Clone()
                        .Translate(new Vec3f(0, animationPositions[animPosition], 0))
                        .Rotate(new Vec3f(0.5f, 0.0f, 0.5f),
                        lookAtPlayerVector.X + animationRotations[animPosition].X,
                        lookAtPlayerVector.Y + animationRotations[animPosition].Y,
                        lookAtPlayerVector.Z + animationRotations[animPosition].Z));
                }
            }

            if (!ResourceSlot.Empty)
            {
                PrepareMesh("ancienttools:shapes/block/mortar/resource_", ResourceSlot, block, mesher, tessThreadTesselator);
            }

            if (!AlchemySlot1.Empty)
            {
                PrepareMesh("ancienttools:shapes/block/mortar/alchemy/alchemy_", AlchemySlot1, block, mesher, tessThreadTesselator);
            }

            if (!AlchemySlot2.Empty)
            {
                PrepareMesh("ancienttools:shapes/block/mortar/alchemy/alchemy_", AlchemySlot2, block, mesher, tessThreadTesselator);
            }
            
            if(!AlchemyOutput.Empty)
            {
                string shapeBase = "ancienttools:shapes/";
                string resourcePath;

                if (AlchemyOutput.Itemstack.Item.FirstCodePart() == "potionbase" && AlchemyOutput.Itemstack.Item.FirstCodePart(1) == "basic")
                    resourcePath = "block/mortar/alchemy/alchemy_base";
                else
                    resourcePath = "block/mortar/alchemy/alchemy_potion";

                this.AddMesh(block, mesher, shapeBase + resourcePath, tessThreadTesselator.GetTexSource(this.Block));
            }
            return false;
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);

            particleColor = tree.GetInt("particleColor");
            lookAtPlayerVector = new Vec3f(tree.GetFloat("x"), tree.GetFloat("y"), tree.GetFloat("z"));
        }
        //-- Particle color and pestle rotation is saved so that things look as they were left proper on game load --//
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetInt("particleColor", particleColor);
            tree.SetFloat("x", lookAtPlayerVector.X + animationRotations[0].X);
            tree.SetFloat("y", lookAtPlayerVector.Y + animationRotations[0].Y);
            tree.SetFloat("z", lookAtPlayerVector.Z + animationRotations[0].Z);
        }
        public void OnInteract(IPlayer byPlayer)
        {
            if (!byPlayer.Entity.Controls.Sneak)
            {
                ItemSlot activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

                //-- If the player is standing and clicks the pestle with an empty hand, attempt to give them a pestle/resource that may be in the mortar --//
                if (activeSlot.Empty)
                {
                    TakeFromMortar(byPlayer);
                }
                else
                {
                    //-- Give the player the resource if the resource in hand matches the resource in the mortar --//
                    if (ReturnMatchingResource(byPlayer, activeSlot))
                    {
                        return;
                    }
                    else
                    {

                        if (InsertPestle(activeSlot))
                            return;

                        if (InsertResource(activeSlot))
                            return;

                        if (ExtractPotion(byPlayer, activeSlot))
                            return;

                        if (InsertAlchemyObject1(activeSlot))
                            return;

                        InsertAlchemyObject2(activeSlot);
                    }
                }
            }
        }
        
        public bool OnSneakInteract(IPlayer byPlayer)
        {
            if (!byPlayer.Entity.Controls.Sneak || PestleSlot.Empty || !byPlayer.InventoryManager.ActiveHotbarSlot.Empty)
                return false;

            if(!ResourceSlot.Empty)
            {
                BeginGrind();
            }
            else if(!AlchemySlot1.Empty && !AlchemySlot2.Empty)
            {
                BeginAlchemyGrind();
            }
            else
            {
                OnInteractStop();
                return false;
            }

            PerformGrind(byPlayer);
            return true;
        }
        private void PrepareMesh(string shapeFolderLocation, ItemSlot inventorySlot, BlockMortar block, ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            string codePath = inventorySlot.Itemstack.Collectible.Code.Path;
            string resourcePath = shapeFolderLocation + inventorySlot.Itemstack.Collectible.Code.Domain + "_";

            foreach (char character in codePath)
            {
                if (character != '-')
                    resourcePath += character;
                else
                    resourcePath += '_';
            }

            //-- If no shape asset is found then a default mesh is used. --//
            if (Api.Assets.Exists(new AssetLocation(resourcePath + ".json")))
            {
                this.AddMesh(block, mesher, resourcePath, tessThreadTesselator.GetTexSource(block));
            }
            else
            {
                resourcePath = "ancienttools:shapes/block/mortar/resource_default";

                this.AddMesh(block, mesher, resourcePath, tessThreadTesselator.GetTexSource(block));
            }
        }
        private void TakeFromMortar(IPlayer byPlayer)
        {
            if (!PestleSlot.Empty)
            {
                GiveObject(byPlayer, PestleSlot);
                this.updateMesh(1);
            }
            else if (!AlchemySlot2.Empty)
            {
                GiveObject(byPlayer, AlchemySlot2);
            }
            else if (!AlchemySlot1.Empty)
            {
                GiveObject(byPlayer, AlchemySlot1);
            }
            else if (!ResourceSlot.Empty)
            {
                GiveObject(byPlayer, ResourceSlot);
            }
        }
        private bool ExtractPotion(IPlayer byPlayer, ItemSlot activeSlot)
        {
            if (activeSlot.Itemstack.Block == null || 
                activeSlot.Itemstack.Block.Code.Domain != "alchemy" || 
                activeSlot.Itemstack.Block.FirstCodePart() != "potionflask")
                return false;

            if (AlchemyOutput.Itemstack.Item.FirstCodePart() == "potionbase" && AlchemyOutput.Itemstack.Item.FirstCodePart(1) == "basic")
                return true;

            string flaskType = activeSlot.Itemstack.Block.FirstCodePart(1);
            string potionType = AlchemyOutput.Itemstack.Item.FirstCodePart(1);
            string potionStrength = AlchemyOutput.Itemstack.Item.LastCodePart();

            ItemStack potion = new ItemStack(Api.World.GetBlock(new AssetLocation("alchemy", "potion-" + flaskType + "-" + potionType + "-" + potionStrength)), 1);

            if (!byPlayer.InventoryManager.TryGiveItemstack(potion, true))
            {
                Api.World.SpawnItemEntity(potion, Pos.ToVec3d());
            }

            AlchemyOutput.TakeOutWhole();
            activeSlot.TakeOut(1);
            activeSlot.MarkDirty();
            AlchemyOutput.MarkDirty();

            return true;
        }
        private bool ReturnMatchingResource(IPlayer byPlayer, ItemSlot activeSlot)
        {
            if (!ResourceSlot.Empty && ResourceSlot.Itemstack.Collectible.Code == activeSlot.Itemstack.Collectible.Code)
            {
                GiveObject(byPlayer, ResourceSlot);
                return true;
            }
            else if (!AlchemySlot1.Empty && AlchemySlot1.Itemstack.Collectible.Code == activeSlot.Itemstack.Collectible.Code)
            {
                GiveObject(byPlayer, AlchemySlot1);
                return true;
            }
            else if (!AlchemySlot2.Empty && AlchemySlot2.Itemstack.Collectible.Code == activeSlot.Itemstack.Collectible.Code)
            {
                GiveObject(byPlayer, AlchemySlot2);
                return true;
            }

            return false;
        }
        private bool InsertPestle(ItemSlot activeSlot)
        {
            if (PestleSlot.Empty)
            {
                if (activeSlot.Itemstack.Collectible.ItemClass == EnumItemClass.Item)
                {
                    if (activeSlot.Itemstack.Item.FirstCodePart() == "pestle")
                    {
                        if (Api.Side == EnumAppSide.Client)
                            stoneSound.Start();

                        InsertObject(activeSlot, PestleSlot, activeSlot.Itemstack.Item, 1);
                        this.updateMesh(1);

                        return true;
                    }
                }
            }

            return false;
        }
        private bool InsertResource(ItemSlot activeSlot)
        {
            if (ResourceSlot.Empty && AlchemySlot1.Empty && AlchemySlot2.Empty)
            {
                if (activeSlot.Itemstack.Collectible.ItemClass == EnumItemClass.Item)
                {
                    if (activeSlot.Itemstack.Item.GrindingProps != null)
                    {
                        if (Api.Side == EnumAppSide.Client)
                            SetGrindingParticlesColor(activeSlot.Itemstack.Item.LastCodePart(), activeSlot.Itemstack.Item.FirstCodePart());

                        InsertObject(activeSlot, ResourceSlot, activeSlot.Itemstack.Item, 1);

                        return true;
                    }
                }
            }

            return false;
        }
        private bool InsertAlchemyObject1(ItemSlot activeSlot)
        {
            if (AlchemySlot1.Empty && ResourceSlot.Empty && !activeSlot.Empty)
            {
                if (AlchemyOutput.Empty)
                {
                    if (activeSlot.Itemstack.Collectible.Class == "BlockMushroom" && activeSlot.Itemstack.Block.FirstCodePart(1) == "flyagaric")
                    {
                        InsertObject(activeSlot, AlchemySlot1, activeSlot.Itemstack.Block, 1);

                        return true;
                    }
                }
                else
                {
                    if (activeSlot.Itemstack.Collectible.ItemClass == EnumItemClass.Item)
                    {
                        InsertObject(activeSlot, AlchemySlot1, activeSlot.Itemstack.Item, 1);
                    }
                    else
                    {
                        InsertObject(activeSlot, AlchemySlot1, activeSlot.Itemstack.Block, 1);
                    }

                    return true;
                }
            }

            return false;
        }
        private bool InsertAlchemyObject2(ItemSlot activeSlot)
        {
            if (AlchemySlot2.Empty && ResourceSlot.Empty)
            {
                if (AlchemyOutput.Empty)
                {
                    if (activeSlot.Itemstack.Collectible.ItemClass == EnumItemClass.Block)
                    {
                        if (activeSlot.Itemstack.Block.FirstCodePart(1) == "horsetail")
                        {
                            InsertObject(activeSlot, AlchemySlot2, activeSlot.Itemstack.Block, 1);

                            return true;
                        }
                    }
                }
                else
                {
                    if (activeSlot.Itemstack.Collectible.ItemClass == EnumItemClass.Item)
                    {
                        InsertObject(activeSlot, AlchemySlot2, activeSlot.Itemstack.Item, 1);
                    }
                    else
                    {
                        InsertObject(activeSlot, AlchemySlot2, activeSlot.Itemstack.Block, 1);
                    }

                    return true;
                }
            }

            return false;
        }
        private void BeginGrind()
        {
            if (grindStartTime < 0)
            {
                grindStartTime = Api.World.ElapsedMilliseconds;
            }
        }
        private void BeginAlchemyGrind()
        {
            if (grindStartTime < 0)
            {
                if (grindStartTime < 0)
                {
                    alchemyRecipe = Api.World.GridRecipes.Find(e => e.Width == 1 && e.Height == 3 &&
                            e.resolvedIngredients[0].SatisfiesAsIngredient(AlchemySlot1.Itemstack) &&
                            e.resolvedIngredients[1].SatisfiesAsIngredient(AlchemySlot2.Itemstack) ||
                            e.Width == 1 && e.Height == 3 &&
                            e.resolvedIngredients[1].SatisfiesAsIngredient(AlchemySlot1.Itemstack) &&
                            e.resolvedIngredients[0].SatisfiesAsIngredient(AlchemySlot2.Itemstack));

                    if (alchemyRecipe != null)
                        grindStartTime = Api.World.ElapsedMilliseconds;
                }
            }
        }
        private void PerformGrind(IPlayer byPlayer)
        {
            if (Api.World.ElapsedMilliseconds < grindStartTime + GRIND_TIME_IN_SECONDS * 1000)
            {
                SetPestleLookAtVector(byPlayer);
                SetPestleAnimationFrame();

                ClientGrind();
            }
            else
            {
                FinishGrind(byPlayer);
            }

            MarkDirty(true);
        }
        //-- Sets the look at vector in such a way that the pestle appears to be held in hand anytime the mortar is being used --//
        private void SetPestleLookAtVector(IPlayer byPlayer)
        {
            Vec3f normal = ((this.Pos.ToVec3f() + new Vec3f(0.5f, 0, 0.5f)) - byPlayer.Entity.Pos.XYZFloat);
            normal.Y = 0.325f;

            double pitch = Math.Asin(normal.Y);
            double yaw = Math.Atan2(normal.X, normal.Z);

            lookAtPlayerVector.Y = (float)yaw;
            lookAtPlayerVector.Z = (float)pitch;
        }
        //-- Tick to the next animation frame any time the pestle is being used --//
        private void SetPestleAnimationFrame()
        {
            if (animPosition < animationPositions.Length - 1)
                animPosition++;
            else
                animPosition = 0;
        }
        private void ClientGrind()
        {
            if(Api.Side == EnumAppSide.Client)
            {
                this.Api.World.SpawnParticles(grindingParticles);

                if (!ambientSound.IsPlaying)
                    ambientSound.Start();
            }
        }
        private void FinishGrind(IPlayer byPlayer)
        {
            grindStartTime = -1;

            if(Api.Side == EnumAppSide.Server)
            {
                if (!ResourceSlot.Empty)
                    GiveGroundItem(byPlayer);
                else
                    MakeAlchemicalBase();
            }
            else if(Api.Side == EnumAppSide.Client)
            {
                StopAudio();
            }

            MarkDirty(true);
        }
        //-- Try to give the player the contents of the mortar resource stack whenever the grind is finished. Puts resource on the ground if there is no room. --//
        private void GiveGroundItem(IPlayer byPlayer)
        {
            ItemStack groundItem = this.ResourceSlot.Itemstack.Collectible.GrindingProps.GroundStack.ResolvedItemstack.Clone();

            if (groundItem.StackSize == 0)
                groundItem.StackSize = 1;

            if (!byPlayer.InventoryManager.TryGiveItemstack(groundItem, true))
            {
                Api.World.SpawnItemEntity(groundItem, Pos.ToVec3d());
            }

            ResourceSlot.TakeOutWhole();
            ResourceSlot.MarkDirty();

            MarkDirty(true);
        }
        private void MakeAlchemicalBase()
        {
            if (AlchemyOutput.Empty)
                AlchemyOutput.Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("alchemy", "potionbase-basic")));
            else
                AlchemyOutput.Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("alchemy", "potionbase-" + alchemyRecipe.Output.ResolvedItemstack.Item.LastCodePart(1) + "-" + alchemyRecipe.Output.ResolvedItemstack.Item.LastCodePart())), 1);
            
            AlchemySlot1.TakeOutWhole();
            AlchemySlot2.TakeOutWhole();

            AlchemyOutput.MarkDirty();
            AlchemySlot1.MarkDirty();
            AlchemySlot2.MarkDirty();

            MarkDirty(true);
        }
        private void StopAudio()
        {
            if (ambientSound.IsPlaying)
            {
                ambientSound.Stop();
            }
        }
        public void OnInteractStop()
        {
            animPosition = 0;
            grindStartTime = -1;

            if (Api.Side == EnumAppSide.Client)
            {
                if (ambientSound.IsPlaying)
                {
                    ambientSound.Stop();
                }
            }

            MarkDirty(true);
        }
        private void AddMesh(BlockMortar block, ITerrainMeshPool mesher, string path, ITexPositionSource textureSource)
        {
            MeshData addMesh = block.GenMesh(Api as ICoreClientAPI, path, textureSource);
            mesher.AddMeshData(addMesh);
        }
        private void InsertObject(ItemSlot playerActiveSlot, ItemSlot inventorySlot, Block block, int takeQuantity)
        {
            inventorySlot.Itemstack = new ItemStack(block, takeQuantity);
            playerActiveSlot.TakeOut(takeQuantity);
            MarkDirty(true);
        }
        private void InsertObject(ItemSlot playerActiveSlot, ItemSlot inventorySlot, Item item, int takeQuantity)
        {
            inventorySlot.Itemstack = new ItemStack(item, takeQuantity);
            playerActiveSlot.TakeOut(takeQuantity);
            MarkDirty(true);
        }
        private void GiveObject(IPlayer byPlayer, ItemSlot inventorySlot)
        {
            byPlayer.InventoryManager.TryGiveItemstack(inventorySlot.TakeOutWhole());
            MarkDirty(true);
        }
        private void InitializeGindingParticles()
        {
            grindingParticles = new SimpleParticleProperties()
            {
                MinPos = new Vec3d(this.Pos.X + 0.25, this.Pos.Y + 0.3, this.Pos.Z + 0.25),
                AddPos = new Vec3d(0.5, 0.1, 0.5),

                GravityEffect = -0.02f,

                MinSize = 0.1f,
                MaxSize = 0.4f,
                SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.1f),

                MinQuantity = 1,
                AddQuantity = 2,

                LifeLength = 1.2f,
                addLifeLength = 1.4f,

                ShouldDieInLiquid = true,

                WithTerrainCollision = true,

                Color = particleColor,
                OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 255),

                ParticleModel = EnumParticleModel.Quad
            };
        }
        private void SetGrindingParticlesColor(string color1, string color2)
        {
            grindingParticles.Color = ParticleColor.GetColour(color1, color2);

            particleColor = grindingParticles.Color;
        }
    }
}
