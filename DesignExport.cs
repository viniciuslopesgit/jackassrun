using Raylib_cs;
using static JackassRun.Col;
using static JackassRun.Gfx;

namespace JackassRun;

/// <summary>Gera a arte padrao do jogo como PNGs editaveis na pasta design/.
/// So cria os arquivos que estiverem faltando: apague um PNG para recuperar o original.</summary>
public static class DesignExport
{
    public static readonly string[] EraSlug = { "selva", "jurassico", "medieval", "futuro" };
    public static readonly string[] HeroSlug = { "batman", "tomb_raider", "homem_aranha" };
    public static readonly string[] GruntSlug = { "soldado", "bazuqueiro", "brutamontes", "faca", "homem_bomba" };

    /// <summary>Indice da folha de cada tipo de inimigo a pe (soldado, bazuqueiro, brutamontes, faca, homem-bomba).</summary>
    public static int GruntIndex(EnemyKind k) => k switch
    {
        EnemyKind.Rocketeer => 1, EnemyKind.Brute => 2, EnemyKind.Knife => 3, EnemyKind.Bomber => 4, _ => 0,
    };

    /// <summary>Refem: civil de camisa branca e gravata, sem arma.</summary>
    public static readonly Look HostageLook = new()
    {
        Skin = Hex(0xe8b890), Hair = Hex(0x4a3020), Shirt = Hex(0xe8e8ee), Pants = Hex(0x3a4058), Boots = Hex(0x2a1e18),
        Gun = Hex(0x404040), Accent = Hex(0xc02030), Hat = 9, Wpn = 7,
    };

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
        if (k == EnemyKind.Knife) { l.Wpn = 8; l.Gun = Hex(0xb0b8c0); l.Shirt = Mul(e.EShirt, 0.65f); }
        // homem-bomba: colete de dinamite vermelho e detonador na mao
        if (k == EnemyKind.Bomber) { l.Wpn = 9; l.Gun = Hex(0x505058); l.Accent = Red; l.Shirt = Hex(0xb8281c); }
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
            for (int k = 0; k < GruntSlug.Length; k++)
            {
                var look = EnemyLook(era, k switch { 3 => EnemyKind.Knife, 4 => EnemyKind.Bomber, _ => (EnemyKind)k });
                Need(GruntPath(root, e, k), CW * Cols, CH * Rows, () => CharSheet(look));
            }
            Need(FlyerPath(root, e), 32 * 4, 24, () => { for (int f = 0; f < 4; f++) Flyer(16 + f * 32, 12, 1, era, f / 14f + 0.001f, null); });
            Need(TurretPath(root, e), 32 * 4, 16, () =>
            {
                Turret(12, 15, 1, era, 0f, 0, null); Turret(44, 15, 1, era, 0.3f, 0, null);
                Turret(76, 15, 1, era, 0f, 2, null); Turret(108, 15, 1, era, 0.3f, 2, null);
            });
            Need(TilesPath(root, e), 8 * K.T, 5 * K.T, () => Tiles(era, e));
            Need(BgPath(root, e, "ceu"), K.W, K.H, () => Sky(era));
            Need(BgPath(root, e, "fundo"), BgLoop, K.H, () => FarLayer(era));
            Need(BgPath(root, e, "meio"), BgLoop, K.H, () => MidLayer(era));
        }
        Need(PropPath(root, "refem"), CW * Cols, CH * Rows, () => CharSheet(HostageLook));
        Need(PropPath(root, "barril"), 32, 16, () => { Barrel(8, 15, false, 0); Barrel(24, 15, true, 0); });
        Need(PropPath(root, "glorb"), 64, 16, () => { for (int f = 0; f < 4; f++) Glorb(8 + f * 16, 8, f * 0.26f); });
        Need(PropPath(root, "jaula"), 64, 48, () => { CageBars(16, 47, 0f); CageBars(48, 47, 0.13f); });
        Need(PropPath(root, "projeteis"), 32 * 12, 32, () => { for (int k = 0; k < 12; k++) { Projectile(k, 16 + k * 32, 8, 0f); Projectile(k, 16 + k * 32, 24, 0.05f); } });

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
            case 9: // batarangue (quadro B = girado)
                if (time == 0) { Rect(x - 5, y, 11, 2, Hex(0x2a2a34)); Rect(x - 6, y - 1, 2, 2, Hex(0x2a2a34)); Rect(x + 5, y - 1, 2, 2, Hex(0x2a2a34)); Rect(x - 1, y - 1, 3, 1, Hex(0x4a4a58)); }
                else { Rect(x, y - 5, 2, 11, Hex(0x2a2a34)); Rect(x - 1, y - 6, 2, 2, Hex(0x2a2a34)); Rect(x - 1, y + 5, 2, 2, Hex(0x2a2a34)); Rect(x + 1, y - 1, 1, 3, Hex(0x4a4a58)); }
                break;
            case 10: // teia
                PixelCircle(x, y, 2, White);
                Rect(x - 4, y, 3, 1, Hex(0xd8d8e0)); Rect(x - 3, y - 2, 1, 1, Hex(0xd8d8e0)); Rect(x - 3, y + 2, 1, 1, Hex(0xd8d8e0));
                if (time > 0) { Rect(x + 2, y - 3, 1, 1, White); Rect(x + 2, y + 3, 1, 1, White); }
                break;
            case 11: // flecha explosiva
                Rect(x - 7, y, 11, 1, Hex(0x8a5a2a));
                Rect(x - 8, y - 1, 2, 3, Hex(0xd8d0c0));
                Rect(x + 4, y - 1, 3, 3, Hex(0xd8342a));
                Rect(x + 5, y, 1, 1, time == 0 ? Yellow : Orange);
                break;
            case 8: // bomba
                PixelCircle(x, y, 4, Hex(0x2c2c32)); PixelCircle(x, y, 3, Hex(0x45454e));
                Rect(x - 1, y - 2, 1, 1, Hex(0x7a7a7a));
                Rect(x, y - 6, 1, 2, time == 0 ? Yellow : Orange);
                break;
        }
    }

    // ------------------------------------------------------------------ tiles

    /// <summary>Folha 8x5 de tiles 16x16 (ver LEIAME).</summary>
    static void Tiles(Era e, int eraIdx)
    {
        const int T = K.T;
        for (int v = 0; v < 4; v++)
            for (int depth = 0; depth < 3; depth++)
                Dirt(v * T, depth * T, e, depth, v);
        Brick(0, 3 * T, e, 0); Brick(T, 3 * T, e, 1);
        Steel(2 * T, 3 * T, e); Crate(3 * T, 3 * T);
        for (int d = 1; d <= 3; d++) Cracks((3 + d) * T, 3 * T, d);
        Door(6 * T, 4 * T, true); Door(7 * T, 4 * T, false);
        if (e.Style == BgStyle.Future) return;
        var flower = e.Style switch { BgStyle.Jungle => Red, BgStyle.Dino => Orange, _ => Magenta };
        for (int sway = 0; sway < 2; sway++)
        {
            int by = 4 * T;       // enfeites: a base da celula encosta no topo do bloco
            // tufo A: tres folhas
            int bx = sway * T;
            Rect(bx + 4 + sway, by + 10, 1, 4, e.TopLight); Rect(bx + 4, by + 14, 1, 2, e.Top);
            Rect(bx + 6, by + 11, 1, 5, e.Top); Rect(bx + 6 + sway, by + 11, 1, 1, e.TopLight);
            Rect(bx + 8 + sway, by + 12, 1, 3, e.TopLight); Rect(bx + 8, by + 15, 1, 1, e.Top);
            // tufo B: capim baixo
            bx = 2 * T + sway * T;
            Rect(bx + 9 + sway, by + 13, 1, 2, e.TopLight); Rect(bx + 10, by + 14, 2, 2, e.Top);
            Rect(bx + 12 + sway, by + 12, 1, 3, e.TopLight);
            // flor
            bx = 4 * T + sway * T;
            Rect(bx + 7, by + 10, 1, 6, e.Top); Rect(bx + 8, by + 13, 2, 1, e.Top);
            Rect(bx + 6 + sway, by + 7, 3, 3, flower); Rect(bx + 7 + sway, by + 6, 1, 5, flower);
            Rect(bx + 5 + sway, by + 8, 5, 1, flower);
            Rect(bx + 7 + sway, by + 8, 1, 1, Yellow);
        }
    }

    static void Dirt(int x, int y, Era e, int depth, int variant)
    {
        const int T = K.T;
        var c = depth == 0 ? e.Dirt : depth == 1 ? Col.Lerp(e.Dirt, e.DirtDark, 0.45f) : e.DirtDark;
        var dark = Mul(c, 0.8f); var light = Mul(c, 1.14f);
        Rect(x, y, T, T, c);
        // pedrinhas e textura
        for (int i = 0; i < 7; i++)
        {
            uint h = Hash.H(variant * 31 + i, depth * 7 + 3);
            int px = (int)(h % 14), py = (int)((h >> 5) % 14);
            Rect(x + px, y + py, 2, 1, dark);
            if (i % 2 == 0) Rect(x + px, y + py - 1, 1, 1, light);
        }
        if (depth >= 1)
        {
            uint h = Hash.H(variant, depth + 40);
            int px = 2 + (int)(h % 9), py = 4 + (int)((h >> 4) % 7);
            Rect(x + px, y + py, 4, 3, Mul(c, depth == 2 ? 0.86f : 1.1f));
            Rect(x + px, y + py, 4, 1, Mul(c, depth == 2 ? 1.0f : 1.22f));
        }
        if (depth == 0)
        {
            // grama: faixa no topo com fios pendurados
            Rect(x, y, T, 5, e.Top);
            Rect(x, y, T, 1, e.TopLight);
            Rect(x, y + 1, T, 1, Col.Lerp(e.Top, e.TopLight, 0.4f));
            for (int i = 0; i < T; i++)
            {
                uint h = Hash.H(i + variant * 17, 9);
                int len = (int)(h % 4);
                if (len > 0) Rect(x + i, y + 5, 1, len, len > 2 ? Mul(e.Top, 0.85f) : e.Top);
            }
            Rect(x, y + 5, T, 1, A(Mul(c, 0.7f), 0.5f));
        }
    }

    /// <summary>Porta de madeira (2 blocos de altura): top = parte de cima, com janelinha.</summary>
    static void Door(int x, int y, bool top)
    {
        const int T = K.T;
        var frame = Hex(0x5a3a1e); var wood = Hex(0x9a6432); var light = Hex(0xb87c44);
        Rect(x + 1, y, T - 2, T, frame);
        Rect(x + 3, y + (top ? 2 : 0), T - 6, T - (top ? 2 : 1), wood);
        for (int i = 0; i < 3; i++) Rect(x + 4 + i * 3, y + (top ? 2 : 0), 1, T - (top ? 2 : 1), Mul(wood, 0.85f));
        Rect(x + 3, y + (top ? 2 : 0), 1, T - 2, light);
        if (top) { Rect(x + 5, y + 4, 6, 4, Hex(0x2a2a3a)); Rect(x + 5, y + 4, 6, 1, Hex(0x6a7088)); }
        else { Rect(x + 10, y + 5, 2, 2, Hex(0xd8b040)); Rect(x + 3, y + 10, T - 6, 1, Mul(wood, 0.8f)); }
    }

    static void Brick(int x, int y, Era e, int odd)
    {
        const int T = K.T;
        var mortar = e.BrickDark;
        Rect(x, y, T, T, mortar);
        for (int row = 0; row < 4; row++)
        {
            int off = ((row + odd) % 2) * 4;
            for (int bx = -off; bx < T; bx += 8)
            {
                int x0 = Math.Max(0, bx), x1 = Math.Min(T, bx + 7);
                if (x1 <= x0) continue;
                var bc = Mul(e.Brick, 0.92f + 0.16f * Hash.F(bx + row * 7 + odd * 3, 77));
                Rect(x + x0, y + row * 4, x1 - x0, 3, bc);
                Rect(x + x0, y + row * 4, x1 - x0, 1, Mul(bc, 1.18f));
            }
        }
    }

    static void Steel(int x, int y, Era e)
    {
        const int T = K.T;
        Rect(x, y, T, T, e.SteelDark);
        Rect(x + 1, y + 1, T - 2, T - 2, e.Steel);
        Rect(x + 1, y + 1, T - 2, 1, Mul(e.Steel, 1.25f));
        Rect(x + 1, y + 1, 1, T - 2, Mul(e.Steel, 1.12f));
        // brilho diagonal
        for (int i = 0; i < 5; i++) Rect(x + 4 + i, y + 9 - i, 1, 1, Mul(e.Steel, 1.3f));
        // rebites nos cantos
        foreach (var (rx, ry) in new[] { (2, 2), (12, 2), (2, 12), (12, 12) })
        {
            Rect(x + rx, y + ry, 2, 2, Mul(e.Steel, 1.35f));
            Rect(x + rx + 1, y + ry + 1, 1, 1, e.SteelDark);
        }
    }

    static void Crate(int x, int y)
    {
        const int T = K.T;
        var wood = Hex(0xb07a3a); var dark = Hex(0x7a4e22); var light = Hex(0xd09a5a);
        Rect(x, y, T, T, dark);
        Rect(x + 2, y + 2, T - 4, T - 4, wood);
        for (int i = 0; i < 3; i++) Rect(x + 2, y + 5 + i * 4, T - 4, 1, Mul(wood, 0.85f));     // tabuas
        for (int i = 0; i < T - 4; i++) Rect(x + 2 + i, y + 2 + i, 2, 1, dark);              // travessa diagonal
        Rect(x, y, T, 1, light); Rect(x, y, 1, T, Mul(light, 0.9f));
    }

    static void Cracks(int x, int y, int dmg)
    {
        var c = A(Hex(0x000000), 0.55f);   // fica como uma sombra da cor do proprio tile
        Rect(x + 7, y + 3, 1, 4, c); Rect(x + 8, y + 7, 1, 3, c);
        if (dmg > 1) { Rect(x + 9, y + 9, 3, 1, c); Rect(x + 3, y + 10, 3, 1, c); Rect(x + 6, y + 9, 1, 1, c); }
        if (dmg > 2) { Rect(x + 11, y + 2, 1, 3, c); Rect(x + 12, y + 5, 2, 1, c); Rect(x + 2, y + 4, 2, 1, c); Rect(x + 4, y + 13, 1, 2, c); }
    }

    // ------------------------------------------------------------------ cenarios

    static void Sky(Era e)
    {
        // ceu em faixas de cor (pixel art, sem degrade liso)
        const int bands = 10;
        for (int i = 0; i < bands; i++)
        {
            int y0 = i * K.H / bands, y1 = (i + 1) * K.H / bands;
            Rect(0, y0, K.W, y1 - y0, Col.Lerp(e.SkyTop, e.SkyBot, i / (float)(bands - 1)));
        }
        int sunX = 230, sunY = e.Style == BgStyle.Future ? 70 : 40;
        int sr = e.Style == BgStyle.Future ? 34 : 16;
        PixelCircle(sunX, sunY, sr, e.Sun);
        if (e.Style == BgStyle.Future)
            for (int i = 0; i < 6; i++) Rect(sunX - sr - 1, sunY + 4 + i * 5, sr * 2 + 3, 1 + i / 2, e.SkyBot);
        if (e.Style == BgStyle.Medieval)
        {
            // lua crescente: "corta" o circulo com a cor da faixa de ceu de cada linha
            int cx = sunX + 5, cy = sunY - 3, r = sr - 2;
            for (int dy = -r; dy <= r; dy++)
            {
                int half = (int)MathF.Sqrt(r * r - dy * dy + r * 0.6f);
                int y = cy + dy, band = Math.Clamp(y * bands / K.H, 0, bands - 1);
                Rect(cx - half, y, half * 2 + 1, 1, Col.Lerp(e.SkyTop, e.SkyBot, band / (float)(bands - 1)));
            }
        }
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
  7 bala inimiga (pinte de branco/cinza: o jogo aplica a cor da era), 8 bomba, 9 batarangue, 10 teia, 11 flecha.

## tiles/<era>.png
Folha de 8x5 tiles de **16x16** (o heroi tem ~1 bloco de altura, como no Broforce):
- Linha 0: terra com grama (topo exposto), 4 variacoes
- Linha 1: terra logo abaixo do topo, 4 variacoes
- Linha 2: terra profunda, 4 variacoes
- Linha 3: 0 tijolo (linha par), 1 tijolo (linha impar), 2 aco, 3 caixote, 4-6 rachaduras (camada por cima, 1 a 3 de dano)
- Linha 4: enfeites desenhados **em cima** do tile de grama: 0-1 tufo A (2 quadros de vento), 2-3 tufo B, 4-5 flor;
  6 porta (parte de cima), 7 porta (parte de baixo)

Nada tem contorno preto: as formas sao definidas so pelas cores e pelo sombreamento.

## cenarios/<era>/
- **ceu.png** (320x180): fundo fixo com sol/lua.
- **fundo.png** e **meio.png** (960x180, transparentes): camadas de parallax que se repetem
  na horizontal. A borda direita deve encaixar na esquerda.

Efeitos (fogo, fumaca, sangue, faiscas, brilhos, estrelas) sao gerados pelo codigo.
""";
}
