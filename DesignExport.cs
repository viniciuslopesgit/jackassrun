using Raylib_cs;
using static JackassRun.Col;
using static JackassRun.Gfx;

namespace JackassRun;

/// <summary>Gera a arte padrao do jogo como PNGs editaveis na pasta design/.
/// So cria os arquivos que estiverem faltando: apague um PNG para recuperar o original.</summary>
public static class DesignExport
{
    public static readonly string[] EraSlug = { "selva", "jurassico", "medieval", "futuro" };
    public static readonly string[] HeroSlug = { "jean_rockfire", "shotgun_sheila", "doc_chrono", "blastronauta", "naomi_katana" };
    public static readonly string[] GruntSlug = { "soldado", "bazuqueiro", "brutamontes" };

    // Folha de personagem: celulas de 32x26, pes no pixel (14, 24) de cada celula.
    public const int CW = 32, CH = 26, AX = 14, AY = 24, Cols = 6, Rows = 5;
    const int BgLoop = 960;          // largura das camadas de cenario (repetem sem emenda)
    const int BgSeed = 4242;

    public static string HeroPath(string root, int i) => Path.Combine(root, "sprites", "herois", HeroSlug[i] + ".png");
    public static string GruntPath(string root, int era, int kind) => Path.Combine(root, "sprites", "inimigos", $"{EraSlug[era]}_{GruntSlug[kind]}.png");
    public static string FlyerPath(string root, int era) => Path.Combine(root, "sprites", "inimigos", $"{EraSlug[era]}_voador.png");
    public static string TurretPath(string root, int era) => Path.Combine(root, "sprites", "inimigos", $"{EraSlug[era]}_torreta.png");
    public static string PropPath(string root, string name) => Path.Combine(root, "sprites", "objetos", name + ".png");
    public static string TilesPath(string root, int era) => Path.Combine(root, "tiles", EraSlug[era] + ".png");
    public static string BgPath(string root, int era, string layer) => Path.Combine(root, "cenarios", EraSlug[era], layer + ".png");

    public static Look EnemyLook(Era e, EnemyKind k)
    {
        var l = new Look
        {
            Skin = e.ESkin, Hair = e.EHat, Shirt = e.EShirt, Pants = e.EPants, Boots = Mul(e.EPants, 0.6f),
            Gun = e.EGun, Accent = e.EAccent, Hat = e.EHatStyle, GunLen = 7, GunH = 2, Wpn = 1,
        };
        if (k == EnemyKind.Rocketeer) { l.Wpn = 4; l.GunLen = 9; l.GunH = 3; l.Gun = Mul(e.EAccent, 0.7f); l.Shirt = Mul(e.EShirt, 0.8f); }
        if (k == EnemyKind.Brute) { l.Wpn = 6; l.Bulk = 1; l.GunLen = 9; l.GunH = 3; l.Shirt = Col.Lerp(e.EShirt, Red, 0.35f); }
        return l;
    }

    /// <summary>Cria todos os arquivos que faltam. Retorna quantos foram gerados.</summary>
    public static int EnsureAll(string root)
    {
        int n = 0;
        void Need(string path, int w, int h, Action draw)
        {
            if (File.Exists(path)) return;
            Save(path, w, h, draw);
            n++;
        }

        for (int i = 0; i < Chars.All.Length; i++)
        {
            var look = Chars.All[i].Look;
            Need(HeroPath(root, i), CW * Cols, CH * Rows, () => CharSheet(look));
        }
        for (int e = 0; e < Eras.All.Length; e++)
        {
            var era = Eras.All[e];
            for (int k = 0; k < 3; k++)
            {
                var look = EnemyLook(era, (EnemyKind)k);
                Need(GruntPath(root, e, k), CW * Cols, CH * Rows, () => CharSheet(look));
            }
            Need(FlyerPath(root, e), 32 * 4, 24, () => { for (int f = 0; f < 4; f++) Flyer(16 + f * 32, 12, 1, era, f / 14f + 0.001f, null); });
            Need(TurretPath(root, e), 32 * 4, 16, () =>
            {
                Turret(12, 15, 1, era, 0f, 0, null); Turret(44, 15, 1, era, 0.3f, 0, null);
                Turret(76, 15, 1, era, 0f, 2, null); Turret(108, 15, 1, era, 0.3f, 2, null);
            });
            Need(TilesPath(root, e), 64, 40, () => Tiles(era, e));
            Need(BgPath(root, e, "ceu"), K.W, K.H, () => Sky(era));
            Need(BgPath(root, e, "fundo"), BgLoop, K.H, () => FarLayer(era));
            Need(BgPath(root, e, "meio"), BgLoop, K.H, () => MidLayer(era));
        }
        Need(PropPath(root, "barril"), 32, 16, () => { Barrel(8, 15, false, 0); Barrel(24, 15, true, 0); });
        Need(PropPath(root, "glorb"), 64, 16, () => { for (int f = 0; f < 4; f++) Glorb(8 + f * 16, 8, f * 0.26f); });
        Need(PropPath(root, "jaula"), 64, 48, () => { CageBars(16, 47, 0f); CageBars(48, 47, 0.13f); });
        Need(PropPath(root, "projeteis"), 32 * 9, 32, () => { for (int k = 0; k < 9; k++) { Projectile(k, 16 + k * 32, 8, 0f); Projectile(k, 16 + k * 32, 24, 0.05f); } });

        string readme = Path.Combine(root, "LEIAME.md");
        if (!File.Exists(readme)) { File.WriteAllText(readme, Readme); n++; }
        return n;
    }

    static void Save(string path, int w, int h, Action draw)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var rt = Raylib.LoadRenderTexture(w, h);
        Raylib.BeginTextureMode(rt);
        Raylib.ClearBackground(new Color(0, 0, 0, 0));
        draw();
        Raylib.EndTextureMode();
        var img = Raylib.LoadImageFromTexture(rt.Texture);
        Raylib.ImageFlipVertical(ref img);
        Raylib.ExportImage(img, path);
        Raylib.UnloadImage(img);
        Raylib.UnloadRenderTexture(rt);
    }

    // ------------------------------------------------------------------ personagens

    static void CharSheet(Look l)
    {
        void F(int col, int row, Anim a) => Humanoid(col * CW + AX, row * CH + AY, 1, l, a);
        F(0, 0, Anim.Of(AState.Idle, 1.0f));
        F(1, 0, Anim.Of(AState.Idle, 0.5f));
        F(2, 0, Anim.Of(AState.Idle, 1.0f));
        F(3, 0, Anim.Of(AState.Idle, 3.42f));
        for (int f = 0; f < 6; f++) F(f, 1, new Anim { S = AState.Run, T = f / 14f + 0.001f, Speed = 88 });
        F(0, 2, Anim.Of(AState.Jump, 0));
        F(1, 2, Anim.Of(AState.Fall, 0));
        F(2, 2, Anim.Of(AState.Fall, 0.1f));
        F(3, 2, new Anim { S = AState.Idle, T = 1f, Land = 0.1f });
        F(0, 3, Anim.Of(AState.Climb, 0));
        F(1, 3, Anim.Of(AState.Climb, 0.12f));
        F(2, 3, Anim.Of(AState.Dash, 0));
        F(3, 3, Anim.Of(AState.Hurt, 0));
        F(0, 4, Anim.Of(AState.Cheer, 0));
        F(1, 4, Anim.Of(AState.Cheer, 0.17f));
        F(2, 4, Anim.Of(AState.Tumble, 0));
        F(3, 4, Anim.Of(AState.Tumble, 0.08f));
    }

    // ------------------------------------------------------------------ objetos

    static void CageBars(int cx, int fy, float t)
    {
        var frame = Hex(0x6a5844);
        Rect(cx - 9, fy - 28, 18, 2, frame); Rect(cx - 9, fy - 28, 18, 1, Mul(frame, 1.3f));
        Rect(cx - 9, fy - 2, 18, 2, frame); Rect(cx - 9, fy - 2, 18, 1, Mul(frame, 1.3f));
        for (int i = 0; i < 6; i++) Rect(cx - 8 + i * 3, fy - 26, 1, 24, Hex(0x9a9aa8));
        Rect(cx - 8, fy - 26, 1, 24, Hex(0xd0d0dc));
        Rect(cx + 8, fy - 38, 1, 10, Mul(frame, 0.8f));
        int fl = (int)(t * 8) % 3;
        Rect(cx + 9, fy - 38, 5 + (fl == 1 ? 1 : 0), 2, Red);
        Rect(cx + 9, fy - 36, 4 + fl, 2, Mul(Red, 0.8f));
    }

    /// <summary>Projeteis apontando para a direita, centrados em (x,y). Ordem = Art.Proj*.</summary>
    static void Projectile(int k, int x, int y, float time)
    {
        switch (k)
        {
            case 0: // bala
                Rect(x - 8, y, 5, 1, A(Orange, 0.5f));
                Rect(x - 3, y - 1, 6, 2, Yellow);
                Rect(x, y - 1, 3, 1, White);
                break;
            case 1: // chumbo
                Rect(x - 3, y, 2, 1, A(Orange, 0.6f));
                Rect(x - 1, y - 1, 2, 2, Yellow);
                break;
            case 2: // laser
                Rect(x - 12, y - 2, 24, 4, A(Cyan, 0.55f));
                Rect(x - 11, y - 1, 22, 2, White);
                break;
            case 3: // foguete do jogador
            case 4: // foguete inimigo
            {
                var body = k == 3 ? Hex(0x55702f) : Hex(0x8a8a9a);
                Rect(x - 3, y - 1, 6, 3, body); Rect(x - 3, y - 1, 6, 1, Mul(body, 1.3f));
                Rect(x + 3, y - 1, 1, 3, Red);
                int fl = time > 0 ? 2 : 0;
                Rect(x - 5 - fl, y, 2 + fl, 1, Yellow);
                PixelCircle(x - 5, y, 1 + (fl > 0 ? 1 : 0), Orange);
                break;
            }
            case 5: // granada
                PixelCircle(x, y, 3, Hex(0x34501e)); PixelCircle(x, y, 2, Hex(0x4a6a2a));
                Rect(x - 1, y - 2, 1, 1, Hex(0x8aaa5a));
                if (time == 0) Rect(x, y - 4, 1, 1, Red);
                break;
            case 6: // dinamite
                Rect(x - 1, y - 3, 3, 6, Red); Rect(x - 1, y - 3, 1, 6, Mul(Red, 1.3f)); Rect(x, y - 5, 1, 2, Hex(0xe8e0c0));
                Rect(x, y - 6, 1, 1, time == 0 ? Yellow : Orange);
                break;
            case 7: // bala inimiga (branca: o jogo pinta com a cor da era)
                PixelCircle(x, y, 3, Hex(0xa8a8a8));
                PixelCircle(x, y, 2, White);
                Rect(x - 1, y - 1, 1 + (time > 0 ? 1 : 0), 1, White);
                break;
            case 8: // bomba
                PixelCircle(x, y, 4, Hex(0x2c2c32)); PixelCircle(x, y, 3, Hex(0x45454e));
                Rect(x - 1, y - 2, 1, 1, Hex(0x7a7a7a));
                Rect(x, y - 6, 1, 2, time == 0 ? Yellow : Orange);
                break;
        }
    }

    // ------------------------------------------------------------------ tiles

    /// <summary>Folha 8x5 de tiles 8x8 (ver LEIAME).</summary>
    static void Tiles(Era e, int eraIdx)
    {
        for (int v = 0; v < 4; v++)
            for (int depth = 0; depth < 3; depth++)
                Dirt(v * 8, depth * 8, e, depth, Hash.H(v * 13 + 5, depth + 3));
        Brick(0, 24, e, 0); Brick(8, 24, e, 1);
        Steel(16, 24, e); Crate(24, 24);
        for (int d = 1; d <= 3; d++) Cracks(24 + d * 8, 24, d);
        if (e.Style != BgStyle.Future)
            for (int sway = 0; sway < 2; sway++)
            {
                int bx = sway * 8, by = 32;
                Rect(bx + 2 + sway, by + 5, 1, 2, e.TopLight); Rect(bx + 2, by + 7, 1, 1, e.Top);
                bx = 16 + sway * 8;
                Rect(bx + 5 + sway, by + 6, 1, 1, e.TopLight); Rect(bx + 5, by + 7, 2, 1, e.Top);
                bx = 32 + sway * 8;
                var flower = e.Style switch { BgStyle.Jungle => Red, BgStyle.Dino => Orange, _ => Magenta };
                Rect(bx + 4, by + 5, 1, 3, e.Top);
                Rect(bx + 3 + sway, by + 3, 3, 2, flower);
                Rect(bx + 4 + sway, by + 3, 1, 1, Yellow);
            }
    }

    static void Dirt(int x, int y, Era e, int depth, uint hsh)
    {
        var c = depth == 0 ? e.Dirt : depth == 1 ? Col.Lerp(e.Dirt, e.DirtDark, 0.45f) : e.DirtDark;
        Rect(x, y, 8, 8, c);
        var dark = Mul(c, 0.78f);
        Rect(x + (int)(hsh % 6), y + 2 + (int)(hsh >> 4) % 5, 2, 1, dark);
        Rect(x + (int)(hsh >> 8) % 7, y + (int)(hsh >> 12) % 7, 1, 1, Mul(c, 1.15f));
        if (depth == 2) Rect(x + 2, y + 3, 3, 2, Mul(c, 0.85f));
        if (depth == 0)
        {
            Rect(x, y, 8, 3, e.Top);
            Rect(x, y, 8, 1, e.TopLight);
            Rect(x + (int)(hsh % 7), y + 3, 1, 1 + (int)((hsh >> 3) % 2), e.Top);
            Rect(x + (int)((hsh >> 5) % 7), y + 3, 1, 1, e.Top);
        }
    }

    static void Brick(int x, int y, Era e, int odd)
    {
        Rect(x, y, 8, 8, e.Brick);
        Rect(x, y + 3, 8, 1, e.BrickDark);
        Rect(x, y + 7, 8, 1, e.BrickDark);
        Rect(x + (odd == 0 ? 3 : 7), y, 1, 3, e.BrickDark);
        Rect(x + (odd == 0 ? 6 : 2), y + 4, 1, 3, e.BrickDark);
        Rect(x, y, 8, 1, Mul(e.Brick, 1.2f));
        Rect(x, y + 4, 8, 1, Mul(e.Brick, 1.1f));
    }

    static void Steel(int x, int y, Era e)
    {
        Rect(x, y, 8, 8, e.SteelDark);
        Rect(x + 1, y + 1, 6, 6, e.Steel);
        Rect(x + 1, y + 1, 6, 1, Mul(e.Steel, 1.25f));
        Rect(x + 2, y + 2, 1, 1, Mul(e.Steel, 1.4f));
        Rect(x + 1, y + 1, 1, 1, e.SteelDark); Rect(x + 6, y + 1, 1, 1, e.SteelDark);
        Rect(x + 1, y + 6, 1, 1, e.SteelDark); Rect(x + 6, y + 6, 1, 1, e.SteelDark);
    }

    static void Crate(int x, int y)
    {
        Rect(x, y, 8, 8, Hex(0x6a4420));
        Rect(x + 1, y + 1, 6, 6, Hex(0xb07a3a));
        for (int i = 1; i < 7; i++) Rect(x + i, y + i, 1, 1, Hex(0x6a4420));
        Rect(x + 1, y + 1, 6, 1, Hex(0xd09a5a));
    }

    static void Cracks(int x, int y, int dmg)
    {
        var c = A(Hex(0x000000), 0.6f);   // fica como uma sombra da cor do proprio tile
        Rect(x + 3, y + 2, 1, 3, c);
        if (dmg > 1) { Rect(x + 4, y + 4, 2, 1, c); Rect(x + 1, y + 5, 2, 1, c); }
        if (dmg > 2) { Rect(x + 5, y + 1, 1, 2, c); Rect(x + 2, y + 1, 1, 1, c); }
    }

    // ------------------------------------------------------------------ cenarios

    static void Sky(Era e)
    {
        Raylib.DrawRectangleGradientV(0, 0, K.W, K.H, e.SkyTop, e.SkyBot);
        int sunX = 230, sunY = e.Style == BgStyle.Future ? 70 : 40;
        int sr = e.Style == BgStyle.Future ? 34 : 16;
        var skyAt = Col.Lerp(e.SkyTop, e.SkyBot, sunY / (float)K.H);    // cor do ceu na altura do sol
        PixelCircle(sunX, sunY, sr + 8, Col.Lerp(skyAt, e.Sun, 0.15f));
        PixelCircle(sunX, sunY, sr + 4, Col.Lerp(skyAt, e.Sun, 0.32f));
        PixelCircle(sunX, sunY, sr, e.Sun);
        if (e.Style == BgStyle.Future)
            for (int i = 0; i < 6; i++) Rect(sunX - sr - 1, sunY + 4 + i * 5, sr * 2 + 3, 1 + i / 2, e.SkyBot);
        if (e.Style == BgStyle.Medieval) PixelCircle(sunX + 5, sunY - 3, sr - 2, skyAt);
    }

    /// <summary>Funcao de altura que fecha o ciclo: os ultimos 96px se misturam com o comeco.</summary>
    static int Loop(Func<float, float> f, int x)
    {
        const int B = 96;
        float v = f(x);
        if (x >= BgLoop - B) { float t = (x - (BgLoop - B)) / (float)B; v = v * (1 - t) + f(x - BgLoop) * t; }
        return (int)v;
    }

    static int Volcano(int x)
    {
        int local = ((x % 192) + 192) % 192;
        int h = (int)MathF.Max(30, 100 - MathF.Abs(local - 96) * 1.05f);
        return Math.Min(h, 92) + (int)(Hash.Noise(local / 10f, 11) * 4);
    }

    static int Skyline(int x, int width, int min, int amp, int seed, out int b)
    {
        int n = BgLoop / width;
        b = ((x / width) % n + n) % n;
        int local = x - (x / width) * width;
        if (local < 3) return min / 2;
        int h = min + (int)(Hash.F(b, seed) * amp);
        if (Hash.F(b, seed + 9) > 0.7f && Math.Abs(local - width / 2) < 2) h += 14;
        return h;
    }

    static int Castle(int x, int seed)
    {
        int b = ((x / 48) % (BgLoop / 48));
        int local = x % 48;
        int h = 26 + (int)(Hash.F(b, seed) * 14);
        if (local > 15 && local < 27) { h += 20; if (local > 16 && local < 26) h += (int)(8 - MathF.Abs(local - 21) * 1.6f) + 6; }
        else if ((local / 3) % 2 == 0) h += 3;
        return h;
    }

    static void FarLayer(Era e)
    {
        var edge = Col.Lerp(e.Far, e.SkyBot, 0.35f);
        for (int x = 0; x < BgLoop; x++)
        {
            int h = e.Style switch
            {
                BgStyle.Dino => Volcano(x),
                BgStyle.Future => Skyline(x, 24, 60, 50, BgSeed + 1, out _),
                _ => Loop(v => 40 + Hash.Fbm(v / 70f, BgSeed + 1) * 60, x),
            };
            Rect(x, K.H - h, 1, h, e.Far);
            Rect(x, K.H - h, 1, 1, edge);
            if (e.Style == BgStyle.Dino && h >= 88) Rect(x, K.H - h, 1, 3, Orange);
        }
        if (e.Style == BgStyle.Future) Windows(24, 60, 50, BgSeed + 1, A(e.Sun, 0.55f), e.Far);
    }

    static void MidLayer(Era e)
    {
        var edge = Col.Lerp(e.Mid, e.SkyBot, 0.3f);
        for (int x = 0; x < BgLoop; x++)
        {
            int h = e.Style switch
            {
                BgStyle.Jungle => Loop(v => 30 + Hash.Fbm(v / 30f, BgSeed + 2) * 30 + MathF.Abs(MathF.Sin(v / 9f)) * 6, x),
                BgStyle.Dino => Loop(v => 26 + Hash.Fbm(v / 26f, BgSeed + 3) * 22, x) + ((x / 3) % 5 == 0 ? 4 : 0),
                BgStyle.Medieval => Castle(x, BgSeed + 4),
                _ => Skyline(x, 30, 40, 40, BgSeed + 5, out _),
            };
            Rect(x, K.H - h, 1, h, e.Mid);
            Rect(x, K.H - h, 1, 1, edge);
        }
        if (e.Style == BgStyle.Future) Windows(30, 40, 40, BgSeed + 5, A(Cyan, 0.7f), e.Mid);
        if (e.Style == BgStyle.Medieval)
            for (int b = 0; b < BgLoop / 48; b++)
            {
                int h = 26 + (int)(Hash.F(b, BgSeed + 4) * 14) + 20;
                Rect(b * 48 + 20, K.H - h + 8, 2, 3, Yellow);
            }
    }

    static void Windows(int width, int min, int amp, int seed, Color c, Color wall)
    {
        for (int b = 0; b < BgLoop / width; b++)
        {
            int hgt = min + (int)(Hash.F(b, seed) * amp);
            int x0 = b * width, col = 0;
            for (int wx = x0 + 4; wx < x0 + width - 3; wx += 4, col++)
            {
                int row = 0;
                for (int wy = K.H - hgt + 4; wy < K.H - 4; wy += 5, row++)
                    if (Hash.F(b * 97 + col, row + seed) > 0.6f) Rect(wx, wy, 2, 2, Col.Lerp(wall, c, c.A / 255f) with { A = 255 });
            }
        }
    }

    const string Readme = """
# Pasta de arte do Jackass Run

Tudo aqui e PNG comum: edite no Aseprite, Photoshop, Krita, Piskel, LibreSprite...
- O jogo le esta pasta ao abrir. Com o jogo aberto, aperte **F5** para recarregar a arte.
- Apague um arquivo para o jogo gerar de novo a versao original.
- Mantenha o tamanho das imagens e das celulas. Fundo transparente = vazio.
- Desenhe tudo **olhando para a direita**: o jogo espelha sozinho.

## sprites/herois e sprites/inimigos (soldado, bazuqueiro, brutamontes)
Folha de 6 colunas x 5 linhas, celulas de **32x26**. Os pes ficam no pixel **(14, 24)** de cada celula
(centro do corpo, chao logo abaixo). Corpos girados ao morrer giram em torno de (14, 15).

| Linha | Colunas |
|---|---|
| 0 Parado | 0 normal, 1 respirando, 2 normal (variacao), 3 piscando |
| 1 Correndo | 0 a 5 (ciclo de corrida) |
| 2 No ar | 0 pulo, 1 queda A, 2 queda B, 3 aterrissagem |
| 3 Acoes | 0 escalada A, 1 escalada B, 2 dash, 3 levando dano / morto |
| 4 Extras | 0 comemorando A, 1 comemorando B, 2 voando morto A, 3 voando morto B |

Os tiros saem da ponta da arma: metralhadora/escopeta/laser/fuzil a ~(+11, -7) dos pes, bazuca a (+11, -11).
Os prisioneiros nas jaulas usam a folha do heroi correspondente.

## sprites/inimigos/*_voador.png
4 quadros de 32x24 (bater de asas), centro do corpo em (16, 12).

## sprites/inimigos/*_torreta.png
4 quadros de 32x16, base no pixel (12, 15): 0 luz acesa, 1 luz apagada, 2 e 3 = mesmo com recuo do tiro.

## sprites/objetos
- **barril.png**: 2 quadros 16x16, base em (8, 15): normal e piscando (prestes a explodir).
- **glorb.png**: 4 quadros 16x16, centro (8, 8).
- **jaula.png**: 2 quadros 32x48, base em (16, 47) (bandeira balancando). O prisioneiro e desenhado atras.
- **projeteis.png**: celulas 32x16, centro (16, 8), apontando para a direita. Linha 0 e 1 = quadros A e B.
  Colunas: 0 bala, 1 chumbo, 2 laser, 3 foguete, 4 foguete inimigo, 5 granada, 6 dinamite,
  7 bala inimiga (pinte de branco/cinza: o jogo aplica a cor da era), 8 bomba.

## tiles/<era>.png
Folha de 8x5 tiles de **8x8**:
- Linha 0: terra com grama (topo exposto), 4 variacoes
- Linha 1: terra logo abaixo do topo, 4 variacoes
- Linha 2: terra profunda, 4 variacoes
- Linha 3: 0 tijolo (linha par), 1 tijolo (linha impar), 2 aco, 3 caixote, 4-6 rachaduras (camada por cima, 1 a 3 de dano)
- Linha 4: enfeites desenhados **em cima** do tile de grama: 0-1 tufo A (2 quadros de vento), 2-3 tufo B, 4-5 flor

Nada tem contorno preto: as formas sao definidas so pelas cores e pelo sombreamento.

## cenarios/<era>/
- **ceu.png** (320x180): fundo fixo com sol/lua.
- **fundo.png** e **meio.png** (960x180, transparentes): camadas de parallax que se repetem
  na horizontal. A borda direita deve encaixar na esquerda.

Efeitos (fogo, fumaca, sangue, faiscas, brilhos, estrelas) sao gerados pelo codigo.
""";
}
