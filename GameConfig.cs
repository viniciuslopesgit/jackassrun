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
}
