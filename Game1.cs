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
    private Texture2D _particleTexture;
    private Texture2D _planetTexture;
    private Texture2D _oberthTexture;
    private Texture2D _jemHadarFighterTexture;
    private Texture2D _jemHadarBattleshipTexture;
    private Texture2D _constitutionTexture;
    private Texture2D _excelsiorTexture;

    // Strategic View Textures
    private Texture2D _strategicBackground;
    private Texture2D _federationLogo;
    private Texture2D _dominionLogo;

    struct BackgroundElement
    {
        public Vector2 Position;
        public float Speed;
        public Color Color;
        public float Scale;
        public float Rotation;
        public float RotationSpeed;
    }
    private List<BackgroundElement> _nebulae = new List<BackgroundElement>();
    private List<BackgroundElement> _planets = new List<BackgroundElement>();

    struct Star
    {
        public Vector2 Position;
        public float Speed;
        public Color Color;
        public int Size;
    }
    private List<Star> _stars = new List<Star>();

    struct Explosion
    {
        public Vector2 Position;
        public float Timer;
        public float Duration;
        public float MaxRadius;
    }

    public class Enemy
    {
        public Vector2 Position;
        public bool IsBoss;
        public int Health;
    }

    // Game States
    enum GameState { StrategicView, TacticalMission, MissionReport, GameOverVictory, GameOverDefeat }
    private GameState _currentState = GameState.StrategicView;

    // Player Data
    enum ShipClass { Oberth, Constitution, Excelsior, Defiant, Akira, Galaxy, Sovereign }
    private ShipClass _playerShip = ShipClass.Oberth;
    private int _missionsCompleted = 0;
    private int _livesRemaining = 3;
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
    private List<Explosion> _explosions = new List<Explosion>();
    private Random _rng = new Random();
    private bool _missionEnding = false;
    private float _missionEndTimer = 0f;
    private bool _missionWon = false;

    // Camera/Effects
    private Vector2 _cameraOffset = Vector2.Zero;
    private float _screenShakeTimer = 0f;
    private float _screenShakeMagnitude = 0f;

    // Mission Stats
    private int _missionShipsDestroyed = 0;
    private int _missionShipsEscaped = 0;
    private int _missionDamageTaken = 0;
    private float _reportTimer = 0f;

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

        _particleTexture = new Texture2D(GraphicsDevice, 32, 32);
        Color[] pData = new Color[32 * 32];
        Vector2 center = new Vector2(16, 16);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(center, new Vector2(x, y));
                float alpha = (float)Math.Pow(1f - MathHelper.Clamp(dist / 16f, 0, 1), 2);
                pData[y * 32 + x] = Color.White * alpha;
            }
        }
        _particleTexture.SetData(pData);

        // Procedural Planet Texture
        _planetTexture = new Texture2D(GraphicsDevice, 64, 64);
        Color[] pData2 = new Color[64 * 64];
        Vector2 pCenter = new Vector2(32, 32);
        for(int y = 0; y < 64; y++) {
            for(int x = 0; x < 64; x++) {
                float dist = Vector2.Distance(pCenter, new Vector2(x, y));
                if (dist <= 31.5f) {
                    float nx = (x - 32) / 32f;
                    float ny = -(y - 32) / 32f;
                    float nz = (float)Math.Sqrt(Math.Max(0, 1 - nx*nx - ny*ny));
                    Vector3 normal = new Vector3(nx, ny, nz);
                    Vector3 lightDir = Vector3.Normalize(new Vector3(-1f, 1f, 1f));
                    float diffuse = MathHelper.Clamp(Vector3.Dot(normal, lightDir), 0f, 1f);
                    float intensity = MathHelper.Clamp(diffuse + 0.2f, 0f, 1f);
                    pData2[y * 64 + x] = new Color(intensity, intensity, intensity, 1f);
                } else {
                    pData2[y * 64 + x] = Color.Transparent;
                }
            }
        }
        _planetTexture.SetData(pData2);

        // Populate Nebulae and Planets
        for (int i = 0; i < 15; i++)
        {
            _nebulae.Add(new BackgroundElement {
                Position = new Vector2(_rng.Next(-200, 1000), _rng.Next(-200, 800)),
                Speed = (float)(_rng.NextDouble() * 10 + 5),
                Color = (_rng.Next(2) == 0 ? Color.MediumVioletRed : Color.DarkBlue) * 0.3f,
                Scale = (float)(_rng.NextDouble() * 15 + 10),
                Rotation = (float)(_rng.NextDouble() * Math.PI * 2),
                RotationSpeed = (float)(_rng.NextDouble() * 0.05 - 0.025)
            });
        }

        for (int i = 0; i < 3; i++)
        {
            _planets.Add(new BackgroundElement {
                Position = new Vector2(_rng.Next(0, 800), _rng.Next(-600, 800)),
                Speed = (float)(_rng.NextDouble() * 20 + 10),
                Color = new Color((float)_rng.NextDouble()*0.6f+0.4f, (float)_rng.NextDouble()*0.6f+0.4f, (float)_rng.NextDouble()*0.6f+0.4f),
                Scale = (float)(_rng.NextDouble() * 1.5f + 0.5f),
                Rotation = (float)(_rng.NextDouble() * Math.PI * 2),
                RotationSpeed = (float)(_rng.NextDouble() * 0.1 - 0.05)
            });
        }

        try { _oberthTexture = Texture2D.FromFile(GraphicsDevice, "assets/ships/Oberth.png"); } catch { }
        try { _constitutionTexture = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/Consitution.png"); } catch { }
        try { _excelsiorTexture = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/Excelsior.png"); } catch { }
        try { _jemHadarFighterTexture = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/Vanguard.png"); } catch { }
        try { _jemHadarBattleshipTexture = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/JemHadar_Battleship.png"); } catch { }

        try { _strategicBackground = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/Background_Strategic.png"); } catch { }
        try { _federationLogo = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/Faction_Federation_Logo.png"); } catch { }
        try { _dominionLogo = Texture2D.FromFile(GraphicsDevice, "Assets/Ships/Faction_Dominion_Logo.png"); } catch { }

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
            star.Position.Y += star.Speed * 3f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (star.Position.Y > GraphicsDevice.Viewport.Height)
            {
                star.Position.Y = 0;
                star.Position.X = _rng.Next(0, GraphicsDevice.Viewport.Width);
            }
            _stars[i] = star;
        }

        for (int i = 0; i < _nebulae.Count; i++)
        {
            var neb = _nebulae[i];
            neb.Position.Y += neb.Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            neb.Rotation += neb.RotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (neb.Position.Y > GraphicsDevice.Viewport.Height + 400)
            {
                neb.Position.Y = -400;
                neb.Position.X = _rng.Next(-200, GraphicsDevice.Viewport.Width + 200);
            }
            _nebulae[i] = neb;
        }

        for (int i = 0; i < _planets.Count; i++)
        {
            var planet = _planets[i];
            planet.Position.Y += planet.Speed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            planet.Rotation += planet.RotationSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (planet.Position.Y > GraphicsDevice.Viewport.Height + 200)
            {
                planet.Position.Y = -200;
                planet.Position.X = _rng.Next(-100, GraphicsDevice.Viewport.Width + 100);
                planet.Color = new Color((float)_rng.NextDouble()*0.6f+0.4f, (float)_rng.NextDouble()*0.6f+0.4f, (float)_rng.NextDouble()*0.6f+0.4f);
            }
            _planets[i] = planet;
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
                _explosions.Clear();
                _screenShakeTimer = 0f;
                _cameraOffset = Vector2.Zero;
                _missionEnding = false;
                _missionEndTimer = 0f;
                _missionWon = false;
                _missionShipsDestroyed = 0;
                _missionShipsEscaped = 0;
                _missionDamageTaken = 0;

                bool isBoss = (_missionsCompleted > 0 && _missionsCompleted % 3 == 0);
                if (isBoss)
                {
                    _enemies.Add(new Enemy { Position = new Vector2(400, -100), IsBoss = true, Health = 400 + (_missionsCompleted * 50) });
                }
                else
                {
                    int enemyCount = 15 + (_missionsCompleted * 5) + _rng.Next(0, 10);
                    int minHeight = -1000 - (_missionsCompleted * 300);
                    for(int i = 0; i < enemyCount; i++)
                        _enemies.Add(new Enemy { Position = new Vector2(_rng.Next(40, 760), _rng.Next(minHeight, -50)), IsBoss = false, Health = 20 });
                }
            }
        }
        else if (_currentState == GameState.TacticalMission)
        {
            if (_playerShields > 0)
            {
                // Move player
                if (kb.IsKeyDown(Keys.W)) _playerPos.Y -= 350f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (kb.IsKeyDown(Keys.S)) _playerPos.Y += 350f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (kb.IsKeyDown(Keys.A)) _playerPos.X -= 350f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (kb.IsKeyDown(Keys.D)) _playerPos.X += 350f * (float)gameTime.ElapsedGameTime.TotalSeconds;

                _playerPos.X = MathHelper.Clamp(_playerPos.X, 30, GraphicsDevice.Viewport.Width - 30);
                _playerPos.Y = MathHelper.Clamp(_playerPos.Y, 30, GraphicsDevice.Viewport.Height - 30);

                // Fire torpedoes
                if (kb.IsKeyDown(Keys.Space) && _torpedoes.Count < 15)
                {
                    _torpedoes.Add(new Vector2(_playerPos.X, _playerPos.Y - 20));
                }
            }

            // Move Torpedoes
            for (int i = _torpedoes.Count - 1; i >= 0; i--)
            {
                _torpedoes[i] = new Vector2(_torpedoes[i].X, _torpedoes[i].Y - 700f * (float)gameTime.ElapsedGameTime.TotalSeconds);
                if (_torpedoes[i].Y < 0) _torpedoes.RemoveAt(i);
            }

            // Move Enemy Projectiles
            for (int i = _enemyProjectiles.Count - 1; i >= 0; i--)
            {
                _enemyProjectiles[i] = new Vector2(_enemyProjectiles[i].X, _enemyProjectiles[i].Y + 450f * (float)gameTime.ElapsedGameTime.TotalSeconds);
                if (_enemyProjectiles[i].Y > 600)
                {
                    _enemyProjectiles.RemoveAt(i);
                }
                else if (_playerShields > 0 && GetPlayerBounds().Intersects(GetProjectileBounds(_enemyProjectiles[i])))
                {
                    _playerShields -= 10;
                    _missionDamageTaken += 10;
                    _enemyProjectiles.RemoveAt(i);
                }
            }

            // Move Enemies and Collision
            bool won = true;
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                won = false;
                var enemy = _enemies[i];
                float enemyMoveSpd = enemy.IsBoss ? 30f : (150f + Math.Min(150f, _missionsCompleted * 10f));
                enemy.Position.Y += enemyMoveSpd * (float)gameTime.ElapsedGameTime.TotalSeconds;

                // Enemies fire back randomly (only if fully on screen or close to it)
                if (enemy.Position.Y > -50 && _rng.NextDouble() < (enemy.IsBoss ? 0.08 : 0.005))
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
                        _missionShipsDestroyed++;
                        if (enemy.IsBoss) 
                        {
                            _score += 1000;
                            _explosions.Add(new Explosion { Position = enemy.Position, Timer = 0f, Duration = 2.0f, MaxRadius = 1200f });
                            _screenShakeTimer = 1.5f;
                            _screenShakeMagnitude = 25f;
                        }
                        else
                        {
                            _explosions.Add(new Explosion { Position = enemy.Position, Timer = 0f, Duration = 1.0f, MaxRadius = 400f });
                            _screenShakeTimer = 0.3f;
                            _screenShakeMagnitude = 5f;
                        }
                        _enemies.RemoveAt(i);
                    }
                }
                else if (enemy.Position.Y > 600 || Vector2.Distance(enemy.Position, _playerPos) < (enemy.IsBoss ? 70 : 40))
                {
                    // Enemy escaped or hit player, take shield damage
                    int dmg = enemy.IsBoss ? 50 : 20;
                    _playerShields -= dmg;
                    _missionDamageTaken += dmg;

                    if (enemy.Position.Y > 600)
                        _missionShipsEscaped++;
                    else
                        _missionShipsDestroyed++; // Crashed into player => destroyed

                    _enemies.RemoveAt(i);
                }
            }

            // Update Explosions
            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                var exp = _explosions[i];
                exp.Timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (exp.Timer >= exp.Duration)
                {
                    _explosions.RemoveAt(i);
                }
                else
                {
                    _explosions[i] = exp;
                }
            }

            if (_screenShakeTimer > 0)
            {
                _screenShakeTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                _cameraOffset = new Vector2(
                    ((float)_rng.NextDouble() * 2 - 1) * _screenShakeMagnitude,
                    ((float)_rng.NextDouble() * 2 - 1) * _screenShakeMagnitude);
            }
            else
            {
                _cameraOffset = Vector2.Zero;
            }

            if (!_missionEnding)
            {
                if (_playerShields <= 0)
                {
                    _missionEnding = true;
                    _missionEndTimer = 3.0f; // Give time to watch player explode
                    _missionWon = false;
                    _explosions.Add(new Explosion { Position = _playerPos, Timer = 0f, Duration = 2.5f, MaxRadius = 1500f });
                    _screenShakeTimer = 2.5f;
                    _screenShakeMagnitude = 30f;
                }
                else if (won)
                {
                    _missionEnding = true;
                    _missionEndTimer = 2.5f; // Give time to watch enemies explode
                    _missionWon = true;
                }
            }
            else
            {
                _missionEndTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_missionEndTimer <= 0)
                {
                    _missionEnding = false;
                    _reportTimer = 1.0f; // 1-second delay before allowing dismiss
                    _currentState = GameState.MissionReport;
                }
            }
        }
        else if (_currentState == GameState.MissionReport)
        {
            if (_reportTimer > 0)
                _reportTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_reportTimer <= 0 && (kb.IsKeyDown(Keys.Space) || kb.IsKeyDown(Keys.Enter)))
            {
                if (!_missionWon)
                {
                    _livesRemaining--;
                    _federationStrength -= 20; // mission failed penalty
                    _playerShip = ShipClass.Oberth;

                    if (_federationStrength <= 0 || _livesRemaining <= 0)
                        _currentState = GameState.GameOverDefeat;
                    else
                        _currentState = GameState.StrategicView;
                }
                else
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
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(blendState: BlendState.NonPremultiplied, transformMatrix: Matrix.CreateTranslation(_cameraOffset.X, _cameraOffset.Y, 0));

        if (_currentState == GameState.StrategicView)
        {
            if (_strategicBackground != null)
            {
                _spriteBatch.Draw(_strategicBackground, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), Color.White);
            }
            else
            {
                // Fallback starfield if no background image
                foreach (var star in _stars)
                    _spriteBatch.Draw(_pixel, new Rectangle((int)star.Position.X, (int)star.Position.Y, star.Size, star.Size), star.Color);
            }

            DrawText("STRATEGIC VIEW", 58, 24, Color.White, 4);

            int midY = GraphicsDevice.Viewport.Height / 2;

            // Federation Side
            int fedH = 128;
            int fedW = 128;
            if (_federationLogo != null)
            {
                float fedScale = 128f / Math.Max(_federationLogo.Width, _federationLogo.Height);
                fedW = (int)(_federationLogo.Width * fedScale);
                fedH = (int)(_federationLogo.Height * fedScale);
                _spriteBatch.Draw(_federationLogo, new Rectangle(100, midY - fedH / 2, fedW, fedH), Color.White);
            }
            else
            {
                _spriteBatch.Draw(_pixel, new Rectangle(100, midY - fedH / 2, fedW, fedH), Color.Blue);
            }

            string fedLabel = "FEDERATION";
            int fedLabelWidth = fedLabel.Length * 3 * 4;
            DrawText(fedLabel, 100 + (fedW / 2) - (fedLabelWidth / 2), midY - fedH / 2 - 24, Color.Cyan, 3);

            int fedBarX = 100 + fedW + 20;
            int fedBarY = midY - 20;
            _spriteBatch.Draw(_pixel, new Rectangle(fedBarX, fedBarY, 304, 40), Color.DarkBlue * 0.55f);
            int fedSegments = Math.Max(0, _federationStrength) / 5;
            for (int i = 0; i < fedSegments; i++)
                _spriteBatch.Draw(_pixel, new Rectangle(fedBarX + 4 + i * 15, fedBarY + 5, 11, 30), Color.Cyan);

            // Dominion Side
            int domH = 128;
            int domW = 128;
            if (_dominionLogo != null)
            {
                float domScale = 128f / Math.Max(_dominionLogo.Width, _dominionLogo.Height);
                domW = (int)(_dominionLogo.Width * domScale);
                domH = (int)(_dominionLogo.Height * domScale);
                _spriteBatch.Draw(_dominionLogo, new Rectangle(GraphicsDevice.Viewport.Width - 100 - domW, midY - domH / 2, domW, domH), Color.White);
            }
            else
            {
                _spriteBatch.Draw(_pixel, new Rectangle(GraphicsDevice.Viewport.Width - 100 - domW, midY - domH / 2, domW, domH), Color.Red);
            }

            string domLabel = "DOMINION";
            int domLabelWidth = domLabel.Length * 3 * 4;
            DrawText(domLabel, (GraphicsDevice.Viewport.Width - 100 - domW) + (domW / 2) - (domLabelWidth / 2), midY - domH / 2 - 24, Color.OrangeRed, 3);

            int domBarRight = GraphicsDevice.Viewport.Width - 100 - domW - 20;
            int domBarY = midY - 20;
            _spriteBatch.Draw(_pixel, new Rectangle(domBarRight - 304, domBarY, 304, 40), Color.DarkRed * 0.55f);
            int domSegments = Math.Max(0, _dominionStrength) / 5;
            for (int i = 0; i < domSegments; i++)
                _spriteBatch.Draw(_pixel, new Rectangle(domBarRight - 15 - i * 15, domBarY + 5, 11, 30), Color.OrangeRed);

            // Player class marker (e.g. size reflects ship class)
            int shipMarkerSize = 64 + (int)_playerShip * 16;
            Rectangle shipRect = new Rectangle(GraphicsDevice.Viewport.Width / 2 - shipMarkerSize / 2, midY + 40, shipMarkerSize, shipMarkerSize);
            if (_playerShip == ShipClass.Oberth && _oberthTexture != null) _spriteBatch.Draw(_oberthTexture, shipRect, Color.White);
            else if (_playerShip == ShipClass.Constitution && _constitutionTexture != null) _spriteBatch.Draw(_constitutionTexture, shipRect, Color.White);
            else if (_playerShip == ShipClass.Excelsior && _excelsiorTexture != null) _spriteBatch.Draw(_excelsiorTexture, shipRect, Color.White);
            else _spriteBatch.Draw(_pixel, shipRect, Color.White);

            // Flashing Start prompt
            if ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0)
            {
                int promptX = GraphicsDevice.Viewport.Width / 2 - 180;
                int promptY = GraphicsDevice.Viewport.Height - 102;
                DrawText("PRESS ENTER TO START", promptX + 16, promptY + 5, Color.White, 4);
            }

            DrawText("LIVES", 70, GraphicsDevice.Viewport.Height - 52, Color.White, 3);
            for (int i = 0; i < 3; i++)
            {
                int iconX = 150 + i * 42;
                int iconY = GraphicsDevice.Viewport.Height - 62;
                bool active = i < _livesRemaining;
                Color iconColor = active ? Color.White : Color.Gray * 0.45f;
                if (active && _livesRemaining == 1 && i == 0)
                {
                    float pulse = 0.75f + 0.25f * (float)Math.Sin(gameTime.TotalGameTime.TotalSeconds * 6f);
                    iconColor *= pulse;
                }
                if (_oberthTexture != null)
                {
                    float scale = 32f / Math.Max(_oberthTexture.Width, _oberthTexture.Height);
                    int w = (int)(_oberthTexture.Width * scale);
                    int h = (int)(_oberthTexture.Height * scale);
                    _spriteBatch.Draw(_oberthTexture, new Rectangle(iconX, iconY, w, h), iconColor);
                }
                else
                {
                    _spriteBatch.Draw(_pixel, new Rectangle(iconX, iconY + 8, 18, 18), active ? Color.Cyan : Color.DarkGray);
                }
            }
        }
        else if (_currentState == GameState.TacticalMission)
        {
            // Draw Background: Nebulae
            foreach (var neb in _nebulae)
                _spriteBatch.Draw(_particleTexture, neb.Position, null, neb.Color, neb.Rotation, new Vector2(16, 16), neb.Scale, SpriteEffects.None, 0f);

            // Draw Background: Planets
            foreach (var planet in _planets)
                _spriteBatch.Draw(_planetTexture, planet.Position, null, planet.Color, planet.Rotation, new Vector2(32, 32), planet.Scale, SpriteEffects.None, 0f);

            // Draw Stars
            foreach (var star in _stars)
            {
                _spriteBatch.Draw(_pixel, new Rectangle((int)star.Position.X, (int)star.Position.Y, star.Size, star.Size), star.Color);
            }

            // Draw Score
            DrawNumber(_score, 20, 20, Color.Yellow);

            // Draw Shields
            _spriteBatch.Draw(_pixel, new Rectangle(20, 45, Math.Max(0, _playerShields) * 2, 10), Color.Cyan);

            // Draw Player
            if (_playerShields > 0)
            {
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

            // Draw Explosions
            foreach (var exp in _explosions)
            {
                float progress = exp.Timer / exp.Duration;
                float currentRadius = exp.MaxRadius * (float)Math.Sqrt(progress);
                Color expColor = Color.Lerp(new Color(255, 200, 0), Color.DarkRed, progress);
                expColor *= (1f - progress); // fade out
                int size = (int)currentRadius;

                // Outer fire glow
                _spriteBatch.Draw(_particleTexture, new Rectangle((int)exp.Position.X - size / 2, (int)exp.Position.Y - size / 2, size, size), expColor);

                // Hot core
                int coreSize = (int)(size * 0.6f);
                Color coreColor = Color.White * (1f - progress);
                _spriteBatch.Draw(_particleTexture, new Rectangle((int)exp.Position.X - coreSize / 2, (int)exp.Position.Y - coreSize / 2, coreSize, coreSize), coreColor);
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
        else if (_currentState == GameState.MissionReport)
        {
            // Background overlay
            _spriteBatch.Draw(_pixel, new Rectangle(100, 100, 600, 400), Color.DarkBlue * 0.9f);
            _spriteBatch.Draw(_pixel, new Rectangle(104, 104, 592, 392), Color.Black * 0.35f);

            string headline = _missionWon ? "MISSION SUCCESSFUL" : "MISSION FAILED";
            Color headlineColor = _missionWon ? Color.LimeGreen : Color.Red;
            int headlineX = GraphicsDevice.Viewport.Width / 2 - (headline.Length * 4 * 5) / 2;
            DrawText(headline, headlineX, 120, headlineColor, 5);

            DrawText("LIVES REMAINING", 220, 210, Color.White, 4);
            DrawNumber(_livesRemaining, 500, 204, Color.Yellow, 8);

            int startX = 250;
            int startY = 150;
            int spacingY = 90;

            // Destroyed
            if (_jemHadarFighterTexture != null)
                _spriteBatch.Draw(_jemHadarFighterTexture, new Rectangle(startX, startY, 40, 40), Color.White);
            else
                _spriteBatch.Draw(_pixel, new Rectangle(startX, startY, 40, 40), Color.Magenta);

            DrawNumber(_missionShipsDestroyed, startX + 70, startY, Color.LimeGreen, 8);

            // Escaped
            if (_jemHadarFighterTexture != null)
                _spriteBatch.Draw(_jemHadarFighterTexture, new Rectangle(startX, startY + spacingY, 40, 40), Color.White * 0.4f);
            else
                _spriteBatch.Draw(_pixel, new Rectangle(startX, startY + spacingY, 40, 40), Color.Magenta * 0.4f);

            DrawNumber(_missionShipsEscaped, startX + 70, startY + spacingY, Color.Yellow, 8);

            // Damage Incurred (Shield logic icon)
            _spriteBatch.Draw(_pixel, new Rectangle(startX, startY + spacingY * 2 + 15, 40, 10), Color.Cyan);
            DrawNumber(_missionDamageTaken, startX + 70, startY + spacingY * 2, Color.Red, 8);

            if (_reportTimer <= 0)
            {
                _spriteBatch.Draw(_pixel, new Rectangle(145, 420, 410, 44), Color.Black * 0.75f);
                _spriteBatch.Draw(_pixel, new Rectangle(145, 420, 410, 4), Color.Cyan);
                _spriteBatch.Draw(_pixel, new Rectangle(145, 460, 410, 4), Color.OrangeRed);
                DrawText("PRESS SPACE OR ENTER", 158, 428, Color.White, 5);
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

    private void DrawNumber(int number, int x, int y, Color color, int scale = 4)
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
                        _spriteBatch.Draw(_pixel, new Rectangle(x + i * (4 * scale) + col * scale, y + row * scale, scale, scale), color);
                }
            }
        }
    }

    private void DrawText(string text, int x, int y, Color color, int scale = 4)
    {
        int cursorX = x;
        foreach (char ch in text.ToUpperInvariant())
        {
            if (ch == ' ')
            {
                cursorX += scale * 4;
                continue;
            }

            int[,] glyph = ch switch
            {
                'A' => new[,] { { 0, 1, 0 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
                'C' => new[,] { { 0, 1, 1 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 0, 1, 1 } },
                'D' => new[,] { { 1, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 0 } },
                'E' => new[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 1, 0 }, { 1, 0, 0 }, { 1, 1, 1 } },
                'F' => new[,] { { 1, 1, 1 }, { 1, 0, 0 }, { 1, 1, 0 }, { 1, 0, 0 }, { 1, 0, 0 } },
                'G' => new[,] { { 0, 1, 1 }, { 1, 0, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 1 } },
                'H' => new[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
                'I' => new[,] { { 1, 1, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 1, 1, 1 } },
                'N' => new[,] { { 1, 0, 1 }, { 1, 1, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
                'L' => new[,] { { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 0, 0 }, { 1, 1, 1 } },
                'M' => new[,] { { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 } },
                'O' => new[,] { { 0, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 } },
                'P' => new[,] { { 1, 1, 0 }, { 1, 0, 1 }, { 1, 1, 0 }, { 1, 0, 0 }, { 1, 0, 0 } },
                'R' => new[,] { { 1, 1, 0 }, { 1, 0, 1 }, { 1, 1, 0 }, { 1, 0, 1 }, { 1, 0, 1 } },
                'S' => new[,] { { 0, 1, 1 }, { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 }, { 1, 1, 0 } },
                'T' => new[,] { { 1, 1, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 }, { 0, 1, 0 } },
                'V' => new[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 }, { 0, 1, 0 } },
                'W' => new[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 1, 1 }, { 1, 0, 1 } },
                'U' => new[,] { { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 1, 0, 1 }, { 0, 1, 0 } },
                '/' => new[,] { { 0, 0, 1 }, { 0, 1, 0 }, { 0, 1, 0 }, { 1, 0, 0 }, { 1, 0, 0 } },
                _ => new[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } }
            };

            for (int row = 0; row < 5; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    if (glyph[row, col] == 1)
                        _spriteBatch.Draw(_pixel, new Rectangle(cursorX + col * scale, y + row * scale, scale, scale), color);
                }
            }

            cursorX += scale * 4;
        }
    }

    private Rectangle GetPlayerBounds()
    {
        int shipSize = 10 + (int)_playerShip * 3;
        int width = shipSize;
        int height = shipSize;

        if (_playerShip == ShipClass.Oberth && _oberthTexture != null)
        {
            float scale = 64f / Math.Max(_oberthTexture.Width, _oberthTexture.Height);
            width = (int)(_oberthTexture.Width * scale);
            height = (int)(_oberthTexture.Height * scale);
        }
        else if (_playerShip == ShipClass.Constitution && _constitutionTexture != null)
        {
            float scale = 80f / Math.Max(_constitutionTexture.Width, _constitutionTexture.Height);
            width = (int)(_constitutionTexture.Width * scale);
            height = (int)(_constitutionTexture.Height * scale);
        }
        else if (_playerShip == ShipClass.Excelsior && _excelsiorTexture != null)
        {
            float scale = 96f / Math.Max(_excelsiorTexture.Width, _excelsiorTexture.Height);
            width = (int)(_excelsiorTexture.Width * scale);
            height = (int)(_excelsiorTexture.Height * scale);
        }

        return new Rectangle((int)_playerPos.X - width / 2, (int)_playerPos.Y - height / 2, width, height);
    }

    private static Rectangle GetProjectileBounds(Vector2 projectile) => new Rectangle((int)projectile.X - 2, (int)projectile.Y - 10, 4, 20);
}
