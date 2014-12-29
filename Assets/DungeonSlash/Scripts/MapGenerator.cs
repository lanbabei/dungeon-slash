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
        public XmlElement xmlElement;
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

    public Object tmxFile;
    public Texture2D texture2D;

	void Start ()
    {
        //string xmlText = System.IO.File.ReadAllText();
        Generate(AssetDatabase.GetAssetPath(tmxFile));
    }

	public void Generate (string tmxFilepath) {
        string xmlText = System.IO.File.ReadAllText(tmxFilepath);

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
            layerInfo.xmlElement = layer;

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
        float ppu = sprites[0].pixelsPerUnit;

        if (ppu != mapInfo.tileHeight) Debug.LogWarning("Please check the texture's pixel per unit property.");

        // Create a Map Game Object.
        GameObject map = new GameObject("Map");

        // Create the layers.
        foreach (LayerInfo layerInfo in layers)
        {
            // Create the parent layer.
            GameObject layer = new GameObject(layerInfo.name);
            layer.transform.parent = map.transform;

            int order = GetSortingOrder(layerInfo.xmlElement);
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
                     * It won't adjust to the sprites ppu anymore. */
                    Vector3 position = new Vector3(j * spriteRenderer.sprite.textureRect.width / spriteRenderer.sprite.pixelsPerUnit,
                                                   i * spriteRenderer.sprite.textureRect.height / spriteRenderer.sprite.pixelsPerUnit, 0);
                    
                    /*Vector3 position = new Vector3(j * layerInfo.width / mapInfo.tileWidth,
                                                   i * layerInfo.height / mapInfo.tileHeight); */
                    sprite.transform.position = position;
                }
            }
        }

        // We will now create colliders
        foreach (XmlElement collidableObjects in xml.GetElementsByTagName("objectgroup"))
        {
            GameObject collidables = new GameObject(collidableObjects.GetAttribute("name"));
            collidables.transform.parent = map.transform;
            int order = GetSortingOrder(collidableObjects);
            foreach (XmlElement collidableObject in collidableObjects.GetElementsByTagName("object"))
            {
                GameObject collidable = new GameObject(collidableObject.GetAttribute("name"));
                collidable.transform.parent = collidables.transform;

                if (collidableObject.HasAttribute("gid"))
                {
                    int gid = int.Parse(collidableObject.GetAttribute("gid")) - 1;
                    if (gid >= 0)
                    {
                        SpriteRenderer spriteRenderer = collidable.AddComponent<SpriteRenderer>();
                        spriteRenderer.sprite = sprites[gid];
                        spriteRenderer.sortingOrder = order;
                    }
                    collidable.AddComponent<BoxCollider2D>();

                    Vector3 position = new Vector3(float.Parse(collidableObject.GetAttribute("x")) / mapInfo.tileWidth,
                                                   (mapInfo.height + 1) - float.Parse(collidableObject.GetAttribute("y")) / mapInfo.tileHeight,
                                               0);
                    collidable.transform.position = position;
                }

                if (collidableObject.HasAttribute("width") && collidableObject.HasAttribute("height"))
                {
                    BoxCollider2D boxCollider = collidable.AddComponent<BoxCollider2D>();
                    boxCollider.size = new Vector2((float)float.Parse(collidableObject.GetAttribute("width")) / mapInfo.tileWidth,
                                                   (float)float.Parse(collidableObject.GetAttribute("height")) / mapInfo.tileHeight);
                    boxCollider.center = new Vector2(0.5f, 0.5f);

                    Vector3 position = new Vector3(float.Parse(collidableObject.GetAttribute("x")) / mapInfo.tileWidth,
                                                   (mapInfo.height - 1) - float.Parse(collidableObject.GetAttribute("y")) / mapInfo.tileHeight,
                                                   0);
                    collidable.transform.position = position;
                }

                XmlElement properties = collidableObject.GetElementsByTagName("properties").Item(0) as XmlElement;
                foreach (XmlElement property in properties)
                {
                    // Supported properties.
                    if (property.GetAttribute("name").Equals("isTriggered"))
                    {
                        collidable.collider2D.isTrigger = bool.Parse(property.GetAttribute("value"));
                    }
                }
            }
        }
	}

    private int GetSortingOrder(XmlElement element)
    {
        XmlElement properties = element.GetElementsByTagName("properties").Item(0) as XmlElement;
        foreach (XmlElement property in properties)
        {
            if (property.GetAttribute("name").Equals("sortingOrder"))
            {
                return int.Parse(property.GetAttribute("value"));
            }
        }
        return 0;
    }
}
