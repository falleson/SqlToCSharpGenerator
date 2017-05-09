using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToCSharpGenerator.Models
{
    public class ObjectInfo
    {
        /// <summary>
        /// 对象名称 表/视图名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 对象类别 表/视图名
        /// </summary>
        public string NodeType { get; set; }
    }
}
