# Instalação em outro Mac e em outro iPhone

O app não está na App Store nem no TestFlight: ele é instalado pelo Mac, com o Xcode, em um iPhone
conectado. Escolha o caso:

- **A. Outro iPhone, mesmo Mac já configurado** → vá direto à [seção 4](#4-preparar-o-iphone) e à
  [seção 5](#5-compilar-e-instalar-no-iphone).
- **B. Outro Mac (outro membro da equipe)** → siga tudo, da seção 1 à 5.

Requisitos do iPhone: **iOS 15 ou mais** e suporte a ARKit (iPhone XS ou mais novo). LiDAR (linha Pro
a partir do 12 Pro) é opcional: sem ele, só a oclusão da mão deixa de funcionar. Android não é suportado
na POC.

## 1. Instalar as ferramentas (Mac)

1. **Xcode** pela App Store. Abra uma vez para instalar os componentes e confirme que ele está
   selecionado (só com as Command Line Tools o build falha em `actool`):
   ```sh
   xcode-select -p          # deve mostrar /Applications/Xcode.app/Contents/Developer
   sudo xcode-select -s /Applications/Xcode.app/Contents/Developer   # se não mostrar
   ```
2. **Unity Hub** (unity.com/download). Entre com uma conta Unity e ative a licença **Personal**
   (Hub → Preferências → Licenças). Sem licença, o modo batch sai com código 198.
3. No Hub, instale o editor **6000.6.4f1** com o módulo **iOS Build Support**. A versão precisa ser
   exatamente essa; os comandos abaixo supõem o caminho padrão
   `/Applications/Unity/Hub/Editor/6000.6.4f1/`.
4. **Git** (vem com o Xcode) e acesso ao repositório privado `viniciusalegre20/OdontoAR` (peça para
   ser adicionado como colaborador).

## 2. Clonar o projeto (fora do iCloud)

```sh
mkdir -p ~/Developer && cd ~/Developer
git clone https://github.com/viniciusalegre20/OdontoAR.git
cd OdontoAR
```

> **Não** clone em `~/Documents` nem `~/Desktop` se o iCloud Drive sincroniza essas pastas: o iCloud
> cria cópias "arquivo 2.cpp" durante o build e o link falha.

Abra o projeto uma vez pelo Unity Hub ("Add project from disk" → pasta `OdontoAR`). A primeira
abertura reimporta tudo e demora alguns minutos. Depois rode o menu **OdontoAR → Configurar projeto
iOS** (troca a plataforma para iOS; pode rodar quantas vezes quiser) e feche o Unity.

Para conferir que está tudo certo, rode os testes (devem passar os 126):
```sh
UNITY="/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Builds/tests.xml
```

## 3. Assinatura com o seu Apple ID (só no caso B)

O projeto está configurado para assinar com o time pessoal do Vinicius (`9BVJCCNT6Z`) e com o bundle
id `com.auraworks.odontoar`, que já pertence a esse time. Em outro Mac, com outro Apple ID:

1. Xcode → Settings → **Accounts** → "+" → Apple ID. Uma conta gratuita basta (cria um *Personal Team*).
2. Descubra o seu **Team ID** (10 caracteres). Depois de assinar qualquer app uma vez, ele aparece com:
   ```sh
   security find-certificate -c "Apple Development" -p | openssl x509 -noout -subject
   ```
   O valor é o `OU=` da saída. Também aparece no Xcode em Signing & Capabilities → Team.
3. Em `Assets/_OdontoAR/Editor/ProjectSetup.cs`, troque:
   - `AppleTeamId` pelo seu Team ID;
   - `BundleId` por um id só seu, por exemplo `com.auraworks.odontoar.seunome` (dois times não
     podem usar o mesmo bundle id).
4. Rode de novo **OdontoAR → Configurar projeto iOS** (ou por terminal, sem `-quit`:
   `"$UNITY" -batchmode -projectPath . -executeMethod OdontoAR.Editor.ProjectSetup.ConfigureIOS`).

> Não faça commit dessas duas trocas: elas valem só para a sua máquina. Use `git stash` ou
> `git checkout -- Assets/_OdontoAR/Editor/ProjectSetup.cs ProjectSettings` antes de enviar outras
> alterações.

## 4. Preparar o iPhone

1. Conecte o iPhone ao Mac por cabo, desbloqueie e toque em **Confiar neste computador**.
2. Ligue o **Modo de Desenvolvedor**: Ajustes → Privacidade e Segurança → Modo de Desenvolvedor →
   ativar e reiniciar. (A opção só aparece depois de conectar o iPhone a um Mac com Xcode.)
3. Anote o identificador do aparelho (UDID):
   ```sh
   xcrun devicectl list devices
   ```

## 5. Compilar e instalar no iPhone

Na pasta do projeto, com o Unity **fechado**:

```sh
UNITY="/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity"
UDID=<identificador do passo 4>

# 1) Gerar o projeto Xcode em Builds/iOS (~5–10 min)
"$UNITY" -batchmode -quit -projectPath . -executeMethod OdontoAR.Editor.BuildScript.BuildIOS -logFile Builds/build.log

# 2) Compilar e assinar (registra o aparelho no time automaticamente)
cd Builds/iOS
xcodebuild -project Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -destination "id=$UDID" -allowProvisioningUpdates -derivedDataPath ../DerivedData build
cd ../..

# 3) Instalar e abrir
xcrun devicectl device install app --device "$UDID" Builds/DerivedData/Build/Products/Release-iphoneos/OdontoAR.app
xcrun devicectl device process launch --device "$UDID" com.auraworks.odontoar
```

No caso B, troque `com.auraworks.odontoar` no último comando pelo seu bundle id.

**Alternativa pelo Xcode:** depois do passo 1, abra `Builds/iOS/Unity-iPhone.xcodeproj`, em
*Signing & Capabilities* escolha o seu time, selecione o iPhone no topo e clique em ▶.

**Primeira abertura no iPhone:** se aparecer "Desenvolvedor não confiável", vá em Ajustes → Geral →
VPN e Gerenciamento de Dispositivo → o seu Apple ID → **Confiar**. Depois abra o OdontoAR, digite um
código de participante (ex.: `P-901`) e permita a câmera.

## 6. Conta gratuita: validade e limites

- O app instalado com conta gratuita **expira em 7 dias**: depois disso ele não abre. Basta repetir a
  seção 5 (o registro de uso no aparelho é mantido se o app não for apagado).
- Cada Apple ID gratuito instala no máximo **3 apps** de desenvolvimento por aparelho e registra poucos
  aparelhos por semana.
- Para distribuir sem cabo (TestFlight) é preciso a conta paga do Apple Developer Program, fora do
  escopo da POC.

## 7. Problemas comuns

| Sintoma | Solução |
| --- | --- |
| Unity sai com código **198** | Hub sem login ou sem licença ativa (seção 1) |
| Build falha em **actool** | `xcode-select` apontando para as Command Line Tools (seção 1) |
| Erros de link com arquivos "… 2.cpp" | Projeto dentro do iCloud: mova para `~/Developer` |
| "No profiles for …" / "bundle identifier is not available" | Time ou bundle id de outra pessoa (seção 3) |
| "Developer Mode disabled" | Seção 4, passo 2 |
| "Desenvolvedor não confiável" | Ajustes → Geral → VPN e Gerenciamento de Dispositivo → Confiar |
| "maximum number of apps for free development profiles" | Apague outro app de desenvolvimento do iPhone |
| App parou de abrir depois de uma semana | Perfil gratuito expirou: repita a seção 5 |
| Unity aberto durante o build | Feche o Unity: o modo batch não abre o projeto em uso |
| Arcada sai do lugar / "O rastreamento reiniciou" | Mais luz e uma superfície com textura; posicione de novo |

Os logs do build ficam em `Builds/build.log`. Os logs do app saem no Console do Mac (app *Console* →
o iPhone → filtrar por `[OdontoAR]`).
