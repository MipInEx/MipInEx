using System;
using UnityEngine;

namespace TestGame
{
    public class GameInstanceManager : MonoBehaviour
    {
        private static GameInstanceManager? instance;

        private void InitializeSelf()
        {

        }

        internal static void Initialize()
        {
            if (GameInstanceManager.instance == null)
            {
                GameObject obj = new GameObject("Game Instance Manager");
                GameInstanceManager.instance = obj.AddComponent<GameInstanceManager>();
                GameInstanceManager.instance.InitializeSelf();
                DontDestroyOnLoad(obj);
            }
        }
    }
}
