using UnityEngine;

namespace rockScene.MaterialChanger
{
    public class MaterialChanger : MonoBehaviour
    {
        public Material newMaterial;
        public Material oldMaterial;

        private Renderer objectRenderer;

        void Start()
        {
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            foreach (Renderer rend in allRenderers)
            {
                Material[] materialsArray = rend.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materialsArray.Length; i++)
                {
                    if (materialsArray[i] == oldMaterial)
                    {
                        materialsArray[i] = newMaterial;
                        changed = true;
                    }
                }

                if (changed)
                {
                    rend.sharedMaterials = materialsArray;
                }
            }
        }
    }
}