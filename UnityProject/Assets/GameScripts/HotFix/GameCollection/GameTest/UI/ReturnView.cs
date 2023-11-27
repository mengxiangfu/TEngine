using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class ReturnView : UIWindow
    {
        #region 脚本工具生成的代码
        private Button m_btnJump;
        public override void ScriptGenerator()
        {
            m_btnJump = FindChildComponent<Button>("m_btnJump");
            m_btnJump.onClick.AddListener(OnClickJumpBtn);
        }
        #endregion

        #region 事件
        private void OnClickJumpBtn()
        {
            GameModule.Collection.InnerGameShutDownProcedure();
        }
        #endregion

    }
}
