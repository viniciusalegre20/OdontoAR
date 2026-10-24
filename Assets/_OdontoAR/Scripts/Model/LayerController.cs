using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>
    /// RF08: liga e desliga as camadas coroa, raiz, osso e nervos a partir do <see cref="ModelState"/>.
    /// Coroa e raiz são submeshes da mesma malha de cada dente, então a camada é trocada pelo material
    /// de cada submesh. Com osso ou nervos visíveis, coroa e raiz ficam translúcidas (visualização integrada).
    /// </summary>
    public sealed class LayerController : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;

        [Header("Materiais (gerados pelo menu OdontoAR/Criar cena AR)")]
        [SerializeField] Material crown;
        [SerializeField] Material crownTranslucent;
        [SerializeField] Material root;
        [SerializeField] Material rootTranslucent;
        [SerializeField] Material hidden;

        [Header("Renderers de cada camada")]
        [SerializeField] Renderer[] teeth;
        [SerializeField] Renderer[] bone;
        [SerializeField] Renderer[] nerves;

        enum Slot { Crown, Root, Other }

        // Por dente: o que cada submesh é (lido dos materiais importados) e o array reaproveitado a cada troca.
        Slot[][] slots;
        Material[][] materials;
        Collider[] teethColliders;

        void Awake()
        {
            slots = new Slot[teeth.Length][];
            materials = new Material[teeth.Length][];
            teethColliders = new Collider[teeth.Length];

            for (var i = 0; i < teeth.Length; i++)
            {
                var imported = teeth[i].sharedMaterials;
                materials[i] = (Material[])imported.Clone();
                slots[i] = new Slot[imported.Length];
                for (var s = 0; s < imported.Length; s++)
                    slots[i][s] = imported[s] == crown ? Slot.Crown : imported[s] == root ? Slot.Root : Slot.Other;
                teethColliders[i] = teeth[i].GetComponent<Collider>();
            }
        }

        void OnEnable()
        {
            holder.State.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            holder.State.Changed -= Apply;
        }

        void Apply()
        {
            var state = holder.State;
            var crownVisible = state.IsLayerVisible(Layer.Crown);
            var rootVisible = state.IsLayerVisible(Layer.Root);
            var boneVisible = state.IsLayerVisible(Layer.Bone);
            var nervesVisible = state.IsLayerVisible(Layer.Nerves);
            var integrated = boneVisible || nervesVisible;

            var crownMaterial = !crownVisible ? hidden : integrated ? crownTranslucent : crown;
            var rootMaterial = !rootVisible ? hidden : integrated ? rootTranslucent : root;
            var toothVisible = crownVisible || rootVisible;

            for (var i = 0; i < teeth.Length; i++)
            {
                var array = materials[i];
                for (var s = 0; s < array.Length; s++)
                {
                    if (slots[i][s] == Slot.Crown)
                        array[s] = crownMaterial;
                    else if (slots[i][s] == Slot.Root)
                        array[s] = rootMaterial;
                }

                teeth[i].sharedMaterials = array;
                teeth[i].enabled = toothVisible;
                if (teethColliders[i] != null)
                    teethColliders[i].enabled = toothVisible; // dente oculto não pode ser tocado (T7)
            }

            SetVisible(bone, boneVisible);
            SetVisible(nerves, nervesVisible);
        }

        static void SetVisible(Renderer[] renderers, bool visible)
        {
            foreach (var r in renderers)
            {
                r.enabled = visible;
                var collider = r.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = visible;
            }
        }
    }
}
