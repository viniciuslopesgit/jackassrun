using System.Numerics;
using Raylib_cs;
using static JackassRun.Col;

namespace JackassRun;

/// <summary>Desenho procedural em pixel art: todo sprite e montado com retangulos
/// e ganha um contorno escuro de 1px, como nos sprites de Super Time Force.</summary>
public static class Gfx
{
    struct Part { public int X, Y, W, H; public Color C; public bool NoOutline; }
    static readonly Part[] parts = new Part[48];
    static int np;

    static void P(int x, int y, int w, int h, Color c, bool noOutline = false)
    {
        if (np < parts.Length) parts[np++] = new Part { X = x, Y = y, W = w, H = h, C = c, NoOutline = noOutline };
    }

    public static void Rect(int x, int y, int w, int h, Color c) => Raylib.DrawRectangle(x, y, w, h, c);

    /// <summary>Retangulo com contorno escuro.</summary>
    public static void Box(int x, int y, int w, int h, Color c)
    {
        Raylib.DrawRectangle(x - 1, y - 1, w + 2, h + 2, Ink);
        Raylib.DrawRectangle(x, y, w, h, c);
    }

    static void Flush(int fx, int fy, int facing, float alpha, Color? over)
    {
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < np; i++)
            {
                ref var p = ref parts[i];
                int x = facing >= 0 ? fx + p.X : fx - p.X - p.W;
                int y = fy + p.Y;
                if (pass == 0)
                {
                    if (!p.NoOutline) Raylib.DrawRectangle(x - 1, y - 1, p.W + 2, p.H + 2, A(Ink, alpha));
                }
                else
                {
                    var c = over.HasValue && !p.NoOutline ? over.Value : p.C;
                    Raylib.DrawRectangle(x, y, p.W, p.H, A(c, alpha));
                }
            }
        np = 0;
    }

    /// <summary>Boneco cabecudo. (fx,fy) = pes, centro. Usado para herois, inimigos, prisioneiros e fantasmas.</summary>
    public static void Humanoid(int fx, int fy, int facing, in Look L, float runT, bool moving, bool air,
        float recoil = 0, float alpha = 1, Color? over = null, bool armsUp = false)
    {
        np = 0;
        int bob = 0, lf = 0, lb = 0, lfy = 0, lby = 0;
        if (air) { lf = 1; lfy = -2; lb = -1; lby = -1; }
        else if (moving)
        {
            float s = MathF.Sin(runT * 15f);
            lf = (int)MathF.Round(s * 2); lb = -lf;
            lfy = s > 0.3f ? -1 : 0; lby = s < -0.3f ? -1 : 0;
            bob = MathF.Abs(s) > 0.7f ? -1 : 0;
        }
        int B = L.Bulk;
        var backShade = Mul(L.Pants, 0.72f);

        // perna de tras + braco de tras
        P(-3 + lb - B, -5 + lby, 3, 3, backShade);
        P(-3 + lb - B, -2 + lby, 3, 2, Mul(L.Boots, 0.8f));
        P(-4 - B, -10 + bob - B, 2, 4, Mul(L.Shirt, 0.75f));
        // perna da frente
        P(0 + lf + B, -5 + lfy, 3, 3, L.Pants);
        P(0 + lf + B, -2 + lfy, 4, 2, L.Boots);
        // tronco
        P(-3 - B, -11 + bob - B * 2, 7 + B * 2, 6 + B * 2, L.Shirt);
        P(-3 - B, -6 + bob, 7 + B * 2, 1, Mul(L.Pants, 0.6f));
        // cabeca
        int hy = -18 + bob - B * 2;
        P(-4, hy, 8, 7, L.Skin);

        switch (L.Hat)
        {
            case 0: // bandana
                P(-4, hy, 8, 2, L.Hair); P(-4, hy, 3, 4, L.Hair);
                P(-4, hy + 2, 8, 1, L.Accent, true); P(-7, hy + 2, 3, 1, L.Accent); P(-6, hy + 3, 2, 1, L.Accent);
                P(2, hy + 3, 1, 2, Ink, true);
                break;
            case 1: // capacete militar
                P(-5, hy - 1, 9, 3, L.Hair); P(3, hy + 1, 3, 1, Mul(L.Hair, 0.8f));
                P(2, hy + 3, 1, 2, Ink, true);
                P(-4, hy + 5, 1, 2, L.Accent, true);
                break;
            case 2: // cabelo maluco + oculos de protecao
                P(-5, hy - 2, 3, 3, L.Hair); P(-2, hy - 3, 3, 3, L.Hair); P(1, hy - 2, 3, 3, L.Hair);
                P(-5, hy, 3, 5, L.Hair);
                P(0, hy + 2, 5, 3, L.Accent); P(1, hy + 3, 1, 1, White, true); P(3, hy + 3, 1, 1, White, true);
                break;
            case 3: // mascara ninja
                P(-4, hy, 8, 7, L.Hair); P(0, hy + 3, 4, 2, L.Skin, true); P(2, hy + 3, 1, 1, Ink, true);
                P(-4, hy + 1, 8, 1, L.Accent, true); P(-8, hy + 1, 4, 1, L.Accent); P(-7, hy + 2, 2, 2, L.Accent);
                break;
            case 4: // capacete espacial
                P(-5, hy - 1, 10, 9, L.Hair); P(0, hy + 1, 5, 4, L.Accent); P(3, hy + 1, 1, 1, White, true);
                P(-4, hy - 3, 1, 2, Mul(L.Hair, 0.7f));
                break;
            case 5: // cranio/osso (homem das cavernas)
                P(-5, hy + 2, 3, 6, Mul(L.Shirt, 0.7f));
                P(-5, hy - 1, 9, 3, L.Accent); P(3, hy - 3, 2, 3, L.Accent); P(-4, hy - 3, 2, 3, L.Accent);
                P(2, hy + 3, 1, 2, Ink, true);
                break;
            case 6: // elmo de cavaleiro
                P(-5, hy - 1, 10, 8, L.Hair); P(0, hy + 3, 5, 1, Ink, true);
                P(-2, hy - 4, 3, 3, L.Accent); P(-4, hy - 3, 2, 2, L.Accent);
                break;
            case 7: // robo
                P(-5, hy - 1, 10, 8, L.Hair); P(-1, hy + 2, 6, 2, L.Accent, true);
                P(-1, hy - 4, 1, 3, Mul(L.Hair, 0.7f)); P(-2, hy - 5, 3, 1, L.Accent);
                break;
            case 8: // chapeu de cowboy + cabelo comprido
                P(-5, hy + 1, 3, 7, L.Hair); P(-4, hy, 8, 2, L.Hair);
                P(-6, hy - 1, 13, 1, L.Accent); P(-3, hy - 4, 7, 3, L.Accent); P(-3, hy - 2, 7, 1, Mul(L.Accent, 0.6f), true);
                P(2, hy + 3, 1, 2, Ink, true);
                break;
        }

        // arma / lamina
        int gy = -9 + bob - B, gx = 2 - (int)recoil;
        if (armsUp)
        {
            P(-5, hy - 3, 2, 5, L.Shirt); P(3, hy - 3, 2, 5, L.Shirt);
            P(-5, hy - 5, 2, 2, L.Skin); P(3, hy - 5, 2, 2, L.Skin);
        }
        else if (L.Blade)
        {
            P(gx, gy, 2, 2, L.Accent);
            P(gx + 2, gy - 1, L.GunLen, 1, L.Gun);
            P(gx + 1 + L.GunLen, gy - 2, 1, 1, L.Gun);
            P(0, gy, 3, 3, L.Shirt); P(2, gy + 1, 2, 2, L.Skin);
        }
        else if (L.GunLen > 0)
        {
            P(gx - 1, gy + 1, 2, 2, Mul(L.Gun, 0.75f));
            P(gx, gy - (L.GunH > 2 ? 1 : 0), L.GunLen, L.GunH, L.Gun);
            P(gx + L.GunLen - 2, gy - 1 - (L.GunH > 2 ? 1 : 0), 1, 1, L.Gun);
            P(0, gy, 3, 3, L.Shirt); P(2, gy + 1, 2, 2, L.Skin);
        }

        Flush(fx, fy, facing, alpha, over);
    }

    /// <summary>Inimigo voador; o visual muda com a era (helidrone, pterodactilo, morcego, OVNI).</summary>
    public static void Flyer(int cx, int cy, int facing, Era e, float t, Color? over, float alpha = 1)
    {
        np = 0;
        int flap = MathF.Sin(t * 16f) > 0 ? -1 : 1;
        var body = e.FlyBody; var wing = e.FlyWing;
        switch (e.Style)
        {
            case BgStyle.Jungle: // helidrone
                P(-6, -3, 12, 6, body); P(-10, -1, 4, 2, body); P(-11, -3, 2, 2, body);
                P(2, -2, 3, 2, Cyan); P(-1, -5, 2, 2, Mul(body, 0.7f));
                int r = (int)(t * 40) % 2 == 0 ? 8 : 5;
                P(-r, -6, r * 2, 1, wing);
                P(-2, 3, 4, 2, Mul(body, 0.6f));
                break;
            case BgStyle.Dino: // pterodactilo
                P(-5, -2, 10, 4, body); P(5, -3, 3, 3, body); P(8, -2, 4, 1, Yellow); P(3, -5, 1, 3, body);
                P(6, -2, 1, 1, Ink, true);
                if (flap < 0) { P(-6, -7, 6, 4, wing); P(-1, -8, 4, 3, wing); }
                else { P(-6, 2, 6, 3, wing); P(-1, 2, 4, 4, wing); }
                break;
            case BgStyle.Medieval: // morcego-gargula
                P(-4, -3, 8, 7, body); P(-3, -6, 2, 3, body); P(1, -6, 2, 3, body);
                P(1, -1, 2, 1, Red, true);
                if (flap < 0) { P(-12, -6, 8, 3, wing); P(4, -6, 8, 3, wing); }
                else { P(-12, 0, 8, 3, wing); P(4, 0, 8, 3, wing); }
                break;
            default: // OVNI
                P(-9, -1, 18, 4, body); P(-4, -5, 8, 4, wing); P(-2, -4, 2, 1, White, true);
                for (int i = 0; i < 3; i++)
                    P(-6 + i * 5, 1, 2, 1, ((int)(t * 8) + i) % 3 == 0 ? Yellow : Mul(body, 0.6f), true);
                break;
        }
        Flush(cx, cy, facing, alpha, over);
    }

    public static void Turret(int cx, int fy, int facing, Era e, float t, Color? over)
    {
        np = 0;
        P(-7, -6, 14, 6, e.SteelDark); P(-6, -5, 12, 1, e.Steel, true);
        P(-4, -10, 8, 4, e.Steel); P(3, -9, 8, 3, Mul(e.SteelDark, 0.8f));
        P(-1, -9, 2, 2, (int)(t * 4) % 2 == 0 ? Red : Mul(Red, 0.4f), true);
        Flush(cx, fy, facing, 1, over);
    }

    public static void Barrel(int cx, int fy, bool blink)
    {
        np = 0;
        var red = blink ? White : Hex(0xd8342a);
        P(-4, -10, 8, 10, red); P(-4, -8, 8, 1, Yellow, true); P(-4, -3, 8, 1, Yellow, true);
        P(-2, -7, 3, 3, Ink, true); P(-3, -10, 2, 10, Mul(red, 1.25f), true);
        Flush(cx, fy, 1, 1, null);
    }

    public static void Cage(int cx, int fy, float t, int ch)
    {
        // prisioneiro dentro
        var look = Chars.All[ch % Chars.All.Length].Look;
        bool wave = MathF.Sin(t * 6) > 0;
        Humanoid(cx, fy - 1, 1, look, 0, false, false, 0, 1, null, wave);
        // grades
        var frame = Hex(0x5a4a3a);
        Box(cx - 8, fy - 24, 16, 2, frame);
        Box(cx - 8, fy - 2, 16, 2, frame);
        for (int i = 0; i < 5; i++) Rect(cx - 7 + i * 3, fy - 22, 1, 20, Hex(0x9a9aa8));
        // bandeira
        Rect(cx + 7, fy - 34, 1, 10, Ink);
        Rect(cx + 8, fy - 34 + (int)(MathF.Sin(t * 5) * 1), 6, 4, Red);
        if (MathF.Sin(t * 2.3f) > 0.4f) Text("SOCORRO!", cx - 20, fy - 44, 10, White, Ink);
    }

    public static void Glorb(int cx, int cy, float t)
    {
        float p = 0.5f + 0.5f * MathF.Sin(t * 6);
        Raylib.DrawCircle(cx, cy, 5 + p * 2, A(Cyan, 0.25f));
        Raylib.DrawCircle(cx, cy, 4, Ink);
        Raylib.DrawCircle(cx, cy, 3, Col.Lerp(Cyan, Magenta, p));
        Rect(cx - 1, cy - 2, 1, 1, White);
    }

    public static void Text(string s, int x, int y, int size, Color c, Color? outline = null)
    {
        var o = outline ?? Ink;
        Raylib.DrawText(s, x - 1, y, size, o); Raylib.DrawText(s, x + 1, y, size, o);
        Raylib.DrawText(s, x, y - 1, size, o); Raylib.DrawText(s, x, y + 1, size, o);
        Raylib.DrawText(s, x + 1, y + 1, size, o);
        Raylib.DrawText(s, x, y, size, c);
    }

    public static void TextC(string s, int cx, int y, int size, Color c, Color? outline = null) =>
        Text(s, cx - Raylib.MeasureText(s, size) / 2, y, size, c, outline);

    public static void Line(float x0, float y0, float x1, float y1, Color c) =>
        Raylib.DrawLineV(new Vector2(x0, y0), new Vector2(x1, y1), c);
}
