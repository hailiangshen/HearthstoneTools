using Microsoft.Win32;
using NetFwTypeLib;
using System;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace AutoNETBlock
{
    public class Helper
    {
        const string HearthstoneNETBlockRuleName = "HearthstoneNETBlock";

        protected static bool IsAdministrator()
        {
            WindowsIdentity current = WindowsIdentity.GetCurrent();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(current);
            return windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// 获取系统版本
        /// </summary>
        /// <returns></returns>
        protected string GetSystemVersion()
        {
            using(RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"Software\\Microsoft\\Windows NT\\CurrentVersion"))
            {
                return rk.GetValue("ProductName").ToString();
            }
        }

        public bool CreateHearthstoneNETBlockRule(string appPath)
        {
            return CreateOutRule(NET_FW_IP_PROTOCOL_.NET_FW_IP_PROTOCOL_ANY, HearthstoneNETBlockRuleName, appPath, "炉石自动断线整活规则");
        }

        public bool SetHearthstoneBlock(bool enabled)
        {
            return SetRuleBlock(HearthstoneNETBlockRuleName, enabled);
        }

        public bool hasSetRule()
        {
            return GetRule(HearthstoneNETBlockRuleName) != null;
        }

        /// <summary>
        /// 设置规则启用禁用
        /// </summary>
        /// <param name="ruleName"></param>
        /// <param name="enabled"></param>
        /// <returns></returns>
        private bool SetRuleBlock(string ruleName, bool enabled)
        {
            if (!Helper.IsAdministrator())
            {
                throw new Exception("请使用管理员身份运行");
            }

            var rule = GetRule(ruleName);
            if(rule != null)
            {
                if (rule.Enabled != enabled)
                {
                    rule.Enabled = enabled;
                }
                return true;
            }
            throw new Exception("规则未找到，请先添加规则");
        }

        private INetFwRule GetRule(string ruleName)
        {
            INetFwPolicy2 policy2 = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            foreach (INetFwRule item in policy2.Rules)
            {
                if (item.Name == ruleName)
                {
                    return item;
                }
            }
            return null;
        }

        private bool CreateOutRule(
            NET_FW_IP_PROTOCOL_ type, 
            string ruleName, 
            string appPath,
            string description = null,
            string localAddresses = null, 
            string localPorts = null, 
            string remoteAddresses = null, 
            string remotePorts = null
            )
        {
            if(!Helper.IsAdministrator())
            {
                throw new Exception("请使用管理员身份运行");
            }

            if(GetRule(ruleName) != null)
            {
                return true;
            }

            INetFwPolicy2 policy2 = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            //创建防火墙规则类的实例: 有关该接口的详细介绍：https://docs.microsoft.com/zh-cn/windows/win32/api/netfw/nn-netfw-inetfwrule
            INetFwRule rule = (INetFwRule)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwRule"));
            
            rule.Name = ruleName;
            
            rule.Description = description;
            
            rule.Direction = NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_OUT;
            
            rule.Protocol = (int)type;
            //为规则添加应用程序（注意这里是应用程序的绝对路径名）
            rule.ApplicationName = appPath;
              
            if (!string.IsNullOrEmpty(localAddresses))
            {
                rule.LocalAddresses = localAddresses;
            }

            if (!string.IsNullOrEmpty(localPorts))
            {
                //需要移除空白字符（不能包含空白字符，下同）
                rule.LocalPorts = localPorts.Replace(" ", "");// "1-29999, 30003-33332, 33334-55554, 55556-60004, 60008-65535";
            }
            
            if (!string.IsNullOrEmpty(remoteAddresses))
            {
                rule.RemoteAddresses = remoteAddresses;
            }
            
            if (!string.IsNullOrEmpty(remotePorts))
            {
                rule.RemotePorts = remotePorts.Replace(" ", "");
            }
            //设置规则是阻止还是允许（ALLOW=允许，BLOCK=阻止）
            rule.Action = NET_FW_ACTION_.NET_FW_ACTION_BLOCK;
            //分组 名
            rule.Grouping = "defaultGroupsName";

            rule.InterfaceTypes = "All";

            //是否启用规则,默认不启用
            rule.Enabled = false;
            try
            {
                policy2.Rules.Add(rule);
            }
            catch (Exception e)
            {
                string error = $"防火墙添加规则出错：{ruleName} {e.Message}";
                throw new Exception(error);
            }
            return true;
        }

        /// <summary>
        /// 删除WindowsDefender防火墙规则
        /// <summary>
        /// <param name="ruleName"></param>
        private bool DeleteRule(string ruleName)
        {
            INetFwPolicy2 policy2 = (INetFwPolicy2)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            try
            {
                //根据规则名称移除规则
                policy2.Rules.Remove(ruleName);
            }
            catch (Exception e)
            {
                string error = $"防火墙删除规则出错：{ruleName} {e.Message}";
                throw new Exception(error);
            }
            return true;
        }
    }
}
