# Jackass Run — Time Force Bros

Corrida infinita horizontal em C# (.NET 10 + Raylib-cs), misturando Broforce
(terreno destrutível, explosões em cadeia, resgate de prisioneiros) com
Super Time Force (rebobinar o tempo, vidas passadas que lutam ao seu lado, eras temporais).

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
- **Desabamentos**: estruturas sem apoio (sem chão embaixo nem aço segurando) caem e esmagam quem estiver embaixo.
- **Mundo em camadas**: céu, superfície e subsolo. Alguns buracos levam a **cavernas** (com prêmios) e
  **túneis** que correm por baixo da superfície; outros (sem fundo) são fatais. Plataformas e torres levam para cima.
  A câmera segue na vertical e **afasta o zoom** dentro de cavernas/túneis e em quedas longas.
- **Prédios (inspirado em Door Kickers: Action Squad)**: salas escuras com porta. Correr contra a porta a
  **arromba e atordoa** quem está atrás; cair em cima de um inimigo também atordoa. Inimigo atordoado vale o dobro.
  **Reféns**: encoste para salvar (+300); acertar um refém custa -500. Inimigos novos: **faca** (mata no contato)
  e **homem-bomba** (corre até você e explode; atire antes e ele explode nos vizinhos).
- **Eras** a cada 200 m: Selva 1985 → Jurássico → Idade Média → Futuro Neon (e repete, mais rápido).

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

## Geração dos cenários (`Game.Gen.cs`)

Inspirada no level design do Broforce. Cada era é uma **fase** de 5 trechos com arco de tensão:
**Chegada** (calma) → **Arredores** (ponte ou casa) → **Subsolo** (caverna ou túnel: dois caminhos) →
**Posto avançado** (bunker, acampamento com barris ou ponte) → **Fortaleza** (clímax, com prisioneiro no fim).

- Cada trecho tem uma **peça central** do seu ato e **recheio** sorteado por pesos, sem repetir o anterior.
- **Orçamento de inimigos** por ato (sobe ao longo da fase e da corrida): sem inimigos espalhados à toa.
- **Respiros** (chão calmo com prémios) depois de trechos intensos e **prisioneiros como recompensa**.
- **Justiça**: buracos têm corrida antes e aterrissagem sem inimigos; barris aparecem antes dos inimigos.
- Ajuste em `Fillers` (pesos), `Anchors` (peças centrais), `Budget` (inimigos por ato) e `Cost` (preço de cada inimigo).
- Do **Metal Slug** (`Game.Slug.cs`): **emboscadas** — tropas que entram correndo pela direita ou caem de
  paraquedas quando o herói passa por um ponto.

## Arte editável (pasta `design/`)

Todos os sprites, tiles e cenários são PNGs em `design/` — edite em qualquer editor de pixel art
(Aseprite, Krita, Photoshop, Piskel...). Veja `design/LEIAME.md` para o formato de cada folha.

- Com o jogo aberto, aperte **F5** para recarregar a arte.
- Apague um PNG para o jogo gerar de novo a versão original.
- Efeitos (fogo, fumaça, sangue, faíscas, brilhos) continuam gerados por código.

## Estrutura

- `Game.cs` — loop, estados, rebobinar/seleção
- `Game.Sim.cs` — física, jogador, fantasmas, inimigos, armas, explosões
- `Game.Gen.cs` — geração procedural determinística por chunk
- `Game.Draw.cs` — cenários com parallax, tiles, HUD e telas
- `Art.cs` — carrega a arte de `design/` · `DesignExport.cs` — gera a arte padrão que faltar
- `Gfx.cs` / `Sprites.cs` — desenho procedural usado para gerar a arte padrão · `Sfx.cs` — sons e música sintetizados
- `Game.Bot.cs` — autoteste: `JACKASS_AUTOTEST=<pasta> dotnet run` joga sozinho e salva screenshots
