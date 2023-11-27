using Cysharp.Threading.Tasks;
using TEngine;
using YooAsset;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureManager>;

namespace GameMain
{
    /// <summary>
    /// 流程 => 启动器。
    /// </summary>
    public class ProcedureReturnMain : ProcedureBase
    {
        public override bool UseNativeDialog => true;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            ReleaseAsset(procedureOwner).Forget();
        }

        private async UniTaskVoid ReleaseAsset(ProcedureOwner procedureOwner)
        {
            // 释放小游戏的package
            YooAssets.DestroyPackage(GameModule.Resource.packageName);

            // 将DefaultPackage改为默认并释放无用资源
            GameModule.Resource.SetDefaultPackageName(GameModule.Resource.defaultPackageName);
            var package = YooAssets.GetPackage(GameModule.Resource.defaultPackageName);
            GameModule.Resource.SetDefaultPackage(package);
            GameModule.Resource.ForceUnloadAllAssets(true);
            await UniTask.WaitUntil(() => !GameModule.Resource.inUnloadUnused);

            //MODIFY TE 添加小程序合集相对TE修改
            UpdatePackageInfo updatePackageInfo = new UpdatePackageInfo();
            updatePackageInfo.PackageName = GameModule.Resource.defaultPackageName;
            updatePackageInfo.MainDLLName = SettingsUtils.HybridCLRCustomGlobalSettings.LogicMainDllName;
            updatePackageInfo.HotUpdateAssemblies = SettingsUtils.HybridCLRCustomGlobalSettings.HotUpdateAssemblies;
            procedureOwner.SetData<UpdatePackageInfo>("updatePackageInfo", updatePackageInfo);

            ChangeState<ProcedureLoadAssembly>(procedureOwner);
        }
    }
}
