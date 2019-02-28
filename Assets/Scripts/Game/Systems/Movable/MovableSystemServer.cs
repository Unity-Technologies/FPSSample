using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovableSystemServer
{
    int spawnNum;

    public MovableSystemServer(GameWorld world, BundledResourceManager bundledResourceManager)
    {
        m_GameWorld = world;
        m_BundledResourceManager = bundledResourceManager;
        Console.AddCommand("spawnbox", CmdSpawnBox, "Spawn <n> boxes", GetHashCode());
        Console.AddCommand("despawnboxes", CmdDespawnBoxes, "Despawn all boxes", GetHashCode());
    }

    private void CmdDespawnBoxes(string[] args)
    {
        foreach(var box in m_Movables)
        {
            m_GameWorld.RequestDespawn(box.gameObject);
        }
        m_Movables.Clear();
    }

    private void CmdSpawnBox(string[] args)
    {
        if (args.Length > 0)
            int.TryParse(args[0], out spawnNum);
        else
            spawnNum = 1;
        spawnNum = Mathf.Clamp(spawnNum, 1, 100);
    }

    public void Shutdown()
    {
        Console.RemoveCommandsWithTag(GetHashCode());
    }

    public void Update()
    {
        if (spawnNum <= 0)
            return;
        spawnNum--;

        int x = spawnNum % 10 - 5;
        int z = spawnNum / 10 - 5;

        GameObject prefab = (GameObject)m_BundledResourceManager.GetSingleAssetResource(Game.game.movableBoxPrototype);

        var movable = m_GameWorld.Spawn<Movable>(prefab, new Vector3(40+x*3,30,30+z*3), UnityEngine.Random.rotation);  // level_00: new Vector3(-20+x*3,10,-10+z*3)
        m_Movables.Add(movable);
    }

    private List<Movable> m_Movables = new List<Movable>();

    private GameWorld m_GameWorld;
    private BundledResourceManager m_BundledResourceManager;
}
