using UnityEngine;

namespace OdontoAR.Model
{
    /// <summary>
    /// RF08: liga e desliga as camadas coroa, raiz, osso e nervos a partir do <see cref="ModelState"/>.
    /// Coroa e raiz são submeshes da mesma malha de cada dente, então a camada é trocada pelo material
    /// de cada submesh. Com osso ou nervos visíveis, coroa e raiz ficam translúcidas (visualização integrada).
    /// RF09: com uma estrutura selecionada, todas as outras ficam esmaecidas.
    /// </summary>
    public sealed class LayerController : MonoBehaviour
    {
        [SerializeField] ModelStateHolder holder;

        [Header("Materiais (gerados pelo menu OdontoAR/Criar cena AR)")]
        [SerializeField] Material crown;
        [SerializeField] Material crownTranslucent;
        [SerializeField] Material crownDimmed;
        [SerializeField] Material root;
        [SerializeField] Material rootTranslucent;
        [SerializeField] Material rootDimmed;
        [SerializeField] Material bone;
        [SerializeField] Material boneDimmed;
        [SerializeField] Material nerve;
        [SerializeField] Material nerveDimmed;
        [SerializeField] Material hidden;

        [Header("Renderers de cada camada")]
        [SerializeField] Renderer[] teeth;
        [SerializeField] Renderer[] boneRenderers;
        [SerializeField] Renderer[] nerveRenderers;

        enum Slot { Crown, Root, Bone, Nerve, Other }

        // Por renderer: o que cada submesh é (lido dos materiais importados), o array reaproveitado
        // a cada troca e a chave da estrutura.
        Renderer[] all;
        Slot[][] slots;
        Material[][] materials;
        string[] keys;

        void Awake()
        {
            all = new Renderer[teeth.Length + boneRenderers.Length + nerveRenderers.Length];
            teeth.CopyTo(all, 0);
            boneRenderers.CopyTo(all, teeth.Length);
            nerveRenderers.CopyTo(all, teeth.Length + boneRenderers.Length);

            slots = new Slot[all.Length][];
            materials = new Material[all.Length][];
            keys = new string[all.Length];

            for (var i = 0; i < all.Length; i++)
            {
                var imported = all[i].sharedMaterials;
                materials[i] = (Material[])imported.Clone();
                slots[i] = new Slot[imported.Length];
                for (var s = 0; s < imported.Length; s++)
                    slots[i][s] = Classify(imported[s]);

                var structure = all[i].GetComponent<SelectableStructure>();
                keys[i] = structure != null ? structure.Key : null;
            }
        }

        Slot Classify(Material material)
        {
            if (material == crown) return Slot.Crown;
            if (material == root) return Slot.Root;
            if (material == bone) return Slot.Bone;
            if (material == nerve) return Slot.Nerve;
            return Slot.Other;
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
            var crownDimmedMaterial = crownVisible ? crownDimmed : hidden;
            var rootDimmedMaterial = rootVisible ? rootDimmed : hidden;

            for (var i = 0; i < all.Length; i++)
            {
                var dimmed = state.HasSelection && keys[i] != state.SelectedKey;
                var array = materials[i];
                for (var s = 0; s < array.Length; s++)
                {
                    array[s] = slots[i][s] switch
                    {
                        Slot.Crown => dimmed ? crownDimmedMaterial : crownMaterial,
                        Slot.Root => dimmed ? rootDimmedMaterial : rootMaterial,
                        Slot.Bone => dimmed ? boneDimmed : bone,
                        Slot.Nerve => dimmed ? nerveDimmed : nerve,
                        _ => array[s],
                    };
                }
                all[i].sharedMaterials = array;
            }

            SetVisible(teeth, crownVisible || rootVisible); // dente oculto não pode ser tocado
            SetVisible(boneRenderers, boneVisible);
            SetVisible(nerveRenderers, nervesVisible);
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
