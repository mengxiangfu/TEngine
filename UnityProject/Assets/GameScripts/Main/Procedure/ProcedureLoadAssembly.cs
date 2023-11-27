using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HybridCLR;
using UnityEngine;
using TEngine;
using System.Reflection;
using YooAsset;

namespace GameMain
{
    /// <summary>
    /// 流程加载器 - 代码初始化
    /// </summary>
    public class ProcedureLoadAssembly : ProcedureBase
    {
        /// <summary>
        /// 是否需要加载热更新DLL
        /// </summary>
        public bool NeedLoadDll => (int)GameModule.Resource.PlayMode > (int)EPlayMode.EditorSimulateMode;

        private bool m_enableAddressable = true;
        public override bool UseNativeDialog => true;
        private int m_LoadAssetCount;
        private int m_LoadMetadataAssetCount;
        private int m_FailureAssetCount;
        private int m_FailureMetadataAssetCount;
        private bool m_LoadAssemblyComplete;
        private bool m_LoadMetadataAssemblyComplete;
        private bool m_LoadAssemblyWait;
#pragma warning disable CS0414
        private bool m_LoadMetadataAssemblyWait;
#pragma warning restore CS0414
        private Assembly m_MainLogicAssembly;
        private List<Assembly> m_HotfixAssemblys;
        private IFsm<IProcedureManager> m_procedureOwner;
        //MODIFY TE
        private UpdatePackageInfo m_UpdatePackageInfo;

        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Debug("HyBridCLR ProcedureLoadAssembly OnEnter");
            m_procedureOwner = procedureOwner;
            m_LoadAssemblyComplete = false;
            m_HotfixAssemblys = new List<Assembly>();

            //MODIFY TE 添加小程序合集相对TE修改
            m_UpdatePackageInfo = procedureOwner.GetData<UpdatePackageInfo>("updatePackageInfo");

            //AOT Assembly加载原始metadata MODIFY TE
            if (SettingsUtils.HybridCLRCustomGlobalSettings.Enable && !GameModule.Base.LoadMetadataAssemblyComplete)
            {
#if !UNITY_EDITOR
                m_LoadMetadataAssemblyComplete = false;
                LoadMetadataForAOTAssembly();
#else
                m_LoadMetadataAssemblyComplete = true;
#endif
            }
            else
            {
                m_LoadMetadataAssemblyComplete = true;
            }
            
            if (!NeedLoadDll || GameModule.Resource.PlayMode == EPlayMode.EditorSimulateMode)
            {
                m_MainLogicAssembly = GetMainLogicAssembly();
            }
            else
            {
                if (SettingsUtils.HybridCLRCustomGlobalSettings.Enable)
                {
                    // MODIFY TE
                    foreach (string hotUpdateDllName in m_UpdatePackageInfo.HotUpdateAssemblies)
                    {
                        var assetLocation = hotUpdateDllName;
                        if (!m_enableAddressable)
                        {
                            assetLocation = Utility.Path.GetRegularPath(
                                Path.Combine(
                                    "Assets",
                                    SettingsUtils.HybridCLRCustomGlobalSettings.AssemblyTextAssetPath,
                                    $"{hotUpdateDllName}{SettingsUtils.HybridCLRCustomGlobalSettings.AssemblyTextAssetExtension}"));
                        }
                           
                        Log.Debug($"LoadAsset: [ {assetLocation} ]");
                        m_LoadAssetCount++;
                        GameModule.Resource.LoadAssetAsync<TextAsset>(assetLocation,LoadAssetSuccess, customPackageName:m_UpdatePackageInfo.PackageName);
                    }

                    m_LoadAssemblyWait = true;
                }
                else
                {
                    m_MainLogicAssembly = GetMainLogicAssembly();
                }
            }

            if (m_LoadAssetCount == 0)
            {
                m_LoadAssemblyComplete = true;
            }
        }
        
        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            if (!m_LoadAssemblyComplete)
            {
                return;
            }
            if (!m_LoadMetadataAssemblyComplete)
            {
                return;
            }
            AllAssemblyLoadComplete();
        }
        
        private void AllAssemblyLoadComplete()
        {
            ChangeState<ProcedureStartGame>(m_procedureOwner);
#if UNITY_EDITOR
            //MODIFY TE
            m_MainLogicAssembly = AppDomain.CurrentDomain.GetAssemblies().
                First(assembly => $"{assembly.GetName().Name}.dll" == m_UpdatePackageInfo.MainDLLName);
#endif
            if (m_MainLogicAssembly == null)
            {
                Log.Fatal($"Main logic assembly missing.");
                return;
            }
            //MODIFY TE
            var appType = m_MainLogicAssembly.GetType($"{m_MainLogicAssembly.GetName().Name}.GameApp");
            if (appType == null)
            {
                Log.Fatal($"{m_UpdatePackageInfo.MainDLLName} type 'GameApp' missing.");
                return;
            }
            var entryMethod = appType.GetMethod("Entrance");
            if (entryMethod == null)
            {
                Log.Fatal($"{m_UpdatePackageInfo.MainDLLName} entry method 'Entrance' missing.");
                return;
            }
            //object[] objects = new object[] { new object[] { m_HotfixAssemblys } };
            object[] objects = new object[] { new object[] {} };
            entryMethod.Invoke(appType, objects);
        }

        private Assembly GetMainLogicAssembly()
        {
            Assembly mainLogicAssembly = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                //MODIFY TE
                if (string.Compare(m_UpdatePackageInfo.MainDLLName, $"{assembly.GetName().Name}.dll",
                        StringComparison.Ordinal) == 0)
                {
                    mainLogicAssembly = assembly;
                }

                foreach (var hotUpdateDllName in m_UpdatePackageInfo.HotUpdateAssemblies)
                {
                    if (hotUpdateDllName == $"{assembly.GetName().Name}.dll")
                    {
                        m_HotfixAssemblys.Add(assembly);
                    }
                }

                if (mainLogicAssembly != null && m_HotfixAssemblys.Count == m_UpdatePackageInfo.HotUpdateAssemblies.Count)
                {
                    break;
                }
            }

            return mainLogicAssembly;
        }

        /// <summary>
        /// 加载代码资源成功回调。
        /// </summary>
        /// <param name="assetOperationHandle">资源操作句柄。</param>
        private void LoadAssetSuccess(AssetOperationHandle assetOperationHandle)
        {
            m_LoadAssetCount--;
            var assetName = assetOperationHandle.AssetObject.name;
            Log.Debug($"LoadAssetSuccess, assetName: [ {assetName} ]");
            
            var textAsset = assetOperationHandle.AssetObject as TextAsset;
            if (textAsset == null)
            {
                Log.Warning($"Load text asset [ {assetName} ] failed.");
                return;
            }

            try
            {
                //MODIFY TE
                if (!GameModule.Base.LoadCompleteAssemblys.TryGetValue(assetName, out var assembly))
                {
                    assembly = Assembly.Load(textAsset.bytes);
                    GameModule.Base.LoadCompleteAssemblys[assetName] = assembly;
                }
                if (string.Compare(m_UpdatePackageInfo.MainDLLName, $"{assembly.GetName().Name}.dll", StringComparison.Ordinal) == 0)
                {
                    m_MainLogicAssembly = assembly;
                }
                m_HotfixAssemblys.Add(assembly);
                Log.Debug($"Assembly [ {assembly.GetName().Name} ] loaded");
            }
            catch (Exception e)
            {
                m_FailureAssetCount++;
                Log.Fatal(e);
                throw;
            }
            finally
            {
                m_LoadAssemblyComplete = m_LoadAssemblyWait && 0 == m_LoadAssetCount;
            }
            assetOperationHandle.Dispose();
        }

        /// <summary>
        /// 为Aot Assembly加载原始metadata， 这个代码放Aot或者热更新都行。
        /// 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行。
        /// </summary>
        public void LoadMetadataForAOTAssembly()
        {
            // 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
            // 我们在BuildProcessor_xxx里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

            // 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
            // 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
            if (SettingsUtils.HybridCLRCustomGlobalSettings.AOTMetaAssemblies.Count == 0)
            {
                m_LoadMetadataAssemblyComplete = true;
                return;
            }
            //MODIFY TE
            int index = SettingsUtils.HybridCLRCustomGlobalSettings.AOTMetaAssemblies.Count;
            foreach (string aotDllName in SettingsUtils.HybridCLRCustomGlobalSettings.AOTMetaAssemblies)
            {
                var assetLocation = aotDllName;
                if (!m_enableAddressable)
                {
                    assetLocation = Utility.Path.GetRegularPath(
                        Path.Combine(
                            "Assets",
                            SettingsUtils.HybridCLRCustomGlobalSettings.AssemblyTextAssetPath,
                            $"{aotDllName}{SettingsUtils.HybridCLRCustomGlobalSettings.AssemblyTextAssetExtension}"));
                }
                
                
                Log.Debug($"LoadMetadataAsset: [ {assetLocation} ]");
                m_LoadMetadataAssetCount++;
                //MODIFY TE
                index--;
                if (index == 0)
                {
                    m_LoadMetadataAssemblyWait = true;
                }
                GameModule.Resource.LoadAssetAsync<TextAsset>(assetLocation,LoadMetadataAssetSuccess, customPackageName: GameModule.Resource.defaultPackageName);
            }
        }

        /// <summary>
        /// 加载元数据资源成功回调。
        /// </summary>
        /// <param name="assetOperationHandle">资源操作句柄。</param>
        private unsafe void LoadMetadataAssetSuccess(AssetOperationHandle assetOperationHandle)
        {
            m_LoadMetadataAssetCount--;
            string assetName = assetOperationHandle.AssetObject.name;
            Log.Debug($"LoadMetadataAssetSuccess, assetName: [ {assetName} ]");
            var textAsset = assetOperationHandle.AssetObject as TextAsset;
            if (null == textAsset)
            {
                Log.Debug($"LoadMetadataAssetSuccess:Load text asset [ {assetName} ] failed.");
                return;
            }
            try
            {
                byte[] dllBytes = textAsset.bytes;
                fixed (byte* ptr = dllBytes)
                {
                    // 加载assembly对应的dll，会自动为它hook。一旦Aot泛型函数的native函数不存在，用解释器版本代码
                    HomologousImageMode mode = HomologousImageMode.SuperSet;
                    LoadImageErrorCode err = (LoadImageErrorCode)HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(dllBytes,mode); 
                    Log.Warning($"LoadMetadataForAOTAssembly:{assetName}. mode:{mode} ret:{err}");
                }
            }
            catch (Exception e)
            {
                m_FailureMetadataAssetCount++;
                Log.Fatal(e.Message);
                throw;
            }
            finally
            {
                m_LoadMetadataAssemblyComplete = m_LoadMetadataAssemblyWait && 0 == m_LoadMetadataAssetCount;
            }
        }
    }
}