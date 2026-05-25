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
    private Texture2D _jemHadarFighterTexture;
    private Texture2D _jemHadarBattleshipTexture;
    private Texture2D _constitutionTexture;
    private Texture2D _excelsiorTexture;

    struct Star
    {
        public Vector2 Position;
        public float Speed;
        public Color Color;
        public int Size;
    }
    private List<Star> _stars = new List<Star>();

    public class Enemy
    {
        public Vector2 Position;
        public bool IsBoss;
        public int Health;
    }

    // Game States
    enum GameState { StrategicView, TacticalMission, GameOverVictory, GameOverDefeat }
    private GameState _currentState = GameState.StrategicView;

    // Player Data
    enum ShipClass { Oberth, Constitution, Excelsior, Defiant, Akira, Galaxy, Sovereign }
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
    private List<Enemy> _enemies = new List<Enemy>();
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

        try { _oberthTexture = Texture2D.FromFile(GraphicsDevice, "assets/ships/Oberth.png"); } catch { }
        try { _constitutionTexture = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/Consitution.png"); } catch { }
        try { _excelsiorTexture = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/Excelsior.png"); } catch { }
        try { _jemHadarFighterTexture = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/Vanguard.png"); } catch { }
        try { _jemHadarBattleshipTexture = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/JemHadar_Battleship.png"); } catch { }

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

                bool isBoss = (_missionsCompleted > 0 && _missionsCompleted % 3 == 0);
                if (isBoss)
                {
                    _enemies.Add(new Enemy { Position = new Vector2(400, 100), IsBoss = true, Health = 100 });
                }
                else
                {
                    int enemyCount = _rng.Next(1, 4 + _missionsCompleted);
                    for(int i = 0; i < enemyCount; i++)
                        _enemies.Add(new Enemy { Position = new Vector2(_rng.Next(0,800), _rng.Next(0, 200)), IsBoss = false, Health = 20 });
                }
            }
        }
        else if (_currentState == GameState.TacticalMission)
        {
            // Move player
            if (kb.IsKeyDown(Keys.W)) _playerPos.Y -= 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kb.IsKeyDown(Keys.S)) _playerPos.Y += 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kb.IsKeyDown(Keys.A)) _playerPos.X -= 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (kb.IsKeyDown(Keys.D)) _playerPos.X += 200f * (float)gameTime.ElapsedGameTime.TotalSeconds;

            _playerPos.X = MathHelper.Clamp(_playerPos.X, 30, GraphicsDevice.Viewport.Width - 30);
            _playerPos.Y = MathHelper.Clamp(_playerPos.Y, 30, GraphicsDevice.Viewport.Height - 30);

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
                var enemy = _enemies[i];
                enemy.Position.Y += (enemy.IsBoss ? 10f : 50f) * (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Enemies fire back randomly
                if (_rng.NextDouble() < (enemy.IsBoss ? 0.05 : 0.01))
                {
                    _enemyProjectiles.Add(new Vector2(enemy.Position.X, enemy.Position.Y + (enemy.IsBoss ? 50 : 15)));
                }

                // Collision with torpedoes
                bool hit = false;
                for (int j = _torpedoes.Count - 1; j >= 0; j--)
                {
                    float hitRadius = enemy.IsBoss ? 50 : 30;
                    if (Vector2.Distance(enemy.Position, _torpedoes[j]) < hitRadius)
                    {
                        hit = true;
                        _score += 100;
                        _torpedoes.RemoveAt(j);
                        break;
                    }
                }

                if (hit)
                {
                    enemy.Health -= 20;
                    if (enemy.Health <= 0)
                    {
                        if (enemy.IsBoss) _score += 1000;
                        _enemies.RemoveAt(i);
                    }
                }
                else if (enemy.Position.Y > 600 || Vector2.Distance(enemy.Position, _playerPos) < (enemy.IsBoss ? 70 : 40))
                {
                    // Enemy escaped or hit player, take shield damage
                    _playerShields -= enemy.IsBoss ? 50 : 20;
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
            else if (_playerShip == ShipClass.Constitution && _constitutionTexture != null)
            {
                float scale = 80f / Math.Max(_constitutionTexture.Width, _constitutionTexture.Height);
                int w = (int)(_constitutionTexture.Width * scale);
                int h = (int)(_constitutionTexture.Height * scale);
                playerRect = new Rectangle((int)_playerPos.X - w / 2, (int)_playerPos.Y - h / 2, w, h);
                _spriteBatch.Draw(_constitutionTexture, playerRect, Color.White);
            }
            else if (_playerShip == ShipClass.Excelsior && _excelsiorTexture != null)
            {
                float scale = 96f / Math.Max(_excelsiorTexture.Width, _excelsiorTexture.Height);
                int w = (int)(_excelsiorTexture.Width * scale);
                int h = (int)(_excelsiorTexture.Height * scale);
                playerRect = new Rectangle((int)_playerPos.X - w / 2, (int)_playerPos.Y - h / 2, w, h);
                _spriteBatch.Draw(_excelsiorTexture, playerRect, Color.White);
            }
            else
            {
                _spriteBatch.Draw(_pixel, playerRect, Color.Cyan);
            }

            // Draw Torpedoes (Photon Torpedoes)
            foreach(var torp in _torpedoes)
            {
                _spriteBatch.Draw(_pixel, new Rectangle((int)torp.X - 4, (int)torp.Y - 5, 8, 10), Color.OrangeRed);
                _spriteBatch.Draw(_pixel, new Rectangle((int)torp.X - 2, (int)torp.Y - 3, 4, 6), Color.LightYellow);
            }

            // Draw Enemy Projectiles (Dominion Polaron Beams)
            foreach(var proj in _enemyProjectiles)
            {
                _spriteBatch.Draw(_pixel, new Rectangle((int)proj.X - 2, (int)proj.Y - 10, 4, 20), Color.DarkMagenta);
                _spriteBatch.Draw(_pixel, new Rectangle((int)proj.X - 1, (int)proj.Y - 8, 2, 16), Color.Cyan);
            }

            // Draw Enemies (JemHadar/Breen/Cardassian represented as pink/purple boxes)
            foreach(var enemy in _enemies)
            {
                if (enemy.IsBoss)
                {
                    if (_jemHadarBattleshipTexture != null)
                    {
                        float scale = 128f / Math.Max(_jemHadarBattleshipTexture.Width, _jemHadarBattleshipTexture.Height);
                        int w = (int)(_jemHadarBattleshipTexture.Width * scale);
                        int h = (int)(_jemHadarBattleshipTexture.Height * scale);
                        _spriteBatch.Draw(_jemHadarBattleshipTexture, new Rectangle((int)enemy.Position.X - w / 2, (int)enemy.Position.Y - h / 2, w, h), Color.White);
                    }
                    else
                    {
                        _spriteBatch.Draw(_pixel, new Rectangle((int)enemy.Position.X - 40, (int)enemy.Position.Y - 40, 80, 80), Color.DarkMagenta);
                    }
                }
                else
                {
                    if (_jemHadarFighterTexture != null)
                    {
                        float scale = 64f / Math.Max(_jemHadarFighterTexture.Width, _jemHadarFighterTexture.Height);
                        int w = (int)(_jemHadarFighterTexture.Width * scale);
                        int h = (int)(_jemHadarFighterTexture.Height * scale);
                        _spriteBatch.Draw(_jemHadarFighterTexture, new Rectangle((int)enemy.Position.X - w / 2, (int)enemy.Position.Y - h / 2, w, h), Color.White);
                    }
                    else
                    {
                        _spriteBatch.Draw(_pixel, new Rectangle((int)enemy.Position.X - 15, (int)enemy.Position.Y - 15, 30, 30), Color.Magenta);
                    }
                }
            }
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
