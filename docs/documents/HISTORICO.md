# Histórico e próximos passos — OdontoAR (POC)

Atualizado em 25/10/2026 (commit `ef20187`). O plano com os critérios de aceite está em `PLANO.md`;
aqui fica o que foi feito de fato, as decisões tomadas e o que falta.

## 1. Resumo

O app foi construído do zero (não havia protótipo) entre 23 e 25/10/2026. Todas as tarefas de
código do plano (T0–T11) estão prontas, verificadas no iPhone 16 Pro Max e com 126 testes EditMode
passando. Falta a parte humana da verificação (T11) e a entrega (semana 5).

| Tarefa | Entrega | Situação |
| --- | --- | --- |
| T0 | Projeto Unity 6 (URP) do zero, estrutura `Assets/_OdontoAR`, auditoria inicial | ✅ |
| T1 | AR Foundation + ARKit, configuração iOS por script (`OdontoAR/Configurar projeto iOS`) | ✅ |
| T2 | Build iOS por linha de comando (`BuildScript.BuildIOS`) | ✅ |
| T3 | Cena de RA: plano, retículo, posicionar, girar, pinça, restaurar vista, sombra | ✅ |
| T4 | `ModelState` (classe pura), seletor de arcada e abertura da boca | ✅ |
| T5 | Mapeamento dos nomes do Z-Anatomy para FDI, colliders e validador do modelo | ✅ |
| T6 | Camadas coroa, raiz, osso e nervos com visualização integrada | ✅ |
| T7 | Seleção por toque, contorno ciano e cartão FDI | ✅ |
| T8 | Interface do design system, ícone do app e tela de abertura | ✅ |
| T9 | Registro de uso em JSON e exportação | ✅ |
| T10 | Oclusão com LiDAR | ✅ |
| T11 | Checklist de verificação, contador de FPS, ajustes de posicionamento e giro | 🤖 ✅ · 🧑 em execução |

## 2. O que foi feito, por tarefa

### Base (T0–T2)
- Projeto criado no Unity Hub com o template Universal 3D e versionado no GitHub
  (`viniciusalegre20/OdontoAR`, privado), um commit por tarefa.
- Pacotes que não usamos removidos; AR Foundation, ARKit e XR Management adicionados.
- `ProjectSetup` deixa o projeto pronto para iOS de forma idempotente: nome "OdontoAR", empresa
  "Aura Works", bundle id `com.auraworks.odontoar`, versão 1.0, iOS 15, texto da permissão de câmera
  em português, ARKit no XR Plug-in Management, URP desenhando a câmera, Input System novo.
- `BuildScript` gera o Xcode em `Builds/iOS` sempre do zero; `IOSPostProcess` ajusta o projeto Xcode
  (sandbox de scripts desligado, storyboard próprio da tela de abertura). Compilação e instalação no
  iPhone por `xcodebuild` + `devicectl`, sem abrir o Xcode.

### Realidade aumentada (T3, T10)
- `ARSceneBuilder` (menu `OdontoAR/Criar cena AR`) monta `Scenes/AR.unity` e `Prefabs/Arcadas.prefab`
  com todas as referências ligadas; nada é configurado à mão no Inspector.
- `ARTapToPlace`: retículo sobre o plano detectado, toque cria uma âncora no plano, arcada em tamanho
  real, de frente para o aluno. Detecta a âncora perdida quando o ARKit reinicia o rastreamento e volta
  para a busca da mesa ("O rastreamento reiniciou. Procure a mesa de novo…").
- Sombra de contato sob a arcada e oclusão de ambiente com LiDAR (`AROcclusionManager`, "Best");
  em aparelho sem LiDAR o gerenciador não atua e nada quebra.
- Arcada nivelada pelo plano oclusal (mínimos quadrados nas pontas das cúspides): o modelo do
  Z-Anatomy vinha 14,3° inclinado.

### Modelo e interação (T4–T7)
- `ModelState` concentra arcada, abertura (0–30°), rotação (quaternion), escala (0,5×–4×), camadas
  e seleção, sem MonoBehaviour, testado em EditMode. Posicionar de novo volta à vista padrão.
- `ModelGestures`: 1 dedo na horizontal gira sobre a mesa, na vertical inclina sem limite; 2 dedos
  ampliam e rodam em torno da linha de visão. `ModelView` mantém a arcada apoiada na mesa em qualquer
  rotação e escala.
- `ArchController`: arcada superior, inferior ou ambas e abertura em torno da ATM.
- Modelo: 5 FBX do Z-Anatomy (28 dentes, maxila, mandíbula, nervos). `FdiCode` traduz os nomes em
  inglês (`Upper first molar tooth.l`) para FDI sem renomear no Blender; `ModelValidator` aponta
  nomes sem FDI, dentes sem coroa/raiz e malhas sem collider.
- `LayerController`: coroa e raiz (submalhas dos dentes), osso (sempre translúcido) e nervos; com osso
  ou nervos ligados, coroa a 55% e raiz a 70%.
- `SelectionController`: toque curto seleciona (não conflita com arrastar, pinça nem 3 dedos), prefere
  o dente quando a raiz é vista através do osso, ignora camadas ocultas; contorno ciano (shader
  próprio) e resto esmaecido a 20%. `StructureCatalog` tem os nomes em português.

### Interface (T8)
- Feita a partir do artefato de design system (sem Figma). Tokens de cor e opacidade só no
  `OdontoARTheme`; ícones SF Symbols rasterizados em `UI/Icons`; Inter e JetBrains Mono no lugar da
  SF Pro (a licença da Apple não permite embutir); vidro sem desfoque.
- Telas geradas por `ARUIBuilder`, navegação pelo `ScreenRouter`: abertura, código do participante,
  permissão de câmera (lida do iOS por plugin nativo), busca da mesa, barra superior, seletor de
  arcada, painel de camadas recolhível, cartão da estrutura, ajuda com créditos e registro.
- Ícone do app e tela de abertura do iOS com o símbolo, sem o "Made with Unity".
- Prévias das telas em PNG pelo menu `OdontoAR/Gerar prévias da interface`.

### Registro de uso (T9)
- `UsageRecorder` grava sessões, duração, camadas e estruturas tocadas em JSON
  (`Application.persistentDataPath`), no formato do `registro.md` do design system, identificado só
  pelo código do participante (RNF07). Sessões vazias com menos de 5 s são descartadas.
- Tela de registro (botão de relógio) com resumo e "Exportar registro (JSON)" pela folha de
  compartilhamento do iOS (`OdontoARShare.mm`). Verificado com exportação por AirDrop.

### Verificação (T11)
- `docs/documents/TESTES.md` segue a §2.8 do artigo: RF01–RF10, interação, estabilidade (superfície × luz),
  desempenho, compatibilidade, RNF01–RNF07, revisão do especialista e piloto.
- Contador de FPS escondido (toque com 3 dedos); FPS e eventos de rastreamento também saem no log.
- Desempenho medido: 60 FPS médio com todas as camadas, pior quadro ~31 FPS (meta ≥ 30).

## 3. Decisões

| Decisão | Motivo |
| --- | --- |
| POC só iOS (ARKit) | Aparelho disponível; o código usa AR Foundation, que também roda no Android |
| 4ª camada é **nervos**, não polpa | O modelo do Z-Anatomy não tem polpa. Atualizar o artigo |
| Sem terceiros molares | Ausentes no modelo; o catálogo já tem os nomes para quando existirem |
| Inter/JetBrains Mono em vez de SF Pro | Licença da SF Pro não permite embutir |
| Plugins nativos próprios (câmera, compartilhamento) | `HasUserAuthorization` não reflete a permissão real; evita pacotes de terceiros |
| Projeto em `~/Developer`, fora do iCloud | O iCloud criava cópias "arquivo 2.cpp" durante o build e o link do IL2CPP falhava |
| Cena, prefabs e configuração gerados por scripts de Editor | Reprodutível e sem edição à mão de `.unity`/`.prefab`/`ProjectSettings` |

## 4. Problemas resolvidos

| Problema | Causa | Solução |
| --- | --- | --- |
| Arcada "andando com a câmera" (T3) | ARKit reinicia o mundo com pouca textura/luz (`InsufficientFeatures`) e descarta a âncora | Detectar a âncora perdida e voltar à busca da mesa com aviso |
| Link do IL2CPP falhando | iCloud duplicando arquivos; build em modo "append" | Projeto fora do iCloud; build sempre do zero |
| Tela de abertura padrão do Unity no iOS | Faltava imagem de paisagem; storyboard do Unity escondia o logo | Imagens de retrato e paisagem; storyboard próprio no pós-processamento |
| Tela de permissão errada | `HasUserAuthorization` não reflete o iOS | Plugin `OdontoARCamera.mm` (AVFoundation) |
| Layout inflado | Sprites fatiados com PPU errado | Importar com 400 PPU |
| Arcada inclinada/de lado | Modelo fora do plano oclusal; nivelamento pelo centro dos dentes dava só 5° | Plano pelas pontas das cúspides (14,3°) |
| Arcada tombada ao reposicionar | Rotação anterior mantida | Vista padrão a cada posicionamento |
| Sessões vazias e versão "0.1.0" no JSON | Aberturas curtas; `bundleVersion` antigo | Descarte de sessões < 5 s sem eventos; versão 1.0 |

## 5. Próximos passos

### T11 — verificação (🧑, semana 4: 16–22/11)
1. Preencher `docs/documents/TESTES.md` no iPhone 16 Pro Max, seções 1–6.
2. **Estabilidade:** 2 superfícies (lisa e com textura) × 2 iluminações (comum e pouca luz): tempo até
   detectar, deriva após uma volta na mesa, se a arcada fica presa (confirma a correção da T3).
3. **2º aparelho**, de preferência sem LiDAR (instalação em `docs/documents/INSTALACAO.md`).
4. **Revisão do especialista** (seção 7, RNF06): dentes, nomes FDI, coroa/raiz, osso, nervos, abertura.
5. **Piloto** com 2–3 colegas (seção 8), sem explicar o app, e exportar os JSONs.
6. 🤖 Corrigir o que falhar e registrar na seção 9.

### Semana 5 — entrega (23–30/11)
- 🧑 Vídeo demo gravado no iPhone, prints para o artigo (substituir as Figuras 2 e 3), apresentação.
- 🤖 Atualizar o artigo: tabela de requisitos com a situação real, nervos no lugar de polpa, POC só
  iOS (RNF01 parcial), RNF06 conforme a revisão, resultados da T11.

### Depois da POC (fora do escopo)
- Android (ARCore): o código já usa AR Foundation; falta configurar o alvo e testar.
- Modelo final validado pelo especialista, com polpa e terceiros molares.
- Distribuição por TestFlight (exige conta paga do Apple Developer Program).
- Estudo com alunos.

## 6. Limitações conhecidas
- Com a conta Apple gratuita, o app instalado vale **7 dias** e cada Apple ID instala no máximo 3 apps
  de desenvolvimento; depois é preciso reinstalar pelo Mac (`INSTALACAO.md`).
- Sem LiDAR não há oclusão (a mão não esconde a arcada); o resto funciona.
- Em superfícies muito lisas ou com pouca luz o ARKit pode reiniciar o rastreamento; o app avisa e
  pede para posicionar de novo.
