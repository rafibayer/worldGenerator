﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{


    public Mode mode = Mode.generate; //generate creates a new world using below settings, read reads from text file
    public bool normalize = true;//normalize all values between [0, max]
    public int octaves = 5;//more octaves = more complexity
    public float freq = 100.0f;//high frequency, more large masses, low frequency, more islands and lakes
    public float amp = 1;//amplitude of values

    public int width = 100;//width of generated map in tiles
    public int height = 100;//height of generated map in tiles
    public float scale = 1f;//how large each tile is

    private float[,] values;//values generated by the perlin noise

    public List<worldTile> worldTiles;//list of worldTiles to be used

    //filepath for read/write
    public string savedPath;

    //seed to ensure randomness
    private float seed;

    public enum Mode
    {
        write,//generate a new world and write it to the disk
        generate,//generate a new world but don't write it to the disk
        read//read a world from the disk
    };

    private void Start()
    {
        //generate new map
        if(mode == Mode.generate)
        {
            seed = Random.value;
            values = new float[width, height];
            generateNoiseMap();
            noiseToTile();
        }
        else if(mode == Mode.write)//generate world and write
        {
            seed = Random.value;
            values = new float[width, height];
            generateNoiseMap();
            noiseToTile();
            writeToFile();
        }
        //read map from savedPath
        else if(mode == Mode.read)
        {
            string[] lines = System.IO.File.ReadAllLines(savedPath);
            string[] dim = lines[lines.Length - 1].Split('x');//dimensions are stored on last line in format WxH
            values = new float[int.Parse(dim[0]), int.Parse(dim[1])];//set array size
            width = int.Parse(dim[0]); height = int.Parse(dim[1]);//set dimensions
            for(int row = 0; row < lines.Length - 1; row++)//go until second to last line (last line has dimensions not vals)
            {

                string[] split = lines[row].Split(' ');
                //-1 because i didn't account for fencepost when adding spaces
                for(int col = 0; col < split.Length - 1; col++)
                {
                    values[row, col] = float.Parse(split[col]);
                    
                }
                
            }

            noiseToTile();//convert the values into the tilemap
           

        }
        

    }

    public void generateNoiseMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float noise = 0.0f;
                float gain = 1.0f;

                for (int o = 0; o < octaves; o++)
                {
                    noise += Mathf.PerlinNoise(seed + x * gain / freq, seed + y * gain / freq) * amp / gain;
                    gain *= 2.0f;

                    values[x, y] = noise;

                }

            }
        }

        if (normalize)//if normalized, all values are guaranteed to be between 0 and 1
        {
            float max = 0f;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (values[x, y] > max)
                    {
                        max = values[x, y];
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    values[x, y] /= max;
                }
            }
        }
    }

    //writes the generated noisemap to a file, dimensions are stored on the last line
    //in the format widthxheight
    public void writeToFile()
    {
       string[] lines = new string[height + 1];
       for(int y = 0; y < height; y++)
       {
            string line = "";
            for(int x = 0; x < width; x++)
            {
                line += values[y, x] + " "; //honestly not sure why these are flipped but it works :/
            }
            lines[y] = line;
       }
       lines[lines.Length - 1] = width + "x" + height;

       System.IO.File.WriteAllLines(@"" + savedPath, lines);

    }

    //converts the generated noise into a world made of worldTile GameObjects
    public void noiseToTile()
    {
        worldTiles.Sort(new WorldTileComp());
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3(x * scale, 0, y * scale);
                float val = values[x, y];

                bool spawned = false;
                foreach(worldTile wt in worldTiles)
                {
                    if(val < wt.maxHeight && !spawned)
                    {
                        Instantiate(wt, pos, Quaternion.identity, transform);
                        spawned = true;
                    }
                }
                if(!spawned)
                {
                    Instantiate(worldTiles[worldTiles.Count - 1], pos, Quaternion.identity, transform);
                }

            }

        }
    }

    //get information about the tile at an x,y coordinate
    //returns null if out of bounds
    public worldTile getTileInfo(int x, int y)
    {
        if(x >= width || y >= height)//too high check
        {
            return null;
        }
        if(x < 0 || y < 0)//too low check
        {
            return null;
        }
        float val = values[x, y];
        worldTiles.Sort(new WorldTileComp());
        foreach(worldTile wt in worldTiles)//same method used to place appropriate tile
        {
            if(val < wt.maxHeight)
            {
                return wt;
            }
        }

        return worldTiles[worldTiles.Count - 1];//default to the highest tile

    }

    

    
}
