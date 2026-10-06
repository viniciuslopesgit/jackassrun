using Raylib_cs;
using static JackassRun.Col;

namespace JackassRun;

public enum BgStyle { Jungle, Dino, Medieval, Future, City }

/// <summary>Cenario de um level: paleta, terreno, fundos e inimigos. Os levels usam os cenarios em ordem
/// (level 1 = selva, 2 = jurassico, ...) e depois repetem, mais dificeis.</summary>
public sealed class Era
{
    public string Name = "", Year = "";
    public BgStyle Style;
    public Color SkyTop, SkyBot, Sun, Far, Mid, Near;
    public Color Dirt, DirtDark, Top, TopLight, Brick, BrickDark, Steel, SteelDark;
    public Color ESkin, EShirt, EPants, EHat, EAccent, EGun, EBullet;
    public Color FlyBody, FlyWing;
    public byte EHatStyle;
}

public static class Eras
{
    public static readonly Era[] All =
    {
        new Era {
            Name = "SELVA DE GUERRA", Year = "1985", Style = BgStyle.Jungle,
            SkyTop = Hex(0x2a78d6), SkyBot = Hex(0xf8cf86), Sun = Hex(0xfff3b0),
            Far = Hex(0x6a94bf), Mid = Hex(0x2f6e4c), Near = Hex(0x1d4a33),
            Dirt = Hex(0x8c5a32), DirtDark = Hex(0x55351d), Top = Hex(0x56c03a), TopLight = Hex(0xb2ee5e),
            Brick = Hex(0xb25a3a), BrickDark = Hex(0x6e311d), Steel = Hex(0x8e98aa), SteelDark = Hex(0x4a5266),
            ESkin = Hex(0xe0a070), EShirt = Hex(0x6b7a3a), EPants = Hex(0x48502a), EHat = Hex(0x3e4a22),
            EAccent = Hex(0xd8b840), EGun = Hex(0x2e2e36), EBullet = Hex(0xff5a2a),
            FlyBody = Hex(0x5a6a3a), FlyWing = Hex(0xb0b6c0), EHatStyle = 1,
        },
        new Era {
            Name = "TERRA JURASSICA", Year = "65.000.000 A.C.", Style = BgStyle.Dino,
            SkyTop = Hex(0xd24a2a), SkyBot = Hex(0xffcf68), Sun = Hex(0xfff2c0),
            Far = Hex(0x9a3a2e), Mid = Hex(0x3e5a28), Near = Hex(0x26381a),
            Dirt = Hex(0xa8643a), DirtDark = Hex(0x683a22), Top = Hex(0x7aa83a), TopLight = Hex(0xd0e872),
            Brick = Hex(0x9a8a70), BrickDark = Hex(0x5e5242), Steel = Hex(0x4e3c60), SteelDark = Hex(0x2a1e36),
            ESkin = Hex(0xd89060), EShirt = Hex(0xa8622a), EPants = Hex(0x7a4520), EHat = Hex(0xeee6cc),
            EAccent = Hex(0xeee6cc), EGun = Hex(0x6a4a2a), EBullet = Hex(0xffb020),
            FlyBody = Hex(0xb0603a), FlyWing = Hex(0xe0905a), EHatStyle = 5,
        },
        new Era {
            Name = "CASTELO MEDIEVAL", Year = "1347 D.C.", Style = BgStyle.Medieval,
            SkyTop = Hex(0x150f3a), SkyBot = Hex(0x74508e), Sun = Hex(0xf4f0d4),
            Far = Hex(0x3e3264), Mid = Hex(0x251c40), Near = Hex(0x18122a),
            Dirt = Hex(0x6a5a48), DirtDark = Hex(0x403426), Top = Hex(0x4a8e3c), TopLight = Hex(0x96d062),
            Brick = Hex(0x8e8ea6), BrickDark = Hex(0x54546c), Steel = Hex(0x7a8090), SteelDark = Hex(0x3e4250),
            ESkin = Hex(0xe0b090), EShirt = Hex(0xaab2c4), EPants = Hex(0x6a7082), EHat = Hex(0xc4ccdc),
            EAccent = Hex(0xd82a3a), EGun = Hex(0x5a3a22), EBullet = Hex(0xff6a3a),
            FlyBody = Hex(0x4e3c62), FlyWing = Hex(0x8a62a0), EHatStyle = 6,
        },
        new Era {
            Name = "SAO PAULO EM CAOS", Year = "2025", Style = BgStyle.City,
            SkyTop = Hex(0x18244c), SkyBot = Hex(0x86a2c8), Sun = Hex(0xf2ac88),
            Far = Hex(0x2c3060), Mid = Hex(0x2a2630), Near = Hex(0x201e26),
            Dirt = Hex(0x5e5a56), DirtDark = Hex(0x38342f), Top = Hex(0x8e8a82), TopLight = Hex(0xe4e0d4),
            Brick = Hex(0x8e8a84), BrickDark = Hex(0x56534e), Steel = Hex(0x8a929c), SteelDark = Hex(0x464c56),
            ESkin = Hex(0xc8906a), EShirt = Hex(0x2c2c34), EPants = Hex(0x3e4c6a), EHat = Hex(0x1c1c22),
            EAccent = Hex(0xf0c020), EGun = Hex(0x26262c), EBullet = Hex(0xffc040),
            FlyBody = Hex(0x2a3550), FlyWing = Hex(0xc8ccd4), EHatStyle = 3,
        },
        new Era {
            Name = "FUTURO NEON", Year = "2187", Style = BgStyle.Future,
            SkyTop = Hex(0x12062a), SkyBot = Hex(0xd23a8c), Sun = Hex(0xffd23e),
            Far = Hex(0x3e1a60), Mid = Hex(0x1f0e3c), Near = Hex(0x120826),
            Dirt = Hex(0x3a3a5c), DirtDark = Hex(0x22223a), Top = Hex(0x2ae8e8), TopLight = Hex(0xbaffff),
            Brick = Hex(0x5e3a8e), BrickDark = Hex(0x3a2262), Steel = Hex(0x9eaacc), SteelDark = Hex(0x4a5472),
            ESkin = Hex(0xb8c0d4), EShirt = Hex(0x4a5878), EPants = Hex(0x2e3852), EHat = Hex(0xdce4f4),
            EAccent = Hex(0xff3a5a), EGun = Hex(0x2a2a3a), EBullet = Hex(0xff3ae0),
            FlyBody = Hex(0xa4acc4), FlyWing = Hex(0x3ae0ff), EHatStyle = 7,
        },
    };

    public static int IndexForChunk(int c) => ((c < 0 ? 0 : c) / GameConfig.PedacosPorLevel) % All.Length;
    public static int IndexForCol(int tx) => IndexForChunk((int)MathF.Floor(tx / (float)K.CHUNK));
    public static int IndexForX(float x) => IndexForCol((int)MathF.Floor(x / K.T));
    public static Era ForCol(int tx) => All[IndexForCol(tx)];
    public static Era ForX(float x) => All[IndexForX(x)];
}

/// <summary>Aparencia de um boneco desenhado por partes (cabecao estilo chibi).</summary>
public struct Look
{
    public Color Skin, Hair, Shirt, Pants, Boots, Gun, Accent;
    public byte Hat, Bulk, GunLen, GunH;
    public byte Wpn;          // arma desenhada: ver Sprites.Guns
    public Color Arms, Belt;  // cor dos bracos (uniforme) e do cinto, quando SuitArms / CustomBelt
    public bool SuitArms, CustomBelt, Cape;
    public bool Blade;
}

public sealed class CharDef
{
    public string Name = "", Weapon = "", Special = "", Tag = "";
    public Look Look;
    public float FireRate = 0.1f, Speed = 88f;
}

/// <summary>Herois jogaveis. Indices: 0 Batman, 1 Tomb Raider, 2 Homem-Aranha.
/// ATENCAO: personagens de terceiros, so para teste; troque por herois originais antes de publicar.</summary>
public static class Chars
{
    public const int Batman = 0, TombRaider = 1, Spider = 2;

    public static readonly CharDef[] All =
    {
        new CharDef {
            Name = "BATMAN", Tag = "O CAVALEIRO DAS TREVAS", Weapon = "BATARANGUE", Special = "BOMBA DE FUMACA", FireRate = 0.36f, Speed = 86f,
            Look = new Look { Skin = Hex(0xe8b898), Hair = Hex(0x26262e), Shirt = Hex(0x5c5c68), Pants = Hex(0x4a4a56),
                Boots = Hex(0x1c1c24), Gun = Hex(0x1c1c24), Accent = Hex(0xe8c020), Hat = 10, Wpn = 7,
                Arms = Hex(0x5c5c68), SuitArms = true, Belt = Hex(0xe8c020), CustomBelt = true, Cape = true },
        },
        new CharDef {
            Name = "TOMB RAIDER", Tag = "ARQUEOLOGA ACROBATA", Weapon = "PISTOLAS DUPLAS", Special = "FLECHA EXPLOSIVA", FireRate = 0.11f, Speed = 92f,
            Look = new Look { Skin = Hex(0xe0a884), Hair = Hex(0x4a2c1c), Shirt = Hex(0x2a8a8a), Pants = Hex(0x6a4a2a),
                Boots = Hex(0x3a2416), Gun = Hex(0x2a2a30), Accent = Hex(0x6a4a2a), Hat = 11, Wpn = 10 },
        },
        new CharDef {
            Name = "HOMEM-ARANHA", Tag = "AMIGAO DA VIZINHANCA", Weapon = "TEIA", Special = "LEQUE DE TEIAS", FireRate = 0.3f, Speed = 96f,
            Look = new Look { Skin = Hex(0xc81a22), Hair = Hex(0xc81a22), Shirt = Hex(0xc81a22), Pants = Hex(0x1e3c9c),
                Boots = Hex(0xc81a22), Gun = Hex(0xc81a22), Accent = Hex(0x1e3c9c), Hat = 12, Wpn = 7,
                Arms = Hex(0xc81a22), SuitArms = true, Belt = Hex(0xa81418), CustomBelt = true },
        },
    };
}
