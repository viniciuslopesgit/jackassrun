using Raylib_cs;

namespace JackassRun;

/// <summary>Constantes globais do jogo (resolucao interna em pixels, tiles, fisica).</summary>
public static class K
{
    public const int W = 320, H = 180;          // resolucao interna (pixel art), escalada na janela
    public const int T = 16;                    // tamanho do bloco (heroi ~ ate o ombro, como no Broforce)
    public const int HT = T / 2;
    public const int ROWS = 26;                 // linhas de blocos do mundo (ceu, superficie e subsolo)
    public const float ZOOM_OUT = 4f / 3f;      // zoom afastado (cavernas/quedas): 4x -> 3x na tela, pixels nitidos
    public static readonly int MaxViewW = (int)MathF.Ceiling(W * ZOOM_OUT), MaxViewH = (int)MathF.Ceiling(H * ZOOM_OUT);
    public const int CHUNK = 20;                // colunas por chunk gerado (320 px)
    public const int PX_PER_M = 8;              // pixels por metro no placar
    public const int CHUNKS_PER_ERA = 5;        // chunks ate trocar de era
    public const int HISTORY = 600;             // frames de historico para rebobinar (10s)
    public const float DT = 1f / 60f;
    public const float GRAV = 620f;
    public const int PLAYER_ID = 1;             // dono das balas do jogador
}

/// <summary>RNG xorshift como struct, para poder ser salvo no snapshot do mundo.</summary>
public struct Rng
{
    public uint S;
    public Rng(uint seed) { S = seed == 0 ? 0x9E3779B9u : seed; }
    public uint Next() { uint x = S; x ^= x << 13; x ^= x >> 17; x ^= x << 5; S = x; return x; }
    public float F() => (Next() & 0xFFFFFF) / 16777216f;
    public float Range(float a, float b) => a + (b - a) * F();
    public int Int(int a, int bExclusive) => bExclusive <= a ? a : a + (int)(Next() % (uint)(bExclusive - a));
    public bool Chance(float p) => F() < p;
}

public static class Hash
{
    public static uint H(int x, int y = 0)
    {
        uint h = (uint)x * 374761393u + (uint)y * 668265263u;
        h = (h ^ (h >> 13)) * 1274126177u;
        return h ^ (h >> 16);
    }
    public static float F(int x, int y = 0) => (H(x, y) & 0xFFFF) / 65535f;

    public static float Noise(float x, int seed)
    {
        int i = (int)MathF.Floor(x);
        float f = x - i, u = f * f * (3 - 2 * f);
        float a = F(i, seed), b = F(i + 1, seed);
        return a + (b - a) * u;
    }
    public static float Fbm(float x, int seed) =>
        Noise(x, seed) * 0.6f + Noise(x * 2.3f, seed + 7) * 0.3f + Noise(x * 5.1f, seed + 13) * 0.1f;
}

public static class Col
{
    public static Color Hex(uint rgb, byte a = 255) => new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, a);
    public static Color Mul(Color c, float f) =>
        new((byte)Math.Clamp(c.R * f, 0, 255), (byte)Math.Clamp(c.G * f, 0, 255), (byte)Math.Clamp(c.B * f, 0, 255), c.A);
    public static Color Lerp(Color a, Color b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return new((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t), (byte)(a.A + (b.A - a.A) * t));
    }
    public static Color A(Color c, float a) => new(c.R, c.G, c.B, (byte)Math.Clamp(a * c.A, 0, 255));

    public static readonly Color Ink = Hex(0x170f22);
    public static readonly Color White = Hex(0xffffff);
    public static readonly Color Yellow = Hex(0xffe14a);
    public static readonly Color Orange = Hex(0xff8a1e);
    public static readonly Color Red = Hex(0xe8283a);
    public static readonly Color Cyan = Hex(0x3ae8ff);
    public static readonly Color Magenta = Hex(0xff3ab0);
    public static readonly Color Green = Hex(0x6aff4a);
}

public struct InputState
{
    public bool Left, Right, Up, Down;
    public bool Jump, JumpHeld, Fire, FireHeld, Special, Rewind, Confirm, Back, Pause;

    /// <summary>Junta eventos "pressionado" de frames de render que nao tiveram passo de simulacao.</summary>
    public void MergePressed(in InputState o)
    {
        Jump |= o.Jump; Fire |= o.Fire; Special |= o.Special; Rewind |= o.Rewind;
        Confirm |= o.Confirm; Back |= o.Back; Pause |= o.Pause;
    }
    public void ClearPressed() { Jump = Fire = Special = Rewind = Confirm = Back = Pause = false; }
}

public static class Inp
{
    static bool KD(KeyboardKey k) => Raylib.IsKeyDown(k);
    static bool KP(KeyboardKey k) => Raylib.IsKeyPressed(k);
    static bool GD(GamepadButton b) => Raylib.IsGamepadAvailable(0) && Raylib.IsGamepadButtonDown(0, b);
    static bool GP(GamepadButton b) => Raylib.IsGamepadAvailable(0) && Raylib.IsGamepadButtonPressed(0, b);
    static float Axis(GamepadAxis a) => Raylib.IsGamepadAvailable(0) ? Raylib.GetGamepadAxisMovement(0, a) : 0;

    public static InputState Read()
    {
        var i = new InputState
        {
            Left = KD(KeyboardKey.Left) || KD(KeyboardKey.A) || GD(GamepadButton.LeftFaceLeft) || Axis(GamepadAxis.LeftX) < -0.4f,
            Right = KD(KeyboardKey.Right) || KD(KeyboardKey.D) || GD(GamepadButton.LeftFaceRight) || Axis(GamepadAxis.LeftX) > 0.4f,
            Up = KD(KeyboardKey.Up) || KD(KeyboardKey.W) || GD(GamepadButton.LeftFaceUp) || Axis(GamepadAxis.LeftY) < -0.5f,
            Down = KD(KeyboardKey.Down) || KD(KeyboardKey.S) || GD(GamepadButton.LeftFaceDown) || Axis(GamepadAxis.LeftY) > 0.5f,
            Jump = KP(KeyboardKey.Space) || KP(KeyboardKey.W) || KP(KeyboardKey.Up) || GP(GamepadButton.RightFaceDown),
            JumpHeld = KD(KeyboardKey.Space) || KD(KeyboardKey.W) || KD(KeyboardKey.Up) || GD(GamepadButton.RightFaceDown),
            Fire = KP(KeyboardKey.J) || KP(KeyboardKey.Z) || GP(GamepadButton.RightFaceLeft),
            FireHeld = KD(KeyboardKey.J) || KD(KeyboardKey.Z) || GD(GamepadButton.RightFaceLeft) || GD(GamepadButton.RightTrigger2),
            Special = KP(KeyboardKey.K) || KP(KeyboardKey.X) || GP(GamepadButton.RightFaceRight),
            Rewind = KP(KeyboardKey.L) || KP(KeyboardKey.C) || GP(GamepadButton.RightFaceUp),
            Confirm = KP(KeyboardKey.Enter) || KP(KeyboardKey.Space) || KP(KeyboardKey.J) || KP(KeyboardKey.Z) || GP(GamepadButton.RightFaceDown) || GP(GamepadButton.MiddleRight),
            Back = KP(KeyboardKey.Q) || KP(KeyboardKey.Backspace) || GP(GamepadButton.MiddleLeft),
            Pause = KP(KeyboardKey.Escape) || KP(KeyboardKey.P) || GP(GamepadButton.MiddleRight),
        };
        return i;
    }
}
