using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DominionWar;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;
    private Texture2D _oberthTexture;

    struct Star
    {
        public Vector2 Position;
        public float Speed;
        public Color Color;
        public int Size;
    }
    private List<Star> _stars = new List<Star>();

    // Game States
    enum GameState { StrategicView, TacticalMission, GameOverVictory, GameOverDefeat }
    private GameState _currentState = GameState.StrategicView;

    // Player Data
    enum ShipClass { Oberth, Miranda, Excelsior, Defiant, Akira, Galaxy, Sovereign }
    private ShipClass _playerShip = ShipClass.Oberth;
    private int _missionsCompleted = 0;
    private int _dominionStrength = 100;
    private int _federationStrength = 100;
    private int _score = 0;
    private int _playerShields = 100;

    private readonly int[,,] _font = new int[10, 5, 3] {
        { {1,1,1}, {1,0,1}, {1,0,1}, {1,0,1}, {1,1,1} },
        { {0,1,0}, {1,1,0}, {0,1,0}, {0,1,0}, {1,1,1} },
        { {1,1,1}, {0,0,1}, {1,1,1}, {1,0,0}, {1,1,1} },
        { {1,1,1}, {0,0,1}, {1,1,1}, {0,0,1}, {1,1,1} },
        { {1,0,1}, {1,0,1}, {1,1,1}, {0,0,1}, {0,0,1} },
        { {1,1,1}, {1,0,0}, {1,1,1}, {0,0,1}, {1,1,1} },
        { {1,1,1}, {1,0,0}, {1,1,1}, {1,0,1}, {1,1,1} },
        { {1,1,1}, {0,0,1}, {0,0,1}, {0,0,1}, {0,0,1} },
        { {1,1,1}, {1,0,1}, {1,1,1}, {1,0,1}, {1,1,1} },
        { {1,1,1}, {1,0,1}, {1,1,1}, {0,0,1}, {1,1,1} }
    };

    // Tactical Data
    private Vector2 _playerPos;
    private List<Vector2> _enemies = new List<Vector2>();
    private List<Vector2> _torpedoes = new List<Vector2>();
    private List<Vector2> _enemyProjectiles = new List<Vector2>();
    private Random _rng = new Random();

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // TODO: Add your initialization logic here

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _oberthTexture = Texture2D.FromFile(GraphicsDevice, "assets/ships/Oberth.png");

        for (int i = 0; i < 150; i++)
        {
            _stars.Add(new Star
            {
                Position = new Vector2(_rng.Next(0, 800), _rng.Next(0, 600)),
                Speed = (float)(_rng.NextDouble() * 50 + 20),
                Color = Color.Lerp(Color.White, Color.DarkGray, (float)_rng.NextDouble()),
                Size = _rng.Next(1, 3)
            });
        }
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || kb.IsKeyDown(Keys.Escape))
            Exit();

        for (int i = 0; i < _stars.Count; i++)
        {
            var star = _stars[i];
            star.Position.Y += star.Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (star.Position.Y > GraphicsDevice.Viewport.Height)
            {
                star.Position.Y = 0;
                star.Position.X = _rng.Next(0, GraphicsDevice.Viewport.Width);
            }
            _stars[i] = star;
        }

        if (_currentState == GameState.StrategicView)
        {
            // Press Enter to start next tactical mission
            if (kb.IsKeyDown(Keys.Enter))
            {
                _currentState = GameState.TacticalMission;
                _playerPos = new Vector2(400, 300);
                _playerShields = 100;
                _enemies.Clear();
                _torpedoes.Clear();
                _enemyProjectiles.Clear();
                int enemyCount = _rng.Next(1, 4 + _missionsCompleted);
                for(int i = 0; i < enemyCount; i++)
                    _enemies.Add(new Vector2(_rng.Next(0,800), _rng.Next(0, 200)));
            }
        }
        else if (_currentState == GameState.TacticalMission)
        {
            // Move player
            if (kb.IsKeyDown(Keys.W)) _playerPos.Y -= 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kb.IsKeyDown(Keys.S)) _playerPos.Y += 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kb.IsKeyDown(Keys.A)) _playerPos.X -= 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kb.IsKeyDown(Keys.D)) _playerPos.X += 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Fire torpedoes
            if (kb.IsKeyDown(Keys.Space) && _torpedoes.Count < 5)
            {
                _torpedoes.Add(new Vector2(_playerPos.X, _playerPos.Y - 20));
            }

            // Move Torpedoes
            for (int i = _torpedoes.Count - 1; i >= 0; i--)
            {
                _torpedoes[i] = new Vector2(_torpedoes[i].X, _torpedoes[i].Y - 400f * (float)gameTime.ElapsedGameTime.TotalSeconds);
                if (_torpedoes[i].Y < 0) _torpedoes.RemoveAt(i);
            }

            // Move Enemy Projectiles
            for (int i = _enemyProjectiles.Count - 1; i >= 0; i--)
            {
                _enemyProjectiles[i] = new Vector2(_enemyProjectiles[i].X, _enemyProjectiles[i].Y + 300f * (float)gameTime.ElapsedGameTime.TotalSeconds);
                if (_enemyProjectiles[i].Y > 600)
                {
                    _enemyProjectiles.RemoveAt(i);
                }
                else if (Vector2.Distance(_enemyProjectiles[i], _playerPos) < 30)
                {
                    _playerShields -= 10;
                    _enemyProjectiles.RemoveAt(i);
                }
            }

            // Move Enemies and Collision
            bool won = true;
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                won = false;
                _enemies[i] = new Vector2(_enemies[i].X, _enemies[i].Y + 50f * (float)gameTime.ElapsedGameTime.TotalSeconds);

                // Enemies fire back randomly
                if (_rng.NextDouble() < 0.01)
                {
                    _enemyProjectiles.Add(new Vector2(_enemies[i].X, _enemies[i].Y + 15));
                }

                // Collision with torpedoes
                bool hit = false;
                for (int j = _torpedoes.Count - 1; j >= 0; j--)
                {
                    if (Vector2.Distance(_enemies[i], _torpedoes[j]) < 30)
                    {
                        hit = true;
                        _score += 100;
                        _torpedoes.RemoveAt(j);
                        break;
                    }
                }

                if (hit)
                {
                    _enemies.RemoveAt(i);
                }
                else if (_enemies[i].Y > 600 || Vector2.Distance(_enemies[i], _playerPos) < 40)
                {
                    // Enemy escaped or hit player, take shield damage
                    _playerShields -= 20;
                    _enemies.RemoveAt(i);
                }
            }

            if (_playerShields <= 0)
            {
                _federationStrength -= 20; // mission failed penalty
                _currentState = GameState.StrategicView;
                if (_federationStrength <= 0) _currentState = GameState.GameOverDefeat;
            }
            else if (won)
            {
                // Mission complete!
                _dominionStrength -= 15;
                _missionsCompleted++;
                if (_missionsCompleted % 2 == 0 && (int)_playerShip < 6)
                    _playerShip++; // Promote ship class

                _currentState = GameState.StrategicView;

                if (_dominionStrength <= 0) _currentState = GameState.GameOverVictory;
                if (_federationStrength <= 0) _currentState = GameState.GameOverDefeat;
            }
            else if (_federationStrength <= 0)
            {
                _currentState = GameState.GameOverDefeat;
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin();

        if (_currentState == GameState.StrategicView)
        {
            // Simple visual representation of strategic map
            _spriteBatch.Draw(_pixel, new Rectangle(100, 100, (_federationStrength * 3), 40), Color.Blue);
            _spriteBatch.Draw(_pixel, new Rectangle(100, 200, (_dominionStrength * 3), 40), Color.Red);

            // Player class marker (e.g. size reflects ship class)
            int shipSize = 10 + (int)_playerShip * 5;
            _spriteBatch.Draw(_pixel, new Rectangle(100, 300, shipSize, shipSize), Color.White);

            // Blue represents Federation health, Red represents Dominion health
            // White box represents current ship (gets bigger as promoted)
        }
        else if (_currentState == GameState.TacticalMission)
        {
            // Draw Score
            DrawNumber(_score, 20, 20, Color.Yellow);

            // Draw Shields
            _spriteBatch.Draw(_pixel, new Rectangle(20, 45, Math.Max(0, _playerShields) * 2, 10), Color.Cyan);

            // Draw Stars
            foreach (var star in _stars)
            {
                _spriteBatch.Draw(_pixel, new Rectangle((int)star.Position.X, (int)star.Position.Y, star.Size, star.Size), star.Color);
            }

            // Draw Player
            int shipSize = 10 + (int)_playerShip * 3;
            var playerRect = new Rectangle((int)_playerPos.X - shipSize / 2, (int)_playerPos.Y - shipSize / 2, shipSize, shipSize);

            if (_playerShip == ShipClass.Oberth && _oberthTexture != null)
            {
                float scale = 64f / Math.Max(_oberthTexture.Width, _oberthTexture.Height);
                int w = (int)(_oberthTexture.Width * scale);
                int h = (int)(_oberthTexture.Height * scale);
                playerRect = new Rectangle((int)_playerPos.X - w / 2, (int)_playerPos.Y - h / 2, w, h);
                _spriteBatch.Draw(_oberthTexture, playerRect, Color.White);
            }
            else
            {
                _spriteBatch.Draw(_pixel, playerRect, Color.Cyan);
            }

            // Draw Torpedoes
            foreach(var torp in _torpedoes)
                _spriteBatch.Draw(_pixel, new Rectangle((int)torp.X - 2, (int)torp.Y - 2, 4, 8), Color.Orange);

            // Draw Enemy Projectiles
            foreach(var proj in _enemyProjectiles)
                _spriteBatch.Draw(_pixel, new Rectangle((int)proj.X - 2, (int)proj.Y - 2, 4, 8), Color.Red);

            // Draw Enemies (JemHadar/Breen/Cardassian represented as pink/purple boxes)
            foreach(var enemy in _enemies)
                _spriteBatch.Draw(_pixel, new Rectangle((int)enemy.X - 15, (int)enemy.Y - 15, 30, 30), Color.Magenta);
        }
        else if (_currentState == GameState.GameOverVictory)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(200, 200, 400, 200), Color.LimeGreen);
        }
        else if (_currentState == GameState.GameOverDefeat)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(200, 200, 400, 200), Color.DarkRed);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawNumber(int number, int x, int y, Color color)
    {
        string str = number.ToString();
        for (int i = 0; i < str.Length; i++)
        {
            int digit = str[i] - '0';
            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (_font[digit, row, col] == 1)
                        _spriteBatch.Draw(_pixel, new Rectangle(x + i * 16 + col * 4, y + row * 4, 4, 4), color);
                }
            }
        }
    }
}
