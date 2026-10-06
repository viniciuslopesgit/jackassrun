using System.Numerics;
using Raylib_cs;
using static JackassRun.Col;

namespace JackassRun;

public enum AState : byte { Idle, Run, Jump, Fall, Climb, Dash, Cheer, Tumble, Hurt }

/// <summary>Estado de animacao de um boneco: qual pose, relogio e alguns modificadores.</summary>
public struct Anim
{
    public AState S;
    public float T, Speed, Recoil, Land;
    public int Rot;            // rotacao em passos de 90 graus (corpos voando)

    public static Anim Of(AState s, float t, float speed = 88) => new() { S = s, T = t, Speed = speed };

    public static Anim FromPhysics(bool onGround, bool climbing, float vx, float vy, float t, float land = 0, bool dash = false)
    {
        var s = dash ? AState.Dash : climbing ? AState.Climb : !onGround ? (vy < -20 ? AState.Jump : AState.Fall)
              : MathF.Abs(vx) > 8 ? AState.Run : AState.Idle;
        return new Anim { S = s, T = t, Speed = MathF.Abs(vx), Land = land };
    }
}

/// <summary>Desenho procedural em pixel art. Cada sprite e montado com retangulos e ganha
/// contorno escuro de 1px. Proporcoes e animacao no estilo Broforce: "brutamontes" com
/// cabeca pequena, ombros largos, arma nas duas maos e passadas longas.</summary>
public static partial class Gfx
{
    struct Part { public int X, Y, W, H; public Color C; public bool NoOutline; }
    static readonly Part[] parts = new Part[400];
    static int np, offX, offY;
    public const int Pivot = -9;   // centro do corpo, usado para girar corpos

    static void P(int x, int y, int w, int h, Color c, bool noOutline = false)
    {
        if (w <= 0 || h <= 0) return;
        if (np < parts.Length) parts[np++] = new Part { X = x + offX, Y = y + offY, W = w, H = h, C = c, NoOutline = noOutline };
    }

    public static void Rect(int x, int y, int w, int h, Color c) => Raylib.DrawRectangle(x, y, w, h, c);

    /// <summary>Retangulo preenchido (sem contorno: o visual do jogo usa so cor e sombreamento).</summary>
    public static void Box(int x, int y, int w, int h, Color c) => Raylib.DrawRectangle(x, y, w, h, c);

    /// <summary>Circulo feito de linhas horizontais (fica pixelado e nitido em qualquer escala).</summary>
    public static void PixelCircle(int cx, int cy, int r, Color c)
    {
        if (r <= 0) { Raylib.DrawRectangle(cx, cy, 1, 1, c); return; }
        for (int dy = -r; dy <= r; dy++)
        {
            int half = (int)MathF.Sqrt(r * r - dy * dy + r * 0.6f);
            Raylib.DrawRectangle(cx - half, cy + dy, half * 2 + 1, 1, c);
        }
    }

    /// <summary>Desenha as partes acumuladas: primeiro todos os contornos, depois os preenchimentos.
    /// rot gira em passos de 90 graus em torno do centro do corpo.</summary>
    static void Flush(int fx, int fy, int facing, float alpha, Color? over, int rot = 0, bool outline = false)
    {
        rot &= 3;
        const int px = 0, py = Pivot;
        for (int pass = outline ? 0 : 1; pass < 2; pass++)
            for (int i = 0; i < np; i++)
            {
                ref var p = ref parts[i];
                int rx = facing >= 0 ? p.X : -p.X - p.W, ry = p.Y, w = p.W, h = p.H;
                if (rot != 0)
                {
                    int ax = rx - px, ay = ry - py;
                    switch (rot)
                    {
                        case 1: (rx, ry, w, h) = (-(ay + h), ax, h, w); break;
                        case 2: (rx, ry) = (-(ax + w), -(ay + h)); break;
                        default: (rx, ry, w, h) = (ay, -(ax + w), h, w); break;
                    }
                    rx += px; ry += py;
                }
                int x = fx + rx, y = fy + ry;
                if (pass == 0)
                {
                    if (!p.NoOutline) Raylib.DrawRectangle(x - 1, y - 1, w + 2, h + 2, A(Ink, alpha));
                }
                else
                {
                    var c = over.HasValue && (!p.NoOutline || !outline) ? over.Value : p.C;
                    Raylib.DrawRectangle(x, y, w, h, A(c, alpha));
                }
            }
        np = 0; offX = offY = 0;
    }

    /// <summary>Inimigo voador; o visual muda com a era (helidrone, pterodactilo, morcego, OVNI).</summary>
    public static void Flyer(int cx, int cy, int facing, Era e, float t, Color? over, float alpha = 1, int rot = 0)
    {
        np = 0;
        int f = (int)(t * 14) & 3;
        int wy = f == 0 ? -1 : f == 2 ? 1 : 0;
        var body = e.FlyBody; var wing = e.FlyWing;
        int hover = f == 1 || f == 2 ? 1 : 0;
        offY = Pivot + hover;
        switch (e.Style)
        {
            case BgStyle.Jungle: // helidrone
            case BgStyle.City:   // helicoptero da policia
                offY = Pivot;
                P(-6, -3, 12, 6, body); P(-10, -1, 4, 2, body); P(-11, -3 + (f & 1), 2, 2, body);
                P(-5, -2, 9, 1, Mul(body, 1.3f), true);
                P(2, -2, 3, 2, Cyan); P(-1, -5, 2, 2, Mul(body, 0.7f));
                int r = f switch { 0 => 8, 1 => 5, 2 => 2, _ => 5 };
                P(-r, -6, r * 2, 1, wing);
                P(-2, 3, 4, 2, Mul(body, 0.6f));
                break;
            case BgStyle.Dino: // pterodactilo
                P(-5, -2, 10, 4, body); P(5, -3, 3, 3, body); P(8, -2, 4, 1, Yellow); P(3, -5, 1, 3, body);
                P(-4, -1, 8, 1, Mul(body, 1.25f), true);
                P(6, -2, 1, 1, Ink, true);
                if (wy < 0) { P(-6, -7, 6, 4, wing); P(-1, -8, 4, 3, wing); }
                else if (wy == 0) { P(-9, -2, 6, 2, wing); P(-1, -3, 6, 2, wing); }
                else { P(-6, 2, 6, 3, wing); P(-1, 2, 4, 4, wing); }
                break;
            case BgStyle.Medieval: // morcego-gargula
                P(-4, -3, 8, 7, body); P(-3, -6, 2, 3, body); P(1, -6, 2, 3, body);
                P(1, -1, 2, 1, Red, true);
                if (wy < 0) { P(-12, -6, 8, 3, wing); P(4, -6, 8, 3, wing); }
                else if (wy == 0) { P(-12, -2, 8, 2, wing); P(4, -2, 8, 2, wing); }
                else { P(-12, 1, 8, 3, wing); P(4, 1, 8, 3, wing); }
                break;
            default: // OVNI
                P(-9, -1, 18, 4, body); P(-4, -5, 8, 4, wing); P(-2, -4, 2, 1, White, true);
                P(-8, -1, 16, 1, Mul(body, 1.25f), true);
                for (int i = 0; i < 4; i++)
                    P(-7 + i * 4, 1, 2, 1, ((int)(t * 10) + i) % 4 == 0 ? Yellow : Mul(body, 0.6f), true);
                break;
        }
        Flush(cx, cy - Pivot, facing, alpha, over, rot);
    }

    public static void Turret(int cx, int fy, int facing, Era e, float t, float recoil, Color? over)
    {
        np = 0;
        P(-7, -6, 14, 6, e.SteelDark); P(-6, -5, 12, 1, e.Steel, true);
        P(-4, -10, 8, 4, e.Steel); P(-3, -10, 5, 1, Mul(e.Steel, 1.25f), true);
        P(3 - (int)recoil, -9, 8, 3, Mul(e.SteelDark, 0.8f));
        P(-1, -9, 2, 2, (int)(t * 4) % 2 == 0 ? Red : Mul(Red, 0.4f), true);
        Flush(cx, fy, facing, 1, over);
    }

    public static void Barrel(int cx, int fy, bool blink, float wobble)
    {
        np = 0;
        var red = blink ? White : Hex(0xd8342a);
        int w = (int)wobble;
        P(-4 + w, -10, 8, 10, red); P(-4 + w, -8, 8, 1, Yellow, true); P(-4 + w, -3, 8, 1, Yellow, true);
        P(-2 + w, -7, 3, 3, Mul(red, 0.5f), true); P(-3 + w, -10, 2, 10, Mul(red, 1.25f), true);
        P(2 + w, -10, 2, 10, Mul(red, 0.75f), true);
        Flush(cx, fy, 1, 1, null);
    }

    public static void Cage(int cx, int fy, float t, int ch)
    {
        var look = Chars.All[ch % Chars.All.Length].Look;
        bool wave = MathF.Sin(t * 2.2f) > 0;
        Humanoid(cx, fy - 2, 1, look, Anim.Of(wave ? AState.Cheer : AState.Idle, t));
        var frame = Hex(0x5a4a3a);
        Box(cx - 9, fy - 28, 18, 2, frame);
        Box(cx - 9, fy - 2, 18, 2, frame);
        for (int i = 0; i < 6; i++) Rect(cx - 8 + i * 3, fy - 26, 1, 24, Hex(0x9a9aa8));
        Rect(cx - 8, fy - 26, 1, 24, Hex(0xd0d0dc));
        Rect(cx + 8, fy - 38, 1, 10, Ink);
        int fl = (int)(t * 8) % 3;
        Rect(cx + 9, fy - 38, 5 + (fl == 1 ? 1 : 0), 2, Red);
        Rect(cx + 9, fy - 36, 4 + fl, 2, Mul(Red, 0.8f));
        if (MathF.Sin(t * 2.3f) > 0.4f) TextC("SOCORRO!", cx, fy - 48 - (int)(MathF.Abs(MathF.Sin(t * 8)) * 2), 10, White, Ink);
    }

    public static void Glorb(int cx, int cy, float t)
    {
        float p = 0.5f + 0.5f * MathF.Sin(t * 6);
        var gc = Col.Lerp(Cyan, Magenta, p);
        PixelCircle(cx, cy, 4, Mul(gc, 0.6f));
        PixelCircle(cx, cy, 3, gc);
        Rect(cx - 1, cy - 2, 2, 1, White);
        if (((int)(t * 5) & 3) == 0) { Rect(cx + 4, cy - 4, 1, 1, White); Rect(cx + 3, cy - 5, 3, 1, A(White, 0.5f)); }
    }

    public static void Text(string s, int x, int y, int size, Color c, Color? outline = null)
    {
        // sombra suave em vez de contorno
        var o = outline ?? Ink;
        Raylib.DrawText(s, x + 1, y + 1, size, A(o, 0.55f));
        Raylib.DrawText(s, x, y, size, c);
    }

    public static void TextC(string s, int cx, int y, int size, Color c, Color? outline = null) =>
        Text(s, cx - Raylib.MeasureText(s, size) / 2, y, size, c, outline);

    public static void Line(float x0, float y0, float x1, float y1, Color c) =>
        Raylib.DrawLineV(new Vector2(x0, y0), new Vector2(x1, y1), c);
}
