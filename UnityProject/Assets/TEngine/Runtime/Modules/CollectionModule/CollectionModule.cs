using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using YooAsset;

namespace TEngine
{
    //MODIFY TE
    /// <summary>
    /// 合集游戏模块。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CollectionModule : Module
    {
        // 是否在小游戏中
        public bool InCollectionGame = false;
        // 内部游戏名
        public string InnerGameName { private set; get; }
        // 内部游戏PackageInfo
        public UpdatePackageInfo PackageInfo;

        private ICollectionManager _collectionManager = null;
        private ProcedureBase _entranceProcedure = null;

        // 进入小游戏的所有流程

        [SerializeField] private string[] availableProcedureTypeNames = null;

        // 进入小游戏的启动流程

        [SerializeField] private string entranceProcedureTypeName = null;

        // 退出小游戏的所有流程

        [SerializeField] private string[] availableShutDownProcedureTypeNames = null;

        // 退出小游戏的启动流程

        [SerializeField] private string shutDownProcedureTypeName = null;

        protected override void Awake()
        {
            base.Awake();

            _collectionManager = ModuleImpSystem.GetModule<ICollectionManager>();
            if (_collectionManager == null)
            {
                Log.Fatal("Collection manager is invalid.");
            }
        }

        public async UniTaskVoid InnerGameEntranceProcedure(string gameName, UpdatePackageInfo updatePackageInfo, bool needUnloadAsset = true)
        {
            //调用主游戏程序集GameApp ShutDown
            ShutDownByDllName(SettingsUtils.HybridCLRCustomGlobalSettings.LogicMainDllName);
            //释放主包资源
            if (needUnloadAsset)
            {
                GameModule.Resource.ForceUnloadUnusedAssets(true);
            }

            await UniTask.WaitUntil(() => !GameModule.Resource.inUnloadUnused);

            //开始小游戏启动流程，目前只有launch与主流程不同
            ProcedureBase[] procedures = new ProcedureBase[availableProcedureTypeNames.Length];
            for (int i = 0; i < availableProcedureTypeNames.Length; i++)
            {
                Type procedureType = Utility.Assembly.GetType(availableProcedureTypeNames[i]);
                if (procedureType == null)
                {
                    Log.Error("Can not find procedure type '{0}'.", availableProcedureTypeNames[i]);
                    return;
                }

                procedures[i] = (ProcedureBase)Activator.CreateInstance(procedureType);
                if (procedures[i] == null)
                {
                    Log.Error("Can not create procedure instance '{0}'.", availableProcedureTypeNames[i]);
                    return;
                }

                if (entranceProcedureTypeName == availableProcedureTypeNames[i])
                {
                    _entranceProcedure = procedures[i];
                }
            }

            if (_entranceProcedure == null)
            {
                Log.Error("Entrance procedure is invalid.");
                return;
            }

            InnerGameName = gameName;
            PackageInfo = updatePackageInfo;
            InCollectionGame = true;
            GameModule.Procedure.StartProcedure(_entranceProcedure, procedures);
        }

        public void InnerGameShutDownProcedure()
        {
            //调用小游戏程序集GameApp ShutDown
            ShutDownByDllName(PackageInfo.MainDLLName);

            //小游戏关闭流程，目前只有launch与主流程不同
            ProcedureBase[] procedures = new ProcedureBase[availableShutDownProcedureTypeNames.Length];
            ProcedureBase _shutDownProcedure = null;
            for (int i = 0; i < availableShutDownProcedureTypeNames.Length; i++)
            {
                Type procedureType = Utility.Assembly.GetType(availableShutDownProcedureTypeNames[i]);
                if (procedureType == null)
                {
                    Log.Error("Can not find procedure type '{0}'.", availableShutDownProcedureTypeNames[i]);
                    return;
                }

                procedures[i] = (ProcedureBase)Activator.CreateInstance(procedureType);
                if (procedures[i] == null)
                {
                    Log.Error("Can not create procedure instance '{0}'.", availableShutDownProcedureTypeNames[i]);
                    return;
                }

                if (shutDownProcedureTypeName == availableShutDownProcedureTypeNames[i])
                {
                    _shutDownProcedure = procedures[i];
                }
            }

            if (_shutDownProcedure == null)
            {
                Log.Error("ShutDown procedure is invalid.");
                return;
            }

            InnerGameName = null;
            PackageInfo = null;
            InCollectionGame = false;
            _entranceProcedure = null;
            GameModule.Procedure.StartProcedure(_shutDownProcedure, procedures);
        }

        private void ShutDownByDllName(string dllName)
        {
            string assemblyName = dllName.Split(".dll")[0];
            var appType = Utility.Assembly.GetTypeByAssemblyName(dllName, $"{assemblyName}.GameApp");
            if (appType == null)
            {
                return;
            }
            var entryMethod = appType.GetMethod("Shutdown");
            if (entryMethod == null)
            {
                Log.Fatal($"{PackageInfo.MainDLLName} entry method 'Shutdown' missing.");
                return;
            }
            object[] objects = new object[] { new object[] { ShutdownType.Quit } };
            entryMethod.Invoke(appType, objects);
        }
    }

}