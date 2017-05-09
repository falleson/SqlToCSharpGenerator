using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using Dapper;
using SqlToCSharpGenerator.Models;
using System.IO;

namespace SqlToCSharpGenerator
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();

            string strSql = "select name Name, [type] [NodeType] from sys.objects where type='u'";
            var ObjectInfos = SqlHelper.Query<ObjectInfo>(strSql);

            //var tableNodes = treeView1.Nodes.Add("表");
            //var viewNodes = treeView1.Nodes.Add("视图");

            foreach (var obj in ObjectInfos)
            {
                if (obj.NodeType[0] == 'U')
                {
                    treeView1.Nodes.Add(obj.Name);
                }
                //else
                //{
                //    viewNodes.Nodes.Add(obj.Name);
                //}
            }

        }
        private string Convert2CSharpType(FieldInfo fld)
        {
            switch (fld.Type)
            {
                case "int":
                case "tinyint":
                    if (fld.IsNullable)
                        return "int?";
                    else
                        return "int";
                case "nvarchar":
                    return "string";

                case "datetime":
                    if (fld.IsNullable)
                        return "DateTime?";
                    else
                        return "DateTime";
                case "bit":
                    return "bool";

                default:
                    return "string";
            }
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            var tableName = treeView1.SelectedNode.Text;
            txtEntityName.Text = tableName;

            GenerateEntity(tableName);
        }

        private void GenerateEntity(string entityName)
        {
            //MessageBox.Show(entityName);
            List<FieldInfo> fields = GetFieldList(entityName);

            StringBuilder sb = new StringBuilder();
            foreach (var fld in fields)
            {
                sb.AppendLine("\t/// <summary>");
                sb.AppendLine("\t///" + (string.IsNullOrEmpty(fld.Desc) ? "没有字段说明信息" : fld.Desc));
                sb.AppendLine("\t/// </summary>");
                sb.AppendLine(string.Format("\tpublic {0} {1}{{ get;set; }}", Convert2CSharpType(fld), fld.ColName));
                sb.AppendLine();
            }

            string templateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/Entity.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                templateContent = sr.ReadToEnd();
            }

            templateContent = templateContent
                .Replace("{EntityBody}", sb.ToString())
                .Replace("{EntityName}", entityName);

            richTextBox1.Text = templateContent;
        }

        private List<FieldInfo> GetFieldList(string entityName)
        {
            string strSql = @"SELECT 
                    a.colorder ColOrder,a.name ColName,b.name as [Type],a.length [Length], 
                    COLUMNPROPERTY( a.id,a.name,'IsIdentity') as [Identity], 
                    COLUMNPROPERTY(a.id,a.name,'PRECISION') as [Precision],  
                    COLUMNPROPERTY(a.id,a.name,'Scale') as Scale,
                    a.isnullable IsNullable,isnull(e.text,'') [DefaultValue],g.[value] as [Desc]
                    FROM  syscolumns a 
                    left join systypes b on a.xtype=b.xusertype  
                    inner join sysobjects d on a.id=d.id and d.xtype='U' and d.name<>'dtproperties' 
                    left join syscomments e on a.cdefault=e.id  
                    left join sys.extended_properties g on a.id=g.major_id AND a.colid=g.minor_id
                    WHERE d.name='" + entityName + "' order by a.colorder";

            List<FieldInfo> fields = SqlHelper.Query<FieldInfo>(strSql);

            return fields;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            string entityName = this.txtEntityName.Text;
            List<FieldInfo> fields = GetFieldList(entityName);

            //参数说明
            StringBuilder sb = new StringBuilder();
            foreach (var fld in fields)
            {
                if (fld.Identity == false)
                {
                    sb.Append(("--@" + fld.ColName).PadRight(20));
                    sb.Append(("[" + ParseSqlType(fld) + "]").PadRight(18));
                    sb.Append(fld.Desc);
                    sb.AppendLine();
                }
            }
            string parameterComments = sb.ToString();

            //参数
            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                if (fld.Identity == false)
                {
                    sb.AppendLine(("\t@" + fld.ColName).PadRight(20) + ParseSqlType(fld) + ",");
                }
            }
            string parameters = sb.ToString().Substring(0, sb.Length - 3);  //去掉最后一个及回车换行,

            //Internal parameter declaration
            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                if (fld.Identity == false)
                {
                    sb.AppendLine(("\t\tDECLARE @_" + fld.ColName).PadRight(30) + ParseSqlType(fld));
                }
            }
            string internalParametersDeclaration = sb.ToString();

            //Internal parameter declaration
            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                if (fld.Identity == false)
                {
                    sb.Append("\t\tSET @_" + fld.ColName);
                    sb.Append(" = ");
                    sb.Append("@" + fld.ColName);
                    sb.AppendLine();
                }
            }
            string internalParametersAssignment = sb.ToString();

            //columns
            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                if (fld.Identity == false)
                {
                    sb.AppendLine("\t\t\t" + fld.ColName + ",");
                }
            }
            string columns = sb.ToString(0, sb.Length - 3);

            //columnValues
            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                if (fld.Identity == false)
                {
                    sb.AppendLine("\t\t\t@_" + fld.ColName + ",");
                }
            }
            string columnValues = sb.ToString(0, sb.Length - 3);

            string templateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/Insert-sp.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                templateContent = sr.ReadToEnd();
            }

            templateContent = templateContent
                .Replace("{Author}", Environment.UserName)
                .Replace("{CreatedTime}", DateTime.Now.ToString("yyyy-MM-dd HH:ss"))
                .Replace("{TableName}", entityName)
                .Replace("{ParameterComments}", parameterComments)
                .Replace("{Parameters}", parameters)
                .Replace("{InternalParametersDeclaration}", internalParametersDeclaration)
                .Replace("{InternalParametersAssignment}", internalParametersAssignment)
                .Replace("{Columns}", columns)
                .Replace("{ColumnValues}", columnValues)
                .Replace("{ProcedureName}", "uspInsert" + entityName);

            richTextBox2.Text = templateContent;

            //C#中数据操作代码
            string dalTemplateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/Insert-dal.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                dalTemplateContent = sr.ReadToEnd();
            }

            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                if (fld.Identity == false)
                {
                    sb.AppendLine("\t\tspPara.Add(\"@" + fld.ColName + "\", entity." + fld.ColName + ");");
                }
            }
            var spParams = sb.ToString();

            dalTemplateContent = dalTemplateContent
               .Replace("{TableName}", entityName)
               .Replace("{Parameters}", spParams);

            richTextBox3.Text = dalTemplateContent;
        }

        private string ParseSqlType(FieldInfo fld)
        {
            switch (fld.Type)
            {
                case "nvarchar":
                    if (fld.Length == -1)
                    {
                        return fld.Type.ToUpper() + "(max)";
                    }
                    else
                    {
                        return fld.Type.ToUpper() + "(" + fld.Length/2 + ")";
                    }
                case "varchar":
                    if (fld.Length == -1)
                    {
                        return fld.Type.ToUpper() + "(max)";
                    }
                    else
                    {
                        return fld.Type.ToUpper() + "(" + fld.Length + ")";
                    }
                default:
                    return fld.Type.ToUpper();
            }
        }

        private void frmMain_Resize(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                groupBox2.Height = this.Height - groupBox1.Height - groupBox3.Height - 80;
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            string entityName = this.txtEntityName.Text;
            List<FieldInfo> fields = GetFieldList(entityName);

            //参数说明
            StringBuilder sb = new StringBuilder();
            foreach (var fld in fields)
            {
                sb.Append(("--@" + fld.ColName).PadRight(20));
                sb.Append(("[" + ParseSqlType(fld) + "]").PadRight(18));
                sb.Append(fld.Desc);
                sb.AppendLine();

            }
            string parameterComments = sb.ToString();

            //参数
            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                sb.AppendLine(("\t@" + fld.ColName).PadRight(20) + ParseSqlType(fld) + ",");
            }
            string parameters = sb.ToString().Substring(0, sb.Length - 3);  //去掉最后一个及回车换行,

            //Internal parameter declaration
            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                sb.AppendLine(("\t\tDECLARE @_" + fld.ColName).PadRight(30) + ParseSqlType(fld));
            }
            string internalParametersDeclaration = sb.ToString();

            //Internal parameter declaration
            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                sb.Append("\t\tSET @_" + fld.ColName);
                sb.Append(" = ");
                sb.Append("@" + fld.ColName);
                sb.AppendLine();
            }
            string internalParametersAssignment = sb.ToString();

            //columns
            sb = new StringBuilder();
            string whereClause = "";
            foreach (var fld in fields)
            {
                if (!fld.Identity)
                {
                    sb.AppendLine("\t\t\t" + fld.ColName + "=@_" + fld.ColName + ",");
                }
                else
                {
                    whereClause = "\t\t\t" + fld.ColName + "= @_" + fld.ColName;
                }
            }
            string columns = sb.ToString(0, sb.Length - 3);


            string templateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/Update-sp.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                templateContent = sr.ReadToEnd();
            }

            templateContent = templateContent
                .Replace("{Author}", Environment.UserName)
                .Replace("{CreatedTime}", DateTime.Now.ToString("yyyy-MM-dd HH:ss"))
                .Replace("{TableName}", entityName)
                .Replace("{ParameterComments}", parameterComments)
                .Replace("{Parameters}", parameters)
                .Replace("{InternalParametersDeclaration}", internalParametersDeclaration)
                .Replace("{InternalParametersAssignment}", internalParametersAssignment)
                .Replace("{UpdatingColumns}", columns)
                .Replace("{UpdatingWhere}", whereClause)
                .Replace("{ProcedureName}", "uspUpdate" + entityName);

            richTextBox2.Text = templateContent;

            //C#中数据操作代码
            string dalTemplateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/Update-dal.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                dalTemplateContent = sr.ReadToEnd();
            }

            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                sb.AppendLine("\t\tspPara.Add(\"@" + fld.ColName + "\", entity." + fld.ColName + ");");
            }
            var spParams = sb.ToString();

            dalTemplateContent = dalTemplateContent
               .Replace("{TableName}", entityName)
               .Replace("{Parameters}", spParams);

            richTextBox3.Text = dalTemplateContent;
        }

        private void btnGetAll_Click(object sender, EventArgs e)
        {
            string entityName = this.txtEntityName.Text;
            List<FieldInfo> fields = GetFieldList(entityName);

            //参数说明
            StringBuilder sb = new StringBuilder();
            foreach (var fld in fields)
            {
                sb.Append(("--@" + fld.ColName).PadRight(20));
                sb.Append(("[" + ParseSqlType(fld) + "]").PadRight(18));
                sb.Append(fld.Desc);
                sb.AppendLine();

            }
            string parameterComments = sb.ToString();

            //columns
            sb = new StringBuilder();
            foreach (var fld in fields)
            {
                sb.AppendLine("\t\t\t" + fld.ColName + ",");
            }
            string columns = sb.ToString(0, sb.Length - 3);


            string templateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/GetList-sp.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                templateContent = sr.ReadToEnd();
            }

            string procedureName = "uspGet" + entityName + "List";
            templateContent = templateContent
                .Replace("{Author}", Environment.UserName)
                .Replace("{CreatedTime}", DateTime.Now.ToString("yyyy-MM-dd HH:ss"))
                .Replace("{TableName}", entityName)
                .Replace("{Columns}", columns)
                .Replace("{ParameterComments}", parameterComments)
                .Replace("{ProcedureName}", procedureName);
            
            richTextBox2.Text = templateContent;

            //C#中数据操作代码
            string dalTemplateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/GetList-dal.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                dalTemplateContent = sr.ReadToEnd();
            }

            //sb = new StringBuilder();
            //foreach (var fld in fields)
            //{
            //    sb.AppendLine("\t\tspPara.Add(@" + fld.ColName + ", entity." + fld.ColName + ")");
            //}
            //var spParams = sb.ToString();

            dalTemplateContent = dalTemplateContent
               .Replace("{TableName}", entityName)
               .Replace("{ProcedureName}", procedureName);

            richTextBox3.Text = dalTemplateContent;

        }

        private void btnGetById_Click(object sender, EventArgs e)
        {
            string entityName = this.txtEntityName.Text;
            List<FieldInfo> fields = GetFieldList(entityName);

            //参数说明
            StringBuilder sb = new StringBuilder();
            foreach (var fld in fields)
            {
                sb.Append(("--@" + fld.ColName).PadRight(20));
                sb.Append(("[" + ParseSqlType(fld) + "]").PadRight(18));
                sb.Append(fld.Desc);
                sb.AppendLine();

            }
            string parameterComments = sb.ToString();

            //columns
            sb = new StringBuilder();
            string keyColumn = "";
            foreach (var fld in fields)
            {
                if(fld.Identity)
                {
                    keyColumn = fld.ColName;
                }

                sb.AppendLine("\t\t\t" + fld.ColName + ",");
            }
            string columns = sb.ToString(0, sb.Length - 3);


            string templateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/GetById-sp.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                templateContent = sr.ReadToEnd();
            }

            string procedureName = "uspGet" + entityName + "ById";
            templateContent = templateContent
                .Replace("{Author}", Environment.UserName)
                .Replace("{CreatedTime}", DateTime.Now.ToString("yyyy-MM-dd HH:ss"))
                .Replace("{TableName}", entityName)
                .Replace("{Columns}", columns)
                .Replace("{ParameterComments}", parameterComments)
                .Replace("{ProcedureName}", procedureName)
                .Replace("{KeyColumn}", keyColumn);
            richTextBox2.Text = templateContent;

            //C#中数据操作代码
            string dalTemplateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/GetById-dal.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                dalTemplateContent = sr.ReadToEnd();
            }

            dalTemplateContent = dalTemplateContent
               .Replace("{TableName}", entityName)
               .Replace("{ProcedureName}", procedureName)
               .Replace("{KeyColumn}", keyColumn);

            richTextBox3.Text = dalTemplateContent;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            string entityName = this.txtEntityName.Text;
            List<FieldInfo> fields = GetFieldList(entityName);

            //columns
            StringBuilder sb = new StringBuilder();
            string keyColumn = "";
            foreach (var fld in fields)
            {
                if (fld.Identity)
                {
                    keyColumn = fld.ColName;

                    break;
                }
            }

            string templateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/Delete-sp.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                templateContent = sr.ReadToEnd();
            }

            string procedureName = "uspDelete" + entityName;
            templateContent = templateContent
                .Replace("{Author}", Environment.UserName)
                .Replace("{CreatedTime}", DateTime.Now.ToString("yyyy-MM-dd HH:ss"))
                .Replace("{TableName}", entityName)
                .Replace("{ProcedureName}", procedureName)
                .Replace("{KeyColumn}", keyColumn);
            richTextBox2.Text = templateContent;

            //C#中数据操作代码
            string dalTemplateContent = "";
            using (FileStream fs = File.Open(Environment.CurrentDirectory + "/Templates/Delete-dal.txt", FileMode.Open))
            {
                StreamReader sr = new StreamReader(fs);
                dalTemplateContent = sr.ReadToEnd();
            }

            dalTemplateContent = dalTemplateContent
               .Replace("{TableName}", entityName)
               .Replace("{ProcedureName}", procedureName)
               .Replace("{KeyColumn}", keyColumn);

            richTextBox3.Text = dalTemplateContent;
        }
    }
}
