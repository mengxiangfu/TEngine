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
            Log.Warning("======= ����������־������ɹ�������Inner�ȸ��´��� =======");
        }
    }
}

