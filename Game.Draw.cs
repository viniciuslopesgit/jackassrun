using System.Numerics;
using Raylib_cs;
using static JackassRun.Col;
using static JackassRun.Gfx;

namespace JackassRun;

// Pipeline de desenho:
//  1. Cenario (ceu + 2 camadas de parallax) direto na tela, cada camada com deslocamento sub-pixel proprio.
//  2. Mundo (terreno, personagens, efeitos) numa textura 321x180 com alpha pre-multiplicado,
//     composta na tela com deslocamento sub-pixel da camera -> rolagem suave e pixels nitidos.
//  3. HUD e telas por cima, na mesma grade de pixels.
public sealed partial class Game
{
    int cam;            // camera inteira (pixel perfect) usada no mundo
    float camFrac;      // fracao da camera aplicada na composicao final
    int shY;            // tremor vertical
    float time;         // relogio de animacao visual
    int scrOx, scrOy;   // origem da area de jogo na tela
    float scrScale = 4;

    const int GL_ONE = 1, GL_ZERO = 0, GL_SRC_ALPHA = 0x0302, GL_ONE_MINUS_SRC_ALPHA = 0x0303, GL_ADD = 0x8006;

    /// <summary>Mistura que grava alpha pre-multiplicado numa textura transparente.</summary>
    static void BlendPremulStore()
    {
        Raylib.EndBlendMode();
        Rlgl.SetBlendFactorsSeparate(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA, GL_ONE, GL_ONE_MINUS_SRC_ALPHA, GL_ADD, GL_ADD);
        Raylib.BeginBlendMode(BlendMode.CustomSeparate);
    }

    /// <summary>Mistura aditiva para brilhos (soma cor, nao mexe no alpha).</summary>
    static void BlendGlow()
    {
        Raylib.EndBlendMode();
        Rlgl.SetBlendFactorsSeparate(GL_SRC_ALPHA, GL_ONE, GL_ZERO, GL_ONE, GL_ADD, GL_ADD);
        Raylib.BeginBlendMode(BlendMode.CustomSeparate);
    }

    void Compose(RenderTexture2D worldRT)
    {
        // mundo -> textura
        time += Raylib.GetFrameTime();
        Raylib.BeginTextureMode(worldRT);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        BlendPremulStore();
        DrawWorld();
        Raylib.EndBlendMode();
        Raylib.EndTextureMode();

        // tela
        Raylib.BeginDrawing();
        Raylib.ClearBackground(Ink);
        int sw = Raylib.GetScreenWidth(), sh = Raylib.GetScreenHeight();
        float scale = MathF.Min(sw / (float)K.W, sh / (float)K.H);
        if (scale >= 1) scale = MathF.Floor(scale);
        int dw = (int)(K.W * scale), dh = (int)(K.H * scale);
        scrOx = (sw - dw) / 2; scrOy = (sh - dh) / 2; scrScale = scale;

        Raylib.BeginScissorMode(scrOx, scrOy, dw, dh);
        DrawBackground();
        Raylib.BeginBlendMode(BlendMode.AlphaPremultiply);
        Raylib.DrawTexturePro(worldRT.Texture, new Rectangle(0, 0, K.W + 1, -K.H),
            new Rectangle(scrOx - camFrac * scale, scrOy, (K.W + 1) * scale, dh), Vector2.Zero, 0, Color.White);
        Raylib.EndBlendMode();
        Raylib.BeginMode2D(ScreenCam(0));
        DrawOverlay();
        Raylib.EndMode2D();
        Raylib.EndScissorMode();
    }

    Camera2D ScreenCam(float frac) => new() { Offset = new Vector2(scrOx, scrOy), Target = new Vector2(frac, 0), Zoom = scrScale };

    int SX(float x) => (int)MathF.Floor(x) - cam;
    int SY(float y) => (int)MathF.Floor(y) + shY;

    // ================================================================== MUNDO

    void DrawWorld()
    {
        int sx = shake > 0 ? (int)MathF.Round((float)(fx.NextDouble() * 2 - 1) * shake) : 0;
        shY = shake > 0 ? (int)MathF.Round((float)(fx.NextDouble() * 2 - 1) * shake) : 0;
        int baseCam = (int)MathF.Floor(S.CamX);
        camFrac = S.CamX - baseCam;
        cam = baseCam + sx;

        DrawTerrain();
        DrawDecals();
        DrawFalling();
        DrawProps();
        DrawEnemies();
        DrawCorpses();
        DrawTrails();
        DrawGhosts();
        DrawPlayer();
        DrawBullets();
        foreach (var e in S.Explosions) DrawExplosion(e);
        DrawParticles(true);
        DrawParticles(false);

        BlendGlow();
        DrawGlows();
        BlendPremulStore();

        DrawTexts();
        if (S.FreezeT > 0 && mode != Mode.Rewind)
            Rect(0, 0, K.W + 1, K.H, A(Cyan, 0.10f + 0.04f * MathF.Sin(time * 10)));
    }

    // ------------------------------------------------------------------ terreno

    void DrawTerrain()
    {
        int c0 = (int)MathF.Floor(cam / (float)K.T) - 1, c1 = c0 + K.W / K.T + 3;
        for (int tx = c0; tx <= c1; tx++)
        {
            var e = Eras.ForCol(tx);
            for (int ty = 0; ty < K.ROWS; ty++)
            {
                byte b = Ter.Get(tx, ty);
                int type = Terrain.TypeOf(b);
                if (type == 0) continue;
                DrawTile(tx, ty, type, Terrain.DmgOf(b), e);
            }
            // fenda temporal entre eras
            if (tx % (K.CHUNK * K.CHUNKS_PER_ERA) == 0 && tx > 0)
            {
                int x = tx * K.T - cam;
                for (int y = 0; y < K.H; y += 2)
                {
                    int o = (int)(MathF.Sin(time * 8 + y * 0.3f) * 3);
                    Rect(x + o - 1, y + shY, 3, 2, A(y % 4 == 0 ? Cyan : Magenta, 0.6f));
                    if (Hash.F(y, (int)(time * 20)) > 0.85f) Rect(x + o - 4, y + shY, 9, 1, A(White, 0.8f));
                }
            }
        }
    }

    void DrawTile(int tx, int ty, int type, int dmg, Era e)
    {
        bool upE = !Ter.Solid(tx, ty - 1), dnE = !Ter.Solid(tx, ty + 1) && ty < K.ROWS - 1;
        bool lE = !Ter.Solid(tx - 1, ty), rE = !Ter.Solid(tx + 1, ty);
        int depth = upE ? 0 : !Ter.Solid(tx, ty - 2) ? 1 : 2;
        TileAt(tx * K.T - cam, ty * K.T + shY, tx, ty, type, dmg, Eras.IndexForCol(tx), upE, dnE, lE, rE, depth, true);
    }

    /// <summary>Desenha um tile a partir de design/tiles/&lt;era&gt;.png (ver LEIAME na pasta design).</summary>
    void TileAt(int x, int y, int tx, int ty, int type, int dmg, int era, bool upE, bool dnE, bool lE, bool rE, int depth, bool decor)
    {
        var tex = Art.Tiles[era];
        uint hsh = Hash.H(tx, ty);
        (int col, int row) = type switch
        {
            Terrain.DIRT => ((int)(hsh % 4), depth),
            Terrain.BRICK => (ty & 1, 3),
            Terrain.STEEL => (2, 3),
            _ => (3, 3),
        };
        Raylib.DrawTextureRec(tex, new Rectangle(col * 8, row * 8, 8, 8), new Vector2(x, y), Color.White);
        if (dmg > 0) Raylib.DrawTextureRec(tex, new Rectangle((3 + Math.Min(dmg, 3)) * 8, 24, 8, 8), new Vector2(x, y), Color.White);
        // sem contornos: so uma leve sombra embaixo de blocos suspensos
        if (dnE) Rect(x, y + 7, 8, 1, A(Ink, 0.18f));
        if (upE && type == Terrain.DIRT)
        {
            if (decor)
            {
                // enfeites (tufos e flores) balancando ao vento: 2 quadros cada
                int sway = MathF.Sin(time * 2.6f + tx * 0.9f) > 0.25f ? 1 : 0;
                if ((hsh & 3) == 0) Decor(tex, sway, x, y);
                if ((hsh & 12) == 4) Decor(tex, 2 + sway, x, y);
                if (hsh % 13 == 0) Decor(tex, 4 + sway, x, y);
            }
        }
    }

    static void Decor(Texture2D tex, int col, int x, int y) =>
        Raylib.DrawTextureRec(tex, new Rectangle(col * 8, 32, 8, 8), new Vector2(x, y - 8), Color.White);

    void DrawDecals()
    {
        int left = cam - 2, right = cam + K.W + 2;
        foreach (var d in decals)
        {
            if (d.X < left || d.X > right) continue;
            if (Ter.SolidAt(d.X + 0.5f, d.Y + 0.5f)) Rect(d.X - cam, d.Y + shY, 1, 1, d.C);
        }
    }

    void DrawFalling()
    {
        foreach (var f in S.Falling)
        {
            if (f.Dead) continue;
            int tx = (int)MathF.Floor((f.X + 4) / K.T);
            TileAt(SX(f.X), SY(f.Y), tx, 3, Terrain.TypeOf(f.Tile), Terrain.DmgOf(f.Tile), Eras.IndexForCol(tx), true, true, true, true, 1, false);
        }
    }

    // ------------------------------------------------------------------ entidades

    void DrawProps()
    {
        foreach (var p in S.Props)
        {
            if (p.Done) continue;
            int x = SX(p.X), y = SY(p.Y);
            if (x < -30 || x > K.W + 30) continue;
            switch (p.Kind)
            {
                case PropKind.Barrel:
                    bool lit = p.Fuse >= 0;
                    Art.Cell(Art.Barrel, lit && (int)(time * 30) % 2 == 0 ? 1 : 0, 0, x + (lit ? (int)MathF.Round(MathF.Sin(time * 60)) : 0), y);
                    break;
                case PropKind.Cage:
                    var hero = Art.Heroes[p.Char % Art.Heroes.Length];
                    Art.Human(hero, x, y - 2, 1, Anim.Of(MathF.Sin(p.T * 2.2f) > 0 ? AState.Cheer : AState.Idle, p.T));
                    Art.Cell(Art.Cage, (int)(p.T * 8) % 2, 0, x, y);
                    if (MathF.Sin(p.T * 2.3f) > 0.4f) TextC("SOCORRO!", x, y - 48 - (int)(MathF.Abs(MathF.Sin(p.T * 8)) * 2), 10, White, Ink);
                    break;
                case PropKind.Glorb:
                    Art.Cell(Art.Glorb, (int)(p.T * 5) % 4, 0, x, y + (int)MathF.Round(MathF.Sin(p.T * 3) * 2));
                    break;
            }
        }
    }

    static Look EnemyLook(Era e, EnemyKind k) => DesignExport.EnemyLook(e, k);

    void DrawEnemies()
    {
        foreach (var e in S.Enemies)
        {
            if (e.Dead) continue;
            int x = SX(e.X), y = SY(e.Y);
            if (x < -30 || x > K.W + 30) continue;
            var era = Eras.ForX(e.X);
            int ei = Eras.IndexForX(e.X);
            bool hurt = e.HurtT > 0.05f;
            Color? over = hurt ? White : null;
            Color? tint = S.FreezeT > 0 ? Hex(0xa8f0ff) : null;
            switch (e.Kind)
            {
                case EnemyKind.Flyer:
                    Art.Cell(Art.Flyers[ei], (int)(e.AnimT * 14) & 3, 0, x, y, e.Facing, 0, over ?? tint, hurt);
                    break;
                case EnemyKind.Turret:
                    Art.Cell(Art.Turrets[ei], ((int)(e.AnimT * 4) % 2) + (e.MuzzleT > 0 ? 2 : 0), 0, x, y, e.Facing, 0, over ?? tint, hurt);
                    break;
                default:
                {
                    Anim an;
                    int hop = 0;
                    if (e.HurtT > 0) an = Anim.Of(AState.Hurt, e.AnimT);
                    else if (e.AlertT > 0)
                    {
                        an = Anim.Of(AState.Jump, e.AnimT);
                        hop = (int)(MathF.Sin((1 - e.AlertT / 0.5f) * MathF.PI) * 5);
                    }
                    else an = Anim.FromPhysics(e.OnGround, false, e.VX, e.VY, e.AnimT, e.LandT);
                    an.Recoil = e.MuzzleT > 0 ? 1 : 0;
                    Art.Human(Art.Grunts[ei, (int)e.Kind], x, y - hop, e.Facing, an, 1, over, tint);
                    break;
                }
            }
            if (e.MuzzleT > 0 && e.Kind != EnemyKind.Flyer)
            {
                int gy = e.Kind == EnemyKind.Turret ? y - 8 : y + (e.Kind == EnemyKind.Rocketeer ? -11 : -7);
                MuzzleStar(x + e.Facing * (e.Kind == EnemyKind.Turret ? 12 : e.Kind == EnemyKind.Brute ? 13 : 10), gy, e.Facing, 3, (int)(time * 30), Yellow);
            }
            if (e.AlertT > 0)
            {
                float k = 1 - e.AlertT / 0.5f;
                int by = y - (e.Kind == EnemyKind.Brute ? 38 : 34) - (int)(MathF.Sin(k * MathF.PI) * 6);
                int grow = k < 0.2f ? 1 : 0;
                Rect(x - 2 - grow, by - grow, 4 + grow * 2, 8 + grow, Red);
                Rect(x - 1, by + 1, 2, 4, White); Rect(x - 1, by + 6, 2, 1, White);
            }
            if (e.Kind is EnemyKind.Brute or EnemyKind.Turret && e.Hp > 0)
            {
                int max = e.Kind == EnemyKind.Brute ? 8 : 6;
                int top = e.Kind == EnemyKind.Brute ? y - 36 : y - 15;
                Rect(x - 7, top, 14, 2, A(Ink, 0.45f));
                Rect(x - 7, top, 14 * e.Hp / max, 2, Red);
            }
        }
    }

    void DrawCorpses()
    {
        foreach (var c in corpses)
        {
            if (c.Sprite == null) continue;
            float a = c.Life < 0.6f ? ((int)(c.Life * 20) % 2 == 0 ? 0.3f : 1f) : 1f;
            int rot = (((int)MathF.Round(c.Angle / 90f)) % 4 + 4) % 4;
            int x = SX(c.X), y = SY(c.Y);
            if (c.IsFlyer) Art.Cell(c.Sprite, 0, 0, x, y, c.Facing, rot, A(White, a));
            else
            {
                var an = Anim.Of(c.Rest ? AState.Hurt : AState.Tumble, c.T);
                an.Rot = rot;
                Art.Human(c.Sprite, x, y - Pivot, c.Facing, an, a);
            }
        }
    }

    void DrawTrails()
    {
        foreach (var t in trails)
            Art.Human(Art.Heroes[t.Char], SX(t.X), SY(t.Y), t.Facing, t.Anim, t.Life / 0.22f * 0.55f, Chars.All[t.Char].Look.Accent);
    }

    void DrawGhosts()
    {
        foreach (var g in ghosts)
        {
            int idx = S.Frame - g.StartFrame;
            if (idx < 0 || idx >= g.Frames.Count) continue;
            var f = g.Frames[idx];
            var look = Chars.All[g.Char].Look;
            float a = 0.5f + 0.15f * MathF.Sin(time * 12 + g.StartFrame);
            int x = SX(f.X), y = SY(f.Y);
            for (int k = 1; k <= 3; k++)
            {
                int pi = idx - k * 3;
                if (pi < 0) break;
                var pf = g.Frames[pi];
                var pa = Anim.FromPhysics(pf.OnGround, pf.Climbing, pf.VX, pf.VY, (S.Frame - k * 3) * K.DT);
                Art.Human(Art.Heroes[g.Char], SX(pf.X), SY(pf.Y), pf.Facing, pa, 0.16f - k * 0.04f, Cyan);
            }
            var an = Anim.FromPhysics(f.OnGround, f.Climbing, f.VX, f.VY, S.Frame * K.DT);
            an.Recoil = f.Fire ? 2 : 0;
            Art.Human(Art.Heroes[g.Char], x, y, f.Facing, an, a);
            if (f.Fire && g.Char != 4) { var gg = Sprites.GunOf(look); MuzzleStar(x + f.Facing * gg.MuzzleX, y + gg.MuzzleY, f.Facing, 3, S.Frame, A(Yellow, 0.7f)); }
            int left = g.Frames.Count - idx;
            if (g.Died && left < 120 && !g.Resolved && (int)(time * 8) % 2 == 0)
                TextC("!", x, y - 36, 10, Red);
        }
    }

    void DrawPlayer()
    {
        var p = P;
        if (p.Dead) return;
        if (p.InvulnT > 0 && (int)(time * 20) % 2 == 0 && p.DashT <= 0) return;
        var ch = Chars.All[p.Char];
        int x = SX(p.X), y = SY(p.Y);
        var an = Anim.FromPhysics(p.OnGround, p.Climbing, p.VX, p.VY, p.AnimT, p.LandT, p.DashT > 0);
        an.Recoil = p.Recoil;
        Art.Human(Art.Heroes[p.Char], x, y, p.Facing, an);
        if (p.MuzzleT > 0 && p.Char != 4)
        {
            var gun = Sprites.GunOf(ch.Look);
            int mx = x + p.Facing * gun.MuzzleX, my = y + gun.MuzzleY;
            MuzzleStar(mx, my, p.Facing, p.Char == 1 || p.Char == 3 ? 5 : 3, S.Frame, Yellow);
        }
        if (p.Shield > 0)
        {
            int r = 14 + (int)MathF.Round(MathF.Sin(time * 8));
            Raylib.DrawCircleLines(x, y - 11, r, A(Cyan, 0.85f));
            Raylib.DrawCircleLines(x, y - 11, r + 1, A(Cyan, 0.3f));
        }
        if (p.InvulnT > 0.5f) TextC("VOCE", x, y - 40 - (int)(MathF.Abs(MathF.Sin(time * 6)) * 3), 10, Yellow);
    }

    void DrawBullets()
    {
        foreach (var b in S.Bullets)
        {
            if (b.Dead) continue;
            int x = SX(b.X), y = SY(b.Y);
            int d = b.VX >= 0 ? 1 : -1;
            float a = b.Ghost ? 0.6f : 1f;
            if (b.Kind == BulletKind.Slash)
            {
                float t = 1 - b.Life / (b.W > 40 ? 0.14f : 0.1f);
                int w = (int)(b.W / 2);
                for (int i = -3; i <= 3; i++)
                {
                    int len = w - Math.Abs(i) * Math.Abs(i);
                    Rect(x - len, y + i * 3, len * 2, 1, A(i == 0 ? White : Hex(0xdfe8f0), (1 - t) * a));
                }
                continue;
            }
            int col = b.Kind switch
            {
                BulletKind.Bullet => Art.ProjBullet, BulletKind.Pellet => Art.ProjPellet, BulletKind.Laser => Art.ProjLaser,
                BulletKind.Rocket => Art.ProjRocket, BulletKind.ERocket => Art.ProjERocket, BulletKind.Grenade => Art.ProjGrenade,
                BulletKind.Dynamite => Art.ProjDynamite, BulletKind.EBomb => Art.ProjBomb, _ => Art.ProjEBullet,
            };
            int frame = b.Kind is BulletKind.Grenade ? (int)(b.T * 10) % 2 : (int)(time * 20) % 2;
            var tint = b.Kind == BulletKind.EBullet ? (b.FromPlayer ? Yellow : Eras.ForX(b.X).EBullet) : White;
            Art.Cell(Art.Proj, col, frame, x, y, d, 0, A(tint, a));
        }
    }

    /// <summary>Explosao em "nuvem" de varias bolhas: branco -> amarelo -> laranja -> fumaca.</summary>
    void DrawExplosion(Explosion ex)
    {
        int sd = (int)(ex.X * 7 + ex.Y * 13);
        int n = 5 + (int)(ex.R / 7);
        if (ex.T < 0.05f) { PixelCircle(SX(ex.X), SY(ex.Y), (int)(ex.R * 0.9f), White); return; }
        if (mode != Mode.Rewind) return;
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < n; i++)
            {
                float ang = Hash.F(i, sd) * 6.283f;
                float dist = i == 0 ? 0 : (0.25f + Hash.F(i, sd + 1) * 0.5f) * ex.R;
                float lt = (ex.T - i * 0.02f) / (ex.Dur * 0.95f);
                if (lt < 0 || lt > 1) continue;
                float baseR = ex.R * (0.3f + Hash.F(i, sd + 2) * 0.22f) * (i == 0 ? 1.4f : 1f);
                float grow = 1 - MathF.Pow(1 - MathF.Min(1, lt * 4f), 3);
                float shrink = lt > 0.55f ? 1 - (lt - 0.55f) / 0.45f : 1;
                int r = (int)(baseR * grow * (0.45f + 0.55f * shrink));
                if (r < 1) continue;
                float rise = lt * lt * ex.R * 0.7f;
                int px = SX(ex.X + MathF.Cos(ang) * dist), py = SY(ex.Y + MathF.Sin(ang) * dist * 0.75f - rise);
                Color c = lt < 0.1f ? White : lt < 0.24f ? Yellow : lt < 0.4f ? Orange : lt < 0.55f ? Hex(0xd8401e)
                    : Col.Lerp(Hex(0x6a4a46), Hex(0x3a3240), (lt - 0.55f) / 0.45f);
                float alpha = MathF.Min(1, lt > 0.7f ? shrink / 0.55f : 1);
                if (pass == 0) continue;
                else
                {
                    PixelCircle(px, py, r, A(c, alpha));
                    if (r > 2) PixelCircle(px - r / 3, py - r / 3, r / 2, A(Col.Lerp(c, White, lt < 0.55f ? 0.45f : 0.12f), alpha));
                }
            }
    }

    /// <summary>smokePass: fumaca fica atras de todo o resto.</summary>
    void DrawParticles(bool smokePass)
    {
        foreach (var p in parts)
        {
            if ((p.Kind == PKind.Smoke) != smokePass) continue;
            float t = p.Life / p.Max;
            int x = SX(p.X), y = SY(p.Y);
            switch (p.Kind)
            {
                case PKind.Smoke:
                {
                    int r = Math.Max(2, (int)MathF.Round(p.Size * (0.6f + (1 - t) * 0.9f)));
                    float a = MathF.Min(1, t * 1.6f) * (p.C.A / 255f);
                    PixelCircle(x, y, r, A(r > 2 ? Col.Mul(p.C, 0.7f) : p.C, a));
                    if (r > 2) PixelCircle(x - 1, y - 1, r - 1, A(p.C, a));
                    if (r > 3) PixelCircle(x - r / 3, y - r / 3, r / 3, A(Col.Mul(p.C, 1.35f), a));
                    break;
                }
                case PKind.Fire:
                {
                    // rampa de cor do fogo: branco -> amarelo -> laranja -> vermelho -> fumaca
                    float age = 1 - t;
                    int r = (int)MathF.Round(p.Size * MathF.Min(1, age * 6) * (0.45f + 0.55f * t));
                    Color c = t > 0.9f ? White : t > 0.72f ? Yellow : t > 0.45f ? Orange : t > 0.25f ? FireRed : A(SmokeDark, t / 0.25f);
                    Color edge = t > 0.72f ? Orange : t > 0.45f ? FireRed : t > 0.25f ? Hex(0x8a2414) : A(Col.Mul(SmokeDark, 0.7f), t / 0.25f);
                    PixelCircle(x, y, r, edge);
                    if (r > 0) PixelCircle(x, y - (r > 2 ? 1 : 0), r - 1, c);
                    break;
                }
                case PKind.Flame:
                {
                    // fogo parado queimando no chao
                    float fl = MathF.Sin(time * 28 + p.X * 1.3f);
                    float k = MathF.Min(1, t * 3);
                    int h = (int)((p.Size * 2 + 2 + fl * 1.5f) * k);
                    if (h < 1) break;
                    int w = (int)MathF.Max(1, p.Size * k);
                    Rect(x - w, y - h / 2, w * 2 + 1, h / 2, FireRed);
                    Rect(x - w + 1, y - h + 1, w * 2 - 1, h - 1, Orange);
                    Rect(x - (w > 2 ? 1 : 0), y - h / 2 - 1, w > 2 ? 3 : 1, h / 2, Yellow);
                    Rect(x + (fl > 0 ? 1 : -1), y - h - 1, 1, 2, Orange);
                    if (fl > 0.6f) Rect(x, y - h - 3, 1, 1, Yellow);
                    break;
                }
                case PKind.Blood:
                    Rect(x, y, (int)p.Size, (int)p.Size, p.C);
                    break;
                case PKind.Gib:
                {
                    int s = (int)p.Size;
                    float a = t < 0.2f ? t / 0.2f : 1;
                    Rect(x, y, s, s, A(p.C, a));
                    Rect(x, y + s - 1, s, 1, A(p.C2, a));
                    break;
                }
                case PKind.Debris:
                case PKind.BurnDebris:
                {
                    int s = (int)MathF.Max(1, p.Size);
                    float a = t < 0.25f ? t / 0.25f : 1;
                    Rect(x, y, s, s, A(p.C, a));
                    if (s > 1) Rect(x, y, s, 1, A(Col.Mul(p.C, 1.3f), a));
                    if (p.Kind == PKind.BurnDebris) Rect(x + (int)(time * 30) % 2, y - 1, 1, 1, Yellow);
                    break;
                }
                case PKind.Ember:
                    if (((int)(time * 30) + (int)p.X) % 3 != 0) Rect(x, y, 1, 1, A(p.C, MathF.Min(1, t * 2)));
                    break;
                case PKind.Shell:
                    Rect(x, y, 2, 1, p.C);
                    break;
                case PKind.Pixel:
                    int ps = (int)MathF.Max(1, p.Size * MathF.Min(1, t * 2) + 0.5f);
                    Rect(x, y, ps, ps, p.C);
                    break;
                case PKind.Spark:
                    Gfx.Line(x, y, x - p.VX * 0.025f, y - p.VY * 0.025f, A(p.C, t));
                    break;
                case PKind.Flash:
                {
                    int r = (int)(p.Size * t) + 1;
                    Rect(x - r * 2, y, r * 4 + 1, 1, p.C);
                    Rect(x, y - r * 2, 1, r * 4 + 1, p.C);
                    PixelCircle(x, y, r, p.C);
                    break;
                }
                case PKind.Ring:
                    Raylib.DrawCircleLines(x, y, p.Size * (1 - t * t), A(p.C, t));
                    Raylib.DrawCircleLines(x, y, p.Size * (1 - t * t) + 1, A(p.C, t * 0.4f));
                    break;
            }
        }
    }

    void DrawGlows()
    {
        foreach (var b in S.Bullets)
        {
            if (b.Dead) continue;
            float x = SX(b.X), y = SY(b.Y);
            switch (b.Kind)
            {
                case BulletKind.Bullet: case BulletKind.Pellet: Glow(x, y, 7, Yellow, 0.35f); break;
                case BulletKind.Laser: Glow(x - 6, y, 12, Cyan, 0.5f); Glow(x + 6, y, 12, Cyan, 0.5f); break;
                case BulletKind.Rocket: case BulletKind.ERocket: Glow(x - MathF.Sign(b.VX) * 5, y, 11, Orange, 0.55f); break;
                case BulletKind.EBullet: Glow(x, y, 8, b.FromPlayer ? Yellow : Eras.ForX(b.X).EBullet, 0.5f); break;
                case BulletKind.Dynamite: Glow(x, y - 5, 6, Yellow, 0.5f); break;
                case BulletKind.Slash: Glow(x, y, b.W * 0.6f, White, 0.25f); break;
            }
        }
        foreach (var e in S.Explosions)
        {
            float t = e.T / e.Dur;
            Glow(SX(e.X), SY(e.Y), e.R * (2.2f - t), Orange, 0.45f * (1 - t));
            if (t < 0.2f) Glow(SX(e.X), SY(e.Y), e.R * 1.2f, Yellow, 0.3f);
        }
        if (!P.Dead && P.MuzzleT > 0 && P.Char != 4)
            { var gg = Sprites.GunOf(Chars.All[P.Char].Look); Glow(SX(P.X) + P.Facing * gg.MuzzleX, SY(P.Y) + gg.MuzzleY, 18, Yellow, 0.6f); }
        foreach (var e in S.Enemies)
            if (!e.Dead && e.MuzzleT > 0) Glow(SX(e.X) + e.Facing * 11, SY(e.Y) + (e.Kind == EnemyKind.Rocketeer ? -11 : -7), 14, Orange, 0.55f);
        foreach (var p in S.Props)
            if (!p.Done && p.Kind == PropKind.Glorb) Glow(SX(p.X), SY(p.Y), 10, Cyan, 0.25f + 0.1f * MathF.Sin(p.T * 6));
            else if (!p.Done && p.Kind == PropKind.Barrel && p.Fuse >= 0) Glow(SX(p.X), SY(p.Y) - 5, 14, Red, 0.5f);
        foreach (var p in parts)
            if (p.Kind == PKind.Flash) Glow(SX(p.X), SY(p.Y), p.Size * 3, p.C, 0.5f * p.Life / p.Max);
            else if (p.Kind == PKind.Spark) Glow(SX(p.X), SY(p.Y), 3, p.C, 0.4f * p.Life / p.Max);
            else if (p.Kind == PKind.Fire && p.Life / p.Max > 0.35f && ((int)p.Max * 100 + (int)p.Size) % 3 == 0) Glow(SX(p.X), SY(p.Y), p.Size * 2.2f, Orange, 0.1f);
            else if (p.Kind == PKind.Flame) Glow(SX(p.X), SY(p.Y) - 3, 12, Orange, 0.3f + 0.1f * MathF.Sin(time * 25 + p.X));
            else if (p.Kind == PKind.BurnDebris) Glow(SX(p.X), SY(p.Y), 6, Orange, 0.35f);
        foreach (var g in ghosts)
        {
            int idx = S.Frame - g.StartFrame;
            if (idx < 0 || idx >= g.Frames.Count) continue;
            Glow(SX(g.Frames[idx].X), SY(g.Frames[idx].Y) - 11, 18, Cyan, 0.18f);
        }
        if (P.Shield > 0 && !P.Dead) Glow(SX(P.X), SY(P.Y) - 11, 18, Cyan, 0.2f);
    }

    void DrawTexts()
    {
        foreach (var t in texts)
        {
            float a = MathF.Min(1, t.Life * 2);
            float pop = t.Life > 1.05f ? (t.Life - 1.05f) * 20 : 0;     // "salta" ao aparecer
            Text(t.Text, SX(t.X) - Raylib.MeasureText(t.Text, 10) / 2, SY(t.Y) - (int)pop, 10, A(t.C, a), A(Ink, a));
        }
    }

    // ================================================================== CENARIO (tela, sub-pixel por camada)

    void DrawBackground()
    {
        int ei = Eras.IndexForX(S.CamX + K.W * 0.5f);
        var e = Eras.All[ei];
        Raylib.BeginMode2D(ScreenCam(0));
        Raylib.DrawTexture(Art.Sky[ei], 0, 0, Color.White);
        if (e.Style is BgStyle.Medieval or BgStyle.Future)
            for (int i = 0; i < 50; i++)
            {
                float x = (Hash.F(i, 3) * K.W * 2 - S.CamX * 0.02f) % K.W; if (x < 0) x += K.W;
                int y = (int)(Hash.F(i, 4) * 90);
                bool tw = Hash.F(i, 5) > 0.7f && MathF.Sin(time * 3 + i) > 0.5f;
                Raylib.DrawRectangleRec(new Rectangle(MathF.Floor(x), y, 1, 1), A(White, tw ? 1f : 0.55f));
            }
        // camadas de parallax (posicao em float = rolagem suave sub-pixel)
        BgLayer(Art.Far[ei], S.CamX * 0.12f);
        Raylib.DrawRectangleGradientV(0, K.H - 90, K.W + 1, 90, A(e.SkyBot, 0f), A(e.SkyBot, 0.35f));
        BgLayer(Art.Mid[ei], S.CamX * 0.35f);

        // particulas de ambiente (folhas, cinzas, vagalumes, chuva neon)
        for (int i = 0; i < 30; i++)
        {
            float speed = 10 + Hash.F(i, 9) * 20;
            float px = (Hash.F(i, 7) * K.W * 3 - S.CamX * 0.6f - time * speed) % K.W; if (px < 0) px += K.W;
            float py = (Hash.F(i, 8) * K.H + time * speed * (e.Style == BgStyle.Future ? 6 : 0.8f)) % K.H;
            px = MathF.Round(px * 2) / 2; py = MathF.Round(py * 2) / 2;
            switch (e.Style)
            {
                case BgStyle.Jungle:
                    float ly = py + MathF.Sin(time * 2 + i) * 4;
                    Raylib.DrawRectangleRec(new Rectangle(px, ly, MathF.Sin(time * 3 + i) > 0 ? 2 : 1, 1), A(e.Top, 0.75f));
                    break;
                case BgStyle.Dino:
                    Raylib.DrawRectangleRec(new Rectangle(px, K.H - py, 1, 1), A(i % 3 == 0 ? Yellow : Orange, 0.85f));
                    break;
                case BgStyle.Medieval:
                    if (MathF.Sin(time * 3 + i * 1.7f) > 0) Raylib.DrawRectangleRec(new Rectangle(px, py * 0.7f + 40, 1, 1), Yellow);
                    break;
                default:
                    Raylib.DrawRectangleRec(new Rectangle(px, py, 1, 4), A(Cyan, 0.35f));
                    break;
            }
        }
        Raylib.EndMode2D();
    }

    static void BgLayer(Texture2D t, float scroll)
    {
        if (t.Width <= 0) return;
        float off = scroll % t.Width;
        for (float x = -off; x < K.W + 1; x += t.Width)
            Raylib.DrawTextureV(t, new Vector2(x, K.H - t.Height), Color.White);
    }

    // ================================================================== HUD e telas (tela, grade de pixels)

    void DrawOverlay()
    {
        if (mode is Mode.Play or Mode.Dying or Mode.Paused)
        {
            Raylib.DrawRectangleGradientH(0, 0, 40, K.H, A(Ink, 0.35f), A(Ink, 0));
            Raylib.DrawRectangleGradientH(K.W - 40, 0, 40, K.H, A(Ink, 0), A(Ink, 0.35f));
        }
        switch (mode)
        {
            case Mode.Title: DrawTitle(); break;
            case Mode.Play: DrawHud(); break;
            case Mode.Dying: DrawHud(); DrawDying(); break;
            case Mode.Rewind: DrawRewind(); break;
            case Mode.Select: DrawSelect(); break;
            case Mode.Paused: DrawHud(); DrawPaused(); break;
            case Mode.GameOver: DrawGameOver(); break;
        }
        if (flash > 0) Rect(0, 0, K.W, K.H, A(White, MathF.Min(flash, 0.8f)));
    }

    void DrawHud()
    {
        var ch = Chars.All[P.Char];
        Rect(4, 4, 20, 20, A(Hex(0x2a2040), 0.75f));
        int sx = (int)(scrOx + 4 * scrScale), sy = (int)(scrOy + 4 * scrScale), ss = (int)(20 * scrScale);
        Raylib.EndScissorMode();
        Raylib.BeginScissorMode(sx, sy, ss, ss);
        Art.Human(Art.Heroes[P.Char], 14, 30, 1, Anim.Of(AState.Idle, time));
        Raylib.EndScissorMode();
        Raylib.BeginScissorMode(scrOx, scrOy, (int)(K.W * scrScale), (int)(K.H * scrScale));
        Text(ch.Name, 28, 4, 10, White);
        for (int i = 0; i < P.Specials; i++) Box(28 + i * 6, 16, 4, 6, Orange);
        for (int i = 0; i < P.Shield; i++) Raylib.DrawCircleLines(31 + P.Specials * 6 + i * 8, 19, 3, Cyan);

        int tx = K.W / 2 - 18;
        Text("TIME OUT", tx - 6, 4, 10, Cyan);
        for (int i = 0; i < Math.Min(timeOuts, 8); i++) Hourglass(tx + 50 + i * 7, 5, i);
        if (timeOuts > 8) Text("+" + (timeOuts - 8), tx + 50 + 8 * 7, 4, 10, Cyan);
        Rect(tx - 6, 16, 60, 3, A(Ink, 0.45f));
        Rect(tx - 6, 16, 60 * glorbCount / 12, 3, Magenta);

        string m = Meters + " M";
        Text(m, K.W - 4 - Raylib.MeasureText(m, 10), 4, 10, Yellow);
        string sc = FinalScore().ToString("N0").Replace(',', '.');
        Text(sc, K.W - 4 - Raylib.MeasureText(sc, 10), 15, 10, White);

        if (eraBannerT > 0)
        {
            float a = MathF.Min(1, eraBannerT);
            float slideIn = MathF.Max(0, eraBannerT - 2.5f) * 2;
            int slide = (int)(slideIn * slideIn * 220);
            Rect(0, 52, K.W, 34, A(Ink, 0.55f * a));
            TextC(eraBanner, K.W / 2 + slide, 56, 20, A(Yellow, a), A(Ink, a));
            TextC(eraSub, K.W / 2 - slide, 76, 10, A(Cyan, a), A(Ink, a));
        }
    }

    void Hourglass(int x, int y, int i)
    {
        int f = (int)(time * 2 + i * 0.3f) % 4;
        Rect(x, y, 5, 1, Hex(0x2a8aa8)); Rect(x, y + 7, 5, 1, Hex(0x2a8aa8));
        Rect(x + 1, y + 1, 3, 2 - (f == 3 ? 1 : 0), Cyan); Rect(x + 2, y + 3, 1, 2, Cyan);
        Rect(x + 1, y + 5 + (f == 0 ? 1 : 0), 3, 2 - (f == 0 ? 1 : 0), Hex(0x2a8aa8));
    }

    void DrawDying()
    {
        float a = MathF.Min(1, modeT * 3);
        Rect(0, 0, K.W, K.H, A(Red, 0.15f * a));
        int y = 64 - (int)(MathF.Max(0, 0.3f - modeT) * 100);
        int size = Raylib.MeasureText(deathMsg, 20) > K.W - 24 ? 10 : 20;
        TextC(deathMsg, K.W / 2, y + (20 - size) / 2, size, A(White, a), A(Ink, a));
        TextC(timeOuts > 0 ? "VOLTANDO NO TEMPO..." : "SEM TIME OUTS!", K.W / 2, y + 24, 10, A(timeOuts > 0 ? Cyan : Red, a), A(Ink, a));
    }

    void DrawRewind()
    {
        Rect(0, 0, K.W, K.H, A(Hex(0x2a3a8a), 0.35f));
        for (int y = 0; y < K.H; y += 2) Rect(0, y, K.W, 1, A(Ink, 0.18f));
        for (int i = 0; i < 4; i++)
        {
            int y = (int)((time * 90 + i * 53) % K.H);
            Rect(0, y, K.W, 2 + i % 2, A(White, 0.12f));
            Rect(fx.Next(0, K.W), y, fx.Next(10, 60), 1, A(White, 0.5f));
        }
        bool blink = (int)(time * 3) % 2 == 0;
        Text("<< REBOBINANDO", 8, 8, 10, blink ? White : Cyan);
        TextC("APERTE PULAR / ATIRAR PARA PARAR AQUI", K.W / 2, K.H - 46, 10, Yellow);

        int bw = K.W - 40;
        Rect(20, K.H - 14, bw, 4, A(Ink, 0.5f));
        float pos = hist.Count <= 1 ? 0 : rewindIdx / (float)(hist.Count - 1);
        Rect(20, K.H - 14, (int)(bw * pos), 4, Cyan);
        foreach (var g in ghosts)
        {
            float gs = (g.StartFrame - hist[0].Frame) / (float)Math.Max(1, hist[^1].Frame - hist[0].Frame);
            if (gs >= 0 && gs <= 1) Rect(20 + (int)(bw * gs), K.H - 17, 1, 10, Magenta);
        }
        float secs = (hist[^1].Frame - S.Frame) * K.DT;
        Text("-" + secs.ToString("0.0") + "s", 20 + (int)(bw * pos) - 10, K.H - 26, 10, White);
    }

    void DrawSelect()
    {
        Rect(0, 0, K.W, K.H, A(Ink, 0.55f));
        TextC("ESCOLHA SEU HEROI", K.W / 2, 20, 20, Yellow);
        TextC("TIME OUTS RESTANTES: " + timeOuts, K.W / 2, 42, 10, Cyan);
        int n = Chars.All.Length, slot = 56, x0 = K.W / 2 - (n * slot) / 2;
        for (int i = 0; i < n; i++)
        {
            var c = Chars.All[i];
            int x = x0 + i * slot + slot / 2;
            bool sel = i == selIdx;
            int by = sel ? 58 : 62;
            Rect(x - 24, by, 48, 56, sel ? Hex(0x4a3a82) : Hex(0x221a36));
            if (sel) Rect(x - 24, by + 54, 48, 2, Yellow);
            Art.Human(Art.Heroes[i], x, by + 46, 1, Anim.Of(sel ? AState.Run : AState.Idle, time + i), sel ? 1 : 0.7f);
        }
        var s = Chars.All[selIdx];
        TextC(s.Name, K.W / 2, 124, 20, White);
        TextC(s.Tag, K.W / 2, 144, 10, Magenta);
        TextC("ARMA: " + s.Weapon + "   ESPECIAL: " + s.Special, K.W / 2, 156, 10, Cyan);
        TextC("< >  ESCOLHER     ENTER  VOLTAR AO COMBATE", K.W / 2, 168, 10, A(White, 0.8f));
    }

    void DrawPaused()
    {
        Rect(0, 0, K.W, K.H, A(Ink, 0.6f));
        TextC("PAUSA", K.W / 2, 40, 20, Yellow);
        ControlsText(70);
        TextC("ESC: CONTINUAR     Q: MENU", K.W / 2, 160, 10, Cyan);
    }

    void ControlsText(int y)
    {
        string[] lines =
        {
            "MOVER: SETAS / A D      PULAR: ESPACO / W",
            "ATIRAR: J / Z           ESPECIAL: K / X",
            "TIME OUT (VOLTAR NO TEMPO): L / C",
            "SEGURE CONTRA A PAREDE PARA ESCALAR",
            "F: TELA CHEIA    M: MUSICA",
        };
        for (int i = 0; i < lines.Length; i++) TextC(lines[i], K.W / 2, y + i * 12, 10, White);
    }

    void DrawTitle()
    {
        Rect(0, 0, K.W, K.H, A(Ink, 0.35f));
        int bob = (int)MathF.Round(MathF.Sin(titleT * 3) * 2);
        int gl = (int)(titleT * 10) % 23 == 0 ? 2 : 0;
        TextC("JACKASS RUN", K.W / 2 + 2 + gl, 22 + bob, 30, Magenta, Magenta);
        TextC("JACKASS RUN", K.W / 2 - 2 - gl, 22 + bob, 30, Cyan, Cyan);
        TextC("JACKASS RUN", K.W / 2, 22 + bob, 30, Yellow);
        TextC("TIME FORCE BROS", K.W / 2, 54, 10, White);
        for (int i = 0; i < Chars.All.Length; i++)
        {
            int x = K.W / 2 - 80 + i * 40;
            Art.Human(Art.Heroes[i], x, 98, 1, Anim.Of(AState.Run, titleT + i * 0.27f));
        }
        if ((int)(titleT * 2) % 2 == 0) TextC("APERTE ENTER PARA COMECAR", K.W / 2, 108, 10, Yellow);
        ControlsText(122);
        if (best > 0) Text("RECORDE: " + best.ToString("N0").Replace(',', '.'), 4, 4, 10, Cyan);
    }

    void DrawGameOver()
    {
        Rect(0, 0, K.W, K.H, A(Ink, MathF.Min(0.7f, modeT)));
        TextC("FIM DOS TEMPOS", K.W / 2, 34, 30, Red);
        TextC(deathMsg, K.W / 2, 66, 10, White);
        TextC("DISTANCIA: " + Meters + " M", K.W / 2, 88, 10, Yellow);
        TextC("INIMIGOS: " + S.Kills, K.W / 2, 100, 10, Yellow);
        TextC("PONTOS: " + lastScore.ToString("N0").Replace(',', '.'), K.W / 2, 112, 20, White);
        if (newBest && (int)(modeT * 4) % 2 == 0) TextC("NOVO RECORDE!", K.W / 2, 134, 10, Green);
        else TextC("RECORDE: " + best.ToString("N0").Replace(',', '.'), K.W / 2, 134, 10, Cyan);
        if (modeT > 1) TextC("ENTER: JOGAR DE NOVO     Q: MENU", K.W / 2, 156, 10, White);
    }
}
