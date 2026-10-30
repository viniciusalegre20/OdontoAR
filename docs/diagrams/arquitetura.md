# Arquitetura do OdontoAR

Diagrama em Mermaid (o GitHub desenha automaticamente). Mesmo diagrama da seção 10 do `README.md`.

```mermaid
flowchart TD
    U([Estudante]) --> UI

    subgraph App["App OdontoAR (Unity 6 · C#)"]
        UI["Interface — Scripts/UI<br/>ScreenRouter, LayerPanel, ArchSegment,<br/>StructureCard, HelpSheet, UsageLogScreen"]
        AR["Módulo de RA — Scripts/AR<br/>ARTapToPlace, ModelGestures,<br/>SelectionController"]
        ST["Estado do modelo — Scripts/Model<br/>ModelState (classe pura)"]
        MV["Visualização — Scripts/Model<br/>ModelView, ArchController,<br/>LayerController, StructureCatalog / FdiCode"]
        LG["Registro de uso — Scripts/Logging<br/>UsageRecorder, UsageLog"]
        TH["Tema — OdontoARTheme"]
        NAT["Plugins nativos iOS<br/>OdontoARCamera.mm, OdontoARShare.mm"]
    end

    UI --> ST
    AR --> ST
    ST --> MV
    ST --> LG
    UI -.cores.-> TH
    UI --> NAT

    AR --> ARF["AR Foundation 6.6.2<br/>ARSession, ARPlaneManager, ARRaycastManager,<br/>ARAnchorManager, AROcclusionManager"]
    ARF --> ARK["Apple ARKit XR Plugin 6.6.2"]
    ARK --> HW["iPhone: câmera, IMU, LiDAR"]
    LG --> FS[("JSON local<br/>persistentDataPath")]
    NAT --> IOS["iOS: AVFoundation (permissão),<br/>folha de compartilhamento"]
    MV --> Z["Modelo 3D Z-Anatomy<br/>5 FBX: 28 dentes, maxila, mandíbula, nervos"]
```

Não há servidor, API nem banco de dados: o app funciona offline e grava o registro de uso só no aparelho.
