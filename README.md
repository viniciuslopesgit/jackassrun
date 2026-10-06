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
| Pausa | Esc / P | Start |
| Tela cheia / Música | F / M | — |

Segure contra uma parede no ar para escalar.

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

| Herói | Arma | Especial |
|---|---|---|
| Jean Rockfire | Metralhadora | Granada |
| Shotgun Sheila | Escopeta | Dinamite |
| Doc Chrono | Laser perfurante (atravessa paredes) | Congelar o tempo |
| Blastronauta | Bazuca | Jato explosivo |
| Naomi Katana | Katana (rebate balas) | Dash sombrio |

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
