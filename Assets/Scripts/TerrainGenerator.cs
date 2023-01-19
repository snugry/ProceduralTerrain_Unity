using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TerrainType
{
    grass,
    sand,
    rocks,
    dirt
}

[System.Serializable]
public enum MapType
{
    hills,
    island,
    island2,
    river,
    river2,
    lake
}

public class TerrainGenerator : MonoBehaviour
{
    public int widthX;
    public int widthZ;
    public int height;

    public MapType mapType;

    public int parts;

    public float scale = 0.01f;

    public float textureBorder = 60f;

    public float textureOffset = 5f;

    [SerializeField]
    private Wave[] waves;

    public Texture2D terrainTexGrass;
    public Texture2D terrainTexSand;
    public Texture2D terrainTexRocks;
    public Texture2D terrainTexDirt;

    public GameObject terrain;

    private TerrainData _terrainData;

    private float stepX;
    private float stepZ;

    private int[,] _terrainType;


    // Start is called before the first frame update
    void Start()
    {
        stepX = widthX / parts;
        stepZ = widthZ / parts;

        _terrainData = terrain.GetComponent<Terrain>().terrainData;

        _terrainData.size = new Vector3(widthX, height, widthZ);

        _terrainType = new int[parts+1, parts+1];

       /* TerrainLayer terrainLayerGrass = new TerrainLayer();
        terrainLayerGrass.diffuseTexture = terrainTexGrass;

        TerrainLayer terrainLayerSand = new TerrainLayer();
        terrainLayerSand.diffuseTexture = terrainTexSand;

        TerrainLayer terrainLayerRocks = new TerrainLayer();
        terrainLayerRocks.diffuseTexture = terrainTexRocks;

        TerrainLayer terrainLayerDirt = new TerrainLayer();
        terrainLayerDirt.diffuseTexture = terrainTexDirt;

        TerrainLayer[] layers = new TerrainLayer[4];
        layers[0] = terrainLayerGrass;
        layers[1] = terrainLayerSand;
        layers[2] = terrainLayerRocks;
        layers[3] = terrainLayerDirt;
        _terrainData.terrainLayers = layers;*/

        //_terrain = Terrain.CreateTerrainGameObject(_terrainData);

        _terrainData.heightmapResolution = parts;
        _terrainData.SetHeights(0, 0, GenerateNoiseMap(parts,parts, scale, 0 ,0, waves, mapType));

        
        _terrainData.SetAlphamaps(0, 0, CalculateSplatMap(textureBorder, textureOffset));
        SetDetailMap(textureBorder);
        AddTrees(textureBorder, textureOffset);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private float[,] GenerateNoiseMap(int mapDepth, int mapWidth, float scale, float offsetX, float offsetZ, Wave[] waves, MapType mapType)
    {
        // create an empty noise map with the mapDepth and mapWidth coordinates
        float[,] noiseMap = new float[mapDepth, mapWidth];
        float centerX = mapWidth / 2;
        float centerZ = mapDepth / 2;
        for (int zIndex = 0; zIndex < mapDepth; zIndex++)
        {
            for (int xIndex = 0; xIndex < mapWidth; xIndex++)
            {
                // calculate sample indices based on the coordinates, the scale and the offset
                float sampleX = (xIndex + offsetX) / scale;
                float sampleZ = (zIndex + offsetZ) / scale;
                float noise = 0f;
                float normalization = 0f;
                foreach (Wave wave in waves)
                {
                    if(wave.seed == -1)
                    {
                        wave.seed = Random.Range(0, 10000);
                    }
                    // generate noise value using PerlinNoise for a given Wave
                    noise += wave.amplitude * Mathf.PerlinNoise(sampleX * wave.frequency + wave.seed, sampleZ * wave.frequency + wave.seed);
                    normalization += wave.amplitude;
                }
                // normalize the noise value so that it is within 0 and 1
                noise /= normalization;

                float factor = 0f;
                float factorX = 0f;
                float factorZ = 0f;

                switch (mapType)
                {
                    case MapType.island:
                        factor = (centerX - Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(xIndex, zIndex))) / centerX;
                        noise = noise * factor;
                        break;
                    case MapType.island2:
                        factorX = (centerX - Mathf.Abs(centerX - xIndex)) / centerX;
                        factorZ = (centerZ - Mathf.Abs(centerZ - zIndex)) / centerZ;
                        noise = noise * factorX * factorZ;
                        break;
                    case MapType.lake:
                        factor =  Vector2.Distance(new Vector2(centerX, centerZ), new Vector2(xIndex, zIndex)) / centerX;
                        noise = noise * factor;
                        break;
                    case MapType.river:
                        factorX = Mathf.Abs(centerX - xIndex) / centerX;
                        noise = noise * factorX;
                        break;
                    case MapType.river2:
                        factorZ = Mathf.Abs(centerZ - zIndex) / centerZ;
                        noise = noise * factorZ;
                        break;
                }

                noiseMap[zIndex, xIndex] = noise;
            }
        }
        return noiseMap;
    }

    private float GetNoiseMap(float x, float y, float scale = 1)
    {
        x = x * scale;
        y = y * scale;
        return Mathf.PerlinNoise(x, y);
    }

    private float[,,] CalculateSplatMap(float heightBorder, float heightOffset)
    {
        float[,,] splatmapData = new float[_terrainData.alphamapWidth, _terrainData.alphamapHeight, _terrainData.alphamapLayers];
        Debug.Log(_terrainData.alphamapWidth + " - " + _terrainData.alphamapHeight);

        for (int y = 0; y < _terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < _terrainData.alphamapWidth; x++)
            {
                float height = _terrainData.GetHeight(y, x);
                float steepness = _terrainData.GetSteepness((float)y / _terrainData.alphamapHeight, (float)x / _terrainData.alphamapWidth);
                int x_terrainType = Mathf.RoundToInt(((float)x / _terrainData.alphamapWidth) * parts);
                int z_terrainType = Mathf.RoundToInt(((float)y / _terrainData.alphamapHeight) * parts);

                if (steepness > 45)
                {
                    splatmapData[x, y, 0] = 0;
                    splatmapData[x, y, 1] = 0;
                    splatmapData[x, y, 2] = 1;
                    splatmapData[x, y, 3] = 0;
                    _terrainType[x_terrainType, z_terrainType] = (int)TerrainType.rocks;
                }
                else if (height < heightBorder)
                {
                    splatmapData[x, y, 0] = 0;
                    splatmapData[x, y, 1] = 1;
                    splatmapData[x, y, 2] = 0;
                    splatmapData[x, y, 3] = 0;
                    _terrainType[x_terrainType, z_terrainType] = (int)TerrainType.sand;
                }
                else if (height < (heightBorder + heightOffset))
                {
                    float rand = GetNoiseMap(x + 1000, y + 1000, 0.8f);
                    splatmapData[x, y, 0] = rand;
                    splatmapData[x, y, 1] = 1 - rand;
                    splatmapData[x, y, 2] = 0;
                    splatmapData[x, y, 3] = 0;
                    _terrainType[x_terrainType, z_terrainType] = (int)TerrainType.sand;
                }
                else
                {
                    float rand = GetNoiseMap(x + 5427, y + 7528, 0.04f);
                    splatmapData[x, y, 0] = rand;
                    splatmapData[x, y, 1] = 0;
                    splatmapData[x, y, 2] = 0;
                    splatmapData[x, y, 3] = 1-rand;

                    if(rand > 0.5f)
                    {
                        _terrainType[x_terrainType, z_terrainType] = (int)TerrainType.grass;
                    }
                    else
                    {
                        _terrainType[x_terrainType, z_terrainType] = (int)TerrainType.dirt;
                    }
                }
            }
        }
        return splatmapData;
    }

    private void SetDetailMap(float heightBorder)
    {
        //var map = _terrainData.GetDetailLayer(0, 0, _terrainData.detailWidth, _terrainData.detailHeight, 0);
        int[,] map = new int[_terrainData.detailWidth, _terrainData.detailHeight];

        Debug.Log(_terrainData.detailHeight + " - " + _terrainData.detailWidth);
        // For each pixel in the detail map...
        for (int y = 0; y < _terrainData.detailHeight; y++)
        {
            for (int x = 0; x < _terrainData.detailWidth; x++)
            {
                int x_terrainType = Mathf.RoundToInt(((float)x / _terrainData.detailWidth) * parts);
                int z_terrainType = Mathf.RoundToInt(((float)y / _terrainData.detailHeight) * parts);

                switch (_terrainType[x_terrainType, z_terrainType])
                {
                    case (int)TerrainType.grass:
                        map[x, y] = 1;
                        break;
                    case (int)TerrainType.dirt:
                        map[x, y] = Mathf.RoundToInt(GetNoiseMap(x + 512, y + 512, 0.2f));
                        break;
                }

                /*float height = _terrainData.GetHeight(Mathf.RoundToInt(y / 4), Mathf.RoundToInt(x / 4));
                // If the pixel value is below the threshold then
                // set it to zero.
                if (height > heightBorder)
                {
                    map[x, y] = Mathf.RoundToInt(GetNoiseMap(x + 512, y + 512, 0.4f));
                }*/
            }
        }

        // Assign the modified map back.
        _terrainData.SetDetailLayer(0, 0, 0, map);
    }

    private void AddTrees(float heightBorder, float heightOffset)
    {
        Terrain _terrain = terrain.GetComponent<Terrain>();
        _terrainData.treeInstances = new TreeInstance[0];


        for (int x = 0; x < _terrainData.heightmapResolution; x++)
        {
            for (int z = 0; z < _terrainData.heightmapResolution; z++)
            {
                float height = _terrainData.GetHeight(x,z);

                int x_terrainType = Mathf.RoundToInt(((float)x / _terrainData.heightmapResolution) * parts);
                int z_terrainType = Mathf.RoundToInt(((float)z / _terrainData.heightmapResolution) * parts);

                bool addTree = false;
                switch (_terrainType[z_terrainType, x_terrainType])
                {
                    case (int)TerrainType.grass:
                        if(GetNoiseMap(x + 1700, z + 5178, 0.6f) > 0.86f)
                        {
                            addTree = true;
                        }
                        break;
                    case (int)TerrainType.dirt:
                        if (GetNoiseMap(x + 1700, z + 5178, 0.6f) > 0.70f)
                        {
                            addTree = true;
                        }
                        break;
                }
                if (addTree)
                {
                    TreeInstance treeTemp = new TreeInstance();
                    treeTemp.position = new Vector3((float)x / (float)_terrainData.heightmapResolution, 0, (float)z / (float)_terrainData.heightmapResolution);

                    treeTemp.prototypeIndex = Random.Range(0,_terrainData.treePrototypes.Length);
                    treeTemp.widthScale = 1f;
                    treeTemp.heightScale = 1f;
                    treeTemp.color = Color.white;
                    treeTemp.lightmapColor = Color.white;
                    _terrain.AddTreeInstance(treeTemp);
                }
            }
            _terrain.Flush();
        }
    }
}

[System.Serializable]
public class Wave
{
    public float seed;
    public float frequency;
    public float amplitude;
}
