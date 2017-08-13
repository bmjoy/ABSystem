﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ABSystem
{
    public class ABManager : MonoBehaviour
    {
        // 远程管理器和设置
        public ABRemoteSetting RemoteSetting;
        private ABRemoteManager remoteManager;
        // 本地管理器和设置
        public ABLocalSetting LocalSetting;
        private ABLocalManager localManager;
        // 是否已经进行过检查标记, 只有检查后, 各属性才有效, 才允许访问
        public bool IsCheck { get; private set; }

        public bool AutoUpdate = true;

        private string localVersion;   // 本地版本号
        private string remoteVersion;   // 远程版本号
        private List<ABInfo> localAssetBundleList; // 本地ab包列表
        private List<ABInfo> remoteAssetBundleList;    // 远程ab包列表

        public static ABManager Instance;

        private void Awake()
        {
            Instance = this;
            localManager = new ABLocalManager(LocalSetting);
            remoteManager = new ABRemoteManager(RemoteSetting, localManager);
        }

        /// <summary>
        /// 检查, 并进行属性数据更新
        /// </summary>
        public void Check()
        {
            IsCheck = false;
            localVersion = localManager.Version;
            remoteVersion = remoteManager.Version;
            if (!localVersion.Equals(remoteVersion))
            {
                // 读取本地ab包的清单
                localAssetBundleList = localManager.AseetBundleList;
                // 读取远程ab包的清单
                remoteAssetBundleList = remoteManager.GetAseetBundleList(remoteVersion);
                IEnumerable<ABInfo> updateList;
                if (localAssetBundleList.Count == 0)
                {
                    updateList = remoteAssetBundleList;
                }
                else
                {
                    // 获取更新列表
                    updateList = from remoteab in remoteAssetBundleList
                                 from localab in localAssetBundleList
                                 where localab.HasNewVersion(remoteab) || !localAssetBundleList.Contains(remoteab)
                                 select remoteab; 
                }
                remoteManager.SetDownloadQueue(updateList, remoteVersion);
                remoteManager.GetDownloadSize();
            }
            IsCheck = true;
        }

        /// <summary>
        /// 检查是否有新版本
        /// </summary>
        public bool HasNewVersion
        {
            get
            {
                if (!IsCheck) throw new ABUnCheckException("You should call the 'Check' before access any porperty");
                return !localVersion.Equals(remoteVersion);
            }
        }

        /// <summary>
        /// 获取当前的版本号
        /// </summary>
        public string CurrentVersion
        {
            get
            {
                if (!IsCheck) throw new ABUnCheckException("You should call the 'Check' before access any porperty");
                return HasNewVersion ? remoteVersion : localVersion;
            }
        }

        /// <summary>
        /// 返回需要下载的字节数
        /// </summary>
        public long DownloadSize
        {
            get
            {
                if (!IsCheck) throw new ABUnCheckException("You should call the 'Check' before access any porperty");
                return remoteManager.TotalBytes;
            }
        }

        /// <summary>
        /// 获取下载进度, 范围是: 0-100
        /// </summary>
        public int Progress
        {
            get
            {
                if (!IsCheck) throw new ABUnCheckException("You should call the 'Check' before access any porperty");
                return (int)(remoteManager.BytesReceive / remoteManager.TotalBytes * 100);
            }
        }

        /// <summary>
        ///  当前正在下载的对象, 都下载完后为null
        /// </summary>
        public ABDownloadItem CurrentDownloadItem
        {
            get
            {
                if (!IsCheck) throw new ABUnCheckException("You should call the 'Check' before access any porperty");
                return remoteManager.CurrentDownloadItem;
            }
        }

        public void UpdateToNew()
        {
            if (HasNewVersion)
            {
                // 开始下载
                remoteManager.StartDownload();
                // 清空本地不用的ab包
                localManager.Clear(ABUtility.GetDeleteABList(localAssetBundleList, remoteAssetBundleList));
                // 写入新的信息文件
                localManager.Version = remoteVersion;
                localManager.AseetBundleList = remoteAssetBundleList;
            }
        }

        private void Start()
        {
            if(AutoUpdate)
            {
                Check();
                UpdateToNew();
            }
        }

    }
}

