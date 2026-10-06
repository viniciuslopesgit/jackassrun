using Raylib_cs;

namespace JackassRun;

// Todas as entidades do mundo tem so campos de valor, entao MemberwiseClone basta
// para tirar o snapshot usado pela mecanica de rebobinar o tempo.

public enum EnemyKind { Soldier, Rocketeer, Brute, Flyer, Turret }

public sealed class Enemy
{
    public int Id;
    public EnemyKind Kind;
    public float X, Y, VX, VY, BaseY, Phase;
    public int Hp, Facing = -1, Burst;
    public float FireT, AlertT, StateT, HurtT, MuzzleT, AnimT, LandT, HitDir;
    public bool OnGround, Alerted, Walking, Dead, LastBoom;
    public Enemy Clone() => (Enemy)MemberwiseClone();

    public float HalfW => Kind switch { EnemyKind.Brute => 6, EnemyKind.Turret => 7, EnemyKind.Flyer => 7, _ => 4 };
    public float Height => Kind switch { EnemyKind.Brute => 23, EnemyKind.Turret => 11, EnemyKind.Flyer => 9, _ => 19 };
    /// <summary>Caixa de colisao. Para voadores Y e o centro; para os demais, Y sao os pes.</summary>
    public (float x0, float y0, float x1, float y1) Box =>
        Kind == EnemyKind.Flyer
            ? (X - HalfW, Y - Height / 2, X + HalfW, Y + Height / 2)
            : (X - HalfW, Y - Height, X + HalfW, Y);
}

public enum BulletKind { Bullet, Pellet, Laser, Rocket, Grenade, Dynamite, Slash, EBullet, ERocket, EBomb }

public sealed class Bullet
{
    public int Id, OwnerId, LastHit;
    public BulletKind Kind;
    public bool FromPlayer, Ghost, Pierce, Dead;
    public float X, Y, VX, VY, Life, W = 3, H = 2, Grav, ExplodeR, T;
    public int Dmg = 1;
    public Bullet Clone() => (Bullet)MemberwiseClone();
}

public enum PropKind { Barrel, Cage, Glorb }

public sealed class Prop
{
    public int Id, Hp = 1, Char;
    public PropKind Kind;
    public float X, Y, VY, T, Fuse = -1;
    public bool Done;
    public Prop Clone() => (Prop)MemberwiseClone();
}

/// <summary>Bloco de terreno caindo depois que a estrutura perdeu a sustentacao.</summary>
public sealed class FallingBlock
{
    public float X, Y, VY;      // canto superior esquerdo, em pixels
    public byte Tile;
    public bool Dead;
    public FallingBlock Clone() => (FallingBlock)MemberwiseClone();
}

public sealed class Explosion
{
    public float X, Y, R, T, Dur = 0.55f;
    public bool FromPlayer;
    public Explosion Clone() => (Explosion)MemberwiseClone();
}

/// <summary>Estado completo do mundo num frame. E clonado todo frame para o historico de rebobinar.</summary>
public sealed class WorldState
{
    public int Frame, NextChunk, NextId = 1_000_000, Score, Kills;
    public float CamX, Speed = 34, FreezeT;
    public Rng Rng;
    public List<Enemy> Enemies = new();
    public List<Bullet> Bullets = new();
    public List<Prop> Props = new();
    public List<Explosion> Explosions = new();
    public List<FallingBlock> Falling = new();

    public WorldState Clone()
    {
        var s = (WorldState)MemberwiseClone();
        s.Enemies = Enemies.ConvertAll(e => e.Clone());
        s.Bullets = Bullets.ConvertAll(b => b.Clone());
        s.Props = Props.ConvertAll(p => p.Clone());
        s.Explosions = Explosions.ConvertAll(x => x.Clone());
        s.Falling = Falling.ConvertAll(f => f.Clone());
        return s;
    }
}

public sealed class Player
{
    public int Char, Facing = 1, Specials = 3, Shield, WallDir;
    public float X, Y, VX, VY;
    public float FireT, InvulnT, AnimT, DashT, MuzzleT, Coyote, Recoil, LandT;
    public bool OnGround, Climbing, Dead;
}

/// <summary>Um quadro gravado de uma vida passada (o "fantasma" do Super Time Force).</summary>
public struct GFrame
{
    public float X, Y, VX, VY;
    public sbyte Facing;
    public bool OnGround, Climbing, Fire, Special;
}

public sealed class Ghost
{
    public int Char, StartFrame, KillerId;
    public bool Died, Resolved;
    public readonly List<GFrame> Frames = new();
    public int EndFrame => StartFrame + Frames.Count - 1;
}

/// <summary>Terreno destrutivel. Cada tile e um byte: tipo nos 3 bits altos, dano nos 5 baixos.
/// Toda alteracao entra num log com o frame, para poder ser desfeita ao rebobinar.</summary>
public sealed class Terrain
{
    public const int EMPTY = 0, DIRT = 1, BRICK = 2, STEEL = 3, CRATE = 4;
    static readonly int[] MaxHp = { 0, 5, 3, 999, 2 };

    readonly struct Mod
    {
        public readonly int Frame, X, Y; public readonly byte Old;
        public Mod(int f, int x, int y, byte o) { Frame = f; X = x; Y = y; Old = o; }
    }

    readonly Dictionary<int, byte[]> cols = new();
    readonly List<Mod> log = new();

    public static int TypeOf(byte b) => b >> 5;
    public static int DmgOf(byte b) => b & 31;
    public static int HpOf(int type) => MaxHp[type];
    public static byte Make(int type, int dmg = 0) => (byte)((type << 5) | dmg);

    public byte Get(int tx, int ty)
    {
        if (ty < 0 || ty >= K.ROWS) return 0;
        return cols.TryGetValue(tx, out var c) ? c[ty] : (byte)0;
    }
    public int Type(int tx, int ty) => Get(tx, ty) >> 5;
    public bool Solid(int tx, int ty) => (Get(tx, ty) >> 5) != 0;
    public bool SolidAt(float x, float y) => Solid((int)MathF.Floor(x / K.T), (int)MathF.Floor(y / K.T));

    public void SetRaw(int tx, int ty, byte v)
    {
        if (ty < 0 || ty >= K.ROWS) return;
        if (!cols.TryGetValue(tx, out var c)) { c = new byte[K.ROWS]; cols[tx] = c; }
        c[ty] = v;
    }

    public void Set(int frame, int tx, int ty, byte v)
    {
        byte old = Get(tx, ty);
        if (old == v || ty < 0 || ty >= K.ROWS) return;
        log.Add(new Mod(frame, tx, ty, old));
        SetRaw(tx, ty, v);
    }

    public void RewindTo(int frame)
    {
        for (int i = log.Count - 1; i >= 0 && log[i].Frame > frame; i--)
        {
            var m = log[i];
            SetRaw(m.X, m.Y, m.Old);
            log.RemoveAt(i);
        }
    }

    public void Trim(int minFrame, int minCol)
    {
        int n = 0;
        while (n < log.Count && log[n].Frame <= minFrame) n++;
        if (n > 0) log.RemoveRange(0, n);
        if (cols.Count > 400)
            foreach (var k in cols.Keys.Where(k => k < minCol).ToList()) cols.Remove(k);
    }

    /// <summary>Primeira linha solida da coluna, ou -1 se for um buraco.</summary>
    public int Surface(int tx)
    {
        for (int y = 0; y < K.ROWS; y++) if (Solid(tx, y)) return y;
        return -1;
    }
}

/// <summary>Corpo de inimigo/heroi voando e girando apos morrer (so visual).</summary>
public sealed class Corpse
{
    public float X, Y, VX, VY, Angle, Spin, Life = 2.4f, T;
    public int Facing, Bounces;
    public Look Look;
    public bool IsFlyer, Rest;
    public Era? Era;
}

/// <summary>Imagem residual do heroi durante dash/jato.</summary>
public sealed class Trail
{
    public float X, Y, Life;
    public int Facing, Char;
    public Anim Anim;
}

public enum PKind { Pixel, Smoke, Spark, Flash, Ring, Debris, Fire, Flame, Blood, Gib, Ember, Shell, BurnDebris }

public sealed class Particle
{
    public float X, Y, VX, VY, Life, Max, Size, Grav;
    public Color C, C2;
    public PKind Kind;
}

/// <summary>Mancha permanente (sangue, queimado) grudada num tile.</summary>
public struct Decal
{
    public int X, Y;
    public Color C;
}

public sealed class FloatText
{
    public float X, Y, Life = 1f;
    public string Text = "";
    public Color C;
}
