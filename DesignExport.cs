using Raylib_cs;
using static JackassRun.Col;
using static JackassRun.Gfx;

namespace JackassRun;

/// <summary>Gera a arte padrao do jogo como PNGs editaveis na pasta design/.
/// So cria os arquivos que estiverem faltando: apague um PNG para recuperar o original.</summary>
public static class DesignExport
{
    public static readonly string[] EraSlug = { "selva", "jurassico", "medieval", "cidade", "futuro" };
    public static readonly string[] HeroSlug = { "batman", "tomb_raider", "homem_aranha" };
    public static readonly string[] GruntSlug =
        { "soldado", "bazuqueiro", "brutamontes", "faca", "homem_bomba", "escudeiro", "granadeiro", "atirador", "lanca_chamas" };
    /// <summary>Tipo de inimigo de cada folha de GruntSlug (mesma ordem).</summary>
    public static readonly EnemyKind[] GruntKinds =
    {
        EnemyKind.Soldier, EnemyKind.Rocketeer, EnemyKind.Brute, EnemyKind.Knife, EnemyKind.Bomber,
        EnemyKind.Shield, EnemyKind.Grenadier, EnemyKind.Sniper, EnemyKind.Flamer,
    };

    /// <summary>Indice da folha de cada tipo de inimigo a pe (ver GruntSlug).</summary>
    public static int GruntIndex(EnemyKind k) => Math.Max(0, Array.IndexOf(GruntKinds, k));

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
    public static string DogPath(string root, int era) => Path.Combine(root, "sprites", "inimigos", $"{EraSlug[era]}_cao.png");
    public static string PropPath(string root, string name) => Path.Combine(root, "sprites", "objetos", name + ".png");
    public static string TilesPath(string root, int era) => Path.Combine(root, "tiles", EraSlug[era] + ".png");
    public static string TilePath(string root, string name) => Path.Combine(root, "tiles", name + ".png");
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
        // escudeiro: escudo alto da cor do aco do cenario, faixa na cor de destaque
        if (k == EnemyKind.Shield)
        {
            l.Wpn = 11; l.Shirt = Mul(e.EShirt, 0.85f);
            l.Gun = e.Style switch
            {
                BgStyle.Jungle => Hex(0x6a7458),      // chapa de aco pintada
                BgStyle.Dino => Hex(0x9a7444),        // couro esticado
                BgStyle.Medieval => Hex(0x8a5a2e),    // escudo de madeira
                BgStyle.City => Hex(0x4a5868),        // escudo de choque da policia
                _ => Hex(0x3ab0e0),                   // escudo de energia
            };
        }
        // granadeiro: granada verde na mao e cinto de granadas
        if (k == EnemyKind.Grenadier)
        {
            l.Wpn = 12; l.Gun = Hex(0x4e6e2c); l.Shirt = Col.Lerp(e.EShirt, Hex(0x6a5a2a), 0.4f);
            l.Belt = Hex(0x4e6e2c); l.CustomBelt = true;
        }
        // atirador de elite: roupa escura (camuflagem) e fuzil comprido com luneta
        if (k == EnemyKind.Sniper) { l.Wpn = 13; l.Shirt = Mul(e.EShirt, 0.62f); l.Pants = Mul(e.EPants, 0.7f); l.Gun = Hex(0x2a2a2e); }
        // lanca-chamas: roupa anti-chamas e tanque vermelho nas costas
        if (k == EnemyKind.Flamer)
        {
            l.Wpn = 14; l.Gun = Hex(0x5a5a62); l.Tank = true; l.Accent = Hex(0xd04020);
            l.Shirt = Col.Lerp(e.EShirt, Hex(0xb89a4a), 0.55f); l.Pants = Col.Lerp(e.EPants, Hex(0x8a7038), 0.4f);
        }
        // cao: so as cores (para os pedacos quando explode)
        if (k == EnemyKind.Dog) { var (b, d, lt, _, _) = DogColors(e.Style); l.Skin = b; l.Shirt = d; l.Pants = lt; }
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
                var look = EnemyLook(era, GruntKinds[k]);
                Need(GruntPath(root, e, k), CW * Cols, CH * Rows, () => CharSheet(look));
            }
            Need(DogPath(root, e), DogW * 6, DogH, () => { for (int f = 0; f < 6; f++) Dog(f * DogW, 0, era.Style, f); });
            Need(FlyerPath(root, e), 32 * 4, 24, () => { for (int f = 0; f < 4; f++) Flyer(16 + f * 32, 12, 1, era, f / 14f + 0.001f, null); });
            Need(TurretPath(root, e), 32 * 4, 16, () =>
            {
                Turret(12, 15, 1, era, 0f, 0, null); Turret(44, 15, 1, era, 0.3f, 0, null);
                Turret(76, 15, 1, era, 0f, 2, null); Turret(108, 15, 1, era, 0.3f, 2, null);
            });
            Need(TilesPath(root, e), 8 * K.T, 5 * K.T, () => Tiles(era, e));
            Need(TilePath(root, "fundo_" + EraSlug[e]), 4 * K.T, 2 * K.T, () => BackTiles(era));
            Need(BgPath(root, e, "ceu"), K.W, K.H, () => Sky(era));
            Need(BgPath(root, e, "fundo"), BgLoop, K.H, () => FarLayer(era));
            Need(BgPath(root, e, "meio"), BgLoop, K.H, () => MidLayer(era));
        }
        Need(TilePath(root, "ponte"), 4 * K.T, 2 * K.T, BridgeTiles);
        Need(TilePath(root, "escada"), 2 * K.T, K.T, LadderTiles);
        Need(TilePath(root, "parede"), Eras.All.Length * K.T, 2 * K.T, HouseWall);
        Need(TilePath(root, "concreto"), K.T, 2 * K.T, ConcreteTiles);
        Need(TilePath(root, "telhado"), 4 * K.T, Eras.All.Length * K.T, RoofTiles);
        Need(TilePath(root, "vagao"), 8 * K.T, K.T, WagonTiles);
        Need(PropPath(root, "refem"), CW * Cols, CH * Rows, () => CharSheet(HostageLook));
        Need(PropPath(root, "paraquedas"), 24, 18, ParachuteArt);
        Need(PropPath(root, "carro"), 3 * 32, 16, CarArt);
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

    /// <summary>Folha 8x5 de tiles 16x16 (ver LEIAME). Luz vindo de cima/esquerda; sem contornos pretos.</summary>
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
        int by = 4 * T;       // enfeites: a base da celula encosta no topo do bloco
        if (e.Style == BgStyle.City) { CityDecor(by, e); return; }
        var flower = e.Style switch { BgStyle.Jungle => Hex(0xe0303a), BgStyle.Dino => Hex(0xf08a20), _ => Hex(0xd84ab0) };
        var g0 = Mul(e.Top, 0.72f); var g1 = e.Top; var g2 = e.TopLight;
        for (int sway = 0; sway < 2; sway++)
        {
            // tufo A: folhas altas em leque
            int bx = sway * T;
            Rect(bx + 5, by + 12, 1, 4, g0); Rect(bx + 4 + sway, by + 9, 1, 3, g1); Rect(bx + 3 + sway, by + 8, 1, 1, g2);
            Rect(bx + 7, by + 10, 1, 6, g1); Rect(bx + 7 + sway, by + 8, 1, 2, g2);
            Rect(bx + 9, by + 12, 1, 4, g0); Rect(bx + 10 + sway, by + 10, 1, 2, g1); Rect(bx + 11 + sway, by + 9, 1, 1, g2);
            // tufo B: capim baixo
            bx = 2 * T + sway * T;
            Rect(bx + 8, by + 14, 5, 2, g0); Rect(bx + 9 + sway, by + 12, 1, 2, g1); Rect(bx + 11, by + 13, 1, 1, g2);
            Rect(bx + 12 + sway, by + 12, 1, 2, g1); Rect(bx + 10, by + 13, 1, 1, g1);
            // flor (ou samambaia na era jurassica)
            bx = 4 * T + sway * T;
            if (e.Style == BgStyle.Dino)
            {
                for (int i = 0; i < 5; i++) { Rect(bx + 7 + (i > 2 ? sway : 0), by + 15 - i * 2, 1, 2, i % 2 == 0 ? g1 : g0);
                    Rect(bx + 5 + (i > 2 ? sway : 0), by + 14 - i * 2, 2, 1, g1); Rect(bx + 8 + (i > 2 ? sway : 0), by + 14 - i * 2, 2, 1, g2); }
                continue;
            }
            Rect(bx + 7, by + 10, 1, 6, g0); Rect(bx + 8, by + 13, 2, 1, g1); Rect(bx + 5, by + 12, 2, 1, g1);
            int fx0 = bx + 6 + sway;
            Rect(fx0, by + 7, 3, 3, flower); Rect(fx0 + 1, by + 6, 1, 5, flower); Rect(fx0 - 1, by + 8, 5, 1, flower);
            Rect(fx0, by + 7, 1, 1, Mul(flower, 1.35f));
            Rect(fx0 + 1, by + 8, 1, 1, Hex(0xffe060));
        }
    }

    /// <summary>Enfeites da cidade: saco de lixo, cone, papeis/garrafa (2 quadros cada).</summary>
    static void CityDecor(int by, Era e)
    {
        for (int sway = 0; sway < 2; sway++)
        {
            // saco de lixo preto
            int bx = sway * 16;
            Rect(bx + 4, by + 11, 8, 5, Hex(0x26262c)); Rect(bx + 5, by + 10, 6, 1, Hex(0x26262c));
            Rect(bx + 7, by + 8, 2, 2, Hex(0x34343c)); Rect(bx + 5, by + 11, 2, 1, Hex(0x50505c));
            Rect(bx + 8 + sway, by + 7, 1, 1, Hex(0xe0d040));
            // cone de transito (um meio tombado no quadro 2)
            bx = 32 + sway * 16;
            Rect(bx + 4, by + 15, 8, 1, Hex(0xb0401c));
            Rect(bx + 6, by + 9, 4, 6, Hex(0xf0782a)); Rect(bx + 7, by + 7, 2, 2, Hex(0xf0782a));
            Rect(bx + 6, by + 11, 4, 1, Hex(0xf4f0e8)); Rect(bx + 6, by + 9, 1, 6, Hex(0xff9a4a));
            // papeis e garrafa quebrada
            bx = 64 + sway * 16;
            Rect(bx + 3 + sway, by + 14, 3, 2, Hex(0xe8e4d8)); Rect(bx + 4 + sway, by + 14, 1, 1, Hex(0xb8b4a8));
            Rect(bx + 9, by + 13, 4, 2, Hex(0x3a8a4a)); Rect(bx + 13, by + 14, 1, 1, Hex(0x6ad07a));
            Rect(bx + 9, by + 13, 1, 1, Hex(0x8ae09a));
        }
    }

    static void Px(int x, int y, Color c) => Rect(x, y, 1, 1, c);

    /// <summary>Terra em 3 profundidades (0 = topo com grama, 1 = transicao, 2 = funda). A camada 1 faz a
    /// passagem da cor de cima para a de baixo com pontilhado, para nao formar faixas duras no mundo.
    /// Cada variacao tem um detalhe proprio da era (raiz, osso, pedra, cabo, cano).</summary>
    static void Dirt(int x, int y, Era e, int depth, int variant)
    {
        const int T = K.T;
        var c0 = e.Dirt; var c2 = e.DirtDark; var c1 = Col.Lerp(c0, c2, 0.5f);
        var c = depth == 0 ? c0 : depth == 1 ? c1 : c2;
        uint seed = (uint)(variant * 977 + depth * 131 + (int)e.Style * 7919);
        uint H(int i) => Hash.H((int)seed + i * 13, i * 7 + 3);
        Rect(x, y, T, T, c);
        if (depth == 1)
        {
            for (int py = 0; py < T; py++)
                for (int px = 0; px < T; px++)
                {
                    bool chk = ((px + py) & 1) == 0, chk4 = ((px + py * 3) & 3) == 0;
                    if (py < 2 && chk || py >= 2 && py < 4 && chk4) Px(x + px, y + py, Col.Lerp(c0, c1, 0.35f));
                    if (py >= 14 && chk || py >= 12 && py < 14 && chk4) Px(x + px, y + py, Col.Lerp(c1, c2, 0.65f));
                }
        }
        var hi = Mul(c, 1.14f); var lo = Mul(c, 0.8f); var lo2 = Mul(c, 0.66f);
        // torroes: manchas com luz em cima e sombra embaixo
        for (int i = 0; i < 4; i++)
        {
            uint h = H(i);
            int w = 2 + (int)(h % 3), px = 1 + (int)((h >> 4) % (uint)(T - w - 2)), py = 1 + (int)((h >> 9) % 12);
            Rect(x + px, y + py, w, 2, Mul(c, 1.06f));
            Rect(x + px, y + py, w - 1, 1, hi);
            Rect(x + px + 1, y + py + 2, w, 1, lo);
        }
        // pontinhos
        for (int i = 0; i < 6; i++)
        {
            uint h = H(i + 20);
            Px(x + 1 + (int)(h % 14), y + 1 + (int)((h >> 5) % 14), i % 3 == 0 ? hi : lo2);
        }
        // pedrinha
        {
            uint h = H(40);
            int px = 2 + (int)(h % 10), py = 3 + (int)((h >> 6) % 10);
            var st = Col.Lerp(c, Hex(0xa8a49c), depth == 2 ? 0.12f : 0.38f);
            Rect(x + px, y + py, 2, 2, st); Px(x + px, y + py, Mul(st, depth == 2 ? 1.06f : 1.18f));
            if (depth < 2) Rect(x + px, y + py + 2, 2, 1, lo);
        }
        // detalhe da era
        if (variant == 1 && depth == 1) EraDetail(x, y, e, depth, c);    // so logo abaixo da superficie (nao repete no subsolo)
        if (variant == 3 && depth == 2)
        {
            // pedra enterrada (discreta: a camada funda se repete muito)
            var st = Col.Lerp(c, Hex(0x8a8680), 0.2f);
            Rect(x + 5, y + 7, 5, 3, st); Rect(x + 6, y + 6, 3, 1, st);
            Rect(x + 6, y + 7, 2, 1, Mul(st, 1.12f)); Rect(x + 6, y + 10, 4, 1, Mul(c, 0.82f));
        }
        if (depth == 0)
        {
            if (e.Style == BgStyle.City) { Sidewalk(x, y, e, variant); return; }
            // grama: espessura irregular, luz no topo, ponta escura e sombra sobre a terra
            int[] thick = new int[T];
            for (int i = 0; i < T; i++) thick[i] = 3 + (int)(Hash.H(i + variant * 17, 9) % 3);
            for (int i = 1; i < T - 1; i++) thick[i] = Math.Max(thick[i], (thick[i - 1] + thick[i + 1]) / 2);
            bool neon = e.Style == BgStyle.Future;
            for (int i = 0; i < T; i++)
            {
                int th = neon ? 2 + (thick[i] > 4 ? 1 : 0) : thick[i];
                Rect(x + i, y, 1, th, e.Top);
                Px(x + i, y, e.TopLight);
                if (Hash.H(i, variant + 50) % 3 == 0) Px(x + i, y + 1, Col.Lerp(e.Top, e.TopLight, 0.5f));
                Px(x + i, y + th - 1, Mul(e.Top, neon ? 0.7f : 0.78f));
                Px(x + i, y + th, Mul(c0, 0.7f));
                if (!neon && Hash.H(i + 3, variant + 80) % 5 == 0) Px(x + i, y + th + 1, Mul(c0, 0.82f));
            }
        }
    }

    static void EraDetail(int x, int y, Era e, int depth, Color c)
    {
        switch (e.Style)
        {
            case BgStyle.Jungle:
            {
                // raiz serpenteando
                var root = Col.Lerp(c, Hex(0xc8965a), 0.5f);
                int[] path = { 3, 3, 4, 4, 5, 5, 5, 6, 6, 7, 7, 8, 8, 8 };
                for (int i = 0; i < path.Length; i++) Px(x + 1 + i, y + path[i] + (depth == 2 ? 2 : 0), i % 4 == 0 ? Mul(root, 1.15f) : root);
                Px(x + 6, y + 7 + (depth == 2 ? 2 : 0), root); Px(x + 6, y + 8 + (depth == 2 ? 2 : 0), Mul(root, 0.85f));
                break;
            }
            case BgStyle.Dino:
            {
                // osso fossil
                var bone = Hex(0xe8dcc0); var sh = Hex(0xb8a888);
                Rect(x + 5, y + 7, 6, 2, bone); Rect(x + 4, y + 6, 2, 2, bone); Rect(x + 4, y + 8, 2, 2, bone);
                Rect(x + 10, y + 6, 2, 2, bone); Rect(x + 10, y + 8, 2, 2, bone);
                Rect(x + 5, y + 9, 6, 1, sh); Px(x + 4, y + 9, sh); Px(x + 11, y + 9, sh);
                break;
            }
            case BgStyle.Medieval:
            {
                // caveira enterrada
                var sk = Hex(0xd8d0bc); var sh = Hex(0x9a9282);
                Rect(x + 6, y + 6, 4, 3, sk); Rect(x + 7, y + 9, 2, 1, sk);
                Px(x + 6, y + 7, Hex(0x2a2420)); Px(x + 9, y + 7, Hex(0x2a2420));
                Rect(x + 6, y + 9, 1, 1, sh); Rect(x + 9, y + 9, 1, 1, sh);
                break;
            }
            case BgStyle.Future:
            {
                // cabo / trilha de circuito
                var cab = Hex(0x2ab8c8);
                Rect(x, y + 9, 6, 1, Mul(cab, 0.6f)); Rect(x + 6, y + 6, 1, 4, Mul(cab, 0.6f)); Rect(x + 6, y + 6, 10, 1, Mul(cab, 0.6f));
                Rect(x + 5, y + 8, 3, 3, Mul(cab, 0.45f)); Px(x + 6, y + 9, cab);
                break;
            }
            case BgStyle.City:
            {
                // pedaco de cano enferrujado e entulho
                var pipe = Hex(0x8a5a3a);
                Rect(x + 3, y + 7, 7, 3, pipe); Rect(x + 3, y + 7, 7, 1, Mul(pipe, 1.3f)); Rect(x + 3, y + 9, 7, 1, Mul(pipe, 0.7f));
                Rect(x + 9, y + 6, 2, 5, Mul(pipe, 0.85f));
                Rect(x + 12, y + 11, 2, 2, Mul(c, 1.25f)); Px(x + 12, y + 11, Mul(c, 1.4f));
                break;
            }
        }
    }

    /// <summary>Calcada portuguesa paulistana (ondas pretas e brancas) com guia de concreto.</summary>
    static void Sidewalk(int x, int y, Era e, int variant)
    {
        const int T = K.T;
        var white = e.TopLight; var black = Hex(0x2c2c30);
        for (int px = 0; px < T; px++)
        {
            int wave = (int)MathF.Round(MathF.Sin((px + variant * T) * MathF.PI * 2 / 32f) * 1.4f);
            for (int py = 0; py < 5; py++)
            {
                bool dark = py == 2 + wave || py == 3 + wave && ((px + variant) % 5) != 0;
                Px(x + px, y + py, dark ? black : ((px * 3 + py * 5 + variant) % 7 == 0 ? Mul(white, 0.88f) : white));
            }
        }
        Rect(x, y + 5, T, 2, e.Top);                  // guia
        Rect(x, y + 5, T, 1, Mul(e.Top, 1.15f));
        Rect(x, y + 7, T, 1, Mul(e.Dirt, 0.7f));
        if (variant == 2) { Px(x + 5, y + 1, Hex(0x5a5650)); Px(x + 6, y + 2, Hex(0x5a5650)); Px(x + 6, y + 3, Hex(0x5a5650)); }   // trinca
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

    /// <summary>Tijolos com volume: luz no topo, sombra embaixo, lascas e manchas. Selva: musgo.
    /// Cidade: blocos de concreto com pichacao no estilo "pixo" de Sao Paulo.</summary>
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
                float hv = Hash.F(bx + row * 7 + odd * 3, 77);
                var bc = Mul(e.Brick, 0.9f + 0.18f * hv);
                int by = y + row * 4;
                Rect(x + x0, by, x1 - x0, 3, bc);
                Rect(x + x0, by, x1 - x0, 1, Mul(bc, 1.16f));
                Rect(x + x0, by + 2, x1 - x0, 1, Mul(bc, 0.86f));
                if (x0 == Math.Max(0, bx)) Px(x + x0, by + 1, Mul(bc, 1.08f));
                // lasca no canto e mancha
                if (hv > 0.75f) Px(x + x1 - 1, by, mortar);
                if (hv < 0.2f && x1 - x0 > 3) Px(x + x0 + 2, by + 1, Mul(bc, 0.8f));
            }
        }
        if (e.Style == BgStyle.Jungle)
            foreach (var (mx, my) in new[] { (2 + odd * 5, 0), (3 + odd * 5, 0), (4 + odd * 5, 1), (11, 4), (12, 4), (10, 8 + odd * 4) })
                Px(x + mx, y + my, Hash.H(mx, my) % 2 == 0 ? e.Top : Mul(e.Top, 0.75f));
        if (e.Style == BgStyle.City)
        {
            // concreto sujo: escorrido de fuligem e manchas (o pixo fica nos predios do fundo)
            var soot = Mul(e.Brick, 0.7f);
            if (odd == 1) { Rect(x + 4, y + 1, 1, 6, soot); Rect(x + 5, y + 2, 1, 3, soot); Px(x + 4, y + 8, soot); }
            else { Rect(x + 10, y + 9, 3, 2, Mul(e.Brick, 0.8f)); Px(x + 11, y + 12, soot); }
        }
    }

    /// <summary>Chapa de aco chanfrada: borda escura, chanfro claro em cima/esquerda e escuro embaixo/direita,
    /// textura escovada, brilho diagonal e rebites com volume.</summary>
    static void Steel(int x, int y, Era e)
    {
        const int T = K.T;
        var s = e.Steel;
        Rect(x, y, T, T, e.SteelDark);
        Rect(x + 1, y + 1, T - 2, T - 2, s);
        Rect(x + 1, y + 1, T - 2, 1, Mul(s, 1.32f)); Rect(x + 1, y + 2, 1, T - 3, Mul(s, 1.16f));
        Rect(x + 1, y + T - 2, T - 2, 1, Mul(s, 0.72f)); Rect(x + T - 2, y + 2, 1, T - 3, Mul(s, 0.82f));
        for (int i = 3; i < T - 3; i += 3) Rect(x + i, y + 3, 1, T - 6, Mul(s, 0.95f));
        for (int i = 0; i < 6; i++) { Px(x + 4 + i, y + 11 - i, Mul(s, 1.22f)); Px(x + 5 + i, y + 11 - i, Mul(s, 1.12f)); }
        for (int i = 0; i < 3; i++) Px(x + 9 + i, y + 12 - i, Mul(s, 1.18f));
        foreach (var (rx, ry) in new[] { (3, 3), (11, 3), (3, 11), (11, 11) })
        {
            Rect(x + rx, y + ry, 2, 2, Mul(s, 1.1f));
            Px(x + rx, y + ry, Mul(s, 1.5f));
            Px(x + rx + 1, y + ry + 1, Mul(s, 0.62f));
        }
    }

    /// <summary>ponte.png: linha 0 = corrimao de corda (desenhado no tile ACIMA da tabua): meio, poste esquerdo,
    /// poste direito. Linha 1 = tabua (so os 5px de cima sao piso): inteira, estragada.</summary>
    static void BridgeTiles()
    {
        const int T = K.T;
        var wood = Hex(0x9a6432); var dark = Hex(0x5a3a1e); var light = Hex(0xc08a4e); var rope = Hex(0xd8c08a); var ropeD = Hex(0xa08a5a);
        // corda do corrimao (ligeiramente caida no meio) e amarras ate a tabua
        for (int x = 0; x < T; x++) Rect(x, 7 + (x >= 4 && x < 12 ? 1 : 0), 1, 1, rope);
        Rect(3, 9, 1, 7, ropeD); Rect(11, 9, 1, 7, ropeD);
        // postes
        foreach (var (px, ox) in new[] { (T, 2), (2 * T, 11) })
        {
            Rect(px + ox, 4, 3, 12, wood); Rect(px + ox, 4, 1, 12, light); Rect(px + ox + 2, 4, 1, 12, dark);
            Rect(px + ox - 1, 3, 5, 2, dark);
            for (int x = 0; x < T; x++) Rect(px + x, 7, 1, 1, rope);
        }
        // tabuas
        for (int k = 0; k < 2; k++)
        {
            int x0 = k * T, y0 = T;
            Rect(x0, y0, T, 5, wood);
            Rect(x0, y0, T, 1, light);
            Rect(x0, y0 + 4, T, 1, dark);
            for (int i = 0; i < 4; i++) { Rect(x0 + i * 4 + 3, y0 + 1, 1, 3, dark); Rect(x0 + i * 4 + 1, y0 + 2, 1, 1, Hex(0x3a3030)); }
            Rect(x0 + 3, y0 + 5, 1, 2, ropeD); Rect(x0 + 11, y0 + 5, 1, 2, ropeD);     // amarras por baixo
            if (k == 1)
            {
                // tabua rachada: falta um pedaco e ha lascas
                Clear(x0 + 6, y0, 4, 3);
                Rect(x0 + 5, y0 + 1, 1, 2, dark); Rect(x0 + 10, y0 + 1, 1, 2, dark);
                Rect(x0 + 6, y0 + 3, 4, 1, Mul(dark, 0.8f));
            }
        }
    }

    /// <summary>escada.png: escada de madeira (inteira, estragada). Atravessa-se pelos lados; o topo serve de piso.</summary>
    static void LadderTiles()
    {
        const int T = K.T;
        var wood = Hex(0xa8743a); var dark = Hex(0x6a4420); var light = Hex(0xcc965a);
        for (int k = 0; k < 2; k++)
        {
            int x0 = k * T;
            foreach (int rx in new[] { 3, 11 })
            {
                Rect(x0 + rx, 0, 2, T, wood); Rect(x0 + rx, 0, 1, T, light);
            }
            for (int ry = 1; ry < T; ry += 4)
            {
                Rect(x0 + 5, ry, 6, 2, wood); Rect(x0 + 5, ry, 6, 1, light); Rect(x0 + 5, ry + 1, 6, 1, dark);
            }
            if (k == 1) { Clear(x0 + 6, 5, 4, 2); Rect(x0 + 5, 6, 1, 1, dark); Rect(x0 + 10, 5, 1, 1, dark); }
        }
    }

    /// <summary>concreto.png: ponte de concreto. Celula de cima = tabuleiro (com guarda-corpo baixo e juntas);
    /// celula de baixo = pilar.</summary>
    static void ConcreteTiles()
    {
        const int T = K.T;
        var c = Hex(0xa8a49c); var dark = Hex(0x76726c); var light = Hex(0xccc8c0); var stain = Hex(0x8e8a84);
        // tabuleiro
        Rect(0, 0, T, T, c);
        Rect(0, 0, T, 2, light); Rect(0, 2, T, 1, dark);
        Rect(0, 11, T, 5, Mul(c, 0.86f)); Rect(0, 15, T, 1, dark);              // viga lateral
        Rect(T - 1, 3, 1, 13, dark);                                             // junta entre placas
        Rect(3, 6, 2, 1, stain); Rect(9, 8, 3, 1, stain); Rect(6, 13, 2, 1, Mul(dark, 0.9f));
        // pilar
        Rect(3, T, 10, T, c);
        Rect(3, T, 2, T, light); Rect(11, T, 2, T, dark);
        Rect(6, T + 4, 2, 1, stain); Rect(8, T + 10, 2, 1, stain);
    }

    /// <summary>parede.png: fundo de dentro das casas, uma coluna por era (selva, jurassico, medieval, futuro).
    /// Linha 0 = parede lisa; linha 1 = parede com janela.</summary>
    static void HouseWall()
    {
        const int T = K.T;
        for (int ei = 0; ei < Eras.All.Length; ei++)
        {
            var e = Eras.All[ei];
            var b = Col.Lerp(Mul(e.Brick, 0.42f), Hex(0x24202c), 0.35f);
            var dark = Mul(b, 0.72f); var light = Mul(b, 1.2f);
            for (int row = 0; row < 2; row++)
            {
                int x = ei * T, y = row * T;
                Rect(x, y, T, T, b);
                if (e.Style == BgStyle.Future)
                {
                    // paineis de metal
                    Rect(x, y, T, 1, light); Rect(x, y + T - 1, T, 1, dark); Rect(x + T - 1, y, 1, T, dark);
                    Rect(x + 2, y + 2, 1, 1, light); Rect(x + T - 3, y + 2, 1, 1, light);
                }
                else
                {
                    // tabuas verticais com rodape
                    for (int k = 0; k < 4; k++) Rect(x + k * 4 + 3, y, 1, T, dark);
                    for (int k = 0; k < 4; k++) Rect(x + k * 4, y, 1, T, Mul(b, 1.08f));
                    Rect(x, y + 11, T, 1, light); Rect(x, y + 12, T, 1, dark);
                }
                if (row == 1)
                {
                    // janela com luz fraca
                    Rect(x + 3, y + 2, 10, 8, Mul(dark, 0.8f));
                    Rect(x + 4, y + 3, 8, 6, Col.Lerp(e.SkyTop, Hex(0x101018), 0.55f));
                    Rect(x + 4, y + 3, 3, 2, Col.Lerp(e.SkyBot, Hex(0x101018), 0.4f));
                    Rect(x + 7, y + 3, 1, 6, Mul(dark, 0.8f)); Rect(x + 4, y + 6, 8, 1, Mul(dark, 0.8f));
                }
            }
        }
    }

    /// <summary>telhado.png: uma linha por cenario (selva, jurassico, medieval, cidade, futuro). Colunas: ponta
    /// esquerda, meio, ponta direita e enfeite (antena, chamine...), que e desenhado no bloco de cima do telhado.</summary>
    static void RoofTiles()
    {
        const int T = K.T;
        for (int ei = 0; ei < Eras.All.Length; ei++)
        {
            var st = Eras.All[ei].Style;
            for (int part = 0; part < 3; part++) RoofTile(part * T, ei * T, st, part);
            RoofDecor(3 * T, ei * T, st);
        }
    }

    static void RoofTile(int x, int y, BgStyle st, int part)
    {
        const int T = K.T;
        var (b, d, l) = st switch
        {
            BgStyle.Jungle => (Hex(0xb8963a), Hex(0x7a5e22), Hex(0xe0c070)),       // palha
            BgStyle.Dino => (Hex(0x5a8a2a), Hex(0x34561a), Hex(0x8ab84a)),         // folhas
            BgStyle.Medieval => (Hex(0x5a6478), Hex(0x343a4a), Hex(0x8a94a8)),     // ardosia
            BgStyle.City => (Hex(0xb04a2a), Hex(0x6e2a16), Hex(0xe07a4a)),         // telha de barro
            _ => (Hex(0x24305a), Hex(0x141a34), Hex(0x3ae0ff)),                    // painel solar
        };
        void In(int px, int py, int w, Color c)
        {
            int a = Math.Max(px, x), z = Math.Min(px + w, x + T);
            if (z > a) Rect(a, py, z - a, 1, c);
        }
        Rect(x, y, T, T, b);
        if (st == BgStyle.City)
        {
            // telha colonial (canal e capa): colunas arredondadas com sombra onde uma telha cobre a outra
            for (int i = 0; i < T; i++)
            {
                int k = i % 4;
                var c = k == 0 ? l : k == 1 ? Mul(b, 1.1f) : k == 2 ? b : d;
                Rect(x + i, y, 1, T, c);
            }
            foreach (int oy in new[] { 4, 9, 14 })
            {
                Rect(x, y + oy, T, 1, Mul(d, 0.85f));
                for (int i = 0; i < T; i += 4) Rect(x + i, y + oy - 1, 2, 1, Mul(l, 1.05f));
            }
        }
        else if (st == BgStyle.Medieval)
        {
            // ardosia em escamas, fileiras desencontradas: brilho em cima, arco escuro embaixo
            for (int band = 0; band < 3; band++)
            {
                int sy = y + 1 + band * 5, off = band % 2 == 0 ? 0 : 2;
                for (int sx = x - 4 + off; sx < x + T; sx += 4)
                {
                    In(sx + 1, sy, 2, l);
                    In(sx, sy + 3, 1, d); In(sx + 3, sy + 3, 1, d);
                    In(sx + 1, sy + 4, 2, d);
                    In(sx + 1, sy + 2, 1, Mul(b, 1.12f));
                }
            }
        }
        else if (st == BgStyle.Future)
        {
            // painel solar: celulas escuras com grade em ciano fraco e um reflexo
            var grid = Col.Lerp(b, l, 0.35f);
            Rect(x, y + 5, T, 1, grid); Rect(x, y + 10, T, 1, grid);
            Rect(x + 5, y + 1, 1, T - 2, grid); Rect(x + 10, y + 1, 1, T - 2, grid);
            Px(x + 2, y + 2, l); Px(x + 3, y + 2, Col.Lerp(b, l, 0.6f)); Px(x + 12, y + 7, Col.Lerp(b, l, 0.6f));
        }
        else
        {
            // palha (selva) ou folhas (jurassico): fios verticais irregulares com amarras
            for (int i = 0; i < T; i++)
            {
                uint h = Hash.H(i * 7 + (int)st, 31);
                var c = (h & 3) == 0 ? l : (h & 3) == 1 ? d : Mul(b, 1.06f);
                int len = 4 + (int)(h >> 4) % 8;
                Rect(x + i, y + 2 + (int)(h >> 8) % 4, 1, len, c);
            }
            Rect(x, y + 6, T, 1, d); Rect(x, y + 11, T, 1, d);
            for (int i = 0; i < T; i += 2) Px(x + i, y + T - 1, Mul(d, 0.8f));
            if (st == BgStyle.Dino)
                for (int i = 1; i < T; i += 5) { Px(x + i, y + 3, l); Px(x + i + 1, y + 4, l); Px(x + i + 2, y + 5, d); }
        }
        // cumeeira clara em cima e sombra embaixo
        Rect(x, y, T, 1, l); Rect(x, y + T - 1, T, 1, Mul(d, 0.85f));
        // pontas: tabua de beiral
        if (part == 0) { Rect(x, y, 2, T, d); Rect(x + 2, y + 1, 1, T - 1, Mul(b, 0.8f)); Px(x, y, Mul(d, 0.7f)); }
        if (part == 2) { Rect(x + T - 2, y, 2, T, d); Rect(x + T - 3, y + 1, 1, T - 1, Mul(b, 0.8f)); Px(x + T - 1, y, Mul(d, 0.7f)); }
    }

    /// <summary>vagao.png (128x16): comboio abandonado na selva. Colunas: 0 teto (chapa com cobertura e musgo),
    /// 1 chapa lateral, 2 chapa enferrujada com furo, 3 chassi com roda (pontas do vagao), 4 trilho (enfeite no
    /// chao, atravessa-se), 5 interior de baixo (com banco), 6 interior de cima (janela partida), 7 chassi sem roda
    /// (meio do vagao).</summary>
    static void WagonTiles()
    {
        const int T = K.T;
        var body = Hex(0x3f6e5c); var dark = Hex(0x26453a); var light = Hex(0x6a9e84);
        var rust = Hex(0xa0582a); var rustD = Hex(0x6a3a1e); var moss = Hex(0x5a8e2c); var mossL = Hex(0x86b84a);
        var stripe = Hex(0xb8a050);
        void Panel(int x, bool holey)
        {
            Rect(x, 0, T, T, body);
            Rect(x, 0, 1, T, dark); Rect(x + 1, 0, 1, T, light); Rect(x + T - 1, 0, 1, T, Mul(dark, 0.8f));
            Rect(x + 1, 9, T - 2, 2, A(stripe, 0.55f));                                // faixa pintada desbotada
            foreach (var (rx, ry) in new[] { (3, 2), (12, 2), (3, 13), (12, 13) }) { Px(x + rx, ry, Mul(light, 1.15f)); Px(x + rx, ry + 1, dark); }
            // escorrido de ferrugem
            Rect(x + 6, 3, 2, 4, rust); Rect(x + 6, 7, 1, 4, rust); Px(x + 7, 7, rustD); Px(x + 6, 11, rustD);
            Rect(x + 10, 11, 3, 2, Mul(rust, 0.9f)); Px(x + 11, 13, rustD);
            if (holey)
            {
                // buraco de ferrugem: bordas rasgadas e o escuro de dentro
                Rect(x + 4, 3, 7, 6, rust); Rect(x + 5, 4, 5, 4, Hex(0x14100e)); Px(x + 4, 3, rustD); Px(x + 10, 8, rustD);
                Px(x + 5, 3, Hex(0x14100e)); Px(x + 9, 8, Hex(0x14100e)); Px(x + 11, 5, rust); Px(x + 3, 6, rust);
            }
        }
        // 0 teto
        Panel(0, false);
        var cap = Hex(0x55595c); var capL = Hex(0x7a7e80);
        Rect(1, 0, T - 2, 1, capL); Rect(0, 1, T, 2, cap); Rect(0, 3, T, 1, Hex(0x2a2c2e));
        for (int i = 2; i < T; i += 4) Px(i, 1, Mul(cap, 0.75f));
        foreach (int mx in new[] { 2, 3, 9, 13 }) { Px(mx, 0, moss); Px(mx, 1, mossL); }
        Rect(8, 4, 1, 3, moss); Px(8, 7, mossL);                                       // cipo descendo
        // 1 e 2 chapas
        Panel(T, false);
        Panel(2 * T, true);
        // 3 chassi com roda, por cima do trilho
        {
            int x = 3 * T;
            var beam = Hex(0x2c2c30); var frame = Hex(0x1c1c20);
            Rect(x, 14, T, 1, Hex(0xb8b8c0)); Rect(x, 15, T, 1, Hex(0x6a6a72));       // trilho
            Rect(x, 0, T, 5, beam); Rect(x, 0, T, 1, Hex(0x4a4a50)); Rect(x, 4, T, 1, frame);
            Rect(x + 3, 1, 3, 2, rust); Px(x + 11, 2, rust);
            Rect(x + 2, 5, 12, 2, frame);
            // roda
            var rim = Hex(0x3a3a40); var wheel = Hex(0x5a5a62); var hub = Hex(0x9a9aa2);
            Rect(x + 4, 7, 8, 7, rim); Rect(x + 3, 8, 10, 5, rim);
            Rect(x + 5, 8, 6, 5, wheel); Rect(x + 4, 9, 8, 3, wheel);
            Rect(x + 7, 9, 2, 3, hub); Px(x + 7, 9, Hex(0xc8c8d0));
            Px(x + 5, 8, rust); Px(x + 10, 12, rust);
        }
        // 7 chassi sem roda (meio do vagao): viga, sombra e o trilho por baixo
        {
            int x = 7 * T;
            var beam = Hex(0x2c2c30); var frame = Hex(0x1c1c20);
            Rect(x, 0, T, 5, beam); Rect(x, 0, T, 1, Hex(0x4a4a50)); Rect(x, 4, T, 1, frame);
            Rect(x + 9, 1, 3, 2, rust); Px(x + 3, 2, rust);
            Rect(x, 5, T, 2, frame);
            Rect(x + 2, 7, 12, 1, A(Hex(0x000000), 0.35f));
            Rect(x + 5, 7, 2, 4, frame); Rect(x + 4, 10, 4, 1, frame);                // tanque de freio pendurado
            var wood = Hex(0x5a3e26);
            Rect(x + 1, 14, 6, 2, wood); Rect(x + 9, 14, 6, 2, wood);
            Rect(x, 12, T, 1, Hex(0xb8b8c0)); Rect(x, 13, T, 1, Hex(0x6a6a72));
        }
        // 4 trilho: dormentes de madeira, trilho de aco e um tufo de mato
        {
            int x = 4 * T;
            var wood = Hex(0x5a3e26);
            Rect(x + 1, 14, 6, 2, wood); Rect(x + 9, 14, 6, 2, wood);
            Rect(x + 1, 14, 6, 1, Mul(wood, 1.25f)); Rect(x + 9, 14, 6, 1, Mul(wood, 1.25f));
            Rect(x, 12, T, 1, Hex(0xb8b8c0)); Rect(x, 13, T, 1, Hex(0x6a6a72));
            Px(x + 5, 12, rust); Px(x + 12, 13, rustD);
            Px(x + 8, 11, moss); Px(x + 8, 10, mossL); Px(x + 7, 11, moss);
        }
        // 5 interior de baixo (banco) e 6 interior de cima (janela partida)
        for (int k = 0; k < 2; k++)
        {
            int x = (5 + k) * T;
            var wall = Hex(0x1e302a);
            Rect(x, 0, T, T, wall);
            for (int i = 0; i < T; i += 5) Rect(x + i, 0, 1, T, Hex(0x263c34));
            if (k == 0)
            {
                var seat = Hex(0x5a3a22);
                Rect(x + 1, 9, 14, 2, seat); Rect(x + 1, 9, 14, 1, Mul(seat, 1.3f));
                Rect(x + 2, 11, 1, 5, Hex(0x2a2a2e)); Rect(x + 13, 11, 1, 5, Hex(0x2a2a2e));
                Rect(x + 1, 3, 14, 1, Hex(0x4a5a54));                                // corrimao
                Rect(x + 5, 10, 3, 1, Hex(0x8a5a2a));                                  // rasgo no estofado
            }
            else
            {
                var frameC = Hex(0x14201c); var glass = Hex(0x5a8a6a);
                Rect(x + 2, 3, 12, 10, frameC);
                Rect(x + 3, 4, 10, 8, glass);
                Rect(x + 3, 4, 4, 2, Hex(0x9ac8a0));                                   // luz da selva
                // vidro partido
                Rect(x + 8, 6, 3, 3, frameC); Px(x + 7, 7, frameC); Px(x + 11, 9, frameC); Px(x + 9, 5, frameC);
                Rect(x + 7, 12, 1, 3, moss); Px(x + 7, 15, mossL);                     // cipo entrando pela janela
            }
        }
    }

    /// <summary>Enfeite em cima do telhado: antena de TV (cidade), parabolica (futuro), chamine (selva e medieval),
    /// ossos (jurassico). Apoiado na base da celula.</summary>
    static void RoofDecor(int x, int y, BgStyle st)
    {
        switch (st)
        {
            case BgStyle.City:
            {
                var g = Hex(0x8a8a92); var gd = Hex(0x5a5a62);
                Rect(x + 7, y + 2, 1, 14, g); Rect(x + 8, y + 2, 1, 14, gd);
                Rect(x + 2, y + 3, 11, 1, g); Rect(x + 3, y + 6, 9, 1, g); Rect(x + 4, y + 9, 7, 1, g);
                Px(x + 2, y + 4, gd); Px(x + 12, y + 4, gd);
                Rect(x + 6, y + 14, 4, 2, gd);
                break;
            }
            case BgStyle.Future:
            {
                var m = Hex(0xc8d0e0); var md = Hex(0x8a92a8);
                Rect(x + 7, y + 10, 2, 6, md);
                Rect(x + 5, y + 4, 6, 1, m); Rect(x + 4, y + 5, 8, 3, m); Rect(x + 5, y + 8, 6, 1, md);
                Rect(x + 7, y + 2, 1, 3, md); Px(x + 7, y + 1, Cyan);
                break;
            }
            case BgStyle.Dino:
            {
                var bone = Hex(0xeee6cc); var bd = Hex(0xb8b09a);
                Rect(x + 4, y + 12, 8, 2, bone); Rect(x + 4, y + 13, 8, 1, bd);
                Rect(x + 2, y + 11, 2, 2, bone); Rect(x + 2, y + 14, 2, 2, bone);
                Rect(x + 12, y + 11, 2, 2, bone); Rect(x + 12, y + 14, 2, 2, bone);
                break;
            }
            default:
            {
                var br = st == BgStyle.Medieval ? Hex(0x7a7a8a) : Hex(0x8e6a5a);
                var bd = Mul(br, 0.7f);
                Rect(x + 5, y + 6, 6, 10, br); Rect(x + 4, y + 4, 8, 2, bd); Rect(x + 4, y + 4, 8, 1, Mul(br, 1.2f));
                Rect(x + 5, y + 9, 6, 1, bd); Rect(x + 5, y + 12, 6, 1, bd);
                Px(x + 7, y + 7, bd); Px(x + 9, y + 10, bd); Px(x + 6, y + 13, bd);
                Rect(x + 10, y + 6, 1, 10, bd);
                break;
            }
        }
    }

    // ------------------------------------------------------------------ cao de ataque

    public const int DogW = 24, DogH = 16;      // celula do cao: pes no pixel (12, 15)

    /// <summary>Cores do cao de cada cenario: corpo, escuro, claro (barriga/focinho), olho e coleira.</summary>
    public static (Color body, Color dark, Color light, Color eye, Color collar) DogColors(BgStyle st) => st switch
    {
        BgStyle.Jungle => (Hex(0xa8743a), Hex(0x3a2a1e), Hex(0xd8b07a), Hex(0x1a1010), Hex(0xd8b840)),     // pastor alemao
        BgStyle.Dino => (Hex(0x6a8a3a), Hex(0x3e5a22), Hex(0xc8c890), Hex(0xffd040), Hex(0xa83a2a)),       // raptor
        BgStyle.Medieval => (Hex(0x8a8a96), Hex(0x50505e), Hex(0xc8c8d0), Hex(0xffd060), Hex(0x50505e)),   // lobo
        BgStyle.City => (Hex(0x2e2622), Hex(0x16120f), Hex(0xa8683a), Hex(0x1a1010), Hex(0xc02020)),       // rottweiler
        _ => (Hex(0xa4acc4), Hex(0x4a5472), Hex(0xd8e0f0), Hex(0x3ae0ff), Hex(0xff3a5a)),                  // cao-robo
    };

    // pe da frente (perto, longe) e de tras (perto, longe): deslocamento x e altura, por quadro
    static readonly sbyte[,] DogLegs =
    {
        { 0, 0, 0, 0, 0, 0, 0, 0 },
        { 3, 0, -1, 1, -3, 0, 1, 1 },
        { 1, 2, 1, 0, -1, 0, -1, 2 },
        { -1, 1, 3, 0, 1, 1, -3, 0 },
        { 1, 0, 1, 2, -1, 2, -1, 0 },
        { 4, 3, 3, 3, -4, 2, -3, 2 },
    };

    /// <summary>Um quadro do cao olhando para a direita, na celula (ox, oy). Quadros: 0 parado, 1-4 correndo, 5 salto.
    /// No cenario jurassico e um raptor; no futuro, um cao-robo.</summary>
    static void Dog(int ox, int oy, BgStyle st, int f)
    {
        var (b, d, l, eye, col) = DogColors(st);
        void R(int x, int y, int w, int h, Color c) => Rect(ox + x, oy + y, w, h, c);
        int bob = f is 2 or 4 ? -1 : 0;
        int hy = f == 5 ? -1 : 0;                     // cabeca erguida no salto
        void Leg(int hx, int hipY, int dx, int lift, Color c)
        {
            int fx = hx + dx, bottom = 15 - lift;
            for (int y = hipY; y < bottom; y++)
            {
                float k = (y - hipY) / (float)Math.Max(1, bottom - hipY);
                R((int)MathF.Round(hx + dx * k), y, 2, 1, c);
            }
            R(fx, bottom, 3, 1, Mul(c, 0.75f));
        }
        if (st == BgStyle.Dino)
        {
            // raptor: duas pernas fortes, cauda comprida, bracinhos com garra
            Leg(9, 10 + bob, DogLegs[f, 6], DogLegs[f, 7], Mul(d, 0.9f));
            R(0, 6 + bob, 3, 1, d); R(2, 5 + bob, 5, 2, b);
            R(6, 4 + bob, 9, 5, b);
            for (int i = 0; i < 3; i++) { R(8 + i * 2, 4 + bob, 1, 2, d); }
            R(8, 8 + bob, 6, 1, l);
            R(13, 2 + bob + hy, 3, 5, b);
            R(14, 1 + bob + hy, 7, 2, b); R(15, 3 + bob + hy, 6, 1, l);
            R(16, 3 + bob + hy, 1, 1, White); R(18, 3 + bob + hy, 1, 1, White); R(20, 3 + bob + hy, 1, 1, White);
            R(16, 1 + bob + hy, 1, 1, eye); R(20, 1 + bob + hy, 1, 1, d);
            R(15, 6 + bob, 2, 1, d); R(17, 7 + bob, 1, 1, l);
            R(9, 8 + bob, 3, 3, Mul(b, 0.92f));
            Leg(10, 10 + bob, DogLegs[f, 4], DogLegs[f, 5], b);
            return;
        }
        // pernas de longe (mais escuras) atras do corpo
        Leg(14, 10 + bob, DogLegs[f, 2], DogLegs[f, 3], Mul(d, 0.9f));
        Leg(7, 10 + bob, DogLegs[f, 6], DogLegs[f, 7], Mul(d, 0.9f));
        // cauda (abana) — o lobo tem a cauda mais grossa
        int wag = f % 2;
        R(2, 4 + bob + wag, 1, 2, b); R(3, 5 + bob, 2, 1, b); R(4, 6 + bob, 2, 2, b);
        if (st == BgStyle.Medieval) { R(1, 5 + bob + wag, 3, 2, b); R(1, 6 + bob + wag, 1, 1, l); }
        // corpo
        R(5, 6 + bob, 11, 5, b);
        R(6, 6 + bob, 8, 2, d);
        R(7, 10 + bob, 7, 1, l);
        R(14, 4 + bob, 4, 6, b);
        R(16, 7 + bob, 2, 3, l);
        // cabeca
        int y0 = 2 + bob + hy;
        R(16, y0, 5, 4, b);
        R(20, y0 + 2, 3, 2, l);
        R(22, y0 + 2, 1, 1, Ink);
        R(18, y0 + 1, 1, 1, eye);
        R(16, y0 - 2, 2, 2, d); R(17, y0 - 3, 1, 1, d);
        if (f is >= 1 and <= 4) R(20, y0 + 4, 1, 1, Hex(0xd84a5a));        // lingua de fora correndo
        if (f == 5) { R(20, y0 + 4, 3, 1, d); R(21, y0 + 4, 1, 1, White); } // boca aberta no salto
        if (st != BgStyle.Medieval) R(15, 6 + bob, 3, 1, col);             // coleira (o lobo nao tem)
        if (st == BgStyle.Future)
        {
            // cao-robo: juntas e antena
            R(17, y0 - 3, 1, 2, d); R(17, y0 - 4, 1, 1, col);
            R(6, 9 + bob, 1, 1, l); R(14, 9 + bob, 1, 1, l); R(10, 6 + bob, 2, 1, l);
        }
        // pernas de perto
        Leg(15, 10 + bob, DogLegs[f, 0], DogLegs[f, 1], b);
        Leg(6, 10 + bob, DogLegs[f, 4], DogLegs[f, 5], b);
    }

    /// <summary>paraquedas.png: copula com cordas; a base (ponto onde as cordas se juntam) no fundo, ao centro.</summary>
    static void ParachuteArt()
    {
        var a = Hex(0xd8d0b0); var b = Hex(0xa83a2a); var rope = Hex(0x6a6050);
        Rect(4, 1, 16, 2, a); Rect(2, 3, 20, 3, a); Rect(1, 6, 22, 2, a);
        for (int i = 0; i < 4; i++) Rect(4 + i * 5, 2, 2, 6, b);
        Rect(1, 8, 2, 1, Mul(a, 0.8f)); Rect(21, 8, 2, 1, Mul(a, 0.8f));
        for (int yy = 9; yy < 18; yy++)
        {
            float t = (yy - 9) / 8f;
            Rect((int)(2 + t * 9), yy, 1, 1, rope);
            Rect((int)(21 - t * 9), yy, 1, 1, rope);
        }
    }

    /// <summary>Apaga (deixa transparente) um retangulo ja desenhado.</summary>
    static void Clear(int x, int y, int w, int h)
    {
        Rlgl.DrawRenderBatchActive();
        Rlgl.DisableColorBlend();
        Raylib.DrawRectangle(x, y, w, h, new Color(0, 0, 0, 0));
        Rlgl.DrawRenderBatchActive();
        Rlgl.EnableColorBlend();
    }

    /// <summary>Caixote de madeira: moldura, tabuas com veio, travessa diagonal e pregos.</summary>
    static void Crate(int x, int y)
    {
        const int T = K.T;
        var wood = Hex(0xb07a3a); var dark = Hex(0x6e4620); var light = Hex(0xd6a060); var nail = Hex(0xc8c4bc);
        Rect(x, y, T, T, dark);
        Rect(x + 1, y + 1, T - 2, T - 2, Mul(wood, 0.92f));
        // tabuas de dentro com veio
        for (int i = 0; i < 3; i++)
        {
            int py = y + 3 + i * 3;
            Rect(x + 3, py, T - 6, 2, Mul(wood, 0.96f + i * 0.03f));
            Rect(x + 3, py + 2, T - 6, 1, Mul(wood, 0.72f));
            Px(x + 5 + i * 2, py, Mul(wood, 0.82f)); Px(x + 9 - i, py + 1, Mul(wood, 1.08f));
        }
        // moldura
        Rect(x + 1, y + 1, T - 2, 2, wood); Rect(x + 1, y + 1, T - 2, 1, light);
        Rect(x + 1, y + T - 3, T - 2, 2, Mul(wood, 0.88f)); Rect(x + 1, y + T - 2, T - 2, 1, Mul(wood, 0.7f));
        Rect(x + 1, y + 3, 2, T - 6, Mul(wood, 1.05f)); Px(x + 1, y + 3, light);
        Rect(x + T - 3, y + 3, 2, T - 6, Mul(wood, 0.84f));
        // travessa diagonal
        for (int i = 0; i < 9; i++)
        {
            Rect(x + 3 + i, y + 11 - i, 2, 1, light);
            Px(x + 3 + i, y + 12 - i, Mul(wood, 0.7f));
        }
        foreach (var (nx, ny) in new[] { (2, 2), (13, 2), (2, 13), (13, 13) }) Px(x + nx, y + ny, nail);
    }

    /// <summary>Rachaduras (camada sobre o bloco): linha escura com borda clara de lasca; no estagio 3 faltam pedacos.</summary>
    static void Cracks(int x, int y, int dmg)
    {
        var d = A(Hex(0x000000), 0.6f); var l = A(Hex(0xffffff), 0.22f);
        void Line((int, int)[] pts) { foreach (var (px, py) in pts) { Rect(x + px, y + py, 1, 1, d); Rect(x + px + 1, y + py, 1, 1, l); } }
        Line(new[] { (7, 2), (7, 3), (8, 4), (8, 5), (7, 6), (8, 7), (9, 8) });
        if (dmg > 1)
        {
            Line(new[] { (9, 8), (10, 9), (11, 9), (12, 10), (6, 7), (5, 8), (4, 9), (3, 9) });
            Line(new[] { (9, 9), (9, 10), (8, 11), (8, 12) });
        }
        if (dmg > 2)
        {
            Line(new[] { (12, 2), (11, 3), (12, 4), (13, 5), (2, 4), (3, 5), (3, 6), (4, 13), (5, 12), (6, 13) });
            Rect(x + 7, y + 8, 2, 2, A(Hex(0x000000), 0.75f));
            Rect(x + 12, y + 11, 2, 1, A(Hex(0x000000), 0.75f));
            Rect(x + 2, y + 2, 1, 2, A(Hex(0x000000), 0.7f));
        }
    }

    // ------------------------------------------------------------------ paredes de fundo (atras dos tiles)

    /// <summary>Tom de fundo: escurece e esfria a cor, para a frente sempre se destacar.</summary>
    static Color Deep(Color c, float k) => Col.Lerp(Mul(c, k), Hex(0x14121e), 0.28f);

    /// <summary>fundo_&lt;era&gt;.png (64x32): linha 0 = terra/caverna (tuneis, buracos), linha 1 = parede de
    /// construcao (atras de escadas, torres e bunkers). 4 variacoes cada; emendam sem costura.</summary>
    static void BackTiles(Era e)
    {
        const int T = K.T;
        for (int v = 0; v < 4; v++)
        {
            BackEarth(v * T, 0, e, v);
            BackWall(v * T, T, e, v);
        }
    }

    static void BackEarth(int x, int y, Era e, int v)
    {
        const int T = K.T;
        switch (e.Style)
        {
            case BgStyle.City:
            {
                // galeria de esgoto: tijolinhos escuros, cano, limo escorrendo, grade
                BackBricks(x, y, Deep(e.Brick, 0.42f), Deep(e.BrickDark, 0.36f), v);
                if (v == 1) { Rect(x, y + 5, T, 3, Deep(Hex(0x7a5a3a), 0.7f)); Rect(x, y + 5, T, 1, Deep(Hex(0xa07a4a), 0.7f)); Rect(x + 6, y + 4, 2, 5, Deep(Hex(0x5a4030), 0.7f)); }
                if (v == 2) { Rect(x + 4, y, 2, 11, Deep(Hex(0x3a5a3a), 0.55f)); Rect(x + 5, y + 11, 1, 2, Deep(Hex(0x3a5a3a), 0.55f)); }
                if (v == 3) { Rect(x + 4, y + 4, 8, 8, Deep(Hex(0x1a1a20), 1f)); for (int i = 0; i < 4; i++) Rect(x + 5 + i * 2, y + 4, 1, 8, Deep(Hex(0x5a5a64), 0.7f)); }
                return;
            }
            case BgStyle.Medieval:
            {
                // catacumba: blocos grandes de pedra
                var st = Deep(e.Brick, 0.5f); var mo = Deep(e.BrickDark, 0.4f);
                Rect(x, y, T, T, mo);
                foreach (var (bx, by, bw, bh) in new[] { (0, 0, 9, 7), (10, 0, 6, 7), (0, 8, 5, 7), (6, 8, 10, 7) })
                {
                    Rect(x + bx, y + by, bw, bh, st);
                    Rect(x + bx, y + by, bw, 1, Mul(st, 1.15f)); Rect(x + bx, y + by + bh - 1, bw, 1, Mul(st, 0.85f));
                }
                if (v == 1) { Rect(x + 8, y + 9, 5, 4, Deep(Hex(0x0a0a10), 1f)); Rect(x + 9, y + 10, 3, 2, Deep(Hex(0xd8d0bc), 0.55f)); }   // nicho com cranio
                if (v == 2) { Px(x + 3, y + 2, mo); Px(x + 4, y + 3, mo); Px(x + 4, y + 4, mo); Px(x + 5, y + 5, mo); }
                if (v == 3) { Rect(x + 11, y + 2, 3, 3, Deep(Hex(0x5a5a64), 0.8f)); Px(x + 12, y + 3, Deep(Hex(0x14121e), 1f)); }      // argola de ferro
                return;
            }
            case BgStyle.Future:
            {
                // painel metalico escuro com emendas
                var p = Deep(e.Steel, 0.32f);
                Rect(x, y, T, T, p);
                Rect(x, y, T, 1, Mul(p, 1.3f)); Rect(x, y, 1, T, Mul(p, 1.15f)); Rect(x + T - 1, y, 1, T, Mul(p, 0.75f)); Rect(x, y + T - 1, T, 1, Mul(p, 0.75f));
                if (v == 1) Rect(x + 2, y + 7, T - 4, 1, Deep(e.Top, 0.5f));
                if (v == 2) for (int i = 0; i < 4; i++) Rect(x + 4, y + 4 + i * 2, 8, 1, Mul(p, 0.6f));
                if (v == 3) foreach (var (rx, ry) in new[] { (2, 2), (13, 2), (2, 13), (13, 13) }) Px(x + rx, y + ry, Mul(p, 1.5f));
                return;
            }
        }
        // terra de caverna (selva e jurassico): rocha irregular em 3 tons
        var b0 = Deep(e.DirtDark, 0.62f); var b1 = Deep(e.Dirt, 0.52f); var b2 = Deep(e.Dirt, 0.64f); var sh = Deep(e.DirtDark, 0.42f);
        Rect(x, y, T, T, b0);
        if (e.Style == BgStyle.Dino)
        {
            // estratos de rocha ondulados
            for (int px = 0; px < T; px++)
            {
                int w = (int)MathF.Round(MathF.Sin((px + v * 5) * MathF.PI / 8f));
                Px(x + px, y + 4 + w, b1); Px(x + px, y + 5 + w, sh);
                Px(x + px, y + 11 - w, b1); Px(x + px, y + 12 - w, sh);
            }
            if (v == 1) { Rect(x + 6, y + 6, 4, 4, Deep(Hex(0xd8c8a0), 0.5f)); Px(x + 7, y + 7, b0); Px(x + 8, y + 8, b0); }  // amonite
            if (v == 2) { Rect(x + 3, y + 8, 2, 2, Deep(Hex(0xe0902a), 0.7f)); Px(x + 3, y + 8, Deep(Hex(0xffc060), 0.75f)); } // ambar
            return;
        }
        foreach (var (bx, by, bw, bh) in new[] { (1, 1, 6, 5), (9, 2, 6, 4), (3, 9, 7, 5), (11, 9, 4, 5) })
        {
            int ox = (int)(Hash.H(bx + v * 7, by) % 2);
            Rect(x + bx + ox, y + by, bw - 1, bh, b1);
            Rect(x + bx + ox, y + by, bw - 2, 1, b2);
            Rect(x + bx + ox + 1, y + by + bh, bw - 1, 1, sh);
        }
        if (v == 1)
        {
            // raiz pendurada
            var root = Deep(Hex(0x8a6a40), 0.7f);
            int[] path = { 6, 6, 7, 7, 7, 6, 6, 5, 5, 6, 6, 7 };
            for (int i = 0; i < path.Length; i++) Px(x + path[i], y + i, root);
            Px(x + 8, y + 5, root); Px(x + 9, y + 6, root);
        }
        if (v == 2) foreach (var (mx, my) in new[] { (2, 1), (3, 1), (10, 2), (11, 2), (4, 9) }) Px(x + mx, y + my, Deep(e.Top, 0.5f));   // musgo
        if (v == 3) { Px(x + 7, y + 3, sh); Px(x + 8, y + 4, sh); Px(x + 8, y + 5, sh); Px(x + 9, y + 6, sh); }
    }

    static void BackWall(int x, int y, Era e, int v)
    {
        const int T = K.T;
        if (e.Style == BgStyle.Future)
        {
            var p = Deep(e.Steel, 0.42f);
            Rect(x, y, T, T, p); Rect(x, y, T, 1, Mul(p, 1.3f)); Rect(x, y + 7, T, 1, Mul(p, 0.7f)); Rect(x, y + 8, T, 1, Mul(p, 1.15f));
            if (v == 1) Rect(x + 3, y + 3, 10, 2, Deep(e.Top, 0.5f));
            if (v == 2) Rect(x + 5, y + 10, 6, 4, Mul(p, 0.6f));
            return;
        }
        BackBricks(x, y, Deep(e.Brick, 0.5f), Deep(e.BrickDark, 0.42f), v);
        var hole = Deep(Hex(0x0c0a14), 1f);
        if (v == 1) { Rect(x + 6, y + 3, 4, 9, hole); Rect(x + 6, y + 3, 4, 1, Deep(e.Brick, 0.38f)); }       // seteira / janela estreita
        if (v == 2) { Px(x + 9, y + 2, hole); Px(x + 8, y + 3, hole); Px(x + 8, y + 4, hole); Px(x + 7, y + 5, hole); Px(x + 7, y + 6, hole); }   // rachadura
        if (v == 3)
        {
            if (e.Style == BgStyle.Jungle) foreach (var (mx, my) in new[] { (1, 0), (2, 0), (2, 1), (12, 8), (13, 8) }) Px(x + mx, y + my, Deep(e.Top, 0.55f));
            else if (e.Style == BgStyle.City) Pixo(x + 2, y + 5, 3, 77);
            else Rect(x + 4, y + 9, 8, 2, Deep(e.BrickDark, 0.3f));
        }
    }

    /// <summary>Tijolinhos de fundo (4 fileiras de 4px), alternando o encaixe; v varia o tom de alguns tijolos.</summary>
    static void BackBricks(int x, int y, Color brick, Color mortar, int v)
    {
        const int T = K.T;
        Rect(x, y, T, T, mortar);
        for (int row = 0; row < 4; row++)
        {
            int off = (row % 2) * 4;
            for (int bx = -off; bx < T; bx += 8)
            {
                int x0 = Math.Max(0, bx), x1 = Math.Min(T, bx + 7);
                if (x1 <= x0) continue;
                var bc = Mul(brick, 0.92f + 0.14f * Hash.F(bx + row * 5 + v * 11, 33));
                Rect(x + x0, y + row * 4, x1 - x0, 3, bc);
                Rect(x + x0, y + row * 4, x1 - x0, 1, Mul(bc, 1.12f));
            }
        }
    }

    // ------------------------------------------------------------------ cidade grande (Sao Paulo em caos)
    // Direcao de arte: cidade ao entardecer em pixel art detalhada. Ceu azul profundo com nuvens grandes
    // iluminadas de rosa/laranja (transicoes pontilhadas), skyline anil ao fundo, predios com reboco laranja,
    // tijolo e concreto paulistano, molduras de janela com reflexo, sacadas, cornijas, escadas de incendio e
    // caixas d'agua azuis; arvores atras de um muro de concreto com postes de luz.
    // O caos: janelas quebradas e em chamas com fuligem, colunas de fumaca, pixo no alto dos predios.
    // Sem contornos pretos e sem brilhos: so cor e sombreamento.

    const int Street = 124;     // linha da rua nas camadas de fundo (o chao do jogo cobre daqui para baixo)

    /// <summary>Retangulo pontilhado (xadrez): transicao entre dois tons, como na pixel art classica.</summary>
    static void Dither(int x, int y, int w, int h, Color c, int phase = 0)
    {
        for (int py = 0; py < h; py++)
            for (int px = 0; px < w; px++)
                if (((x + px + y + py + phase) & 1) == 0) Px(x + px, y + py, c);
    }

    /// <summary>Circulo em pixel art, opcionalmente pontilhado e cortado embaixo (maxY).</summary>
    static void Disc(int cx, int cy, int r, Color c, bool dither = false, int maxY = int.MaxValue)
    {
        for (int dy = -r; dy <= r; dy++)
        {
            int y = cy + dy;
            if (y > maxY) break;
            int half = (int)MathF.Sqrt(r * r - dy * dy + r * 0.6f);
            if (!dither) { Rect(cx - half, y, half * 2 + 1, 1, c); continue; }
            for (int x = cx - half; x <= cx + half; x++) if (((x + y) & 1) == 0) Px(x, y, c);
        }
    }

    /// <summary>Nuvem de entardecer: base escura e reta, corpo rosado, topo iluminado em laranja/pessego.</summary>
    static void Cloud(int cx, int cy, int size, int seed)
    {
        var dark = Hex(0x4e3456); var body = Hex(0x8a5270); var lit = Hex(0xd47a6c); var hi = Hex(0xf2ac88);
        int baseY = cy + size / 3;
        int n = 5 + size / 5;
        var blobs = new List<(int x, int y, int r)>();
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            int bx = cx - size + (int)(t * size * 2) + (int)(Hash.F(i, seed) * 6) - 3;
            int r = (int)(size * (0.32f + 0.38f * MathF.Sin(t * MathF.PI)) + Hash.F(i, seed + 1) * 4);
            int by = baseY - r + 2 + (int)(Hash.F(i, seed + 2) * 3);
            blobs.Add((bx, by, r));
        }
        foreach (var (x, y, r) in blobs) Disc(x, y, r, dark, false, baseY);
        foreach (var (x, y, r) in blobs) Disc(x, y - 2, r - 1, body, false, baseY - 3);
        foreach (var (x, y, r) in blobs) Disc(x - 1, y - 3, r - 1, lit, true, baseY - 4);
        foreach (var (x, y, r) in blobs) Disc(x - r / 5, y - r / 3, (int)(r * 0.72f), lit, false, baseY - 6);
        foreach (var (x, y, r) in blobs) Disc(x - r / 4, y - r / 2, (int)(r * 0.5f), hi, true);
        foreach (var (x, y, r) in blobs) Disc(x - r / 3, y - r / 2 - 1, (int)(r * 0.3f), hi);
    }

    /// <summary>Coluna de fumaca de incendio: bolhas escuras que sobem, alargam e tombam com o vento.</summary>
    static void Smoke(int x, int baseY, int height, int seed)
    {
        var dark = Hex(0x2e2832); var body = Hex(0x433a44); var lit = Hex(0x5e5058);
        int steps = Math.Max(3, height / 7);
        for (int i = steps - 1; i >= 0; i--)
        {
            float t = i / (float)steps;
            int r = 3 + (int)(t * 11) + (int)(Hash.F(i, seed) * 3);
            int cx = x + (int)(t * t * 46) + (int)(Hash.F(i, seed + 1) * 4) - 2;
            int cy = baseY - (int)(t * height);
            Disc(cx, cy + 1, r, dark);
            Disc(cx, cy, r - 1, body);
            Disc(cx - r / 3, cy - r / 3, r / 2, lit, true);
        }
    }

    static void CitySky(Era e)
    {
        // ceu em faixas: azul profundo em cima, clareando perto do horizonte
        Color[] stops = { Hex(0x18244c), Hex(0x223464), Hex(0x2e4a82), Hex(0x3e62a0), Hex(0x5a80b8), Hex(0x86a2c8), Hex(0xb8b4c4) };
        int bands = 16;
        for (int i = 0; i < bands; i++)
        {
            float t = i / (float)(bands - 1) * (stops.Length - 1);
            int a = (int)t; float f = t - a;
            var c = Col.Lerp(stops[a], stops[Math.Min(stops.Length - 1, a + 1)], f);
            int y0 = i * 150 / bands, y1 = (i + 1) * 150 / bands;
            Rect(0, y0, K.W, y1 - y0, c);
            if (i > 0) Dither(0, y0, K.W, 1, Col.Lerp(stops[Math.Max(0, a - 1)], c, 0.5f));   // costura pontilhada
        }
        Rect(0, 150, K.W, K.H - 150, stops[^1]);
        // estrelas fracas no alto
        for (int i = 0; i < 18; i++) Px((int)(Hash.F(i, 501) * K.W), (int)(Hash.F(i, 502) * 40), Hex(0x8a9ac8));
        // fumaca de incendios atras da cidade
        Smoke(96, 150, 120, 11);
        Smoke(214, 150, 95, 12);
        // nuvens grandes de entardecer
        Cloud(36, 34, 40, 21);
        Cloud(282, 58, 34, 22);
        Cloud(170, 20, 15, 23);
    }

    /// <summary>Skyline anil ao fundo, com janelas fracas, antenas da Paulista (luz vermelha) e silhuetas
    /// inspiradas em predios famosos de Sao Paulo. Repete sem emenda a cada 960px.</summary>
    static void CityFar(Era e)
    {
        var wall = e.Far; var lit = Mul(e.Far, 1.22f); var dark = Mul(e.Far, 0.84f); var win = Mul(e.Far, 1.38f);
        var warm = Hex(0xc0906a);
        Rect(0, Street, BgLoop, K.H - Street, wall);
        void Tower(int x, int w, int h, int b)
        {
            int top = Street - h;
            Rect(x, top, w, h, wall);
            Rect(x, top, 1, h, lit);
            Rect(x + w - 1, top, 1, h, dark);
            for (int wy = top + 4; wy < Street; wy += 4)
                for (int wx = x + 2; wx < x + w - 2; wx += 3)
                {
                    float v = Hash.F(wx * 7 + b, wy);
                    if (v > 0.93f) Px(wx, wy, warm); else if (v > 0.5f) Px(wx, wy, win);
                }
            float k = Hash.F(b, 74);
            if (k > 0.72f) { int ah = 10 + (int)(Hash.F(b, 75) * 16); Rect(x + w / 2, top - ah, 1, ah, dark); Px(x + w / 2, top - ah, Hex(0xff3a2a)); }
            else if (k > 0.45f) { Rect(x + 2, top - 4, 5, 4, dark); Rect(x + 3, top - 5, 3, 1, dark); }
            else if (k > 0.3f && w > 14) { Rect(x + 3, top - 6, w - 6, 6, wall); Rect(x + 3, top - 6, 1, 6, lit); }
        }
        int cx = 0, bi = 0;
        while (cx < BgLoop)
        {
            int w = 12 + (int)(Hash.F(bi, 71) * 22), h = 44 + (int)(Hash.F(bi, 72) * 62);
            if (BgLoop - cx - w < 12) w = BgLoop - cx;
            Tower(cx, w, h, bi);
            cx += w;
            bi++;
        }
        // marcos: torre escalonada com agulha, lamina alta e o predio largo de faixas onduladas
        Tower(300, 26, 104, 900); Tower(304, 18, 116, 901); Tower(308, 10, 126, 902);
        Rect(312, Street - 146, 2, 20, dark); Px(312, Street - 146, Hex(0xff3a2a));
        Tower(560, 14, 122, 903);
        for (int px = 0; px < 56; px++)
        {
            int top = Street - 86 + (int)(MathF.Sin(px / 9f) * 3);
            Rect(720 + px, top, 1, Street - top, wall); Px(720 + px, top, lit);
            for (int wy = top + 3; wy < Street; wy += 3) Px(720 + px, wy + (int)(MathF.Sin(px / 9f) * 2), dark);
        }
    }

    // ---------------------------------------------------------------- predios do plano do meio

    /// <summary>Janela com moldura e vidro. state: 0 vidro, 1 acesa, 2 quebrada, 3 em chamas, 4 cortina.</summary>
    static void Window(int x, int y, int w, int h, int state, Color frame)
    {
        Rect(x - 1, y - 1, w + 2, h + 2, frame);
        Rect(x - 2, y + h + 1, w + 4, 1, Mul(frame, 1.1f));                 // peitoril
        Rect(x - 2, y + h + 2, w + 4, 1, A(Hex(0x000000), 0.25f));          // sombra do peitoril
        switch (state)
        {
            case 1:
                Rect(x, y, w, h, Hex(0xf0cc80)); Rect(x, y, w, 2, Hex(0xfff0c0));
                Rect(x, y + h - 2, w, 2, Hex(0xd8a860));
                break;
            case 2:
                Rect(x, y, w, h, Hex(0x1c1a24));
                Px(x, y, Hex(0xc8d4e0)); Px(x + 1, y, Hex(0x9aaabb)); Px(x, y + 1, Hex(0x9aaabb));
                Px(x + w - 1, y + h - 1, Hex(0xc8d4e0)); Px(x + w - 2, y + h - 1, Hex(0x8a9aab));
                break;
            case 3:
                // fogo saindo da janela, fuligem por cima
                Dither(x - 1, y - 7, w + 2, 6, Hex(0x2a2026));
                Rect(x, y - 2, w, 1, Hex(0x2a2026));
                Rect(x, y, w, h, Hex(0x3a1a14));
                Rect(x, y + h / 2, w, h - h / 2, Hex(0xe0501e));
                for (int i = 0; i < w; i++)
                {
                    int tongue = 2 + (int)(Hash.F(x + i, y) * (h / 2 + 3));
                    Rect(x + i, y + h - tongue - h / 3, 1, tongue, i % 2 == 0 ? Hex(0xf07a26) : Hex(0xe0501e));
                    if (i % 2 == 1) Rect(x + i, y + h - tongue / 2 - h / 4, 1, Math.Max(1, tongue / 2), Hex(0xffc23a));
                }
                Rect(x + w / 2 - 1, y - 3, 2, 3, Hex(0xf07a26)); Px(x + w / 2, y - 4, Hex(0xffc23a));
                break;
            case 4:
                Rect(x, y, w, h, Hex(0x3a5482)); Rect(x, y, w / 2, h, Hex(0xc8a070)); Rect(x + w / 2 - 1, y, 1, h, Hex(0xa88050));
                break;
            default:
                Rect(x, y, w, h, Hex(0x3e5c8e));
                Rect(x, y + h / 2, w, h - h / 2, Hex(0x34507e));
                Px(x, y, Hex(0xa8c4e4)); Px(x + 1, y, Hex(0x88a8d0)); Px(x, y + 1, Hex(0x88a8d0));
                Px(x + 2, y + 3, Hex(0x6a8cbc)); Px(x + 1, y + 4, Hex(0x6a8cbc));
                break;
        }
        if (w >= 6 && state != 3) Rect(x + w / 2, y, 1, h, frame);           // caixilho
        if (h >= 8 && state != 3) Rect(x, y + h / 2 - 1, w, 1, frame);
    }

    /// <summary>Pixacao paulistana: letras altas, finas e pontudas (estilo runico), 3x6 cada, em preto.</summary>
    static void Pixo(int x, int y, int letters, int seed)
    {
        string[][] glyphs =
        {
            new[] { "X.X", "X.X", "XXX", "X.X", "X.X", "X.X" },
            new[] { "XXX", "X..", "X..", "XX.", "X..", "X.." },
            new[] { "X..", "X..", "X..", "X..", "X..", "XXX" },
            new[] { ".X.", "X.X", "X.X", "XXX", "X.X", "X.X" },
            new[] { "X.X", "XXX", "X.X", "X.X", "X.X", "X.X" },
            new[] { "XXX", ".X.", ".X.", ".X.", ".X.", ".X." },
            new[] { "XX.", "X.X", "XX.", "X.X", "X.X", "XX." },
            new[] { "X.X", "X.X", "X.X", ".X.", ".X.", ".X." },
        };
        var ink = Hex(0x18161c);
        for (int i = 0; i < letters; i++)
        {
            var g = glyphs[(int)(Hash.F(i, seed) * glyphs.Length) % glyphs.Length];
            int lx = x + i * 4;
            for (int gy = 0; gy < 6; gy++)
                for (int gx = 0; gx < 3; gx++)
                    if (g[gy][gx] == 'X') Px(lx + gx, y + gy, ink);
            if (Hash.F(i, seed + 1) > 0.6f) Px(lx, y - 1, ink);          // ponta para cima
        }
        Rect(x, y + 7, letters * 4 - 1, 1, ink);                            // sublinhado tipico
    }

    static int WindowState(int seed)
    {
        float v = Hash.F(seed, 311);
        return v > 0.9f ? 3 : v > 0.78f ? 2 : v > 0.62f ? 1 : v > 0.52f ? 4 : 0;
    }

    /// <summary>Torre de reboco laranja (como na referencia): andares com faixas, janelas com sacada,
    /// cornija clara, casinha de telhado com antena e escada de incendio preta na lateral.</summary>
    static void StuccoTower(int x0, int w, int top, int seed, List<(int x, int y)> fires)
    {
        var b = Hex(0xcc7448); var lt = Hex(0xe6946a); var sh = Hex(0xa4583a); var trim = Hex(0xf2c498); var dk = Hex(0x6e3a2a);
        bool escape = w >= 44;
        int bw = escape ? w - 12 : w;
        Rect(x0, top, bw, Street - top, b);
        Dither(x0 + 3, top + 2, bw - 6, Street - top - 2, Mul(b, 0.96f), seed & 1);
        Rect(x0, top, 2, Street - top, lt); Rect(x0 + bw - 3, top, 3, Street - top, sh);
        int floorH = 17, f = 0;
        for (int fy = top + 8; fy < Street - 10; fy += floorH, f++)
        {
            Rect(x0, fy - 3, bw, 1, trim); Rect(x0, fy - 2, bw, 1, sh);
            int n = Math.Max(1, (bw - 6) / 12);
            int gap = (bw - n * 7) / (n + 1);
            for (int i = 0; i < n; i++)
            {
                int wx = x0 + gap + i * (7 + gap), st = WindowState(seed * 31 + f * 7 + i);
                Window(wx, fy + 1, 7, 9, st, trim);
                if (st == 3) fires.Add((wx + 3, fy - 6));
                if ((f + i) % 2 == 0)
                {
                    // sacadinha com grade
                    Rect(wx - 2, fy + 10, 11, 1, trim);
                    for (int k = 0; k < 11; k += 2) Px(wx - 2 + k, fy + 9, trim);
                    Rect(wx - 2, fy + 8, 11, 1, trim);
                }
            }
        }
        // cornija
        Rect(x0 - 1, top - 3, bw + 2, 3, trim); Rect(x0 - 1, top - 3, bw + 2, 1, Hex(0xffe4c4)); Rect(x0 - 1, top, bw + 2, 1, dk);
        // casinha do telhado com telhado em ponta e antena
        int rw = Math.Max(10, bw / 2), rx = x0 + (bw - rw) / 2, rt = top - 12;
        Rect(rx, rt, rw, 9, b); Rect(rx, rt, 1, 9, lt); Rect(rx + rw - 1, rt, 1, 9, sh);
        Window(rx + rw / 2 - 2, rt + 3, 4, 4, WindowState(seed + 99) == 1 ? 1 : 0, trim);
        for (int i = 0; i <= rw / 2; i++) Rect(rx - 1 + i, rt - 1 - i / 2, rw + 2 - i * 2, 1, i == 0 ? trim : Mul(dk, 1.2f - i * 0.02f));
        Rect(rx + rw / 2, rt - rw / 4 - 8, 1, 7, dk);
        // escada de incendio
        if (escape)
        {
            var iron = Hex(0x231f28); int ex = x0 + bw;
            for (int fy = top + 8; fy < Street - 6; fy += floorH)
            {
                Rect(ex, fy + 9, 11, 1, iron);                                   // plataforma
                for (int k = 0; k < 11; k += 2) Px(ex + k, fy + 6, iron);        // grade
                Rect(ex, fy + 5, 11, 1, iron);
                Rect(ex + 10, fy + 5, 1, 5, iron);
                for (int k = 0; k < 9; k++) Px(ex + 1 + k, fy + 9 + (int)(k * (floorH - 1) / 9f), iron);   // escada diagonal
            }
        }
    }

    /// <summary>Predio de tijolo (como os sobrados da referencia): janelas altas com verga de pedra,
    /// ar-condicionado, cornija com dentes e caixa d'agua azul no telhado.</summary>
    static void BrickBlock(int x0, int w, int top, int seed, List<(int x, int y)> fires)
    {
        var b = Hex(0x9a4a3a); var mortar = Hex(0x7a382c); var lt = Hex(0xb8604a); var sh = Hex(0x6c3226); var stone = Hex(0xdcc8ac);
        Rect(x0, top, w, Street - top, b);
        for (int y = top + 2; y < Street; y += 3)
        {
            Dither(x0, y, w, 1, mortar, y & 1);
            for (int x = x0 + ((y / 3) % 2) * 3; x < x0 + w; x += 6) Px(x, y + 1, mortar);
        }
        Rect(x0, top, 2, Street - top, lt); Rect(x0 + w - 2, top, 2, Street - top, sh);
        int floorH = 19, f = 0;
        for (int fy = top + 9; fy < Street - 12; fy += floorH, f++)
        {
            int n = Math.Max(1, (w - 8) / 14), gap = (w - n * 7) / (n + 1);
            for (int i = 0; i < n; i++)
            {
                int wx = x0 + gap + i * (7 + gap), st = WindowState(seed * 17 + f * 5 + i);
                Rect(wx - 2, fy - 3, 11, 2, stone); Rect(wx - 2, fy - 3, 11, 1, Hex(0xf0e2cc));   // verga
                Window(wx, fy, 7, 11, st, Hex(0xe8e0d0));
                if (st == 3) fires.Add((wx + 3, fy - 8));
                if (st == 0 && Hash.F(seed + f, i + 40) > 0.6f)
                {
                    // ar-condicionado pendurado
                    Rect(wx + 1, fy + 9, 6, 4, Hex(0xb8bcc4)); Rect(wx + 1, fy + 9, 6, 1, Hex(0xd8dce4));
                    Rect(wx + 2, fy + 11, 4, 1, Hex(0x7a7e88));
                }
            }
        }
        // cornija com dentes
        Rect(x0 - 2, top - 5, w + 4, 5, stone); Rect(x0 - 2, top - 5, w + 4, 1, Hex(0xf2e6d2));
        for (int x = x0 - 1; x < x0 + w + 2; x += 2) Px(x, top - 2, Hex(0xa89478));
        Rect(x0 - 2, top, w + 4, 1, sh);
        // caixa d'agua azul (fibra), tipica dos telhados de Sao Paulo
        int tx = x0 + w - 16;
        Rect(tx + 1, top - 9, 1, 4, Hex(0x3a3640)); Rect(tx + 8, top - 9, 1, 4, Hex(0x3a3640));
        Rect(tx, top - 17, 10, 8, Hex(0x2e6ab8)); Rect(tx, top - 17, 2, 8, Hex(0x5a94d8)); Rect(tx + 8, top - 17, 2, 8, Hex(0x22508e));
        Rect(tx - 1, top - 18, 12, 2, Hex(0x22508e)); Rect(tx - 1, top - 18, 12, 1, Hex(0x3a78c8));
        // chamine
        Rect(x0 + 6, top - 10, 5, 5, sh); Rect(x0 + 5, top - 11, 7, 2, Hex(0x5a2a20));
    }

    /// <summary>Predio de concreto paulistano: janelas em fita, manchas escorridas, brises e pixo na platibanda.</summary>
    static void ConcreteBlock(int x0, int w, int top, int seed, List<(int x, int y)> fires)
    {
        var b = Hex(0x8c8a88); var lt = Hex(0xaeaca8); var sh = Hex(0x666462); var stain = Hex(0x74716e);
        Rect(x0, top, w, Street - top, b);
        Rect(x0, top, 2, Street - top, lt); Rect(x0 + w - 2, top, 2, Street - top, sh);
        int f = 0;
        for (int fy = top + 10; fy < Street - 8; fy += 10, f++)
        {
            // fita de janelas com montantes
            Rect(x0 + 3, fy, w - 6, 5, Hex(0x2e3a52));
            Dither(x0 + 3, fy, w - 6, 2, Hex(0x5a7aa4), f & 1);
            for (int mx = x0 + 3; mx < x0 + w - 3; mx += 5) Rect(mx, fy, 1, 5, Hex(0x9a9894));
            Rect(x0 + 2, fy + 5, w - 4, 1, lt);
            int seg = (int)(Hash.F(seed, f) * Math.Max(1, (w - 10) / 5));
            float v = Hash.F(seed + 3, f);
            if (v > 0.75f) Rect(x0 + 4 + seg * 5, fy + 1, 4, 3, Hex(0xf0cc80));
            else if (v < 0.18f)
            {
                int fx0 = x0 + 4 + seg * 5;
                Rect(fx0, fy + 2, 4, 3, Hex(0xe0501e)); Rect(fx0 + 1, fy, 2, 2, Hex(0xffc23a));
                Dither(fx0 - 1, fy - 6, 6, 5, Hex(0x2a2026));
                fires.Add((fx0 + 2, fy - 7));
            }
            // manchas de escorrido sob a janela
            if (Hash.F(seed + 7, f) > 0.4f) Dither(x0 + 5 + (int)(Hash.F(seed + 9, f) * (w - 12)), fy + 6, 2, 3, stain);
        }
        // brises verticais numa faixa lateral
        for (int y = top + 8; y < Street; y += 2) { Px(x0 + w - 6, y, lt); Px(x0 + w - 5, y, sh); }
        // platibanda com pixo (letras retas e pontudas, tipicas de Sao Paulo)
        Rect(x0 - 1, top - 7, w + 2, 7, Hex(0x9a9894)); Rect(x0 - 1, top - 7, w + 2, 1, Hex(0xbab8b4)); Rect(x0 - 1, top, w + 2, 1, sh);
        Pixo(x0 + 2, top - 6, (w - 6) / 4, seed);
        // antena com luz vermelha e parabolica
        Rect(x0 + w / 3, top - 26, 1, 19, Hex(0x3a3842)); Px(x0 + w / 3, top - 26, Hex(0xff3a2a));
        Rect(x0 + w / 3 - 2, top - 20, 5, 1, Hex(0x3a3842));
        Rect(x0 + w - 12, top - 11, 5, 3, Hex(0xd0d0d0)); Rect(x0 + w - 10, top - 8, 1, 1, Hex(0x5a5a5a));
    }

    /// <summary>Galpao baixo com telhado de zinco e um grafite colorido na fachada.</summary>
    static void LowBlock(int x0, int w, int top, int seed)
    {
        var b = Hex(0xc8a070); var lt = Hex(0xe0bc8a); var sh = Hex(0x9a7a52);
        Rect(x0, top, w, Street - top, b);
        Rect(x0, top, 2, Street - top, lt); Rect(x0 + w - 2, top, 2, Street - top, sh);
        for (int x = x0 - 1; x < x0 + w + 1; x++) Rect(x, top - 4, 1, 4, (x & 1) == 0 ? Hex(0x9aa0aa) : Hex(0x70767e));
        Rect(x0 - 1, top - 5, w + 2, 1, Hex(0xc8ced6));
        // grafite: formas coloridas (estilo dos muros de Sao Paulo)
        int gx = x0 + 6, gy = top + 6;
        Disc(gx + 6, gy + 6, 5, Hex(0xf0c020)); Disc(gx + 14, gy + 7, 5, Hex(0x2aa0c8)); Disc(gx + 22, gy + 6, 4, Hex(0xe0405a));
        Rect(gx + 5, gy + 4, 2, 2, Hex(0x18161c)); Rect(gx + 13, gy + 5, 2, 2, Hex(0x18161c));
        Rect(gx + 3, gy + 9, 22, 1, Mul(Hex(0xe0405a), 0.8f));
        Window(x0 + w - 12, top + 6, 6, 6, WindowState(seed), Hex(0xe8e0d0));
    }

    /// <summary>Arvore (copa redonda em 3 tons com pontilhado) espiando por cima do muro.</summary>
    static void Tree(int cx, int cy, int r, int seed)
    {
        var sh = Hex(0x26442c); var mid = Hex(0x386636); var lt = Hex(0x568c40); var hi = Hex(0x80b456);
        Rect(cx - 1, cy, 3, 22, Hex(0x4a3428));
        for (int i = 0; i < 4; i++)
        {
            int ox = (int)((Hash.F(i, seed) - 0.5f) * r * 1.4f), oy = (int)((Hash.F(i, seed + 1) - 0.5f) * r * 0.8f);
            int rr = r - 2 + (int)(Hash.F(i, seed + 2) * 4);
            Disc(cx + ox, cy + oy + 1, rr, sh);
        }
        for (int i = 0; i < 4; i++)
        {
            int ox = (int)((Hash.F(i, seed) - 0.5f) * r * 1.4f), oy = (int)((Hash.F(i, seed + 1) - 0.5f) * r * 0.8f);
            int rr = r - 2 + (int)(Hash.F(i, seed + 2) * 4);
            Disc(cx + ox - 1, cy + oy - 1, rr - 1, mid);
            Disc(cx + ox - rr / 3, cy + oy - rr / 3, rr / 2 + 1, lt, true);
            Disc(cx + ox - rr / 3, cy + oy - rr / 3 - 1, rr / 3, lt);
            Disc(cx + ox - rr / 2, cy + oy - rr / 2, rr / 4, hi, true);
        }
    }

    /// <summary>Plano do meio: predios detalhados, fumaca dos incendios, arvores, muro com grafite e postes.</summary>
    static void CityMid(Era e)
    {
        Rect(0, Street, BgLoop, K.H - Street, e.Mid);
        var fires = new List<(int x, int y)>();
        int x = 0, b = 0;
        while (x < BgLoop)
        {
            int kind = (int)(Hash.F(b, 201) * 4);
            int w = kind switch { 0 => 46 + (int)(Hash.F(b, 202) * 14), 1 => 64 + (int)(Hash.F(b, 203) * 30), 2 => 50 + (int)(Hash.F(b, 204) * 26), _ => 40 + (int)(Hash.F(b, 205) * 16) };
            int gap = Hash.F(b, 206) > 0.6f ? 4 + (int)(Hash.F(b, 207) * 8) : 0;
            if (BgLoop - x - w - gap < 44) { w = BgLoop - x; gap = 0; }
            int h = kind switch { 0 => 92 + (int)(Hash.F(b, 208) * 20), 1 => 62 + (int)(Hash.F(b, 209) * 22), 2 => 76 + (int)(Hash.F(b, 210) * 30), _ => 40 + (int)(Hash.F(b, 211) * 10) };
            int top = Street - h;
            switch (kind)
            {
                case 0: StuccoTower(x + 1, w - 2, top, b, fires); break;
                case 1: BrickBlock(x + 2, w - 4, top, b, fires); break;
                case 2: ConcreteBlock(x + 1, w - 2, top, b, fires); break;
                default: LowBlock(x + 1, w - 2, top, b); break;
            }
            x += w + gap;
            b++;
        }
        // fumaca subindo dos incendios
        for (int i = 0; i < fires.Count; i++)
            if (i % 2 == 0) Smoke(fires[i].x, fires[i].y, 26 + (int)(Hash.F(i, 220) * 22), 300 + i);
        // arvores atras do muro
        for (int tx = 18; tx < BgLoop - 10; tx += 46 + (int)(Hash.F(tx, 230) * 40))
            Tree(tx, 98 + (int)(Hash.F(tx, 231) * 4), 9 + (int)(Hash.F(tx, 232) * 4), tx);
        // muro de concreto com tampa, juntas, pilares, manchas e pixo/grafite
        var wc = Hex(0x9c9488); var wl = Hex(0xb8b0a2); var wd = Hex(0x7a746a); var cap = Hex(0xccc4b4);
        Rect(0, 106, BgLoop, Street - 106, wc);
        Rect(0, 104, BgLoop, 2, cap); Rect(0, 104, BgLoop, 1, Hex(0xe2dacb)); Rect(0, 106, BgLoop, 1, wd);
        Dither(0, Street - 4, BgLoop, 4, wd);
        for (int px = 0; px < BgLoop; px += 32) { Rect(px, 107, 1, Street - 107, wd); Rect(px + 1, 107, 1, Street - 107, wl); }
        for (int px = 16; px < BgLoop; px += 96)
        {
            Rect(px, 101, 6, Street - 101, wl); Rect(px, 101, 6, 1, Hex(0xe8e0d0)); Rect(px + 5, 102, 1, Street - 102, wd);
        }
        var ink = Hex(0x18161c);
        for (int px = 40; px < BgLoop - 30; px += 64 + (int)(Hash.F(px, 240) * 50))
        {
            if (Hash.F(px, 241) > 0.5f) Pixo(px, 110, 5 + (int)(Hash.F(px, 242) * 3), px);
            else
            {
                // throw-up colorido (letras gordas)
                Disc(px + 4, 114, 4, Hex(0xe0405a)); Disc(px + 11, 114, 4, Hex(0x2aa0c8)); Disc(px + 18, 114, 4, Hex(0xf0c020));
                Rect(px + 3, 113, 2, 2, Mul(Hex(0xe0405a), 0.6f)); Rect(px + 10, 113, 2, 2, Mul(Hex(0x2aa0c8), 0.6f));
            }
        }
        // calcada e asfalto embaixo do muro (aparecem quando a camera desce)
        Rect(0, Street, BgLoop, 4, Hex(0x8e8a84)); Rect(0, Street, BgLoop, 1, Hex(0xb4b0a8));
        for (int px = 0; px < BgLoop; px += 12) Px(px, Street + 2, Hex(0x6e6a64));
        Rect(0, Street + 4, BgLoop, 2, Hex(0x5a5650)); Rect(0, Street + 6, BgLoop, 1, Hex(0x3a3640));
        Dither(0, Street + 7, BgLoop, K.H - Street - 7, Mul(e.Mid, 1.12f));
        for (int px = 20; px < BgLoop; px += 40) Rect(px, Street + 16, 14, 1, Hex(0x8a7a4a));   // faixa amarela gasta
        // postes de luz (bracos duplos, lampada acesa sem brilho)
        var pole = Hex(0x2c2c36); var poleL = Hex(0x4a4a58);
        for (int px = 70; px < BgLoop; px += 160)
        {
            Rect(px, 66, 2, Street - 66, pole); Rect(px, 66, 1, Street - 66, poleL);
            Rect(px - 9, 66, 20, 1, pole);
            foreach (int hx in new[] { px - 10, px + 9 })
            {
                Rect(hx, 66, 4, 2, pole); Rect(hx + 1, 68, 2, 1, Hex(0xffe6a0));
            }
            Rect(px - 1, Street - 4, 4, 4, pole);
        }
    }

    /// <summary>carro.png: carros destruidos 32x16, base em (16, 15): 0 carcaca queimada, 1 taxi branco batido,
    /// 2 carro vermelho capotado de lado (amassado).</summary>
    static void CarArt()
    {
        for (int k = 0; k < 3; k++)
        {
            int x = k * 32;
            var body = k == 0 ? Hex(0x3a3634) : k == 1 ? Hex(0xd8d6ce) : Hex(0xa83228);
            var shade = Mul(body, 0.72f); var hi = Mul(body, 1.18f);
            var glass = k == 0 ? Hex(0x1c1a1e) : Hex(0x7a9ab0);
            var tire = Hex(0x1e1e22);
            // carroceria
            Rect(x + 2, 7, 28, 6, body); Rect(x + 2, 7, 28, 1, hi); Rect(x + 2, 12, 28, 1, shade);
            Rect(x + 8, 3, 14, 4, body); Rect(x + 8, 3, 14, 1, hi);
            Rect(x + 10, 4, 5, 3, glass); Rect(x + 16, 4, 5, 3, glass);
            if (k != 0) { Px(x + 11, 5, Hex(0xe0eef8)); Px(x + 17, 4, Hex(0x2a3a48)); Px(x + 18, 5, Hex(0x2a3a48)); Px(x + 19, 6, Hex(0x2a3a48)); }
            // farol, para-choque
            Rect(x + 28, 8, 2, 2, k == 0 ? Hex(0x2a2826) : Hex(0xf0e0a0)); Rect(x + 2, 8, 1, 2, Hex(0x8a2020));
            // rodas (pneu murcho na carcaca)
            foreach (int wx in new[] { 7, 23 })
            {
                Rect(x + wx - 2, k == 0 ? 13 : 12, 5, k == 0 ? 3 : 4, tire);
                Px(x + wx, 13, Hex(0x7a7a80));
            }
            // amassados e ferrugem
            if (k == 0)
            {
                Rect(x + 5, 9, 4, 2, Hex(0x7a4a2a)); Rect(x + 20, 10, 5, 1, Hex(0x7a4a2a)); Px(x + 14, 8, Hex(0x8a5a32));
                Rect(x + 8, 3, 3, 1, Hex(0x2a2826));
            }
            if (k == 1)
            {
                Rect(x + 12, 2, 6, 1, Hex(0xd8b830));              // placa de taxi no teto
                Rect(x + 24, 7, 6, 2, Mul(body, 0.8f)); Px(x + 26, 9, shade);   // frente amassada
                Rect(x + 4, 10, 22, 1, Hex(0xd8b830));
            }
            if (k == 2)
            {
                Rect(x + 20, 8, 3, 3, shade); Rect(x + 6, 7, 4, 1, shade);
                Rect(x + 15, 1, 2, 2, Hex(0x6a6a6a));              // fumacinha parada
            }
        }
    }

    // ------------------------------------------------------------------ cenarios

    static void Sky(Era e)
    {
        if (e.Style == BgStyle.City) { CitySky(e); return; }
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
        if (e.Style == BgStyle.City) { CityFar(e); return; }
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
        if (e.Style == BgStyle.City) { CityMid(e); return; }
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

> As variaveis de cada heroi (velocidade, dano, tiro, especial...) ficam no arquivo **HeroConfig.cs**, na pasta do projeto.

Tudo aqui e PNG comum: edite no Aseprite, Photoshop, Krita, Piskel, LibreSprite...
- O jogo le esta pasta ao abrir. Com o jogo aberto, aperte **F5** para recarregar a arte.
- Apague um arquivo para o jogo gerar de novo a versao original.
- Mantenha o tamanho das imagens e das celulas. Fundo transparente = vazio.
- Desenhe tudo **olhando para a direita**: o jogo espelha sozinho.

## sprites/herois e sprites/inimigos (soldado, bazuqueiro, brutamontes, faca, homem_bomba, escudeiro, granadeiro, atirador, lanca_chamas)
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

## sprites/inimigos/*_cao.png
6 quadros de 24x16, pes no pixel (12, 15), olhando para a direita: 0 parado, 1-4 correndo, 5 salto (bote).
Cada cenario tem o seu: pastor alemao (selva), raptor (jurassico), lobo (medieval), rottweiler (cidade) e
cao-robo (futuro).

## sprites/inimigos/*_torreta.png
4 quadros de 32x16, base no pixel (12, 15): 0 luz acesa, 1 luz apagada, 2 e 3 = mesmo com recuo do tiro.

## sprites/objetos
- **refem.png**: mesmo formato das folhas de personagem (o refem usa as linhas Parado e Comemorando = maos para cima, e Correndo ao fugir).
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

## Animacoes soltas dos herois (opcional)

Em vez de editar a folha `sprites/herois/NOME.png`, pode desenhar uma animacao frame a frame numa pasta:

    sprites/herois/batman/running/running0000.png, running0001.png, ...

- Um PNG por frame, todos do mesmo tamanho (ex.: 32x32), tocados por ordem alfabetica.
- O heroi olha para a DIREITA. Os pes ficam centrados na horizontal; a linha mais baixa desenhada e o chao.
- Fundo: transparente, ou uma cor solida (a cor do pixel do canto superior esquerdo vira transparente).
- Pastas aceites: stop ou idle (parado), running ou run (a correr), jumping ou jump (subindo no pulo),
  falling ou fall (caindo), climbing ou climb (escalando/escada), dash, hurt, cheer, tumble.
  A que nao existir continua a usar a folha. F5 recarrega.
- Arma principal: um PNG na pasta armour/ do heroi (ex.: sprites/herois/batman/armour/shuriken.png) substitui o
  projetil padrao desse heroi. A imagem e desenhada centrada no projetil e gira depois de lancada
  (velocidade em RotacaoArma, no HeroConfig.cs, graus por segundo). Fundo transparente.

## tiles/ponte.png, escada.png, concreto.png e parede.png

- ponte.png (64x32): linha de cima = corrimao de corda, desenhado no tile ACIMA da tabua
  (meio, poste da ponta esquerda, poste da ponta direita). Linha de baixo = tabua inteira e tabua estragada.
  So os 5px de cima da tabua contam como piso / alvo dos tiros.
- escada.png (32x16): escada (a segunda celula esta reservada). O topo da escada serve de piso. Escadas sao indestrutiveis.
- concreto.png (16x32): ponte de concreto. Celula de cima = tabuleiro; celula de baixo = pilar.
- parede.png (80x32): fundo de dentro das casas, uma coluna por era (selva, jurassico, medieval, cidade, futuro).
  Linha de cima = parede lisa; linha de baixo = parede com janela (usada na fileira de cima da sala).

## tiles/telhado.png
- 64x80: uma linha por era (selva, jurassico, medieval, cidade, futuro), celulas de 16x16.
- Colunas: 0 ponta esquerda (beiral), 1 meio, 2 ponta direita, 3 enfeite desenhado no bloco ACIMA do telhado
  (chamine, ossos, antena de TV, parabolica), que aparece sozinho em alguns blocos do meio.
- Na cidade e telha colonial de barro; no medieval, ardosia; na selva, palha; no jurassico, folhas; no futuro,
  painel solar.

## tiles/vagao.png (comboio abandonado da selva)
- 128x16, celulas de 16x16: 0 teto do vagao (com musgo), 1 chapa lateral, 2 chapa enferrujada com furo,
  3 chassi com roda (pontas do vagao), 4 trilho (desenhado em cima do chao), 5 interior de baixo (banco),
  6 interior de cima (janela partida), 7 chassi sem roda (meio do vagao).
- O jogo escolhe a peca sozinho pela vizinhanca. Os cabos eletricos que soltam faiscas sao desenhados pelo codigo.

## sprites/objetos/paraquedas.png
- paraquedas.png (24x18): copula dos paraquedistas; o ponto onde as cordas se juntam fica no fundo, ao centro
  (e ali que fica a cabeca do soldado).

## sprites/objetos/carro.png
- carro.png (96x16): carros destruidos da cidade, celulas de 32x16 com a base em (16, 15):
  0 carcaca queimada (solta fumaca), 1 taxi branco batido, 2 carro vermelho amassado.

## Era "cidade" (Sao Paulo em caos)
- tiles/cidade.png segue o mesmo formato das outras eras. A linha 0 e a calcada portuguesa (ondas pretas e
  brancas) com guia; os enfeites da linha 4 sao saco de lixo, cone e papeis/garrafa.
- cenarios/cidade/: ceu.png (azul profundo, nuvens de entardecer, fumaca), fundo.png (skyline anil) e
  meio.png (predios, arvores, muro com pixo e postes). Nas camadas, a rua fica na linha 124.

## tiles/fundo_<era>.png (paredes de fundo)
- 64x32, celulas de 16x16. Linha 0 = terra/caverna (aparece em tuneis, cavernas e buracos cavados no chao);
  linha 1 = parede de construcao (atras de escadas, torres e bunkers; fica no lugar quando os blocos da frente
  sao destruidos). 4 variacoes por linha, sorteadas por posicao.
- Use tons mais escuros e frios que os blocos da frente. O jogo acrescenta sozinho a sombra de contato
  junto aos blocos solidos vizinhos.
- Toda escada tem parede de fundo atras (terra se estiver dentro do chao, construcao fora dele).
""";
}
