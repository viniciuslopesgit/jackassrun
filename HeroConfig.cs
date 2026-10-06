namespace JackassRun;

/// <summary>Variaveis de um heroi. Os valores de cada heroi ficam em HeroConfig.Stats, logo abaixo.</summary>
public sealed class HeroStats
{
    public string Nome = "", Apelido = "", NomeArma = "", NomeEspecial = "";

    // movimento
    public float VelocidadeCorrida;     // pixels por segundo (a tela anda no maximo a 70)
    public float ForcaPulo;             // impulso do pulo (238 = ~2,8 blocos de altura)
    public int PulosNoAr;               // pulos extras no ar (1 = pulo duplo)
    public float VelocidadeEscalada;    // velocidade subindo paredes
    public float QuedaPlanando;         // velocidade maxima de queda segurando pular (0 = nao plana)

    // tiro
    public float IntervaloTiro;         // segundos entre um tiro e outro (menor = atira mais rapido)
    public float VelocidadeTiro;        // velocidade do projetil em pixels por segundo
    public int DanoTiro;                // dano de cada tiro (soldado tem 2 de vida, brutamontes 8)
    public float DispersaoTiro;         // variacao vertical aleatoria do tiro (0 = reto)
    public float AlcanceTiro;           // segundos que o projetil dura; distancia = VelocidadeTiro x AlcanceTiro
                                        //   (batarangue: metade indo, metade voltando)
    public float AtordoamentoTiro;      // segundos que o inimigo fica atordoado ao ser atingido (0 = nao atordoa)

    // facada (ataque corpo a corpo: sai no lugar do tiro quando o inimigo esta colado a frente)
    public int DanoFacada;              // dano da facada (soldado tem 2 de vida, bazuqueiro 3, brutamontes 8)
    public float AlcanceFacada;         // distancia em pixels a frente do heroi em que a facada acerta

    // dano do tiro nos blocos (vida de cada bloco: terra 8, tijolo 5, caixa 3, porta 2, ponte 2, telhado 3; aco e rocha nao quebram)
    public int DanoTerra;               // dano por tiro num bloco de terra
    public int DanoTijolo;              // dano por tiro num bloco de tijolo
    public int DanoCaixa;               // dano por tiro numa caixa de madeira
    public int DanoPorta;               // dano por tiro numa porta
    public int DanoPonte;               // dano por tiro numa tabua de ponte (vida 2)
    public int DanoTelhado;             // dano por tiro num bloco de telhado (vida 3)

    /// <summary>Dano do tiro deste heroi num tipo de bloco (Terrain.DIRT, BRICK...).</summary>
    public int DanoBloco(int type) => type switch
    {
        Terrain.DIRT => DanoTerra,
        Terrain.BRICK => DanoTijolo,
        Terrain.CRATE => DanoCaixa,
        Terrain.DOOR => DanoPorta,
        Terrain.BRIDGE => DanoPonte,
        Terrain.ROOF => DanoTelhado,
        _ => DanoTiro,
    };

    // especial
    public int EspeciaisIniciais;       // quantos especiais o heroi tem ao entrar
    public float VelocidadeEspecial;    // velocidade do projetil do especial
    public float RaioEspecial;          // alcance da fumaca / raio da explosao da flecha (pixels)
    public float AtordoamentoEspecial;  // segundos de atordoamento causados pelo especial
    public int QuantidadeEspecial;      // quantos projeteis o especial solta (leque de teias)
    public float AberturaEspecial;      // abertura do leque entre um projetil e outro

    // animacao
    public float FpsCorrida;            // frames por segundo da animacao de correr (na velocidade normal; acelera/abranda com o heroi)
    public float FpsAnimacoes;          // frames por segundo das outras animacoes desenhadas em pastas (stop, jump, fall...)
}

/// <summary>Variaveis de cada heroi. Edite os numeros aqui e rode o jogo de novo.
/// A ordem segue Chars.All: 0 Batman, 1 Tomb Raider, 2 Homem-Aranha.</summary>
public static class HeroConfig
{
    public static readonly HeroStats[] Stats =
    {
        // ------------------------------------------------------------------ BATMAN
        new HeroStats
        {
            Nome = "BATMAN", Apelido = "O CAVALEIRO DAS TREVAS", NomeArma = "BATARANGUE", NomeEspecial = "BOMBA DE FUMACA",
            VelocidadeCorrida = 120,

            ForcaPulo = 238,
            PulosNoAr = 0,
            VelocidadeEscalada = 100,
            QuedaPlanando = 400,

            IntervaloTiro = 0.20f,
            VelocidadeTiro = 400,
            DanoTiro = 4,
            DispersaoTiro = 1,
            AlcanceTiro = 1f,
            AtordoamentoTiro = 0.5f,
            DanoFacada = 6,
            AlcanceFacada = 16,

            DanoTerra = 4,
            DanoTijolo = 2,
            DanoCaixa = 2,
            DanoPorta = 2,
            DanoPonte = 2,
            DanoTelhado = 2,

            EspeciaisIniciais = 3,
            VelocidadeEspecial = 0,
            RaioEspecial = 90,
            AtordoamentoEspecial = 2.4f,
            QuantidadeEspecial = 1,
            AberturaEspecial = 0,
            
            FpsCorrida = 15,
            FpsAnimacoes = 10,
        },
        // ------------------------------------------------------------------ TOMB RAIDER
        new HeroStats
        {
            Nome = "TOMB RAIDER", Apelido = "ARQUEOLOGA ACROBATA", NomeArma = "PISTOLAS DUPLAS", NomeEspecial = "FLECHA EXPLOSIVA",
            VelocidadeCorrida = 92,
            ForcaPulo = 238,
            PulosNoAr = 1,
            VelocidadeEscalada = 70,
            QuedaPlanando = 0,
            IntervaloTiro = 0.11f,
            VelocidadeTiro = 360,
            DanoTiro = 1,
            DispersaoTiro = 8,
            AlcanceTiro = 0.7f,
            AtordoamentoTiro = 0,
            DanoFacada = 4,
            AlcanceFacada = 14,
            DanoTerra = 1,
            DanoTijolo = 1,
            DanoCaixa = 1,
            DanoPorta = 1,
            DanoPonte = 1,
            DanoTelhado = 1,
            EspeciaisIniciais = 3,
            VelocidadeEspecial = 300,
            RaioEspecial = 28,
            AtordoamentoEspecial = 0,
            QuantidadeEspecial = 1,
            AberturaEspecial = 0,
            FpsCorrida = 14,
            FpsAnimacoes = 10,
        },
        // ------------------------------------------------------------------ HOMEM-ARANHA
        new HeroStats
        {
            Nome = "HOMEM-ARANHA", Apelido = "AMIGAO DA VIZINHANCA", NomeArma = "TEIA", NomeEspecial = "LEQUE DE TEIAS",
            VelocidadeCorrida = 96,
            ForcaPulo = 262,
            PulosNoAr = 0,
            VelocidadeEscalada = 130,
            QuedaPlanando = 0,
            IntervaloTiro = 0.3f,
            VelocidadeTiro = 300,
            DanoTiro = 1,
            DispersaoTiro = 0,
            AlcanceTiro = 0.7f,
            AtordoamentoTiro = 1.6f,
            DanoFacada = 5,
            AlcanceFacada = 16,
            DanoTerra = 1,
            DanoTijolo = 1,
            DanoCaixa = 1,
            DanoPorta = 1,
            DanoPonte = 1,
            DanoTelhado = 1,
            EspeciaisIniciais = 3,
            VelocidadeEspecial = 280,
            RaioEspecial = 0,
            AtordoamentoEspecial = 1.6f,
            QuantidadeEspecial = 5,
            AberturaEspecial = 55,
            FpsCorrida = 14,
            FpsAnimacoes = 10,
        },
    };

    /// <summary>Copia nomes e numeros basicos para a lista de herois usada no jogo.</summary>
    public static void Apply()
    {
        for (int i = 0; i < Chars.All.Length && i < Stats.Length; i++)
        {
            var c = Chars.All[i]; var s = Stats[i];
            c.Name = s.Nome; c.Tag = s.Apelido; c.Weapon = s.NomeArma; c.Special = s.NomeEspecial;
            c.Speed = s.VelocidadeCorrida; c.FireRate = s.IntervaloTiro;
        }
    }

    public static HeroStats Of(int ch) => Stats[Math.Clamp(ch, 0, Stats.Length - 1)];
}
