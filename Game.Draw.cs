using Raylib_cs;
using static JackassRun.Col;
using static JackassRun.Gfx;

namespace JackassRun;

public sealed partial class Game
{
    int cam;          // camera inteira (pixel perfect)
    int shY;          // tremor vertical
    float time;       // relogio de animacao visual

    void Draw()
    {
        time += K.DT;
        float sx = shake > 0 ? (float)(fx.NextDouble() * 2 - 1) * shake : 0;
        shY = shake > 0 ? (int)((fx.NextDouble() * 2 - 1) * shake) : 0;
        cam = (int)MathF.Floor(S.CamX + sx);

        var era = Eras.ForX(S.CamX + K.W * 0.5f);
        DrawBackground(era);
        DrawTerrain();
        DrawProps();
        DrawEnemies();
        DrawGhosts();
        DrawPlayer();
        DrawBullets();
        DrawExplosions();
        DrawParticles();

        if (S.FreezeT > 0 && mode != Mode.Rewind)
            Rect(0, 0, K.W, K.H, A(Cyan, 0.10f + 0.04f * MathF.Sin(time * 10)));

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

    int SX(float x) => (int)MathF.Floor(x) - cam;
    int SY(float y) => (int)MathF.Floor(y) + shY;

    // ------------------------------------------------------------------ cenario

    void DrawBackground(Era e)
    {
        Raylib.DrawRectangleGradientV(0, 0, K.W, K.H, e.SkyTop, e.SkyBot);
        float camf = S.CamX;
        int seedI = (int)(seed & 0xffff);

        // estrelas nas eras noturnas
        if (e.Style is BgStyle.Medieval or BgStyle.Future)
            for (int i = 0; i < 50; i++)
            {
                int x = (int)(Hash.F(i, 3) * K.W * 2 - camf * 0.02f) % K.W; if (x < 0) x += K.W;
                int y = (int)(Hash.F(i, 4) * 90);
                bool tw = Hash.F(i, 5) > 0.7f && MathF.Sin(time * 3 + i) > 0.5f;
                Rect(x, y, 1, 1, A(White, tw ? 1f : 0.55f));
            }

        // sol / lua
        int sunX = 230, sunY = e.Style == BgStyle.Future ? 70 : 40;
        int sr = e.Style == BgStyle.Future ? 34 : 16;
        Raylib.DrawCircle(sunX, sunY, sr + 6, A(e.Sun, 0.18f));
        Raylib.DrawCircle(sunX, sunY, sr, e.Sun);
        if (e.Style == BgStyle.Future)
            for (int i = 0; i < 6; i++) Rect(sunX - sr, sunY + 4 + i * 5, sr * 2, 1 + i / 2, e.SkyBot);
        if (e.Style == BgStyle.Medieval) Raylib.DrawCircle(sunX + 5, sunY - 3, sr - 2, e.SkyTop);

        // camada distante
        float fx0 = camf * 0.12f;
        for (int x = 0; x < K.W; x++)
        {
            float wx = x + fx0;
            int h = e.Style switch
            {
                BgStyle.Dino => Volcano(wx),
                BgStyle.Future => Skyline(wx, 22, 60, 50, seedI + 1),
                _ => (int)(40 + Hash.Fbm(wx / 70f, seedI + 1) * 60),
            };
            Rect(x, K.H - h, 1, h, e.Far);
        }
        if (e.Style == BgStyle.Future) Windows(fx0, 22, 60, 50, seedI + 1, A(e.Sun, 0.5f));
        if (e.Style == BgStyle.Dino) DrawLavaGlow(fx0, e);

        // camada do meio
        float mx0 = camf * 0.35f;
        for (int x = 0; x < K.W; x++)
        {
            float wx = x + mx0;
            int h = e.Style switch
            {
                BgStyle.Jungle => (int)(30 + Hash.Fbm(wx / 30f, seedI + 2) * 30 + MathF.Abs(MathF.Sin(wx / 9f)) * 6),
                BgStyle.Dino => (int)(26 + Hash.Fbm(wx / 26f, seedI + 3) * 22 + (((int)wx / 3) % 5 == 0 ? 4 : 0)),
                BgStyle.Medieval => Castle(wx, seedI + 4),
                _ => Skyline(wx, 30, 40, 40, seedI + 5),
            };
            Rect(x, K.H - h, 1, h, e.Mid);
        }
        if (e.Style == BgStyle.Future) Windows(mx0, 30, 40, 40, seedI + 5, A(Cyan, 0.7f));
        if (e.Style == BgStyle.Medieval) Windows(mx0, 0, 0, 0, seedI + 4, A(Yellow, 0.8f), castle: true);

        // particulas de ambiente (folhas, cinzas, vagalumes, chuva neon)
        for (int i = 0; i < 26; i++)
        {
            float speed = 10 + Hash.F(i, 9) * 20;
            float px = (Hash.F(i, 7) * K.W * 3 - camf * 0.6f - time * speed) % K.W; if (px < 0) px += K.W;
            float py = (Hash.F(i, 8) * K.H + time * speed * (e.Style == BgStyle.Future ? 6 : 0.8f)) % K.H;
            switch (e.Style)
            {
                case BgStyle.Jungle: Rect((int)px, (int)(py + MathF.Sin(time * 2 + i) * 4), 2, 1, A(e.Top, 0.7f)); break;
                case BgStyle.Dino: Rect((int)px, (int)(K.H - py), 1, 1, A(Orange, 0.8f)); break;
                case BgStyle.Medieval: if (MathF.Sin(time * 3 + i * 1.7f) > 0) Rect((int)px, (int)(py * 0.7f + 40), 1, 1, Yellow); break;
                default: Rect((int)px, (int)py, 1, 4, A(Cyan, 0.35f)); break;
            }
        }
    }

    static int Volcano(float wx)
    {
        float local = ((wx % 180) + 180) % 180;
        int h = (int)MathF.Max(30, 100 - MathF.Abs(local - 90) * 1.1f);
        if (h > 92) h = 92;
        return h + (int)(Hash.Noise(wx / 10f, 11) * 4);
    }

    static int Skyline(float wx, int width, int min, int amp, int seedI)
    {
        int b = (int)MathF.Floor(wx / width);
        float local = wx - b * width;
        if (local < 3) return min / 2;   // vao entre predios
        int h = min + (int)(Hash.F(b, seedI) * amp);
        if (Hash.F(b, seedI + 9) > 0.7f && MathF.Abs(local - width / 2f) < 1.5f) h += 14;  // antena
        return h;
    }

    static int Castle(float wx, int seedI)
    {
        int b = (int)MathF.Floor(wx / 46);
        float local = wx - b * 46;
        int h = 26 + (int)(Hash.F(b, seedI) * 14);
        if (local > 14 && local < 26) { h += 20; if (local > 15 && local < 25) h += (int)(8 - MathF.Abs(local - 20) * 1.6f) + 6; }
        else if (((int)local / 3) % 2 == 0) h += 3;
        return h;
    }

    void Windows(float off, int width, int min, int amp, int seedI, Color c, bool castle = false)
    {
        int b0 = (int)MathF.Floor(off / (castle ? 46 : width)) - 1;
        for (int b = b0; b < b0 + K.W / (castle ? 46 : width) + 3; b++)
        {
            if (castle)
            {
                int bx = (int)(b * 46 - off) + 19;
                int h = 26 + (int)(Hash.F(b, seedI) * 14) + 20;
                Rect(bx, K.H - h + 8, 2, 3, c);
                continue;
            }
            int hgt = min + (int)(Hash.F(b, seedI) * amp);
            int x0 = (int)(b * width - off) + 4;
            for (int wy = K.H - hgt + 4; wy < K.H - 4; wy += 5)
                for (int wx = x0; wx < x0 + width - 6; wx += 4)
                    if (Hash.F(wx + (int)off * 0 + b * 31, wy) > 0.62f) Rect(wx, wy, 2, 2, c);
        }
    }

    void DrawLavaGlow(float off, Era e)
    {
        for (int x = 0; x < K.W; x++)
        {
            float wx = x + off;
            int h = Volcano(wx);
            if (h >= 88)
            {
                Rect(x, K.H - h, 1, 3, Orange);
                if (((int)wx + (int)(time * 20)) % 7 == 0) Rect(x, K.H - h - 2 - (int)(MathF.Sin(time * 4 + wx) * 3), 1, 2, Yellow);
            }
        }
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
                    Rect(x + o - 1, y + shY, 3, 2, A(y % 4 == 0 ? Cyan : Magenta, 0.55f));
                    if (Hash.F(y, (int)(time * 20)) > 0.85f) Rect(x + o - 4, y + shY, 9, 1, A(White, 0.8f));
                }
            }
        }
    }

    void DrawTile(int tx, int ty, int type, int dmg, Era e)
    {
        int x = tx * K.T - cam, y = ty * K.T + shY;
        bool upE = !Ter.Solid(tx, ty - 1), dnE = !Ter.Solid(tx, ty + 1) && ty < K.ROWS - 1;
        bool lE = !Ter.Solid(tx - 1, ty), rE = !Ter.Solid(tx + 1, ty);
        uint hsh = Hash.H(tx, ty);

        switch (type)
        {
            case Terrain.DIRT:
            {
                int depth = upE ? 0 : !Ter.Solid(tx, ty - 2) ? 1 : 2;
                var c = depth == 0 ? e.Dirt : depth == 1 ? Col.Lerp(e.Dirt, e.DirtDark, 0.45f) : e.DirtDark;
                Rect(x, y, 8, 8, c);
                var dark = Col.Mul(c, 0.78f);
                Rect(x + (int)(hsh % 6), y + 2 + (int)(hsh >> 4) % 5, 2, 1, dark);
                Rect(x + (int)(hsh >> 8) % 7, y + (int)(hsh >> 12) % 7, 1, 1, Col.Mul(c, 1.15f));
                if (upE)
                {
                    Rect(x, y, 8, 3, e.Top);
                    Rect(x, y, 8, 1, e.TopLight);
                    Rect(x + (int)(hsh % 7), y + 3, 1, 1 + (int)((hsh >> 3) % 2), e.Top);
                    Rect(x + (int)((hsh >> 5) % 7), y + 3, 1, 1, e.Top);
                    // tufos de grama por cima
                    if ((hsh & 3) == 0 && e.Style != BgStyle.Future) Rect(x + 2, y - 2, 1, 2, e.TopLight);
                }
                break;
            }
            case Terrain.BRICK:
            {
                Rect(x, y, 8, 8, e.Brick);
                Rect(x, y + 3, 8, 1, e.BrickDark);
                Rect(x, y + 7, 8, 1, e.BrickDark);
                Rect(x + (ty % 2 == 0 ? 3 : 7), y, 1, 3, e.BrickDark);
                Rect(x + (ty % 2 == 0 ? 6 : 2), y + 4, 1, 3, e.BrickDark);
                Rect(x, y, 8, 1, Col.Mul(e.Brick, 1.2f));
                break;
            }
            case Terrain.STEEL:
            {
                Rect(x, y, 8, 8, e.SteelDark);
                Rect(x + 1, y + 1, 6, 6, e.Steel);
                Rect(x + 1, y + 1, 6, 1, Col.Mul(e.Steel, 1.25f));
                Rect(x + 1, y + 1, 1, 1, e.SteelDark); Rect(x + 6, y + 1, 1, 1, e.SteelDark);
                Rect(x + 1, y + 6, 1, 1, e.SteelDark); Rect(x + 6, y + 6, 1, 1, e.SteelDark);
                break;
            }
            default:
            {
                var wood = Hex(0xb07a3a);
                Rect(x, y, 8, 8, Hex(0x6a4420));
                Rect(x + 1, y + 1, 6, 6, wood);
                for (int i = 1; i < 7; i++) Rect(x + i, y + i, 1, 1, Hex(0x6a4420));
                Rect(x + 1, y + 1, 6, 1, Hex(0xd09a5a));
                break;
            }
        }
        // contorno nos lados expostos
        if (upE && type != Terrain.DIRT) Rect(x, y, 8, 1, Ink);
        if (dnE) Rect(x, y + 7, 8, 1, Ink);
        if (lE) Rect(x, y, 1, 8, Ink);
        if (rE) Rect(x + 7, y, 1, 8, Ink);
        if (upE && type == Terrain.DIRT) Rect(x, y - 1, 8, 1, Ink);
        // rachaduras
        if (dmg > 0)
        {
            Rect(x + 3, y + 2, 1, 3, Ink);
            if (dmg > 1) { Rect(x + 4, y + 4, 2, 1, Ink); Rect(x + 1, y + 5, 2, 1, Ink); }
            if (dmg > 2) { Rect(x + 5, y + 1, 1, 2, Ink); Rect(x + 2, y + 1, 1, 1, Ink); }
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
                case PropKind.Barrel: Barrel(x, y, p.Fuse >= 0 && (int)(time * 30) % 2 == 0); break;
                case PropKind.Cage: Cage(x, y, p.T, p.Char); break;
                case PropKind.Glorb: Glorb(x, y + (int)(MathF.Sin(p.T * 3) * 2), p.T); break;
            }
        }
    }

    Look EnemyLook(Era e, EnemyKind k)
    {
        var l = new Look
        {
            Skin = e.ESkin, Hair = e.EHat, Shirt = e.EShirt, Pants = e.EPants, Boots = Col.Mul(e.EPants, 0.6f),
            Gun = e.EGun, Accent = e.EAccent, Hat = e.EHatStyle, GunLen = 7, GunH = 2,
        };
        if (k == EnemyKind.Rocketeer) { l.GunLen = 9; l.GunH = 3; l.Gun = Col.Mul(e.EAccent, 0.7f); l.Shirt = Col.Mul(e.EShirt, 0.8f); }
        if (k == EnemyKind.Brute) { l.Bulk = 1; l.GunLen = 9; l.GunH = 3; l.Shirt = Col.Lerp(e.EShirt, Red, 0.35f); }
        return l;
    }

    void DrawEnemies()
    {
        foreach (var e in S.Enemies)
        {
            if (e.Dead) continue;
            int x = SX(e.X), y = SY(e.Y);
            if (x < -30 || x > K.W + 30) continue;
            var era = Eras.ForX(e.X);
            Color? over = e.HurtT > 0 ? White : S.FreezeT > 0 ? Hex(0xa8f0ff) : null;
            switch (e.Kind)
            {
                case EnemyKind.Flyer: Flyer(x, y, e.Facing, era, e.AnimT, over); break;
                case EnemyKind.Turret: Turret(x, y, e.Facing, era, e.AnimT, over); break;
                default:
                    Humanoid(x, y, e.Facing, EnemyLook(era, e.Kind), e.AnimT, MathF.Abs(e.VX) > 1, !e.OnGround,
                        e.MuzzleT > 0 ? 1 : 0, 1, over);
                    break;
            }
            if (e.MuzzleT > 0 && e.Kind != EnemyKind.Flyer)
            {
                int gy = e.Kind == EnemyKind.Turret ? y - 8 : y - (e.Kind == EnemyKind.Brute ? 11 : 9);
                Raylib.DrawCircle(x + e.Facing * 13, gy, 3, Yellow);
                Raylib.DrawCircle(x + e.Facing * 13, gy, 1.5f, White);
            }
            if (e.AlertT > 0)
            {
                int by = y - (e.Kind == EnemyKind.Brute ? 34 : 30) - (int)(MathF.Abs(MathF.Sin(e.AlertT * 20)) * 3);
                Box(x - 2, by, 4, 7, Red);
                Rect(x - 1, by + 1, 2, 3, White); Rect(x - 1, by + 5, 2, 1, White);
            }
            if (e.Kind is EnemyKind.Brute or EnemyKind.Turret && e.Hp > 0)
            {
                int max = e.Kind == EnemyKind.Brute ? 8 : 6;
                int top = e.Kind == EnemyKind.Brute ? y - 29 : y - 15;
                Rect(x - 7, top, 14, 2, Ink);
                Rect(x - 7, top, 14 * e.Hp / max, 2, Red);
            }
        }
    }

    void DrawGhosts()
    {
        foreach (var g in ghosts)
        {
            int idx = S.Frame - g.StartFrame;
            if (idx < 0 || idx >= g.Frames.Count) continue;
            var f = g.Frames[idx];
            var look = Chars.All[g.Char].Look;
            float a = 0.45f + 0.15f * MathF.Sin(time * 12 + g.StartFrame);
            int x = SX(f.X), y = SY(f.Y);
            // rastro temporal
            for (int k = 1; k <= 2; k++)
            {
                int pi = idx - k * 4;
                if (pi < 0) break;
                var pf = g.Frames[pi];
                Humanoid(SX(pf.X), SY(pf.Y), pf.Facing, look, (S.Frame - k * 4) * K.DT, MathF.Abs(pf.VX) > 5, !pf.OnGround, 0, 0.12f, Cyan);
            }
            Humanoid(x, y, f.Facing, look, S.Frame * K.DT, MathF.Abs(f.VX) > 5, !f.OnGround, f.Fire ? 2 : 0, a);
            if (f.Fire && g.Char != 4) Raylib.DrawCircle(x + f.Facing * (5 + look.GunLen), y - 9, 2.5f, A(Yellow, 0.7f));
            // indicador de quanto falta para o "eu do passado" morrer
            int left = g.Frames.Count - idx;
            if (g.Died && left < 120 && !g.Resolved && (int)(time * 8) % 2 == 0)
                TextC("!", x, y - 32, 10, Red);
        }
    }

    void DrawPlayer()
    {
        var p = P;
        if (p.Dead) return;
        if (p.InvulnT > 0 && (int)(time * 20) % 2 == 0 && p.DashT <= 0) return;
        var ch = Chars.All[p.Char];
        int x = SX(p.X), y = SY(p.Y);
        bool moving = MathF.Abs(p.VX) > 5;
        Humanoid(x, y, p.Facing, ch.Look, p.AnimT, moving, !p.OnGround && !p.Climbing || p.Climbing && (int)(time * 10) % 2 == 0, p.Recoil);
        if (p.MuzzleT > 0 && p.Char != 4)
        {
            int mx = x + p.Facing * (6 + ch.Look.GunLen), my = y - 9;
            Raylib.DrawCircle(mx, my, 4, A(Yellow, 0.9f));
            Raylib.DrawCircle(mx, my, 2, White);
            Rect(mx - (p.Facing < 0 ? 6 : 0), my, 6, 1, White);
        }
        if (p.Shield > 0)
        {
            Raylib.DrawCircleLines(x, y - 9, 12 + MathF.Sin(time * 8), A(Cyan, 0.8f));
            Raylib.DrawCircleLines(x, y - 9, 13 + MathF.Sin(time * 8), A(Cyan, 0.35f));
        }
        // seta indicando o jogador nos primeiros segundos
        if (p.InvulnT > 0.5f) TextC("VOCE", x, y - 34 - (int)(MathF.Sin(time * 8) * 2), 10, Yellow);
    }

    void DrawBullets()
    {
        foreach (var b in S.Bullets)
        {
            if (b.Dead) continue;
            int x = SX(b.X), y = SY(b.Y);
            int d = b.VX >= 0 ? 1 : -1;
            float a = b.Ghost ? 0.6f : 1f;
            switch (b.Kind)
            {
                case BulletKind.Bullet:
                    Rect(x - 3, y - 1, 6, 2, A(Yellow, a)); Rect(x - (d > 0 ? 0 : 2), y - 1, 3, 1, A(White, a));
                    Rect(x - 3 - d * 4, y, 4, 1, A(Orange, a * 0.6f));
                    break;
                case BulletKind.Pellet: Rect(x - 1, y - 1, 2, 2, A(Yellow, a)); break;
                case BulletKind.Laser:
                    Rect(x - 10, y - 2, 20, 4, A(Cyan, a * 0.5f));
                    Rect(x - 9, y - 1, 18, 2, A(White, a));
                    break;
                case BulletKind.Rocket:
                case BulletKind.ERocket:
                    var body = b.Kind == BulletKind.Rocket ? Hex(0x55702f) : Hex(0x8a8a9a);
                    Box(x - 3, y - 1, 6, 3, A(body, a));
                    Rect(x + d * 3 - (d < 0 ? 1 : 0), y - 1, 1, 3, Red);
                    Raylib.DrawCircle(x - d * 5, y, 2 + (int)(time * 30) % 2, Orange);
                    break;
                case BulletKind.Grenade:
                    Raylib.DrawCircle(x, y, 3, Ink); Raylib.DrawCircle(x, y, 2, Hex(0x4a6a2a));
                    if ((int)(b.T * 10) % 2 == 0) Rect(x, y - 3, 1, 1, Red);
                    break;
                case BulletKind.Dynamite:
                    Box(x - 1, y - 3, 3, 6, Red); Rect(x, y - 5, 1, 2, Hex(0xe8e0c0));
                    break;
                case BulletKind.Slash:
                {
                    float t = 1 - b.Life / (b.W > 40 ? 0.14f : 0.1f);
                    int w = (int)(b.W / 2);
                    for (int i = -3; i <= 3; i++)
                    {
                        int yy = y + i * 3;
                        int len = w - Math.Abs(i) * 2;
                        Rect(x - len, yy, len * 2, 1, A(i == 0 ? White : Hex(0xdfe8f0), (1 - t) * a));
                    }
                    break;
                }
                case BulletKind.EBullet:
                {
                    var c = b.FromPlayer ? Yellow : Eras.ForX(b.X).EBullet;
                    Raylib.DrawCircle(x, y, 3, Ink);
                    Raylib.DrawCircle(x, y, 2, c);
                    Rect(x - 1, y - 1, 1, 1, White);
                    break;
                }
                case BulletKind.EBomb:
                    Raylib.DrawCircle(x, y, 3.5f, Ink); Raylib.DrawCircle(x, y, 2.5f, Hex(0x3a3a3a));
                    Rect(x, y - 5, 1, 2, (int)(time * 20) % 2 == 0 ? Yellow : Orange);
                    break;
            }
        }
    }

    void DrawExplosions()
    {
        foreach (var e in S.Explosions)
        {
            float t = e.T / e.Dur;
            int x = SX(e.X), y = SY(e.Y);
            float r = e.R * (0.55f + 0.6f * MathF.Sqrt(t));
            if (t < 0.12f) { Raylib.DrawCircle(x, y, e.R * 1.1f, White); continue; }
            var outer = t < 0.5f ? Hex(0xd8401e) : Col.Lerp(Hex(0xd8401e), Hex(0x3a2a2a), (t - 0.5f) * 2);
            Raylib.DrawCircle(x, y, r, outer);
            if (t < 0.75f)
            {
                Raylib.DrawCircle(x + (int)(r * 0.15f), y - (int)(r * 0.2f), r * 0.72f * (1 - t * 0.6f), Orange);
                Raylib.DrawCircle(x - (int)(r * 0.1f), y - (int)(r * 0.1f), r * 0.45f * (1 - t), Yellow);
            }
            if (t < 0.35f) Raylib.DrawCircle(x, y, r * 0.25f, White);
        }
    }

    void DrawParticles()
    {
        foreach (var p in parts)
        {
            float t = p.Life / p.Max;
            int x = SX(p.X), y = SY(p.Y);
            switch (p.Kind)
            {
                case PKind.Pixel:
                case PKind.Debris:
                    int s = (int)MathF.Max(1, p.Size);
                    Rect(x, y, s, s, p.Kind == PKind.Debris && t < 0.3f ? A(p.C, t / 0.3f) : p.C);
                    break;
                case PKind.Smoke:
                    Raylib.DrawCircle(x, y, p.Size * (1.6f - t * 0.6f), A(p.C, t * 0.75f));
                    break;
                case PKind.Spark:
                    Gfx.Line(x, y, x - p.VX * 0.02f, y - p.VY * 0.02f, A(p.C, t));
                    break;
                case PKind.Flash:
                    Raylib.DrawCircle(x, y, p.Size * t, p.C);
                    break;
                case PKind.Ring:
                    Raylib.DrawCircleLines(x, y, p.Size * (1 - t), A(p.C, t));
                    Raylib.DrawCircleLines(x, y, p.Size * (1 - t) + 1, A(p.C, t * 0.5f));
                    break;
            }
        }
        foreach (var t in texts)
        {
            float a = MathF.Min(1, t.Life * 2);
            Text(t.Text, SX(t.X) - Raylib.MeasureText(t.Text, 10) / 2, SY(t.Y), 10, A(t.C, a), A(Ink, a));
        }
    }

    // ------------------------------------------------------------------ HUD e telas

    void DrawHud()
    {
        var ch = Chars.All[P.Char];
        // retrato
        Box(4, 4, 20, 20, Hex(0x2a2040));
        Raylib.BeginScissorMode(4, 4, 20, 20);
        Humanoid(14, 34, 1, ch.Look, 0, false, false);
        Raylib.EndScissorMode();
        Text(ch.Name, 28, 4, 10, White);
        // especiais
        for (int i = 0; i < P.Specials; i++) Box(28 + i * 6, 16, 4, 6, Orange);
        // escudo
        for (int i = 0; i < P.Shield; i++) Raylib.DrawCircleLines(31 + (P.Specials) * 6 + i * 8, 19, 3, Cyan);

        // time outs (ampulhetas)
        int tx = K.W / 2 - 18;
        Text("TIME OUT", tx - 6, 4, 10, Cyan);
        for (int i = 0; i < Math.Min(timeOuts, 8); i++) Hourglass(tx + 50 + i * 7, 5);
        if (timeOuts > 8) Text("+" + (timeOuts - 8), tx + 50 + 8 * 7, 4, 10, Cyan);
        // glorbs
        Rect(tx - 6, 16, 60, 3, Ink);
        Rect(tx - 6, 16, 60 * glorbCount / 12, 3, Magenta);

        // distancia/pontos
        string m = Meters + " M";
        Text(m, K.W - 4 - Raylib.MeasureText(m, 10), 4, 10, Yellow);
        string sc = (FinalScore()).ToString("N0").Replace(',', '.');
        Text(sc, K.W - 4 - Raylib.MeasureText(sc, 10), 15, 10, White);

        // banner de era
        if (eraBannerT > 0)
        {
            float a = MathF.Min(1, eraBannerT);
            int slide = (int)(MathF.Max(0, eraBannerT - 2.5f) * 200);
            Rect(0, 52, K.W, 34, A(Ink, 0.55f * a));
            TextC(eraBanner, K.W / 2 + slide, 56, 20, A(Yellow, a), A(Ink, a));
            TextC(eraSub, K.W / 2 - slide, 76, 10, A(Cyan, a), A(Ink, a));
        }
    }

    static void Hourglass(int x, int y)
    {
        Rect(x, y, 5, 1, Ink); Rect(x, y + 7, 5, 1, Ink);
        Rect(x + 1, y + 1, 3, 2, Cyan); Rect(x + 2, y + 3, 1, 2, Cyan); Rect(x + 1, y + 5, 3, 2, Hex(0x2a8aa8));
    }

    void DrawDying()
    {
        float a = MathF.Min(1, modeT * 3);
        Rect(0, 0, K.W, K.H, A(Red, 0.15f * a));
        int y = 64 - (int)(MathF.Max(0, 0.3f - modeT) * 100);
        TextC(deathMsg, K.W / 2, y, 20, A(White, a), A(Ink, a));
        TextC(timeOuts > 0 ? "VOLTANDO NO TEMPO..." : "SEM TIME OUTS!", K.W / 2, y + 24, 10, A(timeOuts > 0 ? Cyan : Red, a), A(Ink, a));
    }

    void DrawRewind()
    {
        // efeito VHS: tinta azulada, linhas de varredura e glitch
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

        // linha do tempo
        int bw = K.W - 40;
        Rect(20, K.H - 14, bw, 4, Ink);
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
            Box(x - 24, by, 48, 56, sel ? Hex(0x3a2a6a) : Hex(0x221a36));
            if (sel) Raylib.DrawRectangleLines(x - 25, by - 1, 50, 58, Yellow);
            Humanoid(x, by + 46, 1, c.Look, time, sel, false, 0, sel ? 1 : 0.7f);
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
        int bob = (int)(MathF.Sin(titleT * 3) * 2);
        // titulo com sombra colorida deslocada (efeito de distorcao temporal)
        int gl = (int)(titleT * 10) % 23 == 0 ? 2 : 0;
        TextC("JACKASS RUN", K.W / 2 + 2 + gl, 22 + bob, 30, Magenta, Magenta);
        TextC("JACKASS RUN", K.W / 2 - 2 - gl, 22 + bob, 30, Cyan, Cyan);
        TextC("JACKASS RUN", K.W / 2, 22 + bob, 30, Yellow);
        TextC("TIME FORCE BROS", K.W / 2, 54, 10, White);

        // herois desfilando
        for (int i = 0; i < Chars.All.Length; i++)
        {
            int x = K.W / 2 - 80 + i * 40;
            Humanoid(x, 98, 1, Chars.All[i].Look, titleT + i * 0.3f, true, false);
        }
        if ((int)(titleT * 2) % 2 == 0) TextC("APERTE ENTER PARA COMECAR", K.W / 2, 108, 10, Yellow);
        ControlsText(122);
        if (best > 0) Text("RECORDE: " + best.ToString("N0").Replace(',', '.'), 4, K.H - 12, 10, Cyan);
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
