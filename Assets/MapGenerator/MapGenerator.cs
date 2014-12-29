using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using System.Linq;

public class MapGenerator : MonoBehaviour {

    public class MapInfo
    {
        public int width;
        public int height;
        public int tileWidth;
        public int tileHeight;
    }

    public class LayerInfo
    {
        public string name;
        public int width;
        public int height;
        public List<GridInfo> grids = new List<GridInfo>();
    }

    public class GridInfo
    {
        public int gid;
    }

    public class ObjectInfo
    {
        public string name;
        public string type;
    }

    public Object xmlFile;
    public Texture2D texture2D;

	// Use this for initialization
	void Start () {
        string xmlText = System.IO.File.ReadAllText(AssetDatabase.GetAssetPath(xmlFile));

        XmlDocument xml = new XmlDocument();
        xml.LoadXml(xmlText);

        // Map Info
        XmlElement mapXml = xml.GetElementsByTagName("map").Item(0) as XmlElement;
        MapInfo mapInfo = new MapInfo();
        mapInfo.width = int.Parse(mapXml.GetAttribute("width"));
        mapInfo.height = int.Parse(mapXml.GetAttribute("height"));
        mapInfo.tileWidth = int.Parse(mapXml.GetAttribute("tilewidth"));
        mapInfo.tileHeight = int.Parse(mapXml.GetAttribute("tileheight"));

        // Create LayerInfo from XML.
        List<LayerInfo> layers = new List<LayerInfo>();
        foreach (XmlElement layer in xml.GetElementsByTagName("layer"))
        {
            LayerInfo layerInfo = new LayerInfo();
            layerInfo.name = layer.GetAttribute("name");
            layerInfo.width = int.Parse(layer.GetAttribute("width"));
            layerInfo.height = int.Parse(layer.GetAttribute("height"));

            XmlElement data = layer.GetElementsByTagName("data").Item(0) as XmlElement;
            
            // Get the grid.
            foreach (XmlElement grid in data)
            {
                GridInfo gridInfo = new GridInfo();
                gridInfo.gid = int.Parse(grid.GetAttribute("gid"))-1;
                layerInfo.grids.Add(gridInfo);
            }

            layers.Add(layerInfo);
        }

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(texture2D)).OfType<Sprite>().ToArray();

        // Create a Map Game Object.
        GameObject map = new GameObject("Map");

        int order = 0;
        // Create the layers.
        foreach (LayerInfo layerInfo in layers)
        {
            // Create the parent layer.
            GameObject layer = new GameObject(layerInfo.name);
            layer.transform.parent = map.transform;

            // Now, create the grids. Ignore -1.
            int gridIndex = 0;
            for (int i = layerInfo.height; i > 0; i--)
            {
                for (int j = 0; j < layerInfo.width; j++)
                {
                    GridInfo gridInfo = layerInfo.grids[gridIndex];

                    gridIndex++;
                    if (gridInfo.gid == -1) continue;

                    GameObject sprite = new GameObject(string.Format("Tile_{0}_{1}", j, i));
                    sprite.transform.parent = layer.transform;
                    SpriteRenderer spriteRenderer = sprite.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = sprites[gridInfo.gid];
                    spriteRenderer.sortingOrder = order;

                    /* Changed because we have to make sure that the settings are correct.
                     * It won't adjust to the sprites ppu anymore.
                    Vector3 position = new Vector3(j * spriteRenderer.sprite.textureRect.width / spriteRenderer.sprite.pixelsPerUnit,
                                                   i * spriteRenderer.sprite.textureRect.height / spriteRenderer.sprite.pixelsPerUnit, 0);
                    */
                    Vector3 position = new Vector3(j * layerInfo.width / mapInfo.tileWidth,
                                                   i * layerInfo.height / mapInfo.tileHeight);
                    sprite.transform.position = position;
                }
            }

            order++;
        }

        // We will now create colliders
        GameObject collidables = new GameObject("Collidables");
        collidables.transform.parent = map.transform;
        XmlElement collidableObjects = xml.GetElementsByTagName("objectgroup").Item(0) as XmlElement;
        foreach (XmlElement collidableObject in collidableObjects.GetElementsByTagName("object"))
        {
            GameObject collidable = new GameObject(collidableObject.GetAttribute("name"));
            collidable.transform.parent = collidables.transform;
            
            BoxCollider2D boxCollider = collidable.AddComponent<BoxCollider2D>();
            boxCollider.size = new Vector2(int.Parse(collidableObject.GetAttribute("width"))/mapInfo.tileWidth, 
                                           int.Parse(collidableObject.GetAttribute("height"))/mapInfo.tileHeight);
            boxCollider.center = new Vector2(0.5f, 0.5f);

            Vector3 position = new Vector3(int.Parse(collidableObject.GetAttribute("x")) / mapInfo.tileWidth,
                                           int.Parse(collidableObject.GetAttribute("y")) / mapInfo.tileHeight,
                                           0);
            collidable.transform.position = position;

            XmlElement properties = collidableObject.GetElementsByTagName("properties").Item(0) as XmlElement;
            foreach (XmlElement property in properties)
            {
                Debug.Log("p " + property);
                // Supported properties.
                if (property.GetAttribute("name").Equals("isTriggered"))
                {
                    Debug.Log("E");
                    boxCollider.isTrigger = bool.Parse(property.GetAttribute("value"));
                }
            }
        }
	}
}
