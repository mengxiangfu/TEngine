using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using TEngine;
using YooAsset;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureManager>;

namespace GameMain
{
    /// <summary>
    /// 流程 => 用户尝试更新静态版本
    /// </summary>
    public class ProcedureUpdateVersion : ProcedureBase
    {
        public override bool UseNativeDialog => true;

        private ProcedureOwner _procedureOwner;

        //MODIFY TE
        private UpdatePackageInfo m_UpdatePackageInfo;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            _procedureOwner = procedureOwner;

            //MODIFY TE 添加小程序合集相对TE修改
            m_UpdatePackageInfo = procedureOwner.GetData<UpdatePackageInfo>("updatePackageInfo");

            base.OnEnter(procedureOwner);

            UILoadMgr.Show(UIDefine.UILoadUpdate, $"更新静态版本文件...");

            //检查设备是否能够访问互联网
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                Log.Warning("The device is not connected to the network");
                UILoadMgr.Show(UIDefine.UILoadUpdate, LoadText.Instance.Label_Net_UnReachable);
                UILoadTip.ShowMessageBox(LoadText.Instance.Label_Net_UnReachable, MessageShowType.TwoButton,
                    LoadStyle.StyleEnum.Style_Retry,
                    GetStaticVersion().Forget,
                    () => { ChangeState<ProcedureInitResources>(procedureOwner); });
            }

            UILoadMgr.Show(UIDefine.UILoadUpdate, LoadText.Instance.Label_RequestVersionIng);

            // 用户尝试更新静态版本。
            GetStaticVersion().Forget();
        }

        /// <summary>
        /// 向用户尝试更新静态版本。
        /// </summary>
        private async UniTaskVoid GetStaticVersion()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            //MODIFY TE
            var operation = GameModule.Resource.UpdatePackageVersionAsync(customPackageName: m_UpdatePackageInfo.PackageName);

            try
            {
                await operation.ToUniTask();

                if (operation.Status == EOperationStatus.Succeed)
                {
                    //线上最新版本operation.PackageVersion
                    GameModule.Resource.PackageVersion = operation.PackageVersion;
                    //MODIFY TE
                    Log.Debug($"Updated package Version : from {GameModule.Resource.GetPackageVersion(customPackageName: m_UpdatePackageInfo.PackageName)} to {operation.PackageVersion}");
                    ChangeState<ProcedureUpdateManifest>(_procedureOwner);
                }
                else
                {
                    OnGetStaticVersionError(operation.Error);
                }
            }
            catch (Exception e)
            {
                OnGetStaticVersionError(e.Message);
            }
        }

        private void OnGetStaticVersionError(string error)
        {
            Log.Error(error);

            UILoadTip.ShowMessageBox($"用户尝试更新静态版本失败！点击确认重试 \n \n <color=#FF0000>原因{error}</color>", MessageShowType.TwoButton,
                LoadStyle.StyleEnum.Style_Retry
                , () => { ChangeState<ProcedureUpdateVersion>(_procedureOwner); }, UnityEngine.Application.Quit);
        }
    }
}