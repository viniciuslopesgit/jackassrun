# Pasta de arte do Jackass Run

> As variaveis de cada heroi (velocidade, dano, tiro, especial...) ficam no arquivo **HeroConfig.cs**, na pasta do projeto.

Tudo aqui e PNG comum: edite no Aseprite, Photoshop, Krita, Piskel, LibreSprite...
- O jogo le esta pasta ao abrir. Com o jogo aberto, aperte **F5** para recarregar a arte.
- Apague um arquivo para o jogo gerar de novo a versao original.
- Mantenha o tamanho das imagens e das celulas. Fundo transparente = vazio.
- Desenhe tudo **olhando para a direita**: o jogo espelha sozinho.

## sprites/herois e sprites/inimigos (soldado, bazuqueiro, brutamontes, faca, homem_bomba, escudeiro, granadeiro, atirador, lanca_chamas)
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

## sprites/inimigos/*_cao.png
6 quadros de 24x16, pes no pixel (12, 15), olhando para a direita: 0 parado, 1-4 correndo, 5 salto (bote).
Cada cenario tem o seu: pastor alemao (selva), raptor (jurassico), lobo (medieval), rottweiler (cidade) e
cao-robo (futuro).

## sprites/inimigos/*_torreta.png
4 quadros de 32x16, base no pixel (12, 15): 0 luz acesa, 1 luz apagada, 2 e 3 = mesmo com recuo do tiro.

## sprites/objetos
- **refem.png**: mesmo formato das folhas de personagem (o refem usa as linhas Parado e Comemorando = maos para cima, e Correndo ao fugir).
- **barril.png**: 2 quadros 16x16, base em (8, 15): normal e piscando (prestes a explodir).
- **glorb.png**: 4 quadros 16x16, centro (8, 8).
- **jaula.png**: 2 quadros 32x48, base em (16, 47) (bandeira balancando). O prisioneiro e desenhado atras.
- **projeteis.png**: celulas 32x16, centro (16, 8), apontando para a direita. Linha 0 e 1 = quadros A e B.
  Colunas: 0 bala, 1 chumbo, 2 laser, 3 foguete, 4 foguete inimigo, 5 granada, 6 dinamite,
  7 bala inimiga (pinte de branco/cinza: o jogo aplica a cor da era), 8 bomba, 9 batarangue, 10 teia, 11 flecha.

## tiles/<era>.png
Folha de 8x5 tiles de **16x16** (o heroi tem ~1 bloco de altura, como no Broforce):
- Linha 0: terra com grama (topo exposto), 4 variacoes
- Linha 1: terra logo abaixo do topo, 4 variacoes
- Linha 2: terra profunda, 4 variacoes
- Linha 3: 0 tijolo (linha par), 1 tijolo (linha impar), 2 aco, 3 caixote, 4-6 rachaduras (camada por cima, 1 a 3 de dano)
- Linha 4: enfeites desenhados **em cima** do tile de grama: 0-1 tufo A (2 quadros de vento), 2-3 tufo B, 4-5 flor;
  6 porta (parte de cima), 7 porta (parte de baixo)

Nada tem contorno preto: as formas sao definidas so pelas cores e pelo sombreamento.

## cenarios/<era>/
- **ceu.png** (320x180): fundo fixo com sol/lua.
- **fundo.png** e **meio.png** (960x180, transparentes): camadas de parallax que se repetem
  na horizontal. A borda direita deve encaixar na esquerda.

Efeitos (fogo, fumaca, sangue, faiscas, brilhos, estrelas) sao gerados pelo codigo.

## Animacoes soltas dos herois (opcional)

Em vez de editar a folha `sprites/herois/NOME.png`, pode desenhar uma animacao frame a frame numa pasta:

    sprites/herois/batman/running/running0000.png, running0001.png, ...

- Um PNG por frame, todos do mesmo tamanho (ex.: 32x32), tocados por ordem alfabetica.
- O heroi olha para a DIREITA. Os pes ficam centrados na horizontal; a linha mais baixa desenhada e o chao.
- Fundo: transparente, ou uma cor solida (a cor do pixel do canto superior esquerdo vira transparente).
- Pastas aceites: stop ou idle (parado), running ou run (a correr), jumping ou jump (subindo no pulo),
  falling ou fall (caindo), climbing ou climb (escalando/escada), dash, hurt, cheer, tumble.
  A que nao existir continua a usar a folha. F5 recarrega.
- Arma principal: um PNG na pasta armour/ do heroi (ex.: sprites/herois/batman/armour/shuriken.png) substitui o
  projetil padrao desse heroi. A imagem e desenhada centrada no projetil e gira depois de lancada
  (velocidade em RotacaoArma, no HeroConfig.cs, graus por segundo). Fundo transparente.

## tiles/ponte.png, escada.png, concreto.png e parede.png

- ponte.png (64x32): linha de cima = corrimao de corda, desenhado no tile ACIMA da tabua
  (meio, poste da ponta esquerda, poste da ponta direita). Linha de baixo = tabua inteira e tabua estragada.
  So os 5px de cima da tabua contam como piso / alvo dos tiros.
- escada.png (32x16): escada (a segunda celula esta reservada). O topo da escada serve de piso. Escadas sao indestrutiveis.
- concreto.png (16x32): ponte de concreto. Celula de cima = tabuleiro; celula de baixo = pilar.
- parede.png (80x32): fundo de dentro das casas, uma coluna por era (selva, jurassico, medieval, cidade, futuro).
  Linha de cima = parede lisa; linha de baixo = parede com janela (usada na fileira de cima da sala).

## tiles/telhado.png
- 64x80: uma linha por era (selva, jurassico, medieval, cidade, futuro), celulas de 16x16.
- Colunas: 0 ponta esquerda (beiral), 1 meio, 2 ponta direita, 3 enfeite desenhado no bloco ACIMA do telhado
  (chamine, ossos, antena de TV, parabolica), que aparece sozinho em alguns blocos do meio.
- Na cidade e telha colonial de barro; no medieval, ardosia; na selva, palha; no jurassico, folhas; no futuro,
  painel solar.

## tiles/vagao.png (comboio abandonado da selva)
- 128x16, celulas de 16x16: 0 teto do vagao (com musgo), 1 chapa lateral, 2 chapa enferrujada com furo,
  3 chassi com roda (pontas do vagao), 4 trilho (desenhado em cima do chao), 5 interior de baixo (banco),
  6 interior de cima (janela partida), 7 chassi sem roda (meio do vagao).
- O jogo escolhe a peca sozinho pela vizinhanca. Os cabos eletricos que soltam faiscas sao desenhados pelo codigo.

## sprites/objetos/paraquedas.png
- paraquedas.png (24x18): copula dos paraquedistas; o ponto onde as cordas se juntam fica no fundo, ao centro
  (e ali que fica a cabeca do soldado).

## sprites/objetos/carro.png
- carro.png (96x16): carros destruidos da cidade, celulas de 32x16 com a base em (16, 15):
  0 carcaca queimada (solta fumaca), 1 taxi branco batido, 2 carro vermelho amassado.

## Era "cidade" (Sao Paulo em caos)
- tiles/cidade.png segue o mesmo formato das outras eras. A linha 0 e a calcada portuguesa (ondas pretas e
  brancas) com guia; os enfeites da linha 4 sao saco de lixo, cone e papeis/garrafa.
- cenarios/cidade/: ceu.png (azul profundo, nuvens de entardecer, fumaca), fundo.png (skyline anil) e
  meio.png (predios, arvores, muro com pixo e postes). Nas camadas, a rua fica na linha 124.

## tiles/fundo_<era>.png (paredes de fundo)
- 64x32, celulas de 16x16. Linha 0 = terra/caverna (aparece em tuneis, cavernas e buracos cavados no chao);
  linha 1 = parede de construcao (atras de escadas, torres e bunkers; fica no lugar quando os blocos da frente
  sao destruidos). 4 variacoes por linha, sorteadas por posicao.
- Use tons mais escuros e frios que os blocos da frente. O jogo acrescenta sozinho a sombra de contato
  junto aos blocos solidos vizinhos.
- Toda escada tem parede de fundo atras (terra se estiver dentro do chao, construcao fora dele).
