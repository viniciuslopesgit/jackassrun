# Jackass Run — Time Force Bros

Corrida infinita horizontal em C# (.NET 10 + Raylib-cs), misturando Broforce
(terreno destrutível, explosões em cadeia, resgate de prisioneiros) com
Super Time Force (rebobinar o tempo, vidas passadas que lutam ao seu lado).

## Rodar

```bash
cd JackassRun && dotnet run
```

## Controles

| Ação | Teclado | Controle |
|---|---|---|
| Mover | Setas / A D | Analógico / D-pad |
| Pular | Espaço / W / ↑ | A |
| Atirar | J / Z | X / RT |
| Especial | K / X | B |
| Time Out (voltar no tempo) | L / C | Y |
| Subir / descer escada | W ↑ / S ↓ | D-pad |
| Descer através da ponte | S / ↓ | D-pad ↓ |
| Pausa | Esc / P | Start |
| Tela cheia / Ligar som (começa mudo) | F / M | — |

Segure contra uma parede no ar para escalar. Pontes de madeira: atire nas tábuas para derrubar os inimigos no abismo — e não pare em cima, a tábua pisada cai 1 segundo depois (`Tune.BridgeFall`). Pontes de concreto são firmes: balas não as estragam, só explosões.

## Mecânicas

- **Time Out**: ao morrer (ou apertar L), o tempo rebobina até 10s. Escolha onde parar e um herói;
  sua vida anterior vira um fantasma que repete exatamente o que você fez, atirando junto.
- **Salvar seu eu do passado**: mate o inimigo que matou seu fantasma antes da hora e ganhe um escudo.
- **Prisioneiros nas jaulas**: +1 Time Out. **Glorbs**: 12 dão +1 especial.
- **Terreno destrutível**: balas quebram tijolos/terra/caixotes; explosões abrem crateras. Aço é indestrutível.
- **Desabamentos**: estruturas sem apoio caem e esmagam quem estiver embaixo. Alguns blocos **nunca caem** (terra,
  concreto, aço) e seguram o que estiver encostado neles; o **telhado** solto nem sempre cai e, quando cai, parte-se
  no chão. Ajuste em `GameConfig.cs` (`TerraNuncaCai`, `TijoloNuncaCai`, `ChanceTelhadoCair`...).
- **Corpos** continuam com gravidade: se o chão embaixo de um corpo for destruído, ele volta a cair.
- **Mundo em camadas**: céu, superfície e subsolo. Alguns buracos levam a **cavernas** (com prêmios) e
  **túneis** que correm por baixo da superfície; outros (sem fundo) são fatais. Plataformas e torres levam para cima.
  A câmera segue na vertical e **afasta o zoom** dentro de cavernas/túneis e em quedas longas.
- **Prédios (inspirado em Door Kickers: Action Squad)**: salas escuras com porta. Correr contra a porta a
  **arromba e atordoa** quem está atrás; cair em cima de um inimigo também atordoa. Inimigo atordoado vale o dobro.
  **Reféns**: encoste para salvar (+300); acertar um refém custa -500.
- **Inimigos** (entram aos poucos ao longo da corrida, com a aparência do cenário de cada level):
  soldado, bazuqueiro, brutamontes, torreta, voador, **faca** (mata no contato), **homem-bomba** (corre até você e
  explode; atire antes e ele explode nos vizinhos), e ainda:
  - **Escudeiro**: o escudo para os tiros pela frente e ele demora a virar — pule por cima e atire pelas costas,
    ou use explosão, pisão ou facada.
  - **Granadeiro**: ergue o braço (aviso) e joga granadas em arco, que passam por cima de coberturas e acertam
    outras alturas.
  - **Atirador de elite**: fica em postos altos; a mira laser vermelha segue você e trava (pisca) antes do tiro,
    que é muito rápido. Saia da linha ou esconda-se atrás de um bloco.
  - **Lança-chamas**: chega perto e solta um jato de fogo curto (queima caixas e pontes); o tanque nas costas
    explode quando ele morre e leva os vizinhos.
  - **Cão de ataque**: rosna, corre muito e dá o bote (a mordida mata). Um tiro basta. É um raptor na Terra
    Jurássica, um lobo no castelo e um cão-robô no futuro.
  Valores de tempo, alcance e velocidade de cada um ficam no bloco `Tune` de `Game.Sim.cs`.
- **Inimigos perseguem você pelo terreno**: alertados, pulam caixas, degraus e muros baixos (até
  `GameConfig.InimigosSobemBlocos`), saltam buracos pequenos, descem de beiradas para chegar até você (até
  `InimigosDescemBlocos`) e usam escadas. O da faca escala paredes. Quantidade de inimigos: `GameConfig.QuantidadeInimigos`.
- **Levels** (distância de cada um em `GameConfig.MetrosPorLevel`; a corrida nunca para), cada um com o seu cenário: 1 Selva de Guerra → 2 Terra Jurássica →
  3 Castelo Medieval → 4 **São Paulo em Caos** → 5 Futuro Neon; depois os cenários repetem, cada vez mais difíceis.
  O fim de cada level tem uma **bandeira**: ao passar, aparece o balanço (inimigos, resgates) e um bônus de pontos.
  Na cidade: prédios detalhados, calçada portuguesa, viadutos de concreto, pixo, incêndios e **carros destruídos** que explodem depois de alguns tiros.

## Heróis

> Personagens de terceiros, só para teste: troque por heróis originais antes de publicar/vender.

| Herói | Arma | Especial | Movimento |
|---|---|---|---|
| Batman | Batarangue (vai e volta, atravessa) | Bomba de fumaça (atordoa em volta) | Plana com a capa (segure pular) |
| Tomb Raider | Pistolas duplas | Flecha explosiva | Pulo duplo |
| Homem-Aranha | Teia (prende/atordoa) | Leque de teias | Pula mais alto, escala mais rápido |

## Variáveis dos heróis (`HeroConfig.cs`)

Velocidade de corrida, força do pulo, pulos no ar, intervalo/velocidade/dano/alcance do tiro, atordoamento,
especiais iniciais, raio e quantidade do especial... Cada variável tem um comentário explicando.
Edite os números de cada herói em `HeroConfig.cs` e rode o jogo de novo (`dotnet run`).

## Geração dos mapas: módulos desenhados (`Modulos.cs`)

O mapa não é sorteado bloco a bloco. Cada trecho de 40 m é montado com **módulos desenhados à mão** — grades
de texto de 20 colunas (ou duas metades de 10) — e o jogo sorteia as **variações** em cima dessa base:

- **qual módulo** entra (por pesos, conforme a parte do level, sem repetir o anterior), **espelhado** ou não,
  e **em que altura** (os módulos encaixam pela altura do chão das pontas, por isso não surgem colunas soltas);
- blocos opcionais: `c` caixa ou nada, `b` tijolo ou nada (vira janela), **grupos** `1 2 3` (tijolo) e `4 5 6`
  (terra) que aparecem ou somem juntos;
- **inimigos**: `e`/`a`/`g`/`m`/`w` só entram se couberem no **orçamento** do level (no começo há menos); em
  maiúscula (`E A G M`) aparecem sempre. O tipo do inimigo comum é sorteado conforme o avanço da corrida;
- **eventos do Metal Slug**: `!` emboscada (tropas entram correndo pela direita) e `^` paraquedistas.

Cada **level** segue o arco do Broforce: **Chegada** (calma) → **Arredores** (casas, vila, pontes, torres) →
**Subsolo** (caverna, túnel duplo, mina, trincheira) → **Posto avançado** (bunker, acampamento, prédio) →
**Fortaleza** (último trecho, com o prisioneiro). Depois de um módulo intenso vem, de preferência, um respiro, e a
jaula de prisioneiro aparece como recompensa logo depois de um desafio.

Para criar um módulo, copie um existente em `Modulos.cs`, desenhe com a legenda que está no topo do arquivo
(`#` terra, `B` tijolo, `T` telhado, `D` porta, `H` escada, `,` dentro de casa...) e rode o jogo de novo.
Se algo estiver errado no desenho (linha com tamanho diferente, caractere desconhecido, ponta sem chão), o jogo
avisa o nome do módulo e a linha.

## Arte editável (pasta `design/`)

Todos os sprites, tiles e cenários são PNGs em `design/` — edite em qualquer editor de pixel art
(Aseprite, Krita, Photoshop, Piskel...). Veja `design/LEIAME.md` para o formato de cada folha.

- Com o jogo aberto, aperte **F5** para recarregar a arte.
- Apague um PNG para o jogo gerar de novo a versão original.
- Efeitos (fogo, fumaça, sangue, faíscas, brilhos) continuam gerados por código.

## Estrutura

- `Game.cs` — loop, estados, rebobinar/seleção
- `Game.Sim.cs` — física, jogador, fantasmas, inimigos, armas, explosões
- `Modulos.cs` — biblioteca de módulos de mapa (desenhos + legenda) · `Game.Gen.cs` — monta os chunks com os
  módulos, sorteia as variações e distribui inimigos (determinístico por chunk, para o rebobinar)
- `Game.Inimigos.cs` — escudeiro, granadeiro, atirador de elite, lança-chamas e cão
- `Game.Draw.cs` — cenários com parallax, tiles, HUD e telas
- `Art.cs` — carrega a arte de `design/` · `DesignExport.cs` — gera a arte padrão que faltar
- `Gfx.cs` / `Sprites.cs` — desenho procedural usado para gerar a arte padrão · `Sfx.cs` — sons e música sintetizados
- `Game.Bot.cs` — autoteste: `JACKASS_AUTOTEST=<pasta> dotnet run` joga sozinho e salva screenshots
