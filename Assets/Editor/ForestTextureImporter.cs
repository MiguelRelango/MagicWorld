#if UNITY_EDITOR
using UnityEditor;
namespace Hearthbound.EditorTools
{
    public class ForestTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/ForestLook/")) return;
            var t = (TextureImporter)assetImporter;
            t.textureType = TextureImporterType.Default;
            t.sRGBTexture = true;
            t.alphaSource = TextureImporterAlphaSource.FromInput;
            t.alphaIsTransparency = assetPath.EndsWith("Leaves.png");
            t.wrapMode = t.alphaIsTransparency ? UnityEngine.TextureWrapMode.Clamp : UnityEngine.TextureWrapMode.Repeat;
            t.mipmapEnabled = true;
            t.mipMapsPreserveCoverage = t.alphaIsTransparency;
            t.alphaTestReferenceValue = 0.4f;
            t.maxTextureSize = 1024;
            t.anisoLevel = 4;
            t.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
#endif
