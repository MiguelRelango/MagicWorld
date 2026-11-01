using UnityEngine;
namespace Hearthbound
{
    public static class IslandSurfaces
    {
        static Material terrain, water;
        public static Material Terrain()
        {
            if(terrain != null) return terrain;
            Shader shader=Resources.Load<Shader>("ForestLook/IslandBlend");
            if(shader == null || !shader.isSupported) { Debug.LogError("IslandBlend shader unavailable"); return ForestLook.Surface("Grass"); }
            terrain=new Material(shader) {name="Blended island terrain"};
            foreach(string name in new[]{"Sand","Grass","Rock","Snow"})
                terrain.SetTexture("_"+name,Resources.Load<Texture2D>("ForestLook/"+name));
            return terrain;
        }
        public static Material Water()
        {
            if(water != null) return water;
            Shader shader=Resources.Load<Shader>("ForestLook/IslandWater");
            if(shader == null || !shader.isSupported) { Debug.LogError("IslandWater shader unavailable"); return ForestLook.Surface("Water"); }
            water=new Material(shader) {name="Animated sea"};
            water.SetTexture("_Water",Resources.Load<Texture2D>("ForestLook/Water"));
            return water;
        }
    }
}
