using System;
using System.Drawing;
using System.Windows.Forms;

//testing purpose aanderson
namespace ACFramework
{

    class cCritterDoor : cCritterWall
    {

        public cCritterDoor(cVector3 enda, cVector3 endb, float thickness, float height, cGame pownergame)
            : base(enda, endb, thickness, height, pownergame)
        {
        }

        public override bool collide(cCritter pcritter)
        {
            bool collided = base.collide(pcritter);
            if (collided && pcritter.IsKindOf("cCritter3DPlayer"))
            {
                ((cGame3D)Game).setdoorcollision();
                return true;
            }
            return false;
        }

        public override bool IsKindOf(string str)
        {
            return str == "cCritterDoor" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritterDoor";
            }
        }
    }

    //==============Critters for the cGame3D: Player, Ball, Treasure ================ 

    class cCritter3DPlayer : cCritterArmedPlayer
    {
        public cCritter3DPlayer(cGame pownergame)
            : base(pownergame)
        {
            BulletClass = new cCritter3DPlayerBullet();
            Sprite = new cSpriteQuake(ModelsMD2.Marine);        //set player avatar to marine
            Sprite.SpriteAttitude = cMatrix3.scale(2, 0.8f, 0.4f);
            setRadius(cGame3D.PLAYERRADIUS);                    //Default cCritter.PLAYERRADIUS is 0.4.  
            setHealth(10);                                      //set health of player
            Mode = 'G';                                         //set default fire mode
            moveTo(_movebox.LoCorner.add(new cVector3(0.0f, 0.0f, 2.0f)));
            WrapFlag = cCritter.CLAMP; //Use CLAMP so you stop dead at edges.
            Armed = true; //Let's use bullets.
            MaxSpeed = cGame3D.MAXPLAYERSPEED;
            AbsorberFlag = true; //Keeps player from being buffeted about.
            ListenerAcceleration = 160.0f; //So Hopper can overcome gravity.  Only affects hop.

            Sprite.ModelState = State.Idle;

            // YHopper hop strength 12.0
            Listener = new cListenerQuakeScooterYHopper(0.2f, 12.0f);
            // the two arguments are walkspeed and hop strength -- JC

            addForce(new cForceGravity(50.0f)); /* Uses  gravity. Default strength is 25.0.
			Gravity	will affect player using cListenerHopper. */
            AttitudeToMotionLock = false; //It looks nicer is you don't turn the player with motion.
            Attitude = new cMatrix3(new cVector3(0.0f, 0.0f, -1.0f), new cVector3(-1.0f, 0.0f, 0.0f),
                new cVector3(0.0f, 1.0f, 0.0f), Position);
        }

        public override void update(ACView pactiveview, float dt)
        {
            base.update(pactiveview, dt); //Always call this first
        }

        public override bool collide(cCritter pcritter)
        {
            bool playerhigherthancritter = Position.Y - Radius > pcritter.Position.Y;
            /* If you are "higher" than the pcritter, as in jumping on it, you get a point
        and the critter dies.  If you are lower than it, you lose health and the
        critter also dies. To be higher, let's say your low point has to higher
        than the critter's center. We compute playerhigherthancritter before the collide,
        as collide can change the positions. */
            _baseAccessControl = 1;
            bool collided = base.collide(pcritter);
            _baseAccessControl = 0;
            if (!collided)
                return false;
            /* If you're here, you collided.  We'll treat all the guys the same -- the collision
         with a Treasure is different, but we let the Treasure contol that collision. */
            if (playerhigherthancritter)
            {
                Framework.snd.play(Sound.Goopy);
                addScore(10);
            }

            //if the pcritter has been killed
            else if (!pcritter.IsAlive())
            {
                Framework.snd.play(Sound.Crunch);//just make the sound and let pcritter.die() remove it, without player taking damage
            }

            else
            {
                damage(1);
                Framework.snd.play(Sound.Crunch);
            }
            pcritter.die();
            return true;
        }

        public override cCritterBullet shoot()
        {
            Framework.snd.play(Sound.LaserFire);
            return base.shoot();
        }

        public override bool IsKindOf(string str)
        {
            return str == "cCritter3DPlayer" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritter3DPlayer";
            }
        }

        public static char Mode { get; internal set; }
    }


    class cCritter3DPlayerBullet : cCritterBullet
    {
        public cCritter3DPlayerBullet() { }

        public override cCritterBullet Create()
        // has to be a Create function for every type of bullet -- JC
        {
            return new cCritter3DPlayerBullet();
        }

        public override void initialize(cCritterArmed pshooter)
        {
            base.initialize(pshooter);

            //if mode is set to game default - this weapon will do average damage, but has a slow delay between shots
            if (cCritter3DPlayer.Mode == 'G')
            {
                cCritterArmed.setShotWait(0.08f);//sets the delay between bullets
                _hitstrength = 2;                //sets the damage of bullet
                Sprite = new cSpriteSphere();    //sets the appearance of bullet (to sphere)
                Sprite.FillColor = Color.Black;  //sets the color of bullet
                setRadius(0.2f);                 //sets the size of bullet
            }

            else//mode is F - alternate fire mode shoots faster than default bullets, but do less damage and bounce off walls
            {
                cCritterArmed.setShotWait(0.00f); //sets the dealy between bullets
                _hitstrength = 1;                 //sets the damage of bullet
                _dieatedges = false;              //sets the bullet to bounce off walls
                Sprite = new cSpriteSphere();     //sets the appearance of bullet (to sphere)
                Sprite.FillColor = Color.DarkRed; //sets the color of bullet
                setRadius(0.08f);                 //sets the size of bullet            
            }
        }

        public override bool IsKindOf(string str)
        {
            return str == "cCritter3DPlayerBullet" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritter3DPlayerBullet";
            }
        }

        public override bool collide(cCritter pcritter)
        {   //if the bullet hits a critter
            if (isTarget(pcritter) && touch(pcritter))
            {
                delete_me();//destroys bullet on critter collision

                //if the target critter's health is still good
                if(pcritter.Health > 0)
                {
                    //if the default fire mode is on
                    if (cCritter3DPlayer.Mode == 'G')
                    {
                        pcritter.loseHealth(-2);//make the critter take 2 damage
                    }

                    //otherwise it is on rapid fire
                    else
                    {
                        pcritter.loseHealth(-1);//make the critter take 1 damage
                    }
                }

                //if the target critter's health is not so good (0 or less)
                else
                {
                    //set animation to dying, clear all forces, show critter slumped on ground then kill it
                    pcritter.Sprite.ModelState = State.FallbackDie;
                    pcritter.clearForcelist();
                    pcritter.addForce(new cForceDrag(50.0f));
                    pcritter.addForce(new cForceGravity(25.0f, new cVector3(0, -1, 0)));
                    pcritter.setIsAlive(false);
                }
                    
               
                return true;
            }
            return false;
        }
    }

    class cCritter3Dcharacter : cCritter
    {

        public cCritter3Dcharacter(cGame pownergame)
            : base(pownergame)
        {
            addForce(new cForceGravity(25.0f, new cVector3(0.0f, -1, 0.00f)));
            addForce(new cForceDrag(20.0f));  // default friction strength 0.5 
            Density = 2.0f;
            MaxSpeed = 30.0f;
            if (pownergame != null) //Just to be safe.
                Sprite = new cSpriteQuake(Framework.models.selectRandomCritter());

            // example of setting a specific model
            // setSprite(new cSpriteQuake(ModelsMD2.Knight));

            if (Sprite.IsKindOf("cSpriteQuake")) //Don't let the figurines tumble.  
            {
                AttitudeToMotionLock = false;
                Attitude = new cMatrix3(new cVector3(0.0f, 0.0f, 1.0f),
                    new cVector3(1.0f, 0.0f, 0.0f),
                    new cVector3(0.0f, 1.0f, 0.0f), Position);
                /* Orient them so they are facing towards positive Z with heads towards Y. */
            }
            Bounciness = 0.0f; //Not 1.0 means it loses a bit of energy with each bounce.
            setRadius(1.0f);   //set size of generated critter sprites
            MinTwitchThresholdSpeed = 4.0f; //Means sprite doesn't switch direction unless it's moving fast 
            randomizePosition(new cRealBox3(new cVector3(_movebox.Lox, _movebox.Loy, _movebox.Loz + 4.0f),
                new cVector3(_movebox.Hix, _movebox.Loy, _movebox.Midz - 1.0f)));
            /* I put them ahead of the player  */
            randomizeVelocity(0.0f, 30.0f, false);


            if (pownergame != null) //Then we know we added this to a game so pplayer() is valid 
                addForce(new cForceObjectSeek(Player, 0.5f));

            int begf = Framework.randomOb.random(0, 171);
            int endf = Framework.randomOb.random(0, 171);

            if (begf > endf)
            {
                int temp = begf;
                begf = endf;
                endf = temp;
            }

            Sprite.setstate(State.Other, begf, endf, StateType.Repeat);


            _wrapflag = cCritter.BOUNCE;

        }


        public override void update(ACView pactiveview, float dt)
        {
            base.update(pactiveview, dt); //Always call this first
            if ((_outcode & cRealBox3.BOX_HIZ) != 0) /* use bitwise AND to check if a flag is set. */
                delete_me(); //tell the game to remove yourself if you fall up to the hiz.
        }

        // do a delete_me if you hit the left end 

        public override void die()
        {
            Player.addScore(Value);
            base.die();
        }

        public override bool IsKindOf(string str)
        {
            return str == "cCritter3Dcharacter" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritter3Dcharacter";
            }
        }
    }

    class cCritterZombie : cCritter3Dcharacter
    {        

        public cCritterZombie(cGame pownergame)
            : base(pownergame)
        {
            setHealth(4);
  
        }
    }

    class cCritterTreasure : cCritter
    {   // Try jumping through this hoop

        public cCritterTreasure(cGame pownergame) :
        base(pownergame)
        {
            /* The sprites look nice from afar, but bitmap speed is really slow
		when you get close to them, so don't use this. */
            cPolygon ppoly = new cPolygon(24);
            ppoly.Filled = false;
            ppoly.LineWidthWeight = 0.5f;
            Sprite = ppoly;
            _collidepriority = cCollider.CP_PLAYER + 1; /* Let this guy call collide on the
			player, as his method is overloaded in a special way. */
            rotate(new cSpin((float)Math.PI / 2.0f, new cVector3(0.0f, 0.0f, 1.0f))); /* Trial and error shows this
			rotation works to make it face the z diretion. */
            setRadius(cGame3D.TREASURERADIUS);
            FixedFlag = true;
            moveTo(new cVector3(_movebox.Midx, _movebox.Midy - 2.0f,
                _movebox.Loz - 1.5f * cGame3D.TREASURERADIUS));
        }


        public override bool collide(cCritter pcritter)
        {
            if (contains(pcritter)) //disk of pcritter is wholly inside my disk 
            {
                Framework.snd.play(Sound.Clap);
                pcritter.addScore(100);
                pcritter.addHealth(1);
                pcritter.moveTo(new cVector3(_movebox.Midx, _movebox.Loy + 1.0f,
                    _movebox.Hiz - 3.0f));
                return true;
            }
            else
                return false;
        }

        //Checks if pcritter inside.

        public override int collidesWith(cCritter pothercritter)
        {
            if (pothercritter.IsKindOf("cCritter3DPlayer"))
                return cCollider.COLLIDEASCALLER;
            else
                return cCollider.DONTCOLLIDE;
        }

        /* Only collide
			with cCritter3DPlayer. */

        public override bool IsKindOf(string str)
        {
            return str == "cCritterTreasure" || base.IsKindOf(str);
        }

        public override string RuntimeClass
        {
            get
            {
                return "cCritterTreasure";
            }
        }
    }

    //======================cGame3D========================== 
    class cGame3D : cGame
    {
        public static readonly float TREASURERADIUS = 1.2f;
        public static readonly float WALLTHICKNESS = 0.5f;
        public static readonly float PLAYERRADIUS = 0.5f; //sets the player size
        public static readonly float MAXPLAYERSPEED = 10.0f; //sets the player speed
        private cCritterTreasure _ptreasure;
        private bool doorcollision;
        private bool wentThrough = false;
        private float startNewRoom;

        public cGame3D()
        {
            doorcollision = false;
            _menuflags &= ~cGame.MENU_BOUNCEWRAP;
            _menuflags |= cGame.MENU_HOPPER; //Turn on hopper listener option.
            _spritetype = cGame.ST_MESHSKIN;
            setBorder(64.0f, 16.0f, 64.0f); // size of the world

            cRealBox3 skeleton = new cRealBox3();
            skeleton.copy(_border);
            setSkyBox(skeleton);
            /* In this world the coordinates are screwed up to match the screwed up
            listener that I use.  I should fix the listener and the coords.
            Meanwhile...
            I am flying into the screen from HIZ towards LOZ, and
            LOX below and HIX above and
            LOY on the right and HIY on the left. */
            SkyBox.setSideTexture(cRealBox3.HIZ, BitmapRes.Wall3); //Make the near HIZ transparent 
            SkyBox.setSideTexture(cRealBox3.LOZ, BitmapRes.Wall3); //Far wall 
            SkyBox.setSideTexture(cRealBox3.LOX, BitmapRes.Wall3); //left wall 
            SkyBox.setSideTexture(cRealBox3.HIX, BitmapRes.Wall3); //right wall 
            SkyBox.setSideTexture(cRealBox3.LOY, BitmapRes.Concrete); //floor 
            SkyBox.setSideTexture(cRealBox3.HIY, BitmapRes.Concrete); //ceiling 

            WrapFlag = cCritter.BOUNCE;
            _seedcount = 7;
            setPlayer(new cCritter3DPlayer(this));
            _ptreasure = new cCritterTreasure(this);

            cCritterDoor pdwall = new cCritterDoor(
                new cVector3(_border.Lox, _border.Loy, _border.Midz),
                new cVector3(_border.Lox, _border.Midy - 3, _border.Midz),
                0.1f, 2, this);
            cSpriteTextureBox pspritedoor =
                new cSpriteTextureBox(pdwall.Skeleton, BitmapRes.Door);
            pdwall.Sprite = pspritedoor;
        }

        //setRoom1 creates a new room when the player runs through the previous door
        public void setRoom1()
        {
            //remove critters and wall
            Biota.purgeCritters("cCritterWall");
            Biota.purgeCritters("cCritter3Dcharacter");

            setBorder(64.0f, 16.0f, 64.0f); // size of the world

            //create new room 'shell'
            cRealBox3 skeleton = new cRealBox3();
            skeleton.copy(_border);
            setSkyBox(skeleton);

            //set textures and graphics
            SkyBox.setSideTexture(cRealBox3.HIZ, BitmapRes.Wall3); //Make the near HIZ transparent 
            SkyBox.setSideTexture(cRealBox3.LOZ, BitmapRes.Wall3); //Far wall 
            SkyBox.setSideTexture(cRealBox3.LOX, BitmapRes.Wall3); //left wall 
            SkyBox.setSideTexture(cRealBox3.HIX, BitmapRes.Wall3); //right wall 
            SkyBox.setSideTexture(cRealBox3.LOY, BitmapRes.Concrete); //floor 
            SkyBox.setSideTexture(cRealBox3.HIY, BitmapRes.Concrete); //ceiling 

            //set number of critters to be created. Adjust numbers for increasing difficulty between rooms
            _seedcount = 7;

            WrapFlag = cCritter.BOUNCE;
            setPlayer(new cCritter3DPlayer(this));
            _ptreasure = new cCritterTreasure(this);

            //create a door at a new position in the room
            cCritterDoor pdwall = new cCritterDoor(
                new cVector3(_border.Lox, _border.Loy, _border.Midz),
                new cVector3(_border.Lox, _border.Midy - 3, _border.Midz),
                0.1f, 2, this);
            cSpriteTextureBox pspritedoor =
                new cSpriteTextureBox(pdwall.Skeleton, BitmapRes.Door);
            pdwall.Sprite = pspritedoor;

            //move player to new position in next room
            Player.moveTo(new cVector3(0.0f, -10.0f, 32.0f));

            //set collision flag and reset age of new room
            wentThrough = true;
            startNewRoom = Age;
        }

        public override void seedCritters()
        {
            Biota.purgeCritters("cCritterBullet");
            Biota.purgeCritters("cCritter3Dcharacter");
            for (int i = 0; i < _seedcount; i++)
                new cCritter3Dcharacter(this);
            Player.moveTo(new cVector3(0.0f, Border.Loy, Border.Hiz - 3.0f));
            /* We start at hiz and move towards	loz */
        }


        public void setdoorcollision() { doorcollision = true; }

        public override ACView View
        {
            set
            {
                base.View = value; //You MUST call the base class method here.
                value.setUseBackground(ACView.FULL_BACKGROUND); /* The background type can be
			    ACView.NO_BACKGROUND, ACView.SIMPLIFIED_BACKGROUND, or 
			    ACView.FULL_BACKGROUND, which often means: nothing, lines, or
			    planes&bitmaps, depending on how the skybox is defined. */
                value.pviewpointcritter().Listener = new cListenerViewerRide();
            }
        }


        public override cCritterViewer Viewpoint
        {
            set
            {
                if (value.Listener.RuntimeClass == "cListenerViewerRide")
                {
                    value.setViewpoint(new cVector3(0.0f, 0.3f, -1.0f), _border.Center);
                    //Always make some setViewpoint call simply to put in a default zoom.
                    value.zoom(0.35f); //Wideangle 
                    cListenerViewerRide prider = (cListenerViewerRide)(value.Listener);
                    prider.Offset = (new cVector3(-1.5f, 0.0f, 1.0f)); /* This offset is in the coordinate
				    system of the player, where the negative X axis is the negative of the
				    player's tangent direction, which means stand right behind the player. */
                }
                else //Not riding the player.
                {
                    value.zoom(1.0f);
                    /* The two args to setViewpoint are (directiontoviewer, lookatpoint).
				    Note that directiontoviewer points FROM the origin TOWARDS the viewer. */
                    value.setViewpoint(new cVector3(0.0f, 0.3f, 1.0f), _border.Center);
                }
            }
        }

        /* Move over to be above the
			lower left corner where the player is.  In 3D, use a low viewpoint low looking up. */

        public override void adjustGameParameters()
        {
            // (1) End the game if the player is dead 
            if ((Health == 0) && !_gameover) //Player's been killed and game's not over.
            {
                _gameover = true;
                Player.addScore(_scorecorrection); // So user can reach _maxscore  
                //Framework.snd.play(Sound.Hallelujah);
                return;
            }
            // (2) Also don't let the the model count diminish.
            //(need to recheck propcount in case we just called seedCritters).
            int modelcount = Biota.count("cCritter3Dcharacter");
            int modelstoadd = _seedcount - modelcount;
            for (int i = 0; i < modelstoadd; i++)
                new cCritter3Dcharacter(this);
            // (3) Maybe check some other conditions.

            if (wentThrough && (Age - startNewRoom) > 2.0f)
            {
                wentThrough = false;
            }

            if (doorcollision == true)
            {
                setRoom1();
                doorcollision = false;
            }
        }

    }

}