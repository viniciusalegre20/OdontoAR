# Verificação do sistema (T11, §2.8 do artigo)

Testes para garantir que o app está apto ao uso. **Não fazem parte do experimento da pesquisa.**
Requisitos conforme o artigo (Quadro 1 e §2.3). Marque cada linha com **OK**, **Falha** ou **N/A** e
anote o que observou. Uma falha **bloqueante** impede o uso com estudantes; as outras viram ajuste.

- **Versão testada:** 1.0 (build de ____/____)
- **Aparelhos:** iPhone 16 Pro Max (LiDAR) · ____________________ (2º aparelho)
- **Equipe / data:** ______________________

**Contador de FPS:** toque na tela com **3 dedos ao mesmo tempo**; a pílula no canto superior esquerdo
mostra a média e o pior quadro do último meio segundo (ciano: ≥ 30; vermelho: abaixo). Repita o gesto
para esconder. O mesmo valor sai no log a cada 5 s (`[OdontoAR] FPS médio …`).

**Coluna "Dev":** o que já foi verificado durante o desenvolvimento (24–25/10, 16 Pro Max), como
referência. Não substitui a execução do checklist.

---

## 1. Testes funcionais (RF01–RF10)

| ID | Requisito | Passos | Resultado esperado | Dev | Resultado | Observações |
| --- | --- | --- | --- | --- | --- | --- |
| RF01 | Visualizar a arcada em 3D sobreposta ao ambiente real por RA | Abrir o app, passar pelo código e pela câmera; apontar para uma mesa e tocar quando o retículo aparecer | A arcada aparece sobre a mesa, em tamanho real, com a imagem da câmera ao fundo | OK | | |
| RF02 | Posicionar o modelo sobre uma superfície detectada pela câmera | Apontar para a mesa até "Superfície detectada"; tocar; depois "Reposicionar" e tocar em outro ponto | A arcada apoia na mesa onde estava o retículo, de frente para o aluno; "Reposicionar" volta ao retículo | OK | | |
| RF03 | Selecionar arcada superior, inferior ou ambas | Tocar em Superior, Inferior e Ambas no seletor | Só a arcada escolhida aparece; "Ambas" mostra as duas | OK | | |
| RF04 | Rotacionar por gesto de arrastar | Arrastar 1 dedo na horizontal e na vertical; girar 2 dedos | Horizontal: gira sobre a mesa. Vertical: inclina sem limite (vista por cima, por baixo, oclusal). 2 dedos girando: inclina para os lados. A arcada continua apoiada na mesa | | | |
| RF05 | Alterar a escala (zoom) | Pinça para ampliar e reduzir | A arcada cresce até 4× e diminui até 0,5× o tamanho real, continuando apoiada na mesa | OK | | |
| RF06 | Ajustar a abertura entre as arcadas | Com "Ambas", arrastar o slider "Abertura entre arcadas" | A arcada superior abre em torno da ATM de 0° a 30°; o valor em graus acompanha; com uma só arcada o slider fica desativado | OK | | |
| RF07 | Restaurar a vista inicial | Girar e ampliar; tocar em "Restaurar vista" | Giro, inclinação e zoom voltam ao padrão; arcada, abertura e camadas continuam como estavam | OK | | |
| RF08 | Exibir e ocultar camadas (coroa, raiz, osso e nervos)¹ | Tocar em cada chip de camada; desligar osso e nervos | Cada camada some e volta; o chip mostra "Visível"/"Oculta"; com osso ou nervos ligados, coroa e raiz ficam translúcidas | OK | | |
| RF09 | Selecionar uma estrutura e exibir seu nome | Tocar em 5 estruturas diferentes (ex.: 11, 36, 43, maxila, mandíbula) | Contorno ciano na estrutura, resto esmaecido; cartão com nome em português, FDI e camada ("Primeiro molar inferior esquerdo — 36", "Camada: Raiz") | OK | | |
| RF10 | Registrar o uso (tempo de sessão, camadas e estruturas) | Usar o app 2 min; abrir o Registro (relógio); "Exportar registro (JSON)" por AirDrop | Resumo com tempo, sessões e camadas; JSON abre e contém sessões, camadas e seleções com os tempos | OK | | |

¹ O Quadro 1 do artigo cita **polpa e osso alveolar**. A POC usa o modelo do Z-Anatomy, que não tem
polpa; a 4ª camada é **nervos** (decisão registrada em `docs/AUDITORIA.md`). Atualizar o artigo.

## 2. Testes de interação

| # | Situação | Passos | Resultado esperado | Dev | Resultado | Observações |
| --- | --- | --- | --- | --- | --- | --- |
| I1 | Arrastar não seleciona | Arrastar 1 dedo começando em cima de um dente | Só gira; nenhum cartão abre | OK | | |
| I2 | Pinça não seleciona | Fazer pinça com um dedo em cima de um dente | Só amplia/reduz | OK | | |
| I3 | 3 dedos não posicionam nem selecionam | Tocar com 3 dedos antes e depois de posicionar | Só liga/desliga o contador de FPS | | | |
| I4 | Toque em botão não atravessa | Tocar nos botões e chips com a arcada logo atrás | Só o botão responde; o modelo não gira nem seleciona |   | | |
| I5 | Seleção através do osso | Com osso ligado, tocar sobre uma raiz vista através da maxila | Seleciona o dente (camada Raiz), não o osso |   | | |
| I6 | Camada oculta não é tocável | Desligar a coroa; tocar onde ela estava | Não seleciona a coroa | | | |
| I7 | Seleção some com a estrutura | Selecionar um dente superior; tocar em "Inferior" | O cartão fecha | | | |
| I8 | Painel recolhe com o cartão | Selecionar uma estrutura | O painel de camadas recolhe; ao abrir o painel, o cartão fecha |   | | |
| I9 | Ajuda | Tocar em "?"; rolar até Créditos; "Entendi" | Folha abre com os 4 gestos e os créditos do Z-Anatomy; fecha | OK | | |
| I11 | Reposicionar começa reto | Girar/inclinar a arcada; "Reposicionar" e tocar na mesa | A arcada nasce de frente, reta e em tamanho real; abertura e camadas continuam | | | |
| I12 | Rastreamento reiniciado | Cobrir a câmera ou apontar para uma superfície lisa até aparecer "Rastreamento instável" | Se o ARKit reiniciar, o app volta para "O rastreamento reiniciou. Procure a mesa de novo…" em vez de deixar a arcada fora do lugar | | | |
| I10 | Segunda abertura | Fechar o app pelo seletor de apps e abrir de novo | Vai direto para a RA (código e câmera já salvos) | OK | | |

## 3. Posicionamento e estabilidade (RF02, RNF03)

Para cada combinação: posicionar a arcada ao lado de um objeto de referência (ex.: uma caneca),
**andar uma volta ao redor da mesa** e voltar ao ponto inicial.

- **Tempo até "Superfície detectada"** (s), a partir de apontar para a mesa.
- **Deriva:** quanto a arcada saiu do lugar em relação à caneca depois da volta (cm, a olho).
- **Estabilidade:** a arcada parece presa à mesa ou "anda junto com a câmera"? (pendência da T3)

| Superfície | Iluminação | Tempo até detectar (s) | Deriva após a volta (cm) | Presa à mesa? | Observações |
| --- | --- | --- | --- | --- | --- |
| Mesa lisa (ex.: fórmica clara) | Luz comum de sala de estudo | | | | |
| Mesa lisa | Pouca luz (luminária ou só janela) | | | | |
| Superfície com textura (ex.: madeira, carteira) | Luz comum de sala de estudo | | | | |
| Superfície com textura | Pouca luz | | | | |

Também verificar: **oclusão** — passar a mão entre o iPhone e a arcada (16 Pro Max): a mão cobre a
arcada. Dev: **OK** (24/10). Resultado: ____

## 4. Desempenho (RNF02, meta ≥ 30 FPS)

Ligar o contador (3 dedos) e anotar média e pior quadro depois de ~30 s em cada cenário.

| Cenário | Média (FPS) | Pior quadro (FPS) | Dev (16 Pro Max) | Observações |
| --- | --- | --- | --- | --- |
| Procurando superfície | | | | |
| Arcada posicionada, todas as camadas ligadas | | | 60 / ~31 (log, 24/10) | |
| Estrutura selecionada (contorno + esmaecido) | | | | |
| Girando e ampliando sem parar | | | | |
| Abertura em 30°, ampliada 4× | | | | |

## 5. Compatibilidade (≥ 2 aparelhos)

| Aparelho | iOS | LiDAR | Instala e abre | Fluxo completo (RF01–RF10) | FPS (todas as camadas) | Observações |
| --- | --- | --- | --- | --- | --- | --- |
| iPhone 16 Pro Max | | Sim | | | | |
| | | | | | | Sem LiDAR, a oclusão não atua (esperado); o resto deve funcionar |

Requisitos do app: iOS 15 ou mais e ARKit (iPhone XS ou mais novo). Para instalar no 2º aparelho com a
conta gratuita: conectar ao Mac e rodar o build pelo Xcode (o perfil vale 7 dias).

## 6. Requisitos não funcionais (RNF01–RNF07)

| ID | Requisito | Como verificar | Situação | Observações |
| --- | --- | --- | --- | --- |
| RNF01 | Plataforma: smartphones e tablets Android e iOS com RA | Seção 5 | **Parcial** | A POC é só iOS (ARKit); Android (ARCore) fora do escopo da POC. O código usa AR Foundation, que também roda no Android |
| RNF02 | Desempenho ≥ 30 FPS | Seção 4 | | |
| RNF03 | Modelo estável na superfície com iluminação comum | Seção 3 | | Pendência conhecida da T3 |
| RNF04 | Uso sem treinamento, controles em português | Piloto (seção 8): os colegas chegam à arcada e usam camadas e seleção sem ajuda | | |
| RNF05 | Funciona offline; registros no aparelho | Ligar o **modo avião** e repetir o fluxo completo e a exportação (salvar em Arquivos) | | O app não usa rede |
| RNF06 | Modelo a partir de referências e validado por especialista | Seção 7 | **Pendente** | Modelo do Z-Anatomy (BodyParts3D); falta a revisão |
| RNF07 | Sem dados pessoais nos registros, só o código | Abrir o JSON exportado: só código, datas e eventos; sem nome, e-mail, localização ou imagens | | Dev: OK (JSON de 24/10) |

## 7. Revisão do modelo anatômico (especialista, RNF06)

| Item | Pergunta ao especialista | Parecer |
| --- | --- | --- |
| M1 | Os 28 dentes estão na posição e na forma corretas? | |
| M2 | Os nomes e os números FDI do cartão estão corretos (conferir 1 dente de cada quadrante)? | |
| M3 | A separação coroa/raiz está no lugar certo (junção amelocementária)? | |
| M4 | Maxila e mandíbula: proporção e relação com os dentes adequadas para o ensino? | |
| M5 | Nervos: os nervos facial (VII) e vestibulococlear (VIII) fazem sentido neste contexto? | |
| M6 | A ausência dos terceiros molares e da polpa compromete o uso planejado? | |
| M7 | A abertura (rotação em torno da ATM até 30°) é anatomicamente aceitável? | |

## 8. Piloto com 2–3 colegas (RNF04)

Sem explicar o app antes. Entregar o iPhone na tela de abertura e pedir, uma tarefa por vez:

1. "Entre com o código P-9xx e coloque a arcada sobre a mesa."
2. "Mostre só a arcada inferior e depois as duas."
3. "Abra a boca do modelo."
4. "Deixe visíveis só as raízes e o osso."
5. "Encontre o primeiro molar inferior esquerdo e diga o número dele." (resposta: 36)
6. "Volte a vista ao início."
7. "Exporte o registro de uso."

| Colega | Tarefas concluídas sem ajuda (de 7) | Onde travou | Comentários |
| --- | --- | --- | --- |
| 1 | | | |
| 2 | | | |
| 3 | | | |

## 9. Falhas encontradas

| # | Seção / ID | Descrição | Bloqueante? | Correção | Status |
| --- | --- | --- | --- | --- | --- |
| 1 | RNF03 | Arcada às vezes parece "andar junto com a câmera" (pendência da T3) | A avaliar na seção 3 | | Aberta |

## Conclusão

- [ ] Checklist preenchido (seções 1–8)
- [ ] Nenhuma falha bloqueante aberta
- [ ] Revisão do especialista registrada (ou pendência justificada no artigo)
