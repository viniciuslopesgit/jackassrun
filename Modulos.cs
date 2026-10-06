namespace JackassRun;

/// <summary>Parte do level em que um modulo pode aparecer (junte varias com |).</summary>
[Flags]
public enum Ato
{
    Chegada = 1,      // comeco do level: calmo, poucos inimigos
    Arredores = 2,    // casas, pontes, torres
    Subsolo = 4,      // cavernas, tuneis e minas
    Posto = 8,        // bunkers, acampamentos, emboscadas
    Fortaleza = 16,   // o ultimo pedaco do level: climax com o prisioneiro
    Normal = Chegada | Arredores | Subsolo | Posto,
}

/// <summary>Um pedaco de mapa desenhado a mao (a "base"). O jogo monta cada trecho de 40 m com um modulo de
/// 20 colunas ou dois de 10, e sorteia as VARIACOES em cima da base (caixas, grupos de blocos, inimigos, espelho,
/// altura do chao). Assim o mapa nunca e totalmente aleatorio: tem estrutura de level design, mas muda sempre.</summary>
public sealed class Modulo
{
    public string Nome = "";
    public Ato Atos = Ato.Normal;      // em que partes do level pode aparecer
    public int Peso = 10;              // chance relativa de ser escolhido (maior = aparece mais)
    public int Intensidade = 1;        // 0 calmo ... 5 climax; depois de um modulo intenso vem um respiro
    public bool Espelhar;              // pode aparecer espelhado (de tras para a frente)
    public BgStyle[]? Cenarios;        // so nestes cenarios (null = todos)
    public string[] Mapa = Array.Empty<string>();

    public int Largura => Mapa[0].Length;
    public int Altura => Mapa.Length;
    /// <summary>Linha do chao na primeira e na ultima coluna (primeiro '#' de cima para baixo).</summary>
    public int Entrada { get; private set; }
    public int Saida { get; private set; }
    /// <summary>Primeira linha com alguma coisa desenhada (o resto de cima e ceu).</summary>
    public int Topo { get; private set; }

    static int Chao(string[] mapa, int col)
    {
        for (int y = 0; y < mapa.Length; y++) if (mapa[y][col] == '#') return y;
        return -1;
    }

    /// <summary>Confere o desenho e calcula a entrada/saida. Erros aparecem com o nome do modulo.</summary>
    public void Validar()
    {
        if (Mapa.Length == 0) throw new InvalidOperationException($"Modulo '{Nome}': mapa vazio");
        int w = Mapa[0].Length;
        if (w % 5 != 0 || w > K.CHUNK)
            throw new InvalidOperationException($"Modulo '{Nome}': largura {w} (use 10 ou 20 colunas)");
        for (int y = 0; y < Mapa.Length; y++)
        {
            if (Mapa[y].Length != w)
                throw new InvalidOperationException($"Modulo '{Nome}': a linha {y + 1} tem {Mapa[y].Length} colunas (esperado {w})");
            foreach (char ch in Mapa[y])
                if (!Modulos.Legenda.Contains(ch))
                    throw new InvalidOperationException($"Modulo '{Nome}': caractere '{ch}' desconhecido na linha {y + 1} (veja a legenda em Modulos.cs)");
        }
        Entrada = Chao(Mapa, 0); Saida = Chao(Mapa, w - 1);
        Topo = 0;
        while (Topo < Mapa.Length - 1 && Mapa[Topo].All(ch => ch == '.')) Topo++;
        if (Entrada < 0 || Saida < 0)
            throw new InvalidOperationException($"Modulo '{Nome}': a primeira e a ultima coluna precisam ter chao de terra (#)");
    }
}

/// <summary>Biblioteca de modulos do gerador de mapas. Edite os desenhos (ou crie novos) e rode o jogo de novo.
///
/// COMO DESENHAR: cada modulo e uma grade de texto com 20 colunas (ou 10, para meios-modulos que se juntam dois a
/// dois). Cada caractere e um bloco de 16x16. As linhas de cima sao o ceu; a ULTIMA LINHA SE REPETE ATE O FUNDO do
/// mapa (terra = chao firme ate a rocha; '.' = buraco sem fundo, que mata). A primeira e a ultima coluna precisam
/// ter chao de terra: o jogo encaixa um modulo no outro pela altura desse chao (os modulos sobem e descem inteiros).
/// Medidas do heroi: 1 bloco de altura, pula ~2,5 blocos para cima e ~4 de distancia, e escala paredes.
///
/// LEGENDA
///   TERRENO     .  vazio (ceu)                  #  terra
///               B  tijolo                       S  aco (nao quebra)
///               C  caixa de madeira             T  telhado (quebra facil; enfeites como antena aparecem sozinhos)
///               D  porta (2 de altura; o heroi arromba correndo contra ela)
///               H  escada (sempre com parede atras; precisa levar a um piso de 3+ blocos, porta ou subsolo)
///               =  ponte de madeira (tabuas caem depois de pisadas)
///               -  ponte de concreto            |  pilar de concreto
///   FUNDO       ,  dentro de casa, sala ESCURA (quem esta la dentro nao ve o heroi ate a sala ser revelada)
///   (vazio)     ;  dentro de casa, sala clara   :  parede de construcao (torres, portoes)
///               _  terra ao fundo (valas, pocos e tuneis abertos)
///   VARIACOES   c  caixa ou nada (50%)          b  tijolo ou nada (50%)
///               1 2 3  grupos de tijolo: todos os "1" do modulo aparecem juntos ou somem juntos (50%)
///               4 5 6  grupos de terra: idem, com terra (ex.: um tunel que as vezes esta aberto)
///   INIMIGOS    e  inimigo comum (o tipo e sorteado conforme o level: soldado, faca, cao, granadeiro,
///                  escudeiro, lanca-chamas, homem-bomba, bazuqueiro, brutamontes...)
///               a  atirador de posto (soldado, bazuqueiro, granadeiro ou atirador de elite): lugares altos
///               g  brutamontes          m  torreta          w  voador (no meio da celula)
///               Minuscula = so aparece se couber no orcamento de inimigos do level (no comeco aparecem menos).
///               MAIUSCULA (E, A, G, M) = aparece sempre.
///               Os pes ficam na base da celula: desenhe o inimigo em cima do chao.
///   OBJETOS     x  barril explosivo     $  prisioneiro na jaula (+1 time out)     r  refem
///               o  glorb (bonus)        v  carro destruido (so na cidade; fora dela, as vezes um barril)
///   EVENTOS     !  emboscada: ao passar por esta coluna, entram tropas correndo pela direita
///               ^  paraquedistas: ao passar por esta coluna, caem soldados do ceu</summary>
public static class Modulos
{
    public const string Legenda = ".#BSCTDH=-|,;:_cb123456eEaAgGmMwx$rov!^";

    /// <summary>Primeiro trecho da corrida: area segura de aquecimento (sem inimigos).</summary>
    public static readonly Modulo Inicio = new()
    {
        Nome = "INICIO", Intensidade = 0, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            ".......o.....o.BB...",
            "......o.o...o.o.....",
            "..................x.",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        },
    };

    public static readonly Modulo[] Lista =
    {
        // CAMPO ABERTO: Morrinho, pilha de caixas e premios. Bom para comecar o level.
        new() { Nome = "CAMPO ABERTO", Atos = Ato.Chegada | Ato.Arredores, Peso = 10, Intensidade = 1, Espelhar = true, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "......oo.......c....",
            ".ooo.....e.....CC...",
            ".....#######...CC.e.",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // DEGRAUS: Terracos de 4 blocos subindo e descendo, com vigia no alto.
        new() { Nome = "DEGRAUS", Atos = Ato.Chegada | Ato.Arredores, Peso = 8, Intensidade = 1, Espelhar = true, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "........e.o..o......",
            "........####..a.....",
            ".....o..########....",
            "....############.x..",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // SUBIDA: Rampa de dois degraus largos: muda a altura do chao (espelhada vira descida).
        new() { Nome = "SUBIDA", Atos = Ato.Normal, Peso = 6, Intensidade = 1, Espelhar = true, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            ".......ooo....c..a..",
            "............########",
            "...e..##############",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // PLATAFORMAS: Abismo com plataformas flutuantes. As vezes (grupo 1) ha uma rede de tijolos embaixo.
        new() { Nome = "PLATAFORMAS", Atos = Ato.Chegada | Ato.Arredores | Ato.Subsolo, Peso = 8, Intensidade = 2, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "............e.o.....",
            ".........o.SBS......",
            "....o...............",
            "......SBS.........a.",
            "####............####",
            "####............####",
            "####111111111111####",
            "####............####",
            "####............####",
            "####............####",
            "####............####",
            "####............####",
        }},

        // TORRE DE VIGIA: Torre com escada e mirante; barris embaixo derrubam tudo.
        new() { Nome = "TORRE DE VIGIA", Atos = Ato.Chegada | Ato.Arredores | Ato.Posto, Peso = 8, Intensidade = 2, Espelhar = true, Mapa = new[]
        {
            "....................",
            "....................",
            "...........A........",
            "......BBBHBBB.......",
            "........BHB.........",
            "........BHB.........",
            "........BHB......Cc.",
            "...e...xBHBx...e.CC.",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // CASA NO MORRO: O esboco: sobrado com escada interna, valeta com tunel por baixo das caixas e escada que sobe para dentro da casa da direita.
        new() { Nome = "CASA NO MORRO", Atos = Ato.Arredores | Ato.Posto, Peso = 10, Intensidade = 3, Mapa = new[]
        {
            "....................",
            "....................",
            "TTTTTT..............",
            "B,,,,b..............",
            "B,e,,b..............",
            "BHBBBB......TTTTTTTT",
            "DH,,,D......D,,,,,,D",
            "DH,e,D..c...D,,e,r,D",
            "######_CCCC.#H######",
            "######_CCCC.#H######",
            "######_######H######",
            "######_______H######",
            "######_o_e_o_H######",
            "####################",
            "####################",
            "####################",
        }},

        // VILA: Duas casas com telhado e uma rua no meio (carro na cidade).
        new() { Nome = "VILA", Atos = Ato.Arredores, Peso = 9, Intensidade = 2, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....a...........a...",
            "TTTTTTTT....TTTTTTTT",
            ".D,,,,D......D,,,,D.",
            ".D,e,rD.!.v.eD,e,oD.",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // PONTE DE MADEIRA: Abismo com ponte de tabuas (caem depois de pisadas) e posto do outro lado.
        new() { Nome = "PONTE DE MADEIRA", Atos = Ato.Arredores | Ato.Posto, Peso = 8, Intensidade = 3, Cenarios = new[] { BgStyle.Jungle, BgStyle.Dino, BgStyle.Medieval, BgStyle.Future }, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "......o.o.o.........",
            "................c...",
            ".....!.e..e....xC.a.",
            "####==========######",
            "####..........######",
            "####..........######",
            "####..........######",
            "####..........######",
            "####..........######",
            "####..........######",
            "####..........######",
        }},

        // VIADUTO: Ponte de concreto com pilares; paraquedistas no meio.
        new() { Nome = "VIADUTO", Atos = Ato.Arredores | Ato.Posto, Peso = 7, Intensidade = 3, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            ".....e..^v..e.......",
            "###--------------###",
            "###...|......|...###",
            "###...|......|...###",
            "###...|......|...###",
            "###...|......|...###",
            "###...|......|...###",
            "###...|......|...###",
            "###...|......|...###",
        }},

        // ACAMPAMENTO: Barris na frente (reacao em cadeia), barraca e caixas de cobertura. Reforcos chegam correndo.
        new() { Nome = "ACAMPAMENTO", Atos = Ato.Arredores | Ato.Posto, Peso = 8, Intensidade = 3, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "..............ac....",
            ".........TTTT.CC....",
            "....!xxe.;E;;.CC.e..",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // PREDIO: Predio de 3 andares com escadas alternadas; atirador no telhado e prisioneiro no ultimo andar.
        new() { Nome = "PREDIO", Atos = Ato.Arredores | Ato.Posto, Peso = 7, Intensidade = 4, Mapa = new[]
        {
            "........A...........",
            "...TTTTTTTTTTTTTT...",
            "....B,,,,,,,,,,b....",
            "....B,,,,,a,,$,B....",
            "....BHBBBBBBBBBB....",
            "....bH,,,,,,,,,B....",
            "....BH,,,e,,r,,b....",
            "....BBBBBBBBBBHB....",
            "....D,,,,,,,,,HD....",
            "..x.D,,e,,e,,,HD..e.",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // CAVERNA: Buraco que NAO mata: cai-se num salao com premios e prisioneiro; sai-se pela escada.
        new() { Nome = "CAVERNA", Atos = Ato.Subsolo | Ato.Arredores, Peso = 10, Intensidade = 2, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "........e.........a.",
            "###__###########H###",
            "###__###########H###",
            "###__##_______##H###",
            "###_____________H###",
            "###____o_o_o____H###",
            "###__e___e___$__H###",
            "####################",
            "####################",
        }},

        // TUNEL DUPLO: Dois caminhos: por cima (barricada com atiradores) ou pelo tunel (premios).
        new() { Nome = "TUNEL DUPLO", Atos = Ato.Subsolo | Ato.Posto, Peso = 9, Intensidade = 3, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            ".........c..........",
            "......^.aCa..e......",
            "#H################H#",
            "#H################H#",
            "#H________________H#",
            "#H__o__o__e__o__$_H#",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // MINA: Barracao com alcapao: a escada desce para uma galeria com barril, inimigo e prisioneiro.
        new() { Nome = "MINA", Atos = Ato.Subsolo, Peso = 8, Intensidade = 3, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            ".TTTTT..............",
            ".D;;;D............c.",
            ".D;e;D..e......x.Ca.",
            "###H############H###",
            "###H############H###",
            "###H############H###",
            "###H____________H###",
            "###H__o_x_e_o$__H###",
            "####################",
            "####################",
            "####################",
        }},

        // TRINCHEIRA: Valas com soldados dentro e sacos de areia (caixas) nas bordas.
        new() { Nome = "TRINCHEIRA", Atos = Ato.Subsolo | Ato.Posto, Peso = 7, Intensidade = 3, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "..........c.........",
            "...cC....CC.....a...",
            "#####___####___#####",
            "#####___####___#####",
            "#####e__####_e_#####",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // BUNKER: Bunker com teto de aco e torreta; barril encostado na porta; escada nos fundos ate o teto.
        new() { Nome = "BUNKER", Atos = Ato.Posto, Peso = 9, Intensidade = 3, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "..........M.a.......",
            ".......SSSSSSSH.....",
            ".......D,,,,,BH.....",
            "...11.xD,e,e,BHx.e..",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // FORTALEZA: Portao com mirante, patio com tropas, bunker com torreta e o prisioneiro atras.
        new() { Nome = "FORTALEZA", Atos = Ato.Fortaleza, Peso = 10, Intensidade = 5, Mapa = new[]
        {
            "....................",
            "....................",
            "....A.a.............",
            ".HBBBBB.............",
            ".HB:::B.......M.a...",
            ".HB:::B......SSSS...",
            ".HD:::D.^...cBBBB...",
            ".HD:x:D.ee.eCBBBB.$.",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // QUARTEL: Barricada, quartel de 2 andares (prisioneiro em cima) e um tunel de flanco que sai dentro do quartel.
        new() { Nome = "QUARTEL", Atos = Ato.Fortaleza, Peso = 10, Intensidade = 5, Mapa = new[]
        {
            "....................",
            "...........A........",
            ".........TTTTTTTTT..",
            ".........B,,,,,,,b..",
            ".........B,,a,$,,b..",
            ".........BBBBBBBHB..",
            ".....cC..D,,,,,,HD..",
            ".^...CCeeD,e,g,,HD..",
            "##H########H########",
            "##H________H########",
            "##Ho_o_o_e_H########",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // RESPIRO: Chao plano com premios, sem inimigos.
        new() { Nome = "RESPIRO", Atos = Ato.Normal, Peso = 8, Intensidade = 0, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..o.o.o...",
            "..........",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
        }},

        // BURACO: Buraco sem fundo de 3 blocos.
        new() { Nome = "BURACO", Atos = Ato.Normal, Peso = 6, Intensidade = 1, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            ".....o....",
            "....o.o...",
            ".........e",
            "####...###",
            "####...###",
            "####...###",
            "####...###",
            "####...###",
            "####...###",
            "####...###",
            "####...###",
        }},

        // CAIXOTES: Pilha de caixas (cobertura que se destroi).
        new() { Nome = "CAIXOTES", Atos = Ato.Normal, Peso = 6, Intensidade = 1, Espelhar = true, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "....c.....",
            "...CCc....",
            "...CCC..e.",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
        }},

        // JAULA: Recompensa: prisioneiro na jaula.
        new() { Nome = "JAULA", Atos = Ato.Arredores | Ato.Subsolo | Ato.Posto, Peso = 4, Intensidade = 0, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            ".o.o......",
            "...$....e.",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
        }},

        // BARRIS: Barris colados nos inimigos: um tiro resolve.
        new() { Nome = "BARRIS", Atos = Ato.Arredores | Ato.Posto, Peso = 5, Intensidade = 2, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "...xx.ee..",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
        }},

        // CASINHA: Casinha com porta, inimigo e refem.
        new() { Nome = "CASINHA", Atos = Ato.Chegada | Ato.Arredores | Ato.Posto, Peso = 6, Intensidade = 2, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            ".....a....",
            ".TTTTTTTT.",
            "..D,,,,D..",
            "..De,r,D..",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
        }},

        // DEGRAU: Degrau de 2 blocos (espelhado vira descida).
        new() { Nome = "DEGRAU", Atos = Ato.Normal, Peso = 5, Intensidade = 0, Espelhar = true, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            ".......e..",
            ".....#####",
            ".....#####",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
        }},

        // PINGUELA: Pontezinha de madeira sobre um buraco de 4.
        new() { Nome = "PINGUELA", Atos = Ato.Chegada | Ato.Arredores, Peso = 4, Intensidade = 1, Cenarios = new[] { BgStyle.Jungle, BgStyle.Dino, BgStyle.Medieval, BgStyle.Future }, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "....e.....",
            "###====###",
            "###....###",
            "###....###",
            "###....###",
            "###....###",
            "###....###",
            "###....###",
            "###....###",
        }},

        // NINHO: Ninho de metralhadora: caixas com atirador atras.
        new() { Nome = "NINHO", Atos = Ato.Arredores | Ato.Posto, Peso = 5, Intensidade = 2, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "......Cc..",
            "......CCa.",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
        }},
    };

    static Modulos()
    {
        Inicio.Validar();
        foreach (var m in Lista) m.Validar();
    }
}
