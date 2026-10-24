# OdontoAR — contexto para o Claude Code

App iOS de Realidade Aumentada para estudantes de Odontologia: a arcada dentária 3D aparece sobre a mesa e o aluno explora coroa, raiz, osso alveolar e nervos (a POC não tem polpa: o modelo não inclui). Trabalho da UPX (Facens), equipe Aura Works. Prazo da POC: **fim de novembro de 2026**.

O plano de implementação está em `docs/PLANO.md`. Trabalhe **uma tarefa por vez** (T0, T1...) e só marque como concluída quando os critérios de aceite forem atendidos.

## Stack
- Unity 6 (6000.6.4f1), URP, C#
- AR Foundation + Apple ARKit XR Plugin (alvo: iOS; Android fica para depois, então não use APIs exclusivas do iOS fora de `#if UNITY_IOS`)
- Input System novo (`EnhancedTouch`), nunca `UnityEngine.Input`
- uGUI + TextMeshPro (fonte Inter)
- Aparelho de teste: iPhone 16 Pro Max (tem LiDAR)

## Estrutura
```
Assets/_OdontoAR/
  Scripts/AR/        posicionamento, gestos, oclusão
  Scripts/Model/     arcadas, camadas, seleção, catálogo FDI
  Scripts/UI/        telas e painéis
  Scripts/Logging/   registro de uso (RF10)
  Scripts/Theme/     OdontoARTheme.cs (tokens do design system)
  Editor/            build por linha de comando, validadores
  Prefabs/  Scenes/  Materials/  Models/  Data/
  Tests/EditMode/    testes EditMode (asmdef OdontoAR.Tests.EditMode)
```
Assemblies: `OdontoAR` (Scripts/), `OdontoAR.Editor` (Editor/), `OdontoAR.Tests.EditMode`. Ao usar um pacote novo (AR Foundation, Input System...), adicione a referência no `.asmdef` correspondente.

## Regras
- Todo texto visível ao usuário em **português do Brasil**, com nomenclatura anatômica e **notação FDI** ("Primeiro molar inferior esquerdo — 36").
- Cores e opacidades **somente** via `OdontoARTheme`. Nunca escreva hex solto.
- Estado do modelo (arcada, abertura, escala, camadas, seleção) fica em uma classe de estado pura (sem MonoBehaviour), testável em EditMode. MonoBehaviours só leem/escrevem esse estado.
- Modelos 3D vêm do Z-Anatomy (CC BY-SA 4.0): a atribuição em `docs/CREDITOS.md` deve aparecer no app e no artigo.
- Nada de rede: o app funciona offline (RNF05). Registros sem dados pessoais, só o código do participante (RNF07).
- Não edite arquivos `.unity`, `.prefab` ou `ProjectSettings/*.asset` à mão. Para mudar cena ou configuração, escreva um script de Editor (menu `OdontoAR/...`) e peça para o humano executá-lo.
- Não há protótipo prévio: o projeto foi criado do zero (T0). Para testar no Editor sem aparelho, controles de mouse ficam em `#if UNITY_EDITOR` ou num componente que possa ser desativado.

## O que o humano faz (o Claude Code não consegue)
- Abrir o Unity, rodar menus `OdontoAR/...`, ligar referências no Inspector quando pedido.
- Abrir o projeto Xcode gerado, assinar com o Apple ID e rodar no iPhone.
- Testar no aparelho e relatar o resultado.

## Comandos
Modo batch exige o Unity Hub logado com licença ativa (sem isso, sai com código 198).
- Build iOS por terminal (após T2):
  `"/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity" -batchmode -quit -projectPath . -executeMethod OdontoAR.Editor.BuildScript.BuildIOS -logFile Builds/build.log`
- Testes EditMode:
  `... -batchmode -quit -projectPath . -runTests -testPlatform EditMode -testResults Builds/tests.xml`

## Commits
Um commit por tarefa, mensagem em português: `T4: porta gestos do protótipo para RA`.
