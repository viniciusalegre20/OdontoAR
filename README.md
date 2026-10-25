# OdontoAR

App iOS de Realidade Aumentada para estudantes de Odontologia. A arcada dentária 3D aparece sobre a
mesa, em tamanho real, e o aluno explora coroa, raiz, osso alveolar e nervos, com os nomes em português
e a notação FDI. Trabalho da UPX (Facens), equipe **Aura Works**. Prazo da POC: fim de novembro de 2026.

**Situação (25/10/2026):** T0–T10 concluídas e verificadas no iPhone 16 Pro Max; T11 (verificação do
sistema) com a parte de código pronta e os testes com pessoas em execução. Detalhes em
[`docs/HISTORICO.md`](docs/HISTORICO.md).

## Documentação

| Documento | Conteúdo |
| --- | --- |
| [`docs/HISTORICO.md`](docs/HISTORICO.md) | O que foi feito, decisões, problemas resolvidos e próximos passos |
| [`docs/INSTALACAO.md`](docs/INSTALACAO.md) | Como montar o ambiente em outro Mac e instalar o app em outro iPhone |
| [`docs/PLANO.md`](docs/PLANO.md) | Plano de implementação (T0–T11) com critérios de aceite |
| [`docs/TESTES.md`](docs/TESTES.md) | Checklist de verificação (§2.8 do artigo) |
| [`docs/AUDITORIA.md`](docs/AUDITORIA.md) | Estado inicial do projeto e do modelo 3D |
| [`docs/CREDITOS.md`](docs/CREDITOS.md) | Atribuições (Z-Anatomy, fontes) |
| [`CLAUDE.md`](CLAUDE.md) | Regras do projeto e comandos de build |

## Funcionalidades

- **RA:** detecção da mesa, retículo, arcada ancorada em tamanho real com sombra de contato; oclusão
  com LiDAR (a mão passa na frente); volta para a busca da mesa se o ARKit reiniciar o rastreamento.
- **Gestos:** 1 dedo gira sobre a mesa (horizontal) e inclina sem limite (vertical); 2 dedos ampliam
  (0,5×–4×) e rodam; "Restaurar vista" e "Reposicionar".
- **Arcadas:** superior, inferior ou ambas; abertura de 0° a 30° em torno da ATM.
- **Camadas:** coroa, raiz, osso e nervos, com transparência quando o osso ou os nervos estão visíveis.
- **Seleção:** toque em um dente ou osso mostra contorno ciano e cartão ("Primeiro molar inferior
  esquerdo — 36", "Camada: Raiz").
- **Registro de uso:** sessões, camadas e estruturas, só com o código do participante; exportação em
  JSON pela folha de compartilhamento do iOS. Tudo offline.

## Stack

Unity 6000.6.4f1 (URP) · AR Foundation 6.6.2 + Apple ARKit XR Plugin 6.6.2 · Input System
(EnhancedTouch) · uGUI + TextMeshPro (Inter, JetBrains Mono) · plugins nativos iOS em Objective-C++.
Requisitos: iPhone com iOS 15 ou mais e ARKit; LiDAR é opcional (só para a oclusão).

## Início rápido (Mac já configurado)

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity"
cd ~/Developer/OdontoAR
"$UNITY" -batchmode -quit -projectPath . -executeMethod OdontoAR.Editor.BuildScript.BuildIOS -logFile Builds/build.log
```

Depois compile e instale com `xcodebuild` e `devicectl` (passo a passo em
[`docs/INSTALACAO.md`](docs/INSTALACAO.md#5-compilar-e-instalar-no-iphone)).

## Estrutura

```
Assets/_OdontoAR/
  Scripts/AR/          posicionamento, gestos, seleção
  Scripts/Model/       estado do modelo (classe pura), arcadas, camadas, catálogo FDI
  Scripts/UI/          telas, painéis, cartão
  Scripts/Logging/     registro de uso (RF10)
  Scripts/App/         preferências, permissão de câmera, compartilhamento
  Scripts/Diagnostics/ FPS e log de rastreamento
  Scripts/Theme/       OdontoARTheme (tokens do design system)
  Editor/              build, configuração, geração de cena/UI, validador do modelo
  Plugins/iOS/         OdontoARCamera.mm, OdontoARShare.mm
  Models/  Materials/  Prefabs/  Scenes/  Data/  UI/  Shaders/
  Tests/EditMode/      126 testes EditMode
docs/                  documentação
```

## Licenças

Modelos 3D adaptados do Z-Anatomy (BodyParts3D © DBCLS), CC BY-SA 4.0. Fontes Inter e JetBrains
Mono, SIL OFL 1.1. Ver [`docs/CREDITOS.md`](docs/CREDITOS.md).
