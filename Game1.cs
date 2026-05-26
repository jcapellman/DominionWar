using System;
using System.Collections.Generic;
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

    enum EnemyType { Fighter, Interceptor, Bomber, Dreadnought }
    enum MissionType { Assault, HoldTheLine, BossHunt }
    enum PowerUpType { Shield, RapidFire, SpreadShot, ScoreBoost }
    enum WeaponMode { Standard, Twin, Spread }

    struct ShipStats
    {
        public float Speed;
        public int MaxShields;
        public float FireCooldown;
        public int Damage;
        public int ShotCount;
        public float ShotSpread;
        public WeaponMode DefaultWeaponMode;
    }

    class Projectile
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public int Damage;
        public float Radius;
        public bool IsEnemy;
    }

    class PowerUp
    {
        public Vector2 Position;
        public PowerUpType Type;
        public float BobPhase;
    }

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

    private class Enemy
    {
        public Vector2 Position;
        public EnemyType Type;
        public int Health;
        public int MaxHealth;
        public float Phase;
        public float FireTimer;
        public float FireInterval;

        public bool IsBoss => Type == EnemyType.Dreadnought;
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
    private MissionType _currentMissionType = MissionType.Assault;
    private WeaponMode _missionWeaponBonus = WeaponMode.Standard;
    private WeaponMode _nextMissionWeaponBonus = WeaponMode.Standard;
    private int _nextMissionEnemyModifier = 0;
    private float _playerFireCooldown = 0f;
    private float _rapidFireTimer = 0f;
    private float _comboTimer = 0f;
    private int _comboMultiplier = 1;

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
    private List<Projectile> _torpedoes = new List<Projectile>();
    private List<Projectile> _enemyProjectiles = new List<Projectile>();
    private List<Explosion> _explosions = new List<Explosion>();
    private List<PowerUp> _powerUps = new List<PowerUp>();
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

    private ShipStats GetShipStats(ShipClass shipClass) => shipClass switch
    {
        ShipClass.Oberth => new ShipStats { Speed = 420f, MaxShields = 90, FireCooldown = 0.26f, Damage = 18, ShotCount = 1, ShotSpread = 0f, DefaultWeaponMode = WeaponMode.Standard },
        ShipClass.Constitution => new ShipStats { Speed = 380f, MaxShields = 110, FireCooldown = 0.22f, Damage = 18, ShotCount = 2, ShotSpread = 0.10f, DefaultWeaponMode = WeaponMode.Twin },
        ShipClass.Excelsior => new ShipStats { Speed = 345f, MaxShields = 125, FireCooldown = 0.2f, Damage = 16, ShotCount = 3, ShotSpread = 0.12f, DefaultWeaponMode = WeaponMode.Spread },
        ShipClass.Defiant => new ShipStats { Speed = 470f, MaxShields = 95, FireCooldown = 0.16f, Damage = 14, ShotCount = 2, ShotSpread = 0.08f, DefaultWeaponMode = WeaponMode.Twin },
        ShipClass.Akira => new ShipStats { Speed = 395f, MaxShields = 120, FireCooldown = 0.18f, Damage = 15, ShotCount = 3, ShotSpread = 0.11f, DefaultWeaponMode = WeaponMode.Spread },
        ShipClass.Galaxy => new ShipStats { Speed = 320f, MaxShields = 150, FireCooldown = 0.24f, Damage = 22, ShotCount = 3, ShotSpread = 0.14f, DefaultWeaponMode = WeaponMode.Spread },
        ShipClass.Sovereign => new ShipStats { Speed = 360f, MaxShields = 140, FireCooldown = 0.14f, Damage = 18, ShotCount = 4, ShotSpread = 0.12f, DefaultWeaponMode = WeaponMode.Spread },
        _ => new ShipStats { Speed = 360f, MaxShields = 100, FireCooldown = 0.24f, Damage = 16, ShotCount = 1, ShotSpread = 0f, DefaultWeaponMode = WeaponMode.Standard }
    };

    private WeaponMode GetEffectiveWeaponMode() => _missionWeaponBonus != WeaponMode.Standard ? _missionWeaponBonus : GetShipStats(_playerShip).DefaultWeaponMode;

    private void StartMission()
    {
        var stats = GetShipStats(_playerShip);
        _currentMissionType = (_missionsCompleted % 3) switch
        {
            0 => MissionType.Assault,
            1 => MissionType.HoldTheLine,
            _ => MissionType.BossHunt
        };

        _playerPos = new Vector2(400, 300);
        _playerShields = stats.MaxShields;
        _enemies.Clear();
        _torpedoes.Clear();
        _enemyProjectiles.Clear();
        _explosions.Clear();
        _powerUps.Clear();
        _screenShakeTimer = 0f;
        _cameraOffset = Vector2.Zero;
        _missionEnding = false;
        _missionEndTimer = 0f;
        _missionWon = false;
        _missionShipsDestroyed = 0;
        _missionShipsEscaped = 0;
        _missionDamageTaken = 0;
        _playerFireCooldown = 0f;
        _rapidFireTimer = 0f;
        _comboTimer = 0f;
        _comboMultiplier = 1;
        _missionWeaponBonus = _nextMissionWeaponBonus;
        _nextMissionWeaponBonus = WeaponMode.Standard;

        int enemyCount = Math.Max(6, 10 + (_missionsCompleted * 4) + _rng.Next(0, 8) + _nextMissionEnemyModifier);
        _nextMissionEnemyModifier = 0;

        if (_currentMissionType == MissionType.BossHunt)
        {
            _enemies.Add(new Enemy
            {
                Position = new Vector2(400, -140),
                Type = EnemyType.Dreadnought,
                Health = 420 + (_missionsCompleted * 70),
                MaxHealth = 420 + (_missionsCompleted * 70),
                Phase = (float)_rng.NextDouble() * MathHelper.TwoPi,
                FireTimer = 0.1f,
                FireInterval = 0.45f
            });

            for (int i = 0; i < Math.Min(8, 4 + _missionsCompleted); i++)
            {
                AddEnemyWaveUnit(EnemyType.Fighter, true);
            }
        }
        else if (_currentMissionType == MissionType.HoldTheLine)
        {
            for (int i = 0; i < enemyCount; i++)
            {
                var type = i % 4 == 0 ? EnemyType.Bomber : (i % 3 == 0 ? EnemyType.Interceptor : EnemyType.Fighter);
                AddEnemyWaveUnit(type, false);
            }
        }
        else
        {
            for (int i = 0; i < enemyCount; i++)
            {
                var roll = _rng.NextDouble();
                var type = roll < 0.5 ? EnemyType.Fighter : roll < 0.8 ? EnemyType.Interceptor : EnemyType.Bomber;
                AddEnemyWaveUnit(type, false);
            }
        }
    }

    private void AddEnemyWaveUnit(EnemyType type, bool bossEscort)
    {
        int baseY = bossEscort ? _rng.Next(-420, -80) : _rng.Next(-1200, -100);
        int xMin = bossEscort ? 120 : 40;
        int xMax = bossEscort ? 680 : 760;
        int health = type switch
        {
            EnemyType.Interceptor => 26,
            EnemyType.Bomber => 42,
            _ => 20
        };

        float fireInterval = type switch
        {
            EnemyType.Interceptor => 1.0f,
            EnemyType.Bomber => 0.8f,
            _ => 1.35f
        };

        _enemies.Add(new Enemy
        {
            Position = new Vector2(_rng.Next(xMin, xMax), baseY),
            Type = type,
            Health = health,
            MaxHealth = health,
            Phase = (float)_rng.NextDouble() * MathHelper.TwoPi,
            FireTimer = (float)_rng.NextDouble() * fireInterval,
            FireInterval = fireInterval
        });
    }

    private void FirePlayerShots(float dt)
    {
        var stats = GetShipStats(_playerShip);
        if (_playerFireCooldown > 0f)
        {
            _playerFireCooldown -= dt;
        }

        bool wantsFire = Keyboard.GetState().IsKeyDown(Keys.Space);
        if (!wantsFire || _playerFireCooldown > 0f || _playerShields <= 0 || _missionEnding)
        {
            return;
        }

        WeaponMode mode = GetEffectiveWeaponMode();
        int shotCount = stats.ShotCount;
        float spread = stats.ShotSpread;
        if (mode == WeaponMode.Twin)
        {
            shotCount = Math.Max(2, shotCount);
            spread = Math.Max(spread, 0.12f);
        }
        else if (mode == WeaponMode.Spread)
        {
            shotCount = Math.Max(3, shotCount);
            spread = Math.Max(spread, 0.18f);
        }

        float cooldown = stats.FireCooldown;
        if (_rapidFireTimer > 0f)
        {
            cooldown *= 0.5f;
        }

        float startOffset = -((shotCount - 1) * spread) * 0.5f;
        for (int i = 0; i < shotCount; i++)
        {
            float xVelocity = (startOffset + i * spread) * 600f;
            _torpedoes.Add(new Projectile
            {
                Position = new Vector2(_playerPos.X, _playerPos.Y - 16),
                Velocity = new Vector2(xVelocity, -760f),
                Damage = stats.Damage,
                Radius = 6f,
                IsEnemy = false
            });
        }

        _playerFireCooldown = cooldown;
    }

    private void SpawnEnemyProjectile(Enemy enemy)
    {
        Vector2 origin = enemy.Position + new Vector2(0, enemy.IsBoss ? 45 : 12);
        Vector2 velocity;

        if (enemy.Type == EnemyType.Interceptor)
        {
            Vector2 toPlayer = _playerPos - origin;
            if (toPlayer.LengthSquared() < 0.001f)
                toPlayer = Vector2.UnitY;
            else
                toPlayer = Vector2.Normalize(toPlayer);
            velocity = new Vector2(toPlayer.X * 170f, Math.Abs(toPlayer.Y) * 360f + 260f);
        }
        else if (enemy.Type == EnemyType.Bomber)
        {
            velocity = new Vector2(((float)_rng.NextDouble() * 2f - 1f) * 70f, 390f);
        }
        else if (enemy.IsBoss)
        {
            Vector2 toPlayer = Vector2.Normalize(_playerPos - origin);
            velocity = new Vector2(toPlayer.X * 150f, Math.Abs(toPlayer.Y) * 330f + 280f);
        }
        else
        {
            velocity = new Vector2(((float)_rng.NextDouble() * 2f - 1f) * 45f, 420f);
        }

        _enemyProjectiles.Add(new Projectile
        {
            Position = origin,
            Velocity = velocity,
            Damage = enemy.IsBoss ? 30 : enemy.Type == EnemyType.Bomber ? 18 : 10,
            Radius = enemy.IsBoss ? 10f : 5f,
            IsEnemy = true
        });
    }

    private void SpawnPowerUp(Vector2 position)
    {
        PowerUpType type = (PowerUpType)_rng.Next(0, 4);
        _powerUps.Add(new PowerUp
        {
            Position = position,
            Type = type,
            BobPhase = (float)_rng.NextDouble() * MathHelper.TwoPi
        });
    }

    private void ApplyMissionReward(Keys rewardKey)
    {
        if (rewardKey == Keys.D1 || rewardKey == Keys.NumPad1)
        {
            _livesRemaining = Math.Min(5, _livesRemaining + 1);
            _federationStrength = Math.Min(100, _federationStrength + 15);
            _score += 150;
        }
        else if (rewardKey == Keys.D2 || rewardKey == Keys.NumPad2)
        {
            _nextMissionWeaponBonus = WeaponMode.Spread;
            _score += 300;
        }
        else if (rewardKey == Keys.D3 || rewardKey == Keys.NumPad3)
        {
            _nextMissionEnemyModifier -= 4;
            _dominionStrength = Math.Max(0, _dominionStrength - 10);
            _score += 200;
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
            if (kb.IsKeyDown(Keys.Enter))
            {
                StartMission();
                _currentState = GameState.TacticalMission;
            }
        }
        else if (_currentState == GameState.TacticalMission)
        {
            var stats = GetShipStats(_playerShip);
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_rapidFireTimer > 0f)
            {
                _rapidFireTimer -= dt;
            }

            if (_playerShields > 0)
            {
                if (kb.IsKeyDown(Keys.W)) _playerPos.Y -= stats.Speed * dt;
                if (kb.IsKeyDown(Keys.S)) _playerPos.Y += stats.Speed * dt;
                if (kb.IsKeyDown(Keys.A)) _playerPos.X -= stats.Speed * dt;
                if (kb.IsKeyDown(Keys.D)) _playerPos.X += stats.Speed * dt;

                _playerPos.X = MathHelper.Clamp(_playerPos.X, 30, GraphicsDevice.Viewport.Width - 30);
                _playerPos.Y = MathHelper.Clamp(_playerPos.Y, 30, GraphicsDevice.Viewport.Height - 30);

                FirePlayerShots(dt);
            }

            for (int i = _torpedoes.Count - 1; i >= 0; i--)
            {
                _torpedoes[i].Position += _torpedoes[i].Velocity * dt;
                if (_torpedoes[i].Position.Y < -50 || _torpedoes[i].Position.Y > GraphicsDevice.Viewport.Height + 50 || _torpedoes[i].Position.X < -50 || _torpedoes[i].Position.X > GraphicsDevice.Viewport.Width + 50)
                {
                    _torpedoes.RemoveAt(i);
                }
            }

            for (int i = _enemyProjectiles.Count - 1; i >= 0; i--)
            {
                _enemyProjectiles[i].Position += _enemyProjectiles[i].Velocity * dt;
                if (_enemyProjectiles[i].Position.Y > GraphicsDevice.Viewport.Height + 80 || _enemyProjectiles[i].Position.X < -80 || _enemyProjectiles[i].Position.X > GraphicsDevice.Viewport.Width + 80)
                {
                    _enemyProjectiles.RemoveAt(i);
                }
                else if (_playerShields > 0 && GetPlayerBounds().Intersects(GetProjectileBounds(_enemyProjectiles[i])))
                {
                    int dmg = _enemyProjectiles[i].Damage;
                    _playerShields -= dmg;
                    _missionDamageTaken += dmg;
                    _enemyProjectiles.RemoveAt(i);
                    _screenShakeTimer = Math.Max(_screenShakeTimer, 0.15f);
                    _screenShakeMagnitude = Math.Max(_screenShakeMagnitude, 4f);
                }
            }

            for (int i = _powerUps.Count - 1; i >= 0; i--)
            {
                var power = _powerUps[i];
                power.BobPhase += dt * 4f;
                power.Position.Y += (float)Math.Sin(power.BobPhase) * 10f * dt;
                power.Position.X += (float)Math.Cos(power.BobPhase * 0.5f) * 12f * dt;

                if (power.Position.Y > GraphicsDevice.Viewport.Height + 80)
                {
                    _powerUps.RemoveAt(i);
                    continue;
                }

                Rectangle powerBounds = new Rectangle((int)power.Position.X - 16, (int)power.Position.Y - 16, 32, 32);
                if (_playerShields > 0 && GetPlayerBounds().Intersects(powerBounds))
                {
                    if (power.Type == PowerUpType.Shield)
                    {
                        _playerShields = Math.Min(stats.MaxShields + 25, _playerShields + 35);
                        _score += 50;
                    }
                    else if (power.Type == PowerUpType.RapidFire)
                    {
                        _rapidFireTimer = 8f;
                        _score += 100;
                    }
                    else if (power.Type == PowerUpType.SpreadShot)
                    {
                        _missionWeaponBonus = WeaponMode.Spread;
                        _score += 125;
                    }
                    else
                    {
                        _score += 250;
                    }

                    _explosions.Add(new Explosion { Position = power.Position, Timer = 0f, Duration = 0.55f, MaxRadius = 120f });
                    _powerUps.RemoveAt(i);
                }
            }

            bool won = true;
            for (int i = _enemies.Count - 1; i >= 0; i--)
            {
                won = false;
                var enemy = _enemies[i];
                float enemySpeed = enemy.Type switch
                {
                    EnemyType.Interceptor => 185f + Math.Min(110f, _missionsCompleted * 9f),
                    EnemyType.Bomber => 105f + Math.Min(70f, _missionsCompleted * 5f),
                    EnemyType.Dreadnought => 40f,
                    _ => 150f + Math.Min(120f, _missionsCompleted * 8f)
                };

                enemy.Phase += dt * (enemy.IsBoss ? 2.5f : 3.5f);
                if (enemy.Type == EnemyType.Fighter)
                {
                    enemy.Position.Y += enemySpeed * dt;
                    enemy.Position.X += (float)Math.Sin(enemy.Phase) * 55f * dt;
                }
                else if (enemy.Type == EnemyType.Interceptor)
                {
                    enemy.Position.Y += enemySpeed * dt;
                    float chase = Math.Sign(_playerPos.X - enemy.Position.X) * 115f * dt;
                    enemy.Position.X += chase + (float)Math.Sin(enemy.Phase * 1.6f) * 35f * dt;
                }
                else if (enemy.Type == EnemyType.Bomber)
                {
                    enemy.Position.Y += enemySpeed * dt;
                    enemy.Position.X += (float)Math.Sin(enemy.Phase) * 30f * dt;
                }
                else
                {
                    enemy.Position.Y += enemySpeed * dt;
                    enemy.Position.X += (float)Math.Sin(enemy.Phase) * 42f * dt;
                }

                enemy.Position.X = MathHelper.Clamp(enemy.Position.X, 40, GraphicsDevice.Viewport.Width - 40);

                enemy.FireTimer -= dt;
                if (enemy.Position.Y > -60 && enemy.FireTimer <= 0f)
                {
                    SpawnEnemyProjectile(enemy);
                    enemy.FireTimer = enemy.IsBoss ? Math.Max(0.22f, enemy.FireInterval - (_missionsCompleted * 0.01f)) : enemy.FireInterval;
                    if (enemy.IsBoss && enemy.Health < enemy.MaxHealth / 2)
                    {
                        SpawnEnemyProjectile(enemy);
                    }
                }

                bool hit = false;
                for (int j = _torpedoes.Count - 1; j >= 0; j--)
                {
                    if (Vector2.Distance(enemy.Position, _torpedoes[j].Position) < (enemy.IsBoss ? 54 : enemy.Type == EnemyType.Bomber ? 34 : 28))
                    {
                        hit = true;
                        _score += 25 * _comboMultiplier;
                        _torpedoes.RemoveAt(j);
                        break;
                    }
                }

                if (hit)
                {
                    enemy.Health -= GetShipStats(_playerShip).Damage;
                    _screenShakeTimer = Math.Max(_screenShakeTimer, enemy.IsBoss ? 0.45f : 0.16f);
                    _screenShakeMagnitude = Math.Max(_screenShakeMagnitude, enemy.IsBoss ? 9f : 3f);
                }

                if (hit && enemy.Health <= 0)
                {
                    _missionShipsDestroyed++;
                    _comboTimer = 2.2f;
                    _comboMultiplier = Math.Min(6, _comboMultiplier + 1);
                    _score += enemy.IsBoss ? 1200 * _comboMultiplier : 120 * _comboMultiplier;
                    _explosions.Add(new Explosion { Position = enemy.Position, Timer = 0f, Duration = enemy.IsBoss ? 2.0f : 1.0f, MaxRadius = enemy.IsBoss ? 1100f : 420f });
                    _screenShakeTimer = Math.Max(_screenShakeTimer, enemy.IsBoss ? 1.25f : 0.3f);
                    _screenShakeMagnitude = Math.Max(_screenShakeMagnitude, enemy.IsBoss ? 24f : 5f);
                    if (_rng.NextDouble() < (enemy.IsBoss ? 1.0 : 0.35))
                    {
                        SpawnPowerUp(enemy.Position);
                    }
                    _enemies.RemoveAt(i);
                    continue;
                }

                if (enemy.Position.Y > GraphicsDevice.Viewport.Height + 40)
                {
                    _missionShipsEscaped++;
                    _enemies.RemoveAt(i);
                    continue;
                }

                if (_playerShields > 0 && Vector2.Distance(enemy.Position, _playerPos) < (enemy.IsBoss ? 76 : enemy.Type == EnemyType.Bomber ? 46 : 38))
                {
                    int dmg = enemy.IsBoss ? 55 : enemy.Type == EnemyType.Bomber ? 24 : 18;
                    _playerShields -= dmg;
                    _missionDamageTaken += dmg;
                    _missionShipsDestroyed++;
                    _explosions.Add(new Explosion { Position = enemy.Position, Timer = 0f, Duration = 0.85f, MaxRadius = 240f });
                    _enemies.RemoveAt(i);
                    continue;
                }

                _enemies[i] = enemy;
            }

            for (int i = _explosions.Count - 1; i >= 0; i--)
            {
                var exp = _explosions[i];
                exp.Timer += dt;
                if (exp.Timer >= exp.Duration)
                {
                    _explosions.RemoveAt(i);
                }
                else
                {
                    _explosions[i] = exp;
                }
            }

            if (_comboTimer > 0f)
            {
                _comboTimer -= dt;
                if (_comboTimer <= 0f)
                {
                    _comboMultiplier = 1;
                }
            }

            if (_screenShakeTimer > 0)
            {
                _screenShakeTimer -= dt;
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
                    _missionEndTimer = 3.0f;
                    _missionWon = false;
                    _explosions.Add(new Explosion { Position = _playerPos, Timer = 0f, Duration = 2.5f, MaxRadius = 1500f });
                    _screenShakeTimer = 2.5f;
                    _screenShakeMagnitude = 30f;
                }
                else if (won)
                {
                    _missionEnding = true;
                    _missionEndTimer = 2.5f;
                    _missionWon = true;
                    _explosions.Add(new Explosion { Position = _playerPos, Timer = 0f, Duration = 0.7f, MaxRadius = 220f });
                }
            }
            else
            {
                _missionEndTimer -= dt;
                if (_missionEndTimer <= 0)
                {
                    _missionEnding = false;
                    _reportTimer = 0.9f;
                    _currentState = GameState.MissionReport;
                }
            }
        }
        else if (_currentState == GameState.MissionReport)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_reportTimer > 0)
            {
                _reportTimer -= dt;
            }

            if (_reportTimer <= 0)
            {
                if (_missionWon)
                {
                    if (kb.IsKeyDown(Keys.D1) || kb.IsKeyDown(Keys.NumPad1) || kb.IsKeyDown(Keys.D2) || kb.IsKeyDown(Keys.NumPad2) || kb.IsKeyDown(Keys.D3) || kb.IsKeyDown(Keys.NumPad3))
                    {
                        if (kb.IsKeyDown(Keys.D1) || kb.IsKeyDown(Keys.NumPad1))
                        {
                            ApplyMissionReward(Keys.D1);
                        }
                        else if (kb.IsKeyDown(Keys.D2) || kb.IsKeyDown(Keys.NumPad2))
                        {
                            ApplyMissionReward(Keys.D2);
                        }
                        else
                        {
                            ApplyMissionReward(Keys.D3);
                        }

                        _dominionStrength -= 15;
                        _missionsCompleted++;
                        if (_missionsCompleted % 2 == 0 && (int)_playerShip < 6)
                            _playerShip++;

                        if (_dominionStrength <= 0)
                        {
                            _currentState = GameState.GameOverVictory;
                        }
                        else if (_federationStrength <= 0)
                        {
                            _currentState = GameState.GameOverDefeat;
                        }
                        else
                        {
                            _currentState = GameState.StrategicView;
                        }
                    }
                }
                else if (kb.IsKeyDown(Keys.Space) || kb.IsKeyDown(Keys.Enter))
                {
                    _livesRemaining--;
                    _federationStrength -= 20;
                    _playerShip = ShipClass.Oberth;

                    if (_federationStrength <= 0 || _livesRemaining <= 0)
                        _currentState = GameState.GameOverDefeat;
                    else
                        _currentState = GameState.StrategicView;
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
            DrawText("NEXT MISSION " + GetMissionTypeLabel(_currentMissionType), 58, 60, Color.LightCyan, 3);

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

            if (_nextMissionWeaponBonus != WeaponMode.Standard)
            {
                DrawText("ARMORY UPGRADE READY", 490, GraphicsDevice.Viewport.Height - 52, Color.Yellow, 3);
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

            DrawText(GetMissionTypeLabel(_currentMissionType), 115, 18, Color.White, 3);

            if (_comboMultiplier > 1)
            {
                DrawText("COMBO", 20, 70, Color.Orange, 3);
                DrawNumber(_comboMultiplier, 96, 66, Color.Orange, 4);
            }

            if (_rapidFireTimer > 0f)
            {
                DrawText("RAPID FIRE", 20, 98, Color.Cyan, 3);
            }

            DrawText("WEAPON " + GetWeaponModeLabel(GetEffectiveWeaponMode()), 20, 126, Color.LightGreen, 3);

            // Draw Shields
            _spriteBatch.Draw(_pixel, new Rectangle(20, 45, Math.Max(0, _playerShields) * 2, 10), Color.Cyan);

            for (int i = 0; i < _powerUps.Count; i++)
            {
                var power = _powerUps[i];
                Color powerColor = power.Type switch
                {
                    PowerUpType.Shield => Color.Cyan,
                    PowerUpType.RapidFire => Color.Yellow,
                    PowerUpType.SpreadShot => Color.Lime,
                    _ => Color.Orange
                };

                _spriteBatch.Draw(_pixel, new Rectangle((int)power.Position.X - 10, (int)power.Position.Y - 10, 20, 20), powerColor);
                _spriteBatch.Draw(_pixel, new Rectangle((int)power.Position.X - 4, (int)power.Position.Y - 4, 8, 8), Color.White);
            }

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
                _spriteBatch.Draw(_pixel, new Rectangle((int)torp.Position.X - 4, (int)torp.Position.Y - 5, 8, 10), Color.OrangeRed);
                _spriteBatch.Draw(_pixel, new Rectangle((int)torp.Position.X - 2, (int)torp.Position.Y - 3, 4, 6), Color.LightYellow);
            }

            // Draw Enemy Projectiles (Dominion Polaron Beams)
            foreach(var proj in _enemyProjectiles)
            {
                _spriteBatch.Draw(_pixel, new Rectangle((int)proj.Position.X - 2, (int)proj.Position.Y - 10, 4, 20), Color.DarkMagenta);
                _spriteBatch.Draw(_pixel, new Rectangle((int)proj.Position.X - 1, (int)proj.Position.Y - 8, 2, 16), Color.Cyan);
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

            if (_playerShields > 0 && !_missionEnding)
            {
                DrawText("HOLD SPACE TO FIRE", 520, GraphicsDevice.Viewport.Height - 46, Color.White, 2);
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

            DrawText(_missionWon ? "CHOOSE REWARD" : "RETRY FROM STRATEGIC VIEW", 185, 270, Color.LightCyan, 3);

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
                _spriteBatch.Draw(_pixel, new Rectangle(120, 405, 460, 86), Color.Black * 0.75f);
                _spriteBatch.Draw(_pixel, new Rectangle(120, 405, 460, 4), Color.Cyan);
                _spriteBatch.Draw(_pixel, new Rectangle(120, 487, 460, 4), Color.OrangeRed);

                if (_missionWon)
                {
                    DrawText("ONE REPAIR   TWO ARMORY   THREE INTEL", 140, 420, Color.White, 3);
                    DrawText("RESTORE LIVES AND FLEET", 140, 450, Color.Cyan, 2);
                    DrawText("NEXT MISSION SPREAD SHOT", 140, 468, Color.Yellow, 2);
                    DrawText("NEXT MISSION FEWER ENEMIES", 140, 484, Color.LightGreen, 2);
                }
                else
                {
                    DrawText("PRESS SPACE OR ENTER TO CONTINUE", 140, 435, Color.White, 4);
                }
            }
        }
        else if (_currentState == GameState.GameOverVictory)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(170, 180, 460, 220), Color.DarkGreen * 0.95f);
            DrawText("VICTORY", 285, 230, Color.White, 6);
            DrawText("FEDERATION WINS THE WAR", 190, 300, Color.LightGreen, 3);
        }
        else if (_currentState == GameState.GameOverDefeat)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(170, 180, 460, 220), Color.DarkRed * 0.95f);
            DrawText("DEFEAT", 300, 230, Color.White, 6);
            DrawText("THE DOMINION PRESSES THE ADVANTAGE", 155, 300, Color.OrangeRed, 3);
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

    private static string GetMissionTypeLabel(MissionType missionType) => missionType switch
    {
        MissionType.Assault => "ASSAULT",
        MissionType.HoldTheLine => "HOLD THE LINE",
        MissionType.BossHunt => "BOSS HUNT",
        _ => "ASSAULT"
    };

    private static string GetWeaponModeLabel(WeaponMode weaponMode) => weaponMode switch
    {
        WeaponMode.Standard => "STANDARD",
        WeaponMode.Twin => "TWIN",
        WeaponMode.Spread => "SPREAD",
        _ => "STANDARD"
    };

    private static Rectangle GetProjectileBounds(Projectile projectile) => new Rectangle((int)projectile.Position.X - 4, (int)projectile.Position.Y - 8, 8, 16);
}
