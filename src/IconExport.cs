using System.IO;
using UnityEngine;

namespace Levers;

public static class IconExport
{
    public static void ExportSprite(Sprite sprite, string path, string filename)
    {
        if (sprite == null || sprite.texture == null) return;
    
        string filePath = Path.Combine(path, filename + ".png");
        if (File.Exists(filePath))
        {
            return;
        }
    
        try
        {
            Texture2D atlas = sprite.texture;
            Rect spriteRect = sprite.textureRect;
            
            bool coversFullTexture = 
                spriteRect.x == 0 && 
                spriteRect.y == 0 && 
                Mathf.Approximately(spriteRect.width, sprite.texture.width) && 
                Mathf.Approximately(spriteRect.height, sprite.texture.height);

            if (coversFullTexture)
            {
                Export(atlas, path, filename);
                return;
            }
        
            RenderTexture tmp = RenderTexture.GetTemporary(
                atlas.width, 
                atlas.height, 
                0,
                RenderTextureFormat.Default, 
                RenderTextureReadWrite.sRGB);
        
            Graphics.Blit(atlas, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
        
            Texture2D newTex = new Texture2D(
                (int)spriteRect.width, 
                (int)spriteRect.height);
        
            newTex.ReadPixels(spriteRect, 0, 0);
            newTex.Apply();
        
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);
        
            byte[] encoded = newTex.EncodeToPNG();
            File.WriteAllBytes(filePath, encoded);
        }
        catch
        {
            Debug.LogWarning("Faild to export texture: " + filename);
        }
    }
    
    private static void Export(Texture texture, string path, string filename)
    {
        string filePath = Path.Combine(path, filename + ".png");
        if (File.Exists(filePath)) return;
        
        try
        {
            
            RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.sRGB);
            Graphics.Blit(texture, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            Texture2D newTex = new Texture2D(texture.width, texture.height);
            
            newTex.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            newTex.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);
            
            byte[] encoded = newTex.EncodeToPNG();
            File.WriteAllBytes(filePath, encoded);
        }
        catch
        {
            Debug.LogWarning("Failed to export atlas texture: " + filename);
        }
    }
}