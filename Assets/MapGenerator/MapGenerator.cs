using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using System.Linq;

public class MapGenerator : MonoBehaviour {

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

    public TextAsset xmlFile;
    public Texture2D texture2D;

	// Use this for initialization
	void Start () {
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(xmlFile.text);

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

                    Vector3 position = new Vector3(j * spriteRenderer.sprite.textureRect.width / spriteRenderer.sprite.pixelsPerUnit,
                                                   i * spriteRenderer.sprite.textureRect.height / spriteRenderer.sprite.pixelsPerUnit, 0);
                    sprite.transform.position = position;
                }
            }

            order++;
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
