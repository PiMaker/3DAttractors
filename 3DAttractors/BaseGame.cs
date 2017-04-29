using System;
using System.Collections.Generic;
using Hjson;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace _3DAttractors
{
    /// <summary>
    ///     This is the main type for your game.
    /// </summary>
    public class BaseGame : Game
    {
        private readonly List<Vector3> attractors = new List<Vector3>();
        private readonly GraphicsDeviceManager graphics;
        private VertexPositionColor[] attractorCube;
        private Color color;

        private VertexPositionColor[] cube;
        private Vector3 current;
        private Vector3 currentStart;
        private VertexPositionColor[] currentCube;
        private BasicEffect effect;

        private SpriteFont font;

        private int iterationsPerSecond;

        private KeyboardState keyState, oldKeyState;
        private float movementSpeed;
        private List<Vector3> points = new List<Vector3>();
        private float pointSize;
        private SpriteBatch spriteBatch;

        private float angle = 0f;

        private bool running = false;
        private bool spinning = false;

        private Matrix view, project;

        private long iterations = 0;

        public BaseGame()
        {
            this.graphics = new GraphicsDeviceManager(this);
            this.Content.RootDirectory = "Content";
        }

        /// <summary>
        ///     Allows the game to perform any initialization it needs to before starting to run.
        ///     This is where it can query for any required services and load any non-graphic
        ///     related content.  Calling base.Initialize will enumerate through any components
        ///     and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            this.IsMouseVisible = true;
            this.graphics.PreferredBackBufferWidth = 1500;
            this.graphics.PreferredBackBufferHeight = 1500;
            this.graphics.ApplyChanges();

            this.keyState = this.oldKeyState = Keyboard.GetState();

            this.LoadConfig();

            this.view = Matrix.CreateLookAt(new Vector3(0.5f, 0.5f, -2f), Vector3.One * 0.5f, Vector3.Up);

            this.project = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4*0.8f,
                this.Window.ClientBounds.Width / (float) this.Window.ClientBounds.Height, 1, 100);

            base.Initialize();
        }

        /// <summary>
        ///     LoadContent will be called once per game and is the place to load
        ///     all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            this.spriteBatch = new SpriteBatch(this.GraphicsDevice);

            this.font = this.Content.Load<SpriteFont>("Font");

            this.effect = new BasicEffect(this.GraphicsDevice);
        }

        /// <summary>
        ///     UnloadContent will be called once per game and is the place to unload
        ///     game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
        }

        /// <summary>
        ///     Allows the game to run logic such as updating the world,
        ///     checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            this.keyState = Keyboard.GetState();

            if (this.keyState.IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }

            // Reset
            if (this.keyState.IsKeyDown(Keys.R) && this.oldKeyState.IsKeyUp(Keys.R))
            {
                this.points.Clear();
                this.current = this.currentStart;
                this.angle = 0f;
                this.iterations = 0;
                this.running = false;
            }
            // Reload config
            else if (this.keyState.IsKeyDown(Keys.T) && this.oldKeyState.IsKeyUp(Keys.T))
            {
                this.LoadConfig();
            }
            // Running
            else if (this.keyState.IsKeyDown(Keys.P) && this.oldKeyState.IsKeyUp(Keys.P))
            {
                this.running = !this.running;
            }
            // Rotation
            else if (this.keyState.IsKeyDown(Keys.E) && this.oldKeyState.IsKeyUp(Keys.E))
            {
                this.spinning = !this.spinning;
            }

            if (this.spinning)
            {
                angle += 0.015f;
            }

            this.oldKeyState = this.keyState;
            base.Update(gameTime);
        }

        /// <summary>
        ///     This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            this.GraphicsDevice.Clear(Color.CornflowerBlue);

            this.effect.World = Matrix.Identity;
            this.effect.View = this.view;
            this.effect.Projection = this.project;
            this.effect.VertexColorEnabled = true;
            this.effect.CurrentTechnique.Passes[0].Apply();

            foreach (var attractor in this.attractors)
            {
                this.DrawCube(this.attractorCube, attractor, 1f);
            }

            this.DrawCube(this.currentCube, this.current, 1f);

            foreach (var p in this.points)
            {
                this.DrawCube(this.cube, p, this.pointSize);
            }

            this.spriteBatch.Begin();
            this.spriteBatch.DrawString(this.font,
                $"Esc: Exit / P: {(this.running ? "Stop" : "Start")} / R: Reset / T: Reload config / E: {(this.spinning ? "Disable" : "Enable")} rotation\nIteration ({this.iterationsPerSecond}/s): {this.iterations}",
                Vector2.One * 10,
                Color.White);
            this.spriteBatch.End();

            base.Draw(gameTime);
        }

        private void LoadConfig()
        {
            var config = HjsonValue.Load("config.hjson");

            this.iterationsPerSecond = config.Qo()["IterationsPerSecond"];
            this.movementSpeed = config.Qo()["MovementSpeed"];
            this.pointSize = config.Qo()["PointSize"];

            var colorString = config.Qo()["Color"].Qs().Substring(1);
            this.color = new Color(Convert.ToInt32(colorString.Substring(0, 2), 16),
                Convert.ToInt32(colorString.Substring(2, 2), 16),
                Convert.ToInt32(colorString.Substring(4, 2), 16));

            this.attractors.Clear();
            foreach (var attractor in config.Qo()["Attractors"].Qa())
            {
                this.attractors.Add(new Vector3(attractor.Qo()["X"], attractor.Qo()["Y"], attractor.Qo()["Z"]));
            }

            var start = config.Qo()["Start"];
            this.current = this.currentStart = new Vector3(start.Qo()["X"], start.Qo()["Y"], start.Qo()["Z"]);

            this.cube = this.BuildCube(this.color);
            this.currentCube = this.BuildCube(Color.Black);
            this.attractorCube = this.BuildCube(Color.White);
        }

        private void DrawCube(VertexPositionColor[] model, Vector3 position, float size)
        {
            this.effect.World = Matrix.CreateScale(size * 0.0025f) * Matrix.CreateTranslation(position) * Matrix.CreateRotationY(this.angle);
            this.effect.CurrentTechnique.Passes[0].Apply();
            this.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, model, 0, model.Length/3);
        }

        private VertexPositionColor[] BuildCube(Color cubeColor)
        {
            var topLeftFront = new Vector3(-1.0f, 1.0f, -1.0f);
            var topRightFront = new Vector3(1.0f, 1.0f, -1.0f);
            var topLeftBack = new Vector3(-1.0f, 1.0f, 1.0f);
            var topRightBack = new Vector3(1.0f, 1.0f, 1.0f);

            var botLeftFront = new Vector3(-1.0f, -1.0f, -1.0f);
            var botRightFront = new Vector3(1.0f, -1.0f, -1.0f);
            var botLeftBack = new Vector3(-1.0f, -1.0f, 1.0f);
            var botRightBack = new Vector3(1.0f, -1.0f, 1.0f);

            var arr = new VertexPositionColor[36];

            arr[0] = new VertexPositionColor(topLeftFront, cubeColor);
            arr[1] = new VertexPositionColor(botLeftFront, cubeColor);
            arr[2] = new VertexPositionColor(topRightFront, cubeColor);
            arr[3] = new VertexPositionColor(botLeftFront, cubeColor);
            arr[4] = new VertexPositionColor(botRightFront, cubeColor);
            arr[5] = new VertexPositionColor(topRightFront, cubeColor);

            arr[6] = new VertexPositionColor(topLeftBack, cubeColor);
            arr[7] = new VertexPositionColor(topRightBack, cubeColor);
            arr[8] = new VertexPositionColor(botLeftBack, cubeColor);
            arr[9] = new VertexPositionColor(botLeftBack, cubeColor);
            arr[10] = new VertexPositionColor(topRightBack, cubeColor);
            arr[11] = new VertexPositionColor(botRightBack, cubeColor);

            arr[12] = new VertexPositionColor(topLeftFront, cubeColor);
            arr[13] = new VertexPositionColor(topRightBack, cubeColor);
            arr[14] = new VertexPositionColor(topLeftBack, cubeColor);
            arr[15] = new VertexPositionColor(topLeftFront, cubeColor);
            arr[16] = new VertexPositionColor(topRightFront, cubeColor);
            arr[17] = new VertexPositionColor(topRightBack, cubeColor);

            arr[18] = new VertexPositionColor(botLeftFront, cubeColor);
            arr[19] = new VertexPositionColor(botLeftBack, cubeColor);
            arr[20] = new VertexPositionColor(botRightBack, cubeColor);
            arr[21] = new VertexPositionColor(botLeftFront, cubeColor);
            arr[22] = new VertexPositionColor(botRightBack, cubeColor);
            arr[23] = new VertexPositionColor(botRightFront, cubeColor);

            arr[24] = new VertexPositionColor(topLeftFront, cubeColor);
            arr[25] = new VertexPositionColor(botLeftBack, cubeColor);
            arr[26] = new VertexPositionColor(botLeftFront, cubeColor);
            arr[27] = new VertexPositionColor(topLeftBack, cubeColor);
            arr[28] = new VertexPositionColor(botLeftBack, cubeColor);
            arr[29] = new VertexPositionColor(topLeftFront, cubeColor);

            arr[30] = new VertexPositionColor(topRightFront, cubeColor);
            arr[31] = new VertexPositionColor(botRightFront, cubeColor);
            arr[32] = new VertexPositionColor(botRightBack, cubeColor);
            arr[33] = new VertexPositionColor(topRightBack, cubeColor);
            arr[34] = new VertexPositionColor(topRightFront, cubeColor);
            arr[35] = new VertexPositionColor(botRightBack, cubeColor);

            return arr;
        }
    }
}