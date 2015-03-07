/*  CollisionManager.cs
 *  
 *  This class provides collision management between game components in the game
 *  
 *  Nicole Dahlquist & Peter Thomson
 *  
 *  2014-11-11 Created:
 *      + Added collision between Zombie and HelloKitty
 *      
 *  Revision History:
 *  2014-11-12
 *      + Added condition for kitty being above zombie before collision to squish them
 *      
 *  2014-11-16
 *      + Added blocks to collision manager
 *      
 *  2014-11-19
 *      + Fixed bugs with hello kitty intersecting blocks
 *      
 *  2014-11-21
 *      + Fixed more bugs with hello kitty intersecting blocks
 *      + Added arrays containing blocks which can be collided on top and blocks with no collision to simplify collision checking
 *      
 *  2014-11-22
 *      + Fixed hello kitty not jumping after fall
 *      + Added bottom collision with star blocks
 *      + added collision with logs
 *  2014-11-23
 *      + added town object collision
 *  2014-11-24
 *      + added spike collision and death condition
 *  2014-11-27
 *      + added fire collision and death condition
 *      + altered zombie collision for level 2
 *  2014-11-28
 *      +Added falling platforms when kitty collides with platform blocks on top
 *  2014-11-29
 *      + adjust for new kitty dimensions
 *  2014-11-30
 *      + Adjust for multiple hit zombies
 *      + Fixed bug where kitty would not fall walking off a sinking platform
 *      + fixed side collision array to include blocks that were missing
 *  2014-12-4
 *      + fixed bug where kitty could go through top half of blocks
 *      + fixed bug where kitty landed on top of objects as soon as she passed through them
 *      + fixed bug where kitty would interact with pink block on the side if facing forward
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;


namespace NDPTFinalProject
{
    /// <summary>
    /// This class handles all collisions between kitty, zombies and game objects
    /// </summary>
    public class CollisionManager : Microsoft.Xna.Framework.GameComponent
    {
        //Class scope variables
        private int kittyFeetPos;
        private HelloKitty helloKitty;
        private List<Zombie> zombies;
        private List<Block> blocks;
        private KeyboardState oldState;
        private bool isLanded = false;
        private float landHeight = 0f;
        private int protectionTimer = 0;
        float initialKittyY = 0;

        // collision arrays
        private Block.blockTypes[] topCollisionBlocks = {Block.blockTypes.GRASS_PEAK, Block.blockTypes.GRASS_LEFT_TOP, Block.blockTypes.GRASS_RIGHT_TOP,
                                                        Block.blockTypes.GRASS_TOP_EDGE_LEFT, Block.blockTypes.GRASS_TOP_EDGE_RIGHT, Block.blockTypes.GRASS_TOP,
                                                         Block.blockTypes.LOG, Block.blockTypes.STICK_PIT_SMALL,Block.blockTypes.STICK_PIT_LARGE};

        private Block.sharedTypes[] topCollisionSharedBlocks = {Block.sharedTypes.STAR_BLOCK_1, Block.sharedTypes.STAR_BLOCK_2, Block.sharedTypes.STAR_BLOCK_3, Block.sharedTypes.STAR_BLOCK_EMPTY,
                                                        Block.sharedTypes.STAR_BLOCK_3,Block.sharedTypes.PLATFORM};

        private Block.townTypes[] topCollisionTownBlocks = {Block.townTypes.BALCONY_RAIL, Block.townTypes.ROOF_LEFT, Block.townTypes.ROOF_MID, Block.townTypes.ROOF_RIGHT, 
                                                           Block.townTypes.WINDOW_WITH_AWNING, Block.townTypes.TREE, Block.townTypes.FENCE};

        private Block.blockTypes[] noCollisionBlocks = { Block.blockTypes.DEFAULT, Block.blockTypes.DEFAULT_LEFT, Block.blockTypes.DEFAULT_RIGHT, 
                                                           Block.blockTypes.GRASS_BOTH_CORNER, Block.blockTypes.GRASS_LEFT_CORNER, Block.blockTypes.GRASS_RIGHT_CORNER};

        private Block.blockTypes[] sideCollisionBlocks = { Block.blockTypes.GRASS_LEFT, Block.blockTypes.GRASS_LEFT_TOP, Block.blockTypes.GRASS_PEAK , Block.blockTypes.STICK_PIT_LARGE, 
                                                             Block.blockTypes.STICK_PIT_SMALL, Block.blockTypes.GRASS_RIGHT, Block.blockTypes.GRASS_RIGHT_TOP};

        private Block.sharedTypes[] sideCollisionSharedBlocks = { Block.sharedTypes.STAR_BLOCK_1, Block.sharedTypes.STAR_BLOCK_2, Block.sharedTypes.STAR_BLOCK_3, Block.sharedTypes.STAR_BLOCK_EMPTY };

        private Block.sharedTypes[] bottomCollisionBlocks = { Block.sharedTypes.STAR_BLOCK_1, Block.sharedTypes.STAR_BLOCK_2, 
                                                               Block.sharedTypes.STAR_BLOCK_EMPTY, Block.sharedTypes.STAR_BLOCK_3 };
        
        
        
        /// <summary>
        /// Constructor for Collision Manager class
        /// </summary>
        /// <param name="game">Main game</param>
        /// <param name="helloKitty">Hello Kitty</param>
        /// <param name="zombies">Zombies</param>
        /// <param name="blocks">Blocks</param>
        public CollisionManager(Game game, HelloKitty helloKitty, List<Zombie> zombies, List<Block> blocks)
            : base(game)
        {
            // child components
            this.helloKitty = helloKitty;
            this.zombies = zombies;
            this.blocks = blocks;
            int kittyFeetPos = helloKitty.SrcRect.Y + 60;
            
        }


        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {

            base.Initialize();
        }
        
        /// <summary>
        /// Allows the Collision Manager to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            float KittyDeltaY = helloKitty.SrcRect.Y - initialKittyY;
            
            Shared.worldSpeed = new Vector2(2, 2);
            if (protectionTimer >= 0)
            {
                protectionTimer -= gameTime.ElapsedGameTime.Milliseconds;
            }
            else
            {
                protectionTimer = 0;
            }
            
            
            KeyboardState ks = Keyboard.GetState();
            // handle zombie collisions
            foreach (Zombie zombie in zombies)
            {
                
                int zombieHeadPos = zombie.SrcRect.Y;
                
                //Kitty collision with zombies
                if (zombie.SrcRect.Intersects(helloKitty.SrcRect) && zombie.IsAlive && !Shared.isKittyDead)
                {
                    //Kitty is above zombie on collision, zombie dies
                    if (kittyFeetPos <= zombieHeadPos)
                    {
                        helloKitty.bounce(zombie.SrcRect.Y - 60);
                        zombie.squish();
                        protectionTimer = 200;
                    }

                    //Kitty is not above zombie on collision, kitty dies
                    else if (protectionTimer <= 0)
                    {
                       
                        helloKitty.zombieDeath();                        
                    }
                }

                //Block collision with zombies
                foreach (Block b in blocks)
                {
                    if (Shared.level != 2)
                    {
                        if (zombie.SrcRect.Intersects(b.SrcRect))
                        {
                            zombie.PositionChange = -zombie.PositionChange;
                            zombie.Position += new Vector2(zombie.PositionChange, 0);
                        }
                    }
                    else if (Shared.level == 2)
                    {
                        foreach (Block.townTypes ff in b.FireBlocks)
                        {
                            if (b.TownType == ff)
                            {
                                if (zombie.SrcRect.Intersects(b.SrcRect))
                                {
                                    zombie.PositionChange = -zombie.PositionChange;
                                    zombie.Position += new Vector2(zombie.PositionChange, 0);
                                }
                            }
                        }
                    }
                }
            }


            bool topCollision = false;
            bool sideCollision = false;
            
            // handle block collisions with kitty
            foreach (Block b in blocks)
            {
                //Fire blocks
                foreach (Block.townTypes ff in b.FireBlocks)
                {
                    if (b.TownType == ff)
                    {
                        if (helloKitty.SrcRect.Intersects(b.SrcRect))
                        {
                            if(!Shared.isKittyDead)
                            {
                                helloKitty.fall();
                                helloKitty.fry();
                            }
                            
                        }
                    }
                }

                //Kitty is intersecting the block
                if (helloKitty.SrcRect.Intersects(b.SrcRect) && !noCollisionBlocks.Contains(b.BlockType))
                {  

                    //Top collision
                    if ((helloKitty.SrcRect.Y + 60 - b.SrcRect.Y <= 10) && (helloKitty.SrcRect.Y + 60 - b.SrcRect.Y >= 0))
                    {

                        if (topCollisionBlocks.Contains(b.BlockType) || topCollisionSharedBlocks.Contains(b.SharedType)
                            || topCollisionTownBlocks.Contains(b.TownType))
                        {
                            landHeight = b.SrcRect.Y - helloKitty.SrcRect.Height;
                            //Sinking platforms
                            if(b.SharedType == Block.sharedTypes.PLATFORM && !topCollision)
                            {                                
                                if(helloKitty.Position.Y + 60 <= Shared.stage.Y)
                                {
                                    if(KittyDeltaY  >= 0)
                                    {
                                        Vector2 platFormFallVector = new Vector2(0, 1f);
                                        b.Position += platFormFallVector;
                                        helloKitty.Position += platFormFallVector;
                                        helloKitty.land(landHeight);
                                        isLanded = true;
                                    }
                                    
                                }                               
                                
                            }
                            topCollision = true;                            
                        }
                        
                    }                   
                    
                    
                    //Right collision
                   
                        if ((helloKitty.SrcRect.X + helloKitty.SrcRect.Width >= b.SrcRect.X) && (kittyFeetPos > b.SrcRect.Y + b.SrcRect.Height / 5)
                            && (helloKitty.SrcRect.X + helloKitty.SrcRect.Width < b.SrcRect.X + b.SrcRect.Width / 2))
                        {
                            if ((ks.IsKeyDown(Keys.Right) || ks.IsKeyDown(Keys.D)) && (sideCollisionBlocks.Contains(b.BlockType)) | sideCollisionSharedBlocks.Contains(b.SharedType))
                            {
                                sideCollision = true;
                            }
                        }
                    

                    //Left Collision
                    
                        if ((helloKitty.SrcRect.X <= b.SrcRect.X + b.SrcRect.Width) && (kittyFeetPos > b.SrcRect.Y + b.SrcRect.Height / 5)
                            && helloKitty.SrcRect.X > b.SrcRect.X + b.SrcRect.Width/2)
                        {
                            if ((ks.IsKeyDown(Keys.Left) || ks.IsKeyDown(Keys.A)) && (sideCollisionBlocks.Contains(b.BlockType)) | sideCollisionSharedBlocks.Contains(b.SharedType))
                            {
                                sideCollision = true;
                            }
                        }
                    

                    //Bottom collision
                    if ((helloKitty.SrcRect.Y - (b.SrcRect.Y + b.SrcRect.Height) >= -10) && (helloKitty.SrcRect.Y - (b.SrcRect.Y + b.SrcRect.Height) <= 0))
                    {
                        if (bottomCollisionBlocks.Contains(b.SharedType) && !helloKitty.IsFalling)
                        {
                            if(!sideCollision)
                            {
                                if(KittyDeltaY <= 0 )
                                {
                                    b.hitBlock();
                                    helloKitty.fall();
                                }
                                
                            }                            
                        }
                    }

                    //Spike collision death
                    if ((b.BlockType == Block.blockTypes.STICK_PIT_LARGE || b.BlockType == Block.blockTypes.STICK_PIT_SMALL) && (sideCollision || topCollision))
                    {
                        helloKitty.fall();
                        Shared.isKittyDead = true;
                    }
                }
                             
            }            

            //Being called when stuck and trying to jump
            if (topCollision && !sideCollision && !isLanded)
            {          
                 if (helloKitty.IsJumping)

                    {
                        if(KittyDeltaY > 0)
                        {
                            helloKitty.land(landHeight);
                            isLanded = true;
                        }                        
                    }               
            }


            //Being called when stuck and trying to jump
            if (helloKitty.IsJumping)
            {
                isLanded = false;
            }

            //Not being called when stuck
            if (!topCollision && isLanded)
            {
                helloKitty.fall();
                isLanded = false;
                Shared.worldSpeed = new Vector2(2, 2);
            }

            
            // alter world movement based on side collision
            if (sideCollision)
            {
                Shared.worldSpeed = new Vector2(0, 0);
            }
            else
            {
                Shared.worldSpeed = new Vector2(2, 2);
            }                    

            oldState = ks;
            kittyFeetPos = helloKitty.SrcRect.Y + 60;
            initialKittyY = helloKitty.SrcRect.Y;
            base.Update(gameTime);
        }
    }
}
