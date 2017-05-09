using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlToCSharpGenerator.Models
{
    /// <summary>
    /// 字段信息
    /// </summary>
    public class FieldInfo
    {
        public int ColOrder { get; set; }

        public string ColName { get; set; }

        public string Type { get; set; }

        public int Length { get; set; }

        public bool Identity { get; set; }

        public int Precision { get; set; }

        /// <summary>
        /// 字段是否允许为空
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 小数位
        /// </summary>
        public int? Scale { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// 字段描述
        /// </summary>
        public string Desc { get; set; }

    }
}
