using Raylib_cs;

namespace JackassRun;

// Todas as entidades do mundo tem so campos de valor, entao MemberwiseClone basta
// para tirar o snapshot usado pela mecanica de rebobinar o tempo.

public enum EnemyKind
{
    Soldier, Rocketeer, Brute, Flyer, Turret, Knife, Bomber,
    Shield,      // escudeiro: escudo na frente bloqueia tiros; ataque por tras, por cima ou com explosao
    Grenadier,   // granadeiro: joga granadas em arco (passam por cima de coberturas e acertam outras alturas)
    Sniper,      // atirador de elite: mira laser vermelha antes do tiro rapido
    Flamer,      // lanca-chamas: jato de fogo de perto; o tanque explode quando ele morre
    Dog,         // cao de ataque: corre e salta no heroi (mordida mata)
}

public sealed class Enemy
{
    public int Id;
    public EnemyKind Kind;
    public float X, Y, VX, VY, BaseY, Phase;
    public int Hp, Facing = -1, Burst;
    public float FireT, AlertT, StateT, HurtT, MuzzleT, AnimT, LandT, HitDir;
    public float StunT, FuseT = -1;      // atordoado (porta chutada / pisao); pavio do homem-bomba
    public bool OnGround, Alerted, Walking, Dead, LastBoom;
    public bool OnLadder;                // subindo/descendo uma escada
    public float EnterT;                 // entrando correndo pela direita (emboscada): ignora a IA ate acabar
    public bool Para;                    // descendo de paraquedas
    public int LadderDir, LadderCol = int.MinValue;   // -1 sobe, 1 desce; ultima coluna de escada avaliada
    public float AimT, AimX, AimY;      // mira (atirador de elite), preparo da granada, jato (lanca-chamas), salto (cao)
    public float TurnT;                 // escudeiro: tempo com o alvo nas costas antes de virar
    public Enemy Clone() => (Enemy)MemberwiseClone();

    public float HalfW => Kind switch { EnemyKind.Brute => 6, EnemyKind.Turret => 7, EnemyKind.Flyer => 7, EnemyKind.Dog => 6, _ => 4 };
    public float Height => Kind switch { EnemyKind.Brute => 23, EnemyKind.Turret => 11, EnemyKind.Flyer => 9, EnemyKind.Dog => 11, _ => 19 };
    /// <summary>Caixa de colisao. Para voadores Y e o centro; para os demais, Y sao os pes.</summary>
    public (float x0, float y0, float x1, float y1) Box =>
        Kind == EnemyKind.Flyer
            ? (X - HalfW, Y - Height / 2, X + HalfW, Y + Height / 2)
            : (X - HalfW, Y - Height, X + HalfW, Y);
}

public enum BulletKind { Bullet, Pellet, Laser, Rocket, Grenade, Dynamite, Slash, EBullet, ERocket, EBomb, Batarang, Web, Arrow, EGrenade, EFlame }

public sealed class Bullet
{
    public int Id, OwnerId, LastHit;
    public BulletKind Kind;
    public bool FromPlayer, Ghost, Pierce, Dead;
    public float X, Y, VX, VY, Life, W = 3, H = 2, Grav, ExplodeR, T;
    public int Dmg = 1;
    public int Char = -1;      // heroi que atirou (dano nos blocos vem do HeroConfig); -1 = usa Dmg
    public int Spin = 1;       // sentido do giro do sprite da arma (1 horario, -1 anti-horario)
    public Bullet Clone() => (Bullet)MemberwiseClone();
}

public enum PropKind { Barrel, Cage, Glorb, Hostage, Car }

public sealed class Prop
{
    public int Id, Hp = 1, Char;
    public PropKind Kind;
    public float X, Y, VY, T, Fuse = -1;
    public bool Done;
    public Prop Clone() => (Prop)MemberwiseClone();
}

/// <summary>Emboscada (Metal Slug): quando o heroi passa da coluna Col, entram inimigos correndo pela
/// direita da tela (Type 0) ou caindo de paraquedas (Type 1).</summary>
public sealed class Ambush
{
    public int Col, Count, Type;
    public EnemyKind Enemy;
    public bool Fired;
    public Ambush Clone() => (Ambush)MemberwiseClone();
}

/// <summary>Tabua de ponte de madeira pisada pelo heroi: cai quando T chega a zero.</summary>
public sealed class BridgeFuse
{
    public int X, Y;
    public float T;
    public BridgeFuse Clone() => (BridgeFuse)MemberwiseClone();
}

/// <summary>Sala de um predio: fica escura ate ser revelada (porta aberta, parede destruida ou heroi dentro).
/// Inimigos numa sala escura nao enxergam o heroi.</summary>
public sealed class Room
{
    public int X0, Y0, X1, Y1;          // em blocos, inclusivo
    public bool Revealed;
    public float Fade = 1;              // 1 = escuro; some aos poucos ao revelar
    public Room Clone() => (Room)MemberwiseClone();
    public bool Contains(int tx, int ty, int pad = 0) => tx >= X0 - pad && tx <= X1 + pad && ty >= Y0 - pad && ty <= Y1 + pad;
}

/// <summary>Bloco de terreno caindo depois que a estrutura perdeu a sustentacao.</summary>
public sealed class FallingBlock
{
    public float X, Y, VY;      // canto superior esquerdo, em pixels
    public ushort Tile;
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
    public int Phase, PhaseKills0, PhaseRescues;       // level atual (0 = level 1), abates no inicio dele, resgates nele
    public float CamX, Speed = 34, FreezeT;
    public float CamY = 120, Zoom = 1;   // camera vertical e zoom (1 = normal, ZOOM_OUT = afastado)
    public float ShotCD;          // "vez" de atirar: enquanto > 0 nenhum inimigo atira
    public Rng Rng;
    public List<Enemy> Enemies = new();
    public List<Bullet> Bullets = new();
    public List<Prop> Props = new();
    public List<Explosion> Explosions = new();
    public List<FallingBlock> Falling = new();
    public List<Room> Rooms = new();
    public List<Ambush> Ambushes = new();
    public List<BridgeFuse> BridgeFuses = new();

    public WorldState Clone()
    {
        var s = (WorldState)MemberwiseClone();
        s.Enemies = Enemies.ConvertAll(e => e.Clone());
        s.Bullets = Bullets.ConvertAll(b => b.Clone());
        s.Props = Props.ConvertAll(p => p.Clone());
        s.Explosions = Explosions.ConvertAll(x => x.Clone());
        s.Falling = Falling.ConvertAll(f => f.Clone());
        s.Rooms = Rooms.ConvertAll(r => r.Clone());
        s.Ambushes = Ambushes.ConvertAll(a => a.Clone());
        s.BridgeFuses = BridgeFuses.ConvertAll(f => f.Clone());
        return s;
    }
}

public sealed class Player
{
    public int Char, Facing = 1, Specials = 3, Shield, WallDir, AirJumps;
    public bool AltHand;      // pistolas duplas: alterna a mao
    public float X, Y, VX, VY;
    public float FireT, InvulnT, AnimT, DashT, MuzzleT, Coyote, Recoil, LandT;
    public bool OnGround, Climbing, Dead;
    public bool OnLadder;     // subindo/descendo uma escada
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

/// <summary>Terreno destrutivel. Cada tile e um ushort: tipo | aparencia (grama/terra/fundo) | variacao | dano.
/// A aparencia e decidida ao gerar o bloco e viaja com ele: nunca muda por causa dos vizinhos.
/// Toda alteracao entra num log com o frame, para poder ser desfeita ao rebobinar.</summary>
public sealed class Terrain
{
    public const int EMPTY = 0, DIRT = 1, BRICK = 2, STEEL = 3, CRATE = 4, BEDROCK = 5, DOOR = 6, BRIDGE = 7, LADDER = 8, CONCRETE = 9, ROOF = 10;
    static readonly int[] MaxHp = { 0, 8, 5, 999, 3, 999, 2, 2, 3, 12, 3 };
    public static bool Unbreakable(int type) => type == STEEL || type == BEDROCK;
    /// <summary>Ponte e escada nao sao paredes: atravessa-se pelos lados e por baixo.</summary>
    public static bool Passable(int type) => type == BRIDGE || type == LADDER;

    readonly struct Mod
    {
        public readonly int Frame, X, Y; public readonly ushort Old;
        public Mod(int f, int x, int y, ushort o) { Frame = f; X = x; Y = y; Old = o; }
    }

    readonly Dictionary<int, ushort[]> cols = new();
    readonly Dictionary<int, int> surf = new();      // linha da superficie original (abaixo dela = subsolo)

    public void SetSurface(int tx, int row) => surf[tx] = row;
    public bool Underground(int tx, int ty) => surf.TryGetValue(tx, out var r) && ty > r;
    /// <summary>Dentro da terra (inclui a linha da superficie): um buraco aqui mostra parede de terra ao fundo.</summary>
    public bool InGround(int tx, int ty) => surf.TryGetValue(tx, out var r) && ty >= r;
    readonly List<Mod> log = new();

    // bits: 0-3 dano | 4-5 variacao | 6-7 aparencia (0 grama, 1 terra, 2 fundo) | 8-11 tipo | 12-13 parede de fundo
    // A parede de fundo (BACK_*) fica no lugar quando o bloco da frente e destruido.
    public const int BACK_NONE = 0, BACK_EARTH = 1, BACK_WALL = 2, BACK_HOUSE = 3;   // HOUSE = parede de dentro de casa
    const ushort BackMask = 0x3000;
    public static int BackOf(ushort b) => (b >> 12) & 3;
    public static int TypeOf(ushort b) => (b >> 8) & 15;
    public static int DmgOf(ushort b) => b & 15;
    public static int VarOf(ushort b) => (b >> 4) & 3;
    public static int LookOf(ushort b) => (b >> 6) & 3;
    public static int HpOf(int type) => MaxHp[type];
    public static ushort Make(int type, int dmg = 0, int variant = 0, int look = 0) =>
        (ushort)((type << 8) | ((look & 3) << 6) | ((variant & 3) << 4) | Math.Min(dmg, 15));
    public static ushort WithDmg(ushort b, int dmg) => (ushort)((b & ~15) | Math.Min(dmg, 15));

    public ushort Get(int tx, int ty)
    {
        if (ty < 0 || ty >= K.ROWS) return 0;
        return cols.TryGetValue(tx, out var c) ? c[ty] : (ushort)0;
    }
    public int Type(int tx, int ty) => TypeOf(Get(tx, ty));
    public bool Solid(int tx, int ty) { int t = TypeOf(Get(tx, ty)); return t != 0 && !Passable(t); }
    public bool SolidAt(float x, float y) => Solid((int)MathF.Floor(x / K.T), (int)MathF.Floor(y / K.T));
    public bool Ladder(int tx, int ty) => Type(tx, ty) == LADDER;
    public bool LadderAt(float x, float y) => Ladder((int)MathF.Floor(x / K.T), (int)MathF.Floor(y / K.T));
    /// <summary>Piso de mao unica (so segura quem vem de cima): a ponte e o topo de uma escada.</summary>
    public bool Platform(int tx, int ty) { int t = Type(tx, ty); return t == BRIDGE || t == LADDER && Type(tx, ty - 1) != LADDER; }
    /// <summary>Da para pisar neste ponto? (bloco solido ou o topo de uma ponte/escada)</summary>
    public bool StandAt(float x, float y)
    {
        int tx = (int)MathF.Floor(x / K.T), ty = (int)MathF.Floor(y / K.T);
        return Solid(tx, ty) || Platform(tx, ty) && y - ty * K.T < 6;
    }

    public void SetRaw(int tx, int ty, ushort v)
    {
        if (ty < 0 || ty >= K.ROWS) return;
        if (!cols.TryGetValue(tx, out var c)) { c = new ushort[K.ROWS]; cols[tx] = c; }
        c[ty] = v;
    }

    /// <summary>Marca a parede de fundo de uma celula (usado na geracao).</summary>
    public void SetBack(int tx, int ty, int kind)
    {
        ushort v = Get(tx, ty);
        SetRaw(tx, ty, (ushort)((v & ~BackMask) | ((kind & 3) << 12)));
    }

    public void Set(int frame, int tx, int ty, ushort v)
    {
        ushort old = Get(tx, ty);
        v = (ushort)((v & ~BackMask) | (old & BackMask));      // a parede de fundo da celula nao muda
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
            foreach (var k in cols.Keys.Where(k => k < minCol).ToList()) { cols.Remove(k); surf.Remove(k); }
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
    public Sheet? Sprite;
    public bool IsFlyer, Rest;
    public bool Beast;          // animal (cao): desenhado como o voador (um quadro girando), mas sangra
    public int Cell;            // quadro da folha usado no corpo (voador e cao)
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
