# Plano de implementação — OdontoAR (POC)

Prazo: fim de novembro de 2026. Cada tarefa cabe em uma sessão do Claude Code.
Legenda: 🤖 Claude Code faz · 🧑 humano faz no Unity/Xcode/iPhone.

## Semana 1 (26/10–01/11): RA no iPhone

### T0 — Criação do projeto (do zero) ✅ (23/10)
> Não há protótipo WebGL: o projeto começa vazio, só com um modelo 3D básico da arcada.
- 🧑 Entrar no Unity Hub (licença Personal ativa) e criar o projeto com o template **Universal 3D** (Unity 6000.6.4f1).
- 🤖 Mover o projeto para a raiz do repositório, `git init`, `.gitignore`, estrutura `Assets/_OdontoAR/` e `docs/AUDITORIA.md` (estado inicial: versão, pacotes, modelo 3D disponível e sua hierarquia).
- 🧑 Copiar o modelo 3D básico para `Assets/_OdontoAR/Models/`.
- **Aceite:** projeto abre no Unity sem erros; `docs/AUDITORIA.md` revisado pelo humano.

### T1 — Pacotes e configuração iOS ✅ (24/10)
- 🤖 Adicionar ao `Packages/manifest.json`: AR Foundation, Apple ARKit XR Plugin, Input System, TextMeshPro (versões compatíveis com o Unity instalado).
- 🤖 Script `Editor/ProjectSetup.cs` (menu `OdontoAR/Configurar projeto iOS`): plataforma iOS, ARKit ligado no XR Plug-in Management, Camera Usage Description em português, "Requires ARKit", iOS mínimo compatível, Active Input Handling, nome do produto "OdontoAR", empresa "Aura Works" e bundle id.
- 🤖 Remover pacotes do template que não usamos (ver `docs/AUDITORIA.md`).
- 🧑 Rodar o menu e conferir.
- **Aceite:** projeto abre sem erros com o alvo iOS.

### T2 — Build por linha de comando ✅ (24/10)
- 🤖 `Editor/BuildScript.cs` com `BuildIOS()` gerando o projeto Xcode em `Builds/iOS`, saindo com código ≠ 0 em erro.
- ✅ `.gitignore` do Unity (Library, Temp, Builds, Logs, UserSettings) — feito na T0.
- **Aceite:** comando do `CLAUDE.md` gera o Xcode sem erro.

### T3 — Cena de RA
- 🤖 `Editor/ARSceneBuilder.cs` (menu `OdontoAR/Criar cena AR`): cria `Scenes/AR.unity` com XR Origin, AR Session, ARPlaneManager (horizontal), ARRaycastManager, EventSystem com InputSystemUIInputModule, Canvas.
- 🤖 Escrever `ARTapToPlace` e `ModelGestures` em `Scripts/AR/` (EnhancedTouch).
- 🧑 Rodar o menu, arrastar o prefab da arcada no Inspector, build e rodar no iPhone.
- **Aceite (RF01, RF02, RF04, RF05, RF07):** toque posiciona a arcada na mesa; 1 dedo gira; pinça amplia; "Restaurar vista" funciona.

### T4 — Estado do modelo e arcadas
- 🤖 Implementar a lógica de arcada (superior/inferior/ambas, abertura) em `ModelState` (classe pura) + `ArchController` (MonoBehaviour).
- 🤖 Testes EditMode do `ModelState`.
- **Aceite (RF03, RF06):** seletor e slider de abertura funcionam no iPhone; testes passam.

## Semana 2 (02–08/11): camadas e seleção

### T5 — Convenção do modelo + validador
> Modelo recebido em 24/10 (ver `docs/AUDITORIA.md`): 5 FBX do atlas Z-Anatomy, nomes em inglês
> (`Upper first molar tooth.l`), coroa e raiz como materiais (`Teeth`, `Teeth-roots`) dentro da malha
> de cada dente, **sem polpa** (decisão: POC segue sem polpa) e sem terceiros molares.
- 🤖 Mapear os nomes atuais para FDI (`Upper`/`Lower` + tipo + `.l`/`.r` → código), sem renomear no Blender.
- 🤖 `Editor/ModelValidator.cs` (menu `OdontoAR/Validar modelo`): lista objetos que não mapeiam para FDI, dentes sem os materiais de coroa/raiz, malhas sem collider.
- **Aceite:** validador sem erros nos FBX (os 4 terceiros molares ausentes são listados, não são erro).

### T6 — Camadas (RF08)
- 🤖 `LayerController`: 4 camadas — **coroa** e **raiz** (por material/submesh dos dentes), **osso** (maxila + mandíbula, sempre translúcido) e **nervos** (`nervoface2.fbx`). Cores de `OdontoARTheme`, liga/desliga a partir do `ModelState`.
- 🤖 Visualização integrada: com osso ou nervos visíveis, coroa a 55% e raiz a 70%.
- **Aceite:** 4 toggles funcionam no iPhone; FPS ≥ 30 com tudo ligado.

### T7 — Seleção e cartão FDI (RF09)
- 🤖 `StructureCatalog` (ScriptableObject em `Data/`) com nome em português dos 28 dentes do modelo (e dos 4 terceiros molares, para quando existirem), das camadas, da maxila, da mandíbula e dos nervos; chave = código FDI.
- 🤖 `SelectionController`: toque curto (sem arrastar) faz raycast nos colliders, contorno ciano na estrutura, resto a 20%.
- 🤖 Cartão flutuante ancorado ("Primeiro molar inferior esquerdo — 36", "Camada: Raiz").
- **Aceite:** tocar em 5 estruturas diferentes mostra nomes corretos; toque não conflita com girar.

## Semana 3 (09–15/11): interface, registro e oclusão

### T8 — Interface conforme o design system
- 🤖 Telas 1–3 (abertura, código do participante, permissão de câmera), 4–5 (coaching de superfície usando o estado do ARPlaneManager), 6–7 (barra superior, seletor, painel de camadas recolhível com `CamadaChip`), 9 (ajuda, com a seção **Créditos** do `docs/CREDITOS.md`).
- 🤖 Navegação simples por um `ScreenRouter`; centro da tela livre nas telas de RA.
- 🧑 Exportar os ícones como PNG para `Assets/_OdontoAR/UI/Icons`.
- **Aceite:** fluxo completo da abertura até a RA sem dead ends; textos em pt-BR.

### T9 — Registro de uso (RF10)
- 🤖 `UsageLogger`: sessões, duração, toggles de camada e estruturas tocadas, gravando JSON em `Application.persistentDataPath`, identificado só pelo código do participante.
- 🤖 Tela 10 (registro) com resumo e botão "Exportar registro (JSON)" via share sheet do iOS (pacote NativeShare ou plugin nativo mínimo).
- 🤖 Testes EditMode do logger (serialização, sessão interrompida).
- **Aceite:** JSON exportado por AirDrop/Arquivos abre e contém os eventos esperados.

### T10 — Oclusão com LiDAR
- 🤖 `AROcclusionManager` com profundidade do ambiente quando suportado; desligado sem crash em aparelhos sem LiDAR.
- **Aceite:** a mão passa na frente da arcada no 16 Pro Max.

## Semana 4 (16–22/11): testes

### T11 — Verificação do sistema (§2.8 do artigo)
- 🤖 `docs/TESTES.md`: checklist RF01–RF10 e RNF01–RNF07 com passos e resultado esperado.
- 🤖 Contador de FPS ativável por gesto escondido (3 dedos) para medição.
- 🧑 Executar o checklist em 2 superfícies e 2 iluminações; anotar FPS; piloto com 2–3 colegas.
- 🤖 Corrigir o que falhar.
- **Aceite:** checklist preenchido, sem falha bloqueante.

## Semana 5 (23–30/11): folga e entrega
- 🧑 Vídeo demo gravado no iPhone, prints para o artigo (substituir Figuras 2 e 3), apresentação.
- 🤖 Atualizar a tabela de requisitos do artigo com a situação real.

## Fora do escopo da POC
Android, modelo final validado pelo especialista, estudo com alunos, TestFlight.
