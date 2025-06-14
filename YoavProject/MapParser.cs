using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoavProject.Properties;

namespace YoavProject
{

    public class MapCollection
    {
        [JsonProperty("map")]
        public MapData Map { get; set; }
    }

    public class MapData
    {
        [JsonProperty("objects")]
        public Dictionary<string, GameObjectData> Objects { get; set; } = new Dictionary<string, GameObjectData>();
    }

    public class GameObjectData
    {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }
    }
    internal class MapParser
    {
        public static void loadMapRandom(WorldState state)
        {
            var random = new Random();

            // Get random map
            var mapNumber = random.Next(1, 2); // If you have 3 maps

            string jsonString;
            switch (mapNumber)
            {
                case 1:
                    jsonString = System.Text.Encoding.UTF8.GetString(Properties.Resources.Map1);
                    break;
                default:
                    jsonString = System.Text.Encoding.UTF8.GetString(Properties.Resources.Map1); // Default fallback
                    break;
            }
            loadMap(jsonString, state);


        }
        public static void loadMap(string jsonString, WorldState state)
        {
            var mapCollection = JsonConvert.DeserializeObject<MapCollection>(jsonString);
            var map = mapCollection.Map;
            state.clearWorldMap();
            state.setUpForGameMap();
            foreach (var pair in map.Objects)
            {
                int objId = int.Parse(pair.Key);
                var objData = pair.Value;
                Table table = new Table(position: new PointF(objData.X, objData.Y), size: new SizeF(objData.Width, objData.Height));
                state.addWorldInteractable(objId, table);
            }
        }

    }
}
