using GameBase;
using System.Collections;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameTest {
    public class GameTestEnter : Singleton<GameTestEnter>
    {
        public static void Entrance(object[] objects)
        {
            Log.Warning("======= 看到此条日志代表你成功运行了Inner热更新代码 =======");
        }
    }
}

