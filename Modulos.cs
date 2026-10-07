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
/// 20 colunas (na maioria das vezes) ou dois de 10, e sorteia as VARIACOES em cima da base (caixas, grupos de blocos, inimigos, espelho,
/// altura do chao). Assim o mapa nunca e totalmente aleatorio: tem estrutura de level design, mas muda sempre.</summary>
public sealed class Modulo
{
    public string Nome = "";
    public Ato Atos = Ato.Normal;      // em que partes do level pode aparecer
    public int Peso = 10;              // chance relativa de ser escolhido (maior = aparece mais)
    public int Intensidade = 1;        // 0 calmo ... 5 climax; depois de um modulo intenso vem um respiro
    public bool Espelhar;              // pode aparecer espelhado (de tras para a frente)
    public bool Recompensa;            // premio (ex.: jaula): aparece quase so logo depois de um trecho intenso
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
///               V  chapa de metal do comboio (vagao, locomotiva): o desenho escolhe sozinho teto, lateral e
///                  chassi com roda (a fileira de baixo do vagao)
///               ~  trilho do comboio (enfeite em cima do chao; atravessa-se)
///   FUNDO       ,  dentro de casa, sala ESCURA (quem esta la dentro nao ve o heroi ate a sala ser revelada)
///   (vazio)     ;  dentro de casa, sala clara   :  parede de construcao (torres, portoes)
///               _  terra ao fundo (valas, pocos e tuneis abertos)
///               *  dentro do vagao (parede de metal com banco e janela partida)
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
///               z  cabo eletrico partido soltando faiscas (pendurado no bloco de cima; sem nada em cima, num poste)
///   EVENTOS     !  emboscada: ao passar por esta coluna, entram tropas correndo pela direita
///               ^  paraquedistas: ao passar por esta coluna, caem soldados do ceu</summary>
public static class Modulos
{
    public const string Legenda = ".#BSCTDH=-|,;:_cb123456eEaAgGmMwx$rov!^V~*z";

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
        new() { Nome = "CAMPO ABERTO", Atos = Ato.Chegada, Peso = 10, Intensidade = 1, Espelhar = true, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "......oo.......ca...",
            ".ooo..e..e.e...CC...",
            "..e..#######.e.CC.e.",
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
        new() { Nome = "DEGRAUS", Atos = Ato.Chegada, Peso = 8, Intensidade = 1, Espelhar = true, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "........e.oe.o......",
            "........####e.a.....",
            ".....oe.########....",
            ".e..############.xe.",
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
            ".......ooo..e.ce.a..",
            ".........e..########",
            ".e.e..##############",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // PLATAFORMAS: Abismo com plataformas flutuantes. As vezes (grupo 1) ha uma rede de tijolos embaixo.
        new() { Nome = "PLATAFORMAS", Atos = Ato.Chegada | Ato.Arredores | Ato.Subsolo, Peso = 6, Intensidade = 2, Mapa = new[]
        {
            "....................",
            "....................",
            ".........w..........",
            "....................",
            "............e.o.....",
            ".........o.SBS......",
            "....o..e............",
            ".e....SBS.......e.a.",
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
            "...............w....",
            "....................",
            ".......a...A........",
            "......BBBHBBB.......",
            "........BHB.........",
            "........BHB.........",
            "........BHB......Cc.",
            ".e.e...xBHBx.e.e.CCe",
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
        new() { Nome = "CASA NO MORRO", Atos = Ato.Arredores | Ato.Posto, Peso = 12, Intensidade = 3, Mapa = new[]
        {
            "....................",
            "....................",
            "TTTTTT..............",
            "B,,,,b..............",
            "B,e,eb.........a....",
            "BHBBBB......TTTTTTTT",
            "DH,,,D......D,,,,,,D",
            "DH,eeD..c.e.D,ee,r,D",
            "######_CCCC.#H######",
            "######_CCCC.#H######",
            "######_######H######",
            "######_______H######",
            "######_o_e_oeH######",
            "####################",
            "####################",
            "####################",
        }},

        // VILA: Duas casas com telhado e uma rua no meio (carro na cidade).
        new() { Nome = "VILA", Atos = Ato.Arredores, Peso = 11, Intensidade = 2, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....a.e......e..a...",
            "TTTTTTTT....TTTTTTTT",
            ".D,,,,D......D,,,,D.",
            ".D,e,rD.!ev.eD,eeoDe",
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
        new() { Nome = "PONTE DE MADEIRA", Atos = Ato.Arredores | Ato.Posto, Peso = 10, Intensidade = 3, Cenarios = new[] { BgStyle.Jungle, BgStyle.Dino, BgStyle.Medieval, BgStyle.Future }, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            ".........w..........",
            "....................",
            "......o.o.o.........",
            "................c...",
            "..e..!.e..e.e..xC.ae",
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
            "......w.............",
            "....................",
            "....................",
            "....................",
            ".e...e..^v..e..e..e.",
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
            "....!xxee;E;e.CC.e.e",
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
        new() { Nome = "PREDIO", Atos = Ato.Arredores | Ato.Posto, Peso = 8, Intensidade = 4, Mapa = new[]
        {
            "........A.....a.....",
            "...TTTTTTTTTTTTTT...",
            "....B,,,,,,,,,,b....",
            "....B,e,,,a,,$,B....",
            "....BHBBBBBBBBBB....",
            "....bH,,,,,,,,,B....",
            "....BH,e,e,,r,,b....",
            "....BBBBBBBBBBHB....",
            "....D,,,,,,,,,HD....",
            ".ex.D,,e,,e,e,HD..e.",
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
            ".e......e...e.....a.",
            "###__###########H###",
            "###__###########H###",
            "###__##_______##H###",
            "###_____________H###",
            "###____o_o_o____H###",
            "###__e___e_e_$_eH###",
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
            "...e..^.aCa..e..e...",
            "#H################H#",
            "#H################H#",
            "#H________________H#",
            "#H__o_eo__e__oe_$_H#",
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
            ".D;e;D..e..e..ex.Ca.",
            "###H############H###",
            "###H############H###",
            "###H############H###",
            "###H____________H###",
            "###H_eo_x_e_o$_eH###",
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
            ".e.cC....CCe....a.e.",
            "#####___####___#####",
            "#####___####___#####",
            "#####e_e####_ee#####",
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
            "........a.M.a.......",
            ".......SSSSSSSH.....",
            ".......D,,,,,BH.....",
            ".e.11.xD,e,e,BHx.e.e",
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
            "..e.A.a.............",
            ".HBBBBB.............",
            ".HB:::B......aM.a...",
            ".HB:::B......SSSS...",
            ".HD:::D.^...cBBBB...",
            ".HD:xeD.eeeeCBBBB.$e",
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
            ".........Be,a,$,eb..",
            ".........BBBBBBBHB..",
            ".....cC..D,,,,,,HD..",
            ".^.e.CCeeD,e,g,eHDe.",
            "##H########H########",
            "##H________H########",
            "##Ho_o_o_e_H########",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // ---------------------------------------------------------------- SELVA: comboio abandonado e escotilha (so no cenario da selva)

        // TREM ABANDONADO: Dois vagoes enferrujados na linha: arrombe as portas e atravesse por dentro (ou corra pelo teto). Cabos partidos soltam faiscas.
        new() { Nome = "TREM ABANDONADO", Atos = Ato.Arredores | Ato.Posto, Peso = 14, Intensidade = 3, Cenarios = new[] { BgStyle.Jungle }, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "...e........a.......",
            ".VVVVVVVV..VVVVVVVV.",
            ".D**z***D..D****z*D.",
            ".D*e**r*D..D*e**e*D.",
            "~VVVVVVVV~xVVVVVVVV~",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // LOCOMOTIVA: Locomotiva abandonada: suba na caldeira, entre na cabine pela janela e saia pela porta. Poste com cabo partido ao lado da linha.
        new() { Nome = "LOCOMOTIVA", Atos = Ato.Chegada | Ato.Arredores, Peso = 12, Intensidade = 2, Cenarios = new[] { BgStyle.Jungle }, Mapa = new[]
        {
            "....................",
            "....................",
            ".........a..........",
            "........VVVV........",
            "...VV.a.***V........",
            "..VVVVVVV**D........",
            "..VVVVVVV*eD...CCce.",
            "~~VVVVVVVVVV~z~VVVVV",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // TUNEL DE TREM: Tunel de comboio abandonado atravessando um morro: o vagao esquecido la dentro so deixa passar por dentro dele. Cabos faiscando no teto.
        new() { Nome = "TUNEL DE TREM", Atos = Ato.Subsolo | Ato.Arredores, Peso = 14, Intensidade = 3, Cenarios = new[] { BgStyle.Jungle }, Mapa = new[]
        {
            "......a.....a.......",
            "....############....",
            "...##############...",
            "...BBB########BBB...",
            "...___z_VVVV_z___...",
            "..._____D**D_____...",
            "..._____D*eD_____...",
            "~~~~~e~~VVVV~~xe~~~~",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
            "####################",
        }},

        // ESCOTILHA: Escotilha de aco no meio da selva (estilo Lost): a escada desce para um bunker abandonado com cabos faiscando, guardas e um prisioneiro; sai-se pela outra escada.
        new() { Nome = "ESCOTILHA", Atos = Ato.Subsolo | Ato.Posto, Peso = 12, Intensidade = 3, Cenarios = new[] { BgStyle.Jungle }, Mapa = new[]
        {
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "....................",
            "..e.......e...x.a...",
            "####SH_S########H###",
            "####SH_S########H###",
            "####BH:BBBBBBBBBHB##",
            "####BH:::z:::z::HB##",
            "####BH:e:::e::$:HB##",
            "####BBBBBBBBBBBBBB##",
            "####################",
            "####################",
        }},

        // TRILHOS: Linha do comboio com um poste caido soltando faiscas.
        new() { Nome = "TRILHOS", Atos = Ato.Normal, Peso = 8, Intensidade = 1, Cenarios = new[] { BgStyle.Jungle }, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..o.o.....",
            "~~~z~~e~c~",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
        }},

        // VAGAO: Um vagao sozinho na linha, para atravessar por dentro.
        new() { Nome = "VAGAO", Atos = Ato.Chegada | Ato.Arredores | Ato.Posto, Peso = 8, Intensidade = 2, Cenarios = new[] { BgStyle.Jungle }, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "....e.....",
            ".VVVVVVVV.",
            ".D***z**D.",
            ".D*e**o*D.",
            "~VVVVVVVV~",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
            "##########",
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
            ".e.......e",
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
            "...ec.....",
            "...CCc....",
            ".e.CCC..e.",
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
        new() { Nome = "JAULA", Atos = Ato.Arredores | Ato.Subsolo | Ato.Posto, Peso = 6, Intensidade = 0, Recompensa = true, Mapa = new[]
        {
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            "..........",
            ".o.o......",
            "...$..e.e.",
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
            ".e.xx.eee.",
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
            "..e..a....",
            ".TTTTTTTT.",
            "..D,,,,D..",
            "..De,reD.e",
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
            ".......e.e",
            ".....#####",
            "..e..#####",
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
            ".e..e...e.",
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
            "......a...",
            "......Cc..",
            "..e...CCa.",
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
