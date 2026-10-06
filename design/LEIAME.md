# Pasta de arte do Jackass Run

Tudo aqui e PNG comum: edite no Aseprite, Photoshop, Krita, Piskel, LibreSprite...
- O jogo le esta pasta ao abrir. Com o jogo aberto, aperte **F5** para recarregar a arte.
- Apague um arquivo para o jogo gerar de novo a versao original.
- Mantenha o tamanho das imagens e das celulas. Fundo transparente = vazio.
- Desenhe tudo **olhando para a direita**: o jogo espelha sozinho.

## sprites/herois e sprites/inimigos (soldado, bazuqueiro, brutamontes)
Folha de 6 colunas x 5 linhas, celulas de **32x26**. Os pes ficam no pixel **(14, 24)** de cada celula
(centro do corpo, chao logo abaixo). Corpos girados ao morrer giram em torno de (14, 15).

| Linha | Colunas |
|---|---|
| 0 Parado | 0 normal, 1 respirando, 2 normal (variacao), 3 piscando |
| 1 Correndo | 0 a 5 (ciclo de corrida) |
| 2 No ar | 0 pulo, 1 queda A, 2 queda B, 3 aterrissagem |
| 3 Acoes | 0 escalada A, 1 escalada B, 2 dash, 3 levando dano / morto |
| 4 Extras | 0 comemorando A, 1 comemorando B, 2 voando morto A, 3 voando morto B |

Os tiros saem da ponta da arma: metralhadora/escopeta/laser/fuzil a ~(+11, -7) dos pes, bazuca a (+11, -11).
Os prisioneiros nas jaulas usam a folha do heroi correspondente.

## sprites/inimigos/*_voador.png
4 quadros de 32x24 (bater de asas), centro do corpo em (16, 12).

## sprites/inimigos/*_torreta.png
4 quadros de 32x16, base no pixel (12, 15): 0 luz acesa, 1 luz apagada, 2 e 3 = mesmo com recuo do tiro.

## sprites/objetos
- **barril.png**: 2 quadros 16x16, base em (8, 15): normal e piscando (prestes a explodir).
- **glorb.png**: 4 quadros 16x16, centro (8, 8).
- **jaula.png**: 2 quadros 32x48, base em (16, 47) (bandeira balancando). O prisioneiro e desenhado atras.
- **projeteis.png**: celulas 32x16, centro (16, 8), apontando para a direita. Linha 0 e 1 = quadros A e B.
  Colunas: 0 bala, 1 chumbo, 2 laser, 3 foguete, 4 foguete inimigo, 5 granada, 6 dinamite,
  7 bala inimiga (pinte de branco/cinza: o jogo aplica a cor da era), 8 bomba.

## tiles/<era>.png
Folha de 8x5 tiles de **16x16** (o heroi tem ~1 bloco de altura, como no Broforce):
- Linha 0: terra com grama (topo exposto), 4 variacoes
- Linha 1: terra logo abaixo do topo, 4 variacoes
- Linha 2: terra profunda, 4 variacoes
- Linha 3: 0 tijolo (linha par), 1 tijolo (linha impar), 2 aco, 3 caixote, 4-6 rachaduras (camada por cima, 1 a 3 de dano)
- Linha 4: enfeites desenhados **em cima** do tile de grama: 0-1 tufo A (2 quadros de vento), 2-3 tufo B, 4-5 flor

Nada tem contorno preto: as formas sao definidas so pelas cores e pelo sombreamento.

## cenarios/<era>/
- **ceu.png** (320x180): fundo fixo com sol/lua.
- **fundo.png** e **meio.png** (960x180, transparentes): camadas de parallax que se repetem
  na horizontal. A borda direita deve encaixar na esquerda.

Efeitos (fogo, fumaca, sangue, faiscas, brilhos, estrelas) sao gerados pelo codigo.
