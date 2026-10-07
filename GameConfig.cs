namespace JackassRun;

/// <summary>Variaveis gerais do jogo. Edite os numeros aqui e rode o jogo de novo.</summary>
public static class GameConfig
{
    /// <summary>Distancia de cada level, em metros. O mapa e gerado em pedacos de 40 m, entao o valor e
    /// arredondado para o multiplo de 40 mais proximo (minimo 40). Ex.: 200 = 5 pedacos.</summary>
    public const int MetrosPorLevel = 500;

    /// <summary>Metros de cada pedaco de mapa gerado (20 blocos de 16 px, 8 px por metro).</summary>
    public const int MetrosPorPedaco = K.CHUNK * K.T / K.PX_PER_M;

    /// <summary>Quantos pedacos de mapa formam um level (calculado a partir de MetrosPorLevel).</summary>
    public static readonly int PedacosPorLevel = Math.Max(1, (int)MathF.Round(MetrosPorLevel / (float)MetrosPorPedaco));

    // ------------------------------------------------------------------ inimigos

    /// <summary>Quantidade de inimigos nos levels: multiplica o orcamento de inimigos de cada trecho.
    /// 1 = normal, 1.5 = 50% a mais, 2 = o dobro. Cada modulo (Modulos.cs) tem um numero maximo de lugares para
    /// inimigos (as letras e, a, g, m, w); para ter ainda mais, desenhe mais letras nos modulos.</summary>
    public const float QuantidadeInimigos = 1.2f;

    /// <summary>Quantos blocos de altura os inimigos sobem pulando para chegar ao heroi (caixas, degraus, muros).
    /// O brutamontes e o escudeiro, pesados, sobem 1 a menos. O inimigo com faca tambem escala paredes.</summary>
    public const int InimigosSobemBlocos = 2;

    /// <summary>Quantos blocos de altura os inimigos aceitam descer de uma beirada para chegar ao heroi.</summary>
    public const int InimigosDescemBlocos = 6;

    /// <summary>Dano de cada bala inimiga nos blocos do cenario (vida dos blocos: terra 8, tijolo 5, caixa 3,
    /// porta 2, telhado 3; aco e rocha nao quebram, concreto so com explosao). 0 = balas inimigas nao estragam
    /// o cenario. As explosoes dos inimigos (bazuca, granada, bomba, homem-bomba) destroem os blocos sempre.</summary>
    public const int DanoBalaInimigaNosBlocos = 1;

    // ------------------------------------------------------------------ desabamento de blocos

    // Blocos que NUNCA caem, mesmo sem nada embaixo: true = ficam presos no lugar e seguram o que esta encostado
    // neles. Aco e a rocha do fundo nunca caem; escadas e pontes de madeira nao desabam.
    public const bool TerraNuncaCai = true;
    public const bool ConcretoNuncaCai = true;
    public const bool TijoloNuncaCai = false;
    public const bool CaixaNuncaCai = false;
    public const bool PortaNuncaCai = false;
    public const bool VagaoNuncaCai = true;      // chapa de metal dos vagoes e da locomotiva (comboio abandonado)

    /// <summary>Chance (0 a 1) de um bloco de telhado cair quando perde o apoio (ex.: as paredes da casa foram
    /// destruidas). Se nao cair, fica preso no lugar. O telhado que cai se parte ao bater no chao. 0 = nunca cai.</summary>
    public const float ChanceTelhadoCair = 0.35f;

    /// <summary>true se este tipo de bloco nunca cai (ver as opcoes acima).</summary>
    public static bool NuncaCai(int type) => type switch
    {
        Terrain.DIRT => TerraNuncaCai, Terrain.CONCRETE => ConcretoNuncaCai, Terrain.BRICK => TijoloNuncaCai,
        Terrain.CRATE => CaixaNuncaCai, Terrain.DOOR => PortaNuncaCai, Terrain.ROOF => ChanceTelhadoCair <= 0,
        Terrain.WAGON => VagaoNuncaCai,
        _ => Terrain.Unbreakable(type),
    };
}
