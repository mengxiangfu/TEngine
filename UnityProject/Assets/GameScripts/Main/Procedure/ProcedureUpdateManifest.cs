﻿using System;
using Cysharp.Threading.Tasks;
using TEngine;
using YooAsset;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureManager>;

namespace GameMain
{
    /// <summary>
    /// 流程 => 用户尝试更新清单
    /// </summary>
    public class ProcedureUpdateManifest: ProcedureBase
    {
        public override bool UseNativeDialog { get; }

        //MODIFY TE
        private UpdatePackageInfo m_UpdatePackageInfo;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            Log.Info("更新资源清单！！！");

            //MODIFY TE 添加小程序合集相对TE修改
            m_UpdatePackageInfo = procedureOwner.GetData<UpdatePackageInfo>("updatePackageInfo");

            UILoadMgr.Show(UIDefine.UILoadUpdate,$"更新清单文件...");
            
            UpdateManifest(procedureOwner).Forget();
        }

        private async UniTaskVoid UpdateManifest(ProcedureOwner procedureOwner)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));

            //MODIFY TE
            var operation = GameModule.Resource.UpdatePackageManifestAsync(GameModule.Resource.PackageVersion, customPackageName: m_UpdatePackageInfo.PackageName);
            
            await operation.ToUniTask();
            
            if(operation.Status == EOperationStatus.Succeed)
            {
                //更新成功
                //注意：保存资源版本号作为下次默认启动的版本!
                operation.SavePackageVersion();
                ChangeState<ProcedureCreateDownloader>(procedureOwner);
            }
            else
            {
                Log.Error(operation.Error);
                
                UILoadTip.ShowMessageBox($"用户尝试更新清单失败！点击确认重试 \n \n <color=#FF0000>原因{operation.Error}</color>", MessageShowType.TwoButton,
                    LoadStyle.StyleEnum.Style_Retry
                    , () => { ChangeState<ProcedureUpdateManifest>(procedureOwner); }, UnityEngine.Application.Quit);
            }
        }
    }
}