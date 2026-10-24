# Auditoria — estado inicial do projeto (T0)

Data: 23/10/2026. Não existe protótipo prévio: o projeto foi criado do zero no Unity Hub
com o template **Universal 3D** e movido para a raiz do repositório.

## Versão e licença
- Unity **6000.6.4f1**.
- Modo batch funcionando com a licença Personal do Hub (testes EditMode rodaram por terminal).

## Pacotes (`Packages/manifest.json`, vindos do template)
| Pacote | Versão | Uso no OdontoAR |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.6.0 | URP — manter |
| `com.unity.inputsystem` | 1.20.0 | Gestos (EnhancedTouch) — manter |
| `com.unity.ugui` | 2.6.0 | Interface (uGUI; TextMeshPro está incluído nele no Unity 6) — manter |
| `com.unity.test-framework` | 1.8.0 | Testes EditMode — manter |
| `com.unity.ai.navigation` | 2.0.14 | Não usado — candidato a remover na T1 |
| `com.unity.timeline` | 6.6.0 | Não usado — candidato a remover na T1 |
| `com.unity.visualscripting` | 1.9.12 | Não usado — candidato a remover na T1 |
| `com.unity.collab-proxy` | 2.13.6 | Não usado (usamos git) — candidato a remover na T1 |
| `com.unity.ide.rider` / `com.unity.ide.visualstudio` | 3.0.38 / 2.0.26 | Integração com IDE — manter |

**Faltam (T1):** `com.unity.xr.arfoundation`, `com.unity.xr.arkit`, `com.unity.xr.management`.

## Configuração atual (`ProjectSettings`)
- Render pipeline: URP, com os assets `Mobile_RPAsset` e `PC_RPAsset` em `Assets/Settings/` ligados por nível de qualidade.
- Active Input Handling: `1` (só o Input System novo) — já correto.
- `productName: OdontoAr_temp` e `companyName: DefaultCompany`, herdados do Hub. **A T1 precisa corrigir** (via `ProjectSetup.cs`) para "OdontoAR" / "Aura Works" e definir o bundle id.
- Plataforma ativa: macOS (padrão). A T1 troca para iOS.

## Assets
```
Assets/
  InputSystem_Actions.inputactions   ações padrão do template (não usado; avaliar remoção)
  Scenes/SampleScene.unity           cena de exemplo do template (será substituída por Scenes/AR.unity na T3)
  Settings/                          assets do URP (Mobile/PC) e volume profiles — manter
  _OdontoAR/
    Scripts/OdontoAR.asmdef          assembly de runtime (namespace OdontoAR)
    Editor/OdontoAR.Editor.asmdef    assembly de Editor (namespace OdontoAR.Editor)
    Tests/EditMode/                  assembly de testes + SmokeTests.cs (provisório, sai na T4)
```
O tutorial do template (`TutorialInfo/`, `Readme.asset`) foi removido. As demais pastas da estrutura
(`Scripts/AR`, `Prefabs`, `Models`...) só são criadas quando tiverem conteúdo, para evitar pastas vazias
com `.meta` órfão no git.

## Modelo 3D
**Ainda não disponível** (está com um colega da equipe). Impacto:
- T1, T2 e T4 (lógica pura + testes) não dependem do modelo.
- T3 pode avançar com um **placeholder** (duas arcadas feitas de primitivas) para validar posicionamento e gestos no iPhone.
- T5 em diante (camadas, seleção, validador) dependem do modelo na convenção de nomes do `PLANO.md`.

## Pendências para o humano
- [X] Revisar este documento.
- [ ] Trazer o modelo 3D para `Assets/_OdontoAR/Models/` quando possível.
- [X] Confirmar a remoção dos pacotes não usados na T1.
- [X] A pasta `../OdontoAr_temp` (sobrou só `Logs/` e `UserSettings/`) pode ser apagada.
