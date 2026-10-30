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

## Modelo 3D (recebido em 24/10)
Cinco FBX exportados do Blender 5.2 em `Assets/_OdontoAR/Models/`. Todos no **mesmo espaço de
coordenadas** (encaixam entre si sem ajuste), em **metros** e com a **frente em +Z**.

| Arquivo | Conteúdo | Tamanho | Materiais |
|---|---|---|---|
| `dentes-cima.fbx` | 14 dentes superiores, um objeto por dente | 6,0 × 3,2 × 4,7 cm | Teeth, Teeth-roots |
| `Dentes-baixo.fbx` | 14 dentes inferiores, um objeto por dente | 6,4 × 3,5 × 5,1 cm | Teeth, Teeth-roots, Dentine (só nos 1º molares) |
| `maxilla-superior.fbx` | `Maxilla.l` e `Maxilla.r` | 9,2 × 6,8 × 5,4 cm | Bone-5, Suture… |
| `mandibula.fbx` | mandíbula inteira (1 malha) | 10,3 × 8,2 × 8,2 cm | Bone, Cartilage, Suture-1 |
| `nervoface2.fbx` | nervos facial (VII) e vestibulococlear (VIII), l/r | 13,1 × 11,6 × 10,4 cm | Nerve-4, Nerve-8, Nucleus |

Nomes em inglês com lado `.l`/`.r` (ex.: `Upper first molar tooth.l`, `Lower medial incisor.r`),
o que dá para mapear direto para FDI. Os nomes e a divisão parecem do atlas **Z-Anatomy**
(licença CC BY-SA 4.0): se for, é preciso citar a fonte no app/artigo.

### Problemas encontrados
1. ✅ *Resolvido com `nervoface2.fbx`.* **`nervoface.fbx` não tinha geometria.** Provavelmente o nervo é uma curva no Blender e o FBX só
   exporta malhas. Correção no Blender: selecionar o nervo → *Object → Convert → Mesh* → exportar de novo.
2. **Faltam os terceiros molares** (18, 28, 38, 48): 28 dentes em vez de 32. Aceitável na POC.
3. **Coroa e raiz não são objetos separados**: cada dente é uma malha só, com coroa e raiz como
   *submeshes* (materiais `Teeth` e `Teeth-roots`).
4. **Não existe polpa** (decisão: a POC segue sem polpa). Só os 1º molares inferiores têm `Dentine` (dentina), e não polpa. Sem isso,
   a camada "Polpa" do RF08 não tem o que mostrar.
5. **Modelo fora da origem**: as arcadas estão a ~1,5 m de altura (veio de um corpo inteiro). Na
   cena, isso é resolvido com um objeto pai que recentraliza; não precisa mexer no Blender.
6. Os objetos `.l` têm escala negativa (espelhados). O Unity lida bem; só observar se algum
   material aparecer "por dentro" no iPhone.
7. Para a abertura da boca (T4), o pivô da mandíbula precisa ficar na articulação (côndilos),
   não no centro da malha. Dá para resolver na cena, sem mexer no Blender.

### Impacto no plano
- T1–T4: sem impacto. A T3 usa os FBX reais em vez de placeholder.
- T5: a convenção de nomes muda (dentes já vêm um por objeto, com nomes em inglês). Em vez de
  renomear tudo no Blender, o validador e o catálogo FDI podem ler os nomes atuais.
- T6: coroa/raiz podem ser tratadas por material (submesh), sem separar no Blender. A polpa
  depende de alguém modelá-la (ao menos nos dentes que serão demonstrados).

## Pendências para o humano
- [X] Revisar este documento.
- [X] Trazer o modelo 3D para `Assets/_OdontoAR/Models/` quando possível.
- [X] Reexportar `nervoface.fbx` com o nervo convertido em malha → `nervoface2.fbx` (24/10): nervo facial (VII),
  núcleo motor do facial e nervo vestibulococlear (VIII), ~70 mil triângulos, mesmo espaço dos outros FBX.
  O antigo foi apagado.
- [X] Decidir: polpa será modelada? → **Não.** A POC segue sem polpa; a 4ª camada passa a ser "Nervos".
- [X] Confirmar a origem do modelo → Z-Anatomy; créditos em `docs/documents/CREDITOS.md`.
- [X] Confirmar a remoção dos pacotes não usados na T1.
- [X] A pasta `../OdontoAr_temp` (sobrou só `Logs/` e `UserSettings/`) pode ser apagada.
