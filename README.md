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
- **Eras** a cada 200 m: Selva 1985 → Jurássico → Idade Média → Futuro Neon (e repete, mais rápido).

## Heróis

| Herói | Arma | Especial |
|---|---|---|
| Jean Rockfire | Metralhadora | Granada |
| Shotgun Sheila | Escopeta | Dinamite |
| Doc Chrono | Laser perfurante (atravessa paredes) | Congelar o tempo |
| Blastronauta | Bazuca | Jato explosivo |
| Naomi Katana | Katana (rebate balas) | Dash sombrio |

## Estrutura

- `Game.cs` — loop, estados, rebobinar/seleção
- `Game.Sim.cs` — física, jogador, fantasmas, inimigos, armas, explosões
- `Game.Gen.cs` — geração procedural determinística por chunk
- `Game.Draw.cs` — cenários com parallax, tiles, HUD e telas
- `Gfx.cs` — sprites pixel art procedurais · `Sfx.cs` — sons e música sintetizados
- `Game.Bot.cs` — autoteste: `JACKASS_AUTOTEST=<pasta> dotnet run` joga sozinho e salva screenshots
