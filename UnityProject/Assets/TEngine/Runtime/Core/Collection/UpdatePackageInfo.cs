using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TEngine
{
    public class UpdatePackageInfo
    {
        /// <summary>
        /// 包名。
        /// </summary>
        public string PackageName;

        /// <summary>
        /// 主程序集名称。
        /// </summary>
        public string MainDLLName;

        /// <summary>
        /// 热更程序集列表。
        /// </summary>
        public List<string> HotUpdateAssemblies;
    }
}