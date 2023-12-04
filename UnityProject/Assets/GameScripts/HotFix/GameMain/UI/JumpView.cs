using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameMain
{
    [Window(UILayer.UI)]
    class JumpView : UIWindow
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
            //ModuleSystem.Shutdown(ShutdownType.Restart);
            UpdatePackageInfo packageInfo = new UpdatePackageInfo();
            packageInfo.PackageName = "GameTest";
            packageInfo.MainDLLName = "GameTest.dll";
            packageInfo.HotUpdateAssemblies = new System.Collections.Generic.List<string>() { "GameTest.dll" }; 
            GameModule.Collection.InnerGameEntranceProcedure("GameTest", packageInfo).Forget();;
        }
        #endregion

    }
}
