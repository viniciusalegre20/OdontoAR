using System;
using System.IO;
using OdontoAR.App;
using OdontoAR.AR;
using OdontoAR.Model;
using OdontoAR.UI;
using UnityEngine;

namespace OdontoAR.Logging
{
    /// <summary>
    /// RF10: transforma o que acontece no app em eventos do <see cref="UsageLog"/> e grava o registro em
    /// Application.persistentDataPath (registro-P-014.json), só neste aparelho. Uma sessão vai de entrar na
    /// RA até sair dela ou o app ir para o fundo. Cada evento é salvo na hora: se o app for fechado, a sessão
    /// fica como interrompida, com a duração do último evento.
    /// </summary>
    public sealed class UsageRecorder : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;
        [SerializeField] ARTapToPlace tapToPlace;
        [SerializeField] ScreenRouter router;

        [Tooltip("A abertura é registrada quando o slider para de mexer por este tempo (s).")]
        [SerializeField] float openingSettleSeconds = 0.6f;

        [Tooltip("Salva a duração da sessão a cada este tempo, para sessões interrompidas (s).")]
        [SerializeField] float autosaveSeconds = 10f;

        static readonly Layer[] Layers = { Layer.Crown, Layer.Root, Layer.Bone, Layer.Nerves };

        UsageLog log;

        // Último estado visto, para registrar só o que mudou.
        bool wasPlaced;
        bool hadTarget;
        bool surfaceLogged;
        readonly bool[] layerVisible = new bool[4];
        ArchSelection arch;
        string selectedKey;
        Layer selectedLayer;
        int openingDegrees;
        int pendingOpening = -1;
        float openingChangedAt;
        float lastSave;

        static double Clock => Time.realtimeSinceStartupAsDouble;

        /// <summary>O registro do participante atual (carregado do aparelho se ainda não estiver).</summary>
        public UsageLog Log
        {
            get
            {
                var participant = AppPrefs.ParticipantCode;
                if (log == null || log.Participant != participant)
                    log = UsageLog.FromStorageJson(participant, ReadStorage(participant));
                return log;
            }
        }

        void OnEnable()
        {
            router.ScreenChanged += OnScreenChanged;
            holder.State.Changed += OnStateChanged;
        }

        void OnDisable()
        {
            router.ScreenChanged -= OnScreenChanged;
            holder.State.Changed -= OnStateChanged;
            EndSession();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                EndSession();
            else if (ScreenRouter.IsExperience(router.Current))
                StartSession();
        }

        void OnScreenChanged(AppScreen screen)
        {
            if (ScreenRouter.IsExperience(screen))
                StartSession();
            else
                EndSession();
        }

        void StartSession()
        {
            var current = Log;
            if (current.InSession)
                return;

            current.StartSession(DateTimeOffset.Now, Clock);
            SnapshotState();
            surfaceLogged = false;
            Save();
        }

        void EndSession()
        {
            if (log == null || !log.InSession)
                return;

            FlushOpening();
            log.EndSession(Clock);
            Save();
        }

        void Update()
        {
            if (log == null || !log.InSession)
                return;

            // Primeira superfície encontrada em cada posicionamento.
            var hasTarget = tapToPlace.HasTarget;
            if (hasTarget && !hadTarget && !surfaceLogged && !holder.State.IsPlaced)
            {
                surfaceLogged = true;
                Record(UsageEvent.Simple(UsageEvent.SurfaceDetected));
            }
            hadTarget = hasTarget;

            if (pendingOpening >= 0 && Time.unscaledTime - openingChangedAt >= openingSettleSeconds)
                FlushOpening();

            if (Time.unscaledTime - lastSave >= autosaveSeconds)
            {
                log.Touch(Clock);
                Save();
            }
        }

        void OnStateChanged()
        {
            if (log == null || !log.InSession)
                return;

            var state = holder.State;

            if (state.IsPlaced != wasPlaced)
            {
                if (state.IsPlaced)
                    Record(UsageEvent.Simple(UsageEvent.Placed));
                else if (router.Current == AppScreen.AR)
                {
                    Record(UsageEvent.Simple(UsageEvent.Repositioned));
                    surfaceLogged = false;
                }
                wasPlaced = state.IsPlaced;
            }

            for (var i = 0; i < Layers.Length; i++)
            {
                var visible = state.IsLayerVisible(Layers[i]);
                if (visible == layerVisible[i])
                    continue;
                layerVisible[i] = visible;
                Record(UsageEvent.LayerToggle(Layers[i], visible));
            }

            if (state.Arch != arch)
            {
                arch = state.Arch;
                Record(UsageEvent.Arch(arch));
            }

            if (state.HasSelection && (state.SelectedKey != selectedKey || state.SelectedLayer != selectedLayer))
                Record(UsageEvent.Selection(state.SelectedKey, state.SelectedLayer));
            selectedKey = state.SelectedKey;
            selectedLayer = state.SelectedLayer;

            // O slider muda a cada quadro: registra o valor em que o aluno parou.
            var degrees = Mathf.RoundToInt(state.Opening * ModelState.MaxOpeningDegrees);
            if (degrees != (pendingOpening >= 0 ? pendingOpening : openingDegrees))
            {
                pendingOpening = degrees;
                openingChangedAt = Time.unscaledTime;
            }
        }

        void FlushOpening()
        {
            if (pendingOpening < 0)
                return;

            if (pendingOpening != openingDegrees)
            {
                openingDegrees = pendingOpening;
                Record(UsageEvent.Opening(openingDegrees));
            }
            pendingOpening = -1;
        }

        void SnapshotState()
        {
            var state = holder.State;
            wasPlaced = state.IsPlaced;
            hadTarget = tapToPlace.HasTarget;
            for (var i = 0; i < Layers.Length; i++)
                layerVisible[i] = state.IsLayerVisible(Layers[i]);
            arch = state.Arch;
            selectedKey = state.SelectedKey;
            selectedLayer = state.SelectedLayer;
            openingDegrees = Mathf.RoundToInt(state.Opening * ModelState.MaxOpeningDegrees);
            pendingOpening = -1;
        }

        void Record(UsageEvent usageEvent)
        {
            log.Record(usageEvent, Clock);
            Save();
        }

        // ---------- Arquivos ----------

        static string StoragePath(string participant) =>
            Path.Combine(Application.persistentDataPath, $"registro-{participant}.json");

        static string ReadStorage(string participant)
        {
            try
            {
                var path = StoragePath(participant);
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[OdontoAR] Registro não lido: {e.Message}");
                return null;
            }
        }

        void Save()
        {
            lastSave = Time.unscaledTime;
            if (log == null || string.IsNullOrEmpty(log.Participant))
                return;

            try
            {
                File.WriteAllText(StoragePath(log.Participant), log.ToStorageJson());
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[OdontoAR] Registro não salvo: {e.Message}");
            }
        }

        /// <summary>Grava o JSON de exportação numa pasta temporária e devolve o caminho.</summary>
        public string WriteExport()
        {
            var current = Log;
            if (current.InSession)
            {
                FlushOpening();
                current.Touch(Clock);
                Save();
            }

            var path = Path.Combine(Application.temporaryCachePath,
                UsageLog.ExportFileName(current.Participant, DateTimeOffset.Now));
            File.WriteAllText(path, current.ToExportJson(Application.version));
            return path;
        }
    }
}
